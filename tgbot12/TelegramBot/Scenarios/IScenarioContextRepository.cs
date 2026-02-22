using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListBot.TelegramBot.Scenarios;

public interface IScenarioContextRepository
{
    Task<ScenarioContext?> GetContext(long userId, CancellationToken ct = default);
    Task SetContext(long userId, ScenarioContext context, CancellationToken ct = default);
    Task ResetContext(long userId, CancellationToken ct = default);
}
