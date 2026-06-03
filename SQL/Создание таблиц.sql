USE SupportManager;
GO

-- Удаление существующих таблиц (в правильном порядке)
IF OBJECT_ID('Tasks', 'U') IS NOT NULL DROP TABLE Tasks;
IF OBJECT_ID('TaskWorkPlans', 'U') IS NOT NULL DROP TABLE TaskWorkPlans;
IF OBJECT_ID('WorkPlans', 'U') IS NOT NULL DROP TABLE WorkPlans;
IF OBJECT_ID('TaskProgress', 'U') IS NOT NULL DROP TABLE TaskProgress;
IF OBJECT_ID('TaskCategories', 'U') IS NOT NULL DROP TABLE TaskCategories;
IF OBJECT_ID('Clients', 'U') IS NOT NULL DROP TABLE Clients;
IF OBJECT_ID('Employees', 'U') IS NOT NULL DROP TABLE Employees;
IF OBJECT_ID('EmployeeRoles', 'U') IS NOT NULL DROP TABLE EmployeeRoles;
IF OBJECT_ID('TaskPriorities', 'U') IS NOT NULL DROP TABLE TaskPriorities;
IF OBJECT_ID('TaskStatuses', 'U') IS NOT NULL DROP TABLE TaskStatuses;
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

-- Таблица сотрудников
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

-- Таблица клиентов
CREATE TABLE Clients (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CompanyName NVARCHAR(100) NOT NULL,
    ContactPerson NVARCHAR(100),
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Таблица категорий задач
CREATE TABLE TaskCategories (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255)
);

-- Таблица прогресса задач
CREATE TABLE TaskProgress (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Description NVARCHAR(MAX) NOT NULL,
    ProgressPercentage INT NOT NULL DEFAULT 0 CHECK (ProgressPercentage >= 0 AND ProgressPercentage <= 100),
    HoursSpent DECIMAL(4,2) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Таблица планов работ
CREATE TABLE WorkPlans (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PlanDescription NVARCHAR(MAX) NOT NULL,
    TestSteps NVARCHAR(MAX),
    EstimatedHours DECIMAL(5,2),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Таблица задач (создаем без внешних ключей на TaskWorkPlans)
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
    FOREIGN KEY (TaskProgressId) REFERENCES TaskProgress(Id) ON DELETE NO ACTION
);

-- Таблица связей задач с планами работ (создаем после Tasks)
CREATE TABLE TaskWorkPlans (
    Id INT IDENTITY(1,1) NOT NULL,
    TaskId INT NOT NULL,
    WorkPlanId INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsPrimary BIT NOT NULL DEFAULT 0,
    CONSTRAINT [PK_TaskWorkPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskWorkPlans_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskWorkPlans_WorkPlans_WorkPlanId] FOREIGN KEY ([WorkPlanId]) REFERENCES [WorkPlans] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UK_TaskWorkPlans_TaskId_WorkPlanId] UNIQUE ([TaskId], [WorkPlanId])
);

-- Создаем индексы
CREATE INDEX [IX_TaskWorkPlans_TaskId] ON [TaskWorkPlans] ([TaskId]);
CREATE INDEX [IX_TaskWorkPlans_WorkPlanId] ON [TaskWorkPlans] ([WorkPlanId]);
GO

-- ============================================
-- ЗАПОЛНЕНИЕ ДАННЫХ
-- ============================================

-- Заполнение ENUM-таблиц
INSERT INTO TaskStatuses (Id, Name, Description) VALUES
(0, 'New', 'Новая задача'),
(1, 'InProgress', 'В работе'),
(2, 'Testing', 'Тестирование'),
(3, 'Completed', 'Завершена'),
(4, 'OnHold', 'На паузе'),
(5, 'Cancelled', 'Отменена');

INSERT INTO TaskPriorities (Id, Name, Description) VALUES
(0, 'Low', 'Низкий приоритет'),
(1, 'Medium', 'Средний приоритет'),
(2, 'High', 'Высокий приоритет'),
(3, 'Critical', 'Критический приоритет'),
(4, 'Urgent', 'Срочный приоритет');

INSERT INTO EmployeeRoles (Id, Name, Description) VALUES
(1, 'Admin', 'Администратор системы'),
(2, 'Manager', 'Менеджер проектов'),
(3, 'Programmer', 'Программист'),
(4, 'Tester', 'Тестировщик'),
(5, 'Support', 'Техническая поддержка');

-- Заполнение сотрудников
INSERT INTO Employees (FirstName, LastName, Email, PasswordHash, RoleId) VALUES
('Алексей', 'Иванов', 'admin@company.com', 'admin123', 1),
('Мария', 'Петрова', 'manager@company.com', 'manager123', 2),
('Дмитрий', 'Сидоров', 'programmer1@company.com', 'prog123', 3),
('Елена', 'Козлова', 'programmer2@company.com', 'prog456', 3),
('Сергей', 'Федоров', 'tester@company.com', 'tester123', 4),
('Ольга', 'Николаева', 'support@company.com', 'support123', 5);

-- Заполнение клиентов
INSERT INTO Clients (CompanyName, ContactPerson, Email, Phone, Address) VALUES
('ООО "ТехноПрофи"', 'Иван Сидоров', 'tech@technoprofi.ru', '+7-495-123-45-67', 'Москва, ул. Ленина, д. 15'),
('АО "Инновации"', 'Петр Иванов', 'info@innovation.ru', '+7-495-234-56-78', 'Москва, пр-т Мира, д. 25'),
('ИП "Вектор"', 'Анна Смирнова', 'vector@mail.ru', '+7-495-345-67-89', 'Москва, ул. Пушкина, д. 10'),
('ЗАО "СтройГарант"', 'Михаил Козлов', 'build@stroygarant.ru', '+7-495-456-78-90', 'Москва, ш. Энтузиастов, д. 45'),
('ООО "ТоргСервис"', 'Екатерина Волкова', 'sales@torgservice.ru', '+7-495-567-89-01', 'Москва, ул. Тверская, д. 30');

-- Заполнение категорий задач
INSERT INTO TaskCategories (Name, Description) VALUES
('Разработка', 'Задачи по разработке нового функционала'),
('Исправление ошибок', 'Исправление обнаруженных багов'),
('Тестирование', 'Тестирование функционала'),
('Документация', 'Написание технической документации'),
('Поддержка', 'Техническая поддержка пользователей'),
('Оптимизация', 'Оптимизация производительности');

-- Заполнение прогресса задач
INSERT INTO TaskProgress (Description, ProgressPercentage, HoursSpent) VALUES
('Начало разработки модуля авторизации', 25, 4.0),
('Анализ проблемы с отчетами', 50, 4.0),
('Проектирование структуры API', 10, 2.5),
('Подготовка миграций базы данных', 30, 3.5),
('Анализ текущей производительности', 20, 2.0),
('Завершено тестирование модуля', 100, 12.0),
('Исправлены критические ошибки', 75, 6.5),
('Документация готова к ревью', 90, 8.0);

-- Заполнение планов работ
INSERT INTO WorkPlans (PlanDescription, TestSteps, EstimatedHours) VALUES
('Разработать модуль авторизации', 
 '1. Создать форму входа
2. Реализовать проверку учетных данных
3. Настроить систему сессий
4. Протестировать безопасность', 16.5),

('Исправить ошибку в отчетах', 
 '1. Проанализировать проблему
2. Найти причину ошибки
3. Исправить код
4. Протестировать исправление', 8.0),

('Создать API для мобильного приложения', 
 '1. Разработать структуру API
2. Реализовать endpoints
3. Написать документацию
4. Протестировать работу API', 24.0),

('Обновить базу данных', 
 '1. Создать миграции
2. Обновить схемы таблиц
3. Перенести данные
4. Протестировать целостность', 12.5),

('Оптимизировать загрузку страниц', 
 '1. Проанализировать производительность
2. Оптимизировать запросы к БД
3. Кэшировать данные
4. Измерить улучшения', 10.0),

('Тестирование платежного модуля', 
 '1. Проверить обработку успешных платежей
2. Проверить обработку неудачных платежей
3. Тестирование отката транзакций
4. Проверка безопасности', 8.0),

('Документирование API методов', 
 '1. Описать все endpoints
2. Добавить примеры запросов/ответов
3. Описать коды ошибок
4. Создать руководство по использованию', 6.0),

('Поддержка пользователей', 
 '1. Обучение новых пользователей
2. Консультации по функционалу
3. Решение технических проблем
4. Сбор обратной связи', 4.0);

-- Заполнение задач
INSERT INTO Tasks (Title, Description, ClientId, CategoryId, ManagerId, ProgrammerId, StatusId, PriorityId, TaskProgressId, DueDate, EstimatedHours, ActualHours) VALUES
('Разработка системы авторизации', 
 'Необходимо разработать безопасную систему авторизации с поддержкой ролей', 
 1, 1, 2, 3, 1, 2, 1, DATEADD(day, 14, GETDATE()), 16.5, 4.0),

('Исправление ошибки в финансовых отчетах', 
 'В отчете за последний квартал некорректно отображаются итоговые суммы', 
 2, 2, 2, 4, 1, 3, 2, DATEADD(day, 7, GETDATE()), 8.0, 4.0),

('Создание REST API для мобильного приложения', 
 'Разработать API для синхронизации данных с мобильным приложением', 
 3, 1, 2, 3, 0, 2, 3, DATEADD(day, 21, GETDATE()), 24.0, 2.5),

('Миграция базы данных на новую версию', 
 'Обновить структуру БД и перенести существующие данные', 
 4, 6, 2, 4, 1, 1, 4, DATEADD(day, 10, GETDATE()), 12.5, 3.5),

('Оптимизация скорости загрузки каталога', 
 'Ускорить загрузку страницы каталога товаров', 
 5, 6, 2, 3, 0, 2, 5, DATEADD(day, 5, GETDATE()), 10.0, 2.0),

('Тестирование платежного модуля', 
 'Полное тестирование функционала оплаты и обработки транзакций', 
 1, 3, 2, 5, 3, 1, 6, DATEADD(day, 3, GETDATE()), 8.0, 12.0),

('Документирование API методов', 
 'Создание подробной документации для всех API endpoints', 
 2, 4, 2, 4, 2, 1, 7, DATEADD(day, 7, GETDATE()), 6.0, 6.5),

('Поддержка пользователей', 
 'Консультация пользователей по работе с системой', 
 3, 5, 2, 6, 1, 0, 8, DATEADD(day, 1, GETDATE()), 4.0, 8.0);

-- Заполнение таблицы TaskWorkPlans (связи между задачами и планами работ)
-- Для каждой задачи указываем, какие планы к ней относятся и какой из них основной

-- Задача 1: Разработка системы авторизации (основной план - WorkPlanId = 1)
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (1, 1, 1);
-- Дополнительный план для задачи 1
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (1, 2, 0);

-- Задача 2: Исправление ошибки в финансовых отчетах (основной план - WorkPlanId = 2)
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (2, 2, 1);

-- Задача 3: Создание REST API (основной план - WorkPlanId = 3, дополнительный - WorkPlanId = 7)
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (3, 3, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (3, 7, 0);

-- Задача 4: Миграция базы данных (основной план - WorkPlanId = 4)
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (4, 4, 1);

-- Задача 5: Оптимизация загрузки каталога (основной план - WorkPlanId = 5)
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (5, 5, 1);

-- Задача 6: Тестирование платежного модуля (основной план - WorkPlanId = 6)
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (6, 6, 1);

-- Задача 7: Документирование API методов (основной план - WorkPlanId = 7)
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (7, 7, 1);
-- Дополнительный план для задачи 7
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (7, 3, 0);

-- Задача 8: Поддержка пользователей (основной план - WorkPlanId = 8)
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (8, 8, 1);

-- Проверка результатов
SELECT 
    '=== ЗАДАЧИ ===' AS Section;
SELECT Id, Title, ClientId, CategoryId, ManagerId, ProgrammerId, StatusId, PriorityId FROM Tasks;

SELECT '=== ПЛАНЫ РАБОТ ===' AS Section;
SELECT Id, PlanDescription, EstimatedHours FROM WorkPlans;

SELECT '=== СВЯЗИ ЗАДАЧ И ПЛАНОВ ===' AS Section;
SELECT 
    t.Id AS TaskId,
    t.Title AS TaskTitle,
    wp.Id AS WorkPlanId,
    wp.PlanDescription AS WorkPlanDescription,
    twp.IsPrimary,
    CASE WHEN twp.IsPrimary = 1 THEN 'Основной' ELSE 'Дополнительный' END AS PlanType
FROM Tasks t
JOIN TaskWorkPlans twp ON twp.TaskId = t.Id
JOIN WorkPlans wp ON wp.Id = twp.WorkPlanId
ORDER BY t.Id, twp.IsPrimary DESC;