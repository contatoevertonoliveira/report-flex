$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Logins;Integrated Security=True"
$query = "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Login'"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $query
    $reader = $command.ExecuteReader()
    
    while ($reader.Read()) {
        Write-Output "$($reader["COLUMN_NAME"]) - $($reader["DATA_TYPE"])($($reader["CHARACTER_MAXIMUM_LENGTH"]))"
    }
    
    $connection.Close()
}
catch {
    Write-Error $_.Exception.Message
}