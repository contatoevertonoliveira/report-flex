$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=CMS;Integrated Security=True;MultipleActiveResultSets=True"
$sqlFile = "FixCMSDatabase.sql"

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
    Write-Host "CMS Database schema fix completed successfully."
}
catch {
    Write-Error $_.Exception.Message
}
