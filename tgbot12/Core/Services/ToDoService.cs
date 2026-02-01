using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Exceptions;

namespace ToDoListBot.Core.Services
{
    public class ToDoService : IToDoService
    {
        private readonly IToDoRepository _repository;
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        public ToDoService(IToDoRepository repository, int maxTaskCount = 10, int maxTaskLength = 100)
        {
            _repository = repository;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
        }

        public Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
            => _repository.GetAllByUserIdAsync(userId, ct);

        public Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
            => _repository.GetActiveByUserIdAsync(userId, ct);

        public async Task<ToDoItem> AddTaskAsync(ToDoUser user, string name, CancellationToken ct = default)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название задачи не может быть пустым");

            if (name.Length > _maxTaskLength)
                throw new TaskLengthLimitException(name.Length, _maxTaskLength);

            var activeCount = await _repository.CountActiveAsync(user.UserId, ct);
            if (activeCount >= _maxTaskCount)
                throw new TaskCountLimitException(_maxTaskCount);

            if (await _repository.ExistsByNameAsync(user.UserId, name, ct))
                throw new InvalidOperationException("Такая активная задача уже существует");

            var task = new ToDoItem(user, name);
            await _repository.AddAsync(task, ct);
            return task;
        }

        public async Task MarkCompletedAsync(Guid taskId, CancellationToken ct = default)
        {
            var task = await _repository.GetAsync(taskId, ct)
                ?? throw new KeyNotFoundException("Задача не найдена");

            task.Complete();
            await _repository.UpdateAsync(task, ct);
        }

        public async Task DeleteAsync(Guid taskId, CancellationToken ct = default)
        {
            var task = await _repository.GetAsync(taskId, ct);
            if (task == null)
                throw new KeyNotFoundException("Задача не найдена");

            await _repository.DeleteAsync(taskId, ct);
        }

        public Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken ct = default)
        {
            return _repository.FindAsync(user.UserId, t =>
                t.State == ToDoItemState.Active &&
                t.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase), ct);
        }
    }
}