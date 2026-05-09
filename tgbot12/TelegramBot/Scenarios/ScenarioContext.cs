using System;
using System.Collections.Generic;
using ToDoListBot.TelegramBot.Scenarios;

namespace ToDoListBot.TelegramBot
{
    public class ScenarioContext
    {
        public long? UserId { get; set; }
        public ScenarioType CurrentScenario { get; set; }
        public string? CurrentStep { get; set; }
        public IDictionary<string, object?> Data { get; } = new Dictionary<string, object?>();

        // Добавленное свойство
        public DateTime CreatedAt { get; }

        public ScenarioContext(ScenarioType scenario)
        {
            CurrentScenario = scenario;
            CreatedAt = DateTime.UtcNow;
        }
    }
}