



using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListBot.Core.DataAccess.Models
{
    [Table("ToDoItem")]
    public class ToDoItemModel
    {
        [PrimaryKey]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("UserId")]
        public Guid UserId { get; set; }

        [Column("ListId")]
        public Guid? ListId { get; set; }

        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("Deadline")]
        public DateTime? Deadline { get; set; }

        [Column("State")]
        public int State { get; set; } = 0; // 0 = Active, 1 = Completed

        [Column("StateChangedAt")]
        public DateTime? StateChangedAt { get; set; }
    }
}