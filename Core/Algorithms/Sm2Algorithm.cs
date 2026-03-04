using System;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Core.Algorithms
{
    /// <summary>
    /// Simplified SM-2 spaced repetition algorithm.
    /// quality: 5=perfect, 4=correct hesitation, 3=correct difficulty, 
    ///          2=wrong easy recall, 1=wrong blackout, 0=total blackout
    /// </summary>
    public class Sm2Algorithm : ISpacedRepetitionService
    {
        private const double MinEaseFactor = 1.3;

        public void ApplyResult(WordCard card, bool correct, int quality = -1)
        {
            // Default quality from correct/wrong if not specified
            if (quality < 0) quality = correct ? 4 : 1;

            card.ReviewCount++;
            card.LastReviewed = DateTime.UtcNow;

            if (correct)
            {
                card.CorrectAnswers++;
                CalculateNextInterval(card, quality);
            }
            else
            {
                card.WrongAnswers++;
                // Reset interval on failure
                card.IntervalDays = 1;
                card.EaseFactor = Math.Max(MinEaseFactor, card.EaseFactor - 0.2);
                card.NextReview = DateTime.UtcNow.AddDays(1);
            }
        }

        private static void CalculateNextInterval(WordCard card, int quality)
        {
            int newInterval;

            if (card.ReviewCount == 1)
                newInterval = 1;
            else if (card.ReviewCount == 2)
                newInterval = 6;
            else
                newInterval = (int)Math.Round(card.IntervalDays * card.EaseFactor);

            // Update ease factor: EF' = EF + (0.1 - (5-q)*(0.08+(5-q)*0.02))
            double efDelta = 0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02);
            card.EaseFactor = Math.Max(MinEaseFactor, card.EaseFactor + efDelta);

            card.IntervalDays = Math.Max(1, newInterval);
            card.NextReview = DateTime.UtcNow.AddDays(card.IntervalDays);
        }
    }
}
