using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Principal;

namespace ReportFlex.WinForms
{
    public static class DbEnv
    {
        private static readonly object LockObj = new object();
        private static bool Loaded = false;
        private static string DbMode = "Demo";
        private static string EnvCmsConn;
        private static string EnvLoginsConn;
        private static string EnvEmsConn; // connection for EMSEVENTS

        private static void EnsureLoaded()
        {
            if (Loaded) return;
            lock (LockObj)
            {
                if (Loaded) return;
                try
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
                    var envPath = Path.Combine(root, ".env");
                    if (File.Exists(envPath))
                    {
                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var line in File.ReadAllLines(envPath))
                        {
                            var trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                            var idx = trimmed.IndexOf('=');
                            if (idx <= 0) continue;
                            var key = trimmed.Substring(0, idx).Trim();
                            var val = trimmed.Substring(idx + 1).Trim();
                            dict[key] = val;
                        }
                        string m;
                        if (dict.TryGetValue("DB_MODE", out m) && !string.IsNullOrWhiteSpace(m))
                        {
                            DbMode = string.Equals(m, "Real", StringComparison.OrdinalIgnoreCase) ? "Real" : "Demo";
                        }
                        string cms;
                        if (dict.TryGetValue("DB_CMS_CONN", out cms) && !string.IsNullOrWhiteSpace(cms))
                        {
                            EnvCmsConn = cms;
                        }
                        string logins;
                        if (dict.TryGetValue("DB_LOGINS_CONN", out logins) && !string.IsNullOrWhiteSpace(logins))
                        {
                            EnvLoginsConn = logins;
                        }
                        string ems;
                        if (dict.TryGetValue("DB_EMS_CONN", out ems) && !string.IsNullOrWhiteSpace(ems))
                        {
                            EnvEmsConn = ems;
                        }
                    }
                }
                catch
                {
                }
                Loaded = true;
            }
        }

        public static string GetCmsConnectionString()
        {
            EnsureLoaded();
            if (string.Equals(DbMode, "Real", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(EnvCmsConn))
            {
                return EnvCmsConn;
            }
            var cfg = ConfigurationManager.ConnectionStrings["StringConexao1"];
            return cfg != null ? cfg.ConnectionString : "";
        }

        public static string GetLoginsConnectionString()
        {
            EnsureLoaded();
            if (string.Equals(DbMode, "Real", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(EnvLoginsConn))
            {
                return EnvLoginsConn;
            }
            var cfg = ConfigurationManager.ConnectionStrings["StringConexao"];
            return cfg != null ? cfg.ConnectionString : "";
        }

        public static string GetEmsConnectionString()
        {
            EnsureLoaded();
            if (string.Equals(DbMode, "Real", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(EnvEmsConn))
            {
                return EnvEmsConn;
            }
            var cfg = ConfigurationManager.ConnectionStrings["StringConexaoEms"];
            return cfg != null ? cfg.ConnectionString : "";
        }

        public static string GetDiagnosticsFor(string which)
        {
            try
            {
                var user = System.Security.Principal.WindowsIdentity.GetCurrent()?.Name ?? "";
                string cs;
                if (which == "CMS") cs = GetCmsConnectionString();
                else if (which == "Logins") cs = GetLoginsConnectionString();
                else if (which == "EMS") cs = GetEmsConnectionString();
                else cs = "";
                return "WindowsUser=" + user + "\nConn=" + cs;
            }
            catch
            {
                return "";
            }
        }
    }


    public static class DbEnvDiagnostics
    {
        public static string CurrentUser()
        {
            try { return WindowsIdentity.GetCurrent()?.Name ?? ""; } catch { return ""; }
        }
    }
}
