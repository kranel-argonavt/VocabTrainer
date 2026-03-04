using System.Collections.Generic;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Infrastructure.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IWordCardRepository _repository;

        public StatisticsService(IWordCardRepository repository) => _repository = repository;

        public Task<GlobalStats> GetGlobalStatsAsync() => _repository.GetGlobalStatsAsync();

        public Task<List<DailyProgress>> GetDailyProgressAsync(int days = 30) =>
            _repository.GetDailyProgressAsync(days);
    }
}
