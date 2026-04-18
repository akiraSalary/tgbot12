using LinqToDB;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ToDoListBot.Core.DataAcces.Models
{
    [Table("ToDoUser")]
    public class ToDoUserModel
    {
        [PrimaryKey, Column("UserId")]
        public Guid UserId { get; set; }

        [Column("TelegramUserId")]
        public long TelegramUserId { get; set; }

        [Column("TelegramUserName")]
        public string? TelegramUserName { get; set; }

        [Column("RegisteredAt")]
        public DateTime RegisteredAt { get; set; }
    }
}
