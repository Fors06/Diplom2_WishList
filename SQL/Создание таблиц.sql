

USE SupportManager;
GO

-- ENUM-подобные таблицы для статусов и приоритетов
CREATE TABLE TaskStatuses (
    Id INT PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);

CREATE TABLE TaskPriorities (
    Id INT PRIMARY KEY, 
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);

CREATE TABLE EmployeeRoles (
    Id INT PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);


-- Таблица сотрудников (улучшенная)
CREATE TABLE Employees (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (RoleId) REFERENCES EmployeeRoles(Id)
);

-- Таблица клиентов (без изменений)
CREATE TABLE Clients (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CompanyName NVARCHAR(100) NOT NULL,
    ContactPerson NVARCHAR(100),
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Таблица категорий задач (без изменений)
CREATE TABLE TaskCategories (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255)
);

-- Таблица задач (улучшенная)
CREATE TABLE Tasks (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    ClientId INT NOT NULL,
    CategoryId INT NOT NULL,
    ManagerId INT NOT NULL,
    ProgrammerId INT NULL,
    StatusId INT NOT NULL DEFAULT 0,
    PriorityId INT NOT NULL DEFAULT 2,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    DueDate DATETIME2 NULL,
    CompletedDate DATETIME2 NULL,
    EstimatedHours DECIMAL(5,2) NULL,
    ActualHours DECIMAL(5,2) NULL,
    FOREIGN KEY (ClientId) REFERENCES Clients(Id),
    FOREIGN KEY (CategoryId) REFERENCES TaskCategories(Id),
    FOREIGN KEY (ManagerId) REFERENCES Employees(Id),
    FOREIGN KEY (ProgrammerId) REFERENCES Employees(Id),
    FOREIGN KEY (StatusId) REFERENCES TaskStatuses(Id),
    FOREIGN KEY (PriorityId) REFERENCES TaskPriorities(Id)
);

-- Таблица комментариев/прогресса (без изменений)
CREATE TABLE TaskProgress (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TaskId INT NOT NULL,
    EmployeeId INT NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    ProgressPercentage INT NOT NULL DEFAULT 0 CHECK (ProgressPercentage >= 0 AND ProgressPercentage <= 100),
    HoursSpent DECIMAL(4,2) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (TaskId) REFERENCES Tasks(Id),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);

-- Таблица планов работ (без изменений)
CREATE TABLE WorkPlans (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TaskId INT NOT NULL,
    PlanDescription NVARCHAR(MAX) NOT NULL,
    TestSteps NVARCHAR(MAX),
    EstimatedHours DECIMAL(5,2),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (TaskId) REFERENCES Tasks(Id)
);

-- Индексы для улучшения производительности
CREATE INDEX IX_Tasks_StatusId ON Tasks(StatusId);
CREATE INDEX IX_Tasks_PriorityId ON Tasks(PriorityId);
CREATE INDEX IX_Tasks_ProgrammerId ON Tasks(ProgrammerId);
CREATE INDEX IX_Tasks_DueDate ON Tasks(DueDate);
CREATE INDEX IX_TaskProgress_TaskId ON TaskProgress(TaskId);
GO