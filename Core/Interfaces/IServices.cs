using System.Collections.Generic;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;

namespace VocabTrainer.Core.Interfaces
{
    public interface ITrainingService
    {
        Task<List<WordCard>> GetSessionWordsAsync(int count, IReadOnlyList<string>? tags = null);
        Task<List<string>> GetAllTagsAsync();
        Task<int> GetFilteredCountAsync(IReadOnlyList<string> tags);
        Task<bool> ProcessAnswerAsync(WordCard card, bool isCorrect);
        Task<List<WordCard>> GetMultipleChoiceOptionsAsync(WordCard correct, int totalOptions = 4);
        bool ValidateTextInput(string userInput, string expectedAnswer, double tolerance);
    }

    public interface ISpacedRepetitionService
    {
        void ApplyResult(WordCard card, bool correct, int quality = -1);
    }

    public interface IImportService
    {
        Task<ImportResult> PreviewExcelAsync(string filePath, ColumnMapping mapping);
        Task<ImportResult> ImportExcelAsync(string filePath, ColumnMapping mapping);
        Task<ImportResult> PreviewCsvAsync(string filePath, char separator, bool hasHeader);
        Task<ImportResult> ImportCsvAsync(string filePath, char separator, bool hasHeader);
    }

    public interface IStatisticsService
    {
        Task<GlobalStats> GetGlobalStatsAsync();
        Task<List<DailyProgress>> GetDailyProgressAsync(int days = 30);
    }
}
