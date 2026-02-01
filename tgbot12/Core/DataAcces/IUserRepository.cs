using System;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.DataAccess
{
    public interface IUserRepository
    {
        Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct = default);
        Task<ToDoUser?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default);
        Task AddAsync(ToDoUser user, CancellationToken ct = default);
    }
}