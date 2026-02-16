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
    public class FileToDoRepository : IToDoRepository
    {
        private readonly string _basePath;
        private readonly string _indexPath;

        public FileToDoRepository(string basePath)
        {
            _basePath = Path.GetFullPath(basePath);
            _indexPath = Path.Combine(_basePath, "index.json");

            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);

            // Если индекса нет — blank
            if (!File.Exists(_indexPath))
                SaveIndexAsync(new Dictionary<Guid, Guid>()).GetAwaiter().GetResult();
        }

        private async Task<Dictionary<Guid, Guid>> LoadIndexAsync(CancellationToken ct = default)
        {
            if (!File.Exists(_indexPath)) return new Dictionary<Guid, Guid>();

            var json = await File.ReadAllTextAsync(_indexPath, ct);
            return JsonSerializer.Deserialize<Dictionary<Guid, Guid>>(json) ?? new();
        }

        private async Task SaveIndexAsync(Dictionary<Guid, Guid> index, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(index);
            await File.WriteAllTextAsync(_indexPath, json, ct);
        }

        private string GetUserFolder(Guid userId) => Path.Combine(_basePath, $"ToDoItems_{userId:N}");

        private string GetTaskFilePath(Guid taskId) => Path.Combine(_basePath, $"ToDoItem_{taskId:N}.json");

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var folder = GetUserFolder(userId);
            if (!Directory.Exists(folder)) return Array.Empty<ToDoItem>();

            var files = Directory.GetFiles(folder, "ToDoItem_*.json");
            var tasks = new List<ToDoItem>();

            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var task = JsonSerializer.Deserialize<ToDoItem>(json);
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
            return JsonSerializer.Deserialize<ToDoItem>(json);
        }

        public async Task AddAsync(ToDoItem item, CancellationToken ct = default)
        {
            var userFolder = GetUserFolder(item.User.UserId);
            if (!Directory.Exists(userFolder))
                Directory.CreateDirectory(userFolder);

            var path = Path.Combine(userFolder, $"ToDoItem_{item.Id:N}.json");
            var json = JsonSerializer.Serialize(item);
            await File.WriteAllTextAsync(path, json, ct);

            // update index
            var index = await LoadIndexAsync(ct);
            index[item.Id] = item.User.UserId;
            await SaveIndexAsync(index, ct);
        }

        public async Task UpdateAsync(ToDoItem item, CancellationToken ct = default)
        {
            // inmomry empty, overrite
            var path = GetTaskFilePath(item.Id);
            if (!File.Exists(path)) return;

            var json = JsonSerializer.Serialize(item);
            await File.WriteAllTextAsync(path, json, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var path = GetTaskFilePath(id);
            if (File.Exists(path))
                File.Delete(path);

            // delete it
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