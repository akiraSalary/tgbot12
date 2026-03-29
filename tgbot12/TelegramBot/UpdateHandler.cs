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
using ToDoListBot.Helpers;

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
                        await ShowListsAsync(chat.Id, ct);
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

        internal static readonly ConcurrentDictionary<long, TaskCreationData> _pendingTasks = new();

        internal class TaskCreationData
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

        private static readonly Guid NoListId = new Guid("00000000-0000-0000-0000-000000000000");

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
                        var listDto = PagedListCallbackDto.FromString(callbackQuery.Data ?? "");

                       //bez lista s knopkoi
                        if (listDto.ToDoListId == NoListId)
                        {
                            var tasksWithoutList = await _toDoService.GetTasksWithoutListAsync(user.UserId, ct);

                            var pagedTasks = tasksWithoutList.GetBatch(_pageSize, listDto.Page).ToList();

                            var sb = new StringBuilder("Задачи без списка:\n\n");
                            if (pagedTasks.Count == 0)
                                sb.AppendLine("В этом разделе пока нет задач.");
                            else
                                sb.AppendLine($"Страница {listDto.Page + 1}");

                            var taskButtons = tasksWithoutList.Select(t =>
                                new KeyValuePair<string, string>(
                                    t.Name,
                                    new ToDoItemCallbackDto { Action = "showtask", ToDoItemId = t.Id }.ToString()
                                )).ToList();

                            var keyboard = BuildPagedButtons(taskButtons, listDto);

                           
                            var endKeyboard = new InlineKeyboardMarkup(
                                keyboard.InlineKeyboard.Concat(new[]
                                {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "✅ Посмотреть выполненные",
                        new PagedListCallbackDto
                        {
                            Action = "show_completed",
                            ToDoListId = NoListId,   
                            Page = 0
                        }.ToString())
                }
                                }).ToArray()
                            );

                            await _botClient.EditMessageText(
                                chatId, messageId, sb.ToString(),
                                replyMarkup: endKeyboard,
                                parseMode: ParseMode.Markdown,
                                cancellationToken: ct);

                            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                            break;
                        }

                        // s listId
                        var allTasks = await _toDoService.GetByUserIdAndListAsync(user.UserId, listDto.ToDoListId.Value, ct);

                        var pagedTasksList = allTasks.GetBatch(_pageSize, listDto.Page).ToList();

                        var sbList = new StringBuilder("Активные задачи:\n\n");
                        if (pagedTasksList.Count == 0)
                            sbList.AppendLine("В этом списке пока нет активных задач.");

                        var taskButtonsList = allTasks.Select(t =>
                            new KeyValuePair<string, string>(
                                t.Name,
                                new ToDoItemCallbackDto { Action = "showtask", ToDoItemId = t.Id }.ToString()
                            )).ToList();

                        var keyboardList = BuildPagedButtons(taskButtonsList, listDto);

                        var finalKeyboard = new InlineKeyboardMarkup(
                            keyboardList.InlineKeyboard.Concat(new[]
                            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "✅ Посмотреть выполненные",
                    new PagedListCallbackDto
                    {
                        Action = "show_completed",
                        ToDoListId = listDto.ToDoListId,
                        Page = 0
                    }.ToString())
            }
                            }).ToArray()
                        );

                        await _botClient.EditMessageText(
                            chatId, messageId, sbList.ToString(),
                            replyMarkup: finalKeyboard,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                        break;

                    case "show_completed":
                        var completedDto = PagedListCallbackDto.FromString(callbackQuery.Data ?? "");

                        Guid? targetListId = null;

                        
                        if (completedDto.ToDoListId != NoListId && completedDto.ToDoListId != null)
                        {
                            targetListId = completedDto.ToDoListId;   
                        }
                        

                        var completedTasks = await _toDoService.GetCompletedTasksAsync(
                            user.UserId,
                            targetListId,
                            ct);

                        var pagedCompleted = completedTasks.GetBatch(_pageSize, completedDto.Page).ToList();

                        var sb1 = new StringBuilder();

                        if (completedDto.ToDoListId == NoListId || completedDto.ToDoListId == null)
                            sb1.AppendLine("Выполненные задачи без списка:\n\n");
                        else
                            sb1.AppendLine("Выполненные задачи:\n\n");

                        if (pagedCompleted.Count == 0)
                        {
                            sb1.AppendLine("Задач нет");
                        }
                        else
                        {
                            sb1.AppendLine($"Страница {completedDto.Page + 1}");
                        }

                        var completedButtons = pagedCompleted.Select(t =>
                            new KeyValuePair<string, string>(
                                t.Name,
                                new ToDoItemCallbackDto
                                {
                                    Action = "showtask",
                                    ToDoItemId = t.Id
                                }.ToString()
                            )).ToList();

                        var COMPkeyboard = BuildPagedButtons(completedButtons, completedDto);

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            sb1.ToString(),
                            replyMarkup: COMPkeyboard,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                        break;



                    case "showtask":
                        var taskDto = ToDoItemCallbackDto.FromString(callbackQuery.Data ?? "");

                        var task = await _toDoService.GetToDoItemAsync(taskDto.ToDoItemId, ct);
                        if (task == null)
                        {
                            await _botClient.EditMessageText(chatId, messageId, "Задача не найдена.", cancellationToken: ct);
                            break;
                        }

                        var info = new StringBuilder($"**{task.Name}**\n\n");

                        if (task.Deadline.HasValue)
                            info.AppendLine($"Срок: {task.Deadline:dd.MM.yyyy HH:mm}");

                        info.AppendLine($"Время создания: {task.CreatedAt:dd.MM.yyyy HH:mm:ss}");

                        
                        if (task.State == ToDoItemState.Completed && task.StateChangedAt.HasValue)
                        {
                            info.AppendLine($"**Время выполнения: {task.StateChangedAt:dd.MM.yyyy HH:mm:ss}**");
                        }

                        
                        if (task.State == ToDoItemState.Completed)
                        {
                            await _botClient.EditMessageText(
                                chatId,
                                messageId,
                                info.ToString(),
                                parseMode: ParseMode.Markdown,
                                cancellationToken: ct);
                        }
                        else
                        {
                            
                            var buttons = new InlineKeyboardMarkup(new[]
                            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Выполнить",
                    new ToDoItemCallbackDto { Action = "completetask", ToDoItemId = task.Id }.ToString()),

                InlineKeyboardButton.WithCallbackData("❌ Удалить",
                    new ToDoItemCallbackDto { Action = "deletetask", ToDoItemId = task.Id }.ToString())
            }
        });

                            await _botClient.EditMessageText(
                                chatId,
                                messageId,
                                info.ToString(),
                                replyMarkup: buttons,
                                parseMode: ParseMode.Markdown,
                                cancellationToken: ct);
                        }

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                        break;

                    case "completetask":
                        var completeDto = ToDoItemCallbackDto.FromString(callbackQuery.Data ?? "");

                        // Выполняем задачу
                        await HandleCompleteTaskAsync(
                            callbackQuery.Message?.Chat ?? new Chat { Id = chatId },
                            new[] { "completetask", completeDto.ToDoItemId.ToString() },
                            ct);

                        
                        await _botClient.EditMessageReplyMarkup(
                            chatId: chatId,
                            messageId: messageId,
                            replyMarkup: null,
                            cancellationToken: ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "✅ Задача выполнена", cancellationToken: ct);

                       
                        await _botClient.SendMessage(chatId,
                            "✅ Задача выполнена!\n\n" +
                            "Нажми кнопку «✅ Посмотреть выполненные» ниже или команду /show",
                            cancellationToken: ct);

                        break;

                    // Удаление задачи
                    case "deletetask":
                        var deleteDto = ToDoItemCallbackDto.FromString(callbackQuery.Data ?? "");
                        var removeParts = new[] { "removetask", deleteDto.ToDoItemId.ToString() };

                        await HandleRemoveTaskAsync(
                            callbackQuery.Message?.Chat ?? new Chat { Id = chatId },
                            removeParts,
                            ct);

                        await _botClient.EditMessageReplyMarkup(
                             chatId: chatId,
                             messageId: messageId,
                             replyMarkup: null,
                             cancellationToken: ct);
                        break;




                    case "addtask":
                        var listIdStr = data.Split('|')[1];
                        Guid? selectedListId = null;
                        if (listIdStr != "none" && Guid.TryParse(listIdStr, out var parsedId))
                            selectedListId = parsedId;

                        if (!UpdateHandler._pendingTasks.TryGetValue(userId, out var taskData))
                        {
                            await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка: данные задачи потеряны", cancellationToken: ct);
                            return;
                        }

                        var name = taskData.Name;
                        var deadline = taskData.Deadline;

                        var newTask = await _toDoService.AddTaskAsync(user, name, selectedListId, ct);
                        newTask.SetDeadline(deadline);
                        await _toDoService.UpdateTaskAsync(newTask, ct);

                        var where = selectedListId == null ? "без списка" : "в выбранный список";

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"Задача \"{name}\" успешно добавлена {where}!\nДедлайн: {deadline:dd.MM.yyyy}\nID: `{newTask.Id}`",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);

                        UpdateHandler._pendingTasks.TryRemove(userId, out _);
                        await _contextRepository.ResetContext(userId, ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Задача добавлена!", cancellationToken: ct);
                        break;



                    case "addlist":
                        var currentUser = await _userService.GetUserAsync(userId, ct);
                        var addListContext = new ScenarioContext(ScenarioType.AddList)
                        {
                            UserId = userId
                        };

                        
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
                        var listId = dto.ToDoListId.Value;

                        var addTaskContext = new ScenarioContext(ScenarioType.AddTaskToList)
                        {
                            UserId = userId
                        };

                        
                        var userForTask = await _userService.GetUserAsync(userId, ct);
                        if (userForTask != null)
                        {
                            addTaskContext.Data["User"] = userForTask;
                            addTaskContext.Data["ListId"] = listId;
                        }
                        else
                        {
                            await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Пользователь не найден", cancellationToken: ct);
                            return;
                        }

                        await _contextRepository.SetContext(userId, addTaskContext, ct);

                        // Запускаем сценарий
                        await ProcessScenario(addTaskContext, callbackQuery.Message, ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Добавляем задачу в список", cancellationToken: ct);
                        break;

                    case "deletelist":
                        var deleteContext = new ScenarioContext(ScenarioType.DeleteList)
                        {
                            UserId = userId
                        };

                        await _contextRepository.SetContext(userId, deleteContext, ct);

                        // Показываем список списков 
                        await ProcessScenario(deleteContext, callbackQuery.Message, ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Выберите список для удаления", cancellationToken: ct);
                        break;


                        case "delete_confirm":
    var listIdToDelete = dto.ToDoListId!.Value;   

                        var list = await _toDoListService.GetAsync(listIdToDelete, ct);

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            $"Подтверждаете удаление списка **{list?.Name ?? "Неизвестный"}** и всех его задач?",
                            replyMarkup: new InlineKeyboardMarkup(new[]
                            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Да", $"delete_yes|{listIdToDelete}"),
                InlineKeyboardButton.WithCallbackData("❌ Нет", "delete_no")
            }
                            }),
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);

                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                        break;

                    case "delete_yes":
                        var idToDelete = dto.ToDoListId!.Value;
                        await _toDoListService.DeleteAsync(idToDelete, ct);

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "Список успешно удалён ✅",
                            replyMarkup: null,
                            cancellationToken: ct);

                        await _contextRepository.ResetContext(userId, ct);
                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                        break;

                    case "delete_no":
                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "Удаление отменено",
                            replyMarkup: null,
                            cancellationToken: ct);

                        await _contextRepository.ResetContext(userId, ct);
                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
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
                   $"Последнее обновление:29.03.2026\n" +
                   $"Версия: 2.0";
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

        private static readonly int _pageSize = 5;

        private InlineKeyboardMarkup BuildPagedButtons(
     IReadOnlyList<KeyValuePair<string, string>> callbackData,
     PagedListCallbackDto listDto)
        {
            
            var totalCount = callbackData.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)_pageSize);

           
            var currentPageButtons = callbackData
                .Skip(listDto.Page * _pageSize)
                .Take(_pageSize)
                .Select(kvp => new List<InlineKeyboardButton>
                {
            InlineKeyboardButton.WithCallbackData(kvp.Key, kvp.Value)
                })
                .ToList();

            var navigationRow = new List<InlineKeyboardButton>();

            if (listDto.Page > 0)
            {
                navigationRow.Add(InlineKeyboardButton.WithCallbackData("⬅️",
                    new PagedListCallbackDto
                    {
                        Action = listDto.Action,
                        ToDoListId = listDto.ToDoListId,
                        Page = listDto.Page - 1
                    }.ToString()));
            }

            if (listDto.Page < totalPages - 1)
            {
                navigationRow.Add(InlineKeyboardButton.WithCallbackData("➡️",
                    new PagedListCallbackDto
                    {
                        Action = listDto.Action,
                        ToDoListId = listDto.ToDoListId,
                        Page = listDto.Page + 1
                    }.ToString()));
            }

            if (navigationRow.Count > 0)
                currentPageButtons.Add(navigationRow);

            return new InlineKeyboardMarkup(currentPageButtons);
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

            var sb = new StringBuilder("Выберите список\n\n");

            var keyboardRows = new List<List<InlineKeyboardButton>>();

            // Кнопка "Без списка"
            keyboardRows.Add(new List<InlineKeyboardButton>
               {
                  InlineKeyboardButton.WithCallbackData(
                  "⭐ Без списка",
                   new PagedListCallbackDto { Action = "show", ToDoListId = NoListId, Page = 0 }.ToString())
               });

            // Кнопки с названиями списков
            foreach (var list in lists)
            {
                keyboardRows.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(list.Name, $"show|{list.Id}")
        });
            }

            // Нижний ряд кнопок
            keyboardRows.Add(new List<InlineKeyboardButton>
    {
        InlineKeyboardButton.WithCallbackData("📋 Добавить", "addlist"),
        InlineKeyboardButton.WithCallbackData("❌ Удалить", "deletelist")
    });

            var keyboard = new InlineKeyboardMarkup(keyboardRows);

            await _botClient.SendMessage(
                chatId,
                sb.ToString(),
                replyMarkup: keyboard,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
        private async Task HandleCompleteTaskAsync(Chat chat, string[] parts, CancellationToken ct)//
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