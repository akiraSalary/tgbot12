using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ToDoListBot.TelegramBot.Scenarios;

public class ScenarioContext
{
    public ScenarioType CurrentScenario { get; set; } = ScenarioType.None;
    public string? CurrentStep { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();

    public ScenarioContext(ScenarioType scenario)
    {
        CurrentScenario = scenario;
    }
}