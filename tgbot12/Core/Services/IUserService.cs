using System.Threading;
using System.Threading.Tasks;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.Services
{
    public interface IUserService
    {
        Task<ToDoUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken ct = default);
        Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken ct = default);
    }
}