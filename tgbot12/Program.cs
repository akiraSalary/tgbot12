using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Otus.ToDoList.ConsoleBot;           
using Otus.ToDoList.ConsoleBot.Types;     

namespace ToDoListBot
{
    // entiti
    

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

    
    // date acces+ интерфейсики тут
    

    public interface IUserRepository
    {
        ToDoUser? Get(Guid userId);
        ToDoUser? GetByTelegramUserId(long telegramUserId);
        void Add(ToDoUser user);
    }

    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<ToDoUser> _users = new();

        public ToDoUser? Get(Guid userId) =>
            _users.FirstOrDefault(u => u.UserId == userId);

        public ToDoUser? GetByTelegramUserId(long telegramUserId) =>
            _users.FirstOrDefault(u => u.TelegramUserId == telegramUserId);

        public void Add(ToDoUser user)
        {
            if (GetByTelegramUserId(user.TelegramUserId) != null) return;
            _users.Add(user);
        }
    }

    public interface IToDoRepository
    {
        IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);
        IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);
        ToDoItem? Get(Guid id);
        void Add(ToDoItem item);
        void Update(ToDoItem item);
        void Delete(Guid id);
        bool ExistsByName(Guid userId, string name);
        int CountActive(Guid userId);
        IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate);
    }

    public class InMemoryToDoRepository : IToDoRepository
    {
        private readonly List<ToDoItem> _items = new();

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId) =>
            _items.Where(i => i.User.UserId == userId).ToList().AsReadOnly();

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId) =>
            _items.Where(i => i.User.UserId == userId && i.State == ToDoItemState.Active)
                  .ToList().AsReadOnly();

        public ToDoItem? Get(Guid id) =>
            _items.FirstOrDefault(i => i.Id == id);

        public void Add(ToDoItem item) => _items.Add(item);

        public void Update(ToDoItem item) { } 

        public void Delete(Guid id)
        {
            var item = Get(id);
            if (item != null) _items.Remove(item);
        }

        public bool ExistsByName(Guid userId, string name) =>
            _items.Any(i => i.User.UserId == userId &&
                            i.State == ToDoItemState.Active &&
                            string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

        public int CountActive(Guid userId) =>
            GetActiveByUserId(userId).Count;

        public IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate) =>
            _items.Where(i => i.User.UserId == userId && predicate(i))
                  .ToList().AsReadOnly();
    }

  
    // _services_
    

    public interface IUserService
    {
        ToDoUser RegisterUser(long telegramUserId, string telegramUserName);
        ToDoUser? GetUser(long telegramUserId);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
        {
            var existing = _repository.GetByTelegramUserId(telegramUserId);
            if (existing != null) return existing;

            var user = new ToDoUser(telegramUserId, telegramUserName);
            _repository.Add(user);
            return user;
        }

        public ToDoUser? GetUser(long telegramUserId) =>
            _repository.GetByTelegramUserId(telegramUserId);
    }

    public interface IToDoService
    {
        IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);
        IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);
        ToDoItem AddTask(ToDoUser user, string name);
        void MarkCompleted(Guid taskId);
        void Delete(Guid taskId);
        IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix);
    }

    public class ToDoService : IToDoService
    {
        private readonly IToDoRepository _repository;
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        public ToDoService(IToDoRepository repository, int maxTaskCount = 10, int maxTaskLength = 100)
        {
            _repository = repository;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
        }

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId) =>
            _repository.GetAllByUserId(userId);

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId) =>
            _repository.GetActiveByUserId(userId);

        public ToDoItem AddTask(ToDoUser user, string name)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название задачи не может быть пустым");

            if (name.Length > _maxTaskLength)
                throw new ArgumentException($"Длина задачи ({name.Length}) превышает лимит {_maxTaskLength}");

            var activeCount = _repository.CountActive(user.UserId);
            if (activeCount >= _maxTaskCount)
                throw new InvalidOperationException($"Превышено максимальное количество активных задач ({_maxTaskCount})");

            if (_repository.ExistsByName(user.UserId, name))
                throw new InvalidOperationException("Такая активная задача уже существует");

            var task = new ToDoItem(user, name);
            _repository.Add(task);
            return task;
        }

        public void MarkCompleted(Guid taskId)
        {
            var task = _repository.Get(taskId)
                ?? throw new KeyNotFoundException("Задача не найдена");

            task.Complete();
            _repository.Update(task);
        }

        public void Delete(Guid taskId)
        {
            if (_repository.Get(taskId) == null)
                throw new KeyNotFoundException("Задача не найдена");

            _repository.Delete(taskId);
        }

        public IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix)
        {
            return _repository.Find(user.UserId, t =>
                t.State == ToDoItemState.Active &&
                t.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    public interface IToDoReportService
    {
        (int total, int completed, int active, DateTime generatedAt) GetUserStats(Guid userId);
    }

    public class ToDoReportService : IToDoReportService
    {
        private readonly IToDoRepository _repository;

        public ToDoReportService(IToDoRepository repository)
        {
            _repository = repository;
        }

        public (int total, int completed, int active, DateTime generatedAt) GetUserStats(Guid userId)
        {
            var all = _repository.GetAllByUserId(userId);
            int total = all.Count;
            int active = _repository.CountActive(userId);
            int completed = total - active;

            return (total, completed, active, DateTime.UtcNow);
        }
    }

    // update handler)
   

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
                      $"Лимит символов: {_maxTaskLength}\n"+
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

 
    // main program
   

    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "ToDo Telegram Console Bot";

            var userRepo = new InMemoryUserRepository();
            var todoRepo = new InMemoryToDoRepository();

            var userService = new UserService(userRepo);
            var todoService = new ToDoService(todoRepo, maxTaskCount: 10, maxTaskLength: 100);
            var reportService = new ToDoReportService(todoRepo);

            var handler = new UpdateHandler(
                userService,
                todoService,
                reportService,
                maxTaskCount: 10,
                maxTaskLength: 100);

            var botClient = new ConsoleBotClient();
            botClient.StartReceiving(handler);

            Console.WriteLine("Бот запущен. Вводите сообщения как в Telegram...");
            Console.ReadLine();
        }
    }
}