-- tables creation

-- Users table
CREATE TABLE "ToDoUser" (
    "UserId" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TelegramUserId" BIGINT NOT NULL UNIQUE,
    "TelegramUserName" VARCHAR(255),
    "RegisteredAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Lists table
CREATE TABLE "ToDoList" (
    "ListId" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_todolist_user 
        FOREIGN KEY ("UserId") 
        REFERENCES "ToDoUser"("UserId") 
        ON DELETE CASCADE
);

-- Tasks table
CREATE TABLE "ToDoItem" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "ListId" UUID NULL,                    -- может быть NULL для задач без списка
    "Name" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Deadline" TIMESTAMP WITH TIME ZONE NULL,
    "State" INTEGER NOT NULL DEFAULT 0,    -- 0 = Active, 1 = Completed
    "StateChangedAt" TIMESTAMP WITH TIME ZONE NULL,
    
    CONSTRAINT fk_todoitem_user 
        FOREIGN KEY ("UserId") 
        REFERENCES "ToDoUser"("UserId") 
        ON DELETE CASCADE,
        
    CONSTRAINT fk_todoitem_list 
        FOREIGN KEY ("ListId") 
        REFERENCES "ToDoList"("ListId") 
        ON DELETE SET NULL
);

--INDEKSES 

-- task search by user
CREATE INDEX idx_todoitem_userid ON "ToDoItem"("UserId");

-- task search by list
CREATE INDEX idx_todoitem_listid ON "ToDoItem"("ListId");

-- task search by state
CREATE INDEX idx_todoitem_state ON "ToDoItem"("State");

-- uniqe tgID
CREATE UNIQUE INDEX idx_todouser_telegramuserid ON "ToDoUser"("TelegramUserId");

-- comms

COMMENT ON TABLE "ToDoUser" IS 'Пользователи бота';
COMMENT ON TABLE "ToDoList" IS 'Списки задач';
COMMENT ON TABLE "ToDoItem" IS 'Задачи';