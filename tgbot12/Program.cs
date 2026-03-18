using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Services;
using ToDoListBot.Infrastructure.DataAccess;
using ToDoListBot.TelegramBot;
using ToDoListBot.TelegramBot.Scenarios;

namespace ToDoListBot
{
    internal class Program
    {
        private static ITelegramBotClient _botClient = null!;

        static async Task Main(string[] args)
        {
            Console.Title = "ToDo Telegram Bot";

            // token

            string? token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Переменная окружения TELEGRAM_BOT_TOKEN не установлена.");
                Console.Write("Введите токен бота (только для локального теста, не сохраняется): ");
                token = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine("Токен не указан. Выход.");
                    return;
                }
            }

            _botClient = new TelegramBotClient(token);

         
            var me = await _botClient.GetMe();
            Console.WriteLine($"Бот запущен: @{me.Username} ({me.FirstName})");
            await SetMyCommandsAsync();
            var handler = CreateUpdateHandler();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>(), 
                DropPendingUpdates = true 
            };

            using var cts = new CancellationTokenSource();

            _botClient.StartReceiving(
                updateHandler: handler.HandleUpdateAsync,
                errorHandler: handler.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token);

            Console.WriteLine("Нажмите клавишу A для выхода...");

            
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (char.ToLowerInvariant(key.KeyChar) == 'a')
                    {
                        cts.Cancel();
                        Console.WriteLine("Бот остановлен пользователем (A).");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"Бот: @{me.Username}");
                        Console.WriteLine($"ID: {me.Id}");
                        Console.WriteLine($"Имя: {me.FirstName}");
                        Console.WriteLine($"Можно писать в Telegram: @{me.Username}");
                        Console.WriteLine("Нажмите A для выхода...");
                    }
                }

                await Task.Delay(100, cts.Token);
            }

            Console.WriteLine("Программа завершена.");
            await Task.Delay(1500);
        }

        private static UpdateHandler CreateUpdateHandler()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;

            string projectRoot = Directory.GetParent(exeDir)?.Parent?.Parent?.FullName
                ?? exeDir;

            // location
            string dataDir = Path.Combine(projectRoot, "data");
            string usersPath = Path.Combine(dataDir, "users");
            string tasksPath = Path.Combine(dataDir, "tasks");

            // folders if not
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(usersPath);
            Directory.CreateDirectory(tasksPath);

            // debug
            Console.WriteLine($"Папка данных: {dataDir}");
            Console.WriteLine($"Пользователи: {usersPath}");
            Console.WriteLine($"Задачи: {tasksPath}");

            // repos
            var userRepo = new FileUserRepository(usersPath);
            var todoRepo = new FileToDoRepository(tasksPath);

            var toDoListRepo = new FileToDoListRepository(tasksPath);
            var toDoListService = new ToDoListService(toDoListRepo);

            // servs
            var userService = new UserService(userRepo);
            var todoService = new ToDoService(todoRepo, maxTaskCount: 10, maxTaskLength: 100);
            var reportService = new ToDoReportService(todoRepo);

            var scenarios = new IScenario[]
            {
                new AddTaskScenario(userService, todoService, toDoListService),
                new AddListScenario(userService, toDoListService),
                new AddTaskToListScenario(userService, todoService),
                new DeleteListScenario(toDoListService),
            };

            var contextRepository = new InMemoryScenarioContextRepository();

            return new UpdateHandler(
                userService,
                todoService,
                reportService,
                10,
                100,
                _botClient,
                scenarios,
                contextRepository,
                toDoListService
            );
        }

        private static async Task SetMyCommandsAsync()
        {
            var commands = new[]
            {
                new BotCommand { Command = "start",   Description = "Начать работу / зарегистрироваться" },
                new BotCommand { Command = "help",    Description = "Список команд и справка" },
                new BotCommand { Command = "info",    Description = "Информация о пользователе и лимитах" },
                new BotCommand { Command = "addtask", Description = "Добавить новую задачу" },
                new BotCommand { Command = "show",   Description = "Показать только активные задачи" },
                new BotCommand { Command = "completetask", Description = "Завершить задачу по ID" },
                new BotCommand { Command = "removetask",   Description = "Удалить задачу по ID" },
                new BotCommand { Command = "report",  Description = "Статистика по задачам" },
                new BotCommand { Command = "find",    Description = "Поиск активных задач по префиксу" }
            };

            await _botClient.SetMyCommands(commands);
        }
    }
}