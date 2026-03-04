using System;

namespace VocabTrainer.Core.Entities
{
    public class SessionStats
    {
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }

        public int TotalAnswered => CorrectAnswers + WrongAnswers;
        public double AccuracyPercent => TotalAnswered == 0 ? 0 : Math.Round((double)CorrectAnswers / TotalAnswered * 100, 1);
        public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;
    }

    public class GlobalStats
    {
        public int TotalWords { get; set; }
        public int LearnedWords { get; set; }    // SuccessRate >= 80 and ReviewCount >= 5
        public int LearningWords { get; set; }   // In progress
        public int NewWords { get; set; }        // ReviewCount == 0
        public int DueToday { get; set; }
        public double OverallAccuracy { get; set; }
        public int TotalReviews { get; set; }
        public int Streak { get; set; }          // Consecutive days practiced
        public double LearnedPercent => TotalWords == 0 ? 0 : Math.Round((double)LearnedWords / TotalWords * 100, 1);
    }
}
