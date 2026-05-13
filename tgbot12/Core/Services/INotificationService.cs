using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.Services
{
    public interface INotificationService
    {
        // Создает нотиф
        Task<bool> ScheduleNotification(Guid userId, string type, string text, DateTime scheduledAt, CancellationToken ct = default);

        // Возвращает нотиф у которых IsNotified == false && ScheduledAt <= scheduledBefore 
        Task<IReadOnlyList<Notification>> GetScheduledNotification(DateTime scheduledBefore, CancellationToken ct = default);

        Task MarkNotified(Guid notificationId, CancellationToken ct = default);
    }
}