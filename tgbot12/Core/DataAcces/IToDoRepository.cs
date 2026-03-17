using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.DataAccess
{
    public interface IToDoRepository
    {
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<ToDoItem?> GetAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(ToDoItem item, CancellationToken ct = default);
        Task UpdateAsync(ToDoItem item, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct = default);
        Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct = default);
    }
}