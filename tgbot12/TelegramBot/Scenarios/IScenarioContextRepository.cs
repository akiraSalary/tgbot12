
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ToDoListBot.TelegramBot
{
    public interface IScenarioContextRepository
    {
        Task<ScenarioContext?> GetContext(long telegramUserId, CancellationToken ct);
        Task SetContext(long telegramUserId, ScenarioContext context, CancellationToken ct);
        Task ResetContext(long telegramUserId, CancellationToken ct);

        // Новый метод, возвращающий все контексты
        Task<IReadOnlyList<ScenarioContext>> GetContexts(CancellationToken ct);
    }
}