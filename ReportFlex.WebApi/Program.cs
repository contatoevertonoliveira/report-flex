using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:5001");
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRouting();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddResponseCompression();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireClaim("nivel", "SuperAdmin"));
    options.AddPolicy("NotCliente", policy => policy.RequireAssertion(ctx =>
        !ctx.User.HasClaim(c =>
            string.Equals(c.Type, "nivel", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, "Cliente", StringComparison.OrdinalIgnoreCase))));
    options.AddPolicy("AdminsOnly", policy => policy.RequireAssertion(ctx =>
        ctx.User.HasClaim(c =>
            string.Equals(c.Type, "nivel", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Value, "Administrador", StringComparison.OrdinalIgnoreCase)))));
});
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? "dev";
var jwtIssuer = jwtSection.GetValue<string>("Issuer") ?? "app";
var jwtAudience = jwtSection.GetValue<string>("Audience") ?? "users";
var jwtExpires = jwtSection.GetValue<int>("ExpiresMinutes");
if (jwtExpires <= 0) jwtExpires = 120;
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
builder.Services.AddAuthentication().AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = key
    };
});
var envPathRepoRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".env"));
var envLock = new object();
Dictionary<string,string> LoadEnv()
{
    var dict = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    try
    {
        if (File.Exists(envPathRepoRoot))
        {
            foreach (var line in File.ReadAllLines(envPathRepoRoot))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                var idx = trimmed.IndexOf('=');
                if (idx <= 0) continue;
                var key = trimmed.Substring(0, idx).Trim();
                var val = trimmed.Substring(idx + 1).Trim();
                dict[key] = val;
            }
        }
    }
    catch
    {
    }
    return dict;
}

void SaveEnv(IDictionary<string,string> values)
{
    lock (envLock)
    {
        var existing = LoadEnv();
        foreach (var kv in values)
        {
            existing[kv.Key] = kv.Value ?? "";
        }
        var lines = existing.Select(kv => kv.Key + "=" + kv.Value).ToArray();
        try
        {
            var dir = Path.GetDirectoryName(envPathRepoRoot);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(envPathRepoRoot, lines);
        }
        catch
        {
        }
    }
}

var initialEnv = LoadEnv();
var dbMode = initialEnv.TryGetValue("DB_MODE", out var m) && !string.IsNullOrWhiteSpace(m)
    ? (string.Equals(m, "Real", StringComparison.OrdinalIgnoreCase) ? "Real" : "Demo")
    : "Demo";
var realOverrides = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
string? sqlAuthUser = null;
string? sqlAuthPwd = null;
if (initialEnv.TryGetValue("DB_CMS_CONN", out var envCms) && !string.IsNullOrWhiteSpace(envCms))
{
    realOverrides["CMS"] = envCms;
}
if (initialEnv.TryGetValue("DB_LOGINS_CONN", out var envLogins) && !string.IsNullOrWhiteSpace(envLogins))
{
    realOverrides["Logins"] = envLogins;
}
if (initialEnv.TryGetValue("DB_SQL_USER", out var su) && !string.IsNullOrWhiteSpace(su))
{
    sqlAuthUser = su;
}
if (initialEnv.TryGetValue("DB_SQL_PWD", out var sp) && !string.IsNullOrWhiteSpace(sp))
{
    sqlAuthPwd = sp;
}

var app = builder.Build();
app.UseResponseCompression();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
ImagesStatic.MapLegacyImages(app);

static string? GetDtoString(Dictionary<string, System.Text.Json.JsonElement> d, string key)
{
    if (!d.TryGetValue(key, out var el)) return null;
    try
    {
        if (el.ValueKind == System.Text.Json.JsonValueKind.String) return el.GetString();
        if (el.ValueKind == System.Text.Json.JsonValueKind.Number) return el.ToString();
        if (el.ValueKind == System.Text.Json.JsonValueKind.True) return "1";
        if (el.ValueKind == System.Text.Json.JsonValueKind.False) return "0";
    }
    catch
    {
    }
    return null;
}

static int GetDtoInt(Dictionary<string, System.Text.Json.JsonElement> d, string key, int defaultValue)
{
    if (!d.TryGetValue(key, out var el)) return defaultValue;
    try
    {
        if (el.ValueKind == System.Text.Json.JsonValueKind.Number && el.TryGetInt32(out var n)) return n;
        if (el.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(el.GetString(), out var n2)) return n2;
        if (el.ValueKind == System.Text.Json.JsonValueKind.True) return 1;
        if (el.ValueKind == System.Text.Json.JsonValueKind.False) return 0;
    }
    catch
    {
    }
    return defaultValue;
}

// helper used by several export endpoints
static string Escape(string? s) => s == null ? "" : s.Contains(',') ? $"\"{s.Replace("\"","\"\"")}\"" : s;

static DateTime ParseDate(string s)
{
    if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;
    string[] formats = { 
        "yyyy-MM-dd'T'HH:mm:ss", 
        "yyyy-MM-dd'T'HH:mm:ss.fffffffK", 
        "yyyy-MM-dd'T'HH:mm:ss.fffK", 
        "yyyy-MM-dd", 
        "yyyy/MM/dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "o" 
    };
    if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        return dt;
    return DateTime.Parse(s, CultureInfo.InvariantCulture);
}

string GetConn(string name)
{
    static string NormalizeConn(string s)
    {
        try
        {
            var b = new SqlConnectionStringBuilder(s);
            b.Encrypt = true;
            b.TrustServerCertificate = true;
            return b.ConnectionString;
        }
        catch
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            var up = s.TrimEnd().TrimEnd(';');
            var hasEncrypt = Regex.IsMatch(up, @"Encrypt\s*=\s*True", RegexOptions.IgnoreCase);
            var hasTrust = Regex.IsMatch(up, @"TrustServerCertificate\s*=\s*True", RegexOptions.IgnoreCase);
            if (hasEncrypt && hasTrust) return up;
            if (!hasEncrypt) up += ";Encrypt=True";
            if (!hasTrust) up += ";TrustServerCertificate=True";
            return up;
        }
    }
    string? sqlUser = sqlAuthUser;
    string? sqlPwd = sqlAuthPwd;
    if (string.IsNullOrWhiteSpace(sqlUser))
    {
        var env = LoadEnv();
        sqlUser = env.GetValueOrDefault("DB_SQL_USER");
        sqlPwd = env.GetValueOrDefault("DB_SQL_PWD");
    }

    static string ApplySqlAuth(string s, string? user, string? pwd)
    {
        if (string.IsNullOrWhiteSpace(user) || s.Contains("Integrated Security=True", StringComparison.OrdinalIgnoreCase)) return s;
        var b = new SqlConnectionStringBuilder(s);
        b.IntegratedSecurity = false;
        b.Remove("User ID");
        b.Remove("UID");
        b.Remove("Password");
        b.Remove("PWD");
        b["User ID"] = user!;
        if (!string.IsNullOrWhiteSpace(pwd)) b["Password"] = pwd!;
        return b.ConnectionString;
    }
    if (string.Equals(dbMode, "Demo", StringComparison.OrdinalIgnoreCase))
    {
        var v = builder.Configuration.GetConnectionString(name + "Demo")
            ?? builder.Configuration.GetConnectionString(name)
            ?? "";
        v = NormalizeConn(v);
        v = ApplySqlAuth(v, sqlUser, sqlPwd);
        return v;
    }
    
    if (realOverrides.TryGetValue(name, out var ov) && !string.IsNullOrWhiteSpace(ov))
    {
        ov = NormalizeConn(ov);
        try
        {
            var bOv = new SqlConnectionStringBuilder(ov);
            if (string.IsNullOrWhiteSpace(bOv.DataSource))
            {
                var candidates = new[]
                {
                    builder.Configuration.GetConnectionString(name),
                    builder.Configuration.GetConnectionString("CMS"),
                    builder.Configuration.GetConnectionString("Logins"),
                    builder.Configuration.GetConnectionString("EMS")
                };
                SqlConnectionStringBuilder? baseBuilder = null;
                foreach (var c in candidates)
                {
                    if (string.IsNullOrWhiteSpace(c)) continue;
                    try
                    {
                        var bBase = new SqlConnectionStringBuilder(c);
                        if (!string.IsNullOrWhiteSpace(bBase.DataSource))
                        {
                            baseBuilder = bBase;
                            break;
                        }
                    }
                    catch
                    {
                    }
                }
                if (baseBuilder != null)
                {
                    if (!string.IsNullOrWhiteSpace(bOv.InitialCatalog))
                        baseBuilder.InitialCatalog = bOv.InitialCatalog;
                    baseBuilder.Encrypt = bOv.Encrypt;
                    baseBuilder.TrustServerCertificate = bOv.TrustServerCertificate;
                    ov = baseBuilder.ConnectionString;
                }
                else
                {
                    ov = bOv.ConnectionString;
                }
            }
            else
            {
                ov = bOv.ConnectionString;
            }
        }
        catch
        {
        }
        ov = ApplySqlAuth(ov, sqlUser, sqlPwd);
        return ov;
    }
    
    var envData = LoadEnv();
    string envKey;
    if (name == "CMS") envKey = "DB_CMS_CONN";
    else if (name == "Logins") envKey = "DB_LOGINS_CONN";
    else if (name == "EMS") envKey = "DB_EMS_CONN";
    else if (name == "EMSEVENTS") envKey = "DB_EMSEVENTS_CONN";
    else envKey = null;
    if (!string.IsNullOrEmpty(envKey) && envData.TryGetValue(envKey, out var envVal) && !string.IsNullOrWhiteSpace(envVal))
    {
        envVal = NormalizeConn(envVal);
        try
        {
            var bEnv = new SqlConnectionStringBuilder(envVal);
            if (string.IsNullOrWhiteSpace(bEnv.DataSource))
            {
                var candidates = new[]
                {
                    builder.Configuration.GetConnectionString(name),
                    builder.Configuration.GetConnectionString("CMS"),
                    builder.Configuration.GetConnectionString("Logins"),
                    builder.Configuration.GetConnectionString("EMS"),
                    envData.GetValueOrDefault("DB_CMS_CONN"),
                    envData.GetValueOrDefault("DB_LOGINS_CONN"),
                    envData.GetValueOrDefault("DB_EMS_CONN")
                };
                SqlConnectionStringBuilder? baseBuilder = null;
                foreach (var c in candidates)
                {
                    if (string.IsNullOrWhiteSpace(c)) continue;
                    try
                    {
                        var bBase = new SqlConnectionStringBuilder(c);
                        if (!string.IsNullOrWhiteSpace(bBase.DataSource))
                        {
                            baseBuilder = bBase;
                            break;
                        }
                    }
                    catch
                    {
                    }
                }
                if (baseBuilder != null)
                {
                    if (!string.IsNullOrWhiteSpace(bEnv.InitialCatalog))
                        baseBuilder.InitialCatalog = bEnv.InitialCatalog;
                    baseBuilder.Encrypt = bEnv.Encrypt;
                    baseBuilder.TrustServerCertificate = bEnv.TrustServerCertificate;
                    envVal = baseBuilder.ConnectionString;
                }
                else
                {
                    envVal = bEnv.ConnectionString;
                }
            }
            else
            {
                envVal = bEnv.ConnectionString;
            }
        }
        catch
        {
        }
        envVal = ApplySqlAuth(envVal, sqlUser, sqlPwd);
        return envVal;
    }

    if (name == "EMSEVENTS")
    {
        try
        {
            var baseConn = envData.GetValueOrDefault("DB_CMS_CONN") ?? builder.Configuration.GetConnectionString("CMS") ?? "";
            if (!string.IsNullOrWhiteSpace(baseConn))
            {
                var b = new SqlConnectionStringBuilder(baseConn);
                b.InitialCatalog = "EMSEVENTS";
                var derived = NormalizeConn(b.ConnectionString);
                derived = ApplySqlAuth(derived, sqlUser, sqlPwd);
                return derived;
            }
        }
        catch
        {
        }
    }
    
    var cfg = builder.Configuration.GetConnectionString(name)
        ?? builder.Configuration.GetConnectionString(name + "Demo")
        ?? "";
    cfg = NormalizeConn(cfg);
    cfg = ApplySqlAuth(cfg, sqlUser, sqlPwd);
    return cfg;
}

try
{
    using var cnInit = new SqlConnection(GetConn("Logins"));
    cnInit.Open();
    using var cmdInit = cnInit.CreateCommand();
    cmdInit.CommandText = @"
IF OBJECT_ID('dbo.ClientesPortal','U') IS NULL
BEGIN
    CREATE TABLE dbo.ClientesPortal(
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        NOME VARCHAR(200) NOT NULL,
        ENDERECO VARCHAR(300) NULL,
        FONE VARCHAR(50) NULL,
        EMAIL VARCHAR(200) NULL,
        SITE VARCHAR(200) NULL,
        ATIVO INT NULL,
        CAMINHOIMG VARCHAR(255) NULL,
        RESPONSAVEL VARCHAR(100) NULL,
        CLIENT_TOKEN VARCHAR(10) NULL
    );
END

IF OBJECT_ID('dbo.MensagensPortal','U') IS NULL
BEGIN
    CREATE TABLE dbo.MensagensPortal(
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FromUsuario VARCHAR(100) NULL,
        FromNome VARCHAR(200) NULL,
        FromNivel VARCHAR(50) NULL,
        ClientId INT NULL,
        Assunto VARCHAR(200) NOT NULL,
        Texto VARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
        Status VARCHAR(50) NULL
    );
END";
    cmdInit.ExecuteNonQuery();
}
catch
{
}

app.MapGet("/api/admin/report-options", () =>
{
    var env = LoadEnv();
    bool GetFlag(string key) => env.TryGetValue(key, out var v) && (v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
    return Results.Ok(new
    {
        txt = GetFlag("REPORT_TXT"),
        xlsx = GetFlag("REPORT_XLSX"),
        pdf = GetFlag("REPORT_PDF"),
        word = GetFlag("REPORT_WORD"),
        excel = GetFlag("REPORT_EXCEL"),
        csv = GetFlag("REPORT_CSV")
    });
}).RequireAuthorization("NotCliente");

app.MapPost("/api/admin/report-options", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string, bool>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    var values = new Dictionary<string, string>
    {
        ["REPORT_TXT"] = dto.TryGetValue("txt", out var txt) && txt ? "1" : "0",
        ["REPORT_XLSX"] = dto.TryGetValue("xlsx", out var xlsx) && xlsx ? "1" : "0",
        ["REPORT_PDF"] = dto.TryGetValue("pdf", out var pdf) && pdf ? "1" : "0",
        ["REPORT_WORD"] = dto.TryGetValue("word", out var word) && word ? "1" : "0",
        ["REPORT_EXCEL"] = dto.TryGetValue("excel", out var excel) && excel ? "1" : "0",
        ["REPORT_CSV"] = dto.TryGetValue("csv", out var csv) && csv ? "1" : "0"
    };
    SaveEnv(values);
    return Results.Ok(new
    {
        txt = values["REPORT_TXT"] == "1",
        xlsx = values["REPORT_XLSX"] == "1",
        pdf = values["REPORT_PDF"] == "1",
        word = values["REPORT_WORD"] == "1",
        excel = values["REPORT_EXCEL"] == "1",
        csv = values["REPORT_CSV"] == "1"
    });
}).RequireAuthorization("NotCliente");

app.MapGet("/api/admin/report-default-client", async () =>
{
    var env = LoadEnv();
    int cid = 0;
    if (env.TryGetValue("REPORT_DEFAULT_CLIENT_ID", out var v) && int.TryParse(v, out var parsed) && parsed > 0) cid = parsed;
    if (cid <= 0) return Results.Ok(new { id = (int?)null, nome = (string?)null });
    try
    {
        using var cn = new SqlConnection(GetConn("Logins"));
        await cn.OpenAsync();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT NOME FROM dbo.ClientesPortal WHERE Id=@id";
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
        var nome = (string?)(await cmd.ExecuteScalarAsync());
        return Results.Ok(new { id = cid, nome });
    }
    catch
    {
        return Results.Ok(new { id = cid, nome = (string?)null });
    }
}).RequireAuthorization("NotCliente");

app.MapPost("/api/admin/report-default-client", async (HttpContext ctx) =>
{
    int cid = 0;
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<Dictionary<string, int>>();
        if (body != null && body.TryGetValue("clientId", out var id)) cid = id;
    }
    catch { }
    if (cid <= 0) return Results.BadRequest(new { error = "clientId inválido" });
    SaveEnv(new Dictionary<string, string> { ["REPORT_DEFAULT_CLIENT_ID"] = cid.ToString() });
    return Results.Ok(new { ok = true, id = cid });
}).RequireAuthorization("NotCliente");

app.MapGet("/api/admin/queries-config", () =>
{
    var env = LoadEnv();
    if (env.TryGetValue("REPORT_QUERIES_CONFIG", out var raw) && !string.IsNullOrWhiteSpace(raw))
    {
        try
        {
            var obj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(raw);
            return Results.Ok(obj ?? new Dictionary<string, bool>());
        }
        catch
        {
            // ignore parse errors, return empty (all desativadas)
        }
    }
    return Results.Ok(new Dictionary<string, bool>());
}).RequireAuthorization();

app.MapPost("/api/admin/queries-config", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string, bool>>();
    var json = System.Text.Json.JsonSerializer.Serialize(dto ?? new Dictionary<string, bool>());
    SaveEnv(new Dictionary<string, string> { ["REPORT_QUERIES_CONFIG"] = json });
    return Results.Ok(dto ?? new Dictionary<string, bool>());
}).RequireAuthorization("NotCliente");

static string ToOrderDir(string? dir) => string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
static int ToPage(int page) => page <= 0 ? 1 : page;
static int ToPageSize(int pageSize) => (pageSize <= 0 || pageSize > 200) ? 20 : pageSize;

async Task<string?> GetDefaultClientNameAsync()
{
    var env = LoadEnv();
    if (!env.TryGetValue("REPORT_DEFAULT_CLIENT_ID", out var v) || !int.TryParse(v, out var cid) || cid <= 0) return null;
    try
    {
        using var cn = new SqlConnection(GetConn("Logins"));
        await cn.OpenAsync();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT NOME FROM dbo.ClientesPortal WHERE Id=@id";
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
        return (string?)await cmd.ExecuteScalarAsync();
    }
    catch
    {
        return null;
    }
}

app.MapGet("/api/admin/db-mode", () =>
{
    return Results.Ok(new { mode = dbMode });
}).RequireAuthorization("NotCliente");

app.MapPost("/api/admin/db-mode", async (HttpContext ctx) =>
{
    string? mode = ctx.Request.Query["mode"];
    if (string.IsNullOrWhiteSpace(mode))
    {
        try
        {
            var body = await ctx.Request.ReadFromJsonAsync<Dictionary<string, string>>();
            if (body != null && body.TryGetValue("mode", out var m)) mode = m;
        }
        catch { }
    }
    if (string.IsNullOrWhiteSpace(mode))
    {
        return Results.BadRequest(new { error = "Parâmetro 'mode' ausente. Use Real ou Demo." });
    }
    if (!string.Equals(mode, "Real", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(mode, "Demo", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Modo inválido. Use Real ou Demo." });
    }
    dbMode = char.ToUpperInvariant(mode[0]) + mode.Substring(1).ToLowerInvariant();
    SaveEnv(new Dictionary<string,string>
    {
        ["DB_MODE"] = dbMode
    });
    return Results.Ok(new { mode = dbMode });
}).RequireAuthorization("NotCliente");

app.MapGet("/api/admin/connections", () =>
{
    // Recarregar do arquivo .env para garantir valores salvos
    var currentEnv = LoadEnv();
    var cms = realOverrides.TryGetValue("CMS", out var c) ? c : null;
    var logins = realOverrides.TryGetValue("Logins", out var l) ? l : null;
    var ems = realOverrides.TryGetValue("EMS", out var e) ? e : null;
    
    // Se não estiver em realOverrides, tentar carregar do .env ou config
    if (string.IsNullOrWhiteSpace(cms))
    {
        cms = currentEnv.TryGetValue("DB_CMS_CONN", out var envCms) && !string.IsNullOrWhiteSpace(envCms) 
            ? envCms 
            : builder.Configuration.GetConnectionString("CMS");
    }
    if (string.IsNullOrWhiteSpace(logins))
    {
        logins = currentEnv.TryGetValue("DB_LOGINS_CONN", out var envLogins) && !string.IsNullOrWhiteSpace(envLogins) 
            ? envLogins 
            : builder.Configuration.GetConnectionString("Logins");
    }
    if (string.IsNullOrWhiteSpace(ems))
    {
        ems = currentEnv.TryGetValue("DB_EMS_CONN", out var envEms) && !string.IsNullOrWhiteSpace(envEms)
            ? envEms
            : builder.Configuration.GetConnectionString("EMS");
        // derive from cms if still empty
        if (string.IsNullOrWhiteSpace(ems) && !string.IsNullOrWhiteSpace(cms))
        {
            // replace catalog with EMSEVENTS
            ems = System.Text.RegularExpressions.Regex.Replace(cms, "(Initial\\s+Catalog|Database)\\s*=\\s*[^;]+", "$1=EMSEVENTS", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
    
    return Results.Ok(new { CMS = cms, Logins = logins, EMS = ems, mode = dbMode });
}).RequireAuthorization("NotCliente");

app.MapGet("/api/admin/db-info", async () =>
{
    try
    {
        var cmsConn = GetConn("CMS");
        var loginsConn = GetConn("Logins");
        var emsConn = GetConn("EMS");
        // if EMS connection isn't configured, try deriving it from CMS string
        if (string.IsNullOrWhiteSpace(emsConn) && !string.IsNullOrWhiteSpace(cmsConn))
        {
            emsConn = System.Text.RegularExpressions.Regex.Replace(cmsConn, "(Initial\\s+Catalog|Database)\\s*=\\s*[^;]+", "$1=EMSEVENTS", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        var result = new Dictionary<string, object?>();

        try
        {
            using var cnCms = new SqlConnection(cmsConn);
            await cnCms.OpenAsync();
            using var cmdCms = cnCms.CreateCommand();
            cmdCms.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME";
            using var rCms = await cmdCms.ExecuteReaderAsync();
            var tablesCms = new List<string>();
            while (await rCms.ReadAsync())
            {
                tablesCms.Add(rCms.GetString(0));
            }
            var procsCms = new List<string>();
            using (var cmdProcs = cnCms.CreateCommand())
            {
                cmdProcs.CommandText = "SELECT s.name + '.' + p.name FROM sys.procedures p INNER JOIN sys.schemas s ON s.schema_id = p.schema_id ORDER BY s.name, p.name";
                using var rProcs = await cmdProcs.ExecuteReaderAsync();
                while (await rProcs.ReadAsync())
                {
                    procsCms.Add(rProcs.GetString(0));
                }
            }
            result["CMS"] = new { connection = cmsConn, tables = tablesCms, procedures = procsCms };
        }
        catch
        {
            result["CMS"] = null;
        }

        try
        {
            using var cnLogins = new SqlConnection(loginsConn);
            await cnLogins.OpenAsync();
            using var cmdLogins = cnLogins.CreateCommand();
            cmdLogins.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME";
            using var rLogins = await cmdLogins.ExecuteReaderAsync();
            var tablesLogins = new List<string>();
            while (await rLogins.ReadAsync())
            {
                tablesLogins.Add(rLogins.GetString(0));
            }
            var procsLogins = new List<string>();
            using (var cmdProcs = cnLogins.CreateCommand())
            {
                cmdProcs.CommandText = "SELECT s.name + '.' + p.name FROM sys.procedures p INNER JOIN sys.schemas s ON s.schema_id = p.schema_id ORDER BY s.name, p.name";
                using var rProcs = await cmdProcs.ExecuteReaderAsync();
                while (await rProcs.ReadAsync())
                {
                    procsLogins.Add(rProcs.GetString(0));
                }
            }
            result["Logins"] = new { connection = loginsConn, tables = tablesLogins, procedures = procsLogins };
        }
        catch
        {
            result["Logins"] = null;
        }

        try
        {
            using var cnEms = new SqlConnection(emsConn);
            await cnEms.OpenAsync();
            using var cmdEms = cnEms.CreateCommand();
            cmdEms.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME";
            using var rEms = await cmdEms.ExecuteReaderAsync();
            var tablesEms = new List<string>();
            while (await rEms.ReadAsync())
            {
                tablesEms.Add(rEms.GetString(0));
            }
            var procsEms = new List<string>();
            using (var cmdProcs = cnEms.CreateCommand())
            {
                cmdProcs.CommandText = "SELECT s.name + '.' + p.name FROM sys.procedures p INNER JOIN sys.schemas s ON s.schema_id = p.schema_id ORDER BY s.name, p.name";
                using var rProcs = await cmdProcs.ExecuteReaderAsync();
                while (await rProcs.ReadAsync())
                {
                    procsEms.Add(rProcs.GetString(0));
                }
            }
            result["EMS"] = new { connection = emsConn, tables = tablesEms, procedures = procsEms };
        }
        catch
        {
            result["EMS"] = null;
        }

        var identity = System.Security.Principal.WindowsIdentity.GetCurrent()?.Name ?? "";
        return Results.Ok(new { mode = dbMode, identity, databases = result });
    }
    catch (Exception ex)
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent()?.Name ?? "";
        return Results.Ok(new { mode = dbMode, identity, databases = (object?)null, error = ex.Message });
    }
}).RequireAuthorization("NotCliente");

app.MapGet("/api/admin/sql/logins", async () =>
{
    try
    {
        var items = new List<Dictionary<string, object?>>();
        using var cn = new SqlConnection(GetConn("CMS"));
        await cn.OpenAsync();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = @"
SELECT sp.name, sp.type_desc, sp.is_disabled, sp.default_database_name,
       sl.is_locked, sl.is_policy_checked, sp.create_date
FROM sys.server_principals sp
LEFT JOIN sys.sql_logins sl ON sl.principal_id = sp.principal_id
WHERE sp.type IN ('S','U','G')
ORDER BY sp.name";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            row["name"] = r.IsDBNull(0) ? null : r.GetString(0);
            row["type"] = r.IsDBNull(1) ? null : r.GetString(1);
            row["disabled"] = r.IsDBNull(2) ? null : r.GetBoolean(2);
            row["default_db"] = r.IsDBNull(3) ? null : r.GetString(3);
            row["locked"] = r.IsDBNull(4) ? (bool?)null : r.GetBoolean(4);
            row["policy_checked"] = r.IsDBNull(5) ? (bool?)null : r.GetBoolean(5);
            row["created_at"] = r.IsDBNull(6) ? null : r.GetDateTime(6);
            items.Add(row);
        }
        return Results.Ok(new { logins = items });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/admin/sql/auth-mode", async () =>
{
    try
    {
        using var cn = new SqlConnection(GetConn("CMS"));
        await cn.OpenAsync();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT CAST(SERVERPROPERTY('IsIntegratedSecurityOnly') AS INT)";
        var v = await cmd.ExecuteScalarAsync();
        var windowsOnly = (v is int i && i == 1);
        return Results.Ok(new { windowsOnly, mode = windowsOnly ? "WindowsOnly" : "Mixed" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/admin/sql/test-login-only", async () =>
{
    try
    {
        var cmsConn = GetConn("CMS");
        var b = new SqlConnectionStringBuilder(cmsConn);
        var dataSource = b.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return Results.BadRequest(new { error = "Data Source não encontrado na conexão CMS." });
        }
        var testBuilder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = "master",
            Encrypt = true,
            TrustServerCertificate = true,
            IntegratedSecurity = false
        };
        if (!string.IsNullOrWhiteSpace(sqlAuthUser)) testBuilder["User ID"] = sqlAuthUser!;
        if (!string.IsNullOrWhiteSpace(sqlAuthPwd)) testBuilder["Password"] = sqlAuthPwd!;
        try
        {
            using var cn = new SqlConnection(testBuilder.ConnectionString);
            await cn.OpenAsync();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT SYSTEM_USER, DB_NAME()";
            using var r = await cmd.ExecuteReaderAsync();
            string user = "", db = "";
            if (await r.ReadAsync())
            {
                user = r.IsDBNull(0) ? "" : r.GetString(0);
                db = r.IsDBNull(1) ? "" : r.GetString(1);
            }
            return Results.Ok(new { ok = true, user, db, connection = testBuilder.ConnectionString });
        }
        catch (Exception ex)
        {
            return Results.Ok(new { ok = false, error = ex.Message, connection = testBuilder.ConnectionString });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/admin/sql/test-auth", async () =>
{
    var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    async Task<object> Test(string key)
    {
        try
        {
            using var cn = new SqlConnection(GetConn(key));
            await cn.OpenAsync();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT SYSTEM_USER";
            var user = (string?)await cmd.ExecuteScalarAsync();
            return new { ok = true, user };
        }
        catch (Exception ex)
        {
            return new { ok = false, error = ex.Message };
        }
    }
    result["CMS"] = await Test("CMS");
    result["Logins"] = await Test("Logins");
    result["EMS"] = await Test("EMS");
    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/admin/db-table/rows", async (string db, string table, int page, int pageSize) =>
{
    if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(table))
    {
        return Results.BadRequest(new { error = "Parâmetros 'db' e 'table' são obrigatórios." });
    }

    string? dbKey = null;
    if (string.Equals(db, "CMS", StringComparison.OrdinalIgnoreCase)) dbKey = "CMS";
    else if (string.Equals(db, "Logins", StringComparison.OrdinalIgnoreCase)) dbKey = "Logins";
    else if (string.Equals(db, "EMS", StringComparison.OrdinalIgnoreCase) || string.Equals(db, "EMSEVENTS", StringComparison.OrdinalIgnoreCase)) dbKey = "EMS";

    if (dbKey == null)
    {
        return Results.BadRequest(new { error = "Banco inválido. Use CMS, Logins ou EMS." });
    }

    page = ToPage(page);
    pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;

    using var cn = new SqlConnection(GetConn(dbKey));
    await cn.OpenAsync();

    using (var checkCmd = cn.CreateCommand())
    {
        checkCmd.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME=@t";
        checkCmd.Parameters.Add(new SqlParameter("@t", SqlDbType.NVarChar, 128) { Value = table });
        var exists = await checkCmd.ExecuteScalarAsync();
        if (exists == null)
        {
            return Results.BadRequest(new { error = "Tabela não encontrada no banco selecionado." });
        }
    }

    using var cmd = cn.CreateCommand();
    cmd.CommandText = $"SELECT * FROM [{table}] ORDER BY 1 OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });

    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<Dictionary<string, object?>>();
    var schema = r.GetColumnSchema();
    while (await r.ReadAsync())
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in schema)
        {
            var ord = col.ColumnOrdinal ?? -1;
            if (ord < 0) continue;
            object? val = r.IsDBNull(ord) ? null : r.GetValue(ord);
            row[col.ColumnName ?? $"Col{ord + 1}"] = val;
        }
        items.Add(row);
    }

    return Results.Ok(new { page, pageSize, items });
}).RequireAuthorization("NotCliente");

app.MapPost("/api/admin/connections", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string,string>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    var changed = new Dictionary<string,string>();
    if (dto.TryGetValue("CMS", out var cms) && !string.IsNullOrWhiteSpace(cms))
    {
        realOverrides["CMS"] = cms;
        changed["DB_CMS_CONN"] = cms;
    }
    if (dto.TryGetValue("Logins", out var logins) && !string.IsNullOrWhiteSpace(logins))
    {
        realOverrides["Logins"] = logins;
        changed["DB_LOGINS_CONN"] = logins;
    }
    if (dto.TryGetValue("EMS", out var ems) && !string.IsNullOrWhiteSpace(ems))
    {
        realOverrides["EMS"] = ems;
        changed["DB_EMS_CONN"] = ems;
    }
    if (changed.Count > 0)
    {
        SaveEnv(changed);
    }
    return Results.Ok(new { CMS = realOverrides.GetValueOrDefault("CMS"), Logins = realOverrides.GetValueOrDefault("Logins"), EMS = realOverrides.GetValueOrDefault("EMS") });
}).RequireAuthorization("NotCliente");

app.MapPost("/api/admin/connections/runtime", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string,string>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    if (dto.TryGetValue("CMS", out var cms) && !string.IsNullOrWhiteSpace(cms)) realOverrides["CMS"] = cms;
    if (dto.TryGetValue("Logins", out var logins) && !string.IsNullOrWhiteSpace(logins)) realOverrides["Logins"] = logins;
    if (dto.TryGetValue("EMS", out var ems) && !string.IsNullOrWhiteSpace(ems)) realOverrides["EMS"] = ems;
    return Results.Ok(new { CMS = realOverrides.GetValueOrDefault("CMS"), Logins = realOverrides.GetValueOrDefault("Logins"), EMS = realOverrides.GetValueOrDefault("EMS") });
}).RequireAuthorization("NotCliente");

app.MapPost("/api/admin/sql-auth/runtime", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string,string>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    dto.TryGetValue("user", out sqlAuthUser);
    dto.TryGetValue("pwd", out sqlAuthPwd);
    return Results.Ok(new { user = sqlAuthUser, applied = !string.IsNullOrWhiteSpace(sqlAuthUser) });
}).RequireAuthorization();

app.MapPost("/api/admin/sql-auth", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string,string>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    dto.TryGetValue("user", out sqlAuthUser);
    dto.TryGetValue("pwd", out sqlAuthPwd);
    var changes = new Dictionary<string,string>();
    if (!string.IsNullOrWhiteSpace(sqlAuthUser)) changes["DB_SQL_USER"] = sqlAuthUser!;
    if (!string.IsNullOrWhiteSpace(sqlAuthPwd)) changes["DB_SQL_PWD"] = sqlAuthPwd!;
    if (changes.Count > 0) SaveEnv(changes);
    return Results.Ok(new { user = sqlAuthUser, persisted = changes.Count > 0 });
}).RequireAuthorization();
// Disponibiliza o seed sempre, com checagem de segurança interna
{
    app.MapPost("/api/dev/seed", async (int? count, string? scope) =>
    {
        if (!(app.Environment.IsDevelopment() || string.Equals(dbMode, "Demo", StringComparison.OrdinalIgnoreCase)))
        {
            return Results.BadRequest(new { error = "Seed permitido apenas em ambiente Development ou quando o modo de banco é Demo." });
        }
        var rnd = new Random();
        int qty = count.GetValueOrDefault(100);
        if (qty < 1) qty = 1;
        if (qty > 1000) qty = 1000;
        var sc = string.IsNullOrWhiteSpace(scope) ? "all" : scope!.ToLowerInvariant();

        var result = new Dictionary<string, int>();
        if (sc is "all" or "logins")
        {
            try
            {
                using var cn = new SqlConnection(GetConn("Logins"));
                await cn.OpenAsync();
                var insertedClientes = 0;
                for (int i = 1; i <= qty; i++)
                {
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM dbo.Clientes WHERE SBID=@id)
INSERT INTO dbo.Clientes(SBID,NOME,ENDERECO,FONE,EMAIL,SITE,ATIVO,CAMINHOIMG)
VALUES(@id,@n,@e,@f,@mail,@site,1,NULL)";
                    cmd.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = 50000 + i });
                    cmd.Parameters.Add(new SqlParameter("@n", System.Data.SqlDbType.VarChar, 200) { Value = $"Seed Cliente {i}" });
                    cmd.Parameters.Add(new SqlParameter("@e", System.Data.SqlDbType.VarChar, 300) { Value = $"Rua {i}, Centro" });
                    cmd.Parameters.Add(new SqlParameter("@f", System.Data.SqlDbType.VarChar, 50) { Value = $"(11) 5555-{1000 + i:D4}" });
                    cmd.Parameters.Add(new SqlParameter("@mail", System.Data.SqlDbType.VarChar, 200) { Value = $"cliente{i}@seed.local" });
                    cmd.Parameters.Add(new SqlParameter("@site", System.Data.SqlDbType.VarChar, 200) { Value = $"https://cliente{i}.seed" });
                    insertedClientes += await cmd.ExecuteNonQueryAsync();
                }
                result["logins.Clientes"] = insertedClientes;
            }
            catch
            {
                result["logins.error"] = -1;
            }
        }
        if (sc is "all" or "cms")
        {
            try
            {
                using var cn = new SqlConnection(GetConn("CMS"));
                await cn.OpenAsync();

                var empresas = new[] { "ACME", "GLOBEX", "INITECH", "UMBRELLA", "SOYLENT" };
                var terms = new[] { "T1", "T2", "T3", "T4", "T5", "T6" };

                var insertedVTerm = 0;
                foreach (var t in terms)
                {
                    using var cmdV = cn.CreateCommand();
                    cmdV.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM AC_VTERMINAL WHERE VTERMINAL_KEY=@k)
INSERT INTO AC_VTERMINAL(VTERMINAL_KEY,DESCRIPTION) VALUES(@k,@d)";
                    cmdV.Parameters.Add(new SqlParameter("@k", System.Data.SqlDbType.VarChar, 50) { Value = t });
                    cmdV.Parameters.Add(new SqlParameter("@d", System.Data.SqlDbType.VarChar, 200) { Value = $"Terminal {t}" });
                    insertedVTerm += await cmdV.ExecuteNonQueryAsync();
                }
                result["cms.AC_VTERMINAL"] = insertedVTerm;

                var insertedBehavior = 0;
                for (int i = 1; i <= 5; i++)
                {
                    using var cmdB = cn.CreateCommand();
                    cmdB.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM AC_BEHAVIOR WHERE BEHAVIOR_ID=@id)
INSERT INTO AC_BEHAVIOR(BEHAVIOR_ID,DESCRIPTION) VALUES(@id,@d)";
                    cmdB.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = 9000 + i });
                    cmdB.Parameters.Add(new SqlParameter("@d", System.Data.SqlDbType.VarChar, 200) { Value = $"Nível {i}" });
                    insertedBehavior += await cmdB.ExecuteNonQueryAsync();
                }
                result["cms.AC_BEHAVIOR"] = insertedBehavior;

                var insertedEmp = 0;
                var insertedEmpFields = 0;
                var insertedCards = 0;
                for (int i = 1; i <= qty; i++)
                {
                    int sbi = 10000 + i;
                    using var cmdE = cn.CreateCommand();
                    cmdE.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM Employee WHERE SbiID=@id)
INSERT INTO Employee(SbiID,Name) VALUES(@id,@name)";
                    cmdE.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = sbi });
                    cmdE.Parameters.Add(new SqlParameter("@name", System.Data.SqlDbType.VarChar, 200) { Value = $"Seed Emp {i}" });
                    insertedEmp += await cmdE.ExecuteNonQueryAsync();

                    using var cmdEU = cn.CreateCommand();
                    cmdEU.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM EmployeeUserFields WHERE SbiID=@id)
INSERT INTO EmployeeUserFields(SbiID,UF2) VALUES(@id,@uf2)";
                    cmdEU.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = sbi });
                    cmdEU.Parameters.Add(new SqlParameter("@uf2", System.Data.SqlDbType.VarChar, 50) { Value = empresas[i % empresas.Length] });
                    insertedEmpFields += await cmdEU.ExecuteNonQueryAsync();

                    using var cmdC = cn.CreateCommand();
                    cmdC.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM Card WHERE SbiID=@id)
INSERT INTO Card(SbiID,CardNumber) VALUES(@id,@card)";
                    cmdC.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = sbi });
                    cmdC.Parameters.Add(new SqlParameter("@card", System.Data.SqlDbType.VarChar, 50) { Value = $"9000{i:D6}" });
                    insertedCards += await cmdC.ExecuteNonQueryAsync();

                    using var cmdSB = cn.CreateCommand();
                    cmdSB.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM SbiSiteBehavior WHERE SbiID=@id AND Behavior=@b)
INSERT INTO SbiSiteBehavior(SbiID,Behavior) VALUES(@id,@b)";
                    cmdSB.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = sbi });
                    cmdSB.Parameters.Add(new SqlParameter("@b", System.Data.SqlDbType.Int) { Value = 9000 + ((i % 5) + 1) });
                    await cmdSB.ExecuteNonQueryAsync();
                }
                result["cms.Employee"] = insertedEmp;
                result["cms.EmployeeUserFields"] = insertedEmpFields;
                result["cms.Card"] = insertedCards;

                var insertedTransit = 0;
                for (int i = 1; i <= qty; i++)
                {
                    int sbi = 10000 + i;
                    int eventsPerUser = 10;
                    for (int e = 0; e < eventsPerUser; e++)
                    {
                        using var cmdT = cn.CreateCommand();
                        cmdT.CommandText = @"
INSERT INTO HA_TRANSIT(SBI_ID,TERMINAL,STR_DIRECTION,USER_TYPE,TRANSIT_DATE)
VALUES(@sbi,@term,@dir,@ut,@dt)";
                        cmdT.Parameters.Add(new SqlParameter("@sbi", System.Data.SqlDbType.Int) { Value = sbi });
                        cmdT.Parameters.Add(new SqlParameter("@term", System.Data.SqlDbType.VarChar, 50) { Value = terms[rnd.Next(terms.Length)] });
                        cmdT.Parameters.Add(new SqlParameter("@dir", System.Data.SqlDbType.VarChar, 8) { Value = rnd.Next(2) == 0 ? "IN" : "OUT" });
                        cmdT.Parameters.Add(new SqlParameter("@ut", System.Data.SqlDbType.VarChar, 8) { Value = "EMP" });
                        cmdT.Parameters.Add(new SqlParameter("@dt", System.Data.SqlDbType.DateTime) { Value = DateTime.UtcNow.AddMinutes(-rnd.Next(60 * 24 * 30)) });
                        insertedTransit += await cmdT.ExecuteNonQueryAsync();
                    }
                }
                result["cms.HA_TRANSIT"] = insertedTransit;

                var insertedExt = 0;
                var insertedExtUF = 0;
                var insertedExtCard = 0;
                for (int i = 1; i <= qty; i++)
                {
                    int sbi = 30000 + i;
                    using var cmdX = cn.CreateCommand();
                    cmdX.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM ExternalRegular WHERE SbiID=@id)
INSERT INTO ExternalRegular(SbiID,Name,Surname,PreferredName,Identifier) VALUES(@id,@n,@sn,@pn,@ident)";
                    cmdX.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = sbi });
                    cmdX.Parameters.Add(new SqlParameter("@n", System.Data.SqlDbType.VarChar, 200) { Value = $"Ext Nome {i}" });
                    cmdX.Parameters.Add(new SqlParameter("@sn", System.Data.SqlDbType.VarChar, 200) { Value = $"Sobrenome {i}" });
                    cmdX.Parameters.Add(new SqlParameter("@pn", System.Data.SqlDbType.VarChar, 200) { Value = $"Apelido {i}" });
                    cmdX.Parameters.Add(new SqlParameter("@ident", System.Data.SqlDbType.VarChar, 50) { Value = $"EXT{i:D6}" });
                    insertedExt += await cmdX.ExecuteNonQueryAsync();

                    using var cmdXU = cn.CreateCommand();
                    cmdXU.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM ExternalRegularUserFields WHERE SbiID=@id)
INSERT INTO ExternalRegularUserFields(SbiID,UF2) VALUES(@id,@uf2)";
                    cmdXU.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = sbi });
                    cmdXU.Parameters.Add(new SqlParameter("@uf2", System.Data.SqlDbType.VarChar, 50) { Value = empresas[i % empresas.Length] });
                    insertedExtUF += await cmdXU.ExecuteNonQueryAsync();

                    using var cmdXC = cn.CreateCommand();
                    cmdXC.CommandText = @"
IF NOT EXISTS(SELECT 1 FROM Card WHERE SbiID=@id)
INSERT INTO Card(SbiID,CardNumber) VALUES(@id,@card)";
                    cmdXC.Parameters.Add(new SqlParameter("@id", System.Data.SqlDbType.Int) { Value = sbi });
                    cmdXC.Parameters.Add(new SqlParameter("@card", System.Data.SqlDbType.VarChar, 50) { Value = $"8000{i:D6}" });
                    insertedExtCard += await cmdXC.ExecuteNonQueryAsync();
                }
                result["cms.ExternalRegular"] = insertedExt;
                result["cms.ExternalRegularUserFields"] = insertedExtUF;
                result["cms.Card.external"] = insertedExtCard;
            }
            catch
            {
                result["cms.error"] = -1;
            }
        }

        return Results.Ok(result);
    }).RequireAuthorization("NotCliente");
}

// Seed específico com empresas solicitadas e acessos dos últimos 30 dias (dias úteis)
app.MapPost("/api/dev/seed-companies", async () =>
{
    var companies = new[] { "Bain Company", "Santander", "Bradesco", "Itau", "HSBC", "XP Investimentos" };
    var firstNames = new[] { "Ana", "Bruno", "Carla", "Diego", "Eduarda", "Felipe", "Gabriela", "Henrique", "Isabela", "João", "Karen", "Lucas", "Mariana", "Nicolas", "Olivia", "Paulo", "Renata", "Sergio", "Tatiana", "Vinicius", "Yasmin", "Zeca" };
    var lastNames = new[] { "Silva", "Souza", "Oliveira", "Pereira", "Costa", "Almeida", "Ferreira", "Gomes", "Rodrigues", "Santana", "Barbosa", "Carvalho", "Mendes", "Ribeiro" };

    try
    {
        using var cn = new SqlConnection(GetConn("CMS"));
        await cn.OpenAsync();

        // Garantir terminais de catraca
        foreach (var (k, d) in new[] { ("T1", "Catraca T1"), ("T2", "Catraca T2") })
        {
            using var cmdV = cn.CreateCommand();
            cmdV.CommandText = @"IF NOT EXISTS(SELECT 1 FROM AC_VTERMINAL WHERE VTERMINAL_KEY=@k)
INSERT INTO AC_VTERMINAL(VTERMINAL_KEY,DESCRIPTION) VALUES(@k,@d)";
            cmdV.Parameters.Add(new SqlParameter("@k", SqlDbType.VarChar, 50) { Value = k });
            cmdV.Parameters.Add(new SqlParameter("@d", SqlDbType.VarChar, 200) { Value = d });
            await cmdV.ExecuteNonQueryAsync();
        }

        // Criar ~20 funcionários ligados às empresas
        var createdEmployees = 0;
        var createdFields = 0;
        var createdCards = 0;
        var rnd = new Random();
        var totalEmployees = 20;
        var baseSbi = 20000;
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < totalEmployees; i++)
        {
            var sbi = baseSbi + i + 1;
            var company = companies[i % companies.Length];
            string name;
            for (;;)
            {
                name = $"{firstNames[rnd.Next(firstNames.Length)]} {lastNames[rnd.Next(lastNames.Length)]}";
                if (usedNames.Add(name)) break;
            }

            using (var cmdE = cn.CreateCommand())
            {
                cmdE.CommandText = @"IF NOT EXISTS(SELECT 1 FROM Employee WHERE SbiID=@id)
INSERT INTO Employee(SbiID,Name) VALUES(@id,@n)";
                cmdE.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = sbi });
                cmdE.Parameters.Add(new SqlParameter("@n", SqlDbType.VarChar, 200) { Value = name });
                createdEmployees += await cmdE.ExecuteNonQueryAsync();
            }
            using (var cmdEU = cn.CreateCommand())
            {
                cmdEU.CommandText = @"IF NOT EXISTS(SELECT 1 FROM EmployeeUserFields WHERE SbiID=@id)
INSERT INTO EmployeeUserFields(SbiID,UF2) VALUES(@id,@c)";
                cmdEU.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = sbi });
                cmdEU.Parameters.Add(new SqlParameter("@c", SqlDbType.VarChar, 100) { Value = company });
                createdFields += await cmdEU.ExecuteNonQueryAsync();
            }
            using (var cmdC = cn.CreateCommand())
            {
                cmdC.CommandText = @"IF NOT EXISTS(SELECT 1 FROM Card WHERE SbiID=@id)
INSERT INTO Card(SbiID,CardNumber) VALUES(@id,@card)";
                cmdC.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = sbi });
                cmdC.Parameters.Add(new SqlParameter("@card", SqlDbType.VarChar, 50) { Value = $"7{sbi:D7}" });
                createdCards += await cmdC.ExecuteNonQueryAsync();
            }
        }

        // Acessos por dia útil nos últimos 30 dias: 4 eventos (entrada, saída almoço, volta, saída final)
        var createdTransits = 0;
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-30);
        var employees = Enumerable.Range(1, totalEmployees).Select(i => baseSbi + i).ToArray();

        // Limpa possíveis duplicatas anteriores no intervalo para esses SBIs
        using (var cmdDel = cn.CreateCommand())
        {
            cmdDel.CommandText = @"DELETE FROM HA_TRANSIT WHERE TRANSIT_DATE >= @start AND TRANSIT_DATE < @end AND SBI_ID BETWEEN @sbiStart AND @sbiEnd";
            cmdDel.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = startDate });
            cmdDel.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = endDate.AddDays(1) });
            cmdDel.Parameters.Add(new SqlParameter("@sbiStart", SqlDbType.Int) { Value = baseSbi + 1 });
            cmdDel.Parameters.Add(new SqlParameter("@sbiEnd", SqlDbType.Int) { Value = baseSbi + totalEmployees });
            await cmdDel.ExecuteNonQueryAsync();
        }

        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            foreach (var sbi in employees)
            {
                DateTime tIn = new DateTime(d.Year, d.Month, d.Day, 8, 0, 0).AddMinutes(rnd.Next(0, 10));
                DateTime tLunchOut = new DateTime(d.Year, d.Month, d.Day, 12, 0, 0).AddMinutes(rnd.Next(0, 10));
                DateTime tLunchIn = new DateTime(d.Year, d.Month, d.Day, 13, 0, 0).AddMinutes(rnd.Next(0, 10));
                DateTime tOut = new DateTime(d.Year, d.Month, d.Day, 17, 30, 0).AddMinutes(rnd.Next(0, 15));

                foreach (var (dt, dir, term) in new (DateTime dt, string dir, string term)[] {
                    (tIn, "IN", "T1"),
                    (tLunchOut, "OUT", "T2"),
                    (tLunchIn, "IN", "T1"),
                    (tOut, "OUT", "T2")
                })
                {
                    using var cmdT = cn.CreateCommand();
                    cmdT.CommandText = @"INSERT INTO HA_TRANSIT(SBI_ID,TERMINAL,STR_DIRECTION,USER_TYPE,TRANSIT_DATE) VALUES(@sbi,@term,@dir,@ut,@dt)";
                    cmdT.Parameters.Add(new SqlParameter("@sbi", SqlDbType.Int) { Value = sbi });
                    cmdT.Parameters.Add(new SqlParameter("@term", SqlDbType.VarChar, 50) { Value = term });
                    cmdT.Parameters.Add(new SqlParameter("@dir", SqlDbType.VarChar, 8) { Value = dir });
                    cmdT.Parameters.Add(new SqlParameter("@ut", SqlDbType.VarChar, 8) { Value = "EMP" });
                    cmdT.Parameters.Add(new SqlParameter("@dt", SqlDbType.DateTime) { Value = dt });
                    createdTransits += await cmdT.ExecuteNonQueryAsync();
                }
            }
        }

        return Results.Ok(new { employees = createdEmployees, fields = createdFields, cards = createdCards, transits = createdTransits, companies = companies.Length });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("NotCliente");

app.MapGet("/api/login/check", async (string token) =>
{
    token = (token ?? "").Trim();
    string? nome = null, usuario = null, nivel = "Leitor";
    try
    {
        using var cn = new SqlConnection(GetConn("Logins"));
        await cn.OpenAsync();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT NOME, USUARIO FROM dbo.Login WHERE TOKEN=@t AND STATUS='Habilitado'";
        cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = token });
        using var r = await cmd.ExecuteReaderAsync();
        if (r.HasRows)
        {
            await r.ReadAsync();
            nome = r.GetString(0);
            usuario = r.GetString(1);
        }
    }
    catch
    {
    }
    if (nome == null || usuario == null)
    {
        var fallback = new Dictionary<string, (string usuario, string nome, string nivel)>(StringComparer.OrdinalIgnoreCase)
        {
            ["011"] = ("admin","EVERTON","Administrador"),
            ["022"] = ("user","EVERTON","Padrão"),
            ["021"] = ("gerente","ALANA","Padrão"),
            ["031"] = ("basico","ALANA","Básico")
        };
        if (fallback.ContainsKey(token))
        {
            var f = fallback[token];
            usuario = f.usuario; nome = f.nome; nivel = f.nivel;
        }
        else
        {
            return Results.NotFound();
        }
    }
    if (int.TryParse(token, out var tv))
    {
        if (tv >= 0 && tv <= 10) nivel = "SuperAdmin";
        else if (tv <= 20) nivel = "Administrador";
        else if (tv <= 30) nivel = "Padrão";
        else if (tv <= 40) nivel = "Leitor";
    }
    return Results.Ok(new { nome, usuario, nivel });
});

app.MapGet("/api/clientes", async (HttpContext ctx) =>
{
    var list = new List<object>();
    try
    {
        using var cn = new SqlConnection(GetConn("Logins"));
        await cn.OpenAsync();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id,NOME,ENDERECO,FONE,EMAIL,SITE,ATIVO,CAMINHOIMG,RESPONSAVEL,CLIENT_TOKEN FROM dbo.ClientesPortal";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            string? img = r.IsDBNull(7) ? null : r.GetString(7);
            if (!string.IsNullOrEmpty(img) && !img.Contains("://", StringComparison.Ordinal))
            {
                var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";
                if (img.StartsWith("/", StringComparison.Ordinal))
                    img = baseUrl + img;
                else
                    img = baseUrl + "/" + img;
            }
            list.Add(new
            {
                SBID = r.GetInt32(0),
                NOME = r.IsDBNull(1) ? null : r.GetString(1),
                ENDERECO = r.IsDBNull(2) ? null : r.GetString(2),
                FONE = r.IsDBNull(3) ? null : r.GetString(3),
                EMAIL = r.IsDBNull(4) ? null : r.GetString(4),
                SITE = r.IsDBNull(5) ? null : r.GetString(5),
                ATIVO = r.IsDBNull(6) ? (int?)null : r.GetInt32(6),
                CAMINHOIMG = img,
                RESPONSAVEL = r.IsDBNull(8) ? null : r.GetString(8),
                TOKEN = r.IsDBNull(9) ? null : r.GetString(9)
            });
        }
    }
    catch
    {
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/client/current", async (HttpContext ctx) =>
{
    var clientIdHeader = ctx.Request.Headers.TryGetValue("X-Client-Id", out var vals) ? vals.ToString() : null;
    int cid;
    if (!int.TryParse(clientIdHeader, out cid) || cid <= 0)
    {
        var claim = ctx.User?.FindFirst("clientId");
        if (claim == null || !int.TryParse(claim.Value, out cid) || cid <= 0)
        {
            return Results.Ok(new { id = (int?)null, nome = (string?)null, responsavel = (string?)null, logoPath = (string?)null });
        }
    }
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = "SELECT Id,NOME,RESPONSAVEL,CAMINHOIMG,CLIENT_TOKEN,ENDERECO,FONE,EMAIL,SITE FROM dbo.ClientesPortal WHERE Id=@id";
    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
    using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync())
    {
        return Results.Ok(new { id = (int?)null, nome = (string?)null, responsavel = (string?)null, logoPath = (string?)null, clientToken = (string?)null, endereco = (string?)null, fone = (string?)null, email = (string?)null, site = (string?)null });
    }
    var id = r.GetInt32(0);
    var nome = r.IsDBNull(1) ? null : r.GetString(1);
    var resp = r.IsDBNull(2) ? null : r.GetString(2);
    var logo = r.IsDBNull(3) ? null : r.GetString(3);
    var token = r.IsDBNull(4) ? null : r.GetString(4);
    var endereco = r.IsDBNull(5) ? null : r.GetString(5);
    var fone = r.IsDBNull(6) ? null : r.GetString(6);
    var email = r.IsDBNull(7) ? null : r.GetString(7);
    var site = r.IsDBNull(8) ? null : r.GetString(8);
    if (!string.IsNullOrEmpty(logo) && !logo.Contains("://", StringComparison.Ordinal))
    {
        var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";
        if (logo.StartsWith("/", StringComparison.Ordinal))
            logo = baseUrl + logo;
        else
            logo = baseUrl + "/" + logo;
    }
    return Results.Ok(new { id, nome, responsavel = resp, logoPath = logo, clientToken = token, endereco, fone, email, site });
}).RequireAuthorization();

app.MapPost("/api/admin/clients", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    var nomeDto = GetDtoString(dto, "nome");
    if (string.IsNullOrWhiteSpace(nomeDto ?? "")) return Results.BadRequest(new { error = "Nome é obrigatório" });
    using var cn = new SqlConnection(GetConn("Logins"));
    try
    {
        await cn.OpenAsync();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = "Falha ao conectar ao banco Logins. Ajuste em Configurações > Banco Real ou instale o LocalDB.", detail = ex.Message });
    }
    string token = (GetDtoString(dto, "token") ?? "").Trim();
    bool needGen = string.IsNullOrEmpty(token);
    if (!needGen)
    {
        using var chk = cn.CreateCommand();
        chk.CommandText = "SELECT COUNT(*) FROM dbo.ClientesPortal WHERE RTRIM(LTRIM(CLIENT_TOKEN))=@t";
        chk.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = token });
        var count = (int)(await chk.ExecuteScalarAsync() ?? 0);
        if (count > 0) needGen = true;
    }
    if (needGen)
    {
        var rnd = new Random();
        for (int i = 0; i < 200; i++)
        {
            token = rnd.Next(0, 10000).ToString("D4");
            using var chk = cn.CreateCommand();
            chk.CommandText = "SELECT COUNT(*) FROM dbo.ClientesPortal WHERE CLIENT_TOKEN=@t";
            chk.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = token });
            var count = (int)(await chk.ExecuteScalarAsync() ?? 0);
            if (count == 0) break;
        }
    }
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
INSERT INTO dbo.ClientesPortal (NOME,ENDERECO,FONE,EMAIL,SITE,ATIVO,CAMINHOIMG,RESPONSAVEL,CLIENT_TOKEN)
OUTPUT INSERTED.Id
VALUES (@NOME,@ENDERECO,@FONE,@EMAIL,@SITE,@ATIVO,@CAMINHOIMG,@RESPONSAVEL,@CLIENT_TOKEN)";
    var enderecoDto = GetDtoString(dto, "endereco");
    var foneDto = GetDtoString(dto, "fone");
    var emailDto = GetDtoString(dto, "email");
    var siteDto = GetDtoString(dto, "site");
    var ativoDto = GetDtoInt(dto, "ativo", 1);
    var logoDto = GetDtoString(dto, "logoPath");
    var respDto = GetDtoString(dto, "responsavel");
    cmd.Parameters.Add(new SqlParameter("@NOME", SqlDbType.VarChar, 100) { Value = (object?)nomeDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ENDERECO", SqlDbType.VarChar, 200) { Value = (object?)enderecoDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@FONE", SqlDbType.VarChar, 50) { Value = (object?)foneDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@EMAIL", SqlDbType.VarChar, 100) { Value = (object?)emailDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@SITE", SqlDbType.VarChar, 100) { Value = (object?)siteDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ATIVO", SqlDbType.Int) { Value = ativoDto });
    cmd.Parameters.Add(new SqlParameter("@CAMINHOIMG", SqlDbType.VarChar, 255) { Value = (object?)logoDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@RESPONSAVEL", SqlDbType.VarChar, 100) { Value = (object?)respDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@CLIENT_TOKEN", SqlDbType.VarChar, 10) { Value = string.IsNullOrEmpty(token) ? (object)DBNull.Value : token });
    try
    {
        var id = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return Results.Ok(new { id, token });
    }
    catch (SqlException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("SuperAdminOnly");

app.MapPut("/api/admin/clients/{id:int}", async (int id, HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
UPDATE dbo.ClientesPortal SET
NOME=@NOME, ENDERECO=@ENDERECO, FONE=@FONE, EMAIL=@EMAIL, SITE=@SITE, ATIVO=@ATIVO, CAMINHOIMG=@CAMINHOIMG, RESPONSAVEL=@RESPONSAVEL
WHERE Id=@ID";
    cmd.Parameters.Add(new SqlParameter("@ID", SqlDbType.Int) { Value = id });
    var nomeU = GetDtoString(dto, "nome");
    var enderecoU = GetDtoString(dto, "endereco");
    var foneU = GetDtoString(dto, "fone");
    var emailU = GetDtoString(dto, "email");
    var siteU = GetDtoString(dto, "site");
    var ativoU = GetDtoInt(dto, "ativo", 1);
    var logoU = GetDtoString(dto, "logoPath");
    var respU = GetDtoString(dto, "responsavel");
    cmd.Parameters.Add(new SqlParameter("@NOME", SqlDbType.VarChar, 100) { Value = (object?)nomeU ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ENDERECO", SqlDbType.VarChar, 200) { Value = (object?)enderecoU ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@FONE", SqlDbType.VarChar, 50) { Value = (object?)foneU ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@EMAIL", SqlDbType.VarChar, 100) { Value = (object?)emailU ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@SITE", SqlDbType.VarChar, 100) { Value = (object?)siteU ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ATIVO", SqlDbType.Int) { Value = ativoU });
    cmd.Parameters.Add(new SqlParameter("@CAMINHOIMG", SqlDbType.VarChar, 255) { Value = (object?)logoU ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@RESPONSAVEL", SqlDbType.VarChar, 100) { Value = (object?)respU ?? DBNull.Value });
    var n = await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { updated = n });
}).RequireAuthorization("SuperAdminOnly");

app.MapDelete("/api/admin/clients/{id:int}", async (int id) =>
{
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using (var cmd = cn.CreateCommand())
    {
        cmd.CommandText = "DELETE FROM dbo.ClientesPortal WHERE Id=@id";
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        var n = await cmd.ExecuteNonQueryAsync();
        if (n == 0) return Results.NotFound(new { error = "Cliente não encontrado" });
    }
    try
    {
        var dir = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "clients");
        var path = Path.Combine(dir, $"{id}.png");
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
    catch
    {
    }
    return Results.Ok(new { deleted = true });
}).RequireAuthorization("SuperAdminOnly");

app.MapPost("/api/admin/clients/{id:int}/token", async (int id) =>
{
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    string token = "";
    var rnd = new Random();
    for (int i = 0; i < 100; i++)
    {
        token = rnd.Next(0, 10000).ToString("D4");
        using var chk = cn.CreateCommand();
        chk.CommandText = "SELECT COUNT(*) FROM dbo.ClientesPortal WHERE CLIENT_TOKEN=@t AND Id<>@id";
        chk.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = token });
        chk.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        var count = (int)(await chk.ExecuteScalarAsync() ?? 0);
        if (count == 0) break;
    }
    using var cmd = cn.CreateCommand();
    cmd.CommandText = "UPDATE dbo.ClientesPortal SET CLIENT_TOKEN=@t WHERE Id=@id";
    cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = token });
    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { token });
}).RequireAuthorization("SuperAdminOnly");

app.MapPost("/api/admin/clients/{id:int}/logo", async (int id, HttpRequest req) =>
{
    if (!req.HasFormContentType) return Results.BadRequest(new { error = "FormData esperado" });
    var form = await req.ReadFormAsync();
    var file = form.Files["file"];
    if (file == null || file.Length == 0) return Results.BadRequest(new { error = "Arquivo inválido" });
    var dir = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "clients");
    Directory.CreateDirectory(dir);
    var path = Path.Combine(dir, $"{id}.png");
    using (var fs = System.IO.File.Open(path, FileMode.Create, FileAccess.Write))
    {
        await file.CopyToAsync(fs);
    }
    var rel = "/clients/" + $"{id}.png";
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = "UPDATE dbo.ClientesPortal SET CAMINHOIMG=@p WHERE Id=@id";
    cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.VarChar, 255) { Value = rel });
    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { logoPath = rel });
}).RequireAuthorization("SuperAdminOnly");

app.MapGet("/api/reports/access/aggregated", async () =>
{
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT b.BEHAVIOR_ID, b.DESCRIPTION, COUNT(*) AS Total
FROM SbiSiteBehavior sb
INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
GROUP BY b.BEHAVIOR_ID, b.DESCRIPTION
ORDER BY b.BEHAVIOR_ID";
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new { LevelId = r.GetInt32(0), Level = r.GetString(1), Total = r.GetInt32(2) });
    }
    return Results.Ok(items);
}).RequireAuthorization();

app.MapGet("/api/reports/access/aggregated/export", async (HttpContext ctx, string format) =>
{
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT b.BEHAVIOR_ID, b.DESCRIPTION, COUNT(*) AS Total
FROM SbiSiteBehavior sb
INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
GROUP BY b.BEHAVIOR_ID, b.DESCRIPTION
ORDER BY b.BEHAVIOR_ID";
    using var r = await cmd.ExecuteReaderAsync();
    var rows = new List<(int id, string level, int total)>();
    while (await r.ReadAsync())
    {
        rows.Add((r.GetInt32(0), r.GetString(1), r.GetInt32(2)));
    }
    if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
    {
        var sb = new StringBuilder();
        sb.AppendLine("LevelId,Level,Total");
        foreach (var x in rows) sb.AppendLine($"{x.id},{Escape(x.level)},{x.total}");
        return Results.File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "access-by-level.csv");
    }
    if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
            var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = doc.WorkbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "Access By Level" });
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
            void AddRow(params string[] cells)
            {
                var row = new Row();
                foreach (var c in cells) row.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c) });
                sheetData.Append(row);
            }
            AddRow("LevelId","Level","Total");
            foreach (var x in rows) AddRow(x.id.ToString(), x.level, x.total.ToString());
        }
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "access-by-level.xlsx");
    }
    if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
    {
        QuestPDF.Settings.License = LicenseType.Community;
        byte[]? leftLogo = null;
        byte[]? rightLogo = null;
        string? clientNm = null;
        try
        {
            var clientIdHeader = ctx.Request.Headers.TryGetValue("X-Client-Id", out var vals) ? vals.ToString() : null;
            if (int.TryParse(clientIdHeader, out var cid))
            {
                using var cnL = new SqlConnection(GetConn("Logins"));
                await cnL.OpenAsync();
                using var cmdL = cnL.CreateCommand();
                cmdL.CommandText = "SELECT NOME,CAMINHOIMG FROM dbo.ClientesPortal WHERE Id=@id";
                cmdL.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
                using var rL = await cmdL.ExecuteReaderAsync();
                if (rL.HasRows)
                {
                    await rL.ReadAsync();
                    clientNm = rL.IsDBNull(0) ? null : rL.GetString(0);
                    var p = rL.IsDBNull(1) ? null : rL.GetString(1);
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        var full = p.StartsWith("/") ? Path.Combine(app.Environment.ContentRootPath, "wwwroot", p.TrimStart('/')) : p;
                        if (System.IO.File.Exists(full))
                        {
                            leftLogo = await System.IO.File.ReadAllBytesAsync(full);
                        }
                    }
                }
            }
        }
        catch { }
        try
        {
            var rightPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "images-legacy", "Logo_Principal_Fundo2.png");
            if (System.IO.File.Exists(rightPath))
            {
                rightLogo = await System.IO.File.ReadAllBytesAsync(rightPath);
            }
        }
        catch { }
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Row(row =>
                {
                    row.RelativeColumn().AlignLeft().Element(e =>
                    {
                        if (leftLogo != null) e.Image(leftLogo, ImageScaling.FitWidth);
                    });
                    row.RelativeColumn().AlignCenter().Text("Acessos agregados por nível").SemiBold().FontSize(18);
                    row.RelativeColumn().AlignRight().Element(e =>
                    {
                        if (rightLogo != null) e.Image(rightLogo, ImageScaling.FitWidth);
                    });
                });
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                    table.Cell().Text("LevelId"); table.Cell().Text("Level"); table.Cell().Text("Total");
                    foreach (var x in rows)
                    {
                        table.Cell().Text(x.id.ToString());
                        table.Cell().Text(x.level);
                        table.Cell().Text(x.total.ToString());
                    }
                });
            });
        }).GeneratePdf();
        return Results.File(bytes, "application/pdf", "access-by-level.pdf");
    }
    return Results.BadRequest(new { error = "Formato inválido" });
    static string Escape(string? s) => s == null ? "" : s.Contains(',') ? $"\"{s.Replace("\"","\"\"")}\"" : s;
}).RequireAuthorization();
app.MapGet("/api/reports/transit/aggregated", async (string start, string end, string? empresa) =>
{
    var startDt = ParseDate(start);
    var endDt = ParseDate(end);
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = "WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = startDt });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = endDt });
    if (!string.IsNullOrWhiteSpace(empresa))
    {
        where += " AND u.UF2 = @empresa";
        cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa });
    }
    cmd.CommandText = $@"
SELECT u.UF2 as Empresa, t.TERMINAL, COUNT(*) AS Total
FROM HA_TRANSIT t
INNER JOIN Employee e ON e.SbiID = t.SBI_ID
LEFT JOIN EmployeeUserFields u ON u.SbiID = e.SbiID
{where}
GROUP BY u.UF2, t.TERMINAL
ORDER BY u.UF2, t.TERMINAL";
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new { Empresa = r.IsDBNull(0) ? null : r.GetString(0), Terminal = r.IsDBNull(1) ? null : r.GetString(1), Total = r.GetInt32(2) });
    }
    return Results.Ok(items);
}).RequireAuthorization();

// ----------------------------------------------------------------
// new report: door critical events generated by jp4_sp_DoorCritical
// returns the same columns produced by the stored procedure
app.MapGet("/api/reports/door-critical", async (string start, string end) =>
{
    try
    {
        var startDt = ParseDate(start);
        var endDt = ParseDate(end);
        using var cn = new SqlConnection(GetConn("EMS"));
        await cn.OpenAsync();
        using var cmd = cn.CreateCommand();
        // call the proc; it already contains the complicated union logic
        cmd.CommandText = "EXEC dbo.jp4_sp_DoorCritical @DataInicio, @DataFim";
        cmd.Parameters.Add(new SqlParameter("@DataInicio", SqlDbType.VarChar, 20) { Value = startDt.ToString("yyyy-MM-ddTHH:mm:ss") });
        cmd.Parameters.Add(new SqlParameter("@DataFim", SqlDbType.VarChar, 20) { Value = endDt.ToString("yyyy-MM-ddTHH:mm:ss") });
        try
        {
            using var r = await cmd.ExecuteReaderAsync();
            var items = new List<object>();
            while (await r.ReadAsync())
            {
                items.Add(new
                {
                    EventID = r.GetInt32(0),
                    TimeOrder = r.IsDBNull(1) ? (DateTime?)null : r.GetDateTime(1),
                    DataHora = r.IsDBNull(2) ? null : r.GetString(2),
                    TAG = r.IsDBNull(3) ? null : r.GetString(3),
                    Acesso = r.IsDBNull(4) ? null : r.GetString(4),
                    Evento = r.IsDBNull(5) ? null : r.GetString(5),
                    NomeCompleto = r.IsDBNull(6) ? null : r.GetString(6),
                    DocumentoMatricula = r.IsDBNull(7) ? null : r.GetString(7),
                    Cartao = r.IsDBNull(8) ? null : r.GetString(8),
                    Tipo = r.IsDBNull(9) ? null : r.GetString(9),
                    Empresa = r.IsDBNull(10) ? null : r.GetString(10),
                    StatusAcesso = r.IsDBNull(11) ? null : r.GetString(11),
                    DetalheStatusAcesso = r.IsDBNull(12) ? null : r.GetString(12)
                });
            }
            return Results.Ok(new { success = true, count = items.Count, data = items });
        }
        catch (SqlException ex) when (ex.Message.Contains("Could not find stored procedure", StringComparison.OrdinalIgnoreCase))
        {
            using var cn2 = new SqlConnection(GetConn("CMS"));
            await cn2.OpenAsync();
            using var cmd2 = cn2.CreateCommand();
            cmd2.CommandText = @"
SELECT
    ROW_NUMBER() OVER (ORDER BY t.TRANSIT_DATE) AS EventID,
    t.TRANSIT_DATE AS TimeOrder,
    CONVERT(varchar(19), t.TRANSIT_DATE, 120) AS DataHora,
    ISNULL(c.CardNumber,'') AS TAG,
    ISNULL(v.DESCRIPTION,'') AS Acesso,
    CASE t.STR_DIRECTION WHEN 'Entry' THEN 'ENTRADA' WHEN 'Exit' THEN 'SAÍDA' ELSE ISNULL(t.STR_DIRECTION,'') END AS Evento,
    ISNULL(e.Name + ' ' + e.Surname, ISNULL(x.Name + ' ' + x.Surname,'')) AS NomeCompleto,
    ISNULL(e.Identifier, ISNULL(x.Identifier,'')) AS DocumentoMatricula,
    ISNULL(c.CardNumber,'') AS Cartao,
    CASE t.USER_TYPE WHEN 'Employee' THEN 'FUNCIONÁRIO' WHEN 'External Personnel' THEN 'TERCEIRO' ELSE ISNULL(t.USER_TYPE,'') END AS Tipo,
    ISNULL(ue.UF2, ISNULL(ux.UF2,'')) AS Empresa,
    CAST(NULL AS varchar(50)) AS StatusAcesso,
    CAST(NULL AS varchar(100)) AS DetalheStatusAcesso
FROM HA_TRANSIT t
LEFT JOIN Employee e ON e.SbiID = t.SBI_ID
LEFT JOIN EmployeeUserFields ue ON ue.SbiID = e.SbiID
LEFT JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
LEFT JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
LEFT JOIN Card c ON c.SbiID = ISNULL(e.SbiID, x.SbiID)
LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
";
            cmd2.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = startDt });
            cmd2.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = endDt });
            using var r2 = await cmd2.ExecuteReaderAsync();
            var items = new List<object>();
            while (await r2.ReadAsync())
            {
                items.Add(new
                {
                    EventID = r2.GetInt32(0),
                    TimeOrder = r2.IsDBNull(1) ? (DateTime?)null : r2.GetDateTime(1),
                    DataHora = r2.IsDBNull(2) ? null : r2.GetString(2),
                    TAG = r2.IsDBNull(3) ? null : r2.GetString(3),
                    Acesso = r2.IsDBNull(4) ? null : r2.GetString(4),
                    Evento = r2.IsDBNull(5) ? null : r2.GetString(5),
                    NomeCompleto = r2.IsDBNull(6) ? null : r2.GetString(6),
                    DocumentoMatricula = r2.IsDBNull(7) ? null : r2.GetString(7),
                    Cartao = r2.IsDBNull(8) ? null : r2.GetString(8),
                    Tipo = r2.IsDBNull(9) ? null : r2.GetString(9),
                    Empresa = r2.IsDBNull(10) ? null : r2.GetString(10),
                    StatusAcesso = r2.IsDBNull(11) ? null : r2.GetString(11),
                    DetalheStatusAcesso = r2.IsDBNull(12) ? null : r2.GetString(12)
                });
            }
            return Results.Ok(new { success = true, count = items.Count, data = items });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message, innerError = ex.InnerException?.Message });
    }
});

app.MapGet("/api/reports/door-critical/export", async (HttpContext ctx, string start, string end, string format) =>
{
    var startDt = ParseDate(start);
    var endDt = ParseDate(end);
    // exports same data in csv/xlsx/pdf just like the other report endpoints
    using var cn = new SqlConnection(GetConn("EMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = "EXEC dbo.jp4_sp_DoorCritical @DataInicio, @DataFim";
    cmd.Parameters.Add(new SqlParameter("@DataInicio", SqlDbType.VarChar, 20) { Value = startDt.ToString("yyyy-MM-ddTHH:mm:ss") });
    cmd.Parameters.Add(new SqlParameter("@DataFim", SqlDbType.VarChar, 20) { Value = endDt.ToString("yyyy-MM-ddTHH:mm:ss") });

    List<(int EventID, DateTime? TimeOrder, string DataHora, string TAG, string Acesso, string Evento, string NomeCompleto, string DocumentoMatricula, string Cartao, string Tipo, string Empresa, string StatusAcesso, string DetalheStatusAcesso)> rows;
    try
    {
        using var r = await cmd.ExecuteReaderAsync();
        rows = new List<(int, DateTime?, string, string, string, string, string, string, string, string, string, string, string)>();
        while (await r.ReadAsync())
        {
            rows.Add((
                r.GetInt32(0),
                r.IsDBNull(1) ? (DateTime?)null : r.GetDateTime(1),
                r.IsDBNull(2) ? null : r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                r.IsDBNull(6) ? null : r.GetString(6),
                r.IsDBNull(7) ? null : r.GetString(7),
                r.IsDBNull(8) ? null : r.GetString(8),
                r.IsDBNull(9) ? null : r.GetString(9),
                r.IsDBNull(10) ? null : r.GetString(10),
                r.IsDBNull(11) ? null : r.GetString(11),
                r.IsDBNull(12) ? null : r.GetString(12)
            ));
        }
    }
    catch (SqlException ex) when (ex.Message.Contains("Could not find stored procedure", StringComparison.OrdinalIgnoreCase))
    {
        using var cn2 = new SqlConnection(GetConn("CMS"));
        await cn2.OpenAsync();
        using var cmd2 = cn2.CreateCommand();
        cmd2.CommandText = @"
SELECT
    ROW_NUMBER() OVER (ORDER BY t.TRANSIT_DATE) AS EventID,
    t.TRANSIT_DATE AS TimeOrder,
    CONVERT(varchar(19), t.TRANSIT_DATE, 120) AS DataHora,
    ISNULL(c.CardNumber,'') AS TAG,
    ISNULL(v.DESCRIPTION,'') AS Acesso,
    CASE t.STR_DIRECTION WHEN 'Entry' THEN 'ENTRADA' WHEN 'Exit' THEN 'SAÍDA' ELSE ISNULL(t.STR_DIRECTION,'') END AS Evento,
    ISNULL(e.Name + ' ' + e.Surname, ISNULL(x.Name + ' ' + x.Surname,'')) AS NomeCompleto,
    ISNULL(e.Identifier, ISNULL(x.Identifier,'')) AS DocumentoMatricula,
    ISNULL(c.CardNumber,'') AS Cartao,
    CASE t.USER_TYPE WHEN 'Employee' THEN 'FUNCIONÁRIO' WHEN 'External Personnel' THEN 'TERCEIRO' ELSE ISNULL(t.USER_TYPE,'') END AS Tipo,
    ISNULL(ue.UF2, ISNULL(ux.UF2,'')) AS Empresa,
    CAST(NULL AS varchar(50)) AS StatusAcesso,
    CAST(NULL AS varchar(100)) AS DetalheStatusAcesso
FROM HA_TRANSIT t
LEFT JOIN Employee e ON e.SbiID = t.SBI_ID
LEFT JOIN EmployeeUserFields ue ON ue.SbiID = e.SbiID
LEFT JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
LEFT JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
LEFT JOIN Card c ON c.SbiID = ISNULL(e.SbiID, x.SbiID)
LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
";
        cmd2.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
        cmd2.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
        using var r2 = await cmd2.ExecuteReaderAsync();
        rows = new List<(int, DateTime?, string, string, string, string, string, string, string, string, string, string, string)>();
        while (await r2.ReadAsync())
        {
            rows.Add((
                r2.GetInt32(0),
                r2.IsDBNull(1) ? (DateTime?)null : r2.GetDateTime(1),
                r2.IsDBNull(2) ? null : r2.GetString(2),
                r2.IsDBNull(3) ? null : r2.GetString(3),
                r2.IsDBNull(4) ? null : r2.GetString(4),
                r2.IsDBNull(5) ? null : r2.GetString(5),
                r2.IsDBNull(6) ? null : r2.GetString(6),
                r2.IsDBNull(7) ? null : r2.GetString(7),
                r2.IsDBNull(8) ? null : r2.GetString(8),
                r2.IsDBNull(9) ? null : r2.GetString(9),
                r2.IsDBNull(10) ? null : r2.GetString(10),
                r2.IsDBNull(11) ? null : r2.GetString(11),
                r2.IsDBNull(12) ? null : r2.GetString(12)
            ));
        }
    }
    // reuse export logic from existing endpoints
    if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
    {
        var sb = new StringBuilder();
        sb.AppendLine("EventID,TimeOrder,DataHora,TAG,Acesso,Evento,NomeCompleto,DocumentoMatricula,Cartao,Tipo,Empresa,StatusAcesso,DetalheStatusAcesso");
        foreach (var x in rows)
        {
            sb.AppendLine($"{x.EventID},{x.TimeOrder},{Escape(x.DataHora)},{Escape(x.TAG)},{Escape(x.Acesso)},{Escape(x.Evento)},{Escape(x.NomeCompleto)},{Escape(x.DocumentoMatricula)},{Escape(x.Cartao)},{Escape(x.Tipo)},{Escape(x.Empresa)},{Escape(x.StatusAcesso)},{Escape(x.DetalheStatusAcesso)}");
        }
        return Results.File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "door-critical.csv");
    }
    if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
            var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = doc.WorkbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "DoorCritical" });
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
            void AddRow(params string[] cells)
            {
                var row = new Row();
                foreach (var c in cells) row.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c) });
                sheetData.Append(row);
            }
            AddRow("EventID","TimeOrder","DataHora","TAG","Acesso","Evento","NomeCompleto","DocumentoMatricula","Cartao","Tipo","Empresa","StatusAcesso","DetalheStatusAcesso");
            foreach (var x in rows) AddRow(x.EventID.ToString(), x.TimeOrder?.ToString() ?? "", x.DataHora, x.TAG, x.Acesso, x.Evento, x.NomeCompleto, x.DocumentoMatricula, x.Cartao, x.Tipo, x.Empresa, x.StatusAcesso, x.DetalheStatusAcesso);
        }
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "door-critical.xlsx");
    }
    if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
    {
        // reuse PDF helper from earlier (Escape function is already defined earlier in file)
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Row(row => { row.RelativeColumn().Text("Eventos Críticos").SemiBold().FontSize(18); });
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { for(int i=0;i<13;i++) c.RelativeColumn(); });
                    table.Cell().Text("EventID"); table.Cell().Text("TimeOrder"); table.Cell().Text("DataHora"); table.Cell().Text("TAG"); table.Cell().Text("Acesso"); table.Cell().Text("Evento"); table.Cell().Text("NomeCompleto"); table.Cell().Text("DocumentoMatricula"); table.Cell().Text("Cartao"); table.Cell().Text("Tipo"); table.Cell().Text("Empresa"); table.Cell().Text("StatusAcesso"); table.Cell().Text("DetalheStatusAcesso");
                    foreach (var x in rows)
                    {
                        table.Cell().Text(x.EventID.ToString());
                        table.Cell().Text(x.TimeOrder?.ToString() ?? "");
                        table.Cell().Text(x.DataHora ?? "");
                        table.Cell().Text(x.TAG ?? "");
                        table.Cell().Text(x.Acesso ?? "");
                        table.Cell().Text(x.Evento ?? "");
                        table.Cell().Text(x.NomeCompleto ?? "");
                        table.Cell().Text(x.DocumentoMatricula ?? "");
                        table.Cell().Text(x.Cartao ?? "");
                        table.Cell().Text(x.Tipo ?? "");
                        table.Cell().Text(x.Empresa ?? "");
                        table.Cell().Text(x.StatusAcesso ?? "");
                        table.Cell().Text(x.DetalheStatusAcesso ?? "");
                    }
                });
            });
        }).GeneratePdf();
        return Results.File(bytes, "application/pdf", "door-critical.pdf");
    }
    return Results.BadRequest(new { error = "Formato inválido" });
}).RequireAuthorization();

// ----------------------------------------------------------------
// additional door event reports using existing stored procedures
// JP4: jp4_sp_DoorGeneral, jp4_sp_DoorGeneral_byName, jp4_sp_DoorGeneral_bysite
// shape the output to the same columns as door-critical for consistency
static (List<(int EventID, DateTime? TimeOrder, string? DataHora, string? TAG, string? Acesso, string? Evento, string? NomeCompleto, string? DocumentoMatricula, string? Cartao, string? Tipo, string? Empresa, string? StatusAcesso, string? DetalheStatusAcesso)>, string? error) ExecDoorProc(SqlConnection cn, string procText, IEnumerable<SqlParameter> parameters)
{
    try
    {
        using var cmd = cn.CreateCommand();
        cmd.CommandText = procText;
        foreach (var p in parameters) cmd.Parameters.Add(p);
        using var r = cmd.ExecuteReader();
        var rows = new List<(int, DateTime?, string?, string?, string?, string?, string?, string?, string?, string?, string?, string?, string?)>();
        while (r.Read())
        {
            rows.Add((
                r.IsDBNull(0) ? 0 : r.GetInt32(0),
                r.IsDBNull(1) ? (DateTime?)null : r.GetDateTime(1),
                r.IsDBNull(2) ? null : r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                r.IsDBNull(6) ? null : r.GetString(6),
                r.IsDBNull(7) ? null : r.GetString(7),
                r.IsDBNull(8) ? null : r.GetString(8),
                r.IsDBNull(9) ? null : r.GetString(9),
                r.IsDBNull(10) ? null : r.GetString(10),
                r.IsDBNull(11) ? null : r.GetString(11),
                r.IsDBNull(12) ? null : r.GetString(12)
            ));
        }
        return (rows, null);
    }
    catch (Exception ex)
    {
        return (new List<(int, DateTime?, string?, string?, string?, string?, string?, string?, string?, string?, string?, string?, string?)>(), ex.Message);
    }
}

app.MapGet("/api/reports/door-general", async (string start, string end) =>
{
    try
    {
        var startDt = ParseDate(start);
        var endDt = ParseDate(end);
        using var cn = new SqlConnection(GetConn("EMS"));
        await cn.OpenAsync();
        var proc = "EXEC dbo.jp4_sp_DoorGeneral @DataInicio, @DataFim";
        var (rows, err) = ExecDoorProc(cn, proc, new[]
        {
            new SqlParameter("@DataInicio", SqlDbType.VarChar, 20) { Value = startDt.ToString("yyyy-MM-ddTHH:mm:ss") },
            new SqlParameter("@DataFim", SqlDbType.VarChar, 20) { Value = endDt.ToString("yyyy-MM-ddTHH:mm:ss") }
        });
        if (err != null) return Results.BadRequest(new { success = false, error = err });
        return Results.Ok(new { success = true, count = rows.Count, data = rows.Select(x => new {
            EventID = x.EventID, TimeOrder = x.TimeOrder, DataHora = x.DataHora, TAG = x.TAG, Acesso = x.Acesso, Evento = x.Evento,
            NomeCompleto = x.NomeCompleto, DocumentoMatricula = x.DocumentoMatricula, Cartao = x.Cartao, Tipo = x.Tipo, Empresa = x.Empresa,
            StatusAcesso = x.StatusAcesso, DetalheStatusAcesso = x.DetalheStatusAcesso
        }) });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/reports/door-general/export", async (string start, string end, string format) =>
{
    var startDt = ParseDate(start);
    var endDt = ParseDate(end);
    using var cn = new SqlConnection(GetConn("EMS"));
    await cn.OpenAsync();
    var proc = "EXEC dbo.jp4_sp_DoorGeneral @DataInicio, @DataFim";
    var (rows, err) = ExecDoorProc(cn, proc, new[]
    {
        new SqlParameter("@DataInicio", SqlDbType.VarChar, 20) { Value = startDt.ToString("yyyy-MM-ddTHH:mm:ss") },
        new SqlParameter("@DataFim", SqlDbType.VarChar, 20) { Value = endDt.ToString("yyyy-MM-ddTHH:mm:ss") }
    });
    if (err != null) return Results.BadRequest(new { error = err });
    // reuse export logic from critical
    if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
    {
        var sb = new StringBuilder();
        sb.AppendLine("EventID,TimeOrder,DataHora,TAG,Acesso,Evento,NomeCompleto,DocumentoMatricula,Cartao,Tipo,Empresa,StatusAcesso,DetalheStatusAcesso");
        foreach (var x in rows) sb.AppendLine($"{x.EventID},{x.TimeOrder},{Escape(x.DataHora ?? "")},{Escape(x.TAG ?? "")},{Escape(x.Acesso ?? "")},{Escape(x.Evento ?? "")},{Escape(x.NomeCompleto ?? "")},{Escape(x.DocumentoMatricula ?? "")},{Escape(x.Cartao ?? "")},{Escape(x.Tipo ?? "")},{Escape(x.Empresa ?? "")},{Escape(x.StatusAcesso ?? "")},{Escape(x.DetalheStatusAcesso ?? "")}");
        return Results.File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "door-general.csv");
    }
    if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
            var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = doc.WorkbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "DoorGeneral" });
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
            void AddRow(params string[] cells){ var row = new Row(); foreach (var c in cells) row.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c) }); sheetData.Append(row); }
            AddRow("EventID","TimeOrder","DataHora","TAG","Acesso","Evento","NomeCompleto","DocumentoMatricula","Cartao","Tipo","Empresa","StatusAcesso","DetalheStatusAcesso");
            foreach (var x in rows) AddRow(x.EventID.ToString(), x.TimeOrder?.ToString() ?? "", x.DataHora ?? "", x.TAG ?? "", x.Acesso ?? "", x.Evento ?? "", x.NomeCompleto ?? "", x.DocumentoMatricula ?? "", x.Cartao ?? "", x.Tipo ?? "", x.Empresa ?? "", x.StatusAcesso ?? "", x.DetalheStatusAcesso ?? "");
        }
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "door-general.xlsx");
    }
    if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Row(row => { row.RelativeColumn().Text("Eventos Gerais").SemiBold().FontSize(18); });
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { for(int i=0;i<13;i++) c.RelativeColumn(); });
                    table.Cell().Text("EventID"); table.Cell().Text("TimeOrder"); table.Cell().Text("DataHora"); table.Cell().Text("TAG"); table.Cell().Text("Acesso"); table.Cell().Text("Evento"); table.Cell().Text("NomeCompleto"); table.Cell().Text("DocumentoMatricula"); table.Cell().Text("Cartao"); table.Cell().Text("Tipo"); table.Cell().Text("Empresa"); table.Cell().Text("StatusAcesso"); table.Cell().Text("DetalheStatusAcesso");
                    foreach (var x in rows)
                    {
                        table.Cell().Text(x.EventID.ToString());
                        table.Cell().Text(x.TimeOrder?.ToString() ?? "");
                        table.Cell().Text(x.DataHora ?? "");
                        table.Cell().Text(x.TAG ?? "");
                        table.Cell().Text(x.Acesso ?? "");
                        table.Cell().Text(x.Evento ?? "");
                        table.Cell().Text(x.NomeCompleto ?? "");
                        table.Cell().Text(x.DocumentoMatricula ?? "");
                        table.Cell().Text(x.Cartao ?? "");
                        table.Cell().Text(x.Tipo ?? "");
                        table.Cell().Text(x.Empresa ?? "");
                        table.Cell().Text(x.StatusAcesso ?? "");
                        table.Cell().Text(x.DetalheStatusAcesso ?? "");
                    }
                });
            });
        }).GeneratePdf();
        return Results.File(bytes, "application/pdf", "door-general.pdf");
    }
    return Results.BadRequest(new { error = "Formato inválido" });
}).RequireAuthorization();

app.MapGet("/api/reports/door-general/by-name", async (string start, string end, string name) =>
{
    try
    {
        var startDt = ParseDate(start);
        var endDt = ParseDate(end);
        using var cn = new SqlConnection(GetConn("EMS"));
        await cn.OpenAsync();
        var proc = "EXEC dbo.jp4_sp_DoorGeneral_byName @DataInicio, @DataFim, @Name";
        var (rows, err) = ExecDoorProc(cn, proc, new[]
        {
            new SqlParameter("@DataInicio", SqlDbType.VarChar, 20) { Value = startDt.ToString("yyyy-MM-ddTHH:mm:ss") },
            new SqlParameter("@DataFim", SqlDbType.VarChar, 20) { Value = endDt.ToString("yyyy-MM-ddTHH:mm:ss") },
            new SqlParameter("@Name", SqlDbType.VarChar, 100) { Value = name ?? "" }
        });
        if (err != null) return Results.BadRequest(new { success = false, error = err });
        return Results.Ok(new { success = true, count = rows.Count, data = rows.Select(x => new {
            EventID = x.EventID, TimeOrder = x.TimeOrder, DataHora = x.DataHora, TAG = x.TAG, Acesso = x.Acesso, Evento = x.Evento,
            NomeCompleto = x.NomeCompleto, DocumentoMatricula = x.DocumentoMatricula, Cartao = x.Cartao, Tipo = x.Tipo, Empresa = x.Empresa,
            StatusAcesso = x.StatusAcesso, DetalheStatusAcesso = x.DetalheStatusAcesso
        }) });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/reports/door-general/by-name/export", async (string start, string end, string name, string format) =>
{
    var startDt = ParseDate(start);
    var endDt = ParseDate(end);
    using var cn = new SqlConnection(GetConn("EMS"));
    await cn.OpenAsync();
    var proc = "EXEC dbo.jp4_sp_DoorGeneral_byName @DataInicio, @DataFim, @Name";
    var (rows, err) = ExecDoorProc(cn, proc, new[]
    {
        new SqlParameter("@DataInicio", SqlDbType.VarChar, 20) { Value = startDt.ToString("yyyy-MM-ddTHH:mm:ss") },
        new SqlParameter("@DataFim", SqlDbType.VarChar, 20) { Value = endDt.ToString("yyyy-MM-ddTHH:mm:ss") },
        new SqlParameter("@Name", SqlDbType.VarChar, 100) { Value = name ?? "" }
    });
    if (err != null) return Results.BadRequest(new { error = err });
    if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
    {
        var sb = new StringBuilder();
        sb.AppendLine("EventID,TimeOrder,DataHora,TAG,Acesso,Evento,NomeCompleto,DocumentoMatricula,Cartao,Tipo,Empresa,StatusAcesso,DetalheStatusAcesso");
        foreach (var x in rows) sb.AppendLine($"{x.EventID},{x.TimeOrder},{Escape(x.DataHora ?? "")},{Escape(x.TAG ?? "")},{Escape(x.Acesso ?? "")},{Escape(x.Evento ?? "")},{Escape(x.NomeCompleto ?? "")},{Escape(x.DocumentoMatricula ?? "")},{Escape(x.Cartao ?? "")},{Escape(x.Tipo ?? "")},{Escape(x.Empresa ?? "")},{Escape(x.StatusAcesso ?? "")},{Escape(x.DetalheStatusAcesso ?? "")}");
        return Results.File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "door-general-by-name.csv");
    }
    if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
            var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = doc.WorkbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "DoorByName" });
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
            void AddRow(params string[] cells){ var row = new Row(); foreach (var c in cells) row.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c) }); sheetData.Append(row); }
            AddRow("EventID","TimeOrder","DataHora","TAG","Acesso","Evento","NomeCompleto","DocumentoMatricula","Cartao","Tipo","Empresa","StatusAcesso","DetalheStatusAcesso");
            foreach (var x in rows) AddRow(x.EventID.ToString(), x.TimeOrder?.ToString() ?? "", x.DataHora ?? "", x.TAG ?? "", x.Acesso ?? "", x.Evento ?? "", x.NomeCompleto ?? "", x.DocumentoMatricula ?? "", x.Cartao ?? "", x.Tipo ?? "", x.Empresa ?? "", x.StatusAcesso ?? "", x.DetalheStatusAcesso ?? "");
        }
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "door-general-by-name.xlsx");
    }
    if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Row(row => { row.RelativeColumn().Text("Eventos Gerais por Nome").SemiBold().FontSize(18); });
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { for(int i=0;i<13;i++) c.RelativeColumn(); });
                    table.Cell().Text("EventID"); table.Cell().Text("TimeOrder"); table.Cell().Text("DataHora"); table.Cell().Text("TAG"); table.Cell().Text("Acesso"); table.Cell().Text("Evento"); table.Cell().Text("NomeCompleto"); table.Cell().Text("DocumentoMatricula"); table.Cell().Text("Cartao"); table.Cell().Text("Tipo"); table.Cell().Text("Empresa"); table.Cell().Text("StatusAcesso"); table.Cell().Text("DetalheStatusAcesso");
                    foreach (var x in rows)
                    {
                        table.Cell().Text(x.EventID.ToString());
                        table.Cell().Text(x.TimeOrder?.ToString() ?? "");
                        table.Cell().Text(x.DataHora ?? "");
                        table.Cell().Text(x.TAG ?? "");
                        table.Cell().Text(x.Acesso ?? "");
                        table.Cell().Text(x.Evento ?? "");
                        table.Cell().Text(x.NomeCompleto ?? "");
                        table.Cell().Text(x.DocumentoMatricula ?? "");
                        table.Cell().Text(x.Cartao ?? "");
                        table.Cell().Text(x.Tipo ?? "");
                        table.Cell().Text(x.Empresa ?? "");
                        table.Cell().Text(x.StatusAcesso ?? "");
                        table.Cell().Text(x.DetalheStatusAcesso ?? "");
                    }
                });
            });
        }).GeneratePdf();
        return Results.File(bytes, "application/pdf", "door-general-by-name.pdf");
    }
    return Results.BadRequest(new { error = "Formato inválido" });
}).RequireAuthorization();

app.MapGet("/api/reports/door-general/by-site", async (string start, string end, string site) =>
{
    try
    {
        var startDt = ParseDate(start);
        var endDt = ParseDate(end);
        using var cn = new SqlConnection(GetConn("EMS"));
        await cn.OpenAsync();
        var proc = "EXEC dbo.jp4_sp_DoorGeneral_bysite @DataInicio, @DataFim, @Site";
        var (rows, err) = ExecDoorProc(cn, proc, new[]
        {
            new SqlParameter("@DataInicio", SqlDbType.VarChar, 20) { Value = startDt.ToString("yyyy-MM-ddTHH:mm:ss") },
            new SqlParameter("@DataFim", SqlDbType.VarChar, 20) { Value = endDt.ToString("yyyy-MM-ddTHH:mm:ss") },
            new SqlParameter("@Site", SqlDbType.VarChar, 100) { Value = site ?? "" }
        });
        if (err != null) return Results.BadRequest(new { success = false, error = err });
        return Results.Ok(new { success = true, count = rows.Count, data = rows.Select(x => new {
            EventID = x.EventID, TimeOrder = x.TimeOrder, DataHora = x.DataHora, TAG = x.TAG, Acesso = x.Acesso, Evento = x.Evento,
            NomeCompleto = x.NomeCompleto, DocumentoMatricula = x.DocumentoMatricula, Cartao = x.Cartao, Tipo = x.Tipo, Empresa = x.Empresa,
            StatusAcesso = x.StatusAcesso, DetalheStatusAcesso = x.DetalheStatusAcesso
        }) });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/reports/door-general/by-site/export", async (string start, string end, string site, string format) =>
{
    var startDt = ParseDate(start);
    var endDt = ParseDate(end);
    using var cn = new SqlConnection(GetConn("EMS"));
    await cn.OpenAsync();
    var proc = "EXEC dbo.jp4_sp_DoorGeneral_bysite @DataInicio, @DataFim, @Site";
    var (rows, err) = ExecDoorProc(cn, proc, new[]
    {
        new SqlParameter("@DataInicio", SqlDbType.VarChar, 20) { Value = startDt.ToString("yyyy-MM-ddTHH:mm:ss") },
        new SqlParameter("@DataFim", SqlDbType.VarChar, 20) { Value = endDt.ToString("yyyy-MM-ddTHH:mm:ss") },
        new SqlParameter("@Site", SqlDbType.VarChar, 100) { Value = site ?? "" }
    });
    if (err != null) return Results.BadRequest(new { error = err });
    if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
    {
        var sb = new StringBuilder();
        sb.AppendLine("EventID,TimeOrder,DataHora,TAG,Acesso,Evento,NomeCompleto,DocumentoMatricula,Cartao,Tipo,Empresa,StatusAcesso,DetalheStatusAcesso");
        foreach (var x in rows) sb.AppendLine($"{x.EventID},{x.TimeOrder},{Escape(x.DataHora ?? "")},{Escape(x.TAG ?? "")},{Escape(x.Acesso ?? "")},{Escape(x.Evento ?? "")},{Escape(x.NomeCompleto ?? "")},{Escape(x.DocumentoMatricula ?? "")},{Escape(x.Cartao ?? "")},{Escape(x.Tipo ?? "")},{Escape(x.Empresa ?? "")},{Escape(x.StatusAcesso ?? "")},{Escape(x.DetalheStatusAcesso ?? "")}");
        return Results.File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "door-general-by-site.csv");
    }
    if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
            var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = doc.WorkbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "DoorBySite" });
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
            void AddRow(params string[] cells){ var row = new Row(); foreach (var c in cells) row.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c) }); sheetData.Append(row); }
            AddRow("EventID","TimeOrder","DataHora","TAG","Acesso","Evento","NomeCompleto","DocumentoMatricula","Cartao","Tipo","Empresa","StatusAcesso","DetalheStatusAcesso");
            foreach (var x in rows) AddRow(x.EventID.ToString(), x.TimeOrder?.ToString() ?? "", x.DataHora ?? "", x.TAG ?? "", x.Acesso ?? "", x.Evento ?? "", x.NomeCompleto ?? "", x.DocumentoMatricula ?? "", x.Cartao ?? "", x.Tipo ?? "", x.Empresa ?? "", x.StatusAcesso ?? "", x.DetalheStatusAcesso ?? "");
        }
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "door-general-by-site.xlsx");
    }
    if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Row(row => { row.RelativeColumn().Text("Eventos Gerais por Site").SemiBold().FontSize(18); });
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { for(int i=0;i<13;i++) c.RelativeColumn(); });
                    table.Cell().Text("EventID"); table.Cell().Text("TimeOrder"); table.Cell().Text("DataHora"); table.Cell().Text("TAG"); table.Cell().Text("Acesso"); table.Cell().Text("Evento"); table.Cell().Text("NomeCompleto"); table.Cell().Text("DocumentoMatricula"); table.Cell().Text("Cartao"); table.Cell().Text("Tipo"); table.Cell().Text("Empresa"); table.Cell().Text("StatusAcesso"); table.Cell().Text("DetalheStatusAcesso");
                    foreach (var x in rows)
                    {
                        table.Cell().Text(x.EventID.ToString());
                        table.Cell().Text(x.TimeOrder?.ToString() ?? "");
                        table.Cell().Text(x.DataHora ?? "");
                        table.Cell().Text(x.TAG ?? "");
                        table.Cell().Text(x.Acesso ?? "");
                        table.Cell().Text(x.Evento ?? "");
                        table.Cell().Text(x.NomeCompleto ?? "");
                        table.Cell().Text(x.DocumentoMatricula ?? "");
                        table.Cell().Text(x.Cartao ?? "");
                        table.Cell().Text(x.Tipo ?? "");
                        table.Cell().Text(x.Empresa ?? "");
                        table.Cell().Text(x.StatusAcesso ?? "");
                        table.Cell().Text(x.DetalheStatusAcesso ?? "");
                    }
                });
            });
        }).GeneratePdf();
        return Results.File(bytes, "application/pdf", "door-general-by-site.pdf");
    }
    return Results.BadRequest(new { error = "Formato inválido" });
}).RequireAuthorization();

app.MapGet("/api/prestadores", async () =>
{
    var list = new List<object>();
    try
    {
        using var cn = new SqlConnection(GetConn("Logins"));
        await cn.OpenAsync();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT SBID,NOME,ENDERECO,FONE,EMAIL,SITE,ATIVO,CAMINHOIMG FROM dbo.Prestadores";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new
            {
                SBID = r.GetInt32(0),
                NOME = r.IsDBNull(1) ? null : r.GetString(1),
                ENDERECO = r.IsDBNull(2) ? null : r.GetString(2),
                FONE = r.IsDBNull(3) ? null : r.GetString(3),
                EMAIL = r.IsDBNull(4) ? null : r.GetString(4),
                SITE = r.IsDBNull(5) ? null : r.GetString(5),
                ATIVO = r.IsDBNull(6) ? (int?)null : r.GetInt32(6),
                CAMINHOIMG = r.IsDBNull(7) ? null : r.GetString(7)
            });
        }
    }
    catch
    {
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapPost("/api/login/signin", async (string usuario, string senha) =>
{
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = "SELECT NOME,NIVEL FROM dbo.Login WHERE USUARIO=@u AND SENHA=@s AND STATUS='Habilitado'";
    cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.VarChar) { Value = usuario });
    cmd.Parameters.Add(new SqlParameter("@s", SqlDbType.VarChar) { Value = senha });
    using var r = await cmd.ExecuteReaderAsync();
    if (!r.HasRows) return Results.Unauthorized();
    await r.ReadAsync();
    var nome = r.GetString(0);
    var nivel = r.GetString(1);
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>();
    if (!string.IsNullOrEmpty(usuario)) claims.Add(new Claim("usuario", usuario));
    if (!string.IsNullOrEmpty(nome)) claims.Add(new Claim("nome", nome));
    if (!string.IsNullOrEmpty(nivel)) claims.Add(new Claim("nivel", nivel));
    var token = new JwtSecurityToken(jwtIssuer, jwtAudience, claims: claims, expires: DateTime.UtcNow.AddMinutes(jwtExpires), signingCredentials: creds);
    var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = tokenStr, nome, usuario, nivel });
});

app.MapPost("/api/login/signin-token", async (HttpRequest req) =>
{
    string input = "";
    try
    {
        var doc = await req.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        if (doc != null && doc.RootElement.TryGetProperty("token", out var tokElem) && tokElem.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            input = tokElem.GetString() ?? "";
        }
    }
    catch
    {
        input = "";
    }
    input = input.Trim();
    if (string.IsNullOrWhiteSpace(input) && req.Query.ContainsKey("token")) input = req.Query["token"].ToString();
    if (input.StartsWith("TOKEN", StringComparison.OrdinalIgnoreCase))
    {
        input = input.Substring(5).Trim();
    }
    string? usuario = null, nome = null, nivel = null;
    int? clientId = null;
    string? clientName = null;
    var fallback = new Dictionary<string, (string usuario, string nome, string nivel)>(StringComparer.OrdinalIgnoreCase)
    {
        ["0001"] = ("superadmin","SUPERADMIN","SuperAdmin"),
        ["011"] = ("admin","EVERTON","Administrador"),
        ["022"] = ("user","EVERTON","Padrão"),
        ["021"] = ("gerente","ALANA","Padrão"),
        ["031"] = ("basico","ALANA","Básico")
    };
    if (fallback.ContainsKey(input))
    {
        var f = fallback[input];
        usuario = f.usuario; nome = f.nome; nivel = f.nivel;
    }
    else
    {
        try
        {
            using var cn = new SqlConnection(GetConn("Logins"));
            await cn.OpenAsync();
            using (var cmdCli = cn.CreateCommand())
            {
                cmdCli.CommandText = "SELECT Id,NOME FROM dbo.ClientesPortal WHERE RTRIM(LTRIM(CLIENT_TOKEN))=@t AND ISNULL(ATIVO,1)=1";
                cmdCli.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = input });
                using var rCli = await cmdCli.ExecuteReaderAsync();
                if (rCli.HasRows)
                {
                    await rCli.ReadAsync();
                    clientId = rCli.GetInt32(0);
                    clientName = rCli.IsDBNull(1) ? null : rCli.GetString(1);
                    usuario = "cliente";
                    nome = clientName ?? "Cliente";
                    nivel = "Cliente";
                }
            }
            if (usuario == null)
            {
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT USUARIO,NOME,NIVEL FROM dbo.Login WHERE RTRIM(LTRIM(TOKEN))=@t AND STATUS='Habilitado'";
                cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = input });
                using var r = await cmd.ExecuteReaderAsync();
                if (r.HasRows)
                {
                    await r.ReadAsync();
                    usuario = r.GetString(0);
                    nome = r.GetString(1);
                    nivel = r.GetString(2);
                }
            }
        }
        catch
        {
        }
        if (usuario == null) return Results.Unauthorized();
    }
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>();
    if (!string.IsNullOrEmpty(usuario)) claims.Add(new Claim("usuario", usuario));
    if (!string.IsNullOrEmpty(nome)) claims.Add(new Claim("nome", nome));
    if (!string.IsNullOrEmpty(nivel)) claims.Add(new Claim("nivel", nivel));
    if (clientId.HasValue) claims.Add(new Claim("clientId", clientId.Value.ToString()));
    var jwt = new JwtSecurityToken(jwtIssuer, jwtAudience, claims: claims, expires: DateTime.UtcNow.AddMinutes(jwtExpires), signingCredentials: creds);
    var jwtStr = new JwtSecurityTokenHandler().WriteToken(jwt);
    return Results.Ok(new { token = jwtStr, nome, usuario, nivel, clientId, clientName });
});

app.MapPost("/api/client/profile", async (HttpContext ctx) =>
{
    var claimNivel = ctx.User?.FindFirst("nivel")?.Value;
    if (!string.Equals(claimNivel, "Cliente", StringComparison.OrdinalIgnoreCase)) return Results.Forbid();
    var claimCid = ctx.User?.FindFirst("clientId")?.Value;
    if (claimCid == null || !int.TryParse(claimCid, out var cid) || cid <= 0) return Results.BadRequest(new { error = "Cliente não identificado no token" });

    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    string? GetStr(string key)
    {
        if (!dto.TryGetValue(key, out var el)) return null;
        return el.ValueKind == System.Text.Json.JsonValueKind.Null ? null : el.ToString();
    }

    var nomeDto = GetStr("nome");
    var enderecoDto = GetStr("endereco");
    var foneDto = GetStr("fone");
    var emailDto = GetStr("email");
    var siteDto = GetStr("site");
    var respDto = GetStr("responsavel");

    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
UPDATE dbo.ClientesPortal SET
NOME = ISNULL(@NOME, NOME),
ENDERECO = ISNULL(@ENDERECO, ENDERECO),
FONE = ISNULL(@FONE, FONE),
EMAIL = ISNULL(@EMAIL, EMAIL),
SITE = ISNULL(@SITE, SITE),
RESPONSAVEL = ISNULL(@RESPONSAVEL, RESPONSAVEL)
WHERE Id=@ID";
    cmd.Parameters.Add(new SqlParameter("@ID", SqlDbType.Int) { Value = cid });
    cmd.Parameters.Add(new SqlParameter("@NOME", SqlDbType.VarChar, 200) { Value = (object?)nomeDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ENDERECO", SqlDbType.VarChar, 300) { Value = (object?)enderecoDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@FONE", SqlDbType.VarChar, 50) { Value = (object?)foneDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@EMAIL", SqlDbType.VarChar, 200) { Value = (object?)emailDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@SITE", SqlDbType.VarChar, 200) { Value = (object?)siteDto ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@RESPONSAVEL", SqlDbType.VarChar, 100) { Value = (object?)respDto ?? DBNull.Value });
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { ok = true });
}).RequireAuthorization();

app.MapPost("/api/client/token/regenerate", async (HttpContext ctx) =>
{
    var claimNivel = ctx.User?.FindFirst("nivel")?.Value;
    if (!string.Equals(claimNivel, "Cliente", StringComparison.OrdinalIgnoreCase)) return Results.Forbid();
    var claimCid = ctx.User?.FindFirst("clientId")?.Value;
    if (claimCid == null || !int.TryParse(claimCid, out var cid) || cid <= 0) return Results.BadRequest(new { error = "Cliente não identificado no token" });

    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    string token = "";
    var rnd = new Random();
    for (int i = 0; i < 200; i++)
    {
        token = rnd.Next(0, 10000).ToString("D4");
        using var chk = cn.CreateCommand();
        chk.CommandText = "SELECT COUNT(*) FROM dbo.ClientesPortal WHERE CLIENT_TOKEN=@t";
        chk.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = token });
        var count = (int)(await chk.ExecuteScalarAsync() ?? 0);
        if (count == 0) break;
    }
    using (var cmd = cn.CreateCommand())
    {
        cmd.CommandText = "UPDATE dbo.ClientesPortal SET CLIENT_TOKEN=@t WHERE Id=@id";
        cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = token });
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
        await cmd.ExecuteNonQueryAsync();
    }
    return Results.Ok(new { token });
}).RequireAuthorization();

app.MapPost("/api/client/logo", async (HttpContext ctx) =>
{
    var claimNivel = ctx.User?.FindFirst("nivel")?.Value;
    if (!string.Equals(claimNivel, "Cliente", StringComparison.OrdinalIgnoreCase)) return Results.Forbid();
    var claimCid = ctx.User?.FindFirst("clientId")?.Value;
    if (claimCid == null || !int.TryParse(claimCid, out var cid) || cid <= 0) return Results.BadRequest(new { error = "Cliente não identificado no token" });
    if (!ctx.Request.HasFormContentType) return Results.BadRequest(new { error = "Conteúdo inválido" });
    var form = await ctx.Request.ReadFormAsync();
    var file = form.Files["file"];
    if (file == null || file.Length == 0) return Results.BadRequest(new { error = "Arquivo não enviado" });
    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "clients");
    Directory.CreateDirectory(uploads);
    var ext = Path.GetExtension(file.FileName);
    if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
    var fileName = $"client_{cid}{ext}";
    var full = Path.Combine(uploads, fileName);
    using (var stream = System.IO.File.Create(full))
    {
        await file.CopyToAsync(stream);
    }
    var relPath = "/clients/" + fileName;
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using (var cmd = cn.CreateCommand())
    {
        cmd.CommandText = "UPDATE dbo.ClientesPortal SET CAMINHOIMG=@p WHERE Id=@id";
        cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.VarChar, 255) { Value = relPath });
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
        await cmd.ExecuteNonQueryAsync();
    }
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";
    var fullUrl = baseUrl + relPath;
    return Results.Ok(new { logoPath = fullUrl });
}).RequireAuthorization();

app.MapPost("/api/messages", async (HttpContext ctx) =>
{
    var claimUsuario = ctx.User?.FindFirst("usuario")?.Value;
    var claimNome = ctx.User?.FindFirst("nome")?.Value;
    var claimNivel = ctx.User?.FindFirst("nivel")?.Value;
    var claimCid = ctx.User?.FindFirst("clientId")?.Value;
    int? clientId = null;
    if (!string.IsNullOrWhiteSpace(claimCid) && int.TryParse(claimCid, out var cidParsed) && cidParsed > 0)
    {
        clientId = cidParsed;
    }
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    string? assunto = GetDtoString(dto, "assunto");
    string? texto = GetDtoString(dto, "texto");
    if (string.IsNullOrWhiteSpace(assunto) || string.IsNullOrWhiteSpace(texto))
    {
        return Results.BadRequest(new { error = "Assunto e texto são obrigatórios" });
    }
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
INSERT INTO dbo.MensagensPortal(FromUsuario,FromNome,FromNivel,ClientId,Assunto,Texto,CreatedAt,Status)
VALUES(@FromUsuario,@FromNome,@FromNivel,@ClientId,@Assunto,@Texto,GETDATE(),@Status)";
    cmd.Parameters.Add(new SqlParameter("@FromUsuario", SqlDbType.VarChar, 100) { Value = (object?)claimUsuario ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@FromNome", SqlDbType.VarChar, 200) { Value = (object?)claimNome ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@FromNivel", SqlDbType.VarChar, 50) { Value = (object?)claimNivel ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.Int) { Value = (object?)clientId ?? DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@Assunto", SqlDbType.VarChar, 200) { Value = assunto });
    cmd.Parameters.Add(new SqlParameter("@Texto", SqlDbType.VarChar) { Value = texto });
    cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.VarChar, 50) { Value = "Novo" });
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { ok = true });
}).RequireAuthorization();

app.MapGet("/api/admin/messages", async (HttpContext ctx) =>
{
    int page = 1;
    int pageSize = 50;
    if (int.TryParse(ctx.Request.Query["page"], out var p) && p > 0) page = p;
    if (int.TryParse(ctx.Request.Query["pageSize"], out var ps) && ps > 0 && ps <= 200) pageSize = ps;
    int offset = (page - 1) * pageSize;
    var items = new List<object>();
    int total = 0;
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using (var cmdCount = cn.CreateCommand())
    {
        cmdCount.CommandText = "SELECT COUNT(*) FROM dbo.MensagensPortal";
        var scalar = await cmdCount.ExecuteScalarAsync();
        total = scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
    }
    using (var cmd = cn.CreateCommand())
    {
        cmd.CommandText = @"
SELECT Id,FromUsuario,FromNome,FromNivel,ClientId,Assunto,Texto,CreatedAt,Status
FROM dbo.MensagensPortal
ORDER BY CreatedAt DESC, Id DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        cmd.Parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = offset });
        cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            items.Add(new
            {
                Id = r.GetInt32(0),
                FromUsuario = r.IsDBNull(1) ? null : r.GetString(1),
                FromNome = r.IsDBNull(2) ? null : r.GetString(2),
                FromNivel = r.IsDBNull(3) ? null : r.GetString(3),
                ClientId = r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
                Assunto = r.IsDBNull(5) ? null : r.GetString(5),
                Texto = r.IsDBNull(6) ? null : r.GetString(6),
                CreatedAt = r.GetDateTime(7),
                Status = r.IsDBNull(8) ? null : r.GetString(8)
            });
        }
    }
    return Results.Ok(new { total, items });
}).RequireAuthorization("AdminsOnly");

app.MapGet("/api/login/signin-token", async (HttpRequest req) =>
{
    var input = (req.Query.ContainsKey("token") ? req.Query["token"].ToString() : "").Trim();
    if (input.StartsWith("TOKEN", StringComparison.OrdinalIgnoreCase)) input = input.Substring(5).Trim();
    string? usuario = null, nome = null, nivel = null;
    int? clientId = null;
    string? clientName = null;
    var fallback = new Dictionary<string, (string usuario, string nome, string nivel)>(StringComparer.OrdinalIgnoreCase)
    {
        ["0001"] = ("superadmin","SUPERADMIN","SuperAdmin"),
        ["011"] = ("admin","EVERTON","Administrador"),
        ["022"] = ("user","EVERTON","Padrão"),
        ["021"] = ("gerente","ALANA","Padrão"),
        ["031"] = ("basico","ALANA","Básico")
    };
    if (fallback.ContainsKey(input))
    {
        var f = fallback[input];
        usuario = f.usuario; nome = f.nome; nivel = f.nivel;
    }
    else
    {
        try
        {
            using var cn = new SqlConnection(GetConn("Logins"));
            await cn.OpenAsync();
            using (var cmdCli = cn.CreateCommand())
            {
                cmdCli.CommandText = "SELECT Id,NOME FROM dbo.ClientesPortal WHERE RTRIM(LTRIM(CLIENT_TOKEN))=@t AND ISNULL(ATIVO,1)=1";
                cmdCli.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = input });
                using var rCli = await cmdCli.ExecuteReaderAsync();
                if (rCli.HasRows)
                {
                    await rCli.ReadAsync();
                    clientId = rCli.GetInt32(0);
                    clientName = rCli.IsDBNull(1) ? null : rCli.GetString(1);
                    usuario = "cliente";
                    nome = clientName ?? "Cliente";
                    nivel = "Cliente";
                }
            }
            if (usuario == null)
            {
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT USUARIO,NOME,NIVEL FROM dbo.Login WHERE RTRIM(LTRIM(TOKEN))=@t AND STATUS='Habilitado'";
                cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.VarChar) { Value = input });
                using var r = await cmd.ExecuteReaderAsync();
                if (r.HasRows)
                {
                    await r.ReadAsync();
                    usuario = r.GetString(0);
                    nome = r.GetString(1);
                    nivel = r.GetString(2);
                }
            }
        }
        catch { }
        if (usuario == null) return Results.Unauthorized();
    }
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>();
    if (!string.IsNullOrEmpty(usuario)) claims.Add(new Claim("usuario", usuario));
    if (!string.IsNullOrEmpty(nome)) claims.Add(new Claim("nome", nome));
    if (!string.IsNullOrEmpty(nivel)) claims.Add(new Claim("nivel", nivel));
    if (clientId.HasValue) claims.Add(new Claim("clientId", clientId.Value.ToString()));
    var jwt = new JwtSecurityToken(jwtIssuer, jwtAudience, claims: claims, expires: DateTime.UtcNow.AddMinutes(jwtExpires), signingCredentials: creds);
    var jwtStr = new JwtSecurityTokenHandler().WriteToken(jwt);
    return Results.Ok(new { token = jwtStr, nome, usuario, nivel, clientId, clientName });
});
app.MapGet("/api/login/tokens", async (HttpRequest req) =>
{
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = "SELECT USUARIO,NOME,NIVEL,TOKEN FROM dbo.Login WHERE STATUS='Habilitado'";
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    var unmasked = req.Query.ContainsKey("unmasked");
    while (await r.ReadAsync())
    {
        var usuario = r.IsDBNull(0) ? null : r.GetString(0);
        var nome = r.IsDBNull(1) ? null : r.GetString(1);
        var nivel = r.IsDBNull(2) ? null : r.GetString(2);
        var token = r.IsDBNull(3) ? null : r.GetString(3);
        if (unmasked)
            list.Add(new { usuario, nome, nivel, token });
        else
        {
            string mask = "";
            if (!string.IsNullOrEmpty(token))
            {
                var visible = token.Length >= 4 ? token.Substring(token.Length - 4) : token;
                mask = new string('*', Math.Max(0, token.Length - visible.Length)) + visible;
            }
            list.Add(new { usuario, nome, nivel, tokenMasked = mask });
        }
    }
    return Results.Ok(list);
});

app.MapGet("/api/cms/employees/search", async (string? matricula, string? empresa, int page, int pageSize, string? sort, string? dir) =>
{
    var defaultEmpresa = await GetDefaultClientNameAsync();
    var sortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "e.Name",
        ["SbiID"] = "e.SbiID",
        ["CardNumber"] = "c.CardNumber",
        ["Matricula"] = "e.Identifier",
        ["Empresa"] = "uf.UF2"
    };
    var orderCol = sort != null && sortMap.ContainsKey(sort) ? sortMap[sort] : "c.CardNumber";
    var orderDir = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
    if (page <= 0) page = 1;
    if (pageSize <= 0 || pageSize > 200) pageSize = 20;
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = new List<string>();
    if (!string.IsNullOrWhiteSpace(matricula)) { where.Add("e.Identifier = @matricula"); cmd.Parameters.Add(new SqlParameter("@matricula", SqlDbType.VarChar) { Value = matricula }); }
    if (!string.IsNullOrWhiteSpace(empresa)) { where.Add("uf.UF2 = @empresa"); cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa }); }
    var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
    cmd.CommandText = $@"
SELECT e.SbiID,e.Name,e.Surname,e.PreferredName,e.Identifier,uf.UF2,'FUNCIONÁRIO' as Tipo,c.CardNumber
FROM Employee e
INNER JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = e.SbiID ORDER BY CardNumber) c
{whereSql}
ORDER BY {orderCol} {orderDir}
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1)
FROM Employee e
INNER JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
{whereSql}";
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        var empresaRow = r.IsDBNull(5) ? null : r.GetString(5);
        if (string.IsNullOrWhiteSpace(empresaRow)) empresaRow = defaultEmpresa;
        items.Add(new
        {
            CardNumber = r.IsDBNull(7) ? null : r.GetString(7),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            Surname = r.IsDBNull(2) ? null : r.GetString(2),
            PreferredName = r.IsDBNull(3) ? null : r.GetString(3),
            Identifier = r.IsDBNull(4) ? null : r.GetString(4),
            Empresa = empresaRow,
            Tipo = r.IsDBNull(6) ? null : r.GetString(6)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/reports/transit", async (string start, string end, string? empresa, string? terminal, int page, int pageSize) =>
{
    var startDt = ParseDate(start);
    var endDt = ParseDate(end);
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = "WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = startDt });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = endDt });
    if (!string.IsNullOrWhiteSpace(terminal))
    {
        where += " AND t.TERMINAL = @terminal";
        cmd.Parameters.Add(new SqlParameter("@terminal", SqlDbType.VarChar) { Value = terminal });
    }
    if (!string.IsNullOrWhiteSpace(empresa))
    {
        where += " AND u.UF2 = @empresa";
        cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa });
    }
    cmd.CommandText = $@"
SELECT c.CardNumber,e.Name,u.UF2 as Empresa,t.TERMINAL,v.DESCRIPTION as TerminalDescription,t.TRANSIT_DATE
FROM HA_TRANSIT t
INNER JOIN Employee e ON e.SbiID = t.SBI_ID
LEFT JOIN EmployeeUserFields u ON u.SbiID = e.SbiID
OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = e.SbiID ORDER BY CardNumber) c
LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
{where}
ORDER BY c.CardNumber ASC, t.TRANSIT_DATE DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1)
FROM HA_TRANSIT t
INNER JOIN Employee e ON e.SbiID = t.SBI_ID
LEFT JOIN EmployeeUserFields u ON u.SbiID = e.SbiID
{where}";
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            CardNumber = r.IsDBNull(0) ? null : r.GetString(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            Empresa = r.IsDBNull(2) ? null : r.GetString(2),
            Terminal = r.IsDBNull(3) ? null : r.GetString(3),
            TerminalDescription = r.IsDBNull(4) ? null : r.GetString(4),
            TransitDate = r.GetDateTime(5)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/reports/transit/export", async (HttpContext ctx, string start, string end, string? empresa, string? terminal, string format) =>
{
    var startDt = ParseDate(start);
    var endDt = ParseDate(end);
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = "WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = startDt });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = endDt });
    if (!string.IsNullOrWhiteSpace(terminal))
    {
        where += " AND t.TERMINAL = @terminal";
        cmd.Parameters.Add(new SqlParameter("@terminal", SqlDbType.VarChar) { Value = terminal });
    }
    if (!string.IsNullOrWhiteSpace(empresa))
    {
        where += " AND u.UF2 = @empresa";
        cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa });
    }
    cmd.CommandText = $@"
SELECT c.CardNumber,e.Name,u.UF2 as Empresa,t.TERMINAL,v.DESCRIPTION as TerminalDescription,t.TRANSIT_DATE
FROM HA_TRANSIT t
INNER JOIN Employee e ON e.SbiID = t.SBI_ID
LEFT JOIN EmployeeUserFields u ON u.SbiID = e.SbiID
OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = e.SbiID ORDER BY CardNumber) c
LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
{where}
ORDER BY c.CardNumber ASC, t.TRANSIT_DATE DESC";
    using var r = await cmd.ExecuteReaderAsync();
    var rows = new List<(string? card, string? name, string? empresa, string? terminal, string? termDesc, DateTime date)>();
    while (await r.ReadAsync())
    {
        rows.Add((r.IsDBNull(0) ? null : r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4), r.GetDateTime(5)));
    }
    if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
    {
        var sb = new StringBuilder();
        sb.AppendLine("Cracha,Name,Empresa,Terminal,TerminalDescription,TransitDate");
        foreach (var x in rows)
            sb.AppendLine($"{Escape(x.card)},{Escape(x.name)},{Escape(x.empresa)},{Escape(x.terminal)},{Escape(x.termDesc)},{x.date:yyyy-MM-dd HH:mm:ss}");
        return Results.File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "transit.csv");
    }
    if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
            var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = doc.WorkbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "Transit" });
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
            void AddRow(params string[] cells)
            {
                var row = new Row();
                foreach (var c in cells) row.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c) });
                sheetData.Append(row);
            }
            AddRow("Cracha","Name","Empresa","Terminal","TerminalDescription","TransitDate");
            foreach (var x in rows) AddRow(x.card ?? "", x.name ?? "", x.empresa ?? "", x.terminal ?? "", x.termDesc ?? "", x.date.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "transit.xlsx");
    }
    if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
    {
        QuestPDF.Settings.License = LicenseType.Community;
        byte[]? leftLogo = null;
        byte[]? rightLogo = null;
        try
        {
            var clientIdHeader = ctx.Request.Headers.TryGetValue("X-Client-Id", out var vals) ? vals.ToString() : null;
            if (int.TryParse(clientIdHeader, out var cid))
            {
                using var cnL = new SqlConnection(GetConn("Logins"));
                await cnL.OpenAsync();
                using var cmdL = cnL.CreateCommand();
                cmdL.CommandText = "SELECT CAMINHOIMG FROM dbo.ClientesPortal WHERE Id=@id";
                cmdL.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
                using var rL = await cmdL.ExecuteReaderAsync();
                if (rL.HasRows)
                {
                    await rL.ReadAsync();
                    var p = rL.IsDBNull(0) ? null : rL.GetString(0);
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        var full = p.StartsWith("/") ? Path.Combine(app.Environment.ContentRootPath, "wwwroot", p.TrimStart('/')) : p;
                        if (System.IO.File.Exists(full))
                        {
                            leftLogo = await System.IO.File.ReadAllBytesAsync(full);
                        }
                    }
                }
            }
        }
        catch { }
        try
        {
            var rightPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "images-legacy", "Logo_Principal_Fundo2.png");
            if (System.IO.File.Exists(rightPath))
            {
                rightLogo = await System.IO.File.ReadAllBytesAsync(rightPath);
            }
        }
        catch { }
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Row(row =>
                {
                    row.RelativeColumn().AlignLeft().Element(e =>
                    {
                        if (leftLogo != null) e.Image(leftLogo, ImageScaling.FitWidth);
                    });
                    row.RelativeColumn().AlignCenter().Text("Relatório de Trânsitos").SemiBold().FontSize(18);
                    row.RelativeColumn().AlignRight().Element(e =>
                    {
                        if (rightLogo != null) e.Image(rightLogo, ImageScaling.FitWidth);
                    });
                });
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                    });
                    table.Cell().Text("Crachá"); table.Cell().Text("Name"); table.Cell().Text("Empresa"); table.Cell().Text("Terminal"); table.Cell().Text("TerminalDescription"); table.Cell().Text("TransitDate");
                    foreach (var x in rows)
                    {
                        table.Cell().Text(x.card ?? "");
                        table.Cell().Text(x.name ?? "");
                        table.Cell().Text(x.empresa ?? "");
                        table.Cell().Text(x.terminal ?? "");
                        table.Cell().Text(x.termDesc ?? "");
                        table.Cell().Text(x.date.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                });
            });
        }).GeneratePdf();
        return Results.File(bytes, "application/pdf", "transit.pdf");
    }
    return Results.BadRequest(new { error = "Formato inválido" });
    static string Escape(string? s) => s == null ? "" : s.Contains(',') ? $"\"{s.Replace("\"","\"\"")}\"" : s;
}).RequireAuthorization();
app.MapGet("/api/cms/transit/by-card", async (string card) =>
{
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT TOP 100
    q.SbiID,
    q.Name,
    q.CardNumber,
    q.STR_DIRECTION,
    q.USER_TYPE,
    q.TERMINAL,
    q.DESCRIPTION,
    q.TRANSIT_DATE
FROM (
    SELECT
        e.SbiID,
        e.Name,
        c.CardNumber,
        t.STR_DIRECTION,
        t.USER_TYPE,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM Employee e
    INNER JOIN Card c ON c.SbiID = e.SbiID
    INNER JOIN HA_TRANSIT t ON t.SBI_ID = e.SbiID
    INNER JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE c.CardNumber = @card
    UNION ALL
    SELECT
        x.SbiID,
        x.Name,
        c.CardNumber,
        t.STR_DIRECTION,
        t.USER_TYPE,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM ExternalRegular x
    INNER JOIN Card c ON c.SbiID = x.SbiID
    INNER JOIN HA_TRANSIT t ON t.SBI_ID = x.SbiID
    INNER JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE c.CardNumber = @card
) q
ORDER BY q.TRANSIT_DATE DESC";
    cmd.Parameters.Add(new SqlParameter("@card", SqlDbType.VarChar) { Value = card });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        list.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CardNumber = r.IsDBNull(2) ? null : r.GetString(2),
            Direction = r.IsDBNull(3) ? null : r.GetString(3),
            UserType = r.IsDBNull(4) ? null : r.GetString(4),
            Terminal = r.IsDBNull(5) ? null : r.GetString(5),
            TerminalDescription = r.IsDBNull(6) ? null : r.GetString(6),
            TransitDate = r.GetDateTime(7)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/cms/card/by-cpf", async (string cpf) =>
{
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT DISTINCT
    e.SbiID,
    e.Name + ' ' + e.Surname AS Name,
    c.CardNumber,
    'Employee' AS UserType
FROM Employee e
INNER JOIN Card c ON c.SbiID = e.SbiID
WHERE e.PreferredName = @cpf
UNION
SELECT DISTINCT
    x.SbiID,
    x.Name + ' ' + x.Surname AS Name,
    c.CardNumber,
    'External' AS UserType
FROM ExternalRegular x
INNER JOIN Card c ON c.SbiID = x.SbiID
WHERE x.PreferredName = @cpf";
    cmd.Parameters.Add(new SqlParameter("@cpf", SqlDbType.VarChar) { Value = cpf });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        list.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CardNumber = r.IsDBNull(2) ? null : r.GetString(2),
            UserType = r.IsDBNull(3) ? null : r.GetString(3)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

static string DigitsOnly(string? s)
{
    if (string.IsNullOrWhiteSpace(s)) return "";
    var sb = new StringBuilder(s.Length);
    foreach (var ch in s)
    {
        if (ch >= '0' && ch <= '9') sb.Append(ch);
    }
    return sb.ToString();
}

static DateTime ParseDateTimeAny(string? s)
{
    if (string.IsNullOrWhiteSpace(s)) throw new ArgumentException("Data/hora inválida");
    var t = s.Trim();
    var br = CultureInfo.GetCultureInfo("pt-BR");
    var isoFormats = new[]
    {
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "o"
    };
    if (DateTime.TryParseExact(t, isoFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out var dtIso))
        return dtIso;
    var brFormats = new[]
    {
        "dd/MM/yyyy",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy HH:mm:ss"
    };
    if (DateTime.TryParseExact(t, brFormats, br, DateTimeStyles.AllowWhiteSpaces, out var dtBr))
        return dtBr;
    if (DateTime.TryParse(t, br, DateTimeStyles.AllowWhiteSpaces, out var dtBrLoose))
        return dtBrLoose;
    if (DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out var dtInvLoose))
        return dtInvLoose;
    throw new ArgumentException("Data/hora inválida");
}

static string SafeConnSummary(string connString)
{
    try
    {
        var b = new SqlConnectionStringBuilder(connString);
        var auth = b.IntegratedSecurity ? "Integrated" : "Sql";
        return $"Data Source={b.DataSource};Initial Catalog={b.InitialCatalog};Auth={auth}";
    }
    catch
    {
        return "ConnectionString inválida";
    }
}

async Task<(string Name, byte[]? Logo)> GetReportClientInfoAsync(HttpContext http)
{
    int cid = 0;
    try
    {
        var env = LoadEnv();
        if (env.TryGetValue("REPORT_DEFAULT_CLIENT_ID", out var v) && int.TryParse(v, out var parsed) && parsed > 0) cid = parsed;
    }
    catch { }

    if (cid <= 0)
    {
        var clientIdHeader = http.Request.Headers.TryGetValue("X-Client-Id", out var vals) ? vals.ToString() : null;
        if (!int.TryParse(clientIdHeader, out cid) || cid <= 0)
        {
            var claim = http.User?.FindFirst("clientId");
            if (claim != null) int.TryParse(claim.Value, out cid);
        }
    }

    if (cid <= 0) return ("Cliente", null);

    try
    {
        using var cn = new SqlConnection(GetConn("Logins"));
        await cn.OpenAsync(http.RequestAborted);
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT NOME,CAMINHOIMG FROM dbo.ClientesPortal WHERE Id=@id";
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
        using var r = await cmd.ExecuteReaderAsync(http.RequestAborted);
        if (!await r.ReadAsync(http.RequestAborted)) return ("Cliente", null);
        var nome = r.IsDBNull(0) ? null : r.GetString(0);
        var logoPath = r.IsDBNull(1) ? null : r.GetString(1);
        byte[]? logo = null;
        if (!string.IsNullOrWhiteSpace(logoPath) && !logoPath.Contains("://", StringComparison.Ordinal))
        {
            var full = logoPath.StartsWith("/") ? Path.Combine(app.Environment.ContentRootPath, "wwwroot", logoPath.TrimStart('/')) : logoPath;
            if (System.IO.File.Exists(full))
            {
                try { logo = await System.IO.File.ReadAllBytesAsync(full, http.RequestAborted); } catch { }
            }
        }
        var nm = string.IsNullOrWhiteSpace(nome) ? "Cliente" : nome.Trim();
        return (nm, logo);
    }
    catch
    {
        return ("Cliente", null);
    }
}

static string GetReportUser(HttpContext http)
{
    var u = http.User?.FindFirst("usuario")?.Value;
    if (!string.IsNullOrWhiteSpace(u)) return u;
    var n = http.User?.Identity?.Name;
    if (!string.IsNullOrWhiteSpace(n)) return n;
    return "Sistema";
}

byte[] BuildAccessPdf(string clientName, byte[]? clientLogo, string documento, string modo, DateTime? start, DateTime? end, IReadOnlyList<(int Codigo, string? Nome, string? CPF, string? Matricula, string? Empresa, string? Cartao, string? Direcao, string? Tipo, string? Terminal, string? Descricao, DateTime Transito)> rows, string generatedBy)
{
    QuestPDF.Settings.License = LicenseType.Community;
    var title = "Relatório de Acessos";
    var sub = $"Documento: {documento} • Tipo: {modo}";
    if (start != null && end != null) sub += $" • Período: {start:dd/MM/yyyy HH:mm:ss} - {end:dd/MM/yyyy HH:mm:ss}";
    var accent = "#0b3d2e";
    var headerBg = "#0b3d2e";
    var rowAlt = "#f4f7f5";
    var border = "#d1d5db";
    byte[]? leftLogo = null;
    byte[]? rightLogo = clientLogo;
    try
    {
        var env = LoadEnv();
        if (env.TryGetValue("REPORT_LOGO_LEFT", out var lp) && !string.IsNullOrWhiteSpace(lp))
        {
            var full = lp.StartsWith("/") ? Path.Combine(app.Environment.ContentRootPath, "wwwroot", lp.TrimStart('/')) : lp;
            if (System.IO.File.Exists(full)) leftLogo = System.IO.File.ReadAllBytes(full);
        }
        if (rightLogo == null && env.TryGetValue("REPORT_LOGO_RIGHT", out var rp) && !string.IsNullOrWhiteSpace(rp))
        {
            var full = rp.StartsWith("/") ? Path.Combine(app.Environment.ContentRootPath, "wwwroot", rp.TrimStart('/')) : rp;
            if (System.IO.File.Exists(full)) rightLogo = System.IO.File.ReadAllBytes(full);
        }
    }
    catch { }

    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(new QuestPDF.Helpers.PageSize(1190.88f, 841.68f));
            page.Margin(18);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().PaddingBottom(8).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().AlignLeft().Element(e =>
                    {
                        if (leftLogo != null) e.Height(34).Image(leftLogo, ImageScaling.FitHeight);
                        else e.Text("JumperFour").FontSize(18).SemiBold().FontColor(accent);
                    });
                    row.RelativeItem().AlignRight().Element(e =>
                    {
                        if (rightLogo != null) e.Height(34).Image(rightLogo, ImageScaling.FitHeight);
                        else e.Text(clientName).FontSize(16).SemiBold().FontColor("#111827");
                    });
                });
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().AlignLeft().Text(title).SemiBold().FontSize(12);
                    row.RelativeItem().AlignRight().Text(clientName).SemiBold().FontSize(10);
                });
                col.Item().Text(sub).FontSize(9).FontColor("#374151");
                col.Item().PaddingTop(6).LineHorizontal(1);
            });

            page.Content().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.7f);
                    c.RelativeColumn(2.6f);
                    c.RelativeColumn(0.9f);
                    c.RelativeColumn(2.0f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.0f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.8f);
                    c.RelativeColumn(0.9f);
                });

                IContainer HeaderCell(IContainer x) => x
                    .Background(headerBg)
                    .Border(0.5f).BorderColor(border)
                    .PaddingVertical(4).PaddingHorizontal(6)
                    .DefaultTextStyle(s => s.FontColor("#ffffff").SemiBold().FontSize(9));

                IContainer Cell(IContainer x, bool alt) => x
                    .Background(alt ? rowAlt : "#ffffff")
                    .Border(0.5f).BorderColor(border)
                    .PaddingVertical(3).PaddingHorizontal(6);

                table.Header(h =>
                {
                    h.Cell().Element(HeaderCell).Text("DATA E HORA");
                    h.Cell().Element(HeaderCell).Text("TAG");
                    h.Cell().Element(HeaderCell).Text("ACESSO");
                    h.Cell().Element(HeaderCell).Text("EVENTO");
                    h.Cell().Element(HeaderCell).Text("NOME COMPLETO");
                    h.Cell().Element(HeaderCell).Text("DOC / MATRÍCULA");
                    h.Cell().Element(HeaderCell).Text("CARTÃO");
                    h.Cell().Element(HeaderCell).Text("TIPO");
                    h.Cell().Element(HeaderCell).Text("EMPRESA");
                    h.Cell().Element(HeaderCell).Text("STATUS");
                });

                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    var alt = i % 2 == 1;
                    table.Cell().Element(x => Cell(x, alt)).Text(r.Transito.ToString("dd/MM/yyyy HH:mm:ss"));
                    table.Cell().Element(x => Cell(x, alt)).Text(r.Terminal ?? "");
                    table.Cell().Element(x => Cell(x, alt)).Text(r.Descricao ?? "");
                    table.Cell().Element(x => Cell(x, alt)).Text(r.Direcao ?? "");
                    table.Cell().Element(x => Cell(x, alt)).Text(r.Nome ?? "");
                    table.Cell().Element(x => Cell(x, alt)).Text($"{(r.CPF ?? "")}{(string.IsNullOrWhiteSpace(r.Matricula) ? "" : " / " + r.Matricula)}");
                    table.Cell().Element(x => Cell(x, alt)).Text(r.Cartao ?? "");
                    table.Cell().Element(x => Cell(x, alt)).Text(r.Tipo ?? "");
                    table.Cell().Element(x => Cell(x, alt)).Text(r.Empresa ?? "");
                    table.Cell().Element(x => Cell(x, alt)).Text("Liberado");
                }
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(1);
                col.Item().PaddingTop(4).Text($"Gerado por {generatedBy} em {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor("#6b7280");
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().AlignLeft().DefaultTextStyle(s => s.FontSize(9).FontColor("#374151")).Text(t =>
                    {
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });

                    row.RelativeItem().AlignCenter().Text("Relatório - JumperFour").FontSize(9).FontColor("#374151");
                    row.RelativeItem().AlignRight().Text(clientName).FontSize(9).FontColor("#374151");
                });
            });
        });
    }).GeneratePdf();
}

app.MapGet("/api/access/info/by-document", async (string documento) =>
{
    var docRaw = (documento ?? "").Trim();
    var docDigits = DigitsOnly(docRaw);
    var defaultEmpresa = await GetDefaultClientNameAsync();
    var connStr = GetConn("CMS");
    using var cn = new SqlConnection(connStr);
    try
    {
        await cn.OpenAsync();
    }
    catch (SqlException ex) when (ex.Number == 18456)
    {
        return Results.Problem(title: "Falha de autenticação no SQL Server", detail: "Não foi possível autenticar no banco de dados (CMS). Verifique usuário/senha e o modo de autenticação do SQL Server. " + SafeConnSummary(connStr), statusCode: 500);
    }
    using var cmd = cn.CreateCommand();
    cmd.CommandTimeout = 120;
    cmd.CommandText = @"
WITH Persons AS (
    SELECT
        e.SbiID AS SbiID,
        e.Name + ' ' + e.Surname AS Name,
        e.PreferredName AS CPF,
        e.Identifier AS Matricula,
        NULLIF(LTRIM(RTRIM(uf.UF2)), '') AS Empresa,
        'FUNCIONÁRIO' AS Tipo,
        c.CardNumber AS CardNumber,
        e.CommencementDateTime AS Cadastro,
        e.ExpiryDateTime AS Expira
    FROM Employee e
    LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        x.Name + ' ' + x.Surname AS Name,
        x.PreferredName AS CPF,
        x.Identifier AS Matricula,
        COALESCE(ec.Name, NULLIF(LTRIM(RTRIM(uf.UF2)), '')) AS Empresa,
        'TERCEIRO' AS Tipo,
        c.CardNumber AS CardNumber,
        x.CommencementDateTime AS Cadastro,
        x.ExpiryDateTime AS Expira
    FROM ExternalRegular x
    LEFT JOIN ExternalRegularUserFields uf ON uf.SbiID = x.SbiID
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    LEFT JOIN ExternalCompany ec ON ec.ExternalCompanyID = x.ExternalCompanyID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
)
SELECT DISTINCT
    SbiID,
    Name,
    CPF,
    Matricula,
    Empresa,
    Tipo,
    CardNumber,
    Cadastro,
    Expira
FROM Persons
ORDER BY CardNumber, Name;";
    cmd.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
    cmd.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        var tipo = r.IsDBNull(5) ? null : r.GetString(5);
        var empresa = r.IsDBNull(4) ? null : r.GetString(4);
        if (string.IsNullOrWhiteSpace(empresa) && string.Equals(tipo, "FUNCIONÁRIO", StringComparison.OrdinalIgnoreCase))
            empresa = defaultEmpresa;
        list.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CPF = r.IsDBNull(2) ? null : r.GetString(2),
            Matricula = r.IsDBNull(3) ? null : r.GetString(3),
            Empresa = empresa,
            Tipo = tipo,
            CardNumber = r.IsDBNull(6) ? null : r.GetString(6),
            Cadastro = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7),
            Expira = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/access/by-document", async (string documento, string start, string end, string? mode, int page, int pageSize) =>
{
    var docRaw = (documento ?? "").Trim();
    var docDigits = DigitsOnly(docRaw);
    var defaultEmpresa = await GetDefaultClientNameAsync();
    var modeNorm = string.IsNullOrWhiteSpace(mode) ? "all" : mode.Trim().ToLowerInvariant();
    if (modeNorm != "all" && modeNorm != "catracas" && modeNorm != "faciais") modeNorm = "all";

    var startDt = ParseDateTimeAny(start);
    var endDt = ParseDateTimeAny(end);
    if (endDt <= startDt) return Results.BadRequest("Período inválido");
    if ((endDt - startDt).TotalDays > 370)
    {
        return Results.Problem(title: "Período muito grande", detail: "Para períodos acima de 12 meses, use Exportar CSV para gerar o relatório completo sem estourar timeout.", statusCode: 422);
    }

    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;

    var startTicks = startDt.Ticks;
    var endTicks = endDt.Ticks;

    var connStr = GetConn("CMS");
    using var cn = new SqlConnection(connStr);
    try
    {
        await cn.OpenAsync();
    }
    catch (SqlException ex) when (ex.Number == 18456)
    {
        return Results.Problem(title: "Falha de autenticação no SQL Server", detail: "Não foi possível autenticar no banco de dados (CMS). Verifique usuário/senha e o modo de autenticação do SQL Server. " + SafeConnSummary(connStr), statusCode: 500);
    }
    using var cmd = cn.CreateCommand();
    cmd.CommandTimeout = 600;
    cmd.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
    cmd.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
    cmd.Parameters.Add(new SqlParameter("@startTicks", SqlDbType.BigInt) { Value = startTicks });
    cmd.Parameters.Add(new SqlParameter("@endTicks", SqlDbType.BigInt) { Value = endTicks });
    cmd.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });

    cmd.CommandText = @"
WITH People AS (
    SELECT
        e.SbiID AS SbiID,
        e.Name + ' ' + e.Surname AS Name,
        e.PreferredName AS CPF,
        e.Identifier AS Matricula,
        NULLIF(LTRIM(RTRIM(uf.UF2)), '') AS Empresa,
        'FUNCIONÁRIO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        x.Name + ' ' + x.Surname AS Name,
        x.PreferredName AS CPF,
        x.Identifier AS Matricula,
        COALESCE(ec.Name, NULLIF(LTRIM(RTRIM(uf.UF2)), '')) AS Empresa,
        'TERCEIRO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN ExternalRegularUserFields uf ON uf.SbiID = x.SbiID
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    LEFT JOIN ExternalCompany ec ON ec.ExternalCompanyID = x.ExternalCompanyID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
),
EventsFiltered AS (
    SELECT
        p.SbiID,
        p.Name,
        p.CPF,
        p.Matricula,
        p.Empresa,
        p.CardNumber,
        p.TipoPessoa,
        ev.Source AS Terminal,
        ev.Description AS Descricao,
        ev.[Time] AS TimeTicks
    FROM People p
    INNER JOIN [EMSEVENTS].dbo.Events ev
        ON ev.CardNumber = p.CardNumber
    WHERE
        p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
        AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
        AND ev.[Time] >= @startTicks AND ev.[Time] < @endTicks
        AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
        AND (
            @mode = 'all'
            OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
            OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
        )
)
SELECT
    SbiID AS Codigo,
    Name,
    CPF,
    Matricula,
    Empresa,
    CardNumber AS Cartao,
    CASE
        WHEN Descricao LIKE '%ENTRADA%' THEN 'ENTRADA'
        WHEN Descricao LIKE '%SAÍDA%' OR Descricao LIKE '%SAIDA%' THEN 'SAÍDA'
        WHEN Terminal LIKE '%_RDR1' THEN 'ENTRADA'
        WHEN Terminal LIKE '%_RDR2' THEN 'SAÍDA'
        ELSE NULL
    END AS Direcao,
    TipoPessoa AS Tipo,
    Terminal,
    Descricao AS TerminalDescription,
    DATEADD(MILLISECOND, CAST((TimeTicks % 864000000000) / 10000 AS int),
        DATEADD(DAY, CAST(TimeTicks / 864000000000 AS int), CONVERT(datetime2, '0001-01-01'))) AS Transito
FROM EventsFiltered
ORDER BY CardNumber ASC, TimeTicks DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;

WITH People AS (
    SELECT
        e.SbiID AS SbiID,
        c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
)
SELECT COUNT(1) AS Total
FROM People p
INNER JOIN [EMSEVENTS].dbo.Events ev
    ON ev.CardNumber = p.CardNumber
WHERE
    p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
    AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
    AND ev.[Time] >= @startTicks AND ev.[Time] < @endTicks
    AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
    AND (
        @mode = 'all'
        OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
        OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
    );
";

    SqlDataReader r;
    try
    {
        r = await cmd.ExecuteReaderAsync();
    }
    catch (SqlException ex) when (ex.Number == -2)
    {
        return Results.Problem(title: "Consulta muito longa", detail: "A consulta excedeu o tempo limite. Reduza o período, aplique filtros, ou use Exportar CSV para gerar o relatório completo.", statusCode: 504);
    }
    using var _r = r;
    var items = new List<object>();
    while (await _r.ReadAsync())
    {
        var tipo = _r.IsDBNull(7) ? null : _r.GetString(7);
        var empresa = _r.IsDBNull(4) ? null : _r.GetString(4);
        if (string.IsNullOrWhiteSpace(empresa) && string.Equals(tipo, "FUNCIONÁRIO", StringComparison.OrdinalIgnoreCase))
            empresa = defaultEmpresa;
        items.Add(new
        {
            Name = _r.IsDBNull(1) ? null : _r.GetString(1),
            CPF = _r.IsDBNull(2) ? null : _r.GetString(2),
            Matricula = _r.IsDBNull(3) ? null : _r.GetString(3),
            Empresa = empresa,
            Cartao = _r.IsDBNull(5) ? null : _r.GetString(5),
            Direcao = _r.IsDBNull(6) ? null : _r.GetString(6),
            Tipo = tipo,
            Terminal = _r.IsDBNull(8) ? null : _r.GetString(8),
            TerminalDescription = _r.IsDBNull(9) ? null : _r.GetString(9),
            Transito = _r.GetDateTime(10)
        });
    }
    int total = 0;
    if (await _r.NextResultAsync() && await _r.ReadAsync()) total = _r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/access/by-document/all", async (string documento, string? mode, int page, int pageSize) =>
{
    var docRaw = (documento ?? "").Trim();
    var docDigits = DigitsOnly(docRaw);
    var defaultEmpresa = await GetDefaultClientNameAsync();
    var modeNorm = string.IsNullOrWhiteSpace(mode) ? "all" : mode.Trim().ToLowerInvariant();
    if (modeNorm != "all" && modeNorm != "catracas" && modeNorm != "faciais") modeNorm = "all";

    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;

    var connStr = GetConn("CMS");
    using var cn = new SqlConnection(connStr);
    try { await cn.OpenAsync(); }
    catch (SqlException ex) when (ex.Number == 18456)
    {
        return Results.Problem(title: "Falha de autenticação no SQL Server", detail: "Não foi possível autenticar no banco de dados (CMS). Verifique usuário/senha e o modo de autenticação do SQL Server. " + SafeConnSummary(connStr), statusCode: 500);
    }
    using var cmd = cn.CreateCommand();
    cmd.CommandTimeout = 600;
    cmd.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
    cmd.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
    cmd.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    cmd.CommandText = @"
WITH People AS (
    SELECT
        e.SbiID AS SbiID,
        e.Name + ' ' + e.Surname AS Name,
        e.PreferredName AS CPF,
        e.Identifier AS Matricula,
        NULLIF(LTRIM(RTRIM(uf.UF2)), '') AS Empresa,
        'FUNCIONÁRIO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        x.Name + ' ' + x.Surname AS Name,
        x.PreferredName AS CPF,
        x.Identifier AS Matricula,
        COALESCE(ec.Name, NULLIF(LTRIM(RTRIM(uf.UF2)), '')) AS Empresa,
        'TERCEIRO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN ExternalRegularUserFields uf ON uf.SbiID = x.SbiID
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    LEFT JOIN ExternalCompany ec ON ec.ExternalCompanyID = x.ExternalCompanyID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
),
EventsFiltered AS (
    SELECT
        p.SbiID,
        p.Name,
        p.CPF,
        p.Matricula,
        p.Empresa,
        p.CardNumber,
        p.TipoPessoa,
        ev.Source AS Terminal,
        ev.Description AS Descricao,
        ev.[Time] AS TimeTicks
    FROM People p
    INNER JOIN [EMSEVENTS].dbo.Events ev
        ON ev.CardNumber = p.CardNumber
    WHERE
        p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
        AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
        AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
        AND (
            @mode = 'all'
            OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
            OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
        )
)
SELECT
    SbiID AS Codigo,
    Name,
    CPF,
    Matricula,
    Empresa,
    CardNumber AS Cartao,
    CASE
        WHEN Descricao LIKE '%ENTRADA%' THEN 'ENTRADA'
        WHEN Descricao LIKE '%SAÍDA%' OR Descricao LIKE '%SAIDA%' THEN 'SAÍDA'
        WHEN Terminal LIKE '%_RDR1' THEN 'ENTRADA'
        WHEN Terminal LIKE '%_RDR2' THEN 'SAÍDA'
        ELSE NULL
    END AS Direcao,
    TipoPessoa AS Tipo,
    Terminal,
    Descricao AS TerminalDescription,
    DATEADD(MILLISECOND, CAST((TimeTicks % 864000000000) / 10000 AS int),
        DATEADD(DAY, CAST(TimeTicks / 864000000000 AS int), CONVERT(datetime2, '0001-01-01'))) AS Transito
FROM EventsFiltered
ORDER BY CardNumber ASC, TimeTicks DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;

WITH People AS (
    SELECT
        e.SbiID AS SbiID,
        c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
)
SELECT COUNT(1) AS Total
FROM People p
INNER JOIN [EMSEVENTS].dbo.Events ev
    ON ev.CardNumber = p.CardNumber
WHERE
    p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
    AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
    AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
    AND (
        @mode = 'all'
        OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
        OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
    );
";
    SqlDataReader r;
    try
    {
        r = await cmd.ExecuteReaderAsync();
    }
    catch (SqlException ex) when (ex.Number == -2)
    {
        return Results.Problem(title: "Consulta muito longa", detail: "A consulta excedeu o tempo limite. Use um período menor, ou gere o relatório completo via Exportar CSV.", statusCode: 504);
    }
    using var _r = r;
    var items = new List<object>();
    while (await _r.ReadAsync())
    {
        var tipo = _r.IsDBNull(7) ? null : _r.GetString(7);
        var empresa = _r.IsDBNull(4) ? null : _r.GetString(4);
        if (string.IsNullOrWhiteSpace(empresa) && string.Equals(tipo, "FUNCIONÁRIO", StringComparison.OrdinalIgnoreCase))
            empresa = defaultEmpresa;
        items.Add(new
        {
            Name = _r.IsDBNull(1) ? null : _r.GetString(1),
            CPF = _r.IsDBNull(2) ? null : _r.GetString(2),
            Matricula = _r.IsDBNull(3) ? null : _r.GetString(3),
            Empresa = empresa,
            Cartao = _r.IsDBNull(5) ? null : _r.GetString(5),
            Direcao = _r.IsDBNull(6) ? null : _r.GetString(6),
            Tipo = tipo,
            Terminal = _r.IsDBNull(8) ? null : _r.GetString(8),
            TerminalDescription = _r.IsDBNull(9) ? null : _r.GetString(9),
            Transito = _r.GetDateTime(10)
        });
    }
    int total = 0;
    if (await _r.NextResultAsync() && await _r.ReadAsync()) total = _r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/access/by-document/export", async (HttpContext http, string documento, string start, string end, string? mode, string format = "csv") =>
{
    var docRaw = (documento ?? "").Trim();
    var docDigits = DigitsOnly(docRaw);
    var modeNorm = string.IsNullOrWhiteSpace(mode) ? "all" : mode.Trim().ToLowerInvariant();
    if (modeNorm != "all" && modeNorm != "catracas" && modeNorm != "faciais") modeNorm = "all";

    var startDt = ParseDateTimeAny(start);
    var endDt = ParseDateTimeAny(end);
    if (endDt <= startDt) return Results.BadRequest("Período inválido");

    var connStr = GetConn("CMS");
    var fmt = (format ?? "csv").Trim().ToLowerInvariant();
    if (fmt == "excel") fmt = "xlsx";
    if (fmt != "csv" && fmt != "xlsx" && fmt != "pdf") return Results.BadRequest(new { error = "Formato inválido" });
    string fileName = $"acessos-{docDigits}.{fmt}";

    static string Csv(string? s)
    {
        if (s == null) return "";
        var needs = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        if (!needs) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    if (fmt == "csv")
    {
        return Results.Stream(async (stream) =>
        {
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), 64 * 1024, leaveOpen: true);
            await writer.WriteLineAsync("Cracha,Nome,CPF,Matricula,Empresa,Direcao,Tipo,Terminal,Descricao,Transito");

            using var cn = new SqlConnection(connStr);
            try { await cn.OpenAsync(http.RequestAborted); }
            catch (SqlException ex) when (ex.Number == 18456)
            {
                await writer.WriteLineAsync(Csv("Falha de autenticação no SQL Server (CMS)"));
                await writer.FlushAsync();
                return;
            }

            var chunkEnd = endDt;
            while (chunkEnd > startDt)
            {
                var chunkStart = chunkEnd.AddDays(-30);
                if (chunkStart < startDt) chunkStart = startDt;

                using var cmd = cn.CreateCommand();
                cmd.CommandTimeout = 600;
                cmd.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
                cmd.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
                cmd.Parameters.Add(new SqlParameter("@startTicks", SqlDbType.BigInt) { Value = chunkStart.Ticks });
                cmd.Parameters.Add(new SqlParameter("@endTicks", SqlDbType.BigInt) { Value = chunkEnd.Ticks });
                cmd.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
                cmd.CommandText = @"
WITH People AS (
    SELECT
        e.SbiID AS SbiID,
        e.Name + ' ' + e.Surname AS Name,
        e.PreferredName AS CPF,
        e.Identifier AS Matricula,
        uf.UF2 AS Empresa,
        'FUNCIONÁRIO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        x.Name + ' ' + x.Surname AS Name,
        x.PreferredName AS CPF,
        x.Identifier AS Matricula,
        uf.UF2 AS Empresa,
        'TERCEIRO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN ExternalRegularUserFields uf ON uf.SbiID = x.SbiID
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
),
EventsFiltered AS (
    SELECT
        p.SbiID,
        p.Name,
        p.CPF,
        p.Matricula,
        p.Empresa,
        p.CardNumber,
        p.TipoPessoa,
        ev.Source AS Terminal,
        ev.Description AS Descricao,
        ev.[Time] AS TimeTicks
    FROM People p
    INNER JOIN [EMSEVENTS].dbo.Events ev
        ON ev.CardNumber = p.CardNumber
    WHERE
        p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
        AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
        AND ev.[Time] >= @startTicks AND ev.[Time] < @endTicks
        AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
        AND (
            @mode = 'all'
            OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
            OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
        )
)
SELECT
    SbiID AS Codigo,
    Name,
    CPF,
    Matricula,
    Empresa,
    CardNumber AS Cartao,
    CASE
        WHEN Descricao LIKE '%ENTRADA%' THEN 'ENTRADA'
        WHEN Descricao LIKE '%SAÍDA%' OR Descricao LIKE '%SAIDA%' THEN 'SAÍDA'
        WHEN Terminal LIKE '%_RDR1' THEN 'ENTRADA'
        WHEN Terminal LIKE '%_RDR2' THEN 'SAÍDA'
        ELSE NULL
    END AS Direcao,
    TipoPessoa AS Tipo,
    Terminal,
    Descricao AS TerminalDescription,
    DATEADD(MILLISECOND, CAST((TimeTicks % 864000000000) / 10000 AS int),
        DATEADD(DAY, CAST(TimeTicks / 864000000000 AS int), CONVERT(datetime2, '0001-01-01'))) AS Transito
FROM EventsFiltered
ORDER BY CardNumber ASC, TimeTicks DESC;
";
                SqlDataReader r;
                try
                {
                    r = await cmd.ExecuteReaderAsync(http.RequestAborted);
                }
                catch (SqlException ex) when (ex.Number == -2)
                {
                    await writer.WriteLineAsync(Csv("TIMEOUT: consulta excedeu o tempo limite. Use um período menor ou gere em partes."));
                    await writer.FlushAsync();
                    return;
                }
                using var _r = r;
                while (await _r.ReadAsync(http.RequestAborted))
                {
                    var line =
                        Csv(_r.IsDBNull(5) ? null : _r.GetString(5)) + "," +
                        Csv(_r.IsDBNull(1) ? null : _r.GetString(1)) + "," +
                        Csv(_r.IsDBNull(2) ? null : _r.GetString(2)) + "," +
                        Csv(_r.IsDBNull(3) ? null : _r.GetString(3)) + "," +
                        Csv(_r.IsDBNull(4) ? null : _r.GetString(4)) + "," +
                        Csv(_r.IsDBNull(6) ? null : _r.GetString(6)) + "," +
                        Csv(_r.IsDBNull(7) ? null : _r.GetString(7)) + "," +
                        Csv(_r.IsDBNull(8) ? null : _r.GetString(8)) + "," +
                        Csv(_r.IsDBNull(9) ? null : _r.GetString(9)) + "," +
                        _r.GetDateTime(10).ToString("yyyy-MM-dd HH:mm:ss");
                    await writer.WriteLineAsync(line);
                }
                await writer.FlushAsync();

                chunkEnd = chunkStart;
            }
        }, "text/csv", fileName);
    }

    var maxRows = fmt == "pdf" ? 5000 : 20000;
    using var cnAll = new SqlConnection(connStr);
    try { await cnAll.OpenAsync(http.RequestAborted); }
    catch (SqlException ex) when (ex.Number == 18456)
    {
        return Results.Problem(title: "Falha de autenticação no SQL Server", detail: "Não foi possível autenticar no banco de dados (CMS).", statusCode: 500);
    }

    int total = 0;
    {
        using var cmdCount = cnAll.CreateCommand();
        cmdCount.CommandTimeout = 600;
        cmdCount.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
        cmdCount.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
        cmdCount.Parameters.Add(new SqlParameter("@startTicks", SqlDbType.BigInt) { Value = startDt.Ticks });
        cmdCount.Parameters.Add(new SqlParameter("@endTicks", SqlDbType.BigInt) { Value = endDt.Ticks });
        cmdCount.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
        cmdCount.CommandText = @"
WITH People AS (
    SELECT c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
)
SELECT COUNT(1) AS Total
FROM People p
INNER JOIN [EMSEVENTS].dbo.Events ev
    ON ev.CardNumber = p.CardNumber
WHERE
    p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
    AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
    AND ev.[Time] >= @startTicks AND ev.[Time] < @endTicks
    AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
    AND (
        @mode = 'all'
        OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
        OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
    );
";
        try
        {
            using var rCount = await cmdCount.ExecuteReaderAsync(http.RequestAborted);
            if (await rCount.ReadAsync(http.RequestAborted)) total = rCount.IsDBNull(0) ? 0 : rCount.GetInt32(0);
        }
        catch (SqlException ex) when (ex.Number == -2)
        {
            return Results.Problem(title: "Consulta muito longa", detail: "A consulta excedeu o tempo limite. Use um período menor ou exporte em CSV.", statusCode: 504);
        }
    }

    if (total > maxRows)
    {
        return Results.Problem(title: "Consulta muito grande para este formato", detail: $"Total de {total} registros. Para PDF/XLSX, limite em até {maxRows} registros ou use CSV.", statusCode: 422);
    }

    var rows = new List<(int Codigo, string? Nome, string? CPF, string? Matricula, string? Empresa, string? Cartao, string? Direcao, string? Tipo, string? Terminal, string? Descricao, DateTime Transito)>();
    {
        using var cmdAll = cnAll.CreateCommand();
        cmdAll.CommandTimeout = 600;
        cmdAll.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
        cmdAll.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
        cmdAll.Parameters.Add(new SqlParameter("@startTicks", SqlDbType.BigInt) { Value = startDt.Ticks });
        cmdAll.Parameters.Add(new SqlParameter("@endTicks", SqlDbType.BigInt) { Value = endDt.Ticks });
        cmdAll.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
        cmdAll.CommandText = @"
WITH People AS (
    SELECT
        e.SbiID AS SbiID,
        e.Name + ' ' + e.Surname AS Name,
        e.PreferredName AS CPF,
        e.Identifier AS Matricula,
        uf.UF2 AS Empresa,
        'FUNCIONÁRIO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        x.Name + ' ' + x.Surname AS Name,
        x.PreferredName AS CPF,
        x.Identifier AS Matricula,
        uf.UF2 AS Empresa,
        'TERCEIRO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN ExternalRegularUserFields uf ON uf.SbiID = x.SbiID
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
),
EventsFiltered AS (
    SELECT
        p.SbiID,
        p.Name,
        p.CPF,
        p.Matricula,
        p.Empresa,
        p.CardNumber,
        p.TipoPessoa,
        ev.Source AS Terminal,
        ev.Description AS Descricao,
        ev.[Time] AS TimeTicks
    FROM People p
    INNER JOIN [EMSEVENTS].dbo.Events ev
        ON ev.CardNumber = p.CardNumber
    WHERE
        p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
        AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
        AND ev.[Time] >= @startTicks AND ev.[Time] < @endTicks
        AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
        AND (
            @mode = 'all'
            OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
            OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
        )
)
SELECT
    SbiID AS Codigo,
    Name,
    CPF,
    Matricula,
    Empresa,
    CardNumber AS Cartao,
    CASE
        WHEN Descricao LIKE '%ENTRADA%' THEN 'ENTRADA'
        WHEN Descricao LIKE '%SAÍDA%' OR Descricao LIKE '%SAIDA%' THEN 'SAÍDA'
        WHEN Terminal LIKE '%_RDR1' THEN 'ENTRADA'
        WHEN Terminal LIKE '%_RDR2' THEN 'SAÍDA'
        ELSE NULL
    END AS Direcao,
    TipoPessoa AS Tipo,
    Terminal,
    Descricao AS TerminalDescription,
    DATEADD(MILLISECOND, CAST((TimeTicks % 864000000000) / 10000 AS int),
        DATEADD(DAY, CAST(TimeTicks / 864000000000 AS int), CONVERT(datetime2, '0001-01-01'))) AS Transito
FROM EventsFiltered
ORDER BY TimeTicks DESC;
";
        try
        {
            using var rAll = await cmdAll.ExecuteReaderAsync(http.RequestAborted);
            while (await rAll.ReadAsync(http.RequestAborted))
            {
                rows.Add((
                    rAll.GetInt32(0),
                    rAll.IsDBNull(1) ? null : rAll.GetString(1),
                    rAll.IsDBNull(2) ? null : rAll.GetString(2),
                    rAll.IsDBNull(3) ? null : rAll.GetString(3),
                    rAll.IsDBNull(4) ? null : rAll.GetString(4),
                    rAll.IsDBNull(5) ? null : rAll.GetString(5),
                    rAll.IsDBNull(6) ? null : rAll.GetString(6),
                    rAll.IsDBNull(7) ? null : rAll.GetString(7),
                    rAll.IsDBNull(8) ? null : rAll.GetString(8),
                    rAll.IsDBNull(9) ? null : rAll.GetString(9),
                    rAll.GetDateTime(10)
                ));
            }
        }
        catch (SqlException ex) when (ex.Number == -2)
        {
            return Results.Problem(title: "Consulta muito longa", detail: "A consulta excedeu o tempo limite. Use um período menor ou exporte em CSV.", statusCode: 504);
        }
    }

    if (fmt == "xlsx")
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
            var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = doc.WorkbookPart!.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "Acessos" });
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>()!;
            void AddRow(params string[] cells)
            {
                var row = new Row();
                foreach (var c in cells) row.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c ?? "") });
                sheetData.Append(row);
            }
            AddRow("Cracha","Nome","CPF","Matricula","Empresa","Direcao","Tipo","Terminal","Descricao","Transito");
            foreach (var x in rows)
            {
                AddRow(x.Cartao ?? "", x.Nome ?? "", x.CPF ?? "", x.Matricula ?? "", x.Empresa ?? "", x.Direcao ?? "", x.Tipo ?? "", x.Terminal ?? "", x.Descricao ?? "", x.Transito.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    var clientInfo = await GetReportClientInfoAsync(http);
    var bytes = BuildAccessPdf(clientInfo.Name, clientInfo.Logo, docRaw, modeNorm, startDt, endDt, rows, GetReportUser(http));
    return Results.File(bytes, "application/pdf", fileName);
}).RequireAuthorization();

app.MapGet("/api/access/by-document/all/export", async (HttpContext http, string documento, string? mode, string format = "csv") =>
{
    var docRaw = (documento ?? "").Trim();
    var docDigits = DigitsOnly(docRaw);
    var modeNorm = string.IsNullOrWhiteSpace(mode) ? "all" : mode.Trim().ToLowerInvariant();
    if (modeNorm != "all" && modeNorm != "catracas" && modeNorm != "faciais") modeNorm = "all";

    var connStr = GetConn("CMS");
    var fmt = (format ?? "csv").Trim().ToLowerInvariant();
    if (fmt == "excel") fmt = "xlsx";
    if (fmt != "csv" && fmt != "xlsx" && fmt != "pdf") return Results.BadRequest(new { error = "Formato inválido" });
    string fileName = $"acessos-{docDigits}-all.{fmt}";

    static string Csv(string? s)
    {
        if (s == null) return "";
        var needs = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        if (!needs) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    if (fmt == "csv")
    {
        return Results.Stream(async (stream) =>
        {
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), 64 * 1024, leaveOpen: true);
            await writer.WriteLineAsync("Cracha,Nome,CPF,Matricula,Empresa,Direcao,Tipo,Terminal,Descricao,Transito");

            using var cn = new SqlConnection(connStr);
            try { await cn.OpenAsync(http.RequestAborted); }
            catch (SqlException ex) when (ex.Number == 18456)
            {
                await writer.WriteLineAsync(Csv("Falha de autenticação no SQL Server (CMS)"));
                await writer.FlushAsync();
                return;
            }

            long minTicks = 0;
            long maxTicks = 0;
            {
                using var cmdRange = cn.CreateCommand();
                cmdRange.CommandTimeout = 600;
                cmdRange.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
                cmdRange.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
                cmdRange.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
                cmdRange.CommandText = @"
WITH People AS (
    SELECT c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
)
SELECT MIN(ev.[Time]) AS MinTicks, MAX(ev.[Time]) AS MaxTicks
FROM People p
INNER JOIN [EMSEVENTS].dbo.Events ev
    ON ev.CardNumber = p.CardNumber
WHERE
    p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
    AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
    AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
    AND (
        @mode = 'all'
        OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
        OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
    );
";
                using var rr = await cmdRange.ExecuteReaderAsync(http.RequestAborted);
                if (await rr.ReadAsync(http.RequestAborted))
                {
                    if (!rr.IsDBNull(0)) minTicks = rr.GetInt64(0);
                    if (!rr.IsDBNull(1)) maxTicks = rr.GetInt64(1);
                }
            }

            if (minTicks <= 0 || maxTicks <= 0 || maxTicks <= minTicks)
            {
                await writer.FlushAsync();
                return;
            }

            var startDt = new DateTime(minTicks);
            var endDt = new DateTime(maxTicks).AddSeconds(1);
            var chunkEnd = endDt;
            while (chunkEnd > startDt)
            {
                var chunkStart = chunkEnd.AddDays(-30);
                if (chunkStart < startDt) chunkStart = startDt;

                using var cmd = cn.CreateCommand();
                cmd.CommandTimeout = 600;
                cmd.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
                cmd.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
                cmd.Parameters.Add(new SqlParameter("@startTicks", SqlDbType.BigInt) { Value = chunkStart.Ticks });
                cmd.Parameters.Add(new SqlParameter("@endTicks", SqlDbType.BigInt) { Value = chunkEnd.Ticks });
                cmd.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
                cmd.CommandText = @"
WITH People AS (
    SELECT
        e.SbiID AS SbiID,
        e.Name + ' ' + e.Surname AS Name,
        e.PreferredName AS CPF,
        e.Identifier AS Matricula,
        uf.UF2 AS Empresa,
        'FUNCIONÁRIO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        x.Name + ' ' + x.Surname AS Name,
        x.PreferredName AS CPF,
        x.Identifier AS Matricula,
        uf.UF2 AS Empresa,
        'TERCEIRO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN ExternalRegularUserFields uf ON uf.SbiID = x.SbiID
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
),
EventsFiltered AS (
    SELECT
        p.SbiID,
        p.Name,
        p.CPF,
        p.Matricula,
        p.Empresa,
        p.CardNumber,
        p.TipoPessoa,
        ev.Source AS Terminal,
        ev.Description AS Descricao,
        ev.[Time] AS TimeTicks
    FROM People p
    INNER JOIN [EMSEVENTS].dbo.Events ev
        ON ev.CardNumber = p.CardNumber
    WHERE
        p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
        AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
        AND ev.[Time] >= @startTicks AND ev.[Time] < @endTicks
        AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
        AND (
            @mode = 'all'
            OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
            OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
        )
)
SELECT
    SbiID AS Codigo,
    Name,
    CPF,
    Matricula,
    Empresa,
    CardNumber AS Cartao,
    CASE
        WHEN Descricao LIKE '%ENTRADA%' THEN 'ENTRADA'
        WHEN Descricao LIKE '%SAÍDA%' OR Descricao LIKE '%SAIDA%' THEN 'SAÍDA'
        WHEN Terminal LIKE '%_RDR1' THEN 'ENTRADA'
        WHEN Terminal LIKE '%_RDR2' THEN 'SAÍDA'
        ELSE NULL
    END AS Direcao,
    TipoPessoa AS Tipo,
    Terminal,
    Descricao AS TerminalDescription,
    DATEADD(MILLISECOND, CAST((TimeTicks % 864000000000) / 10000 AS int),
        DATEADD(DAY, CAST(TimeTicks / 864000000000 AS int), CONVERT(datetime2, '0001-01-01'))) AS Transito
FROM EventsFiltered
ORDER BY CardNumber ASC, TimeTicks DESC;
";
                SqlDataReader r;
                try { r = await cmd.ExecuteReaderAsync(http.RequestAborted); }
                catch (SqlException ex) when (ex.Number == -2)
                {
                    await writer.WriteLineAsync(Csv("TIMEOUT: consulta excedeu o tempo limite. Use um período menor ou gere em partes."));
                    await writer.FlushAsync();
                    return;
                }
                using var _r = r;
                while (await _r.ReadAsync(http.RequestAborted))
                {
                    var line =
                        Csv(_r.IsDBNull(5) ? null : _r.GetString(5)) + "," +
                        Csv(_r.IsDBNull(1) ? null : _r.GetString(1)) + "," +
                        Csv(_r.IsDBNull(2) ? null : _r.GetString(2)) + "," +
                        Csv(_r.IsDBNull(3) ? null : _r.GetString(3)) + "," +
                        Csv(_r.IsDBNull(4) ? null : _r.GetString(4)) + "," +
                        Csv(_r.IsDBNull(6) ? null : _r.GetString(6)) + "," +
                        Csv(_r.IsDBNull(7) ? null : _r.GetString(7)) + "," +
                        Csv(_r.IsDBNull(8) ? null : _r.GetString(8)) + "," +
                        Csv(_r.IsDBNull(9) ? null : _r.GetString(9)) + "," +
                        _r.GetDateTime(10).ToString("yyyy-MM-dd HH:mm:ss");
                    await writer.WriteLineAsync(line);
                }
                await writer.FlushAsync();
                chunkEnd = chunkStart;
            }
        }, "text/csv", fileName);
    }

    var maxRows = fmt == "pdf" ? 5000 : 20000;
    using var cnAll = new SqlConnection(connStr);
    try { await cnAll.OpenAsync(http.RequestAborted); }
    catch (SqlException ex) when (ex.Number == 18456)
    {
        return Results.Problem(title: "Falha de autenticação no SQL Server", detail: "Não foi possível autenticar no banco de dados (CMS).", statusCode: 500);
    }

    int total = 0;
    {
        using var cmdCount = cnAll.CreateCommand();
        cmdCount.CommandTimeout = 600;
        cmdCount.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
        cmdCount.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
        cmdCount.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
        cmdCount.CommandText = @"
WITH People AS (
    SELECT c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
)
SELECT COUNT(1) AS Total
FROM People p
INNER JOIN [EMSEVENTS].dbo.Events ev
    ON ev.CardNumber = p.CardNumber
WHERE
    p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
    AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
    AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
    AND (
        @mode = 'all'
        OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
        OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
    );
";
        try
        {
            using var rCount = await cmdCount.ExecuteReaderAsync(http.RequestAborted);
            if (await rCount.ReadAsync(http.RequestAborted)) total = rCount.IsDBNull(0) ? 0 : rCount.GetInt32(0);
        }
        catch (SqlException ex) when (ex.Number == -2)
        {
            return Results.Problem(title: "Consulta muito longa", detail: "A consulta excedeu o tempo limite. Use Exportar CSV.", statusCode: 504);
        }
    }

    if (total > maxRows)
    {
        return Results.Problem(title: "Consulta muito grande para este formato", detail: $"Total de {total} registros. Para PDF/XLSX, limite em até {maxRows} registros ou use CSV.", statusCode: 422);
    }

    DateTime? minDt = null;
    DateTime? maxDt = null;
    {
        using var cmdRange = cnAll.CreateCommand();
        cmdRange.CommandTimeout = 600;
        cmdRange.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
        cmdRange.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
        cmdRange.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
        cmdRange.CommandText = @"
WITH People AS (
    SELECT c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
)
SELECT
    MIN(DATEADD(MILLISECOND, CAST((ev.[Time] % 864000000000) / 10000 AS int),
        DATEADD(DAY, CAST(ev.[Time] / 864000000000 AS int), CONVERT(datetime2, '0001-01-01')))) AS MinDt,
    MAX(DATEADD(MILLISECOND, CAST((ev.[Time] % 864000000000) / 10000 AS int),
        DATEADD(DAY, CAST(ev.[Time] / 864000000000 AS int), CONVERT(datetime2, '0001-01-01')))) AS MaxDt
FROM People p
INNER JOIN [EMSEVENTS].dbo.Events ev
    ON ev.CardNumber = p.CardNumber
WHERE
    p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
    AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
    AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
    AND (
        @mode = 'all'
        OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
        OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
    );
";
        try
        {
            using var rr = await cmdRange.ExecuteReaderAsync(http.RequestAborted);
            if (await rr.ReadAsync(http.RequestAborted))
            {
                if (!rr.IsDBNull(0)) minDt = rr.GetDateTime(0);
                if (!rr.IsDBNull(1)) maxDt = rr.GetDateTime(1);
            }
        }
        catch (SqlException ex) when (ex.Number == -2)
        {
            return Results.Problem(title: "Consulta muito longa", detail: "A consulta excedeu o tempo limite. Use Exportar CSV.", statusCode: 504);
        }
    }

    if (minDt == null || maxDt == null || maxDt <= minDt)
    {
        if (fmt == "xlsx")
        {
            using var ms = new MemoryStream();
            using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
            {
                var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
                var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
                var sheets = doc.WorkbookPart!.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "Acessos" });
                var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>()!;
                var header = new Row();
                foreach (var c in new[] { "Cracha","Nome","CPF","Matricula","Empresa","Direcao","Tipo","Terminal","Descricao","Transito" })
                    header.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c) });
                sheetData.Append(header);
            }
            return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        var reportClientInfo = await GetReportClientInfoAsync(http);
        var bytesEmpty = BuildAccessPdf(reportClientInfo.Name, reportClientInfo.Logo, docRaw, modeNorm, null, null, Array.Empty<(int Codigo, string? Nome, string? CPF, string? Matricula, string? Empresa, string? Cartao, string? Direcao, string? Tipo, string? Terminal, string? Descricao, DateTime Transito)>(), GetReportUser(http));
        return Results.File(bytesEmpty, "application/pdf", fileName);
    }

    var rows = new List<(int Codigo, string? Nome, string? CPF, string? Matricula, string? Empresa, string? Cartao, string? Direcao, string? Tipo, string? Terminal, string? Descricao, DateTime Transito)>();
    {
        using var cmdAll = cnAll.CreateCommand();
        cmdAll.CommandTimeout = 600;
        cmdAll.Parameters.Add(new SqlParameter("@docRaw", SqlDbType.NVarChar, 80) { Value = docRaw });
        cmdAll.Parameters.Add(new SqlParameter("@docDigits", SqlDbType.NVarChar, 80) { Value = docDigits });
        cmdAll.Parameters.Add(new SqlParameter("@startTicks", SqlDbType.BigInt) { Value = minDt!.Value.Ticks });
        cmdAll.Parameters.Add(new SqlParameter("@endTicks", SqlDbType.BigInt) { Value = maxDt!.Value.Ticks + 1 });
        cmdAll.Parameters.Add(new SqlParameter("@mode", SqlDbType.VarChar, 20) { Value = modeNorm });
        cmdAll.CommandText = @"
WITH People AS (
    SELECT
        e.SbiID AS SbiID,
        e.Name + ' ' + e.Surname AS Name,
        e.PreferredName AS CPF,
        e.Identifier AS Matricula,
        NULLIF(LTRIM(RTRIM(uf.UF2)), '') AS Empresa,
        'FUNCIONÁRIO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM Employee e
    LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    WHERE
        e.PreferredName = @docRaw OR e.Identifier = @docRaw OR e.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(e.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
    UNION ALL
    SELECT
        x.SbiID AS SbiID,
        x.Name + ' ' + x.Surname AS Name,
        x.PreferredName AS CPF,
        x.Identifier AS Matricula,
        COALESCE(ec.Name, NULLIF(LTRIM(RTRIM(uf.UF2)), '')) AS Empresa,
        'TERCEIRO' AS TipoPessoa,
        c.CardNumber AS CardNumber
    FROM ExternalRegular x
    LEFT JOIN ExternalRegularUserFields uf ON uf.SbiID = x.SbiID
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    LEFT JOIN ExternalCompany ec ON ec.ExternalCompanyID = x.ExternalCompanyID
    WHERE
        x.PreferredName = @docRaw OR x.Identifier = @docRaw OR x.AlternateIdentifier = @docRaw OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.PreferredName, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.Identifier, '.', ''), '-', ''), ' ', '') = @docDigits) OR
        (@docDigits <> '' AND REPLACE(REPLACE(REPLACE(x.AlternateIdentifier, '.', ''), '-', ''), ' ', '') = @docDigits)
),
EventsFiltered AS (
    SELECT
        p.SbiID,
        p.Name,
        p.CPF,
        p.Matricula,
        p.Empresa,
        p.CardNumber,
        p.TipoPessoa,
        ev.Source AS Terminal,
        ev.Description AS Descricao,
        ev.[Time] AS TimeTicks
    FROM People p
    INNER JOIN [EMSEVENTS].dbo.Events ev
        ON ev.CardNumber = p.CardNumber
    WHERE
        p.CardNumber IS NOT NULL AND LTRIM(RTRIM(p.CardNumber)) <> ''
        AND ev.CardNumber IS NOT NULL AND LTRIM(RTRIM(ev.CardNumber)) <> ''
        AND ev.[Time] >= @startTicks AND ev.[Time] < @endTicks
        AND (ev.ConditionName = 'GRANTED' OR ev.AccessReason = 'Granted')
        AND (
            @mode = 'all'
            OR (@mode = 'catracas' AND (ev.Source LIKE '%_CNT%' OR ev.Description LIKE '%CATRACA%' OR ev.Description LIKE '%Catraca%'))
            OR (@mode = 'faciais' AND (ev.Source LIKE '%FAC%' OR ev.Source LIKE '%FACE%' OR ev.Description LIKE '%FACIAL%' OR ev.Description LIKE '%Facial%'))
        )
)
SELECT
    SbiID AS Codigo,
    Name,
    CPF,
    Matricula,
    Empresa,
    CardNumber AS Cartao,
    CASE
        WHEN Descricao LIKE '%ENTRADA%' THEN 'ENTRADA'
        WHEN Descricao LIKE '%SAÍDA%' OR Descricao LIKE '%SAIDA%' THEN 'SAÍDA'
        WHEN Terminal LIKE '%_RDR1' THEN 'ENTRADA'
        WHEN Terminal LIKE '%_RDR2' THEN 'SAÍDA'
        ELSE NULL
    END AS Direcao,
    TipoPessoa AS Tipo,
    Terminal,
    Descricao AS TerminalDescription,
    DATEADD(MILLISECOND, CAST((TimeTicks % 864000000000) / 10000 AS int),
        DATEADD(DAY, CAST(TimeTicks / 864000000000 AS int), CONVERT(datetime2, '0001-01-01'))) AS Transito
FROM EventsFiltered
ORDER BY CardNumber ASC, TimeTicks DESC;
";
        try
        {
            using var rAll = await cmdAll.ExecuteReaderAsync(http.RequestAborted);
            while (await rAll.ReadAsync(http.RequestAborted))
            {
                rows.Add((
                    rAll.GetInt32(0),
                    rAll.IsDBNull(1) ? null : rAll.GetString(1),
                    rAll.IsDBNull(2) ? null : rAll.GetString(2),
                    rAll.IsDBNull(3) ? null : rAll.GetString(3),
                    rAll.IsDBNull(4) ? null : rAll.GetString(4),
                    rAll.IsDBNull(5) ? null : rAll.GetString(5),
                    rAll.IsDBNull(6) ? null : rAll.GetString(6),
                    rAll.IsDBNull(7) ? null : rAll.GetString(7),
                    rAll.IsDBNull(8) ? null : rAll.GetString(8),
                    rAll.IsDBNull(9) ? null : rAll.GetString(9),
                    rAll.GetDateTime(10)
                ));
            }
        }
        catch (SqlException ex) when (ex.Number == -2)
        {
            return Results.Problem(title: "Consulta muito longa", detail: "A consulta excedeu o tempo limite. Use Exportar CSV.", statusCode: 504);
        }
    }

    if (fmt == "xlsx")
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wb = doc.AddWorkbookPart(); wb.Workbook = new Workbook();
            var wsPart = wb.AddNewPart<WorksheetPart>(); wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = doc.WorkbookPart!.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet() { Id = doc.WorkbookPart.GetIdOfPart(wsPart), SheetId = 1, Name = "Acessos" });
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>()!;
            void AddRow(params string[] cells)
            {
                var row = new Row();
                foreach (var c in cells) row.Append(new Cell() { DataType = CellValues.String, CellValue = new CellValue(c ?? "") });
                sheetData.Append(row);
            }
            AddRow("Cracha","Nome","CPF","Matricula","Empresa","Direcao","Tipo","Terminal","Descricao","Transito");
            foreach (var x in rows)
                AddRow(x.Cartao ?? "", x.Nome ?? "", x.CPF ?? "", x.Matricula ?? "", x.Empresa ?? "", x.Direcao ?? "", x.Tipo ?? "", x.Terminal ?? "", x.Descricao ?? "", x.Transito.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    var clientInfo = await GetReportClientInfoAsync(http);
    var startDt = minDt!.Value;
    var endDt = maxDt!.Value;
    var bytes = BuildAccessPdf(clientInfo.Name, clientInfo.Logo, docRaw, modeNorm, startDt, endDt, rows, GetReportUser(http));
    return Results.File(bytes, "application/pdf", fileName);
}).RequireAuthorization();

app.MapGet("/api/cms/person/by-card-info", async (string card) =>
{
    var defaultEmpresa = await GetDefaultClientNameAsync();
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT DISTINCT
    e.SbiID,
    e.Name + ' ' + e.Surname AS Name,
    e.PreferredName AS CPF,
    e.Identifier AS Matricula,
    NULLIF(LTRIM(RTRIM(uf.UF2)), '') AS Empresa,
    'FUNCIONÁRIO' AS Tipo,
    c.CardNumber,
    e.CommencementDateTime AS Cadastro,
    e.ExpiryDateTime AS Expira
FROM Employee e
LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
INNER JOIN Card c ON c.SbiID = e.SbiID
WHERE c.CardNumber = @card
UNION
SELECT DISTINCT
    x.SbiID,
    x.Name + ' ' + x.Surname AS Name,
    x.PreferredName AS CPF,
    x.Identifier AS Matricula,
    COALESCE(ec.Name, NULLIF(LTRIM(RTRIM(ux.UF2)), '')) AS Empresa,
    'TERCEIRO' AS Tipo,
    c.CardNumber,
    x.CommencementDateTime AS Cadastro,
    x.ExpiryDateTime AS Expira
FROM ExternalRegular x
LEFT JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
INNER JOIN Card c ON c.SbiID = x.SbiID
LEFT JOIN ExternalCompany ec ON ec.ExternalCompanyID = x.ExternalCompanyID
WHERE c.CardNumber = @card";
    cmd.Parameters.Add(new SqlParameter("@card", SqlDbType.VarChar) { Value = card });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        var tipo = r.IsDBNull(5) ? null : r.GetString(5);
        var empresa = r.IsDBNull(4) ? null : r.GetString(4);
        if (string.IsNullOrWhiteSpace(empresa) && string.Equals(tipo, "FUNCIONÁRIO", StringComparison.OrdinalIgnoreCase))
            empresa = defaultEmpresa;
        list.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CPF = r.IsDBNull(2) ? null : r.GetString(2),
            Matricula = r.IsDBNull(3) ? null : r.GetString(3),
            Empresa = empresa,
            Tipo = tipo,
            CardNumber = r.IsDBNull(6) ? null : r.GetString(6),
            Cadastro = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7),
            Expira = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/cms/person/by-matricula-info", async (string matricula) =>
{
    var defaultEmpresa = await GetDefaultClientNameAsync();
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT DISTINCT
    e.SbiID,
    e.Name + ' ' + e.Surname AS Name,
    e.PreferredName AS CPF,
    e.Identifier AS Matricula,
    NULLIF(LTRIM(RTRIM(uf.UF2)), '') AS Empresa,
    'FUNCIONÁRIO' AS Tipo,
    c.CardNumber,
    e.CommencementDateTime AS Cadastro,
    e.ExpiryDateTime AS Expira
FROM Employee e
LEFT JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
LEFT JOIN Card c ON c.SbiID = e.SbiID
WHERE e.Identifier = @matricula
UNION
SELECT DISTINCT
    x.SbiID,
    x.Name + ' ' + x.Surname AS Name,
    x.PreferredName AS CPF,
    x.Identifier AS Matricula,
    COALESCE(ec.Name, NULLIF(LTRIM(RTRIM(ux.UF2)), '')) AS Empresa,
    'TERCEIRO' AS Tipo,
    c.CardNumber,
    x.CommencementDateTime AS Cadastro,
    x.ExpiryDateTime AS Expira
FROM ExternalRegular x
LEFT JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
LEFT JOIN Card c ON c.SbiID = x.SbiID
LEFT JOIN ExternalCompany ec ON ec.ExternalCompanyID = x.ExternalCompanyID
WHERE x.Identifier = @matricula";
    cmd.Parameters.Add(new SqlParameter("@matricula", SqlDbType.VarChar) { Value = matricula });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        var tipo = r.IsDBNull(5) ? null : r.GetString(5);
        var empresa = r.IsDBNull(4) ? null : r.GetString(4);
        if (string.IsNullOrWhiteSpace(empresa) && string.Equals(tipo, "FUNCIONÁRIO", StringComparison.OrdinalIgnoreCase))
            empresa = defaultEmpresa;
        list.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CPF = r.IsDBNull(2) ? null : r.GetString(2),
            Matricula = r.IsDBNull(3) ? null : r.GetString(3),
            Empresa = empresa,
            Tipo = tipo,
            CardNumber = r.IsDBNull(6) ? null : r.GetString(6),
            Cadastro = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7),
            Expira = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/cms/transit/by-matricula", async (string matricula, DateTime start, DateTime end, bool onlyTurnstiles, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var whereEmployee = "WHERE e.Identifier = @matricula AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    var whereExternal = "WHERE x.Identifier = @matricula AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    if (onlyTurnstiles)
    {
        whereEmployee += " AND v.DESCRIPTION LIKE '%Catraca%'";
        whereExternal += " AND v.DESCRIPTION LIKE '%Catraca%'";
    }
    cmd.Parameters.Add(new SqlParameter("@matricula", SqlDbType.VarChar) { Value = matricula });
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
    cmd.CommandText = $@"
SELECT q.SbiID,q.Name,q.CardNumber,q.STR_DIRECTION,q.USER_TYPE,q.TERMINAL,q.DESCRIPTION,q.TRANSIT_DATE
FROM (
    SELECT
        e.SbiID,
        e.Name,
        c.CardNumber,
        t.STR_DIRECTION,
        t.USER_TYPE,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    LEFT JOIN Card c ON c.SbiID = e.SbiID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    {whereEmployee}
    UNION ALL
    SELECT
        x.SbiID,
        x.Name,
        c.CardNumber,
        t.STR_DIRECTION,
        t.USER_TYPE,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    LEFT JOIN Card c ON c.SbiID = x.SbiID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    {whereExternal}
) q
ORDER BY q.TRANSIT_DATE DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1) FROM (
    SELECT t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    {whereEmployee}
    UNION ALL
    SELECT t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    {whereExternal}
) z";
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CardNumber = r.IsDBNull(2) ? null : r.GetString(2),
            Direction = r.IsDBNull(3) ? null : r.GetString(3),
            UserType = r.IsDBNull(4) ? null : r.GetString(4),
            Terminal = r.IsDBNull(5) ? null : r.GetString(5),
            TerminalDescription = r.IsDBNull(6) ? null : r.GetString(6),
            TransitDate = r.GetDateTime(7)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/cms/transit/by-card-period", async (string card, DateTime start, DateTime end, bool onlyTurnstiles, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var whereEmployee = "WHERE c.CardNumber = @card AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    var whereExternal = "WHERE c.CardNumber = @card AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    if (onlyTurnstiles)
    {
        whereEmployee += " AND v.DESCRIPTION LIKE '%Catraca%'";
        whereExternal += " AND v.DESCRIPTION LIKE '%Catraca%'";
    }
    cmd.Parameters.Add(new SqlParameter("@card", SqlDbType.VarChar) { Value = card });
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    cmd.CommandText = $@"
SELECT q.SbiID,q.Name,q.CardNumber,q.STR_DIRECTION,q.USER_TYPE,q.TERMINAL,q.DESCRIPTION,q.TRANSIT_DATE
FROM (
    SELECT
        e.SbiID,
        e.Name,
        c.CardNumber,
        t.STR_DIRECTION,
        t.USER_TYPE,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    INNER JOIN Card c ON c.SbiID = e.SbiID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    {whereEmployee}
    UNION ALL
    SELECT
        x.SbiID,
        x.Name,
        c.CardNumber,
        t.STR_DIRECTION,
        t.USER_TYPE,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    INNER JOIN Card c ON c.SbiID = x.SbiID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    {whereExternal}
) q
ORDER BY q.TRANSIT_DATE DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1) FROM (
    SELECT t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    INNER JOIN Card c ON c.SbiID = e.SbiID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    {whereEmployee}
    UNION ALL
    SELECT t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    INNER JOIN Card c ON c.SbiID = x.SbiID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    {whereExternal}
) z";
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CardNumber = r.IsDBNull(2) ? null : r.GetString(2),
            Direction = r.IsDBNull(3) ? null : r.GetString(3),
            UserType = r.IsDBNull(4) ? null : r.GetString(4),
            Terminal = r.IsDBNull(5) ? null : r.GetString(5),
            TerminalDescription = r.IsDBNull(6) ? null : r.GetString(6),
            TransitDate = r.GetDateTime(7)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/reports/access/by-level-period", async (DateTime start, DateTime end) =>
{
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT b.BEHAVIOR_ID, b.DESCRIPTION, COUNT(*) AS Total
FROM HA_TRANSIT t
INNER JOIN SbiSiteBehavior sb ON sb.SbiID = t.SBI_ID
INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
GROUP BY b.BEHAVIOR_ID, b.DESCRIPTION
ORDER BY b.BEHAVIOR_ID";
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new { LevelId = r.GetInt32(0), Level = r.GetString(1), Total = r.GetInt32(2) });
    }
    return Results.Ok(items);
}).RequireAuthorization();

app.MapGet("/api/cms/transit/by-level", async (int? levelId, string? levelName, DateTime start, DateTime end, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var whereLevel = "";
    if (levelId.HasValue) { whereLevel = "AND b.BEHAVIOR_ID = @levelId"; cmd.Parameters.Add(new SqlParameter("@levelId", SqlDbType.Int) { Value = levelId.Value }); }
    else if (!string.IsNullOrWhiteSpace(levelName)) { whereLevel = "AND b.DESCRIPTION = @levelName"; cmd.Parameters.Add(new SqlParameter("@levelName", SqlDbType.VarChar) { Value = levelName }); }
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    cmd.CommandText = $@"
SELECT q.CardNumber,q.Name,q.TERMINAL,q.DESCRIPTION,q.TRANSIT_DATE,q.LevelId,q.Level
FROM (
    SELECT
        c.CardNumber,
        e.Name,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE,
        b.BEHAVIOR_ID AS LevelId,
        b.DESCRIPTION AS Level
    FROM HA_TRANSIT t
    INNER JOIN SbiSiteBehavior sb ON sb.SbiID = t.SBI_ID
    INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = e.SbiID ORDER BY CardNumber) c
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end {whereLevel}
    UNION ALL
    SELECT
        c.CardNumber,
        x.Name,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE,
        b.BEHAVIOR_ID AS LevelId,
        b.DESCRIPTION AS Level
    FROM HA_TRANSIT t
    INNER JOIN SbiSiteBehavior sb ON sb.SbiID = t.SBI_ID
    INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = x.SbiID ORDER BY CardNumber) c
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end {whereLevel}
) q
ORDER BY q.CardNumber ASC, q.TRANSIT_DATE DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1) FROM (
    SELECT t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN SbiSiteBehavior sb ON sb.SbiID = t.SBI_ID
    INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end {whereLevel}
    UNION ALL
    SELECT t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN SbiSiteBehavior sb ON sb.SbiID = t.SBI_ID
    INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end {whereLevel}
) z";
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            CardNumber = r.IsDBNull(0) ? null : r.GetString(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            Terminal = r.IsDBNull(2) ? null : r.GetString(2),
            TerminalDescription = r.IsDBNull(3) ? null : r.GetString(3),
            TransitDate = r.GetDateTime(4),
            LevelId = r.GetInt32(5),
            Level = r.IsDBNull(6) ? null : r.GetString(6)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/cms/company/by-name-info", async (string empresa) =>
{
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT DISTINCT
    e.SbiID,
    e.Name + ' ' + e.Surname AS Name,
    e.PreferredName AS CPF,
    e.Identifier AS Matricula,
    uf.UF2 AS Empresa,
    'Employee' AS Tipo,
    c.CardNumber
FROM Employee e
INNER JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = e.SbiID ORDER BY CardNumber) c
WHERE uf.UF2 = @empresa
UNION
SELECT DISTINCT
    x.SbiID,
    x.Name + ' ' + x.Surname AS Name,
    x.PreferredName AS CPF,
    x.Identifier AS Matricula,
    ux.UF2 AS Empresa,
    'External' AS Tipo,
    c.CardNumber
FROM ExternalRegular x
INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = x.SbiID ORDER BY CardNumber) c
WHERE ux.UF2 = @empresa";
    cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        list.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CPF = r.IsDBNull(2) ? null : r.GetString(2),
            Matricula = r.IsDBNull(3) ? null : r.GetString(3),
            Empresa = r.IsDBNull(4) ? null : r.GetValue(4).ToString(),
            Tipo = r.IsDBNull(5) ? null : r.GetString(5),
            CardNumber = r.IsDBNull(6) ? null : r.GetString(6)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/cms/transit/by-empresa", async (string empresa, DateTime start, DateTime end, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa });
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    cmd.CommandText = @"
SELECT q.CardNumber,q.Name,q.Empresa,q.TERMINAL,q.DESCRIPTION,q.TRANSIT_DATE
FROM (
    SELECT
        c.CardNumber,
        e.Name,
        uf.UF2 AS Empresa,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    INNER JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = e.SbiID ORDER BY CardNumber) c
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE uf.UF2 = @empresa AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
    UNION ALL
    SELECT
        c.CardNumber,
        x.Name,
        ux.UF2 AS Empresa,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
    OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = x.SbiID ORDER BY CardNumber) c
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE ux.UF2 = @empresa AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
) q
ORDER BY q.CardNumber ASC, q.TRANSIT_DATE DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1) FROM (
    SELECT t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    INNER JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    WHERE uf.UF2 = @empresa AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
    UNION ALL
    SELECT t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
    WHERE ux.UF2 = @empresa AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
) z";
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            CardNumber = r.IsDBNull(0) ? null : r.GetString(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            Empresa = r.IsDBNull(2) ? null : r.GetString(2),
            Terminal = r.IsDBNull(3) ? null : r.GetString(3),
            TerminalDescription = r.IsDBNull(4) ? null : r.GetString(4),
            TransitDate = r.GetDateTime(5)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/cms/visitors/by-document", async (string documento, DateTime start, DateTime end, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT
    hv.Name + ' ' + hv.Surname AS Nome,
    hv.VISIT_DOCUMENT AS Documento,
    hv.CONTACT_NAME + ' ' + hv.CONTACT_SURNAME AS Contato,
    hv.SOCIETY AS Visitou,
    v.Telephone AS Telefone,
    v.EMail AS Email,
    hv.VISIT_START AS Entrada,
    hv.VISIT_END AS Saida
FROM HA_VISIT hv
INNER JOIN Visitor v ON hv.SBI_ID = v.SbiID
WHERE hv.VISIT_DOCUMENT = @documento AND hv.VISIT_START >= @start AND hv.VISIT_START < @end
ORDER BY hv.VISIT_START
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1)
FROM HA_VISIT hv
INNER JOIN Visitor v ON hv.SBI_ID = v.SbiID
WHERE hv.VISIT_DOCUMENT = @documento AND hv.VISIT_START >= @start AND hv.VISIT_START < @end";
    cmd.Parameters.Add(new SqlParameter("@documento", SqlDbType.VarChar) { Value = documento });
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            Nome = r.IsDBNull(0) ? null : r.GetString(0),
            Documento = r.IsDBNull(1) ? null : r.GetString(1),
            Contato = r.IsDBNull(2) ? null : r.GetString(2),
            Visitou = r.IsDBNull(3) ? null : r.GetString(3),
            Telefone = r.IsDBNull(4) ? null : r.GetString(4),
            Email = r.IsDBNull(5) ? null : r.GetString(5),
            Entrada = r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6),
            Saida = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/cms/visitors/by-company", async (string empresa, DateTime start, DateTime end, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
SELECT
    hv.Name + ' ' + hv.Surname AS Nome,
    hv.VISIT_DOCUMENT AS Documento,
    hv.CONTACT_NAME + ' ' + hv.CONTACT_SURNAME AS Contato,
    hv.SOCIETY AS Visitou,
    v.Telephone AS Telefone,
    v.EMail AS Email,
    hv.VISIT_START AS Entrada,
    hv.VISIT_END AS Saida
FROM HA_VISIT hv
INNER JOIN Visitor v ON hv.SBI_ID = v.SbiID
WHERE hv.SOCIETY = @empresa AND hv.VISIT_START >= @start AND hv.VISIT_START < @end
ORDER BY hv.VISIT_START
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1)
FROM HA_VISIT hv
INNER JOIN Visitor v ON hv.SBI_ID = v.SbiID
WHERE hv.SOCIETY = @empresa AND hv.VISIT_START >= @start AND hv.VISIT_START < @end";
    cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa });
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            Nome = r.IsDBNull(0) ? null : r.GetString(0),
            Documento = r.IsDBNull(1) ? null : r.GetString(1),
            Contato = r.IsDBNull(2) ? null : r.GetString(2),
            Visitou = r.IsDBNull(3) ? null : r.GetString(3),
            Telefone = r.IsDBNull(4) ? null : r.GetString(4),
            Email = r.IsDBNull(5) ? null : r.GetString(5),
            Entrada = r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6),
            Saida = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/cms/employees/by-matricula", async (string matricula, int page, int pageSize, string? sort, string? dir) =>
{
    var sortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "e.Name",
        ["SbiID"] = "e.SbiID",
        ["Matricula"] = "e.Identifier",
        ["CardNumber"] = "c.CardNumber"
    };
    var orderCol = sort != null && sortMap.ContainsKey(sort) ? sortMap[sort] : "c.CardNumber";
    var orderDir = ToOrderDir(dir);
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = $@"
SELECT e.SbiID,e.Name,e.Surname,e.PreferredName,e.Identifier,c.CardNumber
FROM Employee e
INNER JOIN Card c ON c.SbiID = e.SbiID
WHERE e.Identifier = @matricula
ORDER BY {orderCol} {orderDir}
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1) FROM Employee WHERE Identifier = @matricula";
    cmd.Parameters.Add(new SqlParameter("@matricula", SqlDbType.VarChar) { Value = matricula });
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            Surname = r.IsDBNull(2) ? null : r.GetString(2),
            PreferredName = r.IsDBNull(3) ? null : r.GetString(3),
            Identifier = r.IsDBNull(4) ? null : r.GetString(4),
            CardNumber = r.IsDBNull(5) ? null : r.GetString(5)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/cms/external/search", async (string? matricula, string? empresa, int page, int pageSize, string? sort, string? dir) =>
{
    var sortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "x.Name",
        ["SbiID"] = "x.SbiID",
        ["CardNumber"] = "c.CardNumber",
        ["Matricula"] = "x.Identifier",
        ["Empresa"] = "ux.UF2"
    };
    var orderCol = sort != null && sortMap.ContainsKey(sort) ? sortMap[sort] : "c.CardNumber";
    var orderDir = ToOrderDir(dir);
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = new List<string>();
    if (!string.IsNullOrWhiteSpace(matricula)) { where.Add("x.Identifier = @matricula"); cmd.Parameters.Add(new SqlParameter("@matricula", SqlDbType.VarChar) { Value = matricula }); }
    if (!string.IsNullOrWhiteSpace(empresa)) { where.Add("(ec.Name = @empresa OR ux.UF2 = @empresa)"); cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa }); }
    var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
    cmd.CommandText = $@"
SELECT x.SbiID,x.Name,x.Surname,x.PreferredName,x.Identifier,COALESCE(ec.Name, ux.UF2) as Empresa,c.CardNumber
FROM ExternalRegular x
INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
LEFT JOIN Card c ON c.SbiID = x.SbiID
LEFT JOIN ExternalCompany ec ON ec.ExternalCompanyID = x.ExternalCompanyID
{whereSql}
ORDER BY {orderCol} {orderDir}
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1)
FROM ExternalRegular x
INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
LEFT JOIN ExternalCompany ec ON ec.ExternalCompanyID = x.ExternalCompanyID
{whereSql}";
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            Surname = r.IsDBNull(2) ? null : r.GetString(2),
            PreferredName = r.IsDBNull(3) ? null : r.GetString(3),
            Identifier = r.IsDBNull(4) ? null : r.GetString(4),
            Empresa = r.IsDBNull(5) ? null : r.GetString(5),
            CardNumber = r.IsDBNull(6) ? null : r.GetString(6)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/cms/access/by-level", async (int? levelId, string? levelName, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = "";
    if (levelId.HasValue) { where = "WHERE b.BEHAVIOR_ID = @levelId"; cmd.Parameters.Add(new SqlParameter("@levelId", SqlDbType.Int) { Value = levelId.Value }); }
    else if (!string.IsNullOrWhiteSpace(levelName)) { where = "WHERE b.DESCRIPTION = @levelName"; cmd.Parameters.Add(new SqlParameter("@levelName", SqlDbType.VarChar) { Value = levelName }); }
    cmd.CommandText = $@"
SELECT c.CardNumber,e.Name,b.BEHAVIOR_ID,b.DESCRIPTION
FROM SbiSiteBehavior sb
INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
LEFT JOIN Employee e ON e.SbiID = sb.SbiID
OUTER APPLY (SELECT TOP 1 CardNumber FROM Card c WHERE c.SbiID = sb.SbiID ORDER BY CardNumber) c
ORDER BY c.CardNumber
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1) FROM SbiSiteBehavior sb
INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
{where}";
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            CardNumber = r.IsDBNull(0) ? null : r.GetString(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            LevelId = r.GetInt32(2),
            Level = r.IsDBNull(3) ? null : r.GetString(3)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/cms/transit/by-period", async (DateTime start, DateTime end, string? card, string? terminal, string? userType, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = "WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
    if (!string.IsNullOrWhiteSpace(card))
    {
        where += " AND c.CardNumber = @card";
        cmd.Parameters.Add(new SqlParameter("@card", SqlDbType.VarChar) { Value = card });
    }
    if (!string.IsNullOrWhiteSpace(terminal))
    {
        where += " AND t.TERMINAL = @terminal";
        cmd.Parameters.Add(new SqlParameter("@terminal", SqlDbType.VarChar) { Value = terminal });
    }
    if (!string.IsNullOrWhiteSpace(userType))
    {
        where += " AND t.USER_TYPE = @userType";
        cmd.Parameters.Add(new SqlParameter("@userType", SqlDbType.VarChar) { Value = userType });
    }
    cmd.CommandText = $@"
SELECT e.SbiID,e.Name,c.CardNumber,t.STR_DIRECTION,t.USER_TYPE,t.TERMINAL,v.DESCRIPTION,t.TRANSIT_DATE
FROM HA_TRANSIT t
INNER JOIN Employee e ON e.SbiID = t.SBI_ID
INNER JOIN Card c ON c.SbiID = e.SbiID
INNER JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
{where}
ORDER BY c.CardNumber ASC, t.TRANSIT_DATE DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1)
FROM HA_TRANSIT t
INNER JOIN Employee e ON e.SbiID = t.SBI_ID
INNER JOIN Card c ON c.SbiID = e.SbiID
{where}";
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CardNumber = r.IsDBNull(2) ? null : r.GetString(2),
            Direction = r.IsDBNull(3) ? null : r.GetString(3),
            UserType = r.IsDBNull(4) ? null : r.GetString(4),
            Terminal = r.IsDBNull(5) ? null : r.GetString(5),
            TerminalDescription = r.IsDBNull(6) ? null : r.GetString(6),
            TransitDate = r.GetDateTime(7)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.Run();
