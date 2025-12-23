using Otus.ToDoList.ConsoleBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// классы из types

namespace Otus.ToDoList.ConsoleBot.Types
{
    public class Chat
    {
        public long Id { get; init; }
    }

    public class User
    {
        public long Id { get; init; }
        public string? Username { get; init; }
    }

    public class Message
    {
        public int Id { get; init; }
        public string Text { get; init; } = string.Empty;
        public Chat Chat { get; init; } = null!;
        public User From { get; init; } = null!;
    }

    public class Update
    {
        public Message Message { get; init; } = null!;
    }
}

// tg imitate


namespace Otus.ToDoList.ConsoleBot
{
    public interface ITelegramBotClient
    {
        Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
    }

    public interface IUpdateHandler
    {
        Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken = default);
    }

    public class ConsoleBotClient : ITelegramBotClient
    {
        public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Бот → {chatId}]: {text}");
            Console.ResetColor();
            await Task.CompletedTask;
        }

        public void StartReceiving(IUpdateHandler handler)
        {
            Console.WriteLine("Консольный бот запущен. Вводите команды (например: /help, /start, /addtask Купить молоко)");
            Console.WriteLine("Для выхода введите /exit\n");

            while (true)
            {
                Console.Write("Вы: ");
                string? input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                    break;

                var update = new Otus.ToDoList.ConsoleBot.Types.Update
                {
                    Message = new Otus.ToDoList.ConsoleBot.Types.Message
                    {
                        Chat = new Otus.ToDoList.ConsoleBot.Types.Chat { Id = 1 },
                        From = new Otus.ToDoList.ConsoleBot.Types.User { Id = 1, Username = "ConsoleUser" },
                        Text = input
                    }
                };

                handler.HandleUpdateAsync(this, update, CancellationToken.None).GetAwaiter().GetResult();
            }
        }
    }
}


// break point

namespace ToDoListBot
{
    using Otus.ToDoList.ConsoleBot;
    using Otus.ToDoList.ConsoleBot.Types;

    //cнизу классы

    public enum ToDoItemState
    {
        Active,
        Completed
    }

    public class ToDoUser
    {
        public Guid UserId { get; init; } = Guid.NewGuid();
        public long TelegramUserId { get; init; }
        public string TelegramUserName { get; init; }
        public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

        public ToDoUser(long telegramUserId, string telegramUserName)
        {
            TelegramUserId = telegramUserId;
            TelegramUserName = telegramUserName ?? throw new ArgumentNullException(nameof(telegramUserName));
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
            if (State == ToDoItemState.Completed)
                return;

            State = ToDoItemState.Completed;
            StateChangedAt = DateTime.UtcNow;
        }
    }

    // IUserSevice

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

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
            => _tasks.Where(t => t.User.UserId == userId).ToList().AsReadOnly();

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
            => _tasks.Where(t => t.User.UserId == userId && t.State == ToDoItemState.Active).ToList().AsReadOnly();

        public ToDoItem AddTask(ToDoUser user, string name)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название задачи не может быть пустым");

            if (name.Length > _maxTaskLength)
                throw new ArgumentException($"Длина задачи ({name.Length}) превышает лимит {_maxTaskLength}");

            var activeTasks = GetActiveByUserId(user.UserId);
            if (activeTasks.Count >= _maxTaskCount)
                throw new InvalidOperationException($"Превышено максимальное количество активных задач ({_maxTaskCount})");

            if (activeTasks.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Такая активная задача уже существует");

            var task = new ToDoItem(user, name);
            _tasks.Add(task);
            return task;
        }

        public void MarkCompleted(Guid taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                throw new KeyNotFoundException("Задача не найдена");

            if (task.State == ToDoItemState.Completed)
                throw new InvalidOperationException("Задача уже завершена");

            task.Complete();
        }

        public void Delete(Guid taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                throw new KeyNotFoundException("Задача не найдена");

            _tasks.Remove(task);
        }
    }

    // update'ник

    public class UpdateHandler : IUpdateHandler
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        public UpdateHandler(IUserService userService, IToDoService toDoService, int maxTaskCount, int maxTaskLenght)
        {
            _userService = userService;
            _toDoService = toDoService;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLenght;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct = default)
        {
            if (update.Message is null)
                return;

            var msg = update.Message;
            var chatId = msg.Chat.Id;
            var fromId = msg.From.Id;
            var username = msg.From.Username ?? "User" + fromId;

            // new user

            var user = _userService.GetUser(fromId);
            if (user == null)
            {
                _userService.RegisterUser(fromId, username);
                user = _userService.GetUser(fromId)!;
            }

            var text = msg.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text) || !text.StartsWith('/'))
                return;

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var cmd = parts[0].ToLowerInvariant();

            try
            {
                switch (cmd)
                {
                    case "/start":
                        await botClient.SendMessageAsync(chatId, $"Привет, {user.TelegramUserName}! Ты уже зарегистрирован.", ct);
                        break;

                    case "/help":
                        await SendHelp(botClient, chatId, user, ct);
                        break;

                    case "/info":
                        await SendInfo(botClient, chatId, user, ct);
                        break;

                    case "/addtask":
                        if (parts.Length < 2)
                        {
                            await botClient.SendMessageAsync(chatId, "Использование: /addtask <название задачи>", ct);
                            return;
                        }
                        await AddTask(botClient, chatId, user, string.Join(" ", parts[1..]), ct);
                        break;

                    case "/showtasks":
                        await ShowActiveTasks(botClient, chatId, user, ct);
                        break;

                    case "/showalltasks":
                        await ShowAllTasks(botClient, chatId, user, ct);
                        break;

                    case "/completetask":
                        if (parts.Length < 2 || !Guid.TryParse(parts[1], out var completeId))
                        {
                            await botClient.SendMessageAsync(chatId, "Использование: /completetask <id задачи>", ct);
                            return;
                        }
                        await CompleteTask(botClient, chatId, user, completeId, ct);
                        break;

                    case "/removetask":
                        if (parts.Length < 2 || !Guid.TryParse(parts[1], out var removeId))
                        {
                            await botClient.SendMessageAsync(chatId, "Использование: /removetask <id задачи>", ct);
                            return;
                        }
                        await RemoveTask(botClient, chatId, user, removeId, ct);
                        break;

                    default:
                        await botClient.SendMessageAsync(chatId, "Неизвестная команда. Используй /help", ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessageAsync(chatId, $"Ошибка: {ex.Message}", ct);
            }
        }

        private async Task SendHelp(ITelegramBotClient bot, long chatId, ToDoUser user, CancellationToken ct)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Информация для @{user.TelegramUserName}:");
            sb.AppendLine($"/start — приветствие");
            sb.AppendLine($"/help — эта справка");
            sb.AppendLine($"/info — информация");
            sb.AppendLine($"/addtask <название> — добавить задачу");
            sb.AppendLine($"/showtasks — активные задачи");
            sb.AppendLine($"/showalltasks — все задачи");
            sb.AppendLine($"/completetask <id> — завершить задачу");
            sb.AppendLine($"/removetask <id> — удалить задачу");
            await bot.SendMessageAsync(chatId, sb.ToString(), ct);
        }

        private async Task SendInfo(ITelegramBotClient bot, long chatId, ToDoUser user, CancellationToken ct)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\nИнформация:");
            sb.AppendLine($"Пользователь: @{user.TelegramUserName}");
            sb.AppendLine($"ID: {user.UserId}");
            sb.AppendLine($"Зарегистрирован: {user.TelegramUserName:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"Лимит задач: {_maxTaskCount}");
            sb.AppendLine($"Лимит символов: {_maxTaskLength}");
            sb.AppendLine("\nДата создания: 17.11.2025");
            sb.AppendLine("Версия: 1.3.0");
            sb.AppendLine("Обновлена до актуальной версии: 23.12.2025");
            await bot.SendMessageAsync(chatId, sb.ToString(), ct);
        }
        

        private async Task AddTask(ITelegramBotClient bot, long chatId, ToDoUser user, string name, CancellationToken ct)
        {
            var task = _toDoService.AddTask(user, name);
            await bot.SendMessageAsync(chatId, $"Задача добавлена: \"{task.Name}\" (ID: {task.Id})", ct);
        }

        private async Task ShowActiveTasks(ITelegramBotClient bot, long chatId, ToDoUser user, CancellationToken ct)
        {
            var tasks = _toDoService.GetActiveByUserId(user.UserId);
            if (!tasks.Any())
            {
                await bot.SendMessageAsync(chatId, "У тебя нет активных задач.", ct);
                return;
            }

            var sb = new System.Text.StringBuilder($"Активные задачи ({tasks.Count}):\n");
            foreach (var t in tasks)
            {
                sb.AppendLine($"- {t.Name} (ID: {t.Id}) — {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }
            await bot.SendMessageAsync(chatId, sb.ToString(), ct);
        }

        private async Task ShowAllTasks(ITelegramBotClient bot, long chatId, ToDoUser user, CancellationToken ct)
        {
            var tasks = _toDoService.GetAllByUserId(user.UserId);
            if (!tasks.Any())
            {
                await bot.SendMessageAsync(chatId, "У тебя нет задач.", ct);
                return;
            }

            var sb = new System.Text.StringBuilder($"Все задачи ({tasks.Count}):\n");
            foreach (var t in tasks)
            {
                string state = t.State == ToDoItemState.Active ? "активна" : "выполнена";
                sb.AppendLine($"- {t.Name} ({state}) (ID: {t.Id}) — {t.CreatedAt:dd.MM.yyyy HH:mm}");
            }
            await bot.SendMessageAsync(chatId, sb.ToString(), ct);
        }

        private async Task CompleteTask(ITelegramBotClient bot, long chatId, ToDoUser user, Guid taskId, CancellationToken ct)
        {
            _toDoService.MarkCompleted(taskId);
            await bot.SendMessageAsync(chatId, $"Задача {taskId} завершена.", ct);
        }

        private async Task RemoveTask(ITelegramBotClient bot, long chatId, ToDoUser user, Guid taskId, CancellationToken ct)
        {
            _toDoService.Delete(taskId);
            await bot.SendMessageAsync(chatId, $"Задача {taskId} удалена.", ct);
        }
    }

    
    // мэйн снизу, я про него ваще забыл вот он и упал сюда

    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Тгшный бот интерфейс";

            try
            {
                var userService = new UserService();
                var toDoService = new ToDoService(maxTaskCount: 10, maxTaskLength: 100);

                var handler = new UpdateHandler(userService, toDoService, maxTaskCount:10, maxTaskLenght:100);
                var botClient = new ConsoleBotClient();

                Console.WriteLine("Бот работает, укажите команду:\n");

                botClient.StartReceiving(handler);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nКритическая ошибка: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                Console.WriteLine("\nНажмите Enter для выхода...");
                Console.ReadLine();
            }
        }
    }
}