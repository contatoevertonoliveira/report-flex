$connStr = "Server=(localdb)\MSSQLLocalDB;Initial Catalog=Logins;Integrated Security=True;Connect Timeout=120"
$cmdText = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Login]') AND name = 'TOKEN')
BEGIN
    ALTER TABLE [dbo].[Login] ADD [TOKEN] [varchar](100) NULL;
    PRINT 'Coluna TOKEN criada.';
END
ELSE
BEGIN
    PRINT 'Coluna TOKEN ja existe.';
END

UPDATE [dbo].[Login] SET [TOKEN] = [USUARIO] WHERE [TOKEN] IS NULL;
PRINT 'Tokens preenchidos para usuarios existentes.';
"@

try {
    Write-Host "Conectando ao banco de dados..."
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $cmdText
    
    Write-Host "Executando atualizacao da estrutura..."
    # Parte 1: Adicionar a coluna
    $cmd.CommandText = @"
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Login]') AND name = 'TOKEN')
    BEGIN
        ALTER TABLE [dbo].[Login] ADD [TOKEN] [varchar](100) NULL;
    END
"@
    $cmd.ExecuteNonQuery()
    
    # Parte 2: Atualizar os dados
    Write-Host "Atualizando dados..."
    $cmd.CommandText = "UPDATE [dbo].[Login] SET [TOKEN] = [USUARIO] WHERE [TOKEN] IS NULL;"
    $cmd.ExecuteNonQuery()
    
    $conn.Close()
    Write-Host "Sucesso! Banco de dados atualizado." -ForegroundColor Green
} catch {
    Write-Host "Erro ao atualizar banco de dados:" -ForegroundColor Red
    Write-Host $_
    Write-Host "Detalhes: " + $_.Exception.Message
}
