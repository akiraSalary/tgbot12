using System;
using System.Text.Json.Serialization;   

namespace ToDoListBot.Core.Entities;

public class ToDoItem

{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ToDoUser User { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public Guid? ListId { get; set; } = null;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    [JsonInclude]
    public ToDoItemState State { get; private set; } = ToDoItemState.Active;
    public DateTime? StateChangedAt { get; private set; }
    public ToDoItem(ToDoUser user, string name)
    {
        User = user;
        Name = name;
    }
    public void Complete()
    {
        State = ToDoItemState.Completed;
        StateChangedAt = DateTime.UtcNow;
    }
    public DateTime? Deadline { get; private set; }

    public void SetDeadline(DateTime deadline)
    {
        Deadline = deadline;
    }
}