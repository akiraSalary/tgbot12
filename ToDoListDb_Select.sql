
--data pickout

-- get UserId
SELECT "UserId", "TelegramUserId", "TelegramUserName", "RegisteredAt"
FROM "ToDoUser"
WHERE "TelegramUserId" = @TelegramUserId;

-- get Lists
SELECT "ListId", "UserId", "Name", "CreatedAt"
FROM "ToDoList"
WHERE "UserId" = @UserId
ORDER BY "Name";

-- get Active
SELECT "Id", "UserId", "ListId", "Name", "CreatedAt", "Deadline", "State", "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = @UserId 
  AND "ListId" = @ListId 
  AND "State" = 0          -- 0 = Active
ORDER BY "CreatedAt";

-- 4. get Active without List
SELECT "Id", "UserId", "ListId", "Name", "CreatedAt", "Deadline", "State", "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = @UserId 
  AND "ListId" IS NULL 
  AND "State" = 0
ORDER BY "CreatedAt";

-- 5. get completed
SELECT "Id", "UserId", "ListId", "Name", "CreatedAt", "Deadline", "State", "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = @UserId 
  AND ("ListId" = @ListId OR (@ListId IS NULL AND "ListId" IS NULL))
  AND "State" = 1          -- 1 = Completed
ORDER BY "StateChangedAt" DESC;

-- 6. get task with ID
SELECT "Id", "UserId", "ListId", "Name", "CreatedAt", "Deadline", "State", "StateChangedAt"
FROM "ToDoItem"
WHERE "Id" = @Id;

-- 7. get all tasks (for debug mainly)
SELECT * FROM "ToDoItem" WHERE "UserId" = @UserId ORDER BY "CreatedAt";