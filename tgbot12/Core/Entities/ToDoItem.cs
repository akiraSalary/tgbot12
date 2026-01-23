public class ToDoItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ToDoUser User { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
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
}