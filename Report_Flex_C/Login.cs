using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace WindowsFormsApp1
{
    public partial class frmLogin : Form
    {
        public static string nomeConectado = "";
        public static string usuarioConectado = "";
        public static string nivelAcesso = "";

        public frmLogin()
        {
            InitializeComponent();
            RegisterFocusEvents(this.Controls);
        }

        SqlConnection con = null;
        SqlCommand cmd = null;
        SqlDataReader dr = null;

        private SqlConnection getConexaoBD()
        {
            string strConexao = ConfigurationManager.ConnectionStrings["StringConexao"].ConnectionString;
            return new SqlConnection(strConexao);
        }

        private void RegisterFocusEvents(Control.ControlCollection controls)
        {

            foreach (Control control in controls)
            {
                if ((control is TextBox) ||
                  (control is RichTextBox) ||
                  (control is ComboBox) ||
                  (control is MaskedTextBox))
                {
                    control.Enter += new EventHandler(controlFocus_Enter);
                    control.Leave += new EventHandler(controlFocus_Leave);
                }
                RegisterFocusEvents(control.Controls);
            }
        }

        void controlFocus_Leave(object sender, EventArgs e)
        {
            (sender as Control).BackColor = Color.White;
        }
        void controlFocus_Enter(object sender, EventArgs e)
        {
            (sender as Control).BackColor = Color.Yellow;
        }

        public void VerificaLogin()
        {
            con = getConexaoBD();

            cmd = new SqlCommand("SELECT NOME, USUARIO FROM dbo.Login WHERE USUARIO=@user AND SENHA=@senha AND NIVEL=@nivel AND STATUS='Habilitado'", con);
            cmd.Parameters.Add("@user", SqlDbType.VarChar).Value = txtUsuario.Text;
            cmd.Parameters.Add("@senha", SqlDbType.VarChar).Value = txtSenha.Text;
            cmd.Parameters.Add("@nivel", SqlDbType.VarChar).Value = cboNivel.Text;

            con.Open();
            dr = null;

            dr = cmd.ExecuteReader();

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    usuarioConectado = txtUsuario.Text;
                    nivelAcesso = cboNivel.Text;
                    nomeConectado = dr["NOME"].ToString();
                    MessageBox.Show("Usuário conectado com sucesso! | Bem vindo '" + nomeConectado + "'", "Report Flex 1.0 | INFORMA - Login efetuado!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    PreencherVariaveis();
                    this.Hide();
                }
            }
            else
            {
                MessageBox.Show("Usuario e/ou senha não encontrados!", "Report Flex 1.0 | ALERTA - Login!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtUsuario.Clear();
                txtSenha.Clear();
                txtUsuario.Focus();
                cboNivel.SelectedIndex = 0;
            }
            con.Close();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            cboNivel.SelectedIndex = 0;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtUsuario.Text.Trim().Length == 0)
            {
                MessageBox.Show("A caixa de texto do usuário está vazia. Por favor digite o usuário!", "Report Flex 1.0 | Usuário!!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtUsuario.Focus();
            }
            else if (txtSenha.Text.Trim().Length == 0)
            {
                MessageBox.Show("A caixa de texto da senha está vazia. Por favor digite a senha!", "Report Flex 1.0 | Senha!!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtSenha.Focus();
            }
            else if (cboNivel.SelectedIndex == 0)
            {
                MessageBox.Show("Você precisa de um nível. Por favor selecione um!", "Report Flex 1.0 | Nível!!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                cboNivel.Focus();
            }
            else
            {
                VerificaLogin();
            }
        }

        private void frmLogin_Activated(object sender, EventArgs e)
        {
            ((frmPrincipal)this.MdiParent).TextoConexao = "Aguardando Conexão...";
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void PreencherVariaveis()
        {
            ((frmPrincipal)this.MdiParent).Logado = nomeConectado;
            ((frmPrincipal)this.MdiParent).NivelAcesso = nivelAcesso;
        }

        private void frmLogin_Leave(object sender, EventArgs e)
        {
            if (nomeConectado == "")
            {
                ((frmPrincipal)this.MdiParent).TextoStatus = "";
                ((frmPrincipal)this.MdiParent).TextoConexao = "Desconectado";
                ((frmPrincipal)this.MdiParent).TextoNivel = "";
                ((frmPrincipal)this.MdiParent).habilitaButtonConectar();
            }
            else
            {
                ((frmPrincipal)this.MdiParent).Contador();
            }
        }
    }
}