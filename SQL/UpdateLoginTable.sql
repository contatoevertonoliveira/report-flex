USE Logins;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Login]') AND name = 'TOKEN')
BEGIN
    ALTER TABLE [dbo].[Login] ADD [TOKEN] [varchar](100) NULL;
END
GO

-- Populate TOKEN with USUARIO for existing users so they can log in
UPDATE [dbo].[Login] SET [TOKEN] = [USUARIO] WHERE [TOKEN] IS NULL;
GO
