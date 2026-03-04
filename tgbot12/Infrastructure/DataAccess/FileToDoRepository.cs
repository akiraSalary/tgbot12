using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Infrastructure.DataAccess
{
    public class FileToDoRepository : IToDoRepository
    {
        private readonly string _basePath;
        private readonly string _indexPath;

        // Json type sh
        private static readonly JsonSerializerOptions JsonOptions = new ()
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true,
        };

        public FileToDoRepository(string basePath)
        {
            _basePath = Path.GetFullPath(basePath);
            _indexPath = Path.Combine(_basePath, "index.json");

            // if none = create
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);

            // if none = blank
            if (!File.Exists(_indexPath))
            {
                SaveIndexAsync(new Dictionary<Guid, Guid>()).GetAwaiter().GetResult();
            }
        }

        private async Task<Dictionary<Guid, Guid>> LoadIndexAsync(CancellationToken ct = default)
        {
            if (!File.Exists(_indexPath)) return new Dictionary<Guid, Guid>();

            var json = await File.ReadAllTextAsync(_indexPath, ct);
            return JsonSerializer.Deserialize<Dictionary<Guid, Guid>>(json) ?? new();
        }

        private async Task SaveIndexAsync(Dictionary<Guid, Guid> index, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(index, JsonOptions);
            await File.WriteAllTextAsync(_indexPath, json, ct);
        }

        private string GetTaskFilePath(Guid taskId) => Path.Combine(_basePath, $"ToDoItem_{taskId:N}.json");

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var index = await LoadIndexAsync(ct);

            // new id source
            var taskIds = index
                .Where(kv => kv.Value == userId)
                .Select(kv => kv.Key)
                .ToList();

            var tasks = new List<ToDoItem>();

            foreach (var taskId in taskIds)
            {
                var path = GetTaskFilePath(taskId);
                if (!File.Exists(path)) continue;

                var json = await File.ReadAllTextAsync(path, ct);
                var task = JsonSerializer.Deserialize<ToDoItem>(json, JsonOptions);
                if (task != null) tasks.Add(task);
            }

            return tasks.AsReadOnly();
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var all = await GetAllByUserIdAsync(userId, ct);
            return all.Where(t => t.State == ToDoItemState.Active).ToList().AsReadOnly();
        }

        public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var path = GetTaskFilePath(id);
            if (!File.Exists(path)) return null;

            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<ToDoItem>(json, JsonOptions);
        }

        public async Task AddAsync(ToDoItem item, CancellationToken ct = default)
        {
            var path = GetTaskFilePath(item.Id);
            var json = JsonSerializer.Serialize(item, JsonOptions);
            await File.WriteAllTextAsync(path, json, ct);

            // Обновляем 
            var index = await LoadIndexAsync(ct);
            index[item.Id] = item.User.UserId;
            await SaveIndexAsync(index, ct);
        }

        public async Task UpdateAsync(ToDoItem item, CancellationToken ct = default)
        {
            var path = GetTaskFilePath(item.Id);
            var json = JsonSerializer.Serialize(item, JsonOptions);
            await File.WriteAllTextAsync(path, json, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var path = GetTaskFilePath(id);
            if (File.Exists(path))
                File.Delete(path);

            // Удаляем 
            var index = await LoadIndexAsync(ct);
            index.Remove(id);
            await SaveIndexAsync(index, ct);
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct = default)
        {
            var tasks = await GetActiveByUserIdAsync(userId, ct);
            return tasks.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
        {
            var tasks = await GetActiveByUserIdAsync(userId, ct);
            return tasks.Count;
        }

        public async Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct = default)
        {
            var tasks = await GetAllByUserIdAsync(userId, ct);
            return tasks.Where(predicate).ToList().AsReadOnly();
        }
    }
}