using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListBot.TelegramBot.Scenarios;

public enum ScenarioResult
{
    Processed,      // сообщение 
    Transition,     // переход 
    Completed       // завершёние
}