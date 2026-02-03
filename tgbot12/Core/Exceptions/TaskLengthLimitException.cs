using System;

namespace ToDoListBot.Core.Exceptions;

public class TaskLengthLimitException : Exception
{
    public int ActualLength { get; }
    public int MaxAllowedLength { get; }

    public TaskLengthLimitException(int actualLength, int maxAllowedLength)
        : base($"Длина задачи ({actualLength}) превышает лимит {maxAllowedLength}")
    {
        ActualLength = actualLength;
        MaxAllowedLength = maxAllowedLength;
    }

    public TaskLengthLimitException(int actualLength, int maxAllowedLength, string message)
        : base(message)
    {
        ActualLength = actualLength;
        MaxAllowedLength = maxAllowedLength;
    }
}