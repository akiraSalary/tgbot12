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
    public class AddTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoListService _toDoListService;

        public AddTaskScenario(IUserService userService, IToDoService toDoService, IToDoListService toDoListService)
        {
            _userService = userService;
            _toDoService = toDoService;
            _toDoListService = toDoListService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddTask;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var chatId = message.Chat.Id;
            var text = message.Text?.Trim();

            if (string.IsNullOrEmpty(text))
                return ScenarioResult.Processed;

            switch (context.CurrentStep)
            {
                case null:
                    context.CurrentStep = "Name";
                    context.Data["User"] = await _userService.GetUserAsync(message.From.Id, ct);
                    await bot.SendMessage(chatId, "Введите название задачи:", cancellationToken: ct);
                    return ScenarioResult.Transition;

                case "Name":
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        await bot.SendMessage(chatId, "Название не может быть пустым. Попробуйте снова:", cancellationToken: ct);
                        return ScenarioResult.Processed;
                    }

                    context.Data["Name"] = text;
                    context.CurrentStep = "Deadline";
                    await bot.SendMessage(chatId, "Введите срок выполнения (формат: dd.MM.yyyy):", cancellationToken: ct);
                    return ScenarioResult.Transition;

                case "Deadline":
                    if (DateTime.TryParseExact(text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime deadline))
                    {
                        if (!context.Data.TryGetValue("User", out var userObj) || userObj is not ToDoUser user)
                        {
                            await bot.SendMessage(chatId, "Ошибка: пользователь не найден.", cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        if (!context.Data.TryGetValue("Name", out var nameObj) || nameObj is not string name || string.IsNullOrWhiteSpace(name))
                        {
                            await bot.SendMessage(chatId, "Ошибка: название задачи не найдено.", cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        context.Data["Deadline"] = deadline;

                        // Показываем выбор списка (как на втором скрине)
                        var lists = await _toDoListService.GetUserListsAsync(user.UserId, ct);

                        var sb = new StringBuilder("Выберите список:\n\n");
                        var buttons = new List<List<InlineKeyboardButton>>();

                        // Кнопка "Без списка"
                        buttons.Add(new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData("Без списка", "addtask|none")
                        });

                        // Списки
                        foreach (var list in lists)
                        {
                            buttons.Add(new List<InlineKeyboardButton>
                            {
                                InlineKeyboardButton.WithCallbackData(list.Name, $"addtask|{list.Id}")
                            });
                        }

                        var keyboard = new InlineKeyboardMarkup(buttons);

                        await bot.SendMessage(chatId, sb.ToString(), replyMarkup: keyboard, cancellationToken: ct);

                        return ScenarioResult.Completed; // сценарий завершается, дальше callback
                    }
                    else
                    {
                        await bot.SendMessage(chatId, "Неверный формат. Используйте dd.MM.yyyy. Попробуйте снова:", cancellationToken: ct);
                        return ScenarioResult.Processed;
                    }

                default:
                    await bot.SendMessage(chatId, "Неизвестный шаг. Сценарий завершён.", cancellationToken: ct);
                    return ScenarioResult.Completed;
            }
        }
    }
}