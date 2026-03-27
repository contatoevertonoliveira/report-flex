using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace ReportFlex.WinForms
{
    public static class ApiClient
    {
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        static string ConfigBaseUrl => (ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        static string BaseUrl = ConfigBaseUrl;
        static string[] LastTriedBaseUrls = new string[0];

        static ApiClient()
        {
            try
            {
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11 |
                    SecurityProtocolType.Tls;

                var insecure = (ConfigurationManager.AppSettings["ApiAllowInsecureSsl"] ?? "false").Trim().ToLowerInvariant();
                if (ConfigBaseUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase) && insecure == "true")
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
                }
            }
            catch { }
        }

        static IEnumerable<string> GetBaseUrlCandidates()
        {
            var b = ConfigBaseUrl.TrimEnd('/');
            yield return b;

            if (b.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                yield return "https://" + b.Substring("http://".Length);
            else if (b.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                yield return "http://" + b.Substring("https://".Length);

            if (b.Contains(":5000")) yield return b.Replace(":5000", ":5001");
            if (b.Contains(":5001")) yield return b.Replace(":5001", ":5000");

            if (b.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0)
                yield return b.Replace("localhost", "127.0.0.1");
            if (b.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0)
                yield return b.Replace("127.0.0.1", "localhost");
        }

        static bool IsConnectivityError(WebException ex)
        {
            return ex.Status == WebExceptionStatus.ConnectFailure ||
                   ex.Status == WebExceptionStatus.NameResolutionFailure ||
                   ex.Status == WebExceptionStatus.Timeout ||
                   ex.Status == WebExceptionStatus.SecureChannelFailure ||
                   ex.Status == WebExceptionStatus.TrustFailure;
        }

        static T TryWithBaseUrl<T>(Func<string, T> call)
        {
            var tried = new List<string>();
            foreach (var cand in GetBaseUrlCandidates())
            {
                if (string.IsNullOrWhiteSpace(cand)) continue;
                if (tried.Contains(cand)) continue;
                tried.Add(cand);
                try
                {
                    var result = call(cand);
                    BaseUrl = cand;
                    LastTriedBaseUrls = tried.ToArray();
                    return result;
                }
                catch (WebException ex)
                {
                    if (!IsConnectivityError(ex))
                    {
                        LastTriedBaseUrls = tried.ToArray();
                        throw;
                    }
                }
            }
            LastTriedBaseUrls = tried.ToArray();
            return call(ConfigBaseUrl);
        }

        public class ReportOptions
        {
            public bool csv { get; set; }
            public bool xlsx { get; set; }
            public bool excel { get; set; }
            public bool pdf { get; set; }
            public bool txt { get; set; }
            public bool word { get; set; }
        }

        public class TransitItem
        {
            public int SbiID { get; set; }
            public string Name { get; set; }
            public string Empresa { get; set; }
            public string Terminal { get; set; }
            public string TerminalDescription { get; set; }
            public DateTime TransitDate { get; set; }
        }

        public class TransitResponse
        {
            public int page { get; set; }
            public int pageSize { get; set; }
            public int total { get; set; }
            public List<TransitItem> items { get; set; }
        }

        public class AccessItem
        {
            public int Codigo { get; set; }
            public string Name { get; set; }
            public string CPF { get; set; }
            public string Matricula { get; set; }
            public string Empresa { get; set; }
            public string Cartao { get; set; }
            public string Direcao { get; set; }
            public string Tipo { get; set; }
            public string Terminal { get; set; }
            public string TerminalDescription { get; set; }
            public DateTime Transito { get; set; }
        }

        public class AccessResponse
        {
            public int page { get; set; }
            public int pageSize { get; set; }
            public int total { get; set; }
            public List<AccessItem> items { get; set; }
        }

        static HttpWebRequest CreateRequest(string url, string method)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            if (!string.IsNullOrEmpty(Session.Token))
            {
                req.Headers["Authorization"] = "Bearer " + Session.Token;
            }
            if (Session.ClientId.HasValue)
            {
                req.Headers["X-Client-Id"] = Session.ClientId.Value.ToString();
            }
            return req;
        }

        public static T PostJson<T>(string path, object payload)
        {
            return TryWithBaseUrl(baseUrl =>
            {
                var url = baseUrl + path;
                var req = CreateRequest(url, "POST");
                req.ContentType = "application/json";
                var body = Json.Serialize(payload);
                using (var sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(body);
                }
                try
                {
                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        var txt = sr.ReadToEnd();
                        return Json.Deserialize<T>(txt);
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse http && (int)http.StatusCode == 401)
                    {
                        throw new UnauthorizedAccessException("Unauthorized");
                    }
                    throw;
                }
            });
        }

        public static T GetJson<T>(string path)
        {
            return TryWithBaseUrl(baseUrl =>
            {
                var url = baseUrl + path;
                var req = CreateRequest(url, "GET");
                req.ContentType = "application/json";
                try
                {
                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        var txt = sr.ReadToEnd();
                        return Json.Deserialize<T>(txt);
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse http && (int)http.StatusCode == 401)
                    {
                        throw new UnauthorizedAccessException("Unauthorized");
                    }
                    throw;
                }
            });
        }

        public static ReportOptions GetReportOptions()
        {
            return GetJson<ReportOptions>("/api/admin/report-options");
        }

        public class ClientInfo
        {
            public int id { get; set; }
            public string nome { get; set; }
            public string responsavel { get; set; }
            public string logoPath { get; set; }
            public string clientToken { get; set; }
            public string endereco { get; set; }
            public string fone { get; set; }
            public string email { get; set; }
            public string site { get; set; }
        }

        public static List<ClientInfo> GetClientes()
        {
            return GetJson<List<ClientInfo>>("/api/clientes");
        }

        public class DefaultClient
        {
            public int? id { get; set; }
            public string nome { get; set; }
        }

        public static DefaultClient GetReportDefaultClient()
        {
            return GetJson<DefaultClient>("/api/admin/report-default-client");
        }

        public static byte[] DownloadBinary(string absoluteOrRelativeUrl)
        {
            return TryWithBaseUrl(baseUrl =>
            {
                string url = absoluteOrRelativeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? absoluteOrRelativeUrl
                    : baseUrl + absoluteOrRelativeUrl;
                var req = CreateRequest(url, "GET");
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var ms = new MemoryStream())
                {
                    resp.GetResponseStream().CopyTo(ms);
                    return ms.ToArray();
                }
            });
        }

        public static TransitResponse GetTransitByPeriod(DateTime start, DateTime end, string empresa, string terminal, int page, int pageSize)
        {
            var qs = new StringBuilder();
            qs.Append("?start=").Append(Uri.EscapeDataString(start.ToString("o")));
            qs.Append("&end=").Append(Uri.EscapeDataString(end.ToString("o")));
            qs.Append("&page=").Append(page);
            qs.Append("&pageSize=").Append(pageSize);
            if (!string.IsNullOrWhiteSpace(empresa))
            {
                qs.Append("&empresa=").Append(Uri.EscapeDataString(empresa));
            }
            if (!string.IsNullOrWhiteSpace(terminal))
            {
                qs.Append("&terminal=").Append(Uri.EscapeDataString(terminal));
            }
            return GetJson<TransitResponse>("/api/reports/transit" + qs.ToString());
        }

        public static AccessResponse GetAccessByDocument(string documento, DateTime start, DateTime end, string mode, int page, int pageSize)
        {
            var qs = new StringBuilder();
            qs.Append("?documento=").Append(Uri.EscapeDataString(documento ?? ""));
            qs.Append("&start=").Append(Uri.EscapeDataString(start.ToString("o")));
            qs.Append("&end=").Append(Uri.EscapeDataString(end.ToString("o")));
            qs.Append("&mode=").Append(Uri.EscapeDataString(mode ?? "all"));
            qs.Append("&page=").Append(page);
            qs.Append("&pageSize=").Append(pageSize);
            return GetJson<AccessResponse>("/api/access/by-document" + qs.ToString());
        }

        public static AccessResponse GetAccessByDocumentAll(string documento, string mode, int page, int pageSize)
        {
            var qs = new StringBuilder();
            qs.Append("?documento=").Append(Uri.EscapeDataString(documento ?? ""));
            qs.Append("&mode=").Append(Uri.EscapeDataString(mode ?? "all"));
            qs.Append("&page=").Append(page);
            qs.Append("&pageSize=").Append(pageSize);
            return GetJson<AccessResponse>("/api/access/by-document/all" + qs.ToString());
        }

        public static byte[] DownloadAccessByDocumentExport(string documento, DateTime start, DateTime end, string mode, string format)
        {
            var qs = new StringBuilder();
            qs.Append("?documento=").Append(Uri.EscapeDataString(documento ?? ""));
            qs.Append("&start=").Append(Uri.EscapeDataString(start.ToString("o")));
            qs.Append("&end=").Append(Uri.EscapeDataString(end.ToString("o")));
            qs.Append("&mode=").Append(Uri.EscapeDataString(mode ?? "all"));
            qs.Append("&format=").Append(Uri.EscapeDataString(string.IsNullOrWhiteSpace(format) ? "csv" : format));
            return DownloadBytes("/api/access/by-document/export" + qs.ToString());
        }

        public static byte[] DownloadAccessByDocumentAllExport(string documento, string mode, string format)
        {
            var qs = new StringBuilder();
            qs.Append("?documento=").Append(Uri.EscapeDataString(documento ?? ""));
            qs.Append("&mode=").Append(Uri.EscapeDataString(mode ?? "all"));
            qs.Append("&format=").Append(Uri.EscapeDataString(string.IsNullOrWhiteSpace(format) ? "csv" : format));
            return DownloadBytes("/api/access/by-document/all/export" + qs.ToString());
        }

        public static byte[] DownloadTransitExport(DateTime start, DateTime end, string empresa, string terminal, string format)
        {
            var qs = new StringBuilder();
            qs.Append("?start=").Append(Uri.EscapeDataString(start.ToString("o")));
            qs.Append("&end=").Append(Uri.EscapeDataString(end.ToString("o")));
            qs.Append("&format=").Append(Uri.EscapeDataString(format ?? "xlsx"));
            if (!string.IsNullOrWhiteSpace(empresa))
            {
                qs.Append("&empresa=").Append(Uri.EscapeDataString(empresa));
            }
            if (!string.IsNullOrWhiteSpace(terminal))
            {
                qs.Append("&terminal=").Append(Uri.EscapeDataString(terminal));
            }
            return DownloadBytes("/api/reports/transit/export" + qs.ToString());
        }

        public static byte[] DownloadDoorCriticalExport(DateTime start, DateTime end, string format)
        {
            var qs = new StringBuilder();
            qs.Append("?start=").Append(Uri.EscapeDataString(start.ToString("o")));
            qs.Append("&end=").Append(Uri.EscapeDataString(end.ToString("o")));
            qs.Append("&format=").Append(Uri.EscapeDataString(string.IsNullOrWhiteSpace(format) ? "pdf" : format));
            return DownloadBytes("/api/reports/door-critical/export" + qs.ToString());
        }

        static byte[] DownloadBytes(string pathWithQuery)
        {
            return TryWithBaseUrl(baseUrl =>
            {
                var url = baseUrl + pathWithQuery;
                var req = CreateRequest(url, "GET");
                try
                {
                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var ms = new MemoryStream())
                    {
                        resp.GetResponseStream().CopyTo(ms);
                        return ms.ToArray();
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse http && (int)http.StatusCode == 401)
                    {
                        throw new UnauthorizedAccessException("Unauthorized");
                    }
                    using (var resp = ex.Response as HttpWebResponse)
                    using (var sr = resp != null ? new StreamReader(resp.GetResponseStream()) : null)
                    {
                        var raw = sr != null ? sr.ReadToEnd() : null;
                        if (!string.IsNullOrWhiteSpace(raw))
                            throw new Exception(raw);
                    }
                    throw;
                }
            });
        }

        public static string GetApiDiagnostics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ApiBaseUrl configurado: " + ConfigBaseUrl);
            sb.AppendLine("ApiBaseUrl em uso: " + BaseUrl);
            if (LastTriedBaseUrls != null && LastTriedBaseUrls.Length > 0)
            {
                sb.AppendLine("URLs testadas:");
                foreach (var u in LastTriedBaseUrls) sb.AppendLine("- " + u);
            }
            return sb.ToString();
        }
    }
}
