USE [master]
GO

if exists(select * from sysdatabases where name =N'Monitor')
BEGIN
	drop database Monitor
END
GO

create database Monitor 
ON   
PRIMARY
(
   NAME=Monitor,
   FILENAME ='D:\DataBase\Monitor.mdf',
   SIZE=10,
   MAXSIZE=20,
   FILEGROWTH=5%
)
LOG ON
(
   NAME=Monitor_LOG,
   FILENAME ='D:\DataBase\Monitor.ldf',
   SIZE=1,
   MAXSIZE=10,
   FILEGROWTH=3%
)
GO

USE Monitor
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Log') AND type in (N'U'))
BEGIN
CREATE TABLE Log
(
    Id SMALLINT NOT NULL,
	VarValue REAL NOT NULL,
	TimeStamp DATETIME NOT NULL
)
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'UserList') AND type in (N'U'))
BEGIN
CREATE TABLE UserList
(
    Id INT IDENTITY(1,1) NOT NULL,
	UserName NVARCHAR(50) NOT NULL,
	Password NVARCHAR(50) DEFAULT '123456',
	Role SMALLINT NOT NULL DEFAULT 0,
	Email NVARCHAR(50) NOT NULL,
	Phone NVARCHAR(50) NOT NULL
)
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Protocol') AND type in (N'U'))
BEGIN
CREATE TABLE Protocol
(
    Id SMALLINT IDENTITY(1,1) NOT NULL,
    TagName NVARCHAR(512) NOT NULL,
	DataType TINYINT NOT NULL DEFAULT 0,
	DataSize SMALLINT NOT NULL,
	Address NVARCHAR(64) NOT NULL,
	GroupID SMALLINT NOT NULL DEFAULT 0,
	IsActive BIT NOT NULL DEFAULT 1,
	Archive BIT NOT NULL DEFAULT 0,
	DefaultValue sql_variant,
	Description NVARCHAR(128),
	Maximum REAL NOT NULL DEFAULT 0.0,
	Minimum REAL NOT NULL DEFAULT 0.0,
	Cycle int NOT NULL DEFAULT 0,
	RowVersion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
)
END
GO

CREATE TRIGGER Proto_Tgr
ON Protocol
AFTER UPDATE AS 
BEGIN 
	SET NOCOUNT ON;
	UPDATE Protocol SET RowVersion=SYSDATETIME() WHERE Id IN (SELECT DISTINCT Id FROM inserted)
END 
GO 

ALTER TABLE Protocol  ENABLE TRIGGER Proto_Tgr