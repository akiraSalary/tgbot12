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
    Message? message,
    CancellationToken ct)
        {
            long chatId = context.UserId ?? (message?.Chat.Id ?? 0);
            string? text = message?.Text?.Trim();

            // Надёжно получаем пользователя
            if (!context.Data.TryGetValue("User", out var userObj) || userObj is not ToDoUser user)
            {
                user = await _userService.GetUserAsync(context.UserId ?? message?.From?.Id ?? 0, ct);
                if (user == null)
                {
                    if (chatId != 0)
                        await bot.SendMessage(chatId, "Ошибка: пользователь не найден. Начните с /start", cancellationToken: ct);
                    return ScenarioResult.Completed;
                }
                context.Data["User"] = user;
            }

            switch (context.CurrentStep)
            {
                case null: // Начало сценария
                    context.CurrentStep = "Name";
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
                        if (!context.Data.TryGetValue("Name", out var nameObj) || nameObj is not string name || string.IsNullOrWhiteSpace(name))
                        {
                            await bot.SendMessage(chatId, "Ошибка: название задачи не найдено.", cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        // Сохраняем данные для callback
                        UpdateHandler._pendingTasks[user.TelegramUserId] = new UpdateHandler.TaskCreationData
                        {
                            Name = name,
                            Deadline = deadline
                        };

                        // Показываем выбор списка
                        var lists = await _toDoListService.GetUserListsAsync(user.UserId, ct);

                        var sb = new StringBuilder("Выберите список:\n\n");

                        var keyboardRows = new List<List<InlineKeyboardButton>>
                {
                    new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("⭐ Без списка", "addtask|none")
                    }
                };

                        foreach (var list in lists)
                        {
                            keyboardRows.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(list.Name, $"addtask|{list.Id}")
                    });
                        }

                        var keyboard = new InlineKeyboardMarkup(keyboardRows);

                        await bot.SendMessage(chatId, sb.ToString(), replyMarkup: keyboard, cancellationToken: ct);

                        return ScenarioResult.Completed;   // ← Завершаем сценарий
                    }
                    else
                    {
                        await bot.SendMessage(chatId, "Неверный формат. Используйте dd.MM.yyyy. Попробуйте снова:", cancellationToken: ct);
                        return ScenarioResult.Processed;
                    }

                default:
                    await bot.SendMessage(chatId, "Неизвестный шаг сценария.", cancellationToken: ct);
                    return ScenarioResult.Completed;
            }
        }
    }
    
}
    
    

