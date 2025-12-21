using System;
using System.Collections.Generic;
using System.Linq;

// class todouser

public class ToDoUser
{
    public Guid UserId { get; init; } = Guid.NewGuid();  //newid
    public string TelegramUserName { get; init; } = string.Empty;  //имя
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;  // время регистрации

    // коструктор todouser
    public ToDoUser(string telegramUserName)
    {
        TelegramUserName = telegramUserName;
    }
}
// todoitem(enum)
public enum ToDoItemState
{
    Active,
    Completed                                 //active i completed
}

public class ToDoItem               //todoitem class
{
    public Guid Id { get; init; } = Guid.NewGuid();        //обращается в id
    public ToDoUser User { get; init; } = null!;            // todouser
    public string Name { get; init; } = string.Empty;              //имя
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;              //время создания
    public ToDoItemState State { get; private set; } = ToDoItemState.Active;        // стейт выполнения
    public DateTime? StateChangedAt { get; private set; }                // дата смены стейта

    // коструктор для пункта 2
    public ToDoItem(ToDoUser user, string name)
    {
        User = user;
        Name = name;
    }

    // для работы /completetask
    public void Complete()
    {
        State = ToDoItemState.Completed;
        StateChangedAt = DateTime.UtcNow;
    }
}


class Program
{
    static ToDoUser? currentUser = null;
    static bool isRun = true;

    static int MaxTaskCount = 10;
    static int MaxTaskLength = 100;

    static readonly List<ToDoItem> tasks = new();

    static void Main(string[] args)
    {
        Console.Title = "Тг ботик хех";

        try
        {
            ConfigureLimits();
            RunBot();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nОшибка запуска: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }

    static void ConfigureLimits()
    {
        Console.WriteLine("Настройка лимитов\n");

        MaxTaskCount = ParseAndValidateInt("Введите максимальное количество задач 1-100: ", 1, 100);
        MaxTaskLength = ParseAndValidateInt("Введите максимальную длину задачи 1-100: ", 1, 100);

        Console.WriteLine($"\nЛимиты выставлены: {MaxTaskCount} задач по {MaxTaskLength} символов\n");
    }

    static void RunBot()
    {
        Console.WriteLine("Это консольный тг бот напоминалка!");
        Console.WriteLine("Доступные команды: /start /help /info /echo /addtask /showtasks /showalltasks /completetask /removetask /exit\n");

        while (isRun)
        {
            Console.Write("Команда: ");
            string? input = Console.ReadLine();

            if (input is null)
            {
                isRun = false;
                continue;
            }

            input = input.Trim();
            if (string.IsNullOrWhiteSpace(input)) continue;

            switch (input.ToLower())
            {
                case "/start":
                    StartCommand();
                    break;

                case "/exit":
                    isRun = false;
                    Console.WriteLine($"До свидания, {(currentUser?.TelegramUserName ?? "незнакомец")}!");
                    break;

                case "/help":
                    HelpCommand();
                    break;

                case "/info":
                    InfoCommand();
                    break;

                case "/echo":
                    EchoCommand(input[5..].Trim());
                    break;

                case "/addtask":
                    AddTaskCommand();
                    break;

                case "/showtasks":
                    ShowTasksCommand();           // теперь выводит только ToDoItemState.Active
                    break;

                case "/showalltasks":
                    ShowAllTasksCommand();        // все задачи (выводить с ними стейт)
                    break;

                case "/completetask":
                    CompleteTaskCommand();
                    break;

                case "/removetask":
                    RemoveTaskCommand();
                    break;

                default:
                    Console.WriteLine("Неизвестная команда. /help — список команд\n");
                    break;
            }
        }
    }

    // модуль валидации
    static int ParseAndValidateInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
                return value;

            Console.WriteLine($"ОШИБКА: Введите число от {min} до {max}!\n");
        }
    }

    // commands
    static void StartCommand()                       //start (new)
    { 
        if (currentUser != null)
        {
            Console.WriteLine($"Вы уже представились как {currentUser.TelegramUserName}\n");
            return;
        }

        Console.Write("Введите ваше Telegram-имя: ");
        string? name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Имя не может быть пустым!\n");
            return;
        }

        currentUser = new ToDoUser(name);
        Console.WriteLine($"Привет, @{currentUser.TelegramUserName}! Ваш ID (ID: {currentUser.UserId})\n");
    }

    static void HelpCommand()             //help
    {
        Console.WriteLine($"\nКоманды для @{currentUser?.TelegramUserName ?? "гостя"}:");
        Console.WriteLine("/start          — представиться");
        Console.WriteLine("/help           — эта справка");
        Console.WriteLine("/info           — информация");
        Console.WriteLine("/echo <текст>   — повторить");
        Console.WriteLine("/addtask        — добавить задачу");
        Console.WriteLine("/showtasks      — активные задачи");
        Console.WriteLine("/showalltasks   — все задачи");
        Console.WriteLine("/completetask   — завершить задачу");
        Console.WriteLine("/removetask     — удалить задачу");
        Console.WriteLine("/exit           — выход\n");
    }

    static void InfoCommand()                            //info
    {
        Console.WriteLine($"\nИнформация:");
        Console.WriteLine($"Пользователь: @{currentUser?.TelegramUserName ?? "неизвестен"}");
        Console.WriteLine($"ID: {currentUser?.UserId}");
        Console.WriteLine($"Зарегистрирован: {currentUser?.RegisteredAt:dd.MM.yyyy HH:mm:ss}");
        Console.WriteLine($"Лимит задач: {MaxTaskCount}");
        Console.WriteLine($"Лимит символов: {MaxTaskLength}\n");
        Console.WriteLine("Дата создания: 17.11.2025");
        Console.WriteLine("Версия: 1.2.0");
        Console.WriteLine("Обновлена до актуальной версии: 12.12.2025");
    }

    static void EchoCommand(string text)                    //echo 
    {
        Console.WriteLine($"{currentUser?.TelegramUserName ?? "Гость"}, ты написал: {text}\n");
    }

    static void AddTaskCommand()                 //addtask
    {
        if (currentUser == null)
        {
            Console.WriteLine("Сначала выполните /start\n");
            return;
        }

        if (tasks.Count(t => t.User.UserId == currentUser.UserId && t.State == ToDoItemState.Active) >= MaxTaskCount)
        {
            Console.WriteLine($"Превышено максимальное количество активных задач ({MaxTaskCount})\n");
            return;
        }

        Console.Write("Описание задачи: ");
        string? raw = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(raw))
        {
            Console.WriteLine("Описание не может быть пустым!\n");
            return;
        }

        string name = raw.Trim();
        if (name.Length > MaxTaskLength)
        {
            Console.WriteLine($"Длина задачи ({name.Length}) превышает лимит ({MaxTaskLength})\n");
            return;
        }

        if (tasks.Any(t => t.User.UserId == currentUser.UserId &&
                           t.State == ToDoItemState.Active &&
                           t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("Такая активная задача уже существует!\n");
            return;
        }

        var item = new ToDoItem(currentUser, name);
        tasks.Add(item);
        Console.WriteLine($"Задача добавлена! Активных: {tasks.Count(t => t.State == ToDoItemState.Active)}\n");
    }

  
    static void ShowTasksCommand()         //showtask вывод по примеру id все дела
    {
        if (currentUser == null)
        {
            Console.WriteLine("Сначала /start\n");
            return;
        }

        var active = tasks.Where(t => t.User.UserId == currentUser.UserId && t.State == ToDoItemState.Active).ToList();

        if (!active.Any())
        {
            Console.WriteLine("Активных задач нет.\n");
            return;
        }

        Console.WriteLine($"Активные задачи ({active.Count}):");
        foreach (var t in active)
        {
            Console.WriteLine($"{t.Name} - {t.CreatedAt:dd.MM.yyyy HH:mm:ss} - {t.Id}");
        }
        Console.WriteLine();
    }

           
    static void ShowAllTasksCommand()            //showalltask - все задачи (new command WW)
    {
        if (currentUser == null)
        {
            Console.WriteLine("Сначала /start\n");
            return;
        }

        var userTasks = tasks.Where(t => t.User.UserId == currentUser.UserId).ToList();

        if (!userTasks.Any())
        {
            Console.WriteLine("У вас нет задач.\n");
            return;
        }

        Console.WriteLine($"Все задачи ({userTasks.Count}):");
        foreach (var t in userTasks)
        {
            string state = t.State == ToDoItemState.Active ? "(Active)" : "(Completed)";
            Console.WriteLine($"{t.Name} - {t.CreatedAt:dd.MM.yyyy HH:mm:ss} {state} - {t.Id}");
        }
        Console.WriteLine();
    }

    
    static void CompleteTaskCommand()           //completetask - выполнение задачи и стейт (new command WWW)
    {
        if (currentUser == null)
        {
            Console.WriteLine("Сначала /start\n");
            return;
        }

        ShowTasksCommand(); // работает только с Active

        Console.Write("Введите Id задачи для завершения: ");
        string? idStr = Console.ReadLine();

        if (!Guid.TryParse(idStr, out Guid id))
        {
            Console.WriteLine("Некорректный Id\n");
            return;
        }

        var task = tasks.FirstOrDefault(t => t.Id == id && t.User.UserId == currentUser.UserId && t.State == ToDoItemState.Active);

        if (task == null)
        {
            Console.WriteLine("Задача не найдена или уже завершена\n");
            return;
        }

        task.Complete();
        Console.WriteLine($"/completetask {task.Id}");
        Console.WriteLine("Задача завершена!\n");
    }

    static void RemoveTaskCommand()              //removetask
    {
        if (currentUser == null)
        {
            Console.WriteLine("Сначала /start\n");
            return;
        }

        ShowAllTasksCommand();            

        Console.Write("Введите Id задачи для удаления: ");
        string? idStr = Console.ReadLine();

        if (!Guid.TryParse(idStr, out Guid id))
        {
            Console.WriteLine("Некорректный Id\n");
            return;
        }

        var task = tasks.FirstOrDefault(t => t.Id == id && t.User.UserId == currentUser.UserId);
        if (task == null)
        {
            Console.WriteLine("Задача не найдена\n");
            return;
        }
                   // only on test
        tasks.Remove(task);
        Console.WriteLine("Задача удалена\n");
    }
}