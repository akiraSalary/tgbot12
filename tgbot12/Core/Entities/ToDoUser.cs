
public class ToDoUser
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public long TelegramUserId { get; init; }
    public string TelegramUserName { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
    public ToDoUser(long telegramUserId, string telegramUserName)
    {
        TelegramUserId = telegramUserId;
        TelegramUserName = telegramUserName ?? "Unknown";
    }
}