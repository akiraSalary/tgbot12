using System;
using System.Collections.Generic;

//  новые исключения 
public class TaskCountLimitException : Exception
{
    public TaskCountLimitException(int taskCountLimit)
        : base($"Превышено максимальное количество задач равное {taskCountLimit}") { }
}

public class TaskLengthLimitException : Exception
{
    public TaskLengthLimitException(int taskLengthLimit, int tasklLength)
        : base($"Длина задачи ({tasklLength}) превышает максимально допустимое значение ({taskLengthLimit})") { }
}

public class DuplicateTaskException : Exception
{
    public DuplicateTaskException(string taskText)             //duplication!!!!!!
        : base($"Задача \"{taskText}\" уже существует!") { }
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
        Console.Title = "тг бот хехеехеххеехехех";

        try
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

    static void ConfigureLimits()
    {
        Console.WriteLine("Настройка лимитов\n");

        MaxTaskCount = ParseAndValidateInt("Введите максимальное количество задач 1-100: ", 1, 100);
        MaxTaskLength = ParseAndValidateInt("Введите максимальную длину задачи 1-100: ", 1, 100);

        Console.WriteLine($"\nЛимиты выставлены: {MaxTaskCount} задач по {MaxTaskLength} символов\n");
    }

    // новый цикл по try-catch
    static void RunBot()
    {
        Console.WriteLine("Это консольный тг бот для задачек");
        Console.WriteLine("Доступные команды: /start /help /info /echo /addtask /showtasks /removetask /exit\n");

        while (isRun)
        {
            try
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
                            Console.WriteLine("Сначала выполните /start\n");
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
            // тут catche, вроде даже работающий
            catch (TaskCountLimitException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}\n");
            }
            catch (TaskLengthLimitException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}\n");
            }
            catch (DuplicateTaskException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}\n");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Ошибка ввода: {ex.Message}\n");
            }
        }
    }

 
    static int ParseAndValidateInt(string prompt, int min, int max)      //parse теперь throw arguments
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
            return value;

        throw new ArgumentException($"Некорректное значение. Требуется число от {min} до {max}.");
    }

    static string ValidateString(string? input, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException($"Поле \"{fieldName}\" не может пустовать.");
        return input.Trim();
    }

     // подблок commands
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

    static void HelpCommand()          //help 
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

    static void InfoCommand()          //info 
    {
        Console.WriteLine($"\nИнформация о боте для {userName}:");
        Console.WriteLine($"Пользователь: {userName}");
        Console.WriteLine($"Лимит задач: {MaxTaskCount}");
        Console.WriteLine($"Лимит символов: {MaxTaskLength}\n");
        Console.WriteLine("Дата создания: 17.11.2025");
        Console.WriteLine("Версия: 1.1.1");
        Console.WriteLine("Обновлена до актуальной версии: 09.12.2025");
    }

    static void EchoCommand(string cmd)                    //echo
    {
        string text = cmd.Length > 5 ? cmd.Substring(5).TrimStart() : "";
        Console.WriteLine($"{userName}, бот увидел твой текст: {text}\n");
    }

    
    static void AddTaskCommand()                     // addtask с исключениями
    {
        if (tasks.Count >= MaxTaskCount)
            throw new TaskCountLimitException(MaxTaskCount);

        Console.Write("Описание задачи: ");
        string task = ValidateString(Console.ReadLine(), "Описание задачи");

        if (task.Length > MaxTaskLength)
            throw new TaskLengthLimitException(MaxTaskLength, task.Length);

        if (tasks.Contains(task, StringComparer.OrdinalIgnoreCase))
            throw new DuplicateTaskException(task);

        tasks.Add(task);
        Console.WriteLine($"Задача добавлена! [{tasks.Count}/{MaxTaskCount}]\n");
    }

    static void ShowTasksCommand()               //show
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine("Список задач пуст.\n");
            return;
        }
        Console.WriteLine($"Задачи ({tasks.Count}/{MaxTaskCount}):");
        for (int i = 0; i < tasks.Count; i++)
            Console.WriteLine($" {i + 1,2}. {tasks[i]}");
        Console.WriteLine();
    }

    static void RemoveTaskCommand()            //remove
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