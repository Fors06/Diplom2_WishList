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
    FOREIGN KEY (RoleId) REFERENCES EmployeeRoles(Id) ON DELETE CASCADE
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

-- Таблица комментариев/прогресса (без изменений)
CREATE TABLE TaskProgress (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Description NVARCHAR(MAX) NOT NULL,
    ProgressPercentage INT NOT NULL DEFAULT 0 CHECK (ProgressPercentage >= 0 AND ProgressPercentage <= 100),
    HoursSpent DECIMAL(4,2) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Таблица планов работ (без изменений)
CREATE TABLE WorkPlans (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PlanDescription NVARCHAR(MAX) NOT NULL,
    TestSteps NVARCHAR(MAX),
    EstimatedHours DECIMAL(5,2),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
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
    TaskProgressId INT NOT NULL,
    WorkPlansId INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    DueDate DATETIME2 NULL,
    CompletedDate DATETIME2 NULL,
    EstimatedHours DECIMAL(5,2) NULL,
    ActualHours DECIMAL(5,2) NULL,
    FOREIGN KEY (ClientId) REFERENCES Clients(Id) ON DELETE NO ACTION,
    FOREIGN KEY (CategoryId) REFERENCES TaskCategories(Id) ON DELETE NO ACTION,
    FOREIGN KEY (ManagerId) REFERENCES Employees(Id) ON DELETE NO ACTION,
    FOREIGN KEY (ProgrammerId) REFERENCES Employees(Id) ON DELETE NO ACTION,
    FOREIGN KEY (StatusId) REFERENCES TaskStatuses(Id) ON DELETE NO ACTION,
    FOREIGN KEY (PriorityId) REFERENCES TaskPriorities(Id) ON DELETE NO ACTION,
    FOREIGN KEY (TaskProgressId) REFERENCES TaskProgress(Id) ON DELETE NO ACTION,
    FOREIGN KEY (WorkPlansId) REFERENCES WorkPlans(Id) ON DELETE NO ACTION
);




