using System;

using ToDoListBot.Core.DataAccess;        // репозитории
using ToDoListBot.Core.Entities;          // ToDoUser, ToDoItem

namespace ToDoListBot.Core.Services
{
    public interface IUserService
    {
        ToDoUser RegisterUser(long telegramUserId, string telegramUserName);
        ToDoUser? GetUser(long telegramUserId);
    }
}
