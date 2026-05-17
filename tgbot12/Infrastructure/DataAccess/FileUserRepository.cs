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
    public class FileUserRepository : IUserRepository
    {
        private readonly string _basePath;

        public FileUserRepository(string basePath)
        {
            _basePath = Path.GetFullPath(basePath);
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        private string GetUserFilePath(Guid userId) => Path.Combine(_basePath, $"ToDoUser_{userId:N}.json");

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct = default)
        {
            var path = GetUserFilePath(userId);
            if (!File.Exists(path)) return null;

            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<ToDoUser>(json);
        }

        public async Task<ToDoUser?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default)
        {
            // all files
            var files = Directory.GetFiles(_basePath, "ToDoUser_*.json");

            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var user = JsonSerializer.Deserialize<ToDoUser>(json);
                if (user?.TelegramUserId == telegramUserId)
                    return user;
            }

            return null;
        }

        public async Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct = default)
        {
            var result = new List<ToDoUser>();

            var files = Directory.GetFiles(_basePath, "ToDoUser_*.json");
            foreach (var file in files)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    var json = await File.ReadAllTextAsync(file, ct);
                    var user = System.Text.Json.JsonSerializer.Deserialize<ToDoUser>(json);
                    if (user != null) result.Add(user);
                }
                catch
                {
                    // игнорировать повреждённые файлы
                }
            }

            return result.AsReadOnly();
        }

        public async Task AddAsync(ToDoUser user, CancellationToken ct = default)
        {
            var existing = await GetByTelegramUserIdAsync(user.TelegramUserId, ct);
            if (existing != null) return;

            var path = GetUserFilePath(user.UserId);
            var json = JsonSerializer.Serialize(user);
            await File.WriteAllTextAsync(path, json, ct);
        }
    }
}