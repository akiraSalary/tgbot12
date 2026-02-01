using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.Core.Exceptions;

namespace ToDoListBot.TelegramBot
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoReportService _reportService;
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        private static ToDoUser? CurrentUser; 

        public UpdateHandler(
            IUserService userService,
            IToDoService toDoService,
            IToDoReportService reportService,
            int maxTaskCount,
            int maxTaskLength)
        {
            _userService = userService;
            _toDoService = toDoService;
            _reportService = reportService;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Message?.Text is not { } text)
                return;

            var message = update.Message;
            var chat = message.Chat;
            var from = message.From ?? throw new InvalidOperationException("No From user");

            long tgId = from.Id;
            string username = from.Username ?? "Unknown";

            CurrentUser = await _userService.GetUserAsync(tgId, ct);
            if (CurrentUser == null)
            {
                CurrentUser = await _userService.RegisterUserAsync(tgId, username, ct);
                await botClient.SendMessage(chat, $"Привет, @{username}! Ты зарегистрирован.\nИспользуй /help для списка команд.", ct);
                return;
            }

            var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string cmd = parts[0].ToLowerInvariant();

            try
            {
                switch (cmd)
                {
                    case "/start":
                    case "/help":
                        await SendHelpAsync(botClient, chat, ct);
                        break;

                    case "/info":
                        await SendInfoAsync(botClient, chat, ct);
                        break;

                    case "/addtask":
                        await HandleAddTaskAsync(botClient, chat, parts, ct);
                        break;

                    case "/showtasks":
                        await ShowActiveTasksAsync(botClient, chat, ct);
                        break;

                    case "/showalltasks":
                        await ShowAllTasksAsync(botClient, chat, ct);
                        break;

                    case "/completetask":
                        await HandleCompleteTaskAsync(botClient, chat, parts, ct);
                        break;

                    case "/removetask":
                        await HandleRemoveTaskAsync(botClient, chat, parts, ct);
                        break;

                    case "/report":
                        await HandleReportAsync(botClient, chat, ct);
                        break;

                    case "/find":
                        await HandleFindAsync(botClient, chat, parts, ct);
                        break;

                    default:
                        await botClient.SendMessage(chat, "Неизвестная команда. Используй /help", ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chat, $"Ошибка: {ex.Message}", ct);
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

     // async priv methods

        private async Task SendHelpAsync(ITelegramBotClient bot, Chat chat, CancellationToken ct)
        {
            var sb = new StringBuilder()
                .AppendLine("Доступные команды:")
                .AppendLine("/start, /help — эта справка")
                .AppendLine("/info — информация о тебе и лимитах")
                .AppendLine("/addtask <название> — добавить задачу")
                .AppendLine("/showtasks — показать активные задачи")
                .AppendLine("/showalltasks — показать все задачи")
                .AppendLine("/completetask <id> — завершить задачу")
                .AppendLine("/removetask <id> — удалить задачу")
                .AppendLine("/report — стата по задачам")
                .AppendLine("/find <имя_задачи> — поиск активной задачи по названию");

            await bot.SendMessage(chat, sb.ToString(), ct);
        }

        private async Task SendInfoAsync(ITelegramBotClient bot, Chat chat, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var msg = $"Пользователь: @{CurrentUser.TelegramUserName}\n" +
                      $"Tg ID: {CurrentUser.TelegramUserId}\n" +
                      $"Зареган: {CurrentUser.RegisteredAt:dd.MM.yyyy HH:mm:ss}\n" +
                      $"Лимит задач: {_maxTaskCount}\n" +
                      $"Лимит символов: {_maxTaskLength}\n" +
                      $"\nДата создания: 17.11.2025\n" +
                      $"Версия: 1.5.0\n" +
                      $"Обновлена до актуальной версии: 01.02.2025\n";

            await bot.SendMessage(chat, msg, ct);
        }

        private async Task HandleAddTaskAsync(ITelegramBotClient bot, Chat chat, string[] parts, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            if (parts.Length < 2)
            {
                await bot.SendMessage(chat, "Использование: /addtask Название задачи", ct);
                return;
            }

            string name = string.Join(" ", parts, 1, parts.Length - 1);
            var task = await _toDoService.AddTaskAsync(CurrentUser, name, ct);

            await bot.SendMessage(chat, $"Добавлена задача: \"{task.Name}\" (ID: {task.Id})", ct);
        }

        private async Task ShowActiveTasksAsync(ITelegramBotClient bot, Chat chat, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var tasks = await _toDoService.GetActiveByUserIdAsync(CurrentUser.UserId, ct);

            if (!tasks.Any())
            {
                await bot.SendMessage(chat, "Активных задач пока нет.", ct);
                return;
            }

            var sb = new StringBuilder("Активные задачи:\n");
            foreach (var t in tasks)
            {
                sb.AppendLine($"- {t.Name} (ID: {t.Id}) • {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }

            await bot.SendMessage(chat, sb.ToString(), ct);
        }

        private async Task ShowAllTasksAsync(ITelegramBotClient bot, Chat chat, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var tasks = await _toDoService.GetAllByUserIdAsync(CurrentUser.UserId, ct);

            if (!tasks.Any())
            {
                await bot.SendMessage(chat, "Задач пока нет.", ct);
                return;
            }

            var sb = new StringBuilder("Все задачи:\n");
            foreach (var t in tasks)
            {
                string state = t.State == ToDoItemState.Active ? "активна" : "завершена";
                sb.AppendLine($"- {t.Name} ({state}) (ID: {t.Id}) • {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }

            await bot.SendMessage(chat, sb.ToString(), ct);
        }

        private async Task HandleCompleteTaskAsync(ITelegramBotClient bot, Chat chat, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var id))
            {
                await bot.SendMessage(chat, "Использование: /completetask <id>", ct);
                return;
            }

            await _toDoService.MarkCompletedAsync(id, ct);
            await bot.SendMessage(chat, $"Задача {id} помечена как завершённая.", ct);
        }

        private async Task HandleRemoveTaskAsync(ITelegramBotClient bot, Chat chat, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var id))
            {
                await bot.SendMessage(chat, "Использование: /removetask <id>", ct);
                return;
            }

            await _toDoService.DeleteAsync(id, ct);
            await bot.SendMessage(chat, $"Задача {id} удалена.", ct);
        }

        private async Task HandleReportAsync(ITelegramBotClient bot, Chat chat, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var (total, completed, active, generatedAt) = await _reportService.GetUserStatsAsync(CurrentUser.UserId, ct);

            var msg = $"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}\n" +
                      $"Всего: {total}\n" +
                      $"Завершённых: {completed}\n" +
                      $"Активных: {active}";

            await bot.SendMessage(chat, msg, ct);
        }

        private async Task HandleFindAsync(ITelegramBotClient bot, Chat chat, string[] parts, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            if (parts.Length < 2)
            {
                await bot.SendMessage(chat, "Использование: /find Префикс", ct);
                return;
            }

            string prefix = string.Join(" ", parts, 1, parts.Length - 1);
            var tasks = await _toDoService.FindAsync(CurrentUser, prefix, ct);

            if (!tasks.Any())
            {
                await bot.SendMessage(chat, $"Активных задач, начинающихся на «{prefix}», не найдено.", ct);
                return;
            }

            var sb = new StringBuilder($"Найдено {tasks.Count} активных задач:\n");
            foreach (var t in tasks)
            {
                sb.AppendLine($"- {t.Name} (ID: {t.Id}) • {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }

            await bot.SendMessage(chat, sb.ToString(), ct);
        }
    }
}