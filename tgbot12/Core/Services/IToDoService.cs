using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.Services
{
    public interface IToDoService
    {
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<ToDoItem> AddTaskAsync(ToDoUser user, string name, CancellationToken ct = default);
        Task MarkCompletedAsync(Guid taskId, CancellationToken ct = default);
        Task DeleteAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken ct = default);
        Task UpdateTaskAsync (ToDoItem item, CancellationToken ct = default);
        Task<ToDoItem> AddTaskAsync(ToDoUser user, string name, Guid? listId = null, CancellationToken ct = default);
        Task<IReadOnlyList<ToDoItem>> GetByListIdAsync(Guid listId, CancellationToken ct = default);
    }
}