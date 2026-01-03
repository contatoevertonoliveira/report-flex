USE Logins;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Prestadores])
BEGIN
    INSERT INTO [dbo].[Prestadores] (NOME, ENDERECO, FONE, EMAIL, SITE, ATIVO, CAMINHOIMG) VALUES 
    ('Prestador X', 'Rua X, 100', '(11) 1111-2222', 'contato@prestadorx.com', 'www.prestadorx.com', 1, NULL),
    ('Prestador Y', 'Av Y, 200', '(21) 3333-4444', 'contato@prestadory.com', 'www.prestadory.com', 1, NULL),
    ('Prestador Z', 'Pça Z, 300', '(31) 5555-6666', 'contato@prestadorz.com', 'www.prestadorz.com', 0, NULL);
END
GO
