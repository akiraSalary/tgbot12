using LinqToDB;
using LinqToDB.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAcces.Models;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Infrastructure.DataAccess
{
    public class SqlUserRepository : IUserRepository
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public SqlUserRepository(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = await db.ToDoUsers.FirstOrDefaultAsync(u => u.UserId == userId, ct);
            return model == null ? null : ModelMapper.MapFromModel(model);
        }

        public async Task<ToDoUser?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = await db.ToDoUsers.FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);
            return model == null ? null : ModelMapper.MapFromModel(model);
        }

        public async Task AddAsync(ToDoUser user, CancellationToken ct = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            using var db = _factory.CreateDataContext();

            var existing = await db.ToDoUsers.FirstOrDefaultAsync(u => u.TelegramUserId == user.TelegramUserId, ct);
            if (existing != null) return;

            var model = ModelMapper.MapToModel(user);
            await db.InsertAsync(model);
        }

        public async Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.ToDoUsers.ToListAsync(ct);
            var users = models.Select(ModelMapper.MapFromModel).ToList();
            return users.AsReadOnly();
        }
    }
}