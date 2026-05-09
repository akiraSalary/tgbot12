using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using ToDoListBot.TelegramBot;
using ToDoListBot.TelegramBot.Scenarios;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace ToDoListBot.BackgroundTasks
{
    public class ResetScenarioBackgroundTask : BackgroundTask
    {
        private readonly TimeSpan _resetScenarioTimeout;
        private readonly IScenarioContextRepository _scenarioRepository;
        private readonly ITelegramBotClient _bot;

        public ResetScenarioBackgroundTask(
            TimeSpan resetScenarioTimeout,
            IScenarioContextRepository scenarioRepository,
            ITelegramBotClient bot)
            : base(TimeSpan.FromHours(1), nameof(ResetScenarioBackgroundTask))
        {
            _resetScenarioTimeout = resetScenarioTimeout;
            _scenarioRepository = scenarioRepository ?? throw new ArgumentNullException(nameof(scenarioRepository));
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var contexts = await _scenarioRepository.GetContexts(ct).ConfigureAwait(false);
            var now = DateTime.UtcNow;

            foreach (var ctx in contexts.ToList())
            {
                if (ct.IsCancellationRequested) return;

                // если нет user id skip
                if (!ctx.UserId.HasValue) continue;
                if (ctx.CurrentScenario == ScenarioType.None) continue;

                var age = now - ctx.CreatedAt;
                if (age >= _resetScenarioTimeout)
                {
                    try
                    {
                        // сброс контекст
                        await _scenarioRepository.ResetContext(ctx.UserId.Value, ct).ConfigureAwait(false);

                        // отправляем сообщение пользователю
                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton("/addtask"), new KeyboardButton("/show"), new KeyboardButton("/report") }
                        })
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = false
                        };

                        var text = $"Сценарий отменен, так как не поступил ответ в течение {_resetScenarioTimeout}";
                        await _bot.SendMessage(chatId: ctx.UserId.Value, text: text, replyMarkup: keyboard, cancellationToken: ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // отмена — нормально
                        return;
                    }
                    catch
                    {
                        // Игнорируем ошибки отправки/сброса для того, чтобы другие контексты обработались
                    }
                }
            }
        }
    }
}