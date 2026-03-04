using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VocabTrainer.Common;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Application.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsRepository _settingsRepo;

        [ObservableProperty] private Language _questionLanguage = Language.German;
        [ObservableProperty] private Language _answerLanguage = Language.English;
        [ObservableProperty] private int _wordsPerSession = 10;
        [ObservableProperty] private bool _darkTheme;
        [ObservableProperty] private bool _enableTts;
        [ObservableProperty] private bool _timerMode;
        [ObservableProperty] private int _timerSeconds = 30;
        [ObservableProperty] private TrainingMode _defaultTrainingMode = TrainingMode.Flashcard;
        [ObservableProperty] private double _levenshteinTolerance = 0.2;
        [ObservableProperty] private AppLanguage _interfaceLanguage = AppLanguage.English;

        public ObservableCollection<Language> Languages { get; } = new(System.Enum.GetValues<Language>());
        public ObservableCollection<TrainingMode> TrainingModes { get; } = new(System.Enum.GetValues<TrainingMode>());
        public ObservableCollection<AppLanguage> AppLanguages { get; } = new(System.Enum.GetValues<AppLanguage>());

        public SettingsViewModel(ISettingsRepository settingsRepo)
        {
            _settingsRepo = settingsRepo;
        }

        /// <summary>Called every time user navigates to Settings tab.</summary>
        public async Task LoadAsync()
        {
            var s = await _settingsRepo.LoadAsync();
            ApplyToFields(s);
        }

        public void LoadSettings(AppSettings s) => ApplyToFields(s);

        private void ApplyToFields(AppSettings s)
        {
            QuestionLanguage    = s.QuestionLanguage;
            AnswerLanguage      = s.AnswerLanguage;
            WordsPerSession     = s.WordsPerSession;
            DarkTheme           = s.DarkTheme;
            EnableTts           = s.EnableTts;
            TimerMode           = s.TimerMode;
            TimerSeconds        = s.TimerSeconds;
            DefaultTrainingMode = s.DefaultTrainingMode;
            LevenshteinTolerance = s.LevenshteinTolerance;
            InterfaceLanguage   = s.InterfaceLanguage;
        }

        private AppSettings ToSettings() => new()
        {
            QuestionLanguage    = QuestionLanguage,
            AnswerLanguage      = AnswerLanguage,
            WordsPerSession     = WordsPerSession,
            DarkTheme           = DarkTheme,
            EnableTts           = EnableTts,
            TimerMode           = TimerMode,
            TimerSeconds        = TimerSeconds,
            DefaultTrainingMode = DefaultTrainingMode,
            LevenshteinTolerance = LevenshteinTolerance,
            InterfaceLanguage   = InterfaceLanguage,
        };

        [RelayCommand]
        private async Task Save()
        {
            var settings = ToSettings();
            await _settingsRepo.SaveAsync(settings);
            ApplyTheme(DarkTheme);
            LocalizationService.Instance.Language = InterfaceLanguage;
            ShowInfo(LocalizationService.Instance[Strings.Settings_Saved]);
        }

        [RelayCommand]
        private async Task ResetToDefaults()
        {
            var defaults = new AppSettings(); // all default values
            ApplyToFields(defaults);
            await _settingsRepo.SaveAsync(defaults);
            ApplyTheme(defaults.DarkTheme);
            LocalizationService.Instance.Language = defaults.InterfaceLanguage;
            ShowInfo("Settings reset to defaults.");
        }

        public static void ApplyTheme(bool dark)
        {
            var app = System.Windows.Application.Current;
            var uri = new System.Uri(dark
                ? "pack://application:,,,/Presentation/Themes/DarkTheme.xaml"
                : "pack://application:,,,/Presentation/Themes/LightTheme.xaml");

            var dicts = app.Resources.MergedDictionaries;
            for (int i = dicts.Count - 1; i >= 0; i--)
            {
                var src = dicts[i].Source?.ToString() ?? "";
                if (src.Contains("Theme.xaml")) dicts.RemoveAt(i);
            }
            dicts.Insert(0, new System.Windows.ResourceDictionary { Source = uri });
        }
    }
}
