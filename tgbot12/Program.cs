using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Otus.ToDoList.ConsoleBot; // ← школьная библиотека
using Otus.ToDoList.ConsoleBot.Types;

// === 1. ToDoUser — с TelegramUserId ===
public class ToDoUser
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public long TelegramUserId { get; init; }
    public string TelegramUserName { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

    public ToDoUser(long telegramUserId, string telegramUserName)
    {
        TelegramUserId = telegramUserId;
        TelegramUserName = telegramUserName;
    }
}

// === ToDoItem ===
public enum ToDoItemState
{
    Active,
    Completed
}

public class ToDoItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ToDoUser User { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public ToDoItemState State { get; private set; } = ToDoItemState.Active;
    public DateTime? StateChangedAt { get; private set; }

    public ToDoItem(ToDoUser user, string name)
    {
        User = user;
        Name = name;
    }

    public void Complete()
    {
        State = ToDoItemState.Completed;
        StateChangedAt = DateTime.UtcNow;
    }
}

// === Сервисы ===
public interface IUserService
{
    ToDoUser RegisterUser(long telegramUserId, string telegramUserName);
    ToDoUser? GetUser(long telegramUserId);
}

public class UserService : IUserService
{
    private readonly List<ToDoUser> _users = new();

    public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
    {
        var user = new ToDoUser(telegramUserId, telegramUserName);
        _users.Add(user);
        return user;
    }

    public ToDoUser? GetUser(long telegramUserId) => _users.FirstOrDefault(u => u.TelegramUserId == telegramUserId);
}

public interface IToDoService
{
    IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);
    IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);
    ToDoItem Add(ToDoUser user, string name);
    void MarkCompleted(Guid id);
    void Delete(Guid id);
}

public class ToDoService : IToDoService
{
    private readonly List<ToDoItem> _tasks = new();
    private readonly int _maxTaskCount;
    private readonly int _maxTaskLength;

    public ToDoService(int maxTaskCount, int maxTaskLength)
    {
        _maxTaskCount = maxTaskCount;
        _maxTaskLength = maxTaskLength;
    }

    public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId) => _tasks.Where(t => t.User.UserId == userId).ToList();

    public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId) => _tasks.Where(t => t.User.UserId == userId && t.State == ToDoItemState.Active).ToList();

    public ToDoItem Add(ToDoUser user, string name)
    {
        var activeCount = GetActiveByUserId(user.UserId).Count;
        if (activeCount >= _maxTaskCount)
            throw new InvalidOperationException($"Превышено максимальное количество активных задач ({_maxTaskCount})");

        if (name.Length > _maxTaskLength)
            throw new InvalidOperationException($"Длина задачи ({name.Length}) превышает лимит ({_maxTaskLength})");

        if (GetActiveByUserId(user.UserId).Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Такая активная задача уже существует!");

        var task = new ToDoItem(user, name);
        _tasks.Add(task);
        return task;
    }

    public void MarkCompleted(Guid id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task == null || task.State == ToDoItemState.Completed) return;
        task.Complete();
    }

    public void Delete(Guid id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null) _tasks.Remove(task);
    }
}

// === UpdateHandler ===
public class UpdateHandler : IUpdateHandler
{
    private readonly IUserService _userService;
    private readonly IToDoService _toDoService;

    public UpdateHandler(IUserService userService, IToDoService toDoService)
    {
        _userService = userService;
        _toDoService = toDoService;
    }

    public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message == null || update.Message.Text == null)
            return Task.CompletedTask;

        var message = update.Message;
        var chatId = message.Chat.Id;
        var userId = message.From.Id;
        var text = message.Text.Trim();

        var user = _userService.GetUser(userId);
        if (user == null)
        {
            user = _userService.RegisterUser(userId, message.From.Username ?? "Unknown");
            botClient.SendMessage(message.Chat, $"Привет, @{user.TelegramUserName}! Ты зарегистрирован.");
        }

        try
        {
            switch (text.ToLower())
            {
                case "/start":
                    botClient.SendMessage(message.Chat, $"Привет, @{user.TelegramUserName}! Ты уже зарегистрирован.");
                    break;

                case "/help":
                    botClient.SendMessage(message.Chat, GetHelpText());
                    break;

                case "/info":
                    botClient.SendMessage(message.Chat, $"@{user.TelegramUserName}\nID: {user.TelegramUserId}\nЗарегистрирован: {user.RegisteredAt:dd.MM.yyyy HH:mm:ss}");
                    break;

                case string t when t.StartsWith("/addtask "):
                    var name = t.Substring("/addtask ".Length).Trim();
                    _toDoService.Add(user, name);
                    botClient.SendMessage(message.Chat, "Задача добавлена!");
                    break;

                case "/showtasks":
                    var active = _toDoService.GetActiveByUserId(user.UserId);
                    botClient.SendMessage(message.Chat, FormatTasks(active, "Активные задачи:"));
                    break;

                case "/showalltasks":
                    var all = _toDoService.GetAllByUserId(user.UserId);
                    botClient.SendMessage(message.Chat, FormatTasks(all, "Все задачи:"));
                    break;

                case string t when t.StartsWith("/completetask "):
                    var idStr = t.Substring("/completetask ".Length).Trim();
                    if (Guid.TryParse(idStr, out var id))
                    {
                        _toDoService.MarkCompleted(id);
                        botClient.SendMessage(message.Chat, "Задача завершена!");
                    }
                    else
                    {
                        botClient.SendMessage(message.Chat, "Некорректный ID");
                    }
                    break;

                case string t when t.StartsWith("/removetask "):
                    var removeIdStr = t.Substring("/removetask ".Length).Trim();
                    if (Guid.TryParse(removeIdStr, out var removeId))
                    {
                        _toDoService.Delete(removeId);
                        botClient.SendMessage(message.Chat, "Задача удалена.");
                    }
                    else
                    {
                        botClient.SendMessage(message.Chat, "Некорректный ID");
                    }
                    break;

                default:
                    botClient.SendMessage(message.Chat, "Неизвестная команда. /help — список");
                    break;
            }
        }
        catch (Exception ex)
        {
            botClient.SendMessage(message.Chat, $"Ошибка: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    private string GetHelpText() =>
        "/start — начать\n" +
        "/help — справка\n" +
        "/info — информация\n" +
        "/addtask <текст> — добавить задачу\n" +
        "/showtasks — активные задачи\n" +
        "/showalltasks — все задачи\n" +
        "/completetask <id> — завершить\n" +
        "/removetask <id> — удалить";

    private string FormatTasks(IEnumerable<ToDoItem> items, string title)
    {
        var list = items.ToList();
        if (!list.Any()) return $"{title}\nНет задач.";

        var result = title + "\n";
        foreach (var item in list)
        {
            var state = item.State == ToDoItemState.Active ? "(Active)" : "(Completed)";
            result += $"{item.Name} - {item.CreatedAt:dd.MM.yyyy HH:mm:ss} {state} - {item.Id}\n";
        }
        return result;
    }
}

// === Program.cs ===
class Program
{
    static async Task Main(string[] args)
    {
        var botClient = new ConsoleBotClient(); // ← из школьной библиотеки

        var userService = new UserService();
        var toDoService = new ToDoService(10, 100); // лимиты

        var handler = new UpdateHandler( userService, toDoService);

        botClient.StartReceiving(handler);

        Console.WriteLine("Консольный бот запущен. Вводи команды (Ctrl+C — выход)");
        await Task.Delay(Timeout.Infinite);
    }
}