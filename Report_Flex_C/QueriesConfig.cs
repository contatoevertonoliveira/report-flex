using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace ReportFlex.WinForms
{
    public class QueriesConfig
    {
        public bool CpfInfo { get; set; }
        public bool CpfTodos { get; set; }
        public bool CpfCatracas { get; set; }
        public bool TransitoPeriodo { get; set; }
        public bool CriticoPortas { get; set; }
        public bool EmpresaInfo { get; set; }
        public bool EmpresaTodos { get; set; }
        public bool CrachaInfo { get; set; }
        public bool CrachaTodos { get; set; }
        public bool CrachaCatracas { get; set; }
        public bool MatriculaInfo { get; set; }
        public bool MatriculaTodos { get; set; }
        public bool MatriculaCatracas { get; set; }
        public bool VisitantesDocumento { get; set; }
        public bool VisitantesEmpresa { get; set; }
    }

    public static class QueriesConfigStore
    {
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "consultas.config.json");

        public static QueriesConfig Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var txt = File.ReadAllText(FilePath, Encoding.UTF8);
                    var obj = Json.Deserialize<QueriesConfig>(txt);
                    if (obj != null) return obj;
                }
            }
            catch { }
            return new QueriesConfig(); // default: tudo desativado
        }

        public static void Save(QueriesConfig cfg)
        {
            try
            {
                var txt = Json.Serialize(cfg ?? new QueriesConfig());
                File.WriteAllText(FilePath, txt, Encoding.UTF8);
            }
            catch { }
        }
    }
}
