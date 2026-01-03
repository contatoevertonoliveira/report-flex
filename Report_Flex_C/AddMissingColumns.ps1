$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Logins;Integrated Security=True"

$queries = @(
    "ALTER TABLE Login ADD SOBRENOME VARCHAR(100) NULL",
    "ALTER TABLE Login ADD CARGO VARCHAR(100) NULL",
    "ALTER TABLE Login ADD EMPRESA VARCHAR(100) NULL",
    "ALTER TABLE Login ADD EMAIL VARCHAR(100) NULL"
)

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    foreach ($query in $queries) {
        try {
            $command = $connection.CreateCommand()
            $command.CommandText = $query
            $command.ExecuteNonQuery()
            Write-Host "Executado com sucesso: $query"
        }
        catch {
            Write-Host "Erro ao executar '$query': $($_.Exception.Message)"
        }
    }
    
    $connection.Close()
}
catch {
    Write-Error $_.Exception.Message
}