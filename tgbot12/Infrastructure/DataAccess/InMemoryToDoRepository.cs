public class InMemoryToDoRepository : IToDoRepository
{
    private readonly List<ToDoItem> _items = new();

    public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId) =>
        _items.Where(i => i.User.UserId == userId).ToList().AsReadOnly();

    public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId) =>
        _items.Where(i => i.User.UserId == userId && i.State == ToDoItemState.Active)
              .ToList().AsReadOnly();

    public ToDoItem? Get(Guid id) =>
        _items.FirstOrDefault(i => i.Id == id);

    public void Add(ToDoItem item) => _items.Add(item);

    public void Update(ToDoItem item) { }

    public void Delete(Guid id)
    {
        var item = Get(id);
        if (item != null) _items.Remove(item);
    }

    public bool ExistsByName(Guid userId, string name) =>
        _items.Any(i => i.User.UserId == userId &&
                        i.State == ToDoItemState.Active &&
                        string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

    public int CountActive(Guid userId) =>
        GetActiveByUserId(userId).Count;

    public IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate) =>
        _items.Where(i => i.User.UserId == userId && predicate(i))
              .ToList().AsReadOnly();
}