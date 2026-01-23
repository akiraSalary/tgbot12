using System;

using ToDoListBot.Core.DataAccess;        // репозитории
using ToDoListBot.Core.Entities;          // ToDoUser, ToDoItem

namespace ToDoListBot.Core.Services
{
    public interface IToDoService
    {
        IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);
        IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);
        ToDoItem AddTask(ToDoUser user, string name);
        void MarkCompleted(Guid taskId);
        void Delete(Guid taskId);
        IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix);
    }
}
