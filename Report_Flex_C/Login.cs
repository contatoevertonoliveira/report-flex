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

            cmd = new SqlCommand("SELECT NOME, USUARIO FROM dbo.Login WHERE TOKEN=@token AND STATUS='Habilitado'", con);
            cmd.Parameters.Add("@token", SqlDbType.VarChar).Value = txtToken.Text;

            con.Open();
            dr = null;

            dr = cmd.ExecuteReader();

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    usuarioConectado = dr["USUARIO"].ToString();
                    nomeConectado = dr["NOME"].ToString();
                    
                    // Definir nível de acesso baseado no range do token
                    int tokenVal = 0;
                    if (int.TryParse(txtToken.Text, out tokenVal))
                    {
                        if (tokenVal >= 0 && tokenVal <= 10)
                        {
                            nivelAcesso = "SuperAdmin";
                        }
                        else if (tokenVal >= 11 && tokenVal <= 20)
                        {
                            nivelAcesso = "Administrador";
                        }
                        else if (tokenVal >= 21 && tokenVal <= 30)
                        {
                            nivelAcesso = "Padrão"; // User called "Básico", mapping to Padrão (Level 2)
                        }
                        else if (tokenVal >= 31 && tokenVal <= 40)
                        {
                            nivelAcesso = "Leitor"; // User called "Leitor", mapping to new Level 4 or existing Básico (Level 3) logic
                        }
                        else
                        {
                            // Fallback para token fora do range (se houver legado ou erro)
                             nivelAcesso = "Leitor"; 
                        }
                    }
                    else
                    {
                         nivelAcesso = "Leitor";
                    }

                    MessageBox.Show("Usuário conectado com sucesso! | Bem vindo '" + nomeConectado + "'\nNível: " + nivelAcesso, "Report Flex 1.0 | Login efetuado!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    PreencherVariaveis();
                    this.Hide();
                }
            }
            else
            {
                MessageBox.Show("Token não encontrado ou inválido!", "Report Flex 1.0 | ALERTA - Login!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtToken.Clear();
                txtToken.Focus();
            }
            con.Close();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            try
            {
                // Tenta carregar a imagem do logo do disco
                string imagePath = System.IO.Path.Combine(Application.StartupPath, "images", "Logo_Principal.png");
                if (System.IO.File.Exists(imagePath))
                {
                    PictureBox1.Image = Image.FromFile(imagePath);
                }
                
                // Tenta carregar a imagem do cadeado
                 string cadeadoPath = System.IO.Path.Combine(Application.StartupPath, "images", "Cadeado.png");
                if (System.IO.File.Exists(cadeadoPath))
                {
                    pictureBox2.Image = Image.FromFile(cadeadoPath);
                }
            }
            catch (Exception ex)
            {
                // Ignora erro de carregamento de imagem para não travar o sistema
                Console.WriteLine("Erro ao carregar imagens: " + ex.Message);
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtToken.Text.Trim().Length == 0)
            {
                MessageBox.Show("A caixa de texto do token está vazia. Por favor digite o token!", "Report Flex 1.0 | Token!!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtToken.Focus();
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