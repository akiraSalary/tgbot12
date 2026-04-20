using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Mapping;
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
    public class SqlToDoListRepository : IToDoListRepository
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public SqlToDoListRepository(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<ToDoList?> GetAsync(Guid id, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = await db.ToDoLists.FirstOrDefaultAsync(l => l.ListId == id);
            if (model == null) return null;

            var entity = ModelMapper.MapFromModel(model);
            var userModel = await db.ToDoUsers.FirstOrDefaultAsync(u => u.UserId == model.UserId);
            if (userModel != null)
                entity.User = ModelMapper.MapFromModel(userModel);
            return entity;
        }

        public async Task<IReadOnlyList<ToDoList>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();

            var models = db.ToDoLists.Where(l => l.UserId == userId).ToList();

            var userModel = await db.ToDoUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            var user = userModel != null ? ModelMapper.MapFromModel(userModel) : null;

            var lists = models.Select(m =>
            {
                var l = ModelMapper.MapFromModel(m);
                if (user != null) l.User = user;
                return l;
            }).ToList();

            return lists.AsReadOnly();
        }

        public async Task AddAsync(ToDoList list, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var exists = await db.ToDoLists.AnyAsync(l => l.UserId == list.User.UserId && l.Name == list.Name);
            if (exists) throw new InvalidOperationException("Список с таким именем уже существует");

            var model = ModelMapper.MapToModel(list);
            await db.InsertAsync(model);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            await db.ToDoLists.DeleteAsync(l => l.ListId == id);
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            return await db.ToDoLists.AnyAsync(l => l.UserId == userId && l.Name == name);
        }
    }
}