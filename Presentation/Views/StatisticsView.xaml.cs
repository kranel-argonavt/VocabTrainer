using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VocabTrainer.Application.ViewModels;
using VocabTrainer.Common;

namespace VocabTrainer.Presentation.Views
{
    public partial class StatisticsView : UserControl
    {
        private StatisticsViewModel? _vm;

        public StatisticsView() => InitializeComponent();

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmChanged;
            _vm = e.NewValue as StatisticsViewModel;
            if (_vm != null)
            {
                _vm.PropertyChanged += OnVmChanged;
                Rebuild();
                UpdateProgressBar();
            }
        }

        private void OnVmChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(StatisticsViewModel.MonthYearLabel)
                               or nameof(StatisticsViewModel.CanGoNext))
                Dispatcher.Invoke(Rebuild);

            if (e.PropertyName is nameof(StatisticsViewModel.GlobalStats))
                Dispatcher.Invoke(UpdateProgressBar);
        }

        // ── Progress bar ─────────────────────────────────────────────────────

        private void ProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
            => UpdateProgressBar();

        private void UpdateProgressBar()
        {
            if (_vm == null) return;
            var stats = _vm.GlobalStats;
            if (stats == null || stats.TotalWords == 0)
            {
                BarLearned.Width  = 0;
                BarLearning.Width = 0;
                BarLearning.Margin = new Thickness(0);
                return;
            }

            double total = ProgressBarContainer.ActualWidth;
            if (total <= 0) return;

            double learnedW  = Math.Floor(total * stats.LearnedPercent  / 100.0);
            double learningW = Math.Floor(total * stats.LearningPercent / 100.0);

            // Clamp so segments don't overflow
            learnedW  = Math.Min(learnedW, total);
            learningW = Math.Min(learningW, total - learnedW);

            BarLearned.Width  = learnedW;

            // Yellow segment starts right after green
            BarLearning.Width  = learningW;
            BarLearning.Margin = new Thickness(learnedW, 0, 0, 0);
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e) => _vm?.PrevMonthCommand.Execute(null);
        private void BtnNext_Click(object sender, RoutedEventArgs e) => _vm?.NextMonthCommand.Execute(null);

        // ── Chart ─────────────────────────────────────────────────────────────

        private void Rebuild()
        {
            ChartHost.Children.Clear();
            if (_vm == null) return;

            TxtMonthYear.Text = _vm.MonthYearLabel;
            BtnNext.IsEnabled = _vm.CanGoNext;
            BtnNext.Opacity   = _vm.CanGoNext ? 1.0 : 0.35;

            var loc = LocalizationService.Instance;
            string yAxisText = loc[Strings.Stats_AxisCards];
            string xAxisText = loc[Strings.Stats_AxisDays];

            var accentColor   = TryFindResource("AccentBrush") is SolidColorBrush ab
                                    ? ab.Color : Color.FromRgb(46, 204, 113);
            var textSecondary = TryFindResource("TextSecondaryBrush") as Brush ?? Brushes.Gray;
            var borderColor   = TryFindResource("BorderBrush") is SolidColorBrush bb
                                    ? Color.FromArgb(60, bb.Color.R, bb.Color.G, bb.Color.B)
                                    : Color.FromArgb(50, 255, 255, 255);

            var first       = _vm.DisplayMonth;
            int daysInMonth = DateTime.DaysInMonth(first.Year, first.Month);
            var today       = DateTime.Today;

            var days   = new List<DateTime>();
            var values = new List<int>();
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(first.Year, first.Month, d);
                days.Add(date);
                _vm.ActivityMap.TryGetValue(date, out int cards);
                values.Add(cards);
            }

            int maxVal = values.Max();
            if (maxVal == 0) maxVal = 1;

            const double totalW  = 820;
            const double totalH  = 210;
            const double padLeft = 56;
            const double padRight= 16;
            const double padTop  = 12;
            const double padBot  = 48;

            double chartW = totalW - padLeft - padRight;
            double chartH = totalH - padTop  - padBot;

            var canvas = new Canvas { Width = totalW, Height = totalH, ClipToBounds = true };
            ChartHost.Children.Add(canvas);

            double XOf(int i) => padLeft + i * (chartW / Math.Max(days.Count - 1, 1));
            double YOf(int v) => padTop + chartH - v * (chartH / maxVal);

            // Grid lines + Y labels
            int ySteps = maxVal <= 5 ? maxVal : 5;
            for (int i = 0; i <= ySteps; i++)
            {
                double frac = (double)i / ySteps;
                int    yVal = (int)Math.Round(frac * maxVal);
                double cy   = padTop + chartH - frac * chartH;

                canvas.Children.Add(new Line
                {
                    X1 = padLeft, X2 = totalW - padRight, Y1 = cy, Y2 = cy,
                    Stroke = new SolidColorBrush(borderColor), StrokeThickness = 1,
                });

                var lbl = new TextBlock { Text = yVal.ToString(), FontSize = 10, Foreground = textSecondary };
                lbl.Measure(new Size(40, 20));
                double lw = lbl.DesiredSize.Width > 0 ? lbl.DesiredSize.Width : 14;
                Canvas.SetLeft(lbl, padLeft - lw - 6);
                Canvas.SetTop(lbl,  cy - 7);
                canvas.Children.Add(lbl);
            }

            // Y-axis title
            var yTitle = new TextBlock
            {
                Text = yAxisText, FontSize = 11, Foreground = textSecondary,
                RenderTransform = new RotateTransform(-90),
                RenderTransformOrigin = new Point(0.5, 0.5),
            };
            Canvas.SetLeft(yTitle, 4);
            Canvas.SetTop(yTitle,  padTop + chartH / 2 - 6);
            canvas.Children.Add(yTitle);

            // Gradient fill
            var fillPts = new PointCollection { new Point(XOf(0), padTop + chartH) };
            for (int i = 0; i < days.Count; i++) fillPts.Add(new Point(XOf(i), YOf(values[i])));
            fillPts.Add(new Point(XOf(days.Count - 1), padTop + chartH));

            canvas.Children.Add(new Polygon
            {
                Points = fillPts, StrokeThickness = 0,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0), EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(90, accentColor.R, accentColor.G, accentColor.B), 0),
                        new GradientStop(Color.FromArgb(0,  accentColor.R, accentColor.G, accentColor.B), 1),
                    }
                }
            });

            // Line
            var pts = new PointCollection();
            for (int i = 0; i < days.Count; i++) pts.Add(new Point(XOf(i), YOf(values[i])));
            canvas.Children.Add(new Polyline
            {
                Points = pts,
                Stroke = new SolidColorBrush(accentColor), StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            });

            // Dots
            for (int i = 0; i < days.Count; i++)
            {
                double cx = XOf(i), cy = YOf(values[i]);
                bool isToday = days[i] == today;

                if (isToday)
                {
                    var ring = new Ellipse
                    {
                        Width = 14, Height = 14,
                        Stroke = new SolidColorBrush(Color.FromArgb(100, accentColor.R, accentColor.G, accentColor.B)),
                        StrokeThickness = 2, Fill = Brushes.Transparent,
                    };
                    Canvas.SetLeft(ring, cx - 7); Canvas.SetTop(ring, cy - 7);
                    canvas.Children.Add(ring);
                }

                double r = isToday ? 4.5 : 3.5;
                var dot = new Ellipse
                {
                    Width = r * 2, Height = r * 2,
                    Fill   = new SolidColorBrush(accentColor),
                    Stroke = TryFindResource("CardBrush") as Brush ?? Brushes.DarkSlateGray,
                    StrokeThickness = 2,
                    ToolTip = values[i] > 0
                        ? $"{days[i]:dd MMM yyyy} — {values[i]} {yAxisText.ToLower()}"
                        : $"{days[i]:dd MMM yyyy} — no activity",
                };
                Canvas.SetLeft(dot, cx - r); Canvas.SetTop(dot, cy - r);
                canvas.Children.Add(dot);
            }

            // X-axis labels
            int step = days.Count <= 15 ? 2 : days.Count <= 20 ? 3 : 5;
            for (int i = 0; i < days.Count; i++)
            {
                bool show = i == 0 || i == days.Count - 1 || (i + 1) % step == 0;
                if (!show) continue;

                bool isToday = days[i] == today;
                var lbl = new TextBlock
                {
                    Text = days[i].Day.ToString(), FontSize = 10,
                    Foreground = isToday ? new SolidColorBrush(accentColor) : textSecondary,
                    FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                };
                lbl.Measure(new Size(30, 20));
                double lw = lbl.DesiredSize.Width > 0 ? lbl.DesiredSize.Width : 10;
                Canvas.SetLeft(lbl, XOf(i) - lw / 2);
                Canvas.SetTop(lbl,  padTop + chartH + 8);
                canvas.Children.Add(lbl);

                canvas.Children.Add(new Line
                {
                    X1 = XOf(i), X2 = XOf(i),
                    Y1 = padTop + chartH, Y2 = padTop + chartH + 4,
                    Stroke = new SolidColorBrush(borderColor), StrokeThickness = 1,
                });
            }

            // X-axis title
            var xTitle = new TextBlock { Text = xAxisText, FontSize = 11, Foreground = textSecondary };
            xTitle.Measure(new Size(200, 20));
            double xtW = xTitle.DesiredSize.Width > 0 ? xTitle.DesiredSize.Width : 30;
            Canvas.SetLeft(xTitle, padLeft + chartW / 2 - xtW / 2);
            Canvas.SetTop(xTitle,  padTop + chartH + 28);
            canvas.Children.Add(xTitle);
        }
    }
}
