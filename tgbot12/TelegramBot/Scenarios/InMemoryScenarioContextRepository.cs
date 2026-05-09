using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ToDoListBot.TelegramBot
{
    public class InMemoryScenarioContextRepository : IScenarioContextRepository
    {
        private readonly ConcurrentDictionary<long, ScenarioContext> _store = new();

        public Task<ScenarioContext?> GetContext(long telegramUserId, CancellationToken ct)
            => Task.FromResult(_store.TryGetValue(telegramUserId, out var ctx) ? ctx : null);

        public Task SetContext(long telegramUserId, ScenarioContext context, CancellationToken ct)
        {
            // Устанавливаем UserId в контексте, чтобы фоновые задачи могли знать, кому сбрасывать сценарий
            context.UserId = telegramUserId;
            _store[telegramUserId] = context;
            return Task.CompletedTask;
        }

        public Task ResetContext(long telegramUserId, CancellationToken ct)
        {
            _store.TryRemove(telegramUserId, out _);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ScenarioContext>> GetContexts(CancellationToken ct)
        {
            var list = _store.Values.ToList().AsReadOnly();
            return Task.FromResult((IReadOnlyList<ScenarioContext>)list);
        }
    }
}