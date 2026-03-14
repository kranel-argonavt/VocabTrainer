using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;
using VocabTrainer.Common;

namespace VocabTrainer.Application.ViewModels
{
    public partial class StatisticsViewModel : BaseViewModel
    {
        private readonly IStatisticsService _stats;

        // Raw activity data: date -> cards reviewed
        public Dictionary<DateTime, int> ActivityMap { get; private set; } = new();

        [ObservableProperty] private GlobalStats _globalStats = new();
        [ObservableProperty] private ObservableCollection<DailyProgress> _dailyProgress = new();
        [ObservableProperty] private string _monthYearLabel = "";
        [ObservableProperty] private bool _canGoNext;

        private DateTime _displayMonth;

        public StatisticsViewModel(IStatisticsService stats)
        {
            _stats = stats;
            _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            GlobalStats = await _stats.GetGlobalStatsAsync();

            var daily = await _stats.GetDailyProgressAsync(366);
            DailyProgress.Clear();
            foreach (var d in daily) DailyProgress.Add(d);
            ActivityMap = daily.ToDictionary(d => d.Date.Date, d => d.CardsReviewed);

            UpdateLabels();
            IsLoading = false;
        }

        private void UpdateLabels()
        {
            var today = DateTime.Today;
            var culture = LocalizationService.Instance.Language == AppLanguage.Ukrainian
                ? new CultureInfo("uk-UA")
                : new CultureInfo("en-US");
            MonthYearLabel = _displayMonth.ToString("MMMM yyyy", culture);
            CanGoNext = _displayMonth < new DateTime(today.Year, today.Month, 1);
        }

        public DateTime DisplayMonth => _displayMonth;

        [RelayCommand]
        public void PrevMonth()
        {
            _displayMonth = _displayMonth.AddMonths(-1);
            UpdateLabels();
        }

        [RelayCommand]
        public void NextMonth()
        {
            if (CanGoNext)
            {
                _displayMonth = _displayMonth.AddMonths(1);
                UpdateLabels();
            }
        }

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();
    }
}
