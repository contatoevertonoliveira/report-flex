$connStr = "Server=(localdb)\MSSQLLocalDB;Initial Catalog=Logins;Integrated Security=True;Connect Timeout=120"
$cmdText = @"
-- Atualizar tokens baseados no Nível antigo, garantindo unicidade dentro dos ranges
WITH CTE AS (
    SELECT USUARIO, NIVEL, ROW_NUMBER() OVER (PARTITION BY NIVEL ORDER BY USUARIO) as RN
    FROM Login
)
UPDATE L
SET TOKEN = CASE 
    WHEN C.NIVEL = 'Administrador' THEN RIGHT('000' + CAST(10 + C.RN AS VARCHAR(3)), 3)
    WHEN C.NIVEL = 'Padrão' THEN RIGHT('000' + CAST(20 + C.RN AS VARCHAR(3)), 3)
    WHEN C.NIVEL = 'Básico' THEN RIGHT('000' + CAST(30 + C.RN AS VARCHAR(3)), 3)
    ELSE L.TOKEN
END
FROM Login L
INNER JOIN CTE C ON L.USUARIO = C.USUARIO
WHERE C.NIVEL IN ('Administrador', 'Padrão', 'Básico');

PRINT 'Tokens atualizados com sucesso.';
SELECT USUARIO, NOME, NIVEL, TOKEN FROM Login;
"@

try {
    Write-Host "Conectando ao banco de dados..."
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $cmdText
    
    Write-Host "Executando atualizacao de tokens..."
    $reader = $cmd.ExecuteReader()
    
    # Ler resultados (Output dos prints e select)
    while ($reader.Read()) {
        Write-Host "Usuario: $($reader["USUARIO"]) | Nome: $($reader["NOME"]) | Nivel Antigo: $($reader["NIVEL"]) | Novo Token: $($reader["TOKEN"])"
    }
    
    $conn.Close()
    Write-Host "Concluido." -ForegroundColor Green
} catch {
    Write-Host "Erro ao atualizar tokens:" -ForegroundColor Red
    Write-Host $_
    Write-Host "Detalhes: " + $_.Exception.Message
}
