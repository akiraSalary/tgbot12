using System;

using ToDoListBot.Core.DataAccess;        // репозитории
using ToDoListBot.Core.Entities;          // ToDoUser, ToDoItem

namespace ToDoListBot.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
        {
            var existing = _repository.GetByTelegramUserId(telegramUserId);
            if (existing != null) return existing;

            var user = new ToDoUser(telegramUserId, telegramUserName);
            _repository.Add(user);
            return user;
        }

        public ToDoUser? GetUser(long telegramUserId) =>
            _repository.GetByTelegramUserId(telegramUserId);
    }
}
