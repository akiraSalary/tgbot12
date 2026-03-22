using System;

namespace ToDoListBot.TelegramBot.Dto
{
    public class ToDoItemCallbackDto
    {
        public string Action { get; set; } = string.Empty;
        public Guid ToDoItemId { get; set; }

        public static ToDoItemCallbackDto FromString(string input)
        {
            var parts = input.Split('|');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid callback data format");

            return new ToDoItemCallbackDto
            {
                Action = parts[0],
                ToDoItemId = Guid.Parse(parts[1])
            };
        }

        public override string ToString()
        {
            return $"{Action}|{ToDoItemId}";
        }
    }
}
