using System;
using System.Collections.Generic;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.DataAccess
{
    public interface IUserRepository
    {
        ToDoUser? GetUser(Guid userId);
        ToDoUser? GetByTelegramUserId(long telegramUserId);
        void Add(ToDoUser user);
    }
}