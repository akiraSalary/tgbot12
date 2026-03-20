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
    public class AddListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _toDoListService;


        public AddListScenario(IUserService userService, IToDoListService toDoListService)
        {
            _userService = userService;
            _toDoListService = toDoListService;
        }


        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddList;

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

            // Получаем пользователя из контекста или из сервиса
            if (!context.Data.TryGetValue("User", out var userObj) || userObj is not ToDoUser user)
            {
                // Если пользователя нет в контексте — пытаемся получить
                user = await _userService.GetUserAsync(message.From?.Id ?? context.UserId ?? 0, ct);

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
                    context.CurrentStep = "Name";
                    await bot.SendMessage(chatId, "Введите название нового списка (макс. 10 символов):", cancellationToken: ct);
                    return ScenarioResult.Transition;

                case "Name":
                    if (string.IsNullOrWhiteSpace(text) || text.Length > 10)
                    {
                        await bot.SendMessage(chatId, "Название должно быть от 1 до 10 символов. Попробуйте снова:", cancellationToken: ct);
                        return ScenarioResult.Processed;
                    }

                    var newList = await _toDoListService.AddAsync(user, text, ct);

                    await bot.SendMessage(chatId,
                        $"Список \"{text}\" успешно создан! (ID: {newList.Id})",
                        cancellationToken: ct);

                    return ScenarioResult.Completed;

                default:
                    await bot.SendMessage(chatId, "Неизвестный шаг. Сценарий завершён.", cancellationToken: ct);
                    return ScenarioResult.Completed;
            }
        }
    }
}