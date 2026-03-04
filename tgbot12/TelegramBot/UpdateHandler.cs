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
using ToDoListBot.TelegramBot.Scenarios;

namespace ToDoListBot.TelegramBot
{
    public class UpdateHandler  // помогите 
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoReportService _reportService;
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;
        private readonly ITelegramBotClient _botClient;
        private readonly IEnumerable<IScenario> _scenarios;
        private readonly IScenarioContextRepository _contextRepository;

        private static ToDoUser? CurrentUser;

        public UpdateHandler(
            IUserService userService,
            IToDoService toDoService,
            IToDoReportService reportService,
            int maxTaskCount,
            int maxTaskLength,
            ITelegramBotClient botClient,
            IEnumerable<IScenario> scenarios,
            IScenarioContextRepository contextRepository)
        {
            _userService = userService;
            _toDoService = toDoService;
            _reportService = reportService;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
            _botClient = botClient;
            _scenarios = scenarios;
            _contextRepository = contextRepository;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Message is not { } message) return;
            if (message.Text is not { } text) return;

            var chat = message.Chat;
            var from = message.From ?? throw new InvalidOperationException("No From user");

            long tgId = from.Id;
            string username = from.Username ?? "Unknown";

            CurrentUser = await _userService.GetUserAsync(tgId, ct);

            var context = await _contextRepository.GetContext(tgId, ct);

            // 1. Обработка /cancel в любой момент
            if (text.Trim().ToLowerInvariant() == "/cancel")
            {
                if (context != null && context.CurrentScenario != ScenarioType.None)
                {
                    await _contextRepository.ResetContext(tgId, ct);
                    await SendWithKeyboardAsync(chat, "Сценарий отменён.", GetMainKeyboard(), ct);
                }
                else
                {
                    await SendWithKeyboardAsync(chat, "Нет активного сценария для отмены.", GetMainKeyboard(), ct);
                }
                return;
            }

            // 2. Проверяем, есть ли активный сценарий
           
            if (context != null && context.CurrentScenario != ScenarioType.None)
            {
                await ProcessScenario(context, message, ct);
                return;
            }

            // 3. Обычные команды (если сценария нет)
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
                        var newContext = new ScenarioContext(ScenarioType.AddTask);
                        await _contextRepository.SetContext(tgId, newContext, ct);
                        await SendWithKeyboardAsync(chat, "Введите название задачи:", GetCancelKeyboard(), ct);
                        await ProcessScenario(newContext, message, ct);
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
                            GetMainKeyboard(),
                            ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chat.Id, $"Ошибка: {ex.Message}", cancellationToken: ct);
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            var errorMsg = exception switch
            {
                ApiRequestException api => $"Telegram API Error [{api.ErrorCode}]: {api.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMsg);
            return Task.CompletedTask;
        }



        //keyboards

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

        private static ReplyKeyboardMarkup GetCancelKeyboard() => new(new[]
        {
            new KeyboardButton("/cancel")
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        //messages

        private async Task SendWithKeyboardAsync(Chat chat, string text, ReplyKeyboardMarkup? replyMarkup, CancellationToken ct)
        {
            await _botClient.SendMessage(
                chat.Id,
                text,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
        }

      //info and etc

        private string GetHelpText()
        {
            return "Доступные команды:\n\n" +
                   "/start — начать работу\n" +
                   "/help — эта справка\n" +
                   "/info — информация о тебе и лимитах\n" +
                   "/addtask — добавить задачу (сценарий)\n" +
                   "/showtasks — показать активные задачи\n" +
                   "/showalltasks — показать все задачи\n" +
                   "/completetask <id> — завершить задачу\n" +
                   "/removetask <id> — удалить задачу\n" +
                   "/report — статистика по задачам\n" +
                   "/find <префикс> — поиск по названию\n" +
                   "/cancel — отменить текущий сценарий\n\n" +
                   "ID задач в `обратных кавычках` — удобно копировать.";
        }

        private string GetInfoText()
        {
            if (CurrentUser == null) return "Не удалось получить информацию.";

            return $"Пользователь: @{CurrentUser.TelegramUserName}\n" +
                   $"ID: {CurrentUser.TelegramUserId}\n" +
                   $"Зарегистрирован: {CurrentUser.RegisteredAt:dd.MM.yyyy HH:mm:ss}\n\n" +
                   $"Лимит задач: {_maxTaskCount}\n" +
                   $"Макс. длина: {_maxTaskLength} символов\n" +
                   $"Дата создания: 17.11.2025\n" +
                   $"Последнее обновление: 22.02.2026\n" +
                   $"Версия: 1.8.0";
        }

        //scenarios

        private IScenario GetScenario(ScenarioType scenarioType)
        {
            var scenario = _scenarios.FirstOrDefault(s => s.CanHandle(scenarioType));
            if (scenario == null)
                throw new InvalidOperationException($"Сценарий {scenarioType} не найден");

            return scenario;
        }

        private async Task ProcessScenario(ScenarioContext context, Message message, CancellationToken ct)
        {
            var scenario = GetScenario(context.CurrentScenario);
            var result = await scenario.HandleMessageAsync(_botClient, context, message, ct);

            switch (result)
            {
                case ScenarioResult.Completed:
                    await _contextRepository.ResetContext(message.From.Id, ct);
                    await SendWithKeyboardAsync(message.Chat, "Сценарий завершён.", GetMainKeyboard(), ct);
                    break;

                case ScenarioResult.Transition:
                case ScenarioResult.Processed:
                    // Продолжаем сценарий, клавиатура с /cancel остаётся
                    await SendWithKeyboardAsync(message.Chat, "Продолжайте ввод...", GetCancelKeyboard(), ct);
                    break;
            }
        }

     //other commands

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
            if (parts.Length < 2)
            {
                await SendWithKeyboardAsync(chat, "Использование: /completetask <id>", GetMainKeyboard(), ct);
                return;
            }

            string rawInput = string.Join(" ", parts, 1, parts.Length - 1).Trim();
            string cleanId = rawInput
                .Replace("`", "")
                .Replace("\"", "")
                .Replace("'", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "")
                .Trim();

            if (string.IsNullOrWhiteSpace(cleanId))
            {
                await SendWithKeyboardAsync(chat, "ID задачи не найден.", GetMainKeyboard(), ct);
                return;
            }

            if (!Guid.TryParse(cleanId, out Guid id))
            {
                await SendWithKeyboardAsync(chat,
                    "Неверный формат ID. Скопируйте только сам Guid (без кавычек и текста).",
                    GetMainKeyboard(), ct);
                return;
            }

            try
            {
                await _toDoService.MarkCompletedAsync(id, ct);
                await SendWithKeyboardAsync(chat, $"Задача `{id}` завершена.", GetMainKeyboard(), ct);
            }
            catch (KeyNotFoundException)
            {
                await SendWithKeyboardAsync(chat, $"Задача `{id}` не найдена.", GetMainKeyboard(), ct);
            }
        }

        private async Task HandleRemoveTaskAsync(Chat chat, string[] parts, CancellationToken ct)
        {
            if (parts.Length < 2)
            {
                await SendWithKeyboardAsync(chat, "Использование: /removetask <id>", GetMainKeyboard(), ct);
                return;
            }

            string rawInput = string.Join(" ", parts, 1, parts.Length - 1).Trim();
            string cleanId = rawInput
                .Replace("`", "")
                .Replace("\"", "")
                .Replace("'", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "")
                .Trim();

            if (string.IsNullOrWhiteSpace(cleanId))
            {
                await SendWithKeyboardAsync(chat, "ID задачи не найден.", GetMainKeyboard(), ct);
                return;
            }

            if (!Guid.TryParse(cleanId, out Guid id))
            {
                await SendWithKeyboardAsync(chat,
                    "Неверный формат ID. Скопируйте только сам Guid (без кавычек и текста).",
                    GetMainKeyboard(), ct);
                return;
            }

            try
            {
                await _toDoService.DeleteAsync(id, ct);
                await SendWithKeyboardAsync(chat, $"Задача `{id}` удалена.", GetMainKeyboard(), ct);
            }
            catch (KeyNotFoundException)
            {
                await SendWithKeyboardAsync(chat, $"Задача `{id}` не найдена.", GetMainKeyboard(), ct);
            }
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
                    GetMainKeyboard(),
                    ct);
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