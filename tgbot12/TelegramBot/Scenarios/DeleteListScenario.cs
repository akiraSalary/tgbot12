using System;
using System.Collections.Generic;
using System.Text;
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
    public class DeleteListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _toDoListService;

        public DeleteListScenario(IUserService userService, IToDoListService toDoListService)
        {
            _userService = userService;
            _toDoListService = toDoListService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteList;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message? message,
            CancellationToken ct)
        {
            long chatId = context.UserId ?? (message?.Chat.Id ?? 0);

            // Получаем пользователя
            if (!context.Data.TryGetValue("User", out var userObj) || userObj is not ToDoUser user)
            {
                user = await _userService.GetUserAsync(context.UserId ?? 0, ct);
                if (user == null)
                {
                    await bot.SendMessage(chatId, "Ошибка: пользователь не найден. Начните с /start", cancellationToken: ct);
                    return ScenarioResult.Completed;
                }
                context.Data["User"] = user;
            }

            switch (context.CurrentStep)
            {
                case null:
                    context.CurrentStep = "ChooseList";

                    var lists = await _toDoListService.GetUserListsAsync(user.UserId, ct);

                    var sb = new StringBuilder("Введите список для удаления:\n\n");

                    var keyboardRows = new List<List<InlineKeyboardButton>>();

                    foreach (var list in lists)
                    {
                        keyboardRows.Add(new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(list.Name, $"delete_confirm|{list.Id}")
                        });
                    }

                    var keyboard = new InlineKeyboardMarkup(keyboardRows);

                    await bot.SendMessage(chatId, sb.ToString(), replyMarkup: keyboard, cancellationToken: ct);
                    return ScenarioResult.Transition;

                default:
                    await bot.SendMessage(chatId, "Неизвестный шаг.", cancellationToken: ct);
                    return ScenarioResult.Completed;
            }
        }
    }
}