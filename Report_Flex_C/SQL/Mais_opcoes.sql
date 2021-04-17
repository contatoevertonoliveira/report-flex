

SELECT * FROM CLIENTES
SELECT * FROM PRESTADORES

UPDATE dbo.Clientes SET CAMINHOIMG='C:\Report_Flex\Report_Flex_C\bin\Debug\Logos\Conbras.jpg' where sbid='8009'
UPDATE dbo.Prestadores SET CAMINHOIMG='C:\Report_Flex\Report_Flex_C\bin\Debug\Logos\Jhonson.jpg' where sbid='4005'

UPDATE dbo.Clientes SET ATIVO='0'
UPDATE dbo.Prestadores SET ATIVO='0'