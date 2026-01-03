USE Logins;
GO

UPDATE [dbo].[Login] SET NIVEL = 'Padrão' WHERE USUARIO = 'user';
UPDATE [dbo].[Login] SET NIVEL = 'Padrão' WHERE USUARIO = 'gerente';

IF NOT EXISTS (SELECT * FROM [dbo].[Login] WHERE USUARIO = 'basico')
BEGIN
    INSERT INTO [dbo].[Login] (NOME, USUARIO, SENHA, NIVEL, STATUS) VALUES ('Usuário Básico', 'basico', '123', 'Básico', 'Habilitado');
END
GO
