using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;
using System.Text;

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

    public void HandleUpdateAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message?.Text is not { } text) return;

        var message = update.Message;
        var chat = message.Chat;
        var from = message.From ?? throw new InvalidOperationException("No From user");

        long tgId = from.Id;
        string username = from.Username ?? "Unknown";

        //reg+new user

        CurrentUser = _userService.GetUser(tgId);
        if (CurrentUser == null)
        {
            CurrentUser = _userService.RegisterUser(tgId, username);
            botClient.SendMessage(chat, $"Привет, @{username}! Ты зарегистрирован.\nИспользуй /help для списка команд.");
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
                    SendHelp(botClient, chat);
                    break;

                case "/info":
                    SendInfo(botClient, chat);
                    break;

                case "/addtask":
                    HandleAddTask(botClient, chat, parts);
                    break;

                case "/showtasks":
                    ShowActiveTasks(botClient, chat);
                    break;

                case "/showalltasks":
                    ShowAllTasks(botClient, chat);
                    break;

                case "/completetask":
                    HandleCompleteTask(botClient, chat, parts);
                    break;

                case "/removetask":
                    HandleRemoveTask(botClient, chat, parts);
                    break;

                case "/report":
                    HandleReport(botClient, chat);
                    break;

                case "/find":
                    HandleFind(botClient, chat, parts);
                    break;

                default:
                    botClient.SendMessage(chat, "Неизвестная команда. Используй /help");
                    break;
            }
        }
        catch (Exception ex)
        {
            botClient.SendMessage(chat, $"Ошибка: {ex.Message}");
        }
    }

    private void SendHelp(ITelegramBotClient bot, Chat chat)
    {
        var text = new StringBuilder()
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

        bot.SendMessage(chat, text.ToString());
    }

    private void SendInfo(ITelegramBotClient bot, Chat chat)
    {
        if (CurrentUser == null) return;

        var msg = $"Пользователь: @{CurrentUser.TelegramUserName}\n" +
                  $"Tg ID: {CurrentUser.TelegramUserId}\n" +
                  $"Зареган: {CurrentUser.RegisteredAt:dd.MM.yyyy HH:mm:ss}\n" +
                  $"Лимит задач: {_maxTaskCount}\n" +
                  $"Лимит символов: {_maxTaskLength}\n" +
                  $"\nДата создания: 17.11.2025\n" +
                  $"Версия: 1.4.0\n" +
                  $"Обновлена до актуальной версии: 26.12.2025\n");

        bot.SendMessage(chat, msg);
    }

    private void HandleAddTask(ITelegramBotClient bot, Chat chat, string[] parts)
    {
        if (CurrentUser == null) return;
        if (parts.Length < 2)
        {
            bot.SendMessage(chat, "Использование: /addtask Название задачи");
            return;
        }

        string name = string.Join(" ", parts[1..]);
        var task = _toDoService.AddTask(CurrentUser, name);
        bot.SendMessage(chat, $"Добавлена задача: \"{task.Name}\" (ID: {task.Id})");
    }

    private void ShowActiveTasks(ITelegramBotClient bot, Chat chat)
    {
        if (CurrentUser == null) return;

        var tasks = _toDoService.GetActiveByUserId(CurrentUser.UserId);
        if (!tasks.Any())
        {
            bot.SendMessage(chat, "Активных задач пока нет.");
            return;
        }

        var sb = new StringBuilder("Активные задачи:\n");
        foreach (var t in tasks)
            sb.AppendLine($"- {t.Name} (ID: {t.Id})  •  {t.CreatedAt:dd.MM.yyyy HH:mm}");

        bot.SendMessage(chat, sb.ToString());
    }

    private void ShowAllTasks(ITelegramBotClient bot, Chat chat)
    {
        if (CurrentUser == null) return;

        var tasks = _toDoService.GetAllByUserId(CurrentUser.UserId);
        if (!tasks.Any())
        {
            bot.SendMessage(chat, "Задач пока нет.");
            return;
        }

        var sb = new StringBuilder("Все задачи:\n");
        foreach (var t in tasks)
        {
            string state = t.State == ToDoItemState.Active ? "активна" : "завершена";
            sb.AppendLine($"- {t.Name}  ({state})  (ID: {t.Id})  •  {t.CreatedAt:dd.MM.yyyy HH:mm}");
        }

        bot.SendMessage(chat, sb.ToString());
    }

    private void HandleCompleteTask(ITelegramBotClient bot, Chat chat, string[] parts)
    {
        if (parts.Length < 2 || !Guid.TryParse(parts[1], out var id))
        {
            bot.SendMessage(chat, "Использование: /completetask <id>");
            return;
        }

        _toDoService.MarkCompleted(id);
        bot.SendMessage(chat, $"Задача {id} помечена как завершённая.");
    }

    private void HandleRemoveTask(ITelegramBotClient bot, Chat chat, string[] parts)
    {
        if (parts.Length < 2 || !Guid.TryParse(parts[1], out var id))
        {
            bot.SendMessage(chat, "Использование: /removetask <id>");
            return;
        }

        _toDoService.Delete(id);
        bot.SendMessage(chat, $"Задача {id} удалена.");
    }

    private void HandleReport(ITelegramBotClient bot, Chat chat)
    {
        if (CurrentUser == null) return;

        var (total, completed, active, at) = _reportService.GetUserStats(CurrentUser.UserId);

        var msg = $"Статистика по задачам на {at:dd.MM.yyyy HH:mm:ss}\n" +
                  $"Всего: {total}\n" +
                  $"Завершённых: {completed}\n" +
                  $"Активных: {active}";

        bot.SendMessage(chat, msg);
    }

    private void HandleFind(ITelegramBotClient bot, Chat chat, string[] parts)
    {
        if (CurrentUser == null) return;
        if (parts.Length < 2)
        {
            bot.SendMessage(chat, "Использование: /find Префикс");
            return;
        }

        string prefix = string.Join(" ", parts[1..]);
        var tasks = _toDoService.Find(CurrentUser, prefix);

        if (!tasks.Any())
        {
            bot.SendMessage(chat, $"Активных задач, начинающихся на «{prefix}», не найдено.");
            return;
        }

        var sb = new StringBuilder($"Найдено {tasks.Count} активных задач:\n");
        foreach (var t in tasks)
            sb.AppendLine($"- {t.Name} (ID: {t.Id})  •  {t.CreatedAt:dd.MM.yyyy HH:mm}");

        bot.SendMessage(chat, sb.ToString());
    }
}