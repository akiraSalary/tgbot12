using System;
using System.Collections.Generic;
using System.Linq;
using Otus.ToDoList.ConsoleBot;           //  вот это для ITelegramBotClient/ConsoleBotClient
using Otus.ToDoList.ConsoleBot.Types;     //  а это для User/Chat/Message/Update

namespace ToDoListBot
{
    public enum ToDoItemState
    {
        Active,
        Completed
    }

    public class ToDoUser
    {
        public Guid UserId { get; init; } = Guid.NewGuid();
        public long TelegramUserId { get; init; }
        public string TelegramUserName { get; init; } = string.Empty;
        public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

        public ToDoUser(long telegramUserId, string telegramUserName)
        {
            TelegramUserId = telegramUserId;
            TelegramUserName = telegramUserName ?? "Unknown";
        }
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

    // services
    public interface IUserService
    {
        ToDoUser RegisterUser(long telegramUserId, string telegramUserName);
        ToDoUser? GetUser(long telegramUserId);
    }

    public class UserService : IUserService
    {
        private readonly Dictionary<long, ToDoUser> _users = new();

        public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
        {
            if (_users.TryGetValue(telegramUserId, out var existing))
                return existing;

            var user = new ToDoUser(telegramUserId, telegramUserName);
            _users[telegramUserId] = user;
            return user;
        }

        public ToDoUser? GetUser(long telegramUserId)
        {
            _users.TryGetValue(telegramUserId, out var user);
            return user;
        }
    }

    public interface IToDoService
    {
        IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);
        IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);
        ToDoItem AddTask(ToDoUser user, string name);
        void MarkCompleted(Guid taskId);
        void Delete(Guid taskId);
    }

    public class ToDoService : IToDoService
    {
        private readonly List<ToDoItem> _tasks = new();
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        public ToDoService(int maxTaskCount = 10, int maxTaskLength = 100)
        {
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
        }

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId) =>
            _tasks.Where(t => t.User.UserId == userId).ToList().AsReadOnly();

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId) =>
            _tasks.Where(t => t.User.UserId == userId && t.State == ToDoItemState.Active).ToList().AsReadOnly();

        public ToDoItem AddTask(ToDoUser user, string name)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название задачи не может быть пустым");

            if (name.Length > _maxTaskLength)
                throw new ArgumentException($"Длина задачи ({name.Length}) превышает лимит {_maxTaskLength}");

            var activeCount = GetActiveByUserId(user.UserId).Count;
            if (activeCount >= _maxTaskCount)
                throw new InvalidOperationException($"Превышено максимальное количество активных задач ({_maxTaskCount})");

            if (GetActiveByUserId(user.UserId).Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Такая активная задача уже существует");

            var task = new ToDoItem(user, name);
            _tasks.Add(task);
            return task;
        }

        public void MarkCompleted(Guid taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) throw new KeyNotFoundException("Задача не найдена");
            task.Complete();
        }

        public void Delete(Guid taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) throw new KeyNotFoundException("Задача не найдена");
            _tasks.Remove(task);
        }
    }

    // new update handlier
    public class UpdateHandler : IUpdateHandler
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        public static ToDoUser? _user;      // это юзер (global)

        public UpdateHandler(IUserService userService, IToDoService toDoService, int maxTaskCount, int maxTaskLength)
        {
            _userService = userService;
            _toDoService = toDoService;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
        }

        public void HandleUpdateAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message?.Text is not { } text) return;

            var message = update.Message;
            var chat = message.Chat;
            var fromId = message.From?.Id ?? 0;
            var username = message.From?.Username ?? "Unknown";

                           // rega polzovatelya
            _user = _userService.GetUser(fromId);
            if (_user == null)
            {
                _user = _userService.RegisterUser(fromId, username);
                botClient.SendMessage(chat, $"Привет, @{username}! Ты зарегистрирован, вводи команды (пример: /help,/start,/addtask <текст>.\n");
                return;
            }

            var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();

            try
            {
                switch (cmd)
                {
                    case "/start":
                        SendHelp(botClient, chat);
                        break;

                    case "/help":
                        SendHelp(botClient, chat);
                        break;

                    case "/info":
                        SendInfo(botClient, chat);
                        break;

                    case "/addtask":
                        if (parts.Length < 2)
                        {
                            botClient.SendMessage(chat, "Использование: /addtask <название>\n");
                            break;
                        }
                        AddTask(botClient, chat, string.Join(" ", parts[1..]));
                        break;

                    case "/showtasks":
                        ShowActiveTasks(botClient, chat);
                        break;

                    case "/showalltasks":
                        ShowAllTasks(botClient, chat);
                        break;

                    case "/completetask":
                        if (parts.Length < 2 || !Guid.TryParse(parts[1], out var completeId))
                        {
                            botClient.SendMessage(chat, "Использование: /completetask <id>\n");
                            break;
                        }
                        CompleteTask(botClient, chat, completeId);
                        break;

                    case "/removetask":
                        if (parts.Length < 2 || !Guid.TryParse(parts[1], out var removeId))
                        {
                            botClient.SendMessage(chat, "Использование: /removetask <id>\n");
                            break;
                        }
                        RemoveTask(botClient, chat, removeId);
                        break;

                    default:
                        botClient.SendMessage(chat, "Неизвестная команда. /help — список команд\n");
                        break;
                }
            }
            catch (Exception ex)
            {
                botClient.SendMessage(chat, $"Ошибка: {ex.Message}");
            }
        }

        private void SendHelp(ITelegramBotClient botClient, Chat chat)
        {
            botClient.SendMessage(chat,
                "/start — предстваится\n" +
                "/help — справка по командам\n" +
                "/info — информация\n" +
                "/addtask <название> — добавить задачу\n" +
                "/showtasks — активные задачи\n" +
                "/showalltasks — все задачи\n" +
                "/completetask <id> — завершить задачу по ID\n" +
                "/removetask <id> — удалить задачу по ID\n");
        }

        private void SendInfo(ITelegramBotClient botClient, Chat chat)
        {
            if (_user == null) return;


            botClient.SendMessage(chat,
            $"Пользователь: @{_user.TelegramUserName}\n" +
            $"Tg ID: {_user.TelegramUserId}\n" +
            $"Зареган: {_user.RegisteredAt:dd.MM.yyyy HH:mm:ss}\n" +
            $"Лимит задач: {_maxTaskCount}\n" +
            $"Лимит символов: {_maxTaskLength}\n" +
            $"\nДата создания: 17.11.2025\n" +
            $"Версия: 1.3.1\n" +
            $"Обновлена до актуальной версии: 26.12.2025\n");
        }

        private void AddTask(ITelegramBotClient botClient, Chat chat, string taskName)
        {
            if (_user == null) return;

            var task = _toDoService.AddTask(_user, taskName);
            botClient.SendMessage(chat, $"Задача добавлена: \"{task.Name}\" (ID: {task.Id})\n");
        }

        private void ShowActiveTasks(ITelegramBotClient botClient, Chat chat)
        {
            if (_user == null) return;

            var tasks = _toDoService.GetActiveByUserId(_user.UserId);
            if (!tasks.Any())
            {
                botClient.SendMessage(chat, "Активных задач нет.\n");
                return;
            }

            var sb = new System.Text.StringBuilder("Активные задачи:\n");
            foreach (var t in tasks)
                sb.AppendLine($"- {t.Name} (ID: {t.Id}) — {t.CreatedAt:dd.MM.yyyy HH:mm}");
            botClient.SendMessage(chat, sb.ToString());
        }

        private void ShowAllTasks(ITelegramBotClient botClient, Chat chat)
        {
            if (_user == null) return;

            var tasks = _toDoService.GetAllByUserId(_user.UserId);
            if (!tasks.Any())
            {
                botClient.SendMessage(chat, "Задач нет.\n");
                return;
            }

            var sb = new System.Text.StringBuilder("Все задачи:\n");
            foreach (var t in tasks)
            {
                string state = t.State == ToDoItemState.Active ? "активна" : "завершена";
                sb.AppendLine($"- {t.Name} ({state}) (ID: {t.Id}) — {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }
            botClient.SendMessage(chat, sb.ToString());
        }

        private void CompleteTask(ITelegramBotClient botClient, Chat chat, Guid taskId)
        {
            _toDoService.MarkCompleted(taskId);
            botClient.SendMessage(chat, $"Задача {taskId} завершена.\n");
        }

        private void RemoveTask(ITelegramBotClient botClient, Chat chat, Guid taskId)
        {
            _toDoService.Delete(taskId);
            botClient.SendMessage(chat, $"Задача {taskId} удалена.\n");
        }
    }

    // програм снова снизу =D
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "тгешка бот";

            var userService = new UserService();
            var toDoService = new ToDoService(10, 100);

            var handler = new UpdateHandler(userService, toDoService, 10, 100);

            var botClient = new ConsoleBotClient();

            botClient.StartReceiving(handler);
        }
    }
}