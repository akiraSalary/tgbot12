using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ToDoListBot.TelegramBot.Scenarios;

public interface IScenario
{
    bool CanHandle(ScenarioType scenario);
    Task<ScenarioResult> HandleMessageAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Message message,
        CancellationToken ct);
}