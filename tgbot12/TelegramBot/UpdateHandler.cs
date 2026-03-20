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
using ToDoListBot.TelegramBot.Dto;
using System.Collections.Concurrent;

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
        private readonly IToDoListService _toDoListService;


        private static ToDoUser? CurrentUser;

        public UpdateHandler(
            IUserService userService,
            IToDoService toDoService,
            IToDoReportService reportService,
            int maxTaskCount,
            int maxTaskLength,
            ITelegramBotClient botClient,
            IEnumerable<IScenario> scenarios,
            IScenarioContextRepository contextRepository,
            IToDoListService toDoListService)
        {
            _userService = userService;
            _toDoService = toDoService;
            _reportService = reportService;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
            _botClient = botClient;
            _scenarios = scenarios;
            _contextRepository = contextRepository;
            _toDoListService = toDoListService;
        }



        private async Task SendWithInlineKeyboardAsync(Chat chat, string text, InlineKeyboardMarkup? inlineKeyboard, CancellationToken ct)
        {
            await _botClient.SendMessage(
                chat.Id,
                text,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {


            if (update.CallbackQuery is { } callbackQuery)
            {
                await OnCallbackQuery(callbackQuery, ct);
                return;
            }

            if (update.Message is not { } message)
                return;

            if (message.Text is not { } text)
                return;

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
                        var user = await _userService.GetUserAsync(message.From.Id, ct);
                        if (user == null)
                        {
                            user = await _userService.RegisterUserAsync(
                                message.From.Id,
                                message.From.Username ?? "Unknown",
                                ct);

                            await SendWithKeyboardAsync(
                                chat,
                                $"Привет, @{user.TelegramUserName}! Ты успешно зарегистрирован.\nИспользуй меню или /help",
                                GetMainKeyboard(),
                                ct);
                        }
                        else
                        {
                            await SendWithKeyboardAsync(
                                chat,
                                $"С возвращением, @{user.TelegramUserName}!",
                                GetMainKeyboard(),
                                ct);
                        }
                        break;

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

                    case "show":
                        await ShowListsWithActionsAsync(chat, ct);
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

        private static readonly ConcurrentDictionary<long, TaskCreationData> _pendingTasks = new();

        private class TaskCreationData
        {
            public string Name { get; set; } = string.Empty;
            public DateTime Deadline { get; set; }
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

        private async Task ShowListsWithActionsAsync(Chat chat, CancellationToken ct)
        {
            if (CurrentUser == null)
            {
                await SendWithKeyboardAsync(chat, "Вы не зарегистрированы. Используйте /start", GetMainKeyboard(), ct);
                return;
            }

            var lists = await _toDoListService.GetUserListsAsync(CurrentUser.UserId, ct);

            var sb = new StringBuilder("Ваши списки задач:\n\n");

            if (lists.Count == 0)
            {
                sb.AppendLine("У вас пока нет списков задач.");
            }
            else
            {
                foreach (var list in lists)
                {
                    sb.AppendLine($"• {list.Name} (ID: `{list.Id}`)");
                }
            }

            var buttons = new List<List<InlineKeyboardButton>>();

            // Кнопка создания нового списка
            buttons.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("Создать новый список", "addlist")
    });

            // Если есть списки — кнопка удаления
            if (lists.Count > 0)
            {
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Удалить список", "show|delete")  // можно расширить до выбора списка
        });
            }

            var keyboard = new InlineKeyboardMarkup(buttons);

            await SendWithInlineKeyboardAsync(chat, sb.ToString(), keyboard, ct);
        }


        private async Task OnCallbackQuery(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var data = callbackQuery.Data;
            if (string.IsNullOrEmpty(data)) return;

            var dto = CallbackDto.FromString(data);
            if (dto == null) return;

            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var userId = callbackQuery.From.Id;


            var user = await _userService.GetUserAsync(userId, ct);
            if (user == null)
            {
                await _botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Вы не зарегистрированы. Используйте /start",
                    showAlert: true,
                    cancellationToken: ct);
                return;
            }

            try
            {
                switch (dto.Action)
                {
                    case "show":
                        if (dto.ToDoListId == null)
                        {
                            // списки инлайн
                            await ShowListsAsync(chatId, ct);
                        }
                        else
                        {
                            // конкретная кнопка
                            var tasks = await _toDoService.GetByUserIdAndListAsync(user.UserId, dto.ToDoListId.Value, ct);

                            var sb = new StringBuilder("Задачи списка:\n\n");

                            if (tasks.Count == 0)
                            {
                                sb.AppendLine("Активных задач нет");
                            }
                            else
                            {
                                foreach (var t in tasks)
                                {
                                    string state = t.State == ToDoItemState.Active ? "активна" : "завершена";
                                    sb.AppendLine($"• {t.Name} ({state}) (ID: `{t.Id}`)");
                                }
                            }

                            var buttons = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Добавить задачу в список", $"addtasktolist|{dto.ToDoListId}"),
                InlineKeyboardButton.WithCallbackData("Удалить список", $"deletelist|{dto.ToDoListId}")
            }
        };

                            var keyboard = new InlineKeyboardMarkup(buttons);

                            await _botClient.EditMessageText(
                                chatId,
                                messageId,
                                sb.ToString(),
                                replyMarkup: keyboard,
                                parseMode: ParseMode.Markdown,
                                cancellationToken: ct);
                        }

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                        break;

                    case "addtask":
                        var listIdStr = data.Split('|')[1];
                        Guid? selectedListId = null;
                        if (listIdStr != "none" && Guid.TryParse(listIdStr, out var parsedId))
                            selectedListId = parsedId;

                        if (!_pendingTasks.TryGetValue(userId, out var taskData))
                        {
                            await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка: данные задачи потеряны", cancellationToken: ct);
                            return;
                        }

                        var name = taskData.Name;
                        var deadline = taskData.Deadline;

                        var task = await _toDoService.AddTaskAsync(user, name, selectedListId, ct);
                        task.SetDeadline(deadline);
                        await _toDoService.UpdateTaskAsync(task, ct);

                        var where = selectedListId == null ? "без списка" : "в выбранный список";

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"Задача \"{name}\" добавлена {where} с дедлайном {deadline:dd.MM.yyyy}! (ID: {task.Id})",
                            replyMarkup: null,
                            cancellationToken: ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Задача добавлена!", cancellationToken: ct);

                        // чистим временные данные
                        _pendingTasks.TryRemove(userId, out _);
                        break;

                    case "addlist":
                        var addListContext = new ScenarioContext(ScenarioType.AddList)
                        {
                            UserId = userId
                        };

                        var currentUser = await _userService.GetUserAsync(userId, ct);
                        if (currentUser != null)
                        {
                            addListContext.Data["User"] = currentUser;
                        }

                        await _contextRepository.SetContext(userId, addListContext, ct);

                        // сразу сценарий
                        await ProcessScenario(addListContext, callbackQuery.Message, ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Создаём новый список", cancellationToken: ct);
                        break;

                    case "addtasktolist":
                        var addTaskContext = new ScenarioContext(ScenarioType.AddTaskToList);
                        addTaskContext.Data["ListId"] = dto.ToDoListId;
                        await _contextRepository.SetContext(userId, addTaskContext, ct);

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "Введите название задачи для этого списка:",
                            replyMarkup: null,
                            cancellationToken: ct);

                        await ProcessScenario(addTaskContext, null, ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Начинаем добавление задачи", cancellationToken: ct);
                        break;

                    case "deletelist":
                        var deleteListId = dto.ToDoListId;
                        if (deleteListId == null) return;

                        // Удаляем список
                        await _toDoListService.DeleteAsync(deleteListId.Value, ct);

                        // Уведомляем пользователя
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Список успешно удалён",
                            cancellationToken: ct);

                        // Редактируем сообщен
                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "Список удалён.\n\nВернитесь к /show для просмотра остальных списков.",
                            replyMarkup: null,
                            cancellationToken: ct);
                        break;

                    default:
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Неизвестное действие",
                            showAlert: true,
                            cancellationToken: ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                await _botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    $"Ошибка: {ex.Message}",
                    showAlert: true,
                    cancellationToken: ct);
            }
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
            new[] { new KeyboardButton("/show"), new KeyboardButton("/report") },
            new[] { new KeyboardButton("/help") }
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

        private async Task SendWithInlineKeyboard(Chat chat, string text, InlineKeyboardMarkup? inlineKeyboard, CancellationToken ct)
        {
            await _botClient.SendMessage(
                chat.Id,
                text,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Markdown,
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
                   "/show — показать активные задачи\n" +
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

        private async Task ProcessScenario(ScenarioContext context, Message? message, CancellationToken ct)
        {
            try
            {
                var scenario = GetScenario(context.CurrentScenario);
                var result = await scenario.HandleMessageAsync(_botClient, context, message, ct);

                switch (result)
                {
                    case ScenarioResult.Completed:
                        await _contextRepository.ResetContext(message?.From.Id ?? context.UserId ?? 0, ct);
                        if (message != null)
                            await SendWithKeyboardAsync(message.Chat, "Сценарий завершён.", GetMainKeyboard(), ct);
                        break;

                    case ScenarioResult.Transition:
                    case ScenarioResult.Processed:
                        if (message != null)
                            await SendWithKeyboardAsync(message.Chat, "Продолжайте ввод...", GetCancelKeyboard(), ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (message != null)
                    await _botClient.SendMessage(message.Chat.Id, $"Ошибка: {ex.Message}", cancellationToken: ct);
                await _contextRepository.ResetContext(message?.From.Id ?? context.UserId ?? 0, ct);
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

        private async Task ShowListsAsync(long chatId, CancellationToken ct)
        {
            if (CurrentUser == null)
            {
                await _botClient.SendMessage(chatId, "Вы не зарегистрированы. Используйте /start",
                    replyMarkup: GetMainKeyboard(), cancellationToken: ct);
                return;
            }

            var lists = await _toDoListService.GetUserListsAsync(CurrentUser.UserId, ct);

            var sb = new StringBuilder("Ваши списки задач:\n\n");

            var keyboardRows = new List<List<InlineKeyboardButton>>();

            if (lists.Count == 0)
            {
                sb.AppendLine("У вас пока нет списков задач.");
            }
            else
            {
                foreach (var list in lists)
                {
                    // кнопки списков
                    keyboardRows.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(list.Name, $"show|{list.Id}")
                    });
                        
         

                }
            }

            // новый список
            keyboardRows.Add(new List<InlineKeyboardButton>
                {
                  InlineKeyboardButton.WithCallbackData("Создать новый список", "addlist")
                });

            var keyboard = new InlineKeyboardMarkup(keyboardRows);

            
            await _botClient.SendMessage(
                chatId,
                sb.ToString(),
                replyMarkup: keyboard,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
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