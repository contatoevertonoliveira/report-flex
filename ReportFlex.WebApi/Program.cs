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
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
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
});
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? "dev";
var jwtIssuer = jwtSection.GetValue<string>("Issuer") ?? "app";
var jwtAudience = jwtSection.GetValue<string>("Audience") ?? "users";
var jwtExpires = jwtSection.GetValue<int>("ExpiresMinutes");
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
if (initialEnv.TryGetValue("DB_CMS_CONN", out var envCms) && !string.IsNullOrWhiteSpace(envCms))
{
    realOverrides["CMS"] = envCms;
}
if (initialEnv.TryGetValue("DB_LOGINS_CONN", out var envLogins) && !string.IsNullOrWhiteSpace(envLogins))
{
    realOverrides["Logins"] = envLogins;
}

var app = builder.Build();
app.UseResponseCompression();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
ImagesStatic.MapLegacyImages(app);

string GetConn(string name)
{
    if (string.Equals(dbMode, "Demo", StringComparison.OrdinalIgnoreCase))
    {
        return builder.Configuration.GetConnectionString(name + "Demo")
            ?? builder.Configuration.GetConnectionString(name)
            ?? "";
    }
    if (realOverrides.TryGetValue(name, out var ov) && !string.IsNullOrWhiteSpace(ov))
    {
        return ov;
    }
    return builder.Configuration.GetConnectionString(name)
        ?? builder.Configuration.GetConnectionString(name + "Demo")
        ?? "";
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
END";
    cmdInit.ExecuteNonQuery();
}
catch
{
}

static string ToOrderDir(string? dir) => string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
static int ToPage(int page) => page <= 0 ? 1 : page;
static int ToPageSize(int pageSize) => (pageSize <= 0 || pageSize > 200) ? 20 : pageSize;

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
    var cms = realOverrides.TryGetValue("CMS", out var c) ? c : builder.Configuration.GetConnectionString("CMS") ?? null;
    var logins = realOverrides.TryGetValue("Logins", out var l) ? l : builder.Configuration.GetConnectionString("Logins") ?? null;
    return Results.Ok(new { CMS = cms, Logins = logins, mode = dbMode });
}).RequireAuthorization("NotCliente");

app.MapGet("/api/admin/db-info", async () =>
{
    try
    {
        var cmsConn = GetConn("CMS");
        var loginsConn = GetConn("Logins");
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
            result["CMS"] = new { connection = cmsConn, tables = tablesCms };
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
            result["Logins"] = new { connection = loginsConn, tables = tablesLogins };
        }
        catch
        {
            result["Logins"] = null;
        }

        return Results.Ok(new { mode = dbMode, databases = result });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { mode = dbMode, databases = (object?)null, error = ex.Message });
    }
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
    if (changed.Count > 0)
    {
        SaveEnv(changed);
    }
    return Results.Ok(new { CMS = realOverrides.GetValueOrDefault("CMS"), Logins = realOverrides.GetValueOrDefault("Logins") });
}).RequireAuthorization("NotCliente");

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

app.MapGet("/api/clientes", async () =>
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
            list.Add(new
            {
                SBID = r.GetInt32(0),
                NOME = r.IsDBNull(1) ? null : r.GetString(1),
                ENDERECO = r.IsDBNull(2) ? null : r.GetString(2),
                FONE = r.IsDBNull(3) ? null : r.GetString(3),
                EMAIL = r.IsDBNull(4) ? null : r.GetString(4),
                SITE = r.IsDBNull(5) ? null : r.GetString(5),
                ATIVO = r.IsDBNull(6) ? (int?)null : r.GetInt32(6),
                CAMINHOIMG = r.IsDBNull(7) ? null : r.GetString(7),
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
    if (!int.TryParse(clientIdHeader, out var cid) || cid <= 0)
    {
        return Results.Ok(new { id = (int?)null, nome = (string?)null, responsavel = (string?)null, logoPath = (string?)null });
    }
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = "SELECT Id,NOME,RESPONSAVEL,CAMINHOIMG FROM dbo.ClientesPortal WHERE Id=@id";
    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = cid });
    using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync())
    {
        return Results.Ok(new { id = (int?)null, nome = (string?)null, responsavel = (string?)null, logoPath = (string?)null });
    }
    var id = r.GetInt32(0);
    var nome = r.IsDBNull(1) ? null : r.GetString(1);
    var resp = r.IsDBNull(2) ? null : r.GetString(2);
    var logo = r.IsDBNull(3) ? null : r.GetString(3);
    return Results.Ok(new { id, nome, responsavel = resp, logoPath = logo });
}).RequireAuthorization();

app.MapPost("/api/admin/clients", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string, string?>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    if (string.IsNullOrWhiteSpace(dto.GetValueOrDefault("nome") ?? "")) return Results.BadRequest(new { error = "Nome é obrigatório" });
    using var cn = new SqlConnection(GetConn("Logins"));
    try
    {
        await cn.OpenAsync();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = "Falha ao conectar ao banco Logins. Ajuste em Configurações > Banco Real ou instale o LocalDB.", detail = ex.Message });
    }
    // Gera token único se não informado ou já existente
    string token = (dto.GetValueOrDefault("token") ?? "").Trim();
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
    cmd.Parameters.Add(new SqlParameter("@NOME", SqlDbType.VarChar, 100) { Value = dto.GetValueOrDefault("nome") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ENDERECO", SqlDbType.VarChar, 200) { Value = dto.GetValueOrDefault("endereco") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@FONE", SqlDbType.VarChar, 50) { Value = dto.GetValueOrDefault("fone") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@EMAIL", SqlDbType.VarChar, 100) { Value = dto.GetValueOrDefault("email") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@SITE", SqlDbType.VarChar, 100) { Value = dto.GetValueOrDefault("site") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ATIVO", SqlDbType.Int) { Value = int.TryParse(dto.GetValueOrDefault("ativo"), out var a) ? a : 1 });
    cmd.Parameters.Add(new SqlParameter("@CAMINHOIMG", SqlDbType.VarChar, 255) { Value = dto.GetValueOrDefault("logoPath") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@RESPONSAVEL", SqlDbType.VarChar, 100) { Value = dto.GetValueOrDefault("responsavel") ?? (object)DBNull.Value });
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
    var dto = await ctx.Request.ReadFromJsonAsync<Dictionary<string, string?>>();
    if (dto == null) return Results.BadRequest(new { error = "Payload inválido" });
    using var cn = new SqlConnection(GetConn("Logins"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = @"
UPDATE dbo.ClientesPortal SET
NOME=@NOME, ENDERECO=@ENDERECO, FONE=@FONE, EMAIL=@EMAIL, SITE=@SITE, ATIVO=@ATIVO, CAMINHOIMG=@CAMINHOIMG, RESPONSAVEL=@RESPONSAVEL
WHERE Id=@ID";
    cmd.Parameters.Add(new SqlParameter("@ID", SqlDbType.Int) { Value = id });
    cmd.Parameters.Add(new SqlParameter("@NOME", SqlDbType.VarChar, 100) { Value = dto.GetValueOrDefault("nome") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ENDERECO", SqlDbType.VarChar, 200) { Value = dto.GetValueOrDefault("endereco") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@FONE", SqlDbType.VarChar, 50) { Value = dto.GetValueOrDefault("fone") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@EMAIL", SqlDbType.VarChar, 100) { Value = dto.GetValueOrDefault("email") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@SITE", SqlDbType.VarChar, 100) { Value = dto.GetValueOrDefault("site") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@ATIVO", SqlDbType.Int) { Value = int.TryParse(dto.GetValueOrDefault("ativo"), out var a) ? a : 1 });
    cmd.Parameters.Add(new SqlParameter("@CAMINHOIMG", SqlDbType.VarChar, 255) { Value = dto.GetValueOrDefault("logoPath") ?? (object)DBNull.Value });
    cmd.Parameters.Add(new SqlParameter("@RESPONSAVEL", SqlDbType.VarChar, 100) { Value = dto.GetValueOrDefault("responsavel") ?? (object)DBNull.Value });
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
app.MapGet("/api/reports/transit/aggregated", async (DateTime start, DateTime end, string? empresa) =>
{
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = "WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
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

app.MapGet("/api/login/signin-token", async (HttpRequest req) =>
{
    var input = (req.Query.ContainsKey("token") ? req.Query["token"].ToString() : "").Trim();
    if (input.StartsWith("TOKEN", StringComparison.OrdinalIgnoreCase)) input = input.Substring(5).Trim();
    string? usuario = null, nome = null, nivel = null;
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
        catch { }
        if (usuario == null) return Results.Unauthorized();
    }
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>();
    if (!string.IsNullOrEmpty(usuario)) claims.Add(new Claim("usuario", usuario));
    if (!string.IsNullOrEmpty(nome)) claims.Add(new Claim("nome", nome));
    if (!string.IsNullOrEmpty(nivel)) claims.Add(new Claim("nivel", nivel));
    var jwt = new JwtSecurityToken(jwtIssuer, jwtAudience, claims: claims, expires: DateTime.UtcNow.AddMinutes(jwtExpires), signingCredentials: creds);
    var jwtStr = new JwtSecurityTokenHandler().WriteToken(jwt);
    return Results.Ok(new { token = jwtStr, nome, usuario, nivel });
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
    var sortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "e.Name",
        ["SbiID"] = "e.SbiID",
        ["Matricula"] = "e.Identifier",
        ["Empresa"] = "uf.UF2"
    };
    var orderCol = sort != null && sortMap.ContainsKey(sort) ? sortMap[sort] : "e.SbiID";
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
SELECT e.SbiID,e.Name,e.Surname,e.PreferredName,e.Identifier,uf.UF2,uf.UF21
FROM Employee e
INNER JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
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
        items.Add(new
        {
            SbiID = r.GetInt32(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            Surname = r.IsDBNull(2) ? null : r.GetString(2),
            PreferredName = r.IsDBNull(3) ? null : r.GetString(3),
            Identifier = r.IsDBNull(4) ? null : r.GetString(4),
            Empresa = r.IsDBNull(5) ? null : r.GetString(5),
            Tipo = r.IsDBNull(6) ? null : r.GetString(6)
        });
    }
    int total = 0;
    if (await r.NextResultAsync() && await r.ReadAsync()) total = r.GetInt32(0);
    return Results.Ok(new { page, pageSize, total, items });
}).RequireAuthorization();

app.MapGet("/api/reports/transit", async (DateTime start, DateTime end, string? empresa, string? terminal, int page, int pageSize) =>
{
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = "WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
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
SELECT e.SbiID,e.Name,u.UF2 as Empresa,t.TERMINAL,v.DESCRIPTION as TerminalDescription,t.TRANSIT_DATE
FROM HA_TRANSIT t
INNER JOIN Employee e ON e.SbiID = t.SBI_ID
LEFT JOIN EmployeeUserFields u ON u.SbiID = e.SbiID
LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
{where}
ORDER BY t.TRANSIT_DATE DESC
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
            SbiID = r.GetInt32(0),
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

app.MapGet("/api/reports/transit/export", async (HttpContext ctx, DateTime start, DateTime end, string? empresa, string? terminal, string format) =>
{
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = "WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end";
    cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
    cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });
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
SELECT e.SbiID,e.Name,u.UF2 as Empresa,t.TERMINAL,v.DESCRIPTION as TerminalDescription,t.TRANSIT_DATE
FROM HA_TRANSIT t
INNER JOIN Employee e ON e.SbiID = t.SBI_ID
LEFT JOIN EmployeeUserFields u ON u.SbiID = e.SbiID
LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
{where}
ORDER BY t.TRANSIT_DATE DESC";
    using var r = await cmd.ExecuteReaderAsync();
    var rows = new List<(int id, string? name, string? empresa, string? terminal, string? termDesc, DateTime date)>();
    while (await r.ReadAsync())
    {
        rows.Add((r.GetInt32(0), r.IsDBNull(1) ? null : r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4), r.GetDateTime(5)));
    }
    if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
    {
        var sb = new StringBuilder();
        sb.AppendLine("SbiID,Name,Empresa,Terminal,TerminalDescription,TransitDate");
        foreach (var x in rows)
            sb.AppendLine($"{x.id},{Escape(x.name)},{Escape(x.empresa)},{Escape(x.terminal)},{Escape(x.termDesc)},{x.date:yyyy-MM-dd HH:mm:ss}");
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
            AddRow("SbiID","Name","Empresa","Terminal","TerminalDescription","TransitDate");
            foreach (var x in rows) AddRow(x.id.ToString(), x.name ?? "", x.empresa ?? "", x.terminal ?? "", x.termDesc ?? "", x.date.ToString("yyyy-MM-dd HH:mm:ss"));
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
                    table.Cell().Text("SbiID"); table.Cell().Text("Name"); table.Cell().Text("Empresa"); table.Cell().Text("Terminal"); table.Cell().Text("TerminalDescription"); table.Cell().Text("TransitDate");
                    foreach (var x in rows)
                    {
                        table.Cell().Text(x.id.ToString());
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
            SbiID = r.GetInt32(0),
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
            SbiID = r.GetInt32(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CardNumber = r.IsDBNull(2) ? null : r.GetString(2),
            UserType = r.IsDBNull(3) ? null : r.GetString(3)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/cms/person/by-card-info", async (string card) =>
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
    ux.UF2 AS Empresa,
    'External' AS Tipo,
    c.CardNumber,
    x.CommencementDateTime AS Cadastro,
    x.ExpiryDateTime AS Expira
FROM ExternalRegular x
LEFT JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
INNER JOIN Card c ON c.SbiID = x.SbiID
WHERE c.CardNumber = @card";
    cmd.Parameters.Add(new SqlParameter("@card", SqlDbType.VarChar) { Value = card });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        list.Add(new
        {
            SbiID = r.GetInt32(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CPF = r.IsDBNull(2) ? null : r.GetString(2),
            Matricula = r.IsDBNull(3) ? null : r.GetString(3),
            Empresa = r.IsDBNull(4) ? null : r.GetString(4),
            Tipo = r.IsDBNull(5) ? null : r.GetString(5),
            CardNumber = r.IsDBNull(6) ? null : r.GetString(6),
            Cadastro = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7),
            Expira = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/cms/person/by-matricula-info", async (string matricula) =>
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
    ux.UF2 AS Empresa,
    'External' AS Tipo,
    c.CardNumber,
    x.CommencementDateTime AS Cadastro,
    x.ExpiryDateTime AS Expira
FROM ExternalRegular x
LEFT JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
LEFT JOIN Card c ON c.SbiID = x.SbiID
WHERE x.Identifier = @matricula";
    cmd.Parameters.Add(new SqlParameter("@matricula", SqlDbType.VarChar) { Value = matricula });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        list.Add(new
        {
            SbiID = r.GetInt32(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CPF = r.IsDBNull(2) ? null : r.GetString(2),
            Matricula = r.IsDBNull(3) ? null : r.GetString(3),
            Empresa = r.IsDBNull(4) ? null : r.GetString(4),
            Tipo = r.IsDBNull(5) ? null : r.GetString(5),
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
            SbiID = r.GetInt32(0),
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
            SbiID = r.GetInt32(0),
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
SELECT q.SbiID,q.Name,q.TERMINAL,q.DESCRIPTION,q.TRANSIT_DATE,q.LevelId,q.Level
FROM (
    SELECT
        e.SbiID,
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
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end {whereLevel}
    UNION ALL
    SELECT
        x.SbiID,
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
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end {whereLevel}
) q
ORDER BY q.TRANSIT_DATE DESC
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
            SbiID = r.GetInt32(0),
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
    'Employee' AS Tipo
FROM Employee e
INNER JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
WHERE uf.UF2 = @empresa
UNION
SELECT DISTINCT
    x.SbiID,
    x.Name + ' ' + x.Surname AS Name,
    x.PreferredName AS CPF,
    x.Identifier AS Matricula,
    ux.UF2 AS Empresa,
    'External' AS Tipo
FROM ExternalRegular x
INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
WHERE ux.UF2 = @empresa";
    cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa });
    using var r = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await r.ReadAsync())
    {
        list.Add(new
        {
            SbiID = r.GetInt32(0),
            Name = r.IsDBNull(1) ? null : r.GetString(1),
            CPF = r.IsDBNull(2) ? null : r.GetString(2),
            Matricula = r.IsDBNull(3) ? null : r.GetString(3),
            Empresa = r.IsDBNull(4) ? null : r.GetString(4),
            Tipo = r.IsDBNull(5) ? null : r.GetString(5)
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
SELECT q.SbiID,q.Name,q.Empresa,q.TERMINAL,q.DESCRIPTION,q.TRANSIT_DATE
FROM (
    SELECT
        e.SbiID,
        e.Name,
        uf.UF2 AS Empresa,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN Employee e ON e.SbiID = t.SBI_ID
    INNER JOIN EmployeeUserFields uf ON uf.SbiID = e.SbiID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE uf.UF2 = @empresa AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
    UNION ALL
    SELECT
        x.SbiID,
        x.Name,
        ux.UF2 AS Empresa,
        t.TERMINAL,
        v.DESCRIPTION,
        t.TRANSIT_DATE
    FROM HA_TRANSIT t
    INNER JOIN ExternalRegular x ON x.SbiID = t.SBI_ID
    INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
    LEFT JOIN AC_VTERMINAL v ON v.VTERMINAL_KEY = t.TERMINAL
    WHERE ux.UF2 = @empresa AND t.TRANSIT_DATE >= @start AND t.TRANSIT_DATE < @end
) q
ORDER BY q.TRANSIT_DATE DESC
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
            SbiID = r.GetInt32(0),
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
        ["Matricula"] = "e.Identifier"
    };
    var orderCol = sort != null && sortMap.ContainsKey(sort) ? sortMap[sort] : "e.SbiID";
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
            SbiID = r.GetInt32(0),
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
        ["Matricula"] = "x.Identifier",
        ["Empresa"] = "ux.UF2"
    };
    var orderCol = sort != null && sortMap.ContainsKey(sort) ? sortMap[sort] : "x.SbiID";
    var orderDir = ToOrderDir(dir);
    page = ToPage(page); pageSize = ToPageSize(pageSize);
    var offset = (page - 1) * pageSize;
    using var cn = new SqlConnection(GetConn("CMS"));
    await cn.OpenAsync();
    using var cmd = cn.CreateCommand();
    var where = new List<string>();
    if (!string.IsNullOrWhiteSpace(matricula)) { where.Add("x.Identifier = @matricula"); cmd.Parameters.Add(new SqlParameter("@matricula", SqlDbType.VarChar) { Value = matricula }); }
    if (!string.IsNullOrWhiteSpace(empresa)) { where.Add("ux.UF2 = @empresa"); cmd.Parameters.Add(new SqlParameter("@empresa", SqlDbType.VarChar) { Value = empresa }); }
    var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
    cmd.CommandText = $@"
SELECT x.SbiID,x.Name,x.Surname,x.PreferredName,x.Identifier,ux.UF2,c.CardNumber
FROM ExternalRegular x
INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
LEFT JOIN Card c ON c.SbiID = x.SbiID
{whereSql}
ORDER BY {orderCol} {orderDir}
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
SELECT COUNT(1)
FROM ExternalRegular x
INNER JOIN ExternalRegularUserFields ux ON ux.SbiID = x.SbiID
{whereSql}";
    cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = offset });
    cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });
    using var r = await cmd.ExecuteReaderAsync();
    var items = new List<object>();
    while (await r.ReadAsync())
    {
        items.Add(new
        {
            SbiID = r.GetInt32(0),
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
SELECT e.SbiID,e.Name,b.BEHAVIOR_ID,b.DESCRIPTION
FROM SbiSiteBehavior sb
INNER JOIN AC_BEHAVIOR b ON b.BEHAVIOR_ID = sb.Behavior
LEFT JOIN Employee e ON e.SbiID = sb.SbiID
ORDER BY e.SbiID
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
            SbiID = r.IsDBNull(0) ? (int?)null : r.GetInt32(0),
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
ORDER BY t.TRANSIT_DATE DESC
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
            SbiID = r.GetInt32(0),
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
