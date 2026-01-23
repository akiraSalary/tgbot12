using System;


using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;


using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Services;
using ToDoListBot.Infrastructure.DataAccess;
using ToDoListBot.TelegramBot;

namespace ToDoListBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "бот еххехехеехехехехехеехеехеххехехехеех";

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