--create database Автомастерская;
Create database Автомастерская_2

USE master;
GO

-- Переводим БД в single-user mode
ALTER DATABASE [Автомастерская_2]
SET SINGLE_USER WITH ROLLBACK IMMEDIATE
GO

-- Меняем кодировку COLLATION
ALTER DATABASE [Автомастерская_2]
COLLATE Cyrillic_General_CI_AS ;
GO

-- Переводим БД обратно в multi-user mode
ALTER DATABASE [Автомастерская_2]
SET MULTI_USER WITH ROLLBACK IMMEDIATE;
GO  
 
--Проверяем настройки COLLATION.
SELECT name, collation_name
FROM sys.databases
WHERE name = 'Автомастерская';
GO