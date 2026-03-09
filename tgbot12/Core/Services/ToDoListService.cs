using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.Services
{
    public class ToDoListService : IToDoListService
    {
        private readonly IToDoListRepository _repository;

        public ToDoListService(IToDoListRepository repository)
        {
            _repository = repository;
        }

        public async Task<ToDoList> AddAsync(ToDoUser user, string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 10)
                throw new ArgumentException("Название списка должно быть от 1 до 10 символов");

            if (await _repository.ExistsByNameAsync(user.UserId, name, ct))
                throw new InvalidOperationException("Список с таким именем уже существует");

            var list = new ToDoList(user, name);
            await _repository.AddAsync(list, ct);
            return list;
        }

        public Task<ToDoList?> GetAsync(Guid id, CancellationToken ct = default)
            => _repository.GetAsync(id, ct);

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
            => _repository.DeleteAsync(id, ct);

        public Task<IReadOnlyList<ToDoList>> GetUserListsAsync(Guid userId, CancellationToken ct = default)
            => _repository.GetByUserIdAsync(userId, ct);
    }
}