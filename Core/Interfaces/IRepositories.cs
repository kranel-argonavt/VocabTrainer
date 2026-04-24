using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;

namespace VocabTrainer.Core.Interfaces
{
    public interface IWordCardRepository
    {
        Task<List<WordCard>> GetAllAsync();
        Task<WordCard?> GetByIdAsync(int id);
        Task<List<WordCard>> GetDueTodayAsync();
        Task<List<WordCard>> GetByTagAsync(string tag);
        Task<List<string>> GetAllTagsAsync();
        Task<int> GetFilteredCountAsync(IReadOnlyList<string> tags);
        Task<List<WordCard>> SearchAsync(string query);
        Task<List<WordCard>> GetSessionWordsAsync(int count, Language questionLang, Language answerLang, IReadOnlyList<string>? tags = null);
        Task<bool> ExistsAsync(string german);
        Task AddAsync(WordCard card);
        Task AddRangeAsync(IEnumerable<WordCard> cards);
        Task UpdateAsync(WordCard card);
        Task DeleteAsync(int id);
        Task<GlobalStats> GetGlobalStatsAsync();
        Task<List<DailyProgress>> GetDailyProgressAsync(int days = 30);
        Task RecordReviewAsync(bool correct);
    }

    public interface ISettingsRepository
    {
        Task<AppSettings> LoadAsync();
        Task SaveAsync(AppSettings settings);
    }

    public class DailyProgress
    {
        public DateTime Date { get; set; }
        public int CardsReviewed { get; set; }
        public double Accuracy { get; set; }
        public bool IsFirstOfMonth => Date.Day == 1;
    }
}
