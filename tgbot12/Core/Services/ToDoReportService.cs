
using System;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAccess;

namespace ToDoListBot.Core.Services
{
    public class ToDoReportService : IToDoReportService
    {
        private readonly IToDoRepository _repository;

        public ToDoReportService(IToDoRepository repository)
        {
            _repository = repository;
        }

        public async Task<(int total, int completed, int active, DateTime generatedAt)> GetUserStatsAsync(Guid userId, CancellationToken ct = default)
        {
            var all = await _repository.GetAllByUserIdAsync(userId, ct);
            int total = all.Count;
            int active = await _repository.CountActiveAsync(userId, ct);
            int completed = total - active;
            return (total, completed, active, DateTime.UtcNow);
        }
    }
}