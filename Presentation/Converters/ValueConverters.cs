using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using VocabTrainer.Core.Entities;

namespace VocabTrainer.Presentation.Converters
{
    // bool → Visibility
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            value is Visibility.Visible;
    }

    // bool → Visibility (inverted)
    public class BoolToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            value is Visibility.Collapsed;
    }

    // object != null → Visibility.Visible
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value != null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            Binding.DoNothing;
    }

    // object == null → Visibility.Visible  (show home when no view selected)
    public class NullToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value == null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            Binding.DoNothing;
    }

    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value?.ToString() ?? string.Empty;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            value == null ? Binding.DoNothing : Enum.Parse(t, value.ToString()!);
    }

    public class DifficultyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is DifficultyLevel diff)
                return diff switch
                {
                    DifficultyLevel.Easy   => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                    DifficultyLevel.Medium => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
                    _                      => new SolidColorBrush(Color.FromRgb(231, 76, 60))
                };
            return Brushes.Gray;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    public class PercentageToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type t, object p, CultureInfo c)
        {
            if (values.Length >= 2 && values[0] is double pct && values[1] is double total)
                return total * pct / 100.0;
            return 0.0;
        }
        public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => Array.Empty<object>();
    }

    public class MultipleChoiceColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type t, object p, CultureInfo c)
        {
            if (values.Length >= 3
                && values[0] is bool showResult
                && values[1] is bool isCorrect
                && values[2] is bool isSelected)
            {
                if (!showResult)           return new SolidColorBrush(Color.FromRgb(74, 144, 217));
                if (isCorrect)             return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                if (isSelected && !isCorrect) return new SolidColorBrush(Color.FromRgb(231, 76, 60));
                return new SolidColorBrush(Color.FromRgb(149, 165, 166));
            }
            return new SolidColorBrush(Color.FromRgb(74, 144, 217));
        }
        public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => Array.Empty<object>();
    }

    /// <summary>
    /// Maps CalendarCell.Level (0–4) to a background brush.
    /// 0 = no activity, 1–4 = green intensity.
    /// </summary>
    public class CalendarLevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int level = value is int i ? i : 0;
            return level switch
            {
                1 => new SolidColorBrush(Color.FromRgb(187, 247, 208)), // green-200
                2 => new SolidColorBrush(Color.FromRgb(74,  222, 128)), // green-400
                3 => new SolidColorBrush(Color.FromRgb(34,  197, 94)),  // green-500
                4 => new SolidColorBrush(Color.FromRgb(21,  128, 61)),  // green-700
                _ => new SolidColorBrush(Color.FromRgb(243, 244, 246))  // gray-100 – no activity
            };
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    /// <summary>
    /// Dims cells that belong to adjacent months (IsCurrentMonth = false → opacity 0.3).
    /// </summary>
    public class CurrentMonthToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? 1.0 : 0.25;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
