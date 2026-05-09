
using System.Threading;
using System.Threading.Tasks;

namespace ToDoListBot.BackgroundTasks
{
    public interface IBackgroundTask
    {
        Task Start(CancellationToken ct);
    }
}