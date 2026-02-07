using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;

namespace ToDoListBot.TelegramBot
{
    public class UpdateHandler
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoReportService _reportService;
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        private static ToDoUser? CurrentUser;
        private static readonly HttpClient _http = new HttpClient();

        public UpdateHandler(
            IUserService userService,
            IToDoService toDoService,
            IToDoReportService reportService,
            int maxTaskCount,
            int maxTaskLength)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _toDoService = toDoService ?? throw new ArgumentNullException(nameof(toDoService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Message?.Text is not { } text)
                return;

            var message = update.Message;
            var chatId = message.Chat.Id;
            var from = message.From ?? throw new InvalidOperationException("No From user");

            long tgId = from.Id;
            string username = from.Username ?? "Unknown";

            CurrentUser = await _userService.GetUserAsync(tgId, ct);

            if (CurrentUser == null)
            {
                if (text.Trim().StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentUser = await _userService.RegisterUserAsync(tgId, username, ct);
                    await SendMessageHttpAsync(chatId, $"Привет, @{username}! Ты зарегистрирован. Используй /help для списка команд.", ct);
                }
                else
                {
                    await SendMessageHttpAsync(chatId, "Нажмите /start для регистрации", ct);
                }

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
                        await SendHelpAsync(chatId, ct);
                        break;

                    case "/info":
                        await SendInfoAsync(chatId, ct);
                        break;

                    case "/addtask":
                        await HandleAddTaskAsync(chatId, parts, ct);
                        break;

                    case "/showtasks":
                        await ShowActiveTasksAsync(chatId, ct);
                        break;

                    case "/showalltasks":
                        await ShowAllTasksAsync(chatId, ct);
                        break;

                    case "/completetask":
                        await HandleCompleteTaskAsync(chatId, parts, ct);
                        break;

                    case "/removetask":
                        await HandleRemoveTaskAsync(chatId, parts, ct);
                        break;

                    case "/report":
                        await HandleReportAsync(chatId, ct);
                        break;

                    case "/find":
                        await HandleFindAsync(chatId, parts, ct);
                        break;

                    default:
                        await SendMessageHttpAsync(chatId, "Неизвестная команда. Используй /help", ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                await SendMessageHttpAsync(chatId, $"Ошибка: {ex.Message}", ct);
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            return Task.CompletedTask;
        }


        private static string GetToken() =>
            Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? string.Empty;

        private static async Task SendMessageHttpAsync(long chatId, string text, CancellationToken ct)
        {
            var token = GetToken();
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("TELEGRAM_BOT_TOKEN not set");

            var url = $"https://api.telegram.org/bot{token}/sendMessage";
            var payload = new { chat_id = chatId, text };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(url, content, ct);
            resp.EnsureSuccessStatusCode();
        }

        private Task SendHelpAsync(long chatId, CancellationToken ct)
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

            return SendMessageHttpAsync(chatId, sb.ToString(), ct);
        }

        private Task SendInfoAsync(long chatId, CancellationToken ct)
        {
            if (CurrentUser == null) return Task.CompletedTask;

            var msg = $"Пользователь: @{CurrentUser.TelegramUserName}\n" +
                $"Tg ID: {CurrentUser.TelegramUserId}\n" +
                $"Зареган: {CurrentUser.RegisteredAt:dd.MM.yyyy HH:mm:ss}\n" +
                $"Лимит задач: {_maxTaskCount}\n" +
                $"Лимит символов: {_maxTaskLength}\n"+
                $"\nДата создания: 17.11.2025\n" +
                $"Версия: 1.6.1\n" +
                $"Обновлена до актуальной версии: 08.02.2026\n";

            return SendMessageHttpAsync(chatId, msg, ct);
        }

        private async Task HandleAddTaskAsync(long chatId, string[] parts, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            if (parts.Length < 2)
            {
                await SendMessageHttpAsync(chatId, "Использование: /addtask Название задачи", ct);
                return;
            }

            string name = string.Join(' ', parts, 1, parts.Length - 1);
            var task = await _toDoService.AddTaskAsync(CurrentUser, name, ct);

            await SendMessageHttpAsync(chatId, $"Добавлена задача: \"{task.Name}\" (ID: {task.Id})", ct);
        }

        private async Task ShowActiveTasksAsync(long chatId, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var tasks = await _toDoService.GetActiveByUserIdAsync(CurrentUser.UserId, ct);

            if (!tasks.Any())
            {
                await SendMessageHttpAsync(chatId, "Активных задач пока нет.", ct);
                return;
            }

            var sb = new StringBuilder("Активные задачи:\n");
            foreach (var t in tasks)
                sb.AppendLine($"- {t.Name} (ID: {t.Id}) • {t.CreatedAt:dd.MM.yyyy HH:mm}");

            await SendMessageHttpAsync(chatId, sb.ToString(), ct);
        }

        private async Task ShowAllTasksAsync(long chatId, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var tasks = await _toDoService.GetAllByUserIdAsync(CurrentUser.UserId, ct);

            if (!tasks.Any())
            {
                await SendMessageHttpAsync(chatId, "Задач пока нет.", ct);
                return;
            }

            var sb = new StringBuilder("Все задачи:\n");
            foreach (var t in tasks)
            {
                string state = t.State == ToDoItemState.Active ? "активна" : "завершена";
                sb.AppendLine($"- {t.Name} ({state}) (ID: {t.Id}) • {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }

            await SendMessageHttpAsync(chatId, sb.ToString(), ct);
        }

        private async Task HandleCompleteTaskAsync(long chatId, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var id))
            {
                await SendMessageHttpAsync(chatId, "Использование: /completetask <id>", ct);
                return;
            }

            await _toDoService.MarkCompletedAsync(id, ct);
            await SendMessageHttpAsync(chatId, $"Задача {id} помечена как завершённая.", ct);
        }

        private async Task HandleRemoveTaskAsync(long chatId, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2 || !Guid.TryParse(parts[1], out var id))
            {
                await SendMessageHttpAsync(chatId, "Использование: /removetask <id>", ct);
                return;
            }

            await _toDoService.DeleteAsync(id, ct);
            await SendMessageHttpAsync(chatId, $"Задача {id} удалена.", ct);
        }

        private async Task HandleReportAsync(long chatId, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            var (total, completed, active, generatedAt) = await _reportService.GetUserStatsAsync(CurrentUser.UserId, ct);

            var msg = $"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}\n" +
                $"Всего: {total}\n" +
                $"Завершённых: {completed}\n" +
                $"Активных: {active}";

            await SendMessageHttpAsync(chatId, msg, ct);
        }

        private async Task HandleFindAsync(long chatId, string[] parts, CancellationToken ct)
        {
            if (CurrentUser == null) return;

            if (parts.Length < 2)
            {
                await SendMessageHttpAsync(chatId, "Использование: /find Префикс", ct);
                return;
            }

            string prefix = string.Join(' ', parts, 1, parts.Length - 1);
            var tasks = await _toDoService.FindAsync(CurrentUser, prefix, ct);

            if (!tasks.Any())
            {
                await SendMessageHttpAsync(chatId, $"Активных задач, начинающихся на «{prefix}», не найдено.", ct);
                return;
            }

            var sb = new StringBuilder($"Найдено {tasks.Count} активных задач:\n");
            foreach (var t in tasks)
                sb.AppendLine($"- {t.Name} (ID: {t.Id}) • {t.CreatedAt:dd.MM.yyyy HH:mm}");

            await SendMessageHttpAsync(chatId, sb.ToString(), ct);
        }
    }
}