
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ToDoListBot.Core.Services;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.BackgroundTasks
{
    public class TodayBackgroundTask : IBackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IToDoRepository _toDoRepository;
        private readonly TimeSpan _period;

        // запускается раз в день по умолчанию
        public TodayBackgroundTask(
            INotificationService notificationService,
            IUserRepository userRepository,
            IToDoRepository toDoRepository,
            TimeSpan? period = null)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _toDoRepository = toDoRepository ?? throw new ArgumentNullException(nameof(toDoRepository));
            _period = period ?? TimeSpan.FromDays(1);
        }

        public async Task Start(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var users = await _userRepository.GetUsers(ct);
                    var todayUtc = DateTime.UtcNow.Date;

                    foreach (var user in users)
                    {
                        if (ct.IsCancellationRequested) break;

                        var allActive = await _toDoRepository.GetActiveByUserIdAsync(user.UserId, ct);
                        var todays = allActive.Where(t => t.Deadline.HasValue && t.Deadline.Value.Date == todayUtc).ToList();

                        foreach (var task in todays)
                        {
                            if (ct.IsCancellationRequested) break;

                            var type = $"Today_{task.Id}";
                            var text = $"📅 Сегодня запланировано: {task.Name}";
                            await _notificationService.ScheduleNotification(user.UserId, type, text, DateTime.UtcNow, ct);
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"TodayBackgroundTask error: {ex}");
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