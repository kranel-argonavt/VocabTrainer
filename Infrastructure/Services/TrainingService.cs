using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Infrastructure.Services
{
    public class TrainingService : ITrainingService
    {
        private readonly IWordCardRepository _repository;
        private readonly ISpacedRepetitionService _srs;
        private readonly Random _rng = new();
        // Settings injected per-call to avoid stale state
        private AppSettings _settings = new();

        public TrainingService(IWordCardRepository repository, ISpacedRepetitionService srs)
        {
            _repository = repository;
            _srs = srs;
        }

        public void UpdateSettings(AppSettings settings) => _settings = settings;

        public async Task<List<WordCard>> GetSessionWordsAsync(int count) =>
            await _repository.GetSessionWordsAsync(count, _settings.QuestionLanguage, _settings.AnswerLanguage);

        public async Task<bool> ProcessAnswerAsync(WordCard card, bool isCorrect)
        {
            _srs.ApplyResult(card, isCorrect);
            await _repository.UpdateAsync(card);
            await _repository.RecordReviewAsync(isCorrect);
            return isCorrect;
        }

        public async Task<List<WordCard>> GetMultipleChoiceOptionsAsync(WordCard correct, int totalOptions = 4)
        {
            var all = await _repository.GetAllAsync();
            var wrong = all
                .Where(w => w.Id != correct.Id)
                .OrderBy(_ => _rng.Next())
                .Take(totalOptions - 1)
                .ToList();
            return wrong.Prepend(correct).OrderBy(_ => _rng.Next()).ToList();
        }

        public bool ValidateTextInput(string userInput, string expectedAnswer, double tolerance)
        {
            userInput = (userInput ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(userInput)) return false;

            // Split answer into variants by "/" and check each
            var variants = expectedAnswer
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var variant in variants)
            {
                if (string.Equals(userInput, variant, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (tolerance > 0)
                {
                    var dist = Levenshtein(userInput.ToLower(), variant.ToLower());
                    if (dist <= (int)Math.Round(variant.Length * tolerance))
                        return true;
                }
            }
            return false;
        }

        private static int Levenshtein(string a, string b)
        {
            int m = a.Length, n = b.Length;
            var dp = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++) dp[i, 0] = i;
            for (int j = 0; j <= n; j++) dp[0, j] = j;
            for (int i = 1; i <= m; i++)
                for (int j = 1; j <= n; j++)
                    dp[i, j] = a[i - 1] == b[j - 1]
                        ? dp[i - 1, j - 1]
                        : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
            return dp[m, n];
        }
    }
}
