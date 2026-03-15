using System;

namespace ToDoListBot.TelegramBot.Dto
{
    public class ToDoListCallbackDto : CallbackDto
    {
        public Guid? ToDoListId { get; set; }

        public static new ToDoListCallbackDto? FromString(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var parts = input.Split('|');
            var dto = new ToDoListCallbackDto { Action = parts[0] };

            if (parts.Length > 1 && Guid.TryParse(parts[1], out var id))
                dto.ToDoListId = id;

            return dto;
        }

        public override string ToString()
        {
            return ToDoListId.HasValue
                ? $"{Action}|{ToDoListId}"
                : Action;
        }
    }
}