using System;
using System.Threading;
using System.Threading.Tasks;

using Otus.ToDoList.ConsoleBot;

using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Services;
using ToDoListBot.Infrastructure.DataAccess;
using ToDoListBot.TelegramBot;

namespace ToDoListBot
{
    internal class Program
    {
        static async Task Main(string[] args)
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

            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();s
            };

            Console.WriteLine("Бот запущен. Вводите сообщения как в Telegram... (Ctrl+C для выхода)");

            botClient.StartReceiving(handler, cts.Token);

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                await handler.HandleErrorAsync(botClient, ex, CancellationToken.None);
                Console.WriteLine("Бот остановлен по отмене (Ctrl+C).");
            }

            Console.WriteLine("Бот остановлен.");
        }                     //test 12
    }
}