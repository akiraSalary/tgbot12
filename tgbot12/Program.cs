using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string userName = "";
        bool isStarted = false;

        List<string> tasks = new List<string>(); // не уверен что он должен быть тут, надеюсь угадал

        Console.WriteLine("Это тест менюха тг бота!");
        Console.WriteLine("Доступные команды: /start, /help, /info, /echo, /exit, /addtask, /showtasks, /removetask");

        while (true)
        {
            Console.Write("\nВведите команду: ");
            string? input = Console.ReadLine();

            if (input is null)       // нулл фикс i guess?
                break;

            input = input.Trim();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            // - - а тут /start - -
            if (input == "/start")
            {
                Console.Write("Укажите ваше имя:");
                string? name = Console.ReadLine();

                if (name is null)
                {
                    Console.WriteLine("Ввод отменен.");   // нулл фикс i guess? //ниче лучше я не придумал :3
                    continue;
                }

                userName = name.Trim();
                if (string.IsNullOrWhiteSpace(userName))
                {
                    Console.WriteLine("Имя не может быть пустым!");
                    continue;
                }


                isStarted = true;

                Console.WriteLine($"Отлично, {userName}! Вы можете использовать команды.");
                continue;
            }

            // - - тут /exit - -
            if (input == "/exit")
            {
                Console.WriteLine($"Программа завершена. До свидания, {userName}!");
                break;
            }

            // если команда требует имя но оно не указано отсылаем на старт
            if (!isStarted && input != "/help" && input != "/info")
            {
                Console.WriteLine("Сначала введите /start, и укажите имя!");
                continue;
            }

            // - здесь /help -
            if (input == "/help")
            {
                Console.WriteLine($"{userName},ознакомьтесь со писоком команд:");
                Console.WriteLine("/start — представиться боту");
                Console.WriteLine("/help — список доступных команд");
                Console.WriteLine("/info — информация о боте");
                Console.WriteLine("/echo <текст> — повторение написанного текста");
                Console.WriteLine("/exit — завершение программы");
                Console.WriteLine("/addtask — добавить новую задачу");
                Console.WriteLine("/showtasks — показать все задачи");
                Console.WriteLine("/removetask — удалить задачу по номеру");
                continue;
            }

            // - это /info -
            if (input == "/info")
            {
                Console.WriteLine($"{userName}, здесь инфа по боту");
                Console.WriteLine("Версия 1.0.1");                                     // поменять систему даты на автомат
                Console.WriteLine("Программа создана 17.11.2025");
                Console.WriteLine("Обновленная до актуальной версии 4.12.2025");
                continue;
            }

            // - /echo -
            if (input.StartsWith("/echo"))
            {
                string text = input.Substring(6); // проверка по 6 символу 
                Console.WriteLine($"{userName},бот заметил что вы написали: {text}");
                continue;
            }
            // ---- addtask XD ---
            if (input == "/addtask")
            {
                Console.Write("Введите описание задачи: ");
                string? taskText = Console.ReadLine(); // tyt toje bil null 

                if (string.IsNullOrWhiteSpace(taskText))
                {
                    Console.WriteLine("Пустое место не добавить, укажите описание задачи.");
                    continue;
                }

                tasks.Add(taskText);
                Console.WriteLine($"Задача была добавлена.");              // есть возможность накрутить taskCount 
                continue;
            }

            // ------ /шоутаскс (showtasks) ----

            if (input.Equals("/showtasks", StringComparison.OrdinalIgnoreCase))
            {
                if (tasks.Count == 0)
                {
                    Console.WriteLine("Список задач пуст :(");
                }

                else
                {
                    Console.WriteLine("Список задач:");
                    for (int i = 0; i < tasks.Count; i++)
                        Console.WriteLine($"{i + 1}. {tasks[i]}");
                }
                continue;
            }


            // -- remove -- 

            if (input == "/removetask")
            {
                if (tasks.Count == 0)
                {
                    Console.WriteLine("Нельзя удалить эту задачу :), в списке задач еще ничего нету. ");
                    continue;

                }

                Console.WriteLine("Список задач:");
                for (int i = 0; i < tasks.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {tasks[i]}");
                }

                Console.Write("Введите номер задачи для удаления:");
                string? numberInput = Console.ReadLine();
                int taskNumber;

                if (!int.TryParse(numberInput, out taskNumber))
                {
                    Console.WriteLine("Введен некоректный номер задачи. Попробуте еще.");  // некоректка для комбинаций НЕ чисел)
                    continue;
                }

                if (taskNumber < 1 || taskNumber > tasks.Count)
                {
                    Console.WriteLine("Такого номера в листе нету. Введите корректный.");  // тут понятно в целом
                    continue;
                }

                // --- aaaa index -----
                string removeTask = tasks[taskNumber - 1];     // для удаления
                tasks.RemoveAt(taskNumber - 1);
                Console.WriteLine($"Задача \"{removeTask}\" удалена.");
                continue;
            }


            // не думаю что нужно но добавлю всеравно 
            Console.WriteLine("Этой команды не существует.Озакомьтесь со списком доступных команд в /help.");
        }
    }
}
