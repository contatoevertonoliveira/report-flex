using System;

namespace ReportFlex.WinForms
{
    public static class Session
    {
        public static string Token { get; set; }
        public static string Nome { get; set; }
        public static string Usuario { get; set; }
        public static string Nivel { get; set; }
        public static int? ClientId { get; set; }
        public static string ClientName { get; set; }
    }
}
