using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;
using VocabTrainer.Infrastructure.Data;

namespace VocabTrainer.Infrastructure.Repositories
{
    public class WordCardRepository : IWordCardRepository
    {
        private readonly IDbContextFactory<VocabDbContext> _factory;

        public WordCardRepository(IDbContextFactory<VocabDbContext> factory)
        {
            _factory = factory;
        }

        private VocabDbContext Ctx() => _factory.CreateDbContext();

        public async Task<List<WordCard>> GetAllAsync()
        {
            await using var ctx = Ctx();
            return await ctx.WordCards.OrderBy(w => w.German).ToListAsync();
        }

        public async Task<WordCard?> GetByIdAsync(int id)
        {
            await using var ctx = Ctx();
            return await ctx.WordCards.FindAsync(id);
        }

        public async Task<List<WordCard>> GetDueTodayAsync()
        {
            await using var ctx = Ctx();
            var today = DateTime.UtcNow.Date;
            return await ctx.WordCards
                .Where(w => w.NextReview <= today)
                .OrderBy(w => w.NextReview)
                .ToListAsync();
        }

        public async Task<List<WordCard>> GetByTagAsync(string tag)
        {
            await using var ctx = Ctx();
            return await ctx.WordCards.Where(w => w.Tags.Contains(tag)).ToListAsync();
        }

        public async Task<List<string>> GetAllTagsAsync()
        {
            await using var ctx = Ctx();
            var allTags = await ctx.WordCards
                .Where(w => w.Tags != null && w.Tags != string.Empty)
                .Select(w => w.Tags)
                .ToListAsync();

            return allTags
                .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        public async Task<int> GetFilteredCountAsync(IReadOnlyList<string> tags)
        {
            await using var ctx = Ctx();
            if (tags == null || tags.Count == 0)
                return await ctx.WordCards.CountAsync();

            var all = await ctx.WordCards.ToListAsync();
            return all.Count(w => MatchesTags(w.Tags, tags));
        }

        /// <summary>OR logic: card matches if it has at least one of the selected tags.</summary>
        private static bool MatchesTags(string cardTags, IReadOnlyList<string> selectedTags)
        {
            if (string.IsNullOrWhiteSpace(cardTags)) return false;
            var cardTagSet = cardTags
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return selectedTags.Any(t => cardTagSet.Contains(t));
        }

        public async Task<List<WordCard>> GetSessionWordsAsync(int count, Language questionLang, Language answerLang, IReadOnlyList<string>? tags = null)
        {
            List<WordCard> pool;
            await using var ctx = Ctx();
            var all = await ctx.WordCards.ToListAsync();

            pool = (tags != null && tags.Count > 0)
                ? all.Where(w => MatchesTags(w.Tags, tags)).ToList()
                : all;

            var today = DateTime.UtcNow.Date;
            var due   = pool.Where(w => w.NextReview.Date <= today).OrderBy(w => w.NextReview).ToList();

            if (due.Count >= count) return due.Take(count).ToList();

            var dueIds = due.Select(w => w.Id).ToHashSet();
            var extra  = pool
                .Where(w => !dueIds.Contains(w.Id))
                .OrderBy(w => w.NextReview)
                .Take(count - due.Count)
                .ToList();

            return due.Concat(extra).ToList();
        }


        public async Task<List<WordCard>> SearchAsync(string query)
        {
            await using var ctx = Ctx();
            return await ctx.WordCards
                .Where(w => w.German.Contains(query) || w.English.Contains(query) || w.Ukrainian.Contains(query))
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string german)
        {
            await using var ctx = Ctx();
            return await ctx.WordCards.AnyAsync(w => w.German.ToLower() == german.ToLower());
        }

        public async Task AddAsync(WordCard card)
        {
            await using var ctx = Ctx();
            ctx.WordCards.Add(card);
            await ctx.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<WordCard> cards)
        {
            await using var ctx = Ctx();
            ctx.WordCards.AddRange(cards);
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(WordCard card)
        {
            await using var ctx = Ctx();
            ctx.WordCards.Update(card);
            await ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var ctx = Ctx();
            var card = await ctx.WordCards.FindAsync(id);
            if (card != null)
            {
                ctx.WordCards.Remove(card);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task<GlobalStats> GetGlobalStatsAsync()
        {
            await using var ctx = Ctx();
            var all    = await ctx.WordCards.ToListAsync();
            var today  = DateTime.UtcNow.Date;
            var total  = all.Count;

            // Learned: reviewed at least once with SuccessRate >= 80%
            var learned  = all.Count(w => w.ReviewCount >= 1 && w.SuccessRate >= 80);
            var learning = all.Count(w => w.ReviewCount >= 1 && w.SuccessRate < 80);
            var newWords = all.Count(w => w.ReviewCount == 0);
            // Due today: NextReview is today or in the past
            var due      = all.Count(w => w.NextReview.Date <= today);

            var totalAnswers = all.Sum(w => w.CorrectAnswers + w.WrongAnswers);
            var correct      = all.Sum(w => w.CorrectAnswers);
            double accuracy  = totalAnswers == 0 ? 0
                : Math.Round((double)correct / totalAnswers * 100, 1);

            // Streak: consecutive days with at least 1 review from ReviewHistory
            var reviewDates = await ctx.ReviewHistory
                .Where(r => r.CardsReviewed > 0)
                .Select(r => r.Date)
                .OrderByDescending(d => d)
                .ToListAsync();

            int streak = 0;
            var check = reviewDates.Contains(today) ? today : today.AddDays(-1);
            foreach (var d in reviewDates)
            {
                if (d == check) { streak++; check = check.AddDays(-1); }
                else if (d < check) break;
            }

            return new GlobalStats
            {
                TotalWords      = total,
                LearnedWords    = learned,
                LearningWords   = learning,
                NewWords        = newWords,
                DueToday        = due,
                OverallAccuracy = accuracy,
                TotalReviews    = totalAnswers,
                Streak          = streak,
            };
        }

        public async Task<List<DailyProgress>> GetDailyProgressAsync(int days = 30)
        {
            await using var ctx = Ctx();
            var cutoff = DateTime.UtcNow.Date.AddDays(-days);
            var history = await ctx.ReviewHistory
                .Where(r => r.Date >= cutoff)
                .OrderBy(r => r.Date)
                .ToListAsync();

            return history.Select(r => new DailyProgress
            {
                Date          = r.Date,
                CardsReviewed = r.CardsReviewed,
                Accuracy      = r.CardsReviewed == 0 ? 0
                    : Math.Round((double)r.CorrectAnswers / r.CardsReviewed * 100, 1)
            }).ToList();
        }

        public async Task RecordReviewAsync(bool correct)
        {
            await using var ctx = Ctx();
            var today = DateTime.UtcNow.Date;
            var row = await ctx.ReviewHistory.FindAsync(today);
            if (row == null)
            {
                row = new ReviewHistory { Date = today };
                ctx.ReviewHistory.Add(row);
            }
            row.CardsReviewed++;
            if (correct) row.CorrectAnswers++;
            await ctx.SaveChangesAsync();
        }
    }
}
