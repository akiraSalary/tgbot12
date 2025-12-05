using System;
using System.Collections.Generic;

class Program
{
    static string userName = "";
    static bool isStarted = false;
    static bool isRun = true;                   // есть isRun
    static List<string> tasks = new List<string>();

    static void Main(string[] args)
    {
        Console.WriteLine("Это тест менюха тг бота!");
        Console.WriteLine("Доступные команды: /start, /help, /info, /echo, /addtask, /showtasks, /removetask, /exit");

        while (isRun)                           // применение
        {
            Console.Write("\nВведите команду: ");
            string? input = Console.ReadLine();

            if (input is null)
            {
                isRun = false;
                continue;
            }

            input = input.Trim();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            // обработка
            if (input.Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                StartCommand();
            }
            else if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                isRun = false;
                Console.WriteLine($"Программа завершена. До свидания, {userName}!");
            }
            else if (input.Equals("/help", StringComparison.OrdinalIgnoreCase) ||
                     input.Equals("/info", StringComparison.OrdinalIgnoreCase))
            {
                // постоянные команды
                if (input[1] == 'h') HelpCommand();
                else InfoCommand();
            }
            else if (!isStarted)
            {
                Console.WriteLine("Сначала введите /start и укажите имя!");
            }
            else if (input.StartsWith("/echo", StringComparison.OrdinalIgnoreCase))
            {
                EchoCommand(input);
            }
            else if (input.Equals("/addtask", StringComparison.OrdinalIgnoreCase))
            {
                AddTaskCommand();
            }
            else if (input.Equals("/showtasks", StringComparison.OrdinalIgnoreCase))
            {
                ShowTasksCommand();
            }
            else if (input.Equals("/removetask", StringComparison.OrdinalIgnoreCase))
            {
                RemoveTaskCommand();
            }
            else
            {
                Console.WriteLine("Неизвестная команда. Введите /help для списка команд.");
            }
        }
    }

    // 2 миллиона отдельных методов и т.д

    static void StartCommand()
    {
        Console.Write("Укажите ваше имя: ");
        string? name = Console.ReadLine();

        if (name is null)
        {
            Console.WriteLine("Ввод отменён.");
            return;
        }

        userName = name.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            Console.WriteLine("Имя не может быть пустым!");
            return;
        }

        isStarted = true;
        Console.WriteLine($"Отлично, {userName}! Вы можете использовать команды.");
    }

    // help

    static void HelpCommand()
    {
        Console.WriteLine($"{userName}, список доступных команд:");
        Console.WriteLine("/start        — представиться боту");
        Console.WriteLine("/help         — показать эту справку");
        Console.WriteLine("/info         — информация о боте");
        Console.WriteLine("/echo <текст> — бот повторит текст");
        Console.WriteLine("/addtask      — добавить задачу");
        Console.WriteLine("/showtasks    — показать все задачи");
        Console.WriteLine("/removetask   — удалить задачу по номеру");
        Console.WriteLine("/exit         — выход из программы");
    }

    //info

    static void InfoCommand()
    {
        Console.WriteLine($"{userName}, здесь инфа по боту");
        Console.WriteLine("Версия: 1.0.2");
        Console.WriteLine("Программа создана: 17.11.2025");
        Console.WriteLine("Обновлена до актуальной версии: 05.12.2025");
    }

    // echo

    static void EchoCommand(string input)
    {
        string text = input.Length > 6 ? input.Substring(6).TrimStart() : "";
        Console.WriteLine($"{userName}, бот заметил что ты написал: {text}");
    }

    // адд таск

    static void AddTaskCommand()
    {
        Console.Write("Введите описание задачи: ");
        string? task = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(task))
        {
            Console.WriteLine("Задача не может быть пустой!");
            return;
        }

        tasks.Add(task.Trim());
        Console.WriteLine($"Задача добавлена!");
    }




    /// шоутаск 



    static void ShowTasksCommand()
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine("Список задач пуст :(");
            return;
        }

        Console.WriteLine("Ваши задачи:");
        for (int i = 0; i < tasks.Count; i++)
        {
            Console.WriteLine($"{i + i + 1}. {tasks[i]}");
        }
    }

    // римув



    static void RemoveTaskCommand()
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine("Нет задач для удаления.");
            return;
        }

        ShowTasksCommand(); // оно?

        Console.Write("Введите номер задачи для удаления: ");
        if (!int.TryParse(Console.ReadLine(), out int number) ||
            number < 1 || number > tasks.Count)
        {
            Console.WriteLine("Некорректный номер задачи.");
            return;
        }

        string removedTask = tasks[number - 1];
        tasks.RemoveAt(number - 1);
        Console.WriteLine($"Задача \"{removedTask}\" успешно удалена.");
    }
}