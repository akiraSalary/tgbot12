public interface IUserRepository
{
    ToDoUser? GetUser(Guid userId);
    ToDoUser? GetByTelegramUserId(long telegramUserId);
    void Add(ToDoUser user);
}