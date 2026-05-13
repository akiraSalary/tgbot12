using System;

namespace ToDoListBot.Core.Entities
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ToDoUser User { get; set; } = null!;

        // type
        public string Type { get; set; } = string.Empty;

        // Текст нотификации
        public string Text { get; set; } = string.Empty;

        // Запланированная дата
        public DateTime ScheduledAt { get; set; }

        // Флаг отправки
        public bool IsNotified { get; set; } = false;

        // Фактическая дата отправки
        public DateTime? NotifiedAt { get; set; }
    }
}