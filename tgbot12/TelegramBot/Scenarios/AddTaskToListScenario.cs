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
            Message? message,
            CancellationToken ct)
        {
            long chatId = context.UserId ?? (message?.Chat.Id ?? 0);
            string? text = message?.Text?.Trim();

            
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

            
            if (!context.Data.TryGetValue("ListId", out var listIdObj) || listIdObj is not Guid listId)
            {
                await bot.SendMessage(chatId, "Ошибка: список не найден.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            switch (context.CurrentStep)
            {
                case null:
                    context.CurrentStep = "Name";
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
                        var name = context.Data["Name"] as string ?? "Без названия";

                        var task = await _toDoService.AddTaskAsync(user, name, listId, ct);
                        task.SetDeadline(deadline);                    
                        await _toDoService.UpdateTaskAsync(task, ct);

                        await bot.SendMessage(chatId,
                            $"Задача \"{name}\" успешно добавлена в список!\n" +
                            $"Дедлайн: {deadline:dd.MM.yyyy}\n" +
                            $"ID: {task.Id}",
                            cancellationToken: ct);

                        return ScenarioResult.Completed;
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