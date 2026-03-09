using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.DataAccess
{
    public interface IToDoListRepository
    {
        Task<ToDoList?> GetAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<ToDoList>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task AddAsync(ToDoList list, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct = default);
    }
}