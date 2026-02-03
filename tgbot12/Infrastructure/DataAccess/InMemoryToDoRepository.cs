using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Infrastructure.DataAccess
{
    public class InMemoryToDoRepository : IToDoRepository
    {
        private readonly List<ToDoItem> _items = new();

        public Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ToDoItem>>(
                _items.Where(i => i.User.UserId == userId).ToList().AsReadOnly());
        }

        public Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ToDoItem>>(
                _items.Where(i => i.User.UserId == userId && i.State == ToDoItemState.Active)
                      .ToList().AsReadOnly());
        }

        public Task<ToDoItem?> GetAsync(Guid id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_items.FirstOrDefault(i => i.Id == id));
        }

        public Task AddAsync(ToDoItem item, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ToDoItem item, CancellationToken ct = default)
        {
            // В памяти ничего не нужно делать, объект уже изменён
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var item = GetAsync(id, ct).GetAwaiter().GetResult();
            if (item != null) _items.Remove(item);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(
                _items.Any(i => i.User.UserId == userId &&
                                i.State == ToDoItemState.Active &&
                                string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(GetActiveByUserIdAsync(userId, ct).GetAwaiter().GetResult().Count);
        }

        public Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ToDoItem>>(
                _items.Where(i => i.User.UserId == userId && predicate(i))
                      .ToList().AsReadOnly());
        }
    }
}