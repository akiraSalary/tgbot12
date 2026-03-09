using System;

namespace ToDoListBot.TelegramBot.Dto
{
    public class CallbackDto
    {
        public string Action { get; set; } = string.Empty;
        public Guid? ToDoListId { get; set; }

        public static CallbackDto? FromString(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var parts = input.Split('|');
            if (parts.Length == 0) return null;

            var dto = new CallbackDto { Action = parts[0] };

            if (parts.Length > 1 && Guid.TryParse(parts[1], out var listId))
                dto.ToDoListId = listId;

            return dto;
        }

        public override string ToString()
        {
            if (ToDoListId == null)
                return Action;

            return $"{Action}|{ToDoListId}";
        }
    }
}