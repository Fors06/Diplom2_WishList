-- Скрипт для полной очистки базы данных
USE SupportManager;
GO

-- Удаление таблиц с внешними ключами
DROP TABLE IF EXISTS Tasks;
DROP TABLE IF EXISTS WorkPlans;
DROP TABLE IF EXISTS TaskProgress;

-- Удаление таблиц без внешних ключей
DROP TABLE IF EXISTS TaskPriorities;
DROP TABLE IF EXISTS TaskStatuses;
DROP TABLE IF EXISTS TaskCategories;
DROP TABLE IF EXISTS Clients;
DROP TABLE IF EXISTS Employees;
DROP TABLE IF EXISTS EmployeeRoles;