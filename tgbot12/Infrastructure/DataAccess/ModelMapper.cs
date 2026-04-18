using ToDoListBot.Core.DataAcces.Models;
using ToDoListBot.Core.DataAccess.Models;
using ToDoListBot.Core.Entities;
using System;

namespace ToDoListBot.Infrastructure.DataAccess
{
    internal static class ModelMapper
    {
        // ToDoUser
        public static ToDoUser MapFromModel(ToDoUserModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var user = new ToDoUser(model.TelegramUserId, model.TelegramUserName ?? string.Empty)
            {
                UserId = model.UserId,
                RegisteredAt = model.RegisteredAt
            };

            return user;
        }

        public static ToDoUserModel MapToModel(ToDoUser entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            return new ToDoUserModel
            {
                UserId = entity.UserId,
                TelegramUserId = entity.TelegramUserId,
                TelegramUserName = entity.TelegramUserName,
                RegisteredAt = entity.RegisteredAt
            };
        }

        // ToDoItem
        public static ToDoItem MapFromModel(ToDoItemModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

           
            var user = new ToDoUser(0, string.Empty)
            {
                UserId = model.UserId
            };

            var item = new ToDoItem(user, model.Name, model.ListId, model.Deadline)
            {
                Id = model.Id,
                CreatedAt = model.CreatedAt,
                State = (ToDoItemState)model.State,
                StateChangedAt = model.StateChangedAt
            };

            return item;
        }

        public static ToDoItemModel MapToModel(ToDoItem entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.User == null) throw new ArgumentException("Entity.User must not be null", nameof(entity));

            return new ToDoItemModel
            {
                Id = entity.Id,
                UserId = entity.User.UserId,
                ListId = entity.ListId,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt,
                Deadline = entity.Deadline,
                State = (int)entity.State,
                StateChangedAt = entity.StateChangedAt
            };
        }

        // ToDoList
        public static ToDoList MapFromModel(ToDoListModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            
            var user = new ToDoUser(0, string.Empty)
            {
                UserId = model.UserId
            };

            var list = new ToDoList(user, model.Name)
            {
                Id = model.ListId,
                CreatedAt = model.CreatedAt
            };

            return list;
        }

        public static ToDoListModel MapToModel(ToDoList entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.User == null) throw new ArgumentException("Entity.User must not be null", nameof(entity));

            return new ToDoListModel
            {
                ListId = entity.Id,
                UserId = entity.User.UserId,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
