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
            var now = DateTime.UtcNow.Date;
            return await ctx.WordCards
                .Where(w => w.NextReview <= now.AddDays(1))
                .OrderBy(w => w.NextReview)
                .ToListAsync();
        }

        public async Task<List<WordCard>> GetByTagAsync(string tag)
        {
            await using var ctx = Ctx();
            return await ctx.WordCards.Where(w => w.Tags.Contains(tag)).ToListAsync();
        }

        public async Task<List<WordCard>> SearchAsync(string query)
        {
            await using var ctx = Ctx();
            return await ctx.WordCards
                .Where(w => w.German.Contains(query) || w.English.Contains(query) || w.Ukrainian.Contains(query))
                .ToListAsync();
        }

        public async Task<List<WordCard>> GetSessionWordsAsync(int count, Language questionLang, Language answerLang)
        {
            var due = await GetDueTodayAsync();
            if (due.Count >= count) return due.Take(count).ToList();

            var existingIds = due.Select(w => w.Id).ToHashSet();
            await using var ctx = Ctx();
            var extra = await ctx.WordCards
                .Where(w => !existingIds.Contains(w.Id))
                .OrderBy(w => w.NextReview)
                .Take(count - due.Count)
                .ToListAsync();

            return due.Concat(extra).ToList();
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
            var all = await ctx.WordCards.ToListAsync();
            var total = all.Count;
            var learned = all.Count(w => w.SuccessRate >= 80 && w.ReviewCount >= 5);
            var learning = all.Count(w => w.ReviewCount > 0 && !(w.SuccessRate >= 80 && w.ReviewCount >= 5));
            var newWords = all.Count(w => w.ReviewCount == 0);
            var due = all.Count(w => w.IsDueToday);
            var totalAnswers = all.Sum(w => w.CorrectAnswers + w.WrongAnswers);
            var correct = all.Sum(w => w.CorrectAnswers);
            double accuracy = totalAnswers == 0 ? 0 : Math.Round((double)correct / totalAnswers * 100, 1);

            // Calculate streak: consecutive days with at least 1 review
            var reviewDates = all
                .Where(w => w.LastReviewed.HasValue)
                .Select(w => w.LastReviewed!.Value.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
            int streak = 0;
            var today = DateTime.UtcNow.Date;
            var check = reviewDates.Contains(today) ? today : today.AddDays(-1);
            foreach (var d in reviewDates.OrderByDescending(d => d))
            {
                if (d == check) { streak++; check = check.AddDays(-1); }
                else if (d < check) break;
            }

            return new GlobalStats
            {
                TotalWords    = total,
                LearnedWords  = learned,
                LearningWords = learning,
                NewWords      = newWords,
                DueToday      = due,
                OverallAccuracy = accuracy,
                TotalReviews  = totalAnswers,
                Streak        = streak,
            };
        }

        public async Task<List<DailyProgress>> GetDailyProgressAsync(int days = 30)
        {
            await using var ctx = Ctx();
            var cutoff = DateTime.UtcNow.Date.AddDays(-days);
            var cards = await ctx.WordCards
                .Where(w => w.LastReviewed.HasValue && w.LastReviewed.Value >= cutoff)
                .ToListAsync();

            return cards
                .GroupBy(w => w.LastReviewed!.Value.Date)
                .Select(g => new DailyProgress
                {
                    Date = g.Key,
                    CardsReviewed = g.Count(),
                    Accuracy = g.Sum(w => w.CorrectAnswers + w.WrongAnswers) == 0
                        ? 0
                        : Math.Round((double)g.Sum(w => w.CorrectAnswers) /
                          g.Sum(w => w.CorrectAnswers + w.WrongAnswers) * 100, 1)
                })
                .OrderBy(x => x.Date)
                .ToList();
        }
    }
}
