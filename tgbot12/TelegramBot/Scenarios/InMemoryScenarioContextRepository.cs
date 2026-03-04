using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ToDoListBot.TelegramBot.Scenarios
{
    public class InMemoryScenarioContextRepository : IScenarioContextRepository
    {
        private readonly ConcurrentDictionary<long, ScenarioContext> _contexts = new();

        public Task<ScenarioContext?> GetContext(long userId, CancellationToken ct = default)
        {
            _contexts.TryGetValue(userId, out var context);
            return Task.FromResult(context);
        }

        public Task SetContext(long userId, ScenarioContext context, CancellationToken ct = default)
        {
            _contexts[userId] = context;
            return Task.CompletedTask;
        }

        public Task ResetContext(long userId, CancellationToken ct = default)
        {
            _contexts.TryRemove(userId, out _);
            return Task.CompletedTask;
        }
    }
}