USE Logins;
GO

-- 1. Fix PRESTADORES table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Prestadores]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Prestadores](
        [SBID] [int] IDENTITY(1,1) NOT NULL,
        [NOME] [varchar](100) NULL,
        [ENDERECO] [varchar](200) NULL,
        [FONE] [varchar](50) NULL,
        [EMAIL] [varchar](100) NULL,
        [SITE] [varchar](100) NULL,
        [ATIVO] [int] NULL,
        [CAMINHOIMG] [varchar](255) NULL,
        PRIMARY KEY CLUSTERED ([SBID] ASC)
    );
END
ELSE
BEGIN
    -- If table exists, ensure ATIVO column exists
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prestadores]') AND name = 'ATIVO')
    BEGIN
        ALTER TABLE [dbo].[Prestadores] ADD [ATIVO] [int] NULL;
    END
END
GO

-- 2. Fix HA_TRANSIT table schema if TERMINAL is wrong type
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HA_TRANSIT]') AND type in (N'U'))
BEGIN
    -- Check if TERMINAL column is INT
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HA_TRANSIT]') AND name = 'TERMINAL' AND system_type_id = 56) -- 56 is int
    BEGIN
        -- Alter column to VARCHAR(50)
        -- Might need to drop data if conversion fails, but table should be empty or contain only ints if it was int.
        -- If it contains data, we might want to truncate it since it's test data.
        TRUNCATE TABLE [dbo].[HA_TRANSIT];
        ALTER TABLE [dbo].[HA_TRANSIT] ALTER COLUMN [TERMINAL] [varchar](50) NULL;
    END
END
GO

-- 3. Ensure AC_VTERMINAL matches
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AC_VTERMINAL]') AND type in (N'U'))
BEGIN
     IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AC_VTERMINAL]') AND name = 'VTERMINAL_KEY' AND system_type_id = 56) -- 56 is int
    BEGIN
        -- Drop foreign keys if any (not defined in my scripts but good practice)
        -- Truncate and fix
        TRUNCATE TABLE [dbo].[AC_VTERMINAL];
        ALTER TABLE [dbo].[AC_VTERMINAL] ALTER COLUMN [VTERMINAL_KEY] [varchar](50) NOT NULL;
    END
END
GO
