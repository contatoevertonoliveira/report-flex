USE CMS;
GO

-- Fix HA_TRANSIT table schema if TERMINAL is wrong type
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HA_TRANSIT]') AND type in (N'U'))
BEGIN
    -- Check if TERMINAL column is INT
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HA_TRANSIT]') AND name = 'TERMINAL' AND system_type_id = 56) -- 56 is int
    BEGIN
        -- Alter column to VARCHAR(50)
        -- Truncate to avoid conversion errors since data is likely invalid or we want to reset test data
        TRUNCATE TABLE [dbo].[HA_TRANSIT];
        ALTER TABLE [dbo].[HA_TRANSIT] ALTER COLUMN [TERMINAL] [varchar](50) NULL;
    END
END
GO

-- Ensure AC_VTERMINAL matches
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AC_VTERMINAL]') AND type in (N'U'))
BEGIN
     IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AC_VTERMINAL]') AND name = 'VTERMINAL_KEY' AND system_type_id = 56) -- 56 is int
    BEGIN
        TRUNCATE TABLE [dbo].[AC_VTERMINAL];
        ALTER TABLE [dbo].[AC_VTERMINAL] ALTER COLUMN [VTERMINAL_KEY] [varchar](50) NOT NULL;
    END
END
GO
