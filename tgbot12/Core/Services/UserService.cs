using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ToDoUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken ct = default)
        {
            var existing = await _repository.GetByTelegramUserIdAsync(telegramUserId, ct);
            if (existing != null) return existing;

            var user = new ToDoUser(telegramUserId, telegramUserName ?? "Unknown");
            await _repository.AddAsync(user, ct);
            return user;
        }

        public Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken ct = default)
        {
            return _repository.GetByTelegramUserIdAsync(telegramUserId, ct);
        }
    }
}