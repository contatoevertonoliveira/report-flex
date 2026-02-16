$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=Logins;Integrated Security=True;MultipleActiveResultSets=True"

function Exec-Query {
    param([string]$Query)
    try {
        $cn = New-Object System.Data.SqlClient.SqlConnection
        $cn.ConnectionString = $connectionString
        $cn.Open()
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = $Query
        $reader = $cmd.ExecuteReader()
        $t = New-Object System.Data.DataTable
        $t.Load($reader)
        $cn.Close()
        Write-Host "Query: $Query"
        if ($t.Rows.Count -gt 0) {
            $t | Format-Table -AutoSize
        } else {
            Write-Host "No rows."
        }
        Write-Host "----------------------------"
    } catch {
        Write-Error $_.Exception.Message
    }
}

Exec-Query "SELECT COUNT(*) AS TotalClientes FROM dbo.Clientes"
Exec-Query "SELECT COUNT(*) AS TotalPrestadores FROM dbo.Prestadores"
Exec-Query "SELECT TOP 3 SBID,NOME,ENDERECO,FONE,EMAIL,SITE,ATIVO FROM dbo.Prestadores ORDER BY SBID"
