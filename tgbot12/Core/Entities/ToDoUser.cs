using System;

namespace ToDoListBot.Core.Entities
{

    public class ToDoUser
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public long TelegramUserId { get; set; }
        public string TelegramUserName { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public ToDoUser(long telegramUserId, string telegramUserName)
        {
            TelegramUserId = telegramUserId;
            TelegramUserName = telegramUserName ?? "Unknown";
        }
    }
}
