using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Infrastructure.DataAccess
{
    public class FileToDoListRepository : IToDoListRepository
    {
        private readonly string _basePath;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public FileToDoListRepository(string basePath)
        {
            _basePath = Path.GetFullPath(basePath);
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        private string GetListFilePath(Guid id) => Path.Combine(_basePath, $"ToDoList_{id:N}.json");

        public async Task<ToDoList?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var path = GetListFilePath(id);
            if (!File.Exists(path)) return null;

            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<ToDoList>(json, JsonOptions);
        }

        public async Task<IReadOnlyList<ToDoList>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var files = Directory.GetFiles(_basePath, "ToDoList_*.json");
            var lists = new List<ToDoList>();

            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var list = JsonSerializer.Deserialize<ToDoList>(json, JsonOptions);
                if (list?.User.UserId == userId)
                    lists.Add(list);
            }

            return lists.AsReadOnly();
        }

        public async Task AddAsync(ToDoList list, CancellationToken ct = default)
        {
            if (await ExistsByNameAsync(list.User.UserId, list.Name, ct))
                throw new InvalidOperationException("Список с таким именем уже существует");

            var path = GetListFilePath(list.Id);
            var json = JsonSerializer.Serialize(list, JsonOptions);
            await File.WriteAllTextAsync(path, json, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var path = GetListFilePath(id);
            if (File.Exists(path))
                File.Delete(path);
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct = default)
        {
            var lists = await GetByUserIdAsync(userId, ct);
            return lists.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        
    }
}