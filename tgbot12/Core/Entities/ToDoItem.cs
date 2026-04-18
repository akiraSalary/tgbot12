using System;
using System.Text.Json.Serialization;   

namespace ToDoListBot.Core.Entities;

public class ToDoItem

{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ToDoUser User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public Guid? ListId { get; set; } = null;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonInclude]
    public ToDoItemState State { get; set; } = ToDoItemState.Active;
    public DateTime? Deadline { get; set; }
    [JsonInclude]
    public DateTime? StateChangedAt { get; set; }
    public ToDoItem(ToDoUser user, string name, Guid? listId = null, DateTime? deadline = null)
    {
        User = user;
        Name = name;
        ListId = listId;
        Deadline = deadline;
    }
    
  
    public void Complete()
    {
        if (State == ToDoItemState.Completed)
            return;

        State = ToDoItemState.Completed;
        StateChangedAt = DateTime.UtcNow;
    }
 

    public void SetDeadline(DateTime deadline)
    {
        Deadline = deadline;
    }
}