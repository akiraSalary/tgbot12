
using System;
using LinqToDB.Mapping;

namespace ToDoListBot.Infrastructure.DataAccess.Models
{
    // Используем имя в нижнем регистре — это соответствует привычной PostgreSQL-таблице без кавычек
    [Table(Name = "notifications")]
    public class NotificationModel
    {
        [PrimaryKey, Column(Name = "id")]
        public Guid Id { get; set; }

        [Column(Name = "user_id"), NotNull]
        public Guid UserId { get; set; }

        [Column(Name = "type"), NotNull]
        public string Type { get; set; } = string.Empty;

        [Column(Name = "text"), NotNull]
        public string Text { get; set; } = string.Empty;

        [Column(Name = "scheduled_at"), NotNull]
        public DateTime ScheduledAt { get; set; }

        [Column(Name = "is_notified"), NotNull]
        public bool IsNotified { get; set; }

        [Column(Name = "notified_at")]
        public DateTime? NotifiedAt { get; set; }
    }
}