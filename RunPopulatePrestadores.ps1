$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=Logins;Integrated Security=True;MultipleActiveResultSets=True"
$sqlFile = "PopulatePrestadores.sql"

try {
    $sqlContent = Get-Content $sqlFile -Raw
    
    # Split by GO command
    $commands = $sqlContent -split "(?m)^GO\r?$"

    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()

    foreach ($commandText in $commands) {
        if (-not [string]::IsNullOrWhiteSpace($commandText)) {
            $command = $connection.CreateCommand()
            $command.CommandText = $commandText
            $command.ExecuteNonQuery()
        }
    }

    $connection.Close()
    Write-Host "Prestadores population completed successfully."
}
catch {
    Write-Error $_.Exception.Message
}
