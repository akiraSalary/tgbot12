using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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
        private readonly ITelegramBotClient _botClient;

        public UpdateHandler(
            IUserService userService,
            IToDoService toDoService,
            IToDoReportService reportService,
            int maxTaskCount,
            int maxTaskLength,
            ITelegramBotClient botClient)
        {
            _userService = userService;
            _toDoService = toDoService;
            _reportService = reportService;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
            _botClient = botClient;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Message is not { } message) return;
            if (message.Text is not { } text) return;

            var chat = message.Chat;
            var from = message.From ?? throw new InvalidOperationException("No From user");
            long tgId = from.Id;
            string username = from.Username ?? "Unknown";

            var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string cmd = parts[0].ToLowerInvariant().TrimStart('/');

            // ne static
            ToDoUser? currentUser = await _userService.GetUserAsync(tgId, ct);

            if (currentUser == null)
            {
                if (cmd == "start")
                {
                    currentUser = await _userService.RegisterUserAsync(tgId, username, ct);
                    await SendWithKeyboardAsync(chat,
                        $"Привет, @{username}! Ты зарегистрирован.\nИспользуй меню или /help",
                        GetMainKeyboard(), ct);
                    return;
                }

                // рега
                await SendWithKeyboardAsync(chat, "Пожалуйста, нажмите /start для регистрации.", GetStartKeyboard(), ct);
                return;
            }

            // пользователь и обработка

            try
            {
                switch (cmd)
                {
                    case "start":
                    case "help":
                        await SendWithKeyboardAsync(chat, GetHelpText(), GetMainKeyboard(), ct);
                        break;

                    case "info":
                        await SendWithKeyboardAsync(chat, GetInfoText(currentUser), GetMainKeyboard(), ct);
                        break;

                    case "addtask":
                        await HandleAddTaskAsync(chat, currentUser, parts, ct);
                        break;

                    case "showtasks":
                        await ShowActiveTasksAsync(chat, currentUser, ct);
                        break;

                    case "showalltasks":
                        await ShowAllTasksAsync(chat, currentUser, ct);
                        break;

                    case "completetask":
                        await HandleCompleteTaskAsync(chat, parts, ct);
                        break;

                    case "removetask":
                        await HandleRemoveTaskAsync(chat, parts, ct);
                        break;

                    case "report":
                        await HandleReportAsync(chat, currentUser, ct);
                        break;

                    case "find":
                        await HandleFindAsync(chat, currentUser, parts, ct);
                        break;

                    default:
                        await SendWithKeyboardAsync(chat, "Неизвестная команда. Используй меню или /help", GetMainKeyboard(), ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendMessage(chat.Id, $"Ошибка: {ex.Message}", cancellationToken: ct);
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken ct)
        {
            var errorMsg = exception switch
            {
                ApiRequestException api => $"Telegram API Error [{api.ErrorCode}]: {api.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMsg);
            return Task.CompletedTask;
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            return HandleErrorAsync(botClient, exception, (HandleErrorSource)0, ct);
        }

        // keyboards+methods

        private static ReplyKeyboardMarkup GetStartKeyboard() => new(new[]
        {
            new KeyboardButton("/start")
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        private static ReplyKeyboardMarkup GetMainKeyboard() => new(new KeyboardButton[][]
        {
            new[] { new KeyboardButton("/showtasks"), new KeyboardButton("/report") },
            new[] { new KeyboardButton("/showalltasks"), new KeyboardButton("/help") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        private async Task SendWithKeyboardAsync(Chat chat, string text, ReplyKeyboardMarkup? replyMarkup, CancellationToken ct)
        {
            await _botClient.SendMessage(
                chat.Id,
                text,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
        }

         private string GetHelpText() =>
            "Доступные команды:\n\n" +
            "/start — начать\n" +
            "/help — справка\n" +
            "/info — о тебе и лимитах\n" +
            "/addtask <название> — добавить задачу\n" +
            "/showtasks — активные задачи\n" +
            "/showalltasks — все задачи\n" +
            "/completetask <id> — завершить\n" +
            "/removetask <id> — удалить\n" +
            "/report — статистика\n" +
            "/find <префикс> — поиск\n\n" +
            "ID задач в `кавычках` — удобно копировать.";

        private string GetInfoText(ToDoUser user)
        {
            return $"Пользователь: @{user.TelegramUserName}\n" +
                   $"ID: {user.TelegramUserId}\n" +
                   $"Зарегистрирован: {user.RegisteredAt:dd.MM.yyyy HH:mm:ss}\n\n" +
                   $"Лимит задач: {_maxTaskCount}\n" +
                   $"Макс. длина: {_maxTaskLength} символов\n" +
                   $"Дата создания: 17.11.2025\n" +
                   $"Последнее обновление: 17.02.2026\n" +
                   $"Версия: 1.7.1";
        }

        // обработка команд currentUser

        private async Task HandleAddTaskAsync(Chat chat, ToDoUser user, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2)
            {
                await SendWithKeyboardAsync(chat, "Использование: /addtask Название", GetMainKeyboard(), ct);
                return;
            }

            string name = string.Join(" ", parts[1..]);
            var task = await _toDoService.AddTaskAsync(user, name, ct);

            await SendWithKeyboardAsync(chat,
                $"Добавлена: \"{task.Name}\" (`{task.Id}`)",
                GetMainKeyboard(), ct);
        }

        private async Task ShowActiveTasksAsync(Chat chat, ToDoUser user, CancellationToken ct)
        {
            var tasks = await _toDoService.GetActiveByUserIdAsync(user.UserId, ct);
            if (tasks.Count == 0)
            {
                await SendWithKeyboardAsync(chat, "Активных задач нет.", GetMainKeyboard(), ct);
                return;
            }

            var sb = new StringBuilder("Активные задачи:\n\n");
            foreach (var t in tasks)
                sb.AppendLine($"- {t.Name} (`{t.Id}`) • {t.CreatedAt:dd.MM.yyyy HH:mm}");

            await SendWithKeyboardAsync(chat, sb.ToString(), GetMainKeyboard(), ct);
        }

        private async Task ShowAllTasksAsync(Chat chat, ToDoUser user, CancellationToken ct)
        {
            var tasks = await _toDoService.GetAllByUserIdAsync(user.UserId, ct);
            if (tasks.Count == 0)
            {
                await SendWithKeyboardAsync(chat, "Задач нет.", GetMainKeyboard(), ct);
                return;
            }

            var sb = new StringBuilder("Все задачи:\n\n");
            foreach (var t in tasks)
            {
                string state = t.State == ToDoItemState.Active ? "активна" : "завершена";
                sb.AppendLine($"- {t.Name} ({state}) (`{t.Id}`) • {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }

            await SendWithKeyboardAsync(chat, sb.ToString(), GetMainKeyboard(), ct);
        }

        private async Task HandleCompleteTaskAsync(Chat chat, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var id))
            {
                await SendWithKeyboardAsync(chat, "Использование: /completetask <id>", GetMainKeyboard(), ct);
                return;
            }

            await _toDoService.MarkCompletedAsync(id, ct);
            await SendWithKeyboardAsync(chat, $"Задача `{id}` завершена.", GetMainKeyboard(), ct);
        }

        private async Task HandleRemoveTaskAsync(Chat chat, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var id))
            {
                await SendWithKeyboardAsync(chat, "Использование: /removetask <id>", GetMainKeyboard(), ct);
                return;
            }

            await _toDoService.DeleteAsync(id, ct);
            await SendWithKeyboardAsync(chat, $"Задача `{id}` удалена.", GetMainKeyboard(), ct);
        }

        private async Task HandleReportAsync(Chat chat, ToDoUser user, CancellationToken ct)
        {
            var (total, completed, active, at) = await _reportService.GetUserStatsAsync(user.UserId, ct);
            var msg = $"Статистика на {at:dd.MM.yyyy HH:mm:ss}\n\n" +
                      $"Всего: {total}\n" +
                      $"Завершено: {completed}\n" +
                      $"Активно: {active}";

            await SendWithKeyboardAsync(chat, msg, GetMainKeyboard(), ct);
        }

        private async Task HandleFindAsync(Chat chat, ToDoUser user, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2)
            {
                await SendWithKeyboardAsync(chat, "Использование: /find <префикс>", GetMainKeyboard(), ct);
                return;
            }

            string prefix = string.Join(" ", parts[1..]);
            var tasks = await _toDoService.FindAsync(user, prefix, ct);

            if (tasks.Count == 0)
            {
                await SendWithKeyboardAsync(chat, $"Ничего не найдено на «{prefix}»", GetMainKeyboard(), ct);
                return;
            }

            var sb = new StringBuilder($"Найдено {tasks.Count}:\n\n");
            foreach (var t in tasks)
                sb.AppendLine($"- {t.Name} (`{t.Id}`) • {t.CreatedAt:dd.MM.yyyy HH:mm}");

            await SendWithKeyboardAsync(chat, sb.ToString(), GetMainKeyboard(), ct);
        }
    }
}