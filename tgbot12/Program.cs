using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Services;
using ToDoListBot.Infrastructure.DataAccess;
using ToDoListBot.TelegramBot;

namespace ToDoListBot
{
    internal class Program
    {
        private static ITelegramBotClient _botClient = null!;

        static async Task Main(string[] args)
        {
            Console.Title = "ToDo Telegram Bot";

            
            string token = "8445760730:AAGTA5DDDUJLJpawGb0gf6P4GkKQqt0n4D8"; 

            _botClient = new TelegramBotClient(token);

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Бот запущен: @{me.Username} ({me.FirstName})");

            
            await SetBotCommandsAsync();

            var handler = CreateUpdateHandler();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message },
                DropPendingUpdates = true 
            };

            using var cts = new CancellationTokenSource();

            _botClient.StartReceiving(
                updateHandler: handler.HandleUpdateAsync,
                pollingErrorHandler: handler.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            Console.WriteLine("Наммите клавишу A для выхода...");

            
            var key = Console.ReadKey(true);

            if (char.ToLowerInvariant(key.KeyChar) == 'a')
            {
                Console.WriteLine("Остановка бота...");
                cts.Cancel();
            }
            else
            {
                
                Console.WriteLine($"Бот: @{me.Username}, ID: {me.Id}, Имя: {me.FirstName}");
                Console.WriteLine("Нажмите A для выхода...");
                Console.ReadKey(true); 
                cts.Cancel();
            }

            
            await Task.Delay(1500);
            Console.WriteLine("Программа завершена.");
        }

        private static IUpdateHandler CreateUpdateHandler()
        {
           
            var userRepo = new InMemoryUserRepository();
            var todoRepo = new InMemoryToDoRepository();

            var userService = new UserService(userRepo);
            var todoService = new ToDoService(todoRepo, 10, 100);
            var reportService = new ToDoReportService(todoRepo);

            return new UpdateHandler(
                userService,
                todoService,
                reportService,
                10,
                100,
                _botClient 
            );
        }

        private static async Task SetBotCommandsAsync()
        {
            var commands = new[]
            {
                new BotCommand { Command = "start", Description = "Начать работу / зарегистрироваться" },
                new BotCommand { Command = "help", Description = "Список команд" },
                new BotCommand { Command = "info", Description = "Информация о пользователе и лимитах" },
                new BotCommand { Command = "addtask", Description = "Добавить задачу" },
                new BotCommand { Command = "showtasks", Description = "Показать активные задачи" },
                new BotCommand { Command = "showalltasks", Description = "Показать все задачи" },
                new BotCommand { Command = "completetask", Description = "Завершить задачу" },
                new BotCommand { Command = "removetask", Description = "Удалить задачу" },
                new BotCommand { Command = "report", Description = "Статистика по задачам" },
                new BotCommand { Command = "find", Description = "Поиск задач по префиксу" }
            };

            await _botClient.SetMyCommandsAsync(commands);
        }
    }
}