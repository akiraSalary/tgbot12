using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Infrastructure.DataAccess
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<ToDoUser> _users = new();

        public Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_users.FirstOrDefault(u => u.UserId == userId));
        }

        public Task<ToDoUser?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_users.FirstOrDefault(u => u.TelegramUserId == telegramUserId));
        }

        public Task AddAsync(ToDoUser user, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (GetByTelegramUserIdAsync(user.TelegramUserId, ct).GetAwaiter().GetResult() != null)
                return Task.CompletedTask;

            _users.Add(user);
            return Task.CompletedTask;
        }
    }
}