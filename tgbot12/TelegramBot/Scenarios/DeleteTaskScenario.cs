using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.TelegramBot.Scenarios;

namespace ToDoListBot.TelegramBot.Scenarios
{
    public class DeleteTaskScenario : IScenario
    {
        private readonly IToDoService _toDoService;

        public DeleteTaskScenario(IToDoService toDoService)
        {
            _toDoService = toDoService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteTask;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message? message,
            CancellationToken ct)
        {
            long chatId = context.UserId ?? (message?.Chat.Id ?? 0);

            if (!context.Data.TryGetValue("ToDoItemId", out var idObj) || idObj is not Guid taskId)
            {
                await bot.SendMessage(chatId, "Ошибка: задача не найдена.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            switch (context.CurrentStep)
            {
                case null:
                    context.CurrentStep = "Confirm";
                    await bot.SendMessage(chatId, "Вы точно хотите удалить эту задачу?\n\n✅ Да — удалить\n❌ Нет — отменить",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("✅ Да", $"delete_yes|{taskId}"),
                                InlineKeyboardButton.WithCallbackData("❌ Нет", "delete_no")
                            }
                        }),
                        cancellationToken: ct);
                    return ScenarioResult.Transition;

                default:
                    await bot.SendMessage(chatId, "Неизвестный шаг.", cancellationToken: ct);
                    return ScenarioResult.Completed;
            }
        }
    }
}