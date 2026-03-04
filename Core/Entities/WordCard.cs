using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VocabTrainer.Core.Entities
{
    public class WordCard
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string German { get; set; } = string.Empty;

        [MaxLength(200)]
        public string English { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Ukrainian { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string ExampleSentence { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Tags { get; set; } = string.Empty;

        // Statistics
        public int CorrectAnswers { get; set; } = 0;
        public int WrongAnswers { get; set; } = 0;
        public DateTime? LastReviewed { get; set; }
        public DateTime NextReview { get; set; } = DateTime.UtcNow;
        public int IntervalDays { get; set; } = 1;
        public double EaseFactor { get; set; } = 2.5;
        public int ReviewCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Calculated
        public double SuccessRate =>
            (CorrectAnswers + WrongAnswers) == 0
                ? 0
                : Math.Round((double)CorrectAnswers / (CorrectAnswers + WrongAnswers) * 100, 1);

        public bool IsDueToday => NextReview.Date <= DateTime.UtcNow.Date;

        public DifficultyLevel Difficulty =>
            SuccessRate >= 80 ? DifficultyLevel.Easy :
            SuccessRate >= 50 ? DifficultyLevel.Medium :
            DifficultyLevel.Hard;

        public List<string> GetTags() =>
            string.IsNullOrWhiteSpace(Tags)
                ? new List<string>()
                : new List<string>(Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        /// <summary>
        /// Returns the raw translation string (may contain "/" separated variants).
        /// Use GetTranslations() for validation or GetDisplayTranslation() for UI.
        /// </summary>
        public string GetTranslation(Language lang) => lang switch
        {
            Language.German => German,
            Language.English => English,
            Language.Ukrainian => Ukrainian,
            _ => German
        };

        /// <summary>All accepted variants split by "/", trimmed.</summary>
        public List<string> GetTranslations(Language lang)
        {
            var raw = GetTranslation(lang);
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return new List<string>(
                raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        /// <summary>Human-readable display string showing all variants.</summary>
        public string GetDisplayTranslation(Language lang)
        {
            var variants = GetTranslations(lang);
            return variants.Count == 0 ? string.Empty : string.Join(" / ", variants);
        }

        public void SetTranslation(Language lang, string value)
        {
            switch (lang)
            {
                case Language.German: German = value; break;
                case Language.English: English = value; break;
                case Language.Ukrainian: Ukrainian = value; break;
            }
        }
    }

    public enum Language { German, English, Ukrainian }

    public enum DifficultyLevel { Easy, Medium, Hard }

    public enum TrainingMode { Flashcard, MultipleChoice, TextInput, Mixed }
}
