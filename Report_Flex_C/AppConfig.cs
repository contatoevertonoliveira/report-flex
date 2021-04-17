using System;
using System.Data;
using System.Windows.Forms;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Reflection;

namespace WindowsFormsApp1
{
    public partial class FrmAppConfig : Form
    {
        public FrmAppConfig()
        {
            InitializeComponent();
        }


        private void FrmAppConfig_Load(object sender, EventArgs e)
        {
           
        }

        private void cboServer_SelectedIndexChanged(object sender, EventArgs e)
        {
    

            if(cboServer.SelectedIndex != -1)
            {
                
    
            }
        }

        private void btnSalvar_Click(object sender, EventArgs e)
        {
            txtBd.Enabled = false;
            cboServer.Enabled = false;
            gpbNovo.Enabled = true;
            txtConexao.Focus();

            btnNovo.Enabled = false;

            btnSair.Text = "Cancelar";

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (btnSair.Text == "Cancelar")
            {
                btnNovo.Enabled = true;
                txtServer.Clear();
                txtBanco.Clear();
                txtLogin.Clear();
                txtSenha.Clear();
                gpbNovo.Enabled = false;
                cboServer.Enabled = true;
                txtBd.Enabled = true;
                btnSair.Text = "Sair";
            }
            else
            {
                this.Close();
            }
        }

        private void btnSalvar_Click_1(object sender, EventArgs e)
        {
    

        }

        public void CarregaCombos()
        {

        }
    }
}
