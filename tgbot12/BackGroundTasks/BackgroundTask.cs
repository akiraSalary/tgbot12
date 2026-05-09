
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToDoListBot.BackgroundTasks
{
    public abstract class BackgroundTask : IBackgroundTask
    {
        private readonly TimeSpan _delay;
        private readonly string _name;

        protected BackgroundTask(TimeSpan delay, string name)
        {
            _delay = delay;
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        protected abstract Task Execute(CancellationToken ct);

        public async Task Start(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"{_name}. Execute");
                    await Execute(ct).ConfigureAwait(false);

                    Console.WriteLine($"{_name}. Start delay {_delay}");
                    await Task.Delay(_delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    //cancelled 
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{_name}. Error: {ex}");
                    // pause 
                    try { await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false); }
                    catch (OperationCanceledException) { /* отмена во время pause */ }
                }
            }
        }
    }
}