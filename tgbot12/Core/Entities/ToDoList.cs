using System;
using System.Collections.Generic;

namespace ToDoListBot.Core.Entities
{
    public class ToDoList
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = string.Empty;
        public ToDoUser User { get; init; } = null!;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        // Если нужно хранить задачи внутри списка
        public List<ToDoItem> Items { get; private set; } = new();

        public ToDoList(ToDoUser user, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 10)
                throw new ArgumentException("Название списка должно быть от 1 до 10 символов");

            User = user;
            Name = name;
        }

        public void AddItem(ToDoItem item)
        {
            Items.Add(item);
        }
    }
}
