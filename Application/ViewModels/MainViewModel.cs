using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Application.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IWordCardRepository _repository;
        private readonly ISettingsRepository _settingsRepo;
        private readonly IServiceProvider _sp;

        [ObservableProperty] private BaseViewModel? _currentView;
        [ObservableProperty] private GlobalStats _globalStats = new();
        [ObservableProperty] private AppSettings _settings = new();

        public MainViewModel(
            IWordCardRepository repository,
            ISettingsRepository settingsRepo,
            IServiceProvider serviceProvider)
        {
            _repository = repository;
            _settingsRepo = settingsRepo;
            _sp = serviceProvider;
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            Settings = await _settingsRepo.LoadAsync();
            GlobalStats = await _repository.GetGlobalStatsAsync();
            IsLoading = false;
        }

        [RelayCommand]
        private async Task NavigateToTraining()
        {
            var vm = _sp.GetRequiredService<TrainingViewModel>();
            await vm.InitializeAsync();
            CurrentView = vm;
        }

        [RelayCommand]
        private async Task NavigateToStatistics()
        {
            var vm = _sp.GetRequiredService<StatisticsViewModel>();
            await vm.LoadAsync();
            CurrentView = vm;
        }

        [RelayCommand]
        private async Task NavigateToWordManagement()
        {
            var vm = _sp.GetRequiredService<WordManagementViewModel>();
            await vm.LoadAsync();
            CurrentView = vm;
        }

        [RelayCommand]
        private void NavigateToImport()
        {
            CurrentView = _sp.GetRequiredService<ImportViewModel>();
        }

        [RelayCommand]
        private async Task NavigateToSettings()
        {
            var vm = _sp.GetRequiredService<SettingsViewModel>();
            await vm.LoadAsync(); // always load fresh from file
            CurrentView = vm;
        }

        [RelayCommand]
        private void NavigateHome()
        {
            CurrentView = null;
        }

        public async Task RefreshStats()
        {
            GlobalStats = await _repository.GetGlobalStatsAsync();
        }
    }
}
