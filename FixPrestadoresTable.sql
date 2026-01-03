USE Logins;
GO

-- Tabela Prestadores
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
    -- Se a tabela existe, verificar se a coluna ATIVO existe
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prestadores]') AND name = 'ATIVO')
    BEGIN
        ALTER TABLE [dbo].[Prestadores] ADD [ATIVO] [int] NULL;
    END
END
GO
