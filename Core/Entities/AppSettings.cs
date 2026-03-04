using VocabTrainer.Common;

namespace VocabTrainer.Core.Entities
{
    public class AppSettings
    {
        public Language QuestionLanguage { get; set; } = Language.German;
        public Language AnswerLanguage { get; set; } = Language.English;
        public int WordsPerSession { get; set; } = 10;
        public bool DarkTheme { get; set; } = false;
        public bool EnableTts { get; set; } = false;
        public bool TimerMode { get; set; } = false;
        public int TimerSeconds { get; set; } = 30;
        public TrainingMode DefaultTrainingMode { get; set; } = TrainingMode.Flashcard;
        public double LevenshteinTolerance { get; set; } = 0.2;
        public AppLanguage InterfaceLanguage { get; set; } = AppLanguage.English;
    }
}
