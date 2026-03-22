using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.TelegramBot.Scenarios;
////<summary>
//namespace ToDoListBot.TelegramBot.Scenarios
//{
//    public class CompleteTaskScenario : IScenario
//    {
//        private readonly IToDoService _toDoService;

//        public CompleteTaskScenario(IToDoService toDoService)
//        {
//            _toDoService = toDoService;
//        }

//        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.CompleteTask;

//        public async Task<ScenarioResult> HandleMessageAsync(
//            ITelegramBotClient bot,
//            ScenarioContext context,
//            Message? message,
//            CancellationToken ct)
//        {
//            long chatId = context.UserId ?? (message?.Chat.Id ?? 0);

//            if (!context.Data.TryGetValue("ToDoItemId", out var idObj) || idObj is not Guid taskId)
//            {
//                await bot.SendMessage(chatId, "Ошибка: задача не найдена.", cancellationToken: ct);
//                return ScenarioResult.Completed;
//            }

//            switch (context.CurrentStep)
//            {
//                case null:
//                    context.CurrentStep = "Confirm";
//                    await bot.SendMessage(chatId,
//                        "Отметить задачу как выполненную?\n\n" +
//                        "✅ Да — выполнить\n" +
//                        "❌ Нет — отменить",
//                        replyMarkup: new InlineKeyboardMarkup(new[]
//                        {
//                            new[]
//                            {
//                                InlineKeyboardButton.WithCallbackData("✅ Да", $"complete_yes|{taskId}"),
//                                InlineKeyboardButton.WithCallbackData("❌ Нет", "complete_no")
//                            }
//                        }),
//                        cancellationToken: ct);
//                    return ScenarioResult.Transition;

//                default:
//                    await bot.SendMessage(chatId, "Неизвестный шаг.", cancellationToken: ct);
//                    return ScenarioResult.Completed;
//            }
//        }
//    }
//}
//</summary>