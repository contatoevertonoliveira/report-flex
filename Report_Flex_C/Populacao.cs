using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Configuration;
using ReportFlex.WinForms;

namespace WindowsFormsApp1
{
    public class Populacao
    {
        private SqlConnection con = null;
        private SqlCommand cmd = null;

        private SqlConnection getConexaoBD()
        {
            string strConexao = DbEnv.GetCmsConnectionString();
            return new SqlConnection(strConexao);
        }

        public void AbrirConexao()
        {
            try
            {
                con = getConexaoBD();

                con.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void FechaConexao(string Dados)
        {
            try
            {
                con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
