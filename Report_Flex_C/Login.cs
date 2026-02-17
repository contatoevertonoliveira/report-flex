using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using ReportFlex.WinForms;
using System.Web.Script.Serialization;

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
            try
            {
                var resp = ApiClient.PostJson<Dictionary<string, object>>("/api/login/signin-token", new { token = txtToken.Text });
                if (resp == null || !resp.ContainsKey("token"))
                {
                    throw new Exception("Resposta inválida da API");
                }
                Session.Token = resp["token"] as string;
                Session.Nome = resp.ContainsKey("nome") ? (resp["nome"] as string) : null;
                Session.Usuario = resp.ContainsKey("usuario") ? (resp["usuario"] as string) : null;
                Session.Nivel = resp.ContainsKey("nivel") ? (resp["nivel"] as string) : null;
                if (resp.ContainsKey("clientId") && resp["clientId"] != null)
                {
                    int cid;
                    if (int.TryParse(resp["clientId"].ToString(), out cid))
                    {
                        Session.ClientId = cid;
                    }
                }
                Session.ClientName = resp.ContainsKey("clientName") ? (resp["clientName"] as string) : null;

                usuarioConectado = Session.Usuario ?? "";
                nomeConectado = Session.Nome ?? "";
                nivelAcesso = Session.Nivel ?? "";

                MessageBox.Show("Usuário conectado com sucesso! | Bem vindo '" + nomeConectado + "'\nNível: " + nivelAcesso, "Report Flex 1.0 | Login efetuado!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PreencherVariaveis();
                this.Hide();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Token não encontrado ou inválido!", "Report Flex 1.0 | ALERTA - Login!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtToken.Clear();
                txtToken.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao autenticar via API: " + ex.Message, "Report Flex 1.0 | Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
