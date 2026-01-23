using System;

using ToDoListBot.Core.DataAccess;        // репозитории
using ToDoListBot.Core.Entities;    // ToDoUser, ToDoItem
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

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId) =>
            _repository.GetAllByUserId(userId);

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId) =>
            _repository.GetActiveByUserId(userId);

        public ToDoItem AddTask(ToDoUser user, string name)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название задачи не может быть пустым");

            if (name.Length > _maxTaskLength)
                throw new TaskLengthLimitException(name.Length, _maxTaskLength);

            var activeCount = _repository.CountActive(user.UserId);
            if (activeCount >= _maxTaskCount)
                throw new TaskCountLimitException(_maxTaskCount);

            if (_repository.ExistsByName(user.UserId, name))
                throw new InvalidOperationException("Такая активная задача уже существует");

            var task = new ToDoItem(user, name);
            _repository.Add(task);
            return task;
        }

        public void MarkCompleted(Guid taskId)
        {
            var task = _repository.Get(taskId)
                ?? throw new KeyNotFoundException("Задача не найдена");

            task.Complete();
            _repository.Update(task);
        }

        public void Delete(Guid taskId)
        {
            if (_repository.Get(taskId) == null)
                throw new KeyNotFoundException("Задача не найдена");

            _repository.Delete(taskId);
        }

        public IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix)
        {
            return _repository.Find(user.UserId, t =>
                t.State == ToDoItemState.Active &&
                t.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
