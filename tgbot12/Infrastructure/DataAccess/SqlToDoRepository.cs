using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Async;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.DataAcces.Models;

namespace ToDoListBot.Infrastructure.DataAccess
{
    public class SqlToDoRepository : IToDoRepository
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public SqlToDoRepository(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.ToDoItems.Where(i => i.UserId == userId).ToListAsync(ct);

            var userModel = await db.ToDoUsers.FirstOrDefaultAsync(u => u.UserId == userId, ct);
            ToDoUser? user = userModel != null ? ModelMapper.MapFromModel(userModel) : null;

            var items = models.Select(m =>
            {
                var item = ModelMapper.MapFromModel(m);
                if (user != null) item.User = user;
                return item;
            }).ToList();

            return items.AsReadOnly();
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.ToDoItems.Where(i => i.UserId == userId && i.State == 0).ToListAsync(ct);
            var userModel = await db.ToDoUsers.FirstOrDefaultAsync(u => u.UserId == userId, ct);
            ToDoUser? user = userModel != null ? ModelMapper.MapFromModel(userModel) : null;

            var items = models.Select(m =>
            {
                var item = ModelMapper.MapFromModel(m);
                if (user != null) item.User = user;
                return item;
            }).ToList();

            return items.AsReadOnly();
        }

        public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = await db.ToDoItems.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (model == null) return null;

            var item = ModelMapper.MapFromModel(model);
            var userModel = await db.ToDoUsers.FirstOrDefaultAsync(u => u.UserId == model.UserId, ct);
            if (userModel != null) item.User = ModelMapper.MapFromModel(userModel);
            return item;
        }

        public async Task AddAsync(ToDoItem item, CancellationToken ct = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item.User == null) throw new ArgumentException("Item.User must not be null", nameof(item));

            using var db = _factory.CreateDataContext();
            var model = ModelMapper.MapToModel(item);
            await db.InsertAsync(model);
        }

        public async Task UpdateAsync(ToDoItem item, CancellationToken ct = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            using var db = _factory.CreateDataContext();
            var model = ModelMapper.MapToModel(item);
            await db.UpdateAsync(model);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            await db.ToDoItems.DeleteAsync(i => i.Id == id, ct);
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            return await db.ToDoItems.AnyAsync(i => i.UserId == userId && i.Name == name, ct);
        }

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            return await db.ToDoItems.CountAsync(i => i.UserId == userId && i.State == 0, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct = default)
        {
            var all = await GetAllByUserIdAsync(userId, ct);
            var result = all.Where(predicate).ToList();
            return result.AsReadOnly();
        }
        public async Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.ToDoItems
                .Where(i => i.UserId == userId && i.State == 0 && i.Deadline != null && i.Deadline >= from && i.Deadline <= to)
                .ToListAsync(ct);

            var userModel = await db.ToDoUsers.FirstOrDefaultAsync(u => u.UserId == userId, ct);
            ToDoUser? user = userModel != null ? ModelMapper.MapFromModel(userModel) : null;

            var items = models.Select(m =>
            {
                var item = ModelMapper.MapFromModel(m);
                if (user != null) item.User = user;
                return item;
            }).ToList();

            return items.AsReadOnly();
        }

        public Task<ToDoItem?> GetToDoItemAsync(Guid toDoItemId, CancellationToken ct = default) => GetAsync(toDoItemId, ct);
        public Task<ToDoItem?> GetByIdAsync(Guid id, CancellationToken ct = default) => GetAsync(id, ct);
        public Task<IReadOnlyList<ToDoItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) => GetAllByUserIdAsync(userId, ct);

        public async Task<IReadOnlyList<ToDoItem>> GetByListIdAsync(Guid listId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.ToDoItems.Where(i => i.ListId == listId).ToListAsync(ct);

            var userIds = models.Select(m => m.UserId).Distinct().ToList();
            var userModels = await db.ToDoUsers.Where(u => userIds.Contains(u.UserId)).ToListAsync(ct);
            var userDict = userModels.ToDictionary(u => u.UserId, u => ModelMapper.MapFromModel(u));

            var items = models.Select(m =>
            {
                var item = ModelMapper.MapFromModel(m);
                if (userDict.TryGetValue(m.UserId, out var u)) item.User = u;
                return item;
            }).ToList();

            return items.AsReadOnly();
        }
    }
}