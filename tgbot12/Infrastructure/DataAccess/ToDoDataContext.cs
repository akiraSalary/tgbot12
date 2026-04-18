using LinqToDB;
using LinqToDB.Data;
using ToDoListBot.Core.DataAcces.Models;
using ToDoListBot.Core.DataAccess.Models;

namespace ToDoListBot.Infrastructure.DataAccess
{
    public class ToDoDataContext : DataConnection
    {
        public ToDoDataContext(string connectionString)
            : base(ProviderName.PostgreSQL, connectionString)
        {
        }

        // Таблицы
        public ITable<ToDoItemModel> ToDoItems => this.GetTable<ToDoItemModel>();
        public ITable<ToDoListModel> ToDoLists => this.GetTable<ToDoListModel>();
        public ITable<ToDoUserModel> ToDoUsers => this.GetTable<ToDoUserModel>();
    }
}