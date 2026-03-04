using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Application.ViewModels
{
    public partial class StatisticsViewModel : BaseViewModel
    {
        private readonly IStatisticsService _stats;
        private readonly IWordCardRepository _repository;

        [ObservableProperty] private GlobalStats _globalStats = new();
        [ObservableProperty] private ObservableCollection<WordCard> _hardestWords = new();
        [ObservableProperty] private ObservableCollection<WordCard> _bestWords = new();
        [ObservableProperty] private ObservableCollection<DailyProgress> _dailyProgress = new();

        public StatisticsViewModel(IStatisticsService stats, IWordCardRepository repository)
        {
            _stats = stats;
            _repository = repository;
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            GlobalStats = await _stats.GetGlobalStatsAsync();
            var daily = await _stats.GetDailyProgressAsync(30);
            DailyProgress.Clear();
            foreach (var d in daily) DailyProgress.Add(d);

            var all = await _repository.GetAllAsync();
            HardestWords.Clear();
            foreach (var w in all.Where(w => w.ReviewCount > 0).OrderBy(w => w.SuccessRate).Take(10))
                HardestWords.Add(w);
            BestWords.Clear();
            foreach (var w in all.Where(w => w.ReviewCount > 0).OrderByDescending(w => w.SuccessRate).Take(10))
                BestWords.Add(w);
            IsLoading = false;
        }

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();
    }
}
