

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ToDoListBot.BackgroundTasks
{
    public sealed class BackgroundTaskRunner : IDisposable
    {
        private readonly ConcurrentBag<IBackgroundTask> _tasks = new();
        private Task? _runningTasks;
        private CancellationTokenSource? _stoppingCts;

        public void AddTask(IBackgroundTask task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (_runningTasks != null)
                throw new InvalidOperationException("Tasks are already running.");

            _tasks.Add(task);
        }

        public void StartTasks(CancellationToken ct = default)
        {
            if (_runningTasks != null)
                throw new InvalidOperationException("Tasks are already running.");

            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _runningTasks = Task.WhenAll(
                _tasks.Select(t => RunSafe(t, _stoppingCts.Token))
            );
        }

        private static async Task RunSafe(IBackgroundTask task, CancellationToken ct)
        {
            try
            {
                await task.Start(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // нормальное завершение при отмене
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in {task.GetType().Name}: {ex}");
            }
        }

        public async Task StopTasksAsync(CancellationToken ct = default)
        {
            if (_runningTasks == null)
                return;

            try
            {
                _stoppingCts?.Cancel();
            }
            finally
            {
                try
                {
                    // Ожидаем завершения задач, допускаем отмену ожидания внешним токеном
                    await _runningTasks.WaitAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ожидание было отменено извне
                }
                finally
                {
                    _runningTasks = null;
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _stoppingCts?.Cancel();
            }
            catch { }

            _stoppingCts?.Dispose();
        }
    }
}