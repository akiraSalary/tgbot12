using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

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

            var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Не задана переменная окружения TELEGRAM_BOT_TOKEN. Установите токен бота и перезапустите приложение.");
                Console.ResetColor();
                return;
            }

            var botClient = new TelegramBotClient(token);

            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nОстановка бота...");
            };

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }, 
                DropPendingUpdates = true
            };

            botClient.StartReceiving(
                handler.HandleUpdateAsync,
                handler.HandleErrorAsync,
                receiverOptions,
                cts.Token);

            Console.WriteLine("Бот запущен. Нажмите клавишу A для выхода (любая другая — показать информацию о боте)...");

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.A)
                    {
                        cts.Cancel();
                        Console.WriteLine("Остановка по нажатию A...");
                        break;
                    }

                    try
                    {
                        var me = await botClient.GetMe();
                        Console.WriteLine($"Бот: @{me.Username} (Id: {me.Id}, Name: {me.FirstName} {me.LastName})");
                    }
                    catch (OperationCanceledException)
                    {
                        
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Не удалось получить информацию о боте: {ex.Message}");
                        Console.ResetColor();
                    }
                }

                await Task.Delay(500, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Бот остановлен пользователем (Ctrl+C или A).");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Критическая ошибка в Main: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("Программа завершена.");
        }
    }
}