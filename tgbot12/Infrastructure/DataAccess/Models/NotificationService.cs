using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.Infrastructure.DataAccess.Models;

namespace ToDoListBot.Infrastructure
{
  
    public class NotificationService : INotificationService
    {
        private readonly Func<DataConnection> _createConnection;

       
        public NotificationService(Func<DataConnection> createConnection)
        {
            _createConnection = createConnection;
        }

        public async Task<bool> ScheduleNotification(Guid userId, string type, string text, DateTime scheduledAt, CancellationToken ct = default)
        {
            using var db = _createConnection();
            // проверка наличия такой записи (неважно scheduledAt)
            var exists = await db.GetTable<NotificationModel>()
                                 .Where(n => n.UserId == userId && n.Type == type)
                                 .AnyAsync(ct);

            if (exists)
                return false;

            var model = new NotificationModel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Text = text,
                ScheduledAt = scheduledAt,
                IsNotified = false,
                NotifiedAt = null
            };

            await db.InsertAsync(model);
            return true;
        }

        public async Task<IReadOnlyList<Notification>> GetScheduledNotification(DateTime scheduledBefore, CancellationToken ct = default)
        {
            using var db = _createConnection();

            var rows = await db.GetTable<NotificationModel>()
                               .Where(n => !n.IsNotified && n.ScheduledAt <= scheduledBefore)
                               .ToListAsync(ct);

            var result = rows.Select(r => new Notification
            {
                Id = r.Id,
                User = new Core.Entities.ToDoUser(0, string.Empty) { UserId = r.UserId }, 
                Type = r.Type,
                Text = r.Text,
                ScheduledAt = r.ScheduledAt,
                IsNotified = r.IsNotified,
                NotifiedAt = r.NotifiedAt
            }).ToList().AsReadOnly();

            return result;
        }

        public async Task MarkNotified(Guid notificationId, CancellationToken ct = default)
        {
            using var db = _createConnection();

            await db.GetTable<NotificationModel>()
                    .Where(n => n.Id == notificationId)
                    .Set(n => n.IsNotified, true)
                    .Set(n => n.NotifiedAt, DateTime.UtcNow)
                    .UpdateAsync(ct);
        }
    }
}
