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
(0, 'Новая', 'Новая задача'),
(1, 'В процессе', 'В работе'),
(2, 'Тестирование', 'Тестирование'),
(3, 'Выполнена', 'Завершена'),
(4, 'Пауза', 'На паузе'),
(5, 'Отменена', 'Отменена');

INSERT INTO TaskPriorities (Id, Name, Description) VALUES
(0, 'Низкий', 'Низкий приоритет'),
(1, 'Средний', 'Средний приоритет'),
(2, 'Высокий', 'Высокий приоритет'),
(3, 'Критический', 'Критический приоритет'),
(4, 'Срочный', 'Срочный приоритет');

INSERT INTO EmployeeRoles (Id, Name, Description) VALUES
(1, 'Админ', 'Администратор системы'),
(2, 'Менеджер', 'Менеджер проектов'),
(3, 'Программист', 'Программист'),
(4, 'Тестировщик', 'Тестировщик'),
(5, 'Тех. поддержка', 'Техническая поддержка');

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

-- Заполнение прогресса задач (20 записей)
INSERT INTO TaskProgress (Description, ProgressPercentage, HoursSpent) VALUES
('Начало разработки модуля авторизации', 25, 4.0),
('Анализ проблемы с отчетами', 50, 4.0),
('Проектирование структуры API', 10, 2.5),
('Подготовка миграций базы данных', 30, 3.5),
('Анализ текущей производительности', 20, 2.0),
('Завершено тестирование модуля', 100, 12.0),
('Исправлены критические ошибки', 75, 6.5),
('Документация готова к ревью', 90, 8.0),
('Разработка модуля уведомлений', 15, 2.0),
('Интеграция с платежной системой', 40, 5.0),
('Рефакторинг кода', 60, 4.5),
('Настройка CI/CD пайплайна', 25, 3.0),
('Обновление фронтенда', 80, 6.0),
('Оптимизация SQL запросов', 55, 3.5),
('Создание резервного копирования', 95, 7.0),
('Тестирование безопасности', 70, 5.5),
('Внедрение системы логирования', 35, 3.0),
('Разработка мобильной версии', 10, 1.5),
('Обучение сотрудников', 45, 2.5),
('Анализ требований заказчика', 90, 4.0);

-- Заполнение планов работ (20 записей)
INSERT INTO WorkPlans (PlanDescription, TestSteps, EstimatedHours) VALUES
('Разработать модуль авторизации', 
 '1. Создать форму входа\n2. Реализовать проверку учетных данных\n3. Настроить систему сессий\n4. Протестировать безопасность', 16.5),

('Исправить ошибку в отчетах', 
 '1. Проанализировать проблему\n2. Найти причину ошибки\n3. Исправить код\n4. Протестировать исправление', 8.0),

('Создать API для мобильного приложения', 
 '1. Разработать структуру API\n2. Реализовать endpoints\n3. Написать документацию\n4. Протестировать работу API', 24.0),

('Обновить базу данных', 
 '1. Создать миграции\n2. Обновить схемы таблиц\n3. Перенести данные\n4. Протестировать целостность', 12.5),

('Оптимизировать загрузку страниц', 
 '1. Проанализировать производительность\n2. Оптимизировать запросы к БД\n3. Кэшировать данные\n4. Измерить улучшения', 10.0),

('Тестирование платежного модуля', 
 '1. Проверить обработку успешных платежей\n2. Проверить обработку неудачных платежей\n3. Тестирование отката транзакций\n4. Проверка безопасности', 8.0),

('Документирование API методов', 
 '1. Описать все endpoints\n2. Добавить примеры запросов/ответов\n3. Описать коды ошибок\n4. Создать руководство по использованию', 6.0),

('Поддержка пользователей', 
 '1. Обучение новых пользователей\n2. Консультации по функционалу\n3. Решение технических проблем\n4. Сбор обратной связи', 4.0),

('Разработка системы уведомлений', 
 '1. Разработать архитектуру уведомлений\n2. Реализовать email-уведомления\n3. Реализовать push-уведомления\n4. Настроить шаблоны', 14.0),

('Интеграция с внешними сервисами', 
 '1. Анализ API внешних сервисов\n2. Разработать адаптеры\n3. Реализовать обмен данными\n4. Обработка ошибок интеграции', 18.5),

('Рефакторинг кодовой базы', 
 '1. Анализ текущего кода\n2. Выделение бизнес-логики\n3. Рефакторинг слоя данных\n4. Оптимизация производительности', 12.0),

('Настройка CI/CD', 
 '1. Настроить сборку проекта\n2. Автоматизировать тестирование\n3. Настроить деплой\n4. Автоматизация релизов', 9.5),

('Обновление пользовательского интерфейса', 
 '1. Разработать новый дизайн\n2. Сверстать страницы\n3. Адаптировать под мобильные устройства\n4. Провести юзабилити-тестирование', 20.0),

('Оптимизация производительности БД', 
 '1. Анализ медленных запросов\n2. Создать индексы\n3. Оптимизировать структуру таблиц\n4. Настроить параметры сервера', 11.0),

('Настройка резервного копирования', 
 '1. Разработать стратегию бэкапов\n2. Настроить автоматическое копирование\n3. Проверить восстановление\n4. Настроить архивацию', 6.5),

('Тестирование безопасности', 
 '1. Провести аудит кода\n2. Тестирование на SQL-инъекции\n3. Тестирование XSS\n4. Проверка прав доступа', 10.0),

('Внедрение системы логирования', 
 '1. Выбрать инструмент логирования\n2. Настроить сбор логов\n3. Реализовать мониторинг\n4. Настроить алерты', 8.5),

('Разработка мобильной версии', 
 '1. Разработать адаптивный дизайн\n2. Оптимизировать загрузку\n3. Настроить оффлайн-режим\n4. Тестирование на устройствах', 22.0),

('Обучение персонала', 
 '1. Подготовить обучающие материалы\n2. Провести вебинары\n3. Создать инструкции\n4. Собрать обратную связь', 5.0),

('Анализ и сбор требований', 
 '1. Провести интервью с заказчиком\n2. Проанализировать бизнес-процессы\n3. Составить техническое задание\n4. Согласовать требования', 7.5);

-- Заполнение задач (20 записей)
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
 3, 5, 2, 6, 1, 0, 8, DATEADD(day, 1, GETDATE()), 4.0, 8.0),

('Разработка системы уведомлений', 
 'Создать модуль для отправки уведомлений пользователям через email и push', 
 4, 1, 2, 3, 1, 2, 9, DATEADD(day, 20, GETDATE()), 14.0, 2.0),

('Интеграция с платежной системой', 
 'Подключить внешнюю платежную систему для приема оплаты на сайте', 
 5, 1, 2, 3, 1, 3, 10, DATEADD(day, 18, GETDATE()), 18.5, 5.0),

('Рефакторинг кодовой базы', 
 'Провести рефакторинг легаси-кода для улучшения читаемости и производительности', 
 1, 6, 2, 4, 1, 1, 11, DATEADD(day, 15, GETDATE()), 12.0, 4.5),

('Настройка CI/CD пайплайна', 
 'Настроить непрерывную интеграцию и доставку для автоматизации релизов', 
 2, 1, 2, 3, 1, 2, 12, DATEADD(day, 12, GETDATE()), 9.5, 3.0),

('Обновление пользовательского интерфейса', 
 'Обновить дизайн всех страниц с учетом современных трендов', 
 3, 1, 2, 4, 2, 2, 13, DATEADD(day, 25, GETDATE()), 20.0, 6.0),

('Оптимизация производительности базы данных', 
 'Оптимизировать медленные запросы и структуру индексов', 
 4, 6, 2, 3, 3, 3, 14, DATEADD(day, 8, GETDATE()), 11.0, 3.5),

('Настройка резервного копирования', 
 'Разработать и внедрить систему автоматического резервного копирования', 
 5, 1, 2, 4, 3, 0, 15, DATEADD(day, 6, GETDATE()), 6.5, 7.0),

('Тестирование безопасности', 
 'Провести полное тестирование системы на уязвимости', 
 1, 3, 2, 5, 2, 1, 16, DATEADD(day, 9, GETDATE()), 10.0, 5.5),

('Внедрение системы логирования', 
 'Настроить централизованный сбор и хранение логов', 
 2, 1, 2, 3, 1, 1, 17, DATEADD(day, 11, GETDATE()), 8.5, 3.0),

('Разработка мобильной версии', 
 'Адаптировать веб-приложение под мобильные устройства', 
 3, 1, 2, 4, 0, 2, 18, DATEADD(day, 30, GETDATE()), 22.0, 1.5),

('Обучение персонала', 
 'Провести обучение сотрудников работе с новой системой', 
 4, 5, 2, 6, 1, 0, 19, DATEADD(day, 4, GETDATE()), 5.0, 2.5),

('Анализ требований заказчика', 
 'Собрать и проанализировать требования для следующей версии системы', 
 5, 4, 2, 3, 2, 1, 20, DATEADD(day, 2, GETDATE()), 7.5, 4.0);

-- Заполнение таблицы TaskWorkPlans (связи между задачами и планами работ)
-- Для каждой задачи указываем, какие планы к ней относятся и какой из них основной

-- Задача 1: Разработка системы авторизации
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (1, 1, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (1, 2, 0);

-- Задача 2: Исправление ошибки в финансовых отчетах
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (2, 2, 1);

-- Задача 3: Создание REST API
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (3, 3, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (3, 7, 0);

-- Задача 4: Миграция базы данных
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (4, 4, 1);

-- Задача 5: Оптимизация загрузки каталога
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (5, 5, 1);

-- Задача 6: Тестирование платежного модуля
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (6, 6, 1);

-- Задача 7: Документирование API методов
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (7, 7, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (7, 3, 0);

-- Задача 8: Поддержка пользователей
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (8, 8, 1);

-- Задача 9: Разработка системы уведомлений
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (9, 9, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (9, 17, 0);

-- Задача 10: Интеграция с платежной системой
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (10, 10, 1);

-- Задача 11: Рефакторинг кодовой базы
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (11, 11, 1);

-- Задача 12: Настройка CI/CD пайплайна
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (12, 12, 1);

-- Задача 13: Обновление пользовательского интерфейса
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (13, 13, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (13, 18, 0);

-- Задача 14: Оптимизация производительности базы данных
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (14, 14, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (14, 4, 0);

-- Задача 15: Настройка резервного копирования
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (15, 15, 1);

-- Задача 16: Тестирование безопасности
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (16, 16, 1);

-- Задача 17: Внедрение системы логирования
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (17, 17, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (17, 12, 0);

-- Задача 18: Разработка мобильной версии
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (18, 18, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (18, 3, 0);

-- Задача 19: Обучение персонала
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (19, 19, 1);

-- Задача 20: Анализ требований заказчика
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (20, 20, 1);
INSERT INTO TaskWorkPlans (TaskId, WorkPlanId, IsPrimary) VALUES (20, 8, 0);

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