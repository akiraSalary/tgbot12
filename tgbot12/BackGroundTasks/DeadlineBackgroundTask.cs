
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
    public class DeadlineBackgroundTask : IBackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IToDoRepository _toDoRepository;
        private readonly TimeSpan _period;

        // запускается раз в час по умолчанию
        public DeadlineBackgroundTask(
            INotificationService notificationService,
            IUserRepository userRepository,
            IToDoRepository toDoRepository,
            TimeSpan? period = null)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _toDoRepository = toDoRepository ?? throw new ArgumentNullException(nameof(toDoRepository));
            _period = period ?? TimeSpan.FromHours(1);
        }

        public async Task Start(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var users = await _userRepository.GetUsers(ct); // требуется реализация GetUsers
                    var now = DateTime.UtcNow;

                    foreach (var user in users)
                    {
                        if (ct.IsCancellationRequested) break;

                        // берем просроченные задачи: Deadline >= from && Deadline < = to
                        var from = now.AddDays(-1); 
                        var to = now;
                     
                        IReadOnlyList<ToDoItem> tasks;
                        if (_toDoRepository is object)
                        {
                            try
                            {
                                tasks = await _toDoRepository.GetActiveWithDeadline(user.UserId, from, to, ct);
                            }
                            catch (NotImplementedException)
                            {
                                var allActive = await _toDoRepository.GetActiveByUserIdAsync(user.UserId, ct);
                                tasks = allActive.Where(t => t.Deadline.HasValue && t.Deadline.Value >= from && t.Deadline.Value <= to).ToList().AsReadOnly();
                            }
                        }
                        else
                        {
                            tasks = Array.Empty<ToDoItem>();
                        }

                        foreach (var task in tasks)
                        {
                            if (ct.IsCancellationRequested) break;

                            var type = $"Deadline_{task.Id}";
                            var text = $"⚠️ Вы пропустили дедлайн по задаче: {task.Name}";
                            await _notificationService.ScheduleNotification(user.UserId, type, text, DateTime.UtcNow, ct);
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"DeadlineBackgroundTask error: {ex}");
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