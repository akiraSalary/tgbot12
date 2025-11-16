using System;

class Program
{
    static void Main(string[] args)
    {
        string userName = "";
        bool isStarted = false;

        Console.WriteLine("Это тест менюха тг бота!");
        Console.WriteLine("Доступные команды: /start, /help, /info, /echo, /exit");
        
        while (true)
        {
            Console.Write("\nВведите команду: ");
            string input = Console.ReadLine();



            // - - а тут /start - -
            if (input == "/start")
            {
                Console.Write("Укажите ваше имя:");
                userName = Console.ReadLine();
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
                continue;
            }

            // - это /info -
            if (input == "/info")
            {
                Console.WriteLine($"{userName}, здесь инфа по боту");
                Console.WriteLine("Версия 1.0.0");
                Console.WriteLine("Программа создана 17.11.2025");
                continue;
            }

            // - /echo -
            if (input.StartsWith("/echo"))
            {
                string text = input.Substring(6); // проверка по 6 символу 
                Console.WriteLine($"{userName},бот заметил что вы написали: {text}");
                continue;
            }

            // не думаю что нужно но добавлю всеравно 
            Console.WriteLine("Этой команды не существует.Озакомьтесь со списком доступных команд в /help.");
        }
    }
}