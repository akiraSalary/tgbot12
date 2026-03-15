using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ToDoListBot.Core.Services;
using ToDoListBot.TelegramBot.Scenarios;

namespace ToDoListBot.TelegramBot.Scenarios
{
    public class DeleteListScenario : IScenario
    {
        private readonly IToDoListService _toDoListService;

        public DeleteListScenario(IToDoListService toDoListService)
        {
            _toDoListService = toDoListService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteList;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var chatId = message.Chat.Id;
            var text = message.Text?.Trim().ToLower();

            if (context.CurrentStep == null)
            {
                context.CurrentStep = "Approve";
                var listId = (Guid)context.Data["ListId"];
                var list = await _toDoListService.GetAsync(listId, ct);

                await bot.SendMessage(chatId,
                    $"Подтверждаете удаление списка **{list?.Name}** и всех его задач?\n\n" +
                    "✅ Да — удалить\n❌ Нет — отменить",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Да", "yes"),
                            InlineKeyboardButton.WithCallbackData("❌ Нет", "no")
                        }
                    }),
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);

                return ScenarioResult.Transition;
            }

            if (text == "yes" || context.CurrentStep == "Delete")
            {
                var listId = (Guid)context.Data["ListId"];
                await _toDoListService.DeleteAsync(listId, ct);

                await bot.SendMessage(chatId, "Список и все его задачи успешно удалены.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }
            else
            {
                await bot.SendMessage(chatId, "Удаление отменено.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }
        }
    }
}