using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.TelegramBot.Scenarios;

namespace ToDoListBot.TelegramBot.Scenarios
{
    public class AddTaskToListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;

        public AddTaskToListScenario(IUserService userService, IToDoService toDoService)
        {
            _userService = userService;
            _toDoService = toDoService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddTaskToList;

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
                    // ListId уже должен быть в Data (передаётся при запуске сценария)
                    await bot.SendMessage(chatId, "Введите название задачи для этого списка:", cancellationToken: ct);
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

                        if (!context.Data.TryGetValue("ListId", out var listIdObj) || listIdObj is not Guid listId)
                        {
                            await bot.SendMessage(chatId, "Ошибка: ID списка не найден.", cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        var task = await _toDoService.AddTaskAsync(user, name, listId, ct);  // ← передаём listId
                        task.SetDeadline(deadline);
                        await _toDoService.UpdateTaskAsync(task, ct);

                        await bot.SendMessage(chatId,
                            $"Задача \"{name}\" добавлена в список с дедлайном {deadline:dd.MM.yyyy}! (ID: {task.Id})",
                            cancellationToken: ct);

                        return ScenarioResult.Completed;
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