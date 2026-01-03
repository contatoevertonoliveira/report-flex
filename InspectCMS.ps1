$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=CMS;Integrated Security=True;MultipleActiveResultSets=True"

function Execute-Query {
    param ($query)
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = $connectionString
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $query
        $reader = $command.ExecuteReader()
        $table = New-Object System.Data.DataTable
        $table.Load($reader)
        $connection.Close()
        
        Write-Host "Query: $query"
        if ($table.Rows.Count -gt 0) {
            Write-Host "Rows returned in this sample: $($table.Rows.Count)"
            # Display columns and first row values
            foreach ($col in $table.Columns) {
                Write-Host "$($col.ColumnName): $($table.Rows[0][$col])"
            }
        } else {
            Write-Host "No rows found."
        }
        Write-Host "------------------------------------------------"
    }
    catch {
        Write-Error "Error executing $query : $_"
    }
}

Execute-Query "SELECT TOP 1 * FROM Employee"
Execute-Query "SELECT TOP 1 * FROM EmployeeUserFields"
Execute-Query "SELECT TOP 1 * FROM Card"
Execute-Query "SELECT TOP 1 * FROM HA_TRANSIT"
Execute-Query "SELECT TOP 1 * FROM AC_VTERMINAL"
Execute-Query "SELECT COUNT(*) as TotalEmployees FROM Employee"
Execute-Query "SELECT COUNT(*) as TotalCards FROM Card"
Execute-Query "SELECT COUNT(*) as TotalTransit FROM HA_TRANSIT"
