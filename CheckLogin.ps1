$connString = "Server=(localdb)\MSSQLLocalDB;Integrated Security=True;Database=Logins"
$query = "SELECT * FROM Login"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $query
    $reader = $cmd.ExecuteReader()
    
    $table = New-Object System.Data.DataTable
    $table.Load($reader)
    
    $table | Format-Table -AutoSize
    
    $conn.Close()
} catch {
    Write-Error $_.Exception.Message
}
