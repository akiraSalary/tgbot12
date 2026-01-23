using System;

namespace ToDoListBot.Core.Exceptions;

public class TaskCountLimitException : Exception
{
    public int MaxAllowedCount { get; }

    public TaskCountLimitException(int maxAllowedCount)
        : base($"Превышено максимальное количество активных задач ({maxAllowedCount})")
    {
        MaxAllowedCount = maxAllowedCount;
    }

    public TaskCountLimitException(int maxAllowedCount, string message)
        : base(message)
    {
        MaxAllowedCount = maxAllowedCount;
    }
}