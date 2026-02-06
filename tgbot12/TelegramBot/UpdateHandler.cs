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

        private static ToDoUser? CurrentUser; // временно

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
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } text)
                return;

            var chat = message.Chat;
            var from = message.From ?? throw new InvalidOperationException("No From user");

            long tgId = from.Id;
            string username = from.Username ?? "Unknown";

            CurrentUser = await _userService.GetUserAsync(tgId, ct);

            if (CurrentUser == null)
            {
                CurrentUser = await _userService.RegisterUserAsync(tgId, username, ct);
                await SendWithKeyboardAsync(chat,
                    $"Привет, @{username}! Ты зарегистрирован.\nИспользуй меню или /help",
                    GetStartKeyboard(), ct);
                return;
            }

            var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string cmd = parts[0].ToLowerInvariant().TrimStart('/');

            try
            {
                switch (cmd)
                {
                    case "start":
                    case "help":
                        await SendWithKeyboardAsync(chat, GetHelpText(), GetMainKeyboard(), ct);
                        break;

                    case "info":
                        await SendWithKeyboardAsync(chat, GetInfoText(), GetMainKeyboard(), ct);
                        break;

                    case "addtask":
                        await HandleAddTaskAsync(chat, parts, ct);
                        break;

                    case "showtasks":
                        await ShowActiveTasksAsync(chat, ct);
                        break;

                    case "showalltasks":
                        await ShowAllTasksAsync(chat, ct);
                        break;

                    case "completetask":
                        await HandleCompleteTaskAsync(chat, parts, ct);
                        break;

                    case "removetask":
                        await HandleRemoveTaskAsync(chat, parts, ct);
                        break;

                    case "report":
                        await HandleReportAsync(chat, ct);
                        break;

                    case "find":
                        await HandleFindAsync(chat, parts, ct);
                        break;

                    default:
                        await SendWithKeyboardAsync(chat,
                            "Неизвестная команда. Используй меню или /help",
                            GetMainKeyboard(), ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(
                    chat.Id,
                    $"Ошибка: {ex.Message}",
                    cancellationToken: ct);
            }
        }

        public Task HandlePollingErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var errorMsg = exception switch
            {
                ApiRequestException api => $"Telegram API Error [{api.ErrorCode}]: {api.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMsg);
            return Task.CompletedTask;
        }

        // methods

       

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

        private async Task SendWithKeyboardAsync(Chat chat, string text, IReplyMarkup? replyMarkup, CancellationToken ct)
        {
            await _botClient.SendTextMessageAsync(
                chat.Id,
                text,
                replyMarkup: replyMarkup,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }

        private string GetHelpText()
        {
            return "Доступные команды:\n\n" +
                   "/start — начать работу\n" +
                   "/help — эта справка\n" +
                   "/info — информация о тебе и лимитах\n" +
                   "/addtask <название> — добавить задачу\n" +
                   "/showtasks — показать активные задачи\n" +
                   "/showalltasks — показать все задачи\n" +
                   "/completetask <id> — завершить задачу\n" +
                   "/removetask <id> — удалить задачу\n" +
                   "/report — статистика по задачам\n" +
                   "/find <префикс> — поиск по названию\n\n" +
                   "ID задач оборачиваются в `обратные кавычки` — удобно копировать.";
        }

        private string GetInfoText()
        {
            if (CurrentUser == null) return "Не удалось получить информацию о пользователе.";

            return $"Пользователь: @{CurrentUser.TelegramUserName}\n" +
                   $"Tg ID: {CurrentUser.TelegramUserId}\n" +
                   $"Зареган: {CurrentUser.RegisteredAt:dd.MM.yyyy HH:mm:ss}\n\n" +
                   $"Лимит задач: {_maxTaskCount}\n" +
                   $"Лимит символов": {_maxTaskLength} символов"+
                   $"\nДата создания: 17.11.2025\n" +
                   $"Версия: 1.6.0\n" +
                   $"Обновлена до актуальной версии: 06.02.2026\n";
        }

        private async Task HandleAddTaskAsync(Chat chat, string[] parts, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            if (parts.Length < 2)
            {
                await SendWithKeyboardAsync(chat, "Использование: /addtask Название задачи", GetMainKeyboard(), ct);
                return;
            }

            string name = string.Join(" ", parts, 1, parts.Length - 1);
            var task = await _toDoService.AddTaskAsync(CurrentUser, name, ct);

            await SendWithKeyboardAsync(chat,
                $"Добавлена задача: \"{task.Name}\" (`{task.Id}`)",
                GetMainKeyboard(),
                ct);
        }

        private async Task ShowActiveTasksAsync(Chat chat, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var tasks = await _toDoService.GetActiveByUserIdAsync(CurrentUser.UserId, ct);

            if (tasks.Count == 0)
            {
                await SendWithKeyboardAsync(chat, "Активных задач пока нет.", GetMainKeyboard(), ct);
                return;
            }

            var sb = new StringBuilder("Активные задачи:\n\n");
            foreach (var t in tasks)
            {
                sb.AppendLine($"- {t.Name} (`{t.Id}`) • {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }

            await SendWithKeyboardAsync(chat, sb.ToString(), GetMainKeyboard(), ct);
        }

        private async Task ShowAllTasksAsync(Chat chat, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var tasks = await _toDoService.GetAllByUserIdAsync(CurrentUser.UserId, ct);

            if (tasks.Count == 0)
            {
                await SendWithKeyboardAsync(chat, "Задач пока нет.", GetMainKeyboard(), ct);
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
            await SendWithKeyboardAsync(chat, $"Задача `{id}` помечена как завершённая.", GetMainKeyboard(), ct);
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

        private async Task HandleReportAsync(Chat chat, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var (total, completed, active, generatedAt) = await _reportService.GetUserStatsAsync(CurrentUser.UserId, ct);

            var msg = $"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}\n\n" +
                      $"Всего задач: {total}\n" +
                      $"Завершено: {completed}\n" +
                      $"Активно: {active}";

            await SendWithKeyboardAsync(chat, msg, GetMainKeyboard(), ct);
        }

        private async Task HandleFindAsync(Chat chat, string[] parts, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            if (parts.Length < 2)
            {
                await SendWithKeyboardAsync(chat, "Использование: /find <префикс>", GetMainKeyboard(), ct);
                return;
            }

            string prefix = string.Join(" ", parts, 1, parts.Length - 1);
            var tasks = await _toDoService.FindAsync(CurrentUser, prefix, ct);

            if (tasks.Count == 0)
            {
                await SendWithKeyboardAsync(chat,
                    $"Активных задач, начинающихся на «{prefix}», не найдено.",
                    GetMainKeyboard(), ct);
                return;
            }

            var sb = new StringBuilder($"Найдено {tasks.Count} активных задач:\n\n");
            foreach (var t in tasks)
            {
                sb.AppendLine($"- {t.Name} (`{t.Id}`) • {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }

            await SendWithKeyboardAsync(chat, sb.ToString(), GetMainKeyboard(), ct);
        }
    }
}