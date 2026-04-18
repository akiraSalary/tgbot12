
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListBot.Core.DataAcces.Models
{
    [Table("ToDoList")]
    public class ToDoListModel
    {
        [PrimaryKey, Column("ListId")]
        public Guid ListId { get; set; }

        [Column("UserId")]
        public Guid UserId { get; set; }

        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }
    }
}
