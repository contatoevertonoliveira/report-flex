//using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FrmReports : Form
    {
        public FrmReports()
        {
            InitializeComponent();
        }
        //private FrmReports(string path, bool isEmbeddedResource, Dictionary<string, object> dataSources, Dictionary<string, object> reportParameters = null)
        //{
        //    InitializeComponent();

        //    // path + isEmbeddedResource.
        //    if (isEmbeddedResource)
        //        this.rptRelatorios.LocalReport.ReportEmbeddedResource = path;
        //    else
        //        this.rptRelatorios.LocalReport.ReportPath = path;

        //    // dataSources.
        //    foreach (var dataSource in dataSources)
        //    {
        //        var reportDataSource = new Microsoft.Reporting.WinForms.ReportDataSource(dataSource.Key, dataSource.Value);
        //        this.rptRelatorios.LocalReport.DataSources.Add(reportDataSource);
        //    }

        //    // reportParameters.
        //    if (reportParameters != null)
        //    {
        //        var reportParameterCollection = new List<Microsoft.Reporting.WinForms.ReportParameter>();

        //        foreach (var parameter in reportParameters)
        //        {
        //            var reportParameter = new Microsoft.Reporting.WinForms.ReportParameter(parameter.Key, parameter.Value.ToString());
        //            reportParameterCollection.Add(reportParameter);
        //        }

        //        this.rptRelatorios.LocalReport.SetParameters(reportParameterCollection);
        //    }
        //}

        //public static void ShowReport(string path, bool isEmbeddedResource, Dictionary<string, object> dataSources, Dictionary<string, object> reportParameters = null)
        //{
        //    var FrmRelatorio = new FrmReports(path, isEmbeddedResource, dataSources, reportParameters);
        //    FrmRelatorio.Show();
        //}

        private void Reports_Load(object sender, EventArgs e)
        {
            System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
            {
                Landscape = true,
                PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                {
                    RawKind = (int)System.Drawing.Printing.PaperKind.A4
                },
                Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
            };
            //rptRelatorios.SetPageSettings(ps);
            //rptRelatorios.SetDisplayMode(DisplayMode.PrintLayout);
    

            //this.rptRelatorios.RefreshReport();
        }
    }
}
