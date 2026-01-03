$connectionString = "Server=(localdb)\MSSQLLocalDB;Integrated Security=True;MultipleActiveResultSets=True"
$sqlFile = "SetupCMSDatabase.sql"

try {
    $sqlContent = Get-Content $sqlFile -Raw
    
    # Split by GO command (simple split, assuming GO is on its own line)
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
    Write-Host "Database setup completed successfully."
}
catch {
    Write-Error $_.Exception.Message
}
