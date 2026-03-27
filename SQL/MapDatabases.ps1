param(
  [string]$EnvPath = "",
  [string]$OutDir = ""
)

$ErrorActionPreference = "Stop"

function Read-EnvFile([string]$path) {
  $dict = @{}
  if ([string]::IsNullOrWhiteSpace($path)) { return $dict }
  if (!(Test-Path -LiteralPath $path)) { return $dict }
  foreach ($line in Get-Content -LiteralPath $path) {
    $t = $line.Trim()
    if ($t.Length -eq 0) { continue }
    if ($t.StartsWith("#")) { continue }
    $idx = $t.IndexOf("=")
    if ($idx -le 0) { continue }
    $k = $t.Substring(0, $idx).Trim()
    $v = $t.Substring($idx + 1).Trim()
    $dict[$k] = $v
  }
  return $dict
}

function Find-RepoEnv([string]$startDir) {
  $dir = New-Object System.IO.DirectoryInfo($startDir)
  for ($i = 0; $i -lt 12 -and $dir -ne $null; $i++) {
    $candidate = Join-Path $dir.FullName ".env"
    if (Test-Path -LiteralPath $candidate) { return $candidate }
    $dir = $dir.Parent
  }
  return ""
}

function Normalize-ConnectionString([string]$cs) {
  $x = $cs
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "\bDataSource\b", "Data Source", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "\bInitialCatalog\b", "Initial Catalog", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "\bTrustServerCertificate\b", "TrustServerCertificate", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "\bUserID\b", "User ID", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "\bPwd\b", "Password", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  return $x
}

function Mask-ConnectionString([string]$cs) {
  $x = $cs
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "(^|;)\s*Password\s*=\s*[^;]*", "", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "(^|;)\s*Pwd\s*=\s*[^;]*", "", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "(^|;)\s*User\s*ID\s*=\s*[^;]*", "", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, "(^|;)\s*UID\s*=\s*[^;]*", "", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $x = [System.Text.RegularExpressions.Regex]::Replace($x, ";;+", ";", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase).Trim().TrimEnd(";")
  return $x
}

function Get-ConnForDb([hashtable]$env, [string]$key) {
  if ($env.ContainsKey($key) -and -not [string]::IsNullOrWhiteSpace($env[$key])) { return (Normalize-ConnectionString $env[$key]) }
  return ""
}

function New-Connection([string]$cs) {
  $csN = Normalize-ConnectionString $cs
  $cs2 = $csN
  $cs2 = [System.Text.RegularExpressions.Regex]::Replace($cs2, "(^|;)\s*TrustServerCertificate\s*=\s*[^;]*", "", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $cs2 = [System.Text.RegularExpressions.Regex]::Replace($cs2, "(^|;)\s*Encrypt\s*=\s*[^;]*", "", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  if ($cs2.TrimEnd().EndsWith(";")) { $cs2 = $cs2 + "Encrypt=False" } else { $cs2 = $cs2 + ";Encrypt=False" }
  return New-Object System.Data.SqlClient.SqlConnection($cs2)
}

function Exec-Query([object]$cn, [string]$sql) {
  $cmd = $cn.CreateCommand()
  $null = $cmd.CommandTimeout = 60
  $null = $cmd.CommandText = $sql
  $dt = New-Object System.Data.DataTable
  $da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
  $null = $da.Fill($dt)
  return ,$dt
}

function Get-DbSnapshot([string]$name, [string]$cs) {
  $snap = [ordered]@{
    name = $name
    connection = (Mask-ConnectionString $cs)
    ok = $false
    error = $null
    tables = @()
    foreignKeys = @()
    candidates = @()
    stats = @{}
  }
  try {
    $cn = New-Connection $cs
    $cn.Open()
    $snap.ok = $true

    $info = Exec-Query $cn "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName, SYSTEM_USER AS DbUser"
    if ($info -ne $null -and $info.Rows -ne $null -and $info.Rows.Count -gt 0) {
      $r0 = $info.Rows.Item(0)
      $snap.stats.server = [ordered]@{
        ServerName = $r0["ServerName"]
        DatabaseName = $r0["DatabaseName"]
        DbUser = $r0["DbUser"]
      }
    }

    $tc = Exec-Query $cn "SELECT COUNT(1) AS Cnt FROM sys.tables WHERE is_ms_shipped=0"
    if ($tc -ne $null -and $tc.Rows -ne $null -and $tc.Rows.Count -gt 0) {
      $snap.stats.userTableCount = [int]($tc.Rows.Item(0).Item("Cnt"))
    }

    $tables = Exec-Query $cn @"
SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name
"@

    $counts = Exec-Query $cn @"
SELECT s.name AS SchemaName, t.name AS TableName, SUM(p.rows) AS TotalRows
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.partitions p ON p.object_id = t.object_id
WHERE t.is_ms_shipped = 0 AND p.index_id IN (0,1)
GROUP BY s.name, t.name
ORDER BY s.name, t.name
"@

    $countMap = @{}
    foreach ($row in $counts.Rows) {
      if ($row -eq $null) { continue }
      $k = (([string]$row.Item("SchemaName")) + "." + ([string]$row.Item("TableName")))
      $countMap[$k] = [int64]($row.Item("TotalRows"))
    }

    $cols = Exec-Query $cn @"
SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName, ty.name AS TypeName, c.max_length AS MaxLen, c.is_nullable AS IsNullable
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id
"@

    $colMap = @{}
    foreach ($row in $cols.Rows) {
      if ($row -eq $null) { continue }
      $tk = (([string]$row.Item("SchemaName")) + "." + ([string]$row.Item("TableName")))
      if (-not $colMap.ContainsKey($tk)) { $colMap[$tk] = @() }
      $colMap[$tk] += [ordered]@{
        name = $row.Item("ColumnName")
        type = $row.Item("TypeName")
        maxLen = [int]($row.Item("MaxLen"))
        nullable = ([int]($row.Item("IsNullable")) -eq 1)
      }
    }

    foreach ($row in $tables.Rows) {
      if ($row -eq $null) { continue }
      $tk = (([string]$row.Item("SchemaName")) + "." + ([string]$row.Item("TableName")))
      $rc = 0
      if ($countMap.ContainsKey($tk)) { $rc = $countMap[$tk] }
      $colsForTable = @()
      if ($colMap.ContainsKey($tk)) { $colsForTable = $colMap[$tk] }
      $snap.tables += [ordered]@{
        name = $tk
        rows = $rc
        columns = $colsForTable
      }
    }

    $fks = Exec-Query $cn @"
SELECT 
  s1.name AS FromSchema, t1.name AS FromTable, c1.name AS FromColumn,
  s2.name AS ToSchema,   t2.name AS ToTable,   c2.name AS ToColumn,
  fk.name AS FKName
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.tables t1 ON t1.object_id = fk.parent_object_id
INNER JOIN sys.schemas s1 ON s1.schema_id = t1.schema_id
INNER JOIN sys.columns c1 ON c1.object_id = t1.object_id AND c1.column_id = fkc.parent_column_id
INNER JOIN sys.tables t2 ON t2.object_id = fk.referenced_object_id
INNER JOIN sys.schemas s2 ON s2.schema_id = t2.schema_id
INNER JOIN sys.columns c2 ON c2.object_id = t2.object_id AND c2.column_id = fkc.referenced_column_id
WHERE fk.is_ms_shipped = 0
ORDER BY s1.name, t1.name, fk.name
"@
    foreach ($row in $fks.Rows) {
      if ($row -eq $null) { continue }
      $snap.foreignKeys += [ordered]@{
        from = (([string]$row.Item("FromSchema")) + "." + ([string]$row.Item("FromTable")) + "." + ([string]$row.Item("FromColumn")))
        to = (([string]$row.Item("ToSchema")) + "." + ([string]$row.Item("ToTable")) + "." + ([string]$row.Item("ToColumn")))
        name = $row.Item("FKName")
      }
    }

    $dateCols = @{}
    foreach ($t in $snap.tables) {
      $d = @()
      foreach ($c in $t.columns) {
        $n = ""
        if ($c -ne $null -and $c.Contains("name") -and $c.name -ne $null) { $n = $c.name.ToString() }
        $ty = ""
        if ($c -ne $null -and $c.Contains("type") -and $c.type -ne $null) { $ty = $c.type.ToString().ToLowerInvariant() }
        if ($ty -eq "datetime" -or $ty -eq "datetime2" -or $ty -eq "smalldatetime" -or $ty -eq "date" -or $ty -eq "time") { $d += $n; continue }
        if (-not [string]::IsNullOrWhiteSpace($n)) {
          $u = $n.ToUpperInvariant()
          if ($u.Contains("DATE") -or $u.Contains("TIME")) { $d += $n }
        }
      }
      if ($d.Count -gt 0) { $dateCols[$t.name] = $d }
    }
    $snap.stats.dateColumns = $dateCols

    $cn.Close()
  } catch {
    $snap.ok = $false
    $line = $null
    try { $line = $_.InvocationInfo.ScriptLineNumber } catch { }
    if ($line -ne $null) {
      $snap.error = ($_.Exception.Message + " (linha " + $line + ")")
    } else {
      $snap.error = $_.Exception.Message
    }
  }
  return $snap
}

if ([string]::IsNullOrWhiteSpace($EnvPath)) {
  $EnvPath = Find-RepoEnv (Get-Location).Path
}
if ([string]::IsNullOrWhiteSpace($OutDir)) {
  $OutDir = Join-Path (Get-Location).Path "SQL\_mapping"
}
if (!(Test-Path -LiteralPath $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

$env = Read-EnvFile $EnvPath
$cms = Get-ConnForDb $env "DB_CMS_CONN"
$logins = Get-ConnForDb $env "DB_LOGINS_CONN"
$ems = Get-ConnForDb $env "DB_EMS_CONN"

$map = [ordered]@{
  generatedAt = (Get-Date).ToString("s")
  envPath = $EnvPath
  databases = @()
}

if (-not [string]::IsNullOrWhiteSpace($cms)) { $map.databases += (Get-DbSnapshot "CMS" $cms) }
if (-not [string]::IsNullOrWhiteSpace($logins)) { $map.databases += (Get-DbSnapshot "Logins" $logins) }
if (-not [string]::IsNullOrWhiteSpace($ems)) { $map.databases += (Get-DbSnapshot "EMS(hwreportsview)" $ems) }

$jsonPath = Join-Path $OutDir "db-mapping.json"
$mdPath = Join-Path $OutDir "db-mapping.md"

$map | ConvertTo-Json -Depth 50 | Out-File -FilePath $jsonPath -Encoding UTF8

$sb = New-Object System.Text.StringBuilder
$null = $sb.AppendLine("# Mapeamento de Bancos")
$null = $sb.AppendLine("")
$null = $sb.AppendLine(("Gerado em: {0}" -f $map.generatedAt))
$null = $sb.AppendLine("")

foreach ($db in $map.databases) {
  $null = $sb.AppendLine(("## {0}" -f $db.name))
  $null = $sb.AppendLine(("Conexão (mascarada): {0}" -f $db.connection))
  if (-not $db.ok) {
    $null = $sb.AppendLine(("Status: ERRO -> {0}" -f $db.error))
    $null = $sb.AppendLine("")
    continue
  }
  $null = $sb.AppendLine("Status: OK")
  $null = $sb.AppendLine("")

  $nonnull = @($db.tables | Where-Object { $_.rows -gt 0 })
  $null = $sb.AppendLine(("Tabelas: {0} (com dados: {1})" -f $db.tables.Count, $nonnull.Count))
  $null = $sb.AppendLine("")

  $null = $sb.AppendLine("### Top tabelas por volume")
  $top = @($db.tables | Sort-Object -Property rows -Descending | Select-Object -First 20)
  foreach ($t in $top) {
    $null = $sb.AppendLine(("- {0} -> {1} linhas" -f $t.name, $t.rows))
  }
  $null = $sb.AppendLine("")

  $null = $sb.AppendLine("### Relacionamentos (FK)")
  if ($db.foreignKeys.Count -eq 0) {
    $null = $sb.AppendLine("- (nenhum FK encontrado)")
  } else {
    foreach ($fk in $db.foreignKeys) {
      $null = $sb.AppendLine(("- {0} => {1} ({2})" -f $fk.from, $fk.to, $fk.name))
    }
  }
  $null = $sb.AppendLine("")
}

$sb.ToString() | Out-File -FilePath $mdPath -Encoding UTF8
Write-Host ("Mapeamento gerado: {0}" -f $mdPath)
Write-Host ("JSON gerado: {0}" -f $jsonPath)
