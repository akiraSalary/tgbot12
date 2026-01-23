using System;

using ToDoListBot.Core.DataAccess;        // репозитории
using ToDoListBot.Core.Entities;          // ToDoUser, ToDoItem

namespace ToDoListBot.Core.Services
{
    public interface IToDoReportService
    {
        (int total, int completed, int active, DateTime generatedAt) GetUserStats(Guid userId);
    }
}
