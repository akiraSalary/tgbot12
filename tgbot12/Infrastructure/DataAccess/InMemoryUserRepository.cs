using System;
using System.Collections.Generic;
using System.Linq;

using ToDoListBot.Core.Entities;          //  ToDoUser, ToDoItem
using ToDoListBot.Core.DataAccess;        //  IUserRepository, IToDoRepository

namespace ToDoListBot.Infrastructure.DataAccess
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<ToDoUser> _users = new();
        public ToDoUser? GetUser(Guid userId) =>
            _users.FirstOrDefault(u => u.UserId == userId);
        public ToDoUser? GetByTelegramUserId(long telegramUserId) =>
            _users.FirstOrDefault(u => u.TelegramUserId == telegramUserId);
        public void Add(ToDoUser user)
        {
            if (GetByTelegramUserId(user.TelegramUserId) != null) return;
            _users.Add(user);
        }
    }
}