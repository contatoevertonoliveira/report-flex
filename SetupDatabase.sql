USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Logins')
BEGIN
    CREATE DATABASE Logins;
END
GO

USE Logins;
GO

-- Tabela Login
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Login]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Login](
        [ID] [int] IDENTITY(1,1) NOT NULL,
        [NOME] [varchar](100) NULL,
        [USUARIO] [varchar](50) NULL,
        [SENHA] [varchar](50) NULL,
        [NIVEL] [varchar](50) NULL,
        [STATUS] [varchar](20) NULL,
        PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
GO

-- Tabela Clientes
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Clientes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Clientes](
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
GO

-- Inserir dados fictícios na tabela Login se estiver vazia
IF NOT EXISTS (SELECT 1 FROM [dbo].[Login])
BEGIN
    INSERT INTO [dbo].[Login] (NOME, USUARIO, SENHA, NIVEL, STATUS) VALUES 
    ('Administrador', 'admin', 'admin', 'Administrador', 'Habilitado'),
    ('Usuário Comum', 'user', '123', 'Usuário', 'Habilitado'),
    ('Gerente', 'gerente', '123', 'Gerente', 'Habilitado');
END
GO

-- Inserir dados fictícios na tabela Clientes se estiver vazia
IF NOT EXISTS (SELECT 1 FROM [dbo].[Clientes])
BEGIN
    INSERT INTO [dbo].[Clientes] (NOME, ENDERECO, FONE, EMAIL, SITE, ATIVO, CAMINHOIMG) VALUES 
    ('Cliente A', 'Rua A, 123', '(11) 9999-8888', 'contato@clientea.com', 'www.clientea.com', 1, NULL),
    ('Cliente B', 'Av B, 456', '(21) 8888-7777', 'contato@clienteb.com', 'www.clienteb.com', 1, NULL),
    ('Cliente C', 'Pça C, 789', '(31) 7777-6666', 'contato@clientec.com', 'www.clientec.com', 0, NULL);
END
GO
