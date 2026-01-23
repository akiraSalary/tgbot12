public class ToDoReportService : IToDoReportService
{
    private readonly IToDoRepository _repository;

    public ToDoReportService(IToDoRepository repository)
    {
        _repository = repository;
    }

    public (int total, int completed, int active, DateTime generatedAt) GetUserStats(Guid userId)
    {
        var all = _repository.GetAllByUserId(userId);
        int total = all.Count;
        int active = _repository.CountActive(userId);
        int completed = total - active;

        return (total, completed, active, DateTime.UtcNow);
    }
}