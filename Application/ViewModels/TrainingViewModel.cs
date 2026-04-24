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
        private const int BaseMinimumWordsPerSession = 5;
        private const int AbsoluteMaximumWordsPerSession = 50;

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
        [ObservableProperty] private int  _maxWordsPerSession = AbsoluteMaximumWordsPerSession;

        public int MinWordsPerSession => Math.Min(BaseMinimumWordsPerSession, MaxWordsPerSession);
        public int MidWordsPerSession => CalculateMidWordsPerSession(MinWordsPerSession, MaxWordsPerSession);
        public bool HasAvailableWords => FilteredWordCount > 0;
        public ObservableCollection<WordsScaleLabel> WordsScaleLabels { get; } = new();

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

        // ── Tag filter ────────────────────────────────────────────────────────
        [ObservableProperty] private string _tagSearchQuery = string.Empty;
        [ObservableProperty] private int    _filteredWordCount;

        /// <summary>All tags from DB, filtered by TagSearchQuery for display.</summary>
        public ObservableCollection<TagItem> AvailableTags  { get; } = new();

        /// <summary>Tags the user has pinned/selected.</summary>
        public ObservableCollection<TagItem> SelectedTags   { get; } = new();

        partial void OnTagSearchQueryChanged(string value) => ApplyTagSearch();
        partial void OnWordsPerSessionChanged(int value) => CoerceWordsPerSession();
        partial void OnFilteredWordCountChanged(int value)
        {
            UpdateWordsPerSessionRange(value);
            OnPropertyChanged(nameof(HasAvailableWords));
        }
        partial void OnMaxWordsPerSessionChanged(int value)
        {
            OnPropertyChanged(nameof(MinWordsPerSession));
            OnPropertyChanged(nameof(MidWordsPerSession));
            RefreshWordsScaleLabels();
            CoerceWordsPerSession();
        }

        private void ApplyTagSearch()
        {
            var q = TagSearchQuery.Trim().ToLowerInvariant();
            foreach (var tag in AvailableTags)
                tag.IsVisible = q.Length == 0 || tag.Name.ToLowerInvariant().Contains(q);
        }

        [RelayCommand]
        private async Task ToggleTag(TagItem tag)
        {
            if (SelectedTags.Contains(tag))
            {
                SelectedTags.Remove(tag);
                tag.IsSelected = false;
            }
            else
            {
                SelectedTags.Add(tag);
                tag.IsSelected = true;
            }
            await RefreshFilteredCountAsync();
        }

        private async Task RefreshFilteredCountAsync()
        {
            var tags = SelectedTags.Select(t => t.Name).ToList();
            FilteredWordCount = await _trainingService.GetFilteredCountAsync(tags);
        }

        private void UpdateWordsPerSessionRange(int availableWords)
        {
            MaxWordsPerSession = Math.Clamp(
                availableWords > 0 ? availableWords : BaseMinimumWordsPerSession,
                1,
                AbsoluteMaximumWordsPerSession);
        }

        private void CoerceWordsPerSession()
        {
            var clamped = Math.Clamp(WordsPerSession, MinWordsPerSession, MaxWordsPerSession);
            if (clamped != WordsPerSession)
            {
                WordsPerSession = clamped;
                return;
            }

            _settings.WordsPerSession = clamped;
        }

        private static int CalculateMidWordsPerSession(int min, int max)
        {
            if (max <= min)
                return max;

            return min + ((max - min) / 2);
        }

        private void RefreshWordsScaleLabels()
        {
            WordsScaleLabels.Clear();

            int min = MinWordsPerSession;
            int max = MaxWordsPerSession;

            for (int value = min; value <= max; value += 5)
                WordsScaleLabels.Add(new WordsScaleLabel(value));

            if (WordsScaleLabels.Count == 0 || WordsScaleLabels[^1].Value != max)
                WordsScaleLabels.Add(new WordsScaleLabel(max));
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
            RefreshWordsScaleLabels();
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

            // Load tags from DB — preserve selected ones across sessions within same app run
            var selected = SelectedTags.Select(t => t.Name).ToHashSet();
            var allTags  = await _trainingService.GetAllTagsAsync();
            AvailableTags.Clear();
            foreach (var name in allTags)
            {
                var item = new TagItem(name) { IsSelected = selected.Contains(name) };
                AvailableTags.Add(item);
                if (item.IsSelected && !SelectedTags.Any(t => t.Name == name))
                    SelectedTags.Add(item);
            }
            ApplyTagSearch();
            await RefreshFilteredCountAsync();

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
            if (!HasAvailableWords)
                return;

            WordsPerSession = Math.Clamp(WordsPerSession, MinWordsPerSession, MaxWordsPerSession);

            // Save selected languages to settings
            _settings.QuestionLanguage = QuestionLanguage;
            _settings.AnswerLanguage   = AnswerLanguage;
            _settings.WordsPerSession  = WordsPerSession;
            await _settingsRepo.SaveAsync(_settings);

            IsConfiguring = false;
            IsLoading = true;
            var tags = SelectedTags.Count > 0
                ? SelectedTags.Select(t => t.Name).ToList()
                : null;
            _sessionWords = await _trainingService.GetSessionWordsAsync(WordsPerSession, tags);
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

    public sealed class WordsScaleLabel
    {
        public WordsScaleLabel(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
