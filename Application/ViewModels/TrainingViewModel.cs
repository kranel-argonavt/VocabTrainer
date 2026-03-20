using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Application.ViewModels
{
    public partial class TrainingViewModel : BaseViewModel
    {
        private readonly ITrainingService _trainingService;
        private readonly ISettingsRepository _settingsRepo;
        private readonly Random _rng = new();

        private List<WordCard> _sessionWords = new();
        private int _currentIndex = 0;
        private AppSettings _settings = new();
        private DateTime _sessionStart = DateTime.Now;

        // ── Config screen ──────────────────────────────────────────────────────
        [ObservableProperty] private bool _isConfiguring = true;
        [ObservableProperty] private int  _wordsPerSession = 10;

        // ── Language selection ─────────────────────────────────────────────────
        [ObservableProperty] private Language _questionLanguage = Language.German;
        [ObservableProperty] private Language _answerLanguage   = Language.Ukrainian;

        public ObservableCollection<Language> AvailableLanguages { get; } =
            new(System.Enum.GetValues<Language>());

        partial void OnQuestionLanguageChanged(Language value)
        {
            if (AnswerLanguage == value)
                AnswerLanguage = AvailableLanguages.First(l => l != value);
        }

        partial void OnAnswerLanguageChanged(Language value)
        {
            if (QuestionLanguage == value)
                QuestionLanguage = AvailableLanguages.First(l => l != value);
        }

        // ── Card state ─────────────────────────────────────────────────────────
        [ObservableProperty] private WordCard? _currentCard;
        [ObservableProperty] private bool _showAnswer;
        [ObservableProperty] private bool _sessionComplete;
        [ObservableProperty] private int _progress;
        [ObservableProperty] private int _totalCards;
        [ObservableProperty] private TrainingMode _currentMode = TrainingMode.Flashcard;
        [ObservableProperty] private string _questionText = string.Empty;
        [ObservableProperty] private string _answerText = string.Empty;
        [ObservableProperty] private ObservableCollection<MultipleChoiceOption> _multipleChoiceOptions = new();
        [ObservableProperty] private string _userInput = string.Empty;
        [ObservableProperty] private bool _inputValidated;
        [ObservableProperty] private bool _inputCorrect;
        [ObservableProperty] private string _inputFeedback = string.Empty;
        [ObservableProperty] private int _timerRemaining;
        [ObservableProperty] private bool _timerActive;
        [ObservableProperty] private TrainingMode _effectiveMode;
        [ObservableProperty] private int _sessionCorrect;
        [ObservableProperty] private int _sessionWrong;
        [ObservableProperty] private double _sessionAccuracy;
        [ObservableProperty] private string _sessionDuration = "00:00";

        public ObservableCollection<TrainingMode> AllModes { get; } =
            new(System.Enum.GetValues<TrainingMode>());

        private CancellationTokenSource? _timerCts;

        /// <summary>Called by MainViewModel to get notified when a session ends.</summary>
        public Action? OnSessionCompleted { get; set; }

        public TrainingViewModel(ITrainingService trainingService, ISettingsRepository settingsRepo)
        {
            _trainingService = trainingService;
            _settingsRepo = settingsRepo;
        }

        // ── Entry point: load settings and show config screen ──────────────────
        public async Task InitializeAsync()
        {
            IsLoading = true;
            _settings = await _settingsRepo.LoadAsync();
            CurrentMode      = _settings.DefaultTrainingMode;
            WordsPerSession  = _settings.WordsPerSession;
            QuestionLanguage = _settings.QuestionLanguage;
            AnswerLanguage   = _settings.AnswerLanguage;
            SessionComplete  = false;
            IsLoading        = false;
            IsConfiguring    = true;
        }

        // ── Mode selection on config screen ───────────────────────────────────
        [RelayCommand] private void SetModeFlashcard()    => CurrentMode = TrainingMode.Flashcard;
        [RelayCommand] private void SetModeMultipleChoice() => CurrentMode = TrainingMode.MultipleChoice;
        [RelayCommand] private void SetModeTextInput()    => CurrentMode = TrainingMode.TextInput;
        [RelayCommand] private void SetModeMixed()        => CurrentMode = TrainingMode.Mixed;

        // ── Start button on config screen ──────────────────────────────────────
        [RelayCommand]
        private async Task StartSession()
        {
            // Save selected languages to settings
            _settings.QuestionLanguage = QuestionLanguage;
            _settings.AnswerLanguage   = AnswerLanguage;
            await _settingsRepo.SaveAsync(_settings);

            IsConfiguring = false;
            IsLoading = true;
            _sessionWords = await _trainingService.GetSessionWordsAsync(WordsPerSession);
            _currentIndex = 0;
            _sessionStart = DateTime.Now;
            SessionCorrect = 0;
            SessionWrong = 0;
            SessionAccuracy = 0;
            SessionDuration = "00:00";
            TotalCards = _sessionWords.Count;
            IsLoading = false;

            if (_sessionWords.Any())
                await ShowCardAsync();
        }

        private async Task ShowCardAsync()
        {
            _timerCts?.Cancel();
            _timerCts = null;

            if (_currentIndex >= _sessionWords.Count)
            {
                SessionComplete = true;
                UpdateSessionDisplay();
                OnSessionCompleted?.Invoke();
                return;
            }

            CurrentCard = _sessionWords[_currentIndex];
            ShowAnswer = false;
            InputValidated = false;
            InputFeedback = string.Empty;
            UserInput = string.Empty;
            Progress = _currentIndex + 1;

            EffectiveMode = CurrentMode == TrainingMode.Mixed
                ? (TrainingMode)_rng.Next(0, 3)
                : CurrentMode;

            // Show one random variant for question (not all "/" separated)
            QuestionText = PickVariant(CurrentCard.GetTranslation(_settings.QuestionLanguage));
            AnswerText   = CurrentCard.GetDisplayTranslation(_settings.AnswerLanguage);

            if (EffectiveMode == TrainingMode.MultipleChoice)
            {
                var options = await _trainingService.GetMultipleChoiceOptionsAsync(CurrentCard);
                MultipleChoiceOptions.Clear();
                foreach (var opt in options)
                    MultipleChoiceOptions.Add(new MultipleChoiceOption
                    {
                        // Show one random variant per option button
                        Text      = PickVariant(opt.GetTranslation(_settings.AnswerLanguage)),
                        IsCorrect = opt.Id == CurrentCard.Id,
                        Card      = opt
                    });
            }
            else
            {
                MultipleChoiceOptions.Clear();
            }

            if (_settings.TimerMode && EffectiveMode != TrainingMode.Flashcard)
                StartTimer();
        }

        /// <summary>Returns one random variant from a "/" separated string.</summary>
        private string PickVariant(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            var parts = raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length == 0 ? raw : parts[_rng.Next(parts.Length)];
        }

        [RelayCommand]
        private void RevealAnswer()
        {
            ShowAnswer = true;
            _timerCts?.Cancel();
        }

        [RelayCommand]
        private async Task KnewIt() => await SubmitAnswer(true);

        [RelayCommand]
        private async Task DidNotKnow() => await SubmitAnswer(false);

        [RelayCommand]
        private async Task SelectMultipleChoice(MultipleChoiceOption option)
        {
            _timerCts?.Cancel();
            foreach (var o in MultipleChoiceOptions)
            {
                o.IsSelected = o == option;
                o.ShowResult = true;
            }
            ShowAnswer = true;
            await Task.Delay(600);
            await SubmitAnswer(option.IsCorrect);
        }

        [RelayCommand]
        private async Task ValidateInput()
        {
            if (CurrentCard == null) return;
            _timerCts?.Cancel();
            // Validate against raw field (with "/" variants) not display text
            var rawAnswer = CurrentCard.GetTranslation(_settings.AnswerLanguage);
            bool correct = _trainingService.ValidateTextInput(
                UserInput, rawAnswer, _settings.LevenshteinTolerance);
            InputCorrect = correct;
            // Show accepted variants in feedback
            var displayAnswer = CurrentCard.GetDisplayTranslation(_settings.AnswerLanguage);
            InputFeedback = correct
                ? $"✓ Correct! ({displayAnswer})"
                : $"✗ Wrong. Accepted: {displayAnswer}";
            InputValidated = true;
            ShowAnswer = true;
            await Task.Delay(800);
            await SubmitAnswer(correct);
        }

        private async Task SubmitAnswer(bool correct)
        {
            if (CurrentCard == null) return;
            await _trainingService.ProcessAnswerAsync(CurrentCard, correct);
            if (correct) SessionCorrect++;
            else SessionWrong++;
            UpdateSessionDisplay();
            await Task.Delay(300);
            _currentIndex++;
            await ShowCardAsync();
        }

        private void UpdateSessionDisplay()
        {
            int total = SessionCorrect + SessionWrong;
            SessionAccuracy = total == 0 ? 0 : Math.Round((double)SessionCorrect / total * 100, 1);
            var elapsed = DateTime.Now - _sessionStart;
            SessionDuration = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        }

        [RelayCommand]
        private async Task RestartSession() => await InitializeAsync();

        private void StartTimer()
        {
            _timerCts = new CancellationTokenSource();
            TimerRemaining = _settings.TimerSeconds;
            TimerActive = true;
            var token = _timerCts.Token;

            Task.Run(async () =>
            {
                while (TimerRemaining > 0 && !token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    if (!token.IsCancellationRequested)
                        TimerRemaining--;
                }
                if (!token.IsCancellationRequested && TimerRemaining <= 0)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                        async () => await SubmitAnswer(false));
                }
                TimerActive = false;
            }, CancellationToken.None);
        }
    }

    public partial class MultipleChoiceOption : ObservableObject
    {
        [ObservableProperty] private string _text = string.Empty;
        [ObservableProperty] private bool _isCorrect;
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _showResult;
        public WordCard Card { get; set; } = null!;
    }
}
