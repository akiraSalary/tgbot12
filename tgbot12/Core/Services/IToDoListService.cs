using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.Services
{
    public interface IToDoListService
    {
        Task<ToDoList> AddAsync(ToDoUser user, string name, CancellationToken ct = default);
        Task<ToDoList?> GetAsync(Guid id, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<ToDoList>> GetUserListsAsync(Guid userId, CancellationToken ct = default);
    }
}