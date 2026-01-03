USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'CMS')
BEGIN
    CREATE DATABASE CMS;
END
GO

USE CMS;
GO

-- DROP TABLES IF EXIST (Reverse order of dependencies)
IF OBJECT_ID('dbo.HA_TRANSIT', 'U') IS NOT NULL DROP TABLE dbo.HA_TRANSIT;
IF OBJECT_ID('dbo.AC_VTERMINAL', 'U') IS NOT NULL DROP TABLE dbo.AC_VTERMINAL;
IF OBJECT_ID('dbo.SbiSiteBehavior', 'U') IS NOT NULL DROP TABLE dbo.SbiSiteBehavior;
IF OBJECT_ID('dbo.AC_BEHAVIOR', 'U') IS NOT NULL DROP TABLE dbo.AC_BEHAVIOR;
IF OBJECT_ID('dbo.Card', 'U') IS NOT NULL DROP TABLE dbo.Card;
IF OBJECT_ID('dbo.ExternalRegularUserFields', 'U') IS NOT NULL DROP TABLE dbo.ExternalRegularUserFields;
IF OBJECT_ID('dbo.ExternalRegular', 'U') IS NOT NULL DROP TABLE dbo.ExternalRegular;
IF OBJECT_ID('dbo.EmployeeUserFields', 'U') IS NOT NULL DROP TABLE dbo.EmployeeUserFields;
IF OBJECT_ID('dbo.Employee', 'U') IS NOT NULL DROP TABLE dbo.Employee;
GO

-- 1. Employee
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employee]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Employee](
        [SbiID] [int] NOT NULL,
        [Name] [varchar](100) NULL,
        [Surname] [varchar](100) NULL,
        [PreferredName] [varchar](50) NULL, -- CPF
        [Identifier] [varchar](50) NULL, -- Matricula
        [StateID] [int] NULL, -- 0=Ativo
        [CommencementDateTime] [datetime] NULL,
        [ExpiryDateTime] [datetime] NULL,
        PRIMARY KEY CLUSTERED ([SbiID] ASC)
    );
END
GO

-- 2. EmployeeUserFields
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeUserFields]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeUserFields](
        [SbiID] [int] NOT NULL,
        [UF2] [varchar](100) NULL, -- Empresa
        [UF21] [varchar](100) NULL, -- Tipo
        PRIMARY KEY CLUSTERED ([SbiID] ASC)
    );
END
GO

-- 3. ExternalRegular (Terceiros)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExternalRegular]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ExternalRegular](
        [SbiID] [int] NOT NULL,
        [Name] [varchar](100) NULL,
        [Surname] [varchar](100) NULL,
        [PreferredName] [varchar](50) NULL,
        [Identifier] [varchar](50) NULL,
        [StateID] [int] NULL,
        [CommencementDateTime] [datetime] NULL,
        [ExpiryDateTime] [datetime] NULL,
        PRIMARY KEY CLUSTERED ([SbiID] ASC)
    );
END
GO

-- 4. ExternalRegularUserFields
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExternalRegularUserFields]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ExternalRegularUserFields](
        [SbiID] [int] NOT NULL,
        [UF2] [varchar](100) NULL,
        [UF21] [varchar](100) NULL,
        PRIMARY KEY CLUSTERED ([SbiID] ASC)
    );
END
GO

-- 5. Card
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Card]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Card](
        [SbiID] [int] NOT NULL,
        [CardNumber] [varchar](50) NOT NULL,
        [StateID] [int] DEFAULT 0, -- Added StateID column
        PRIMARY KEY CLUSTERED ([SbiID] ASC) -- Simplified PK
    );
END
GO

-- 6. AC_BEHAVIOR
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AC_BEHAVIOR]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AC_BEHAVIOR](
        [BEHAVIOR_ID] [int] NOT NULL,
        [DESCRIPTION] [varchar](100) NULL,
        PRIMARY KEY CLUSTERED ([BEHAVIOR_ID] ASC)
    );
END
GO

-- 7. SbiSiteBehavior
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SbiSiteBehavior]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SbiSiteBehavior](
        [SbiID] [int] NOT NULL,
        [Behavior] [int] NOT NULL
        -- Missing PK/FKs for simplicity
    );
END
GO

-- 8. AC_VTERMINAL
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AC_VTERMINAL]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AC_VTERMINAL](
        [VTERMINAL_KEY] [varchar](50) NOT NULL,
        [DESCRIPTION] [varchar](100) NULL,
        PRIMARY KEY CLUSTERED ([VTERMINAL_KEY] ASC)
    );
END
GO

-- 9. HA_TRANSIT
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HA_TRANSIT]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[HA_TRANSIT](
        [ID] [int] IDENTITY(1,1) NOT NULL,
        [SBI_ID] [int] NOT NULL,
        [TRANSIT_DATE] [datetime] NOT NULL,
        [STR_DIRECTION] [varchar](20) NULL, -- 'Entry' or 'Exit'
        [USER_TYPE] [varchar](50) NULL, -- 'Employee' or 'External Personnel'
        [TERMINAL] [varchar](50) NULL,
        PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
GO

-------------------------------------------------------------------------
-- POPULATE DATA
-------------------------------------------------------------------------

-- 1. AC_VTERMINAL (Terminais fictícios)
IF NOT EXISTS (SELECT 1 FROM AC_VTERMINAL)
BEGIN
    INSERT INTO AC_VTERMINAL (VTERMINAL_KEY, DESCRIPTION) VALUES
    ('TK2ACA1A', 'Catraca Entrada Principal'),
    ('TK2ACA1B', 'Catraca Saída Principal'),
    ('TK2BCA1A', 'Catraca Refeitório Entrada'),
    ('TK2BCA1B', 'Catraca Refeitório Saída');
END

-- 2. AC_BEHAVIOR (Níveis de Acesso)
IF NOT EXISTS (SELECT 1 FROM AC_BEHAVIOR)
BEGIN
    INSERT INTO AC_BEHAVIOR (BEHAVIOR_ID, DESCRIPTION) VALUES
    (1, 'Acesso Total'),
    (2, 'Acesso Restrito'),
    (3, 'Visitante');
END

-- 3. Funcionários e Empresas (10 Funcionários, 3 Empresas)
-- Limpar dados anteriores para evitar duplicação em execuções repetidas (Opcional, mas seguro para testes)
-- DELETE FROM HA_TRANSIT; DELETE FROM Card; DELETE FROM EmployeeUserFields; DELETE FROM SbiSiteBehavior; DELETE FROM Employee;

DECLARE @i INT = 1;
DECLARE @Company varchar(50);
DECLARE @Name varchar(50);
DECLARE @Surname varchar(50);
DECLARE @CPF varchar(20);
DECLARE @Matricula varchar(20);
DECLARE @CardNumber varchar(20);

WHILE @i <= 10
BEGIN
    -- Definir Empresa
    IF @i <= 3 SET @Company = 'Empresa Alpha';
    ELSE IF @i <= 7 SET @Company = 'Empresa Beta';
    ELSE SET @Company = 'Empresa Gama';

    SET @Name = 'Funcionario ' + CAST(@i AS varchar);
    SET @Surname = 'Teste';
    SET @CPF = '111222333' + RIGHT('00' + CAST(@i AS varchar), 2);
    SET @Matricula = 'M' + RIGHT('000' + CAST(@i AS varchar), 4);
    SET @CardNumber = '100' + RIGHT('00' + CAST(@i AS varchar), 2);

    -- Inserir Employee se não existir
    IF NOT EXISTS (SELECT 1 FROM Employee WHERE SbiID = @i)
    BEGIN
        INSERT INTO Employee (SbiID, Name, Surname, PreferredName, Identifier, StateID, CommencementDateTime, ExpiryDateTime)
        VALUES (@i, @Name, @Surname, @CPF, @Matricula, 0, '2025-01-01', '2026-12-31');
        
        INSERT INTO EmployeeUserFields (SbiID, UF2, UF21)
        VALUES (@i, @Company, 'Normal');

        INSERT INTO SbiSiteBehavior (SbiID, Behavior)
        VALUES (@i, 1); -- Acesso Total

        INSERT INTO Card (SbiID, CardNumber)
        VALUES (@i, @CardNumber);
    END

    SET @i = @i + 1;
END

-- 4. Gerar Logs de Acesso para Dezembro/2025 (Dias úteis)
-- Loop por dias
DECLARE @CurrentDate DATE = '2025-12-01';
DECLARE @EndDate DATE = '2025-12-31';
DECLARE @EmpID INT;

-- Limpar logs de Dezembro/2025 antes de gerar (para idempotência)
DELETE FROM HA_TRANSIT WHERE TRANSIT_DATE >= '2025-12-01' AND TRANSIT_DATE < '2026-01-01';

WHILE @CurrentDate <= @EndDate
BEGIN
    -- Verificar se é dia de semana (segunda a sexta)
    -- DATENAME(dw, @CurrentDate) depende do idioma, DATEPART(dw, ...) depende de SET DATEFIRST.
    -- Vamos usar ((DATEPART(dw, @CurrentDate) + @@DATEFIRST - 1) % 7) para pegar 0=Domingo, 1=Segunda, ..., 6=Sábado
    -- Mas simplificando: Dias da semana apenas.
    
    IF (DATEPART(dw, @CurrentDate) + @@DATEFIRST - 1) % 7 BETWEEN 1 AND 5 -- 1=Seg, 5=Sex
    BEGIN
        SET @EmpID = 1;
        WHILE @EmpID <= 10
        BEGIN
            -- Gerar horários com pequena variação aleatória
            DECLARE @EntryTime DATETIME = CAST(@CurrentDate AS DATETIME) + CAST('08:00:00' AS DATETIME) - CAST('1900-01-01' AS DATETIME);
            DECLARE @LunchExit DATETIME = CAST(@CurrentDate AS DATETIME) + CAST('12:00:00' AS DATETIME) - CAST('1900-01-01' AS DATETIME);
            DECLARE @LunchEntry DATETIME = CAST(@CurrentDate AS DATETIME) + CAST('13:00:00' AS DATETIME) - CAST('1900-01-01' AS DATETIME);
            DECLARE @ExitTime DATETIME = CAST(@CurrentDate AS DATETIME) + CAST('17:00:00' AS DATETIME) - CAST('1900-01-01' AS DATETIME);

            -- Adicionar alguns minutos aleatórios (0-30)
            SET @EntryTime = DATEADD(minute, ABS(CHECKSUM(NEWID()) % 30), @EntryTime);
            SET @LunchExit = DATEADD(minute, ABS(CHECKSUM(NEWID()) % 30), @LunchExit);
            SET @LunchEntry = DATEADD(minute, ABS(CHECKSUM(NEWID()) % 30), @LunchEntry);
            SET @ExitTime = DATEADD(minute, ABS(CHECKSUM(NEWID()) % 30), @ExitTime);

            -- Entrada Manhã
            INSERT INTO HA_TRANSIT (SBI_ID, TRANSIT_DATE, STR_DIRECTION, USER_TYPE, TERMINAL)
            VALUES (@EmpID, @EntryTime, 'Entry', 'Employee', 'TK2ACA1A');

            -- Saída Almoço
            INSERT INTO HA_TRANSIT (SBI_ID, TRANSIT_DATE, STR_DIRECTION, USER_TYPE, TERMINAL)
            VALUES (@EmpID, @LunchExit, 'Exit', 'Employee', 'TK2ACA1A'); -- Usando mesma catraca ou outra

            -- Volta Almoço
            INSERT INTO HA_TRANSIT (SBI_ID, TRANSIT_DATE, STR_DIRECTION, USER_TYPE, TERMINAL)
            VALUES (@EmpID, @LunchEntry, 'Entry', 'Employee', 'TK2ACA1A');

            -- Saída Tarde
            INSERT INTO HA_TRANSIT (SBI_ID, TRANSIT_DATE, STR_DIRECTION, USER_TYPE, TERMINAL)
            VALUES (@EmpID, @ExitTime, 'Exit', 'Employee', 'TK2ACA1A');

            SET @EmpID = @EmpID + 1;
        END
    END

    SET @CurrentDate = DATEADD(day, 1, @CurrentDate);
END
GO
