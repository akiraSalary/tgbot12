using System;

namespace ToDoListBot.TelegramBot.Dto
{
    public class PagedListCallbackDto : ToDoListCallbackDto
    {
        public int Page { get; set; } = 0;

        public static new PagedListCallbackDto FromString(string input)
        {
            var baseDto = ToDoListCallbackDto.FromString(input);
            var parts = input.Split('|');

            int page = 0;
            if (parts.Length > 2 && int.TryParse(parts[2], out int parsedPage))
                page = parsedPage;

            return new PagedListCallbackDto
            {
                Action = baseDto.Action,
                ToDoListId = baseDto.ToDoListId,
                Page = page
            };
        }

        public override string ToString()
        {
            return $"{base.ToString()}|{Page}";
        }
    }
}