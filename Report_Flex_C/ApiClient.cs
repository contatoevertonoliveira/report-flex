using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace ReportFlex.WinForms
{
    public static class ApiClient
    {
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        static string BaseUrl => (ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "http://localhost:5000").TrimEnd('/');

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

        public static T PostJson<T>(string path, object payload)
        {
            var url = BaseUrl + path;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json";
            if (!string.IsNullOrEmpty(Session.Token))
            {
                req.Headers["Authorization"] = "Bearer " + Session.Token;
            }
            if (Session.ClientId.HasValue)
            {
                req.Headers["X-Client-Id"] = Session.ClientId.Value.ToString();
            }
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
        }

        public static T GetJson<T>(string path)
        {
            var url = BaseUrl + path;
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.ContentType = "application/json";
            if (!string.IsNullOrEmpty(Session.Token))
            {
                req.Headers["Authorization"] = "Bearer " + Session.Token;
            }
            if (Session.ClientId.HasValue)
            {
                req.Headers["X-Client-Id"] = Session.ClientId.Value.ToString();
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
            var url = BaseUrl + "/api/reports/transit/export" + qs.ToString();
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            if (!string.IsNullOrEmpty(Session.Token))
            {
                req.Headers["Authorization"] = "Bearer " + Session.Token;
            }
            if (Session.ClientId.HasValue)
            {
                req.Headers["X-Client-Id"] = Session.ClientId.Value.ToString();
            }
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var ms = new MemoryStream())
            {
                resp.GetResponseStream().CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
