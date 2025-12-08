using System;
using System.Collections.Generic;

public class TaskCountLimitException : Exception     //count
{
    public TaskCountLimitException(string message) : base(message) { }
}

public class TaskLengthLimitException : Exception   //limit
{
    public TaskLengthLimitException(string message) : base(message) { }
}

class Program
{
    static string userName = "";
    static bool isStarted = false;
    static bool isRun = true;
    static int MaxTaskCount = 10;
    static int MaxTaskLength = 100;
    static List<string> tasks = new List<string>();

    static void Main(string[] args)
    {
        Console.Title = "тг бот хехеехеххеехехех";        // почему бы и нет ехехехехехех

        try                                   //catch в котором я не уверен
        {
            ConfigureLimits();
            RunBot();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nОшибка при запуске: {ex.Message}");
            Console.WriteLine("Программа будет завершена.");
        }
        finally
        {
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }

    static void ConfigureLimits()     //лимиты
    {
        Console.WriteLine("Настройка лимитов\n");

        MaxTaskCount = ParseAndValidateInt("Введите максимальное количество задач 1-100: ", 1, 100);
        MaxTaskLength = ParseAndValidateInt("Введите максимальную длину задачи 1-100: ", 1, 100);

        Console.WriteLine($"\nЛимиты выставлены: {MaxTaskCount} задач  по {MaxTaskLength} символов\n");
    }

    static void RunBot()
    {
       
        Console.WriteLine("Это консольный тг бот для задачек");
        Console.WriteLine("Доступные команды: /start /help /info /echo /addtask /showtasks /removetask /exit\n");

        while (isRun)
        {
            Console.Write("Команда: ");
            string? input = Console.ReadLine();      //null fix

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
                    Console.WriteLine($"До свидания, {userName}!");
                    break;

                case "/help":
                    HelpCommand();
                    break;

                case "/info":
                    InfoCommand();
                    break;

                default:
                    if (!isStarted)
                    { 
                        Console.WriteLine("Сначала выполните /start\n");      //запрос старта
                    }
                    else if (input.StartsWith("/echo", StringComparison.OrdinalIgnoreCase))
                        EchoCommand(input);
                    else if (input.Equals("/addtask", StringComparison.OrdinalIgnoreCase))
                        AddTaskCommand();
                    else if (input.Equals("/showtasks", StringComparison.OrdinalIgnoreCase))
                        ShowTasksCommand();
                    else if (input.Equals("/removetask", StringComparison.OrdinalIgnoreCase))
                        RemoveTaskCommand();
                    else
                        Console.WriteLine("Неизвестная команда. Введите /help\n");
                    break;
            }
        }
    }

    static int ParseAndValidateInt(string prompt, int min, int max)        //parse
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
                return value;

            Console.WriteLine($"ОШИБКА: Введите число от {min} до {max}!\n");
        }
    }

    static string ValidateString(string? input, string fieldName)           //validate
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException($"Поле \"{fieldName}\" не может быть пустым!");
        return input.Trim();
    }

    //comands

    static void StartCommand()
    {
        Console.Write("Введите ваше имя: ");
        try
        {
            userName = ValidateString(Console.ReadLine(), "Имя");
            isStarted = true;
            Console.WriteLine($"Привет, {userName}!\n");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}\n");
        }
    }

    static void HelpCommand()
    {
        Console.WriteLine($"\n{userName}, команды:");
        Console.WriteLine("/start      — представиться");
        Console.WriteLine("/help       — эта справка");
        Console.WriteLine("/info       — информация");
        Console.WriteLine("/echo <текст> — повторить");
        Console.WriteLine("/addtask    — добавить задачу");
        Console.WriteLine("/showtasks  — показать задачи");
        Console.WriteLine("/removetask — удалить задачу");
        Console.WriteLine("/exit       — выход\n");
    }

    static void InfoCommand()
    {
        Console.WriteLine($"\nИнформация о боте для {userName}:");
        Console.WriteLine($"Пользователь: {userName}");
        Console.WriteLine($"Лимит задач: {MaxTaskCount}");
        Console.WriteLine($"Лимит символов: {MaxTaskLength}\n");
        Console.WriteLine("Дата создания: 17.11.2025");
        Console.WriteLine("Версия: 1.1.0");
    }

    static void EchoCommand(string cmd)
    {
        string text = cmd.Length > 5 ? cmd.Substring(5).TrimStart() : "";
        Console.WriteLine($"{userName}, ты написал: {text}\n");
    }

    static void AddTaskCommand()
    {
        if (tasks.Count >= MaxTaskCount)
        {
            Console.WriteLine($"Превышено максимальное количество задач равное {MaxTaskCount}!\n");
            return;
        }

        Console.Write("Описание задачи: ");
        string? raw = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(raw))
        {
            Console.WriteLine("Поле \"Описание задачи\" не может быть пустым!\n");
            return;
        }

        string task = raw.Trim();

        if (task.Length > MaxTaskLength)
        {
            Console.WriteLine($"Длина задачи ({task.Length}) превышает максимально допустимое значение ({MaxTaskLength})!\n");
            return;
        }

        if (tasks.Exists(t => t.Equals(task, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("Такая задача уже существует! Дубликаты запрещены.\n");
            return;
        }

        tasks.Add(task);
        Console.WriteLine($"Задача добавлена! [{tasks.Count}/{MaxTaskCount}]\n");
    }

    static void ShowTasksCommand()
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine("Список задач пуст.\n");
            return;
        }

        Console.WriteLine($"Задачи ({tasks.Count}/{MaxTaskCount}):");
        for (int i = 0; i < tasks.Count; i++)
            Console.WriteLine($"  {i + 1,2}. {tasks[i]}");
        Console.WriteLine();
    }

    static void RemoveTaskCommand()
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine("Нет задач для удаления.\n");
            return;
        }

        ShowTasksCommand();
        int num = ParseAndValidateInt("Номер задачи для удаления: ", 1, tasks.Count);

        string removed = tasks[num - 1];
        tasks.RemoveAt(num - 1);
        Console.WriteLine($"Удалена задача: {removed}\n");
    }
}