
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Telegram.Bot;
using ToDoListBot.Core.Services;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.DataAccess;

namespace ToDoListBot.BackgroundTasks
{
    public class NotificationBackgroundTask : IBackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly ITelegramBotClient _bot;
        private readonly IUserRepository _userRepository;
        private readonly TimeSpan _period;

        public NotificationBackgroundTask(
            INotificationService notificationService,
            ITelegramBotClient bot,
            IUserRepository userRepository,
            TimeSpan? period = null) // по умолчанию раз в минуту
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _period = period ?? TimeSpan.FromMinutes(1);
        }

        public async Task Start(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var notifications = await _notificationService.GetScheduledNotification(now, ct);

                    foreach (var n in notifications)
                    {
                        if (ct.IsCancellationRequested) break;

                        // Получаем пользователя
                        var user = await _userRepository.GetUserAsync(n.User.UserId, ct);
                        if (user == null) continue;

                        try
                        {
                            await _bot.SendMessage(user.TelegramUserId, n.Text, cancellationToken: ct);
                        }
                        catch
                        {
                            // логирование/игнорирование ошибок отправки
                        }

                        await _notificationService.MarkNotified(n.Id, ct);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"NotificationBackgroundTask error: {ex}");
                }

                try
                {
                    await Task.Delay(_period, ct);
                }
                catch (OperationCanceledException) { }
            }
        }
    }
}