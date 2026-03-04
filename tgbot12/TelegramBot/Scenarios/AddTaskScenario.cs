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
    public class AddTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;

        public AddTaskScenario(IUserService userService, IToDoService toDoService)
        {
            _userService = userService;
            _toDoService = toDoService;
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
                case null: // Начало сценария — просим название
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
                        // Безопасно извлекаем данные из контекста
                        if (!context.Data.TryGetValue("User", out var userObj) || userObj is not ToDoUser user)
                        {
                            await bot.SendMessage(chatId, "Ошибка: пользователь не найден в контексте сценария.", cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        if (!context.Data.TryGetValue("Name", out var nameObj) || nameObj is not string name || string.IsNullOrWhiteSpace(name))
                        {
                            await bot.SendMessage(chatId, "Ошибка: название задачи не найдено в контексте.", cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        var task = await _toDoService.AddTaskAsync(user, name, ct);
                        
                         task.SetDeadline(deadline); 
                         await _toDoService.UpdateTaskAsync(task, ct);

                        await bot.SendMessage(chatId,
                            $"Задача \"{name}\" добавлена с дедлайном {deadline:dd.MM.yyyy}! (ID: {task.Id})",
                            cancellationToken: ct);

                        return ScenarioResult.Completed;
                    }
                    else
                    {
                        await bot.SendMessage(chatId,
                            "Неверный формат даты. Используйте dd.MM.yyyy. Попробуйте снова:",
                            cancellationToken: ct);
                        return ScenarioResult.Processed;
                    }

                default:
                    await bot.SendMessage(chatId, "Неизвестный шаг сценария. Сценарий завершён.", cancellationToken: ct);
                    return ScenarioResult.Completed;
            }
        }
    }
}