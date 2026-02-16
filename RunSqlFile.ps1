param(
    [Parameter(Mandatory=$true)]
    [string]$SqlFile
)

$connectionString = "Server=(localdb)\MSSQLLocalDB;Integrated Security=True;MultipleActiveResultSets=True"

try {
    if (-not (Test-Path $SqlFile)) {
        throw "SQL file not found: $SqlFile"
    }

    $sqlContent = Get-Content $SqlFile -Raw
    $commands = $sqlContent -split "(?m)^GO\r?$"

    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()

    foreach ($commandText in $commands) {
        if (-not [string]::IsNullOrWhiteSpace($commandText)) {
            $command = $connection.CreateCommand()
            $command.CommandText = $commandText
            $command.ExecuteNonQuery() | Out-Null
        }
    }

    $connection.Close()
    Write-Host "Executed $SqlFile"
}
catch {
    Write-Error $_.Exception.Message
}
