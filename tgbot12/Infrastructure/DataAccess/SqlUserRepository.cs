


using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.DataAcces.Models;

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

            var exists = await db.ToDoUsers.AnyAsync(u => u.TelegramUserId == user.TelegramUserId, ct);
            if (exists) return;

            var model = ModelMapper.MapToModel(user);
            await db.InsertAsync(model, ct);
        }
    }
}