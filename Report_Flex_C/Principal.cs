using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class frmPrincipal : Form
    {
        public string Logado = "";
        public string NivelAcesso = "";

        public frmPrincipal()
        {
            InitializeComponent();
        }

        private void frmPrincipal_Load(object sender, EventArgs e)
        {

            picLogo.Left = (ClientSize.Width - picLogo.Width) / 2;
            picLogo.Top = (ClientSize.Height - picLogo.Height) / 2;
        }



        private void btnSair_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Deseja realmente sair?", "Report Flex 1.0 | Saindo...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        public void FecharControles()
        {
            btnCadastro.Enabled = false;
            btnCadastro1.Enabled = false;
            btnCabecalho.Enabled = false;
            btnCabecalho1.Enabled = false;
            btnConsultar.Enabled = false;
            btnConsultar1.Enabled = false;
            btnCadastro.Enabled = false;
            btnBanco.Enabled = false;
            btnBanco1.Enabled = false;
            btnDefinicao.Enabled = false;
            btnDefinicao1.Enabled = false;
            btnContent.Enabled = false;
            btnConectar.Enabled = false;
            btnConectar1.Enabled = false;
            btnSobre.Enabled = false;
            btnLogoff.Enabled = false;
            btnLogoff1.Enabled = false;

            Image Fundo = Properties.Resources.Logo_Principal_Fundo;
            picLogo.Image = Fundo;

        }

        public void AbreControles()
        {
            btnCadastro.Enabled = true;
            btnCadastro1.Enabled = true;
            btnCabecalho.Enabled = true;
            btnCabecalho1.Enabled = true;
            btnConsultar.Enabled = true;
            btnConsultar1.Enabled = true;
            btnCadastro.Enabled = true;
            btnBanco.Enabled = true;
            btnBanco1.Enabled = true;
            btnDefinicao.Enabled = true;
            btnDefinicao1.Enabled = true;
            btnContent.Enabled = true;
            btnConectar.Enabled = true;
            btnConectar1.Enabled = true;
            btnSobre.Enabled = true;
            btnLogoff.Enabled = true;
            btnLogoff1.Enabled = true;

            Image Fundo2 = Properties.Resources.Logo_Principal_Fundo2;
            picLogo.Image = Fundo2;

        }

        private void btnConectar1_Click(object sender, EventArgs e)
        {
            if (lblStatus.Text == "")
            {
                frmLogin Login = new frmLogin();
                Login.MdiParent = this;
                Login.Show();
                desabilitarButtonConectar();
            }
        }

        public void desabilitarButtonConectar()
        {
            btnConectar.Enabled = false;
            btnConectar1.Enabled = false;
        }
        public void habilitaButtonConectar()
        {
            btnConectar.Enabled = true;
            btnConectar1.Enabled = true;
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            if (lblStatus.Text == "")
            {
                frmLogin Login = new frmLogin();
                Login.MdiParent = this;
                Login.Show();
                desabilitarButtonConectar();
            }
            else
            {
                MessageBox.Show("Você já está conectado!", "Report Flex 1.0 | Usuário Logado!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        public string TextoStatus
        {
            get { return lblStatus.Text; }
            set { lblStatus.Text = value; }
        }
        public string TextoConexao
        {
            get { return lblConexao.Text; }
            set { lblConexao.Text = value; }
        }
        public string TextoNivel
        {
            get { return lblNivel.Text; }
            set { lblNivel.Text = value; }
        }

        private void frmPrincipal_Activated(object sender, EventArgs e)
        {
            if (Logado == "")
            {
                lblStatus.Text = "";
                lblConexao.Text = "Desconectado";
                lblNivel.Text = "";
                btnConectar.Enabled = true;
                btnConectar1.Enabled = true;
            }
        }

        public void Contador()
        {
            if (NivelAcesso == "Administrador")
            {
                txtContador.Text = "1";
            }
            else if (NivelAcesso == "Padrão")
            {
                txtContador.Text = "2";
            }
            else if (NivelAcesso == "Básico")
            {
                txtContador.Text = "3";
            }
        }

        private void txtContador_TextChanged(object sender, EventArgs e)
        {
            if (txtContador.Text.Trim().Length == 0)
            {
                FecharControles();
            }
            else
            {
                AbreControles();
                if (txtContador.Text == "1")
                {
                    lblStatus.Text = Logado;
                    lblNivel.Text = "Administrador";
                    lblConexao.Font = new Font("Tahoma", 10F, FontStyle.Bold);
                    lblConexao.ForeColor = Color.ForestGreen;
                    lblConexao.Text = "Conectado";
                    btnConectar.Enabled = false;
                    btnConectar1.Enabled = false;
                    btnPopulacao.Enabled = true;
                    btnBanco.Enabled = false;
                    btnLogoff.Enabled = true;
                    btnLogoff1.Enabled = true;
                }
                else if (txtContador.Text == "2")
                {
                    lblStatus.Text = Logado;
                    lblNivel.Text = "Padrão";
                    lblConexao.Font = new Font("Tahoma", 10F, FontStyle.Bold);
                    lblConexao.ForeColor = Color.ForestGreen;
                    lblConexao.Text = "Conectado";
                    FecharControles();
                    btnConsultar.Enabled = true;
                    btnConsultar1.Enabled = true;
                    btnContent.Enabled = true;
                    btnSobre.Enabled = true;
                    btnPopulacao.Enabled = true;
                    btnDefinicao.Enabled = true;
                    btnDefinicao1.Enabled = true;
                    btnCabecalho.Enabled = true;
                    btnBanco.Enabled = false;
                    btnLogoff.Enabled = true;
                    btnLogoff1.Enabled = true;
                }
                else if (txtContador.Text == "3")
                {
                    lblStatus.Text = Logado;
                    lblNivel.Text = "Básico";
                    lblConexao.Font = new Font("Tahoma", 10F, FontStyle.Bold);
                    lblConexao.ForeColor = Color.ForestGreen;
                    lblConexao.Text = "Conectado";
                    FecharControles();
                    btnConsultar.Enabled = true;
                    btnConsultar1.Enabled = true;
                    btnContent.Enabled = true;
                    btnBanco.Enabled = false;
                    btnPopulacao.Enabled = true;
                    btnSobre.Enabled = true;
                    btnLogoff.Enabled = true;
                    btnLogoff1.Enabled = true;
                }
            }
        }

        private void frmPrincipal_Leave(object sender, EventArgs e)
        {
            if (txtContador.Text == "")
            {
                if (lblConexao.Text == "Conectado")
                {
                    AbreControles();
                }
            }

        }

        private void btnLogoff_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Deseja fazer o logoff?", "Report Flex 1.0 | Logoff...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                FechaForms();
                FecharControles();
                lblStatus.Text = "";
                lblConexao.Text = "Desconectado";
                lblNivel.Text = "";
                txtContador.Text = "";
                btnConectar.Enabled = true;
                btnConectar1.Enabled = true;
                btnSobre.Enabled = false;
                lblConexao.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                lblConexao.ForeColor = Color.DarkRed;
                Logado = "";
                NivelAcesso = "";
            }
            else
            {
                return;
            }
        }

        private void btnLogoff1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Deseja fazer o logoff?", "Report Flex 1.0 | Logoff...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                FechaForms();
                FecharControles();
                lblStatus.Text = "";
                lblConexao.Text = "Desconectado";
                lblNivel.Text = "";
                txtContador.Text = "";
                btnConectar.Enabled = true;
                btnConectar1.Enabled = true;
                btnSobre.Enabled = false;
                lblConexao.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                lblConexao.ForeColor = Color.DarkRed;
                Logado = "";
                NivelAcesso = "";
            }
            else
            {
                return;
            }
        }

        private void btnCadastro_Click(object sender, EventArgs e)
        {
            FrmUsuarios Usuarios = new FrmUsuarios();
            Usuarios.MdiParent = this.MdiParent;
            Usuarios.Show();
        }

        private void btnCadastro1_Click(object sender, EventArgs e)
        {
            FrmUsuarios Usuarios = new FrmUsuarios();
            Usuarios.MdiParent = this.MdiParent;
            Usuarios.Show();
        }

        private void btnCabecalho_Click(object sender, EventArgs e)
        {
            FrmDefinicao Definicao = new FrmDefinicao();
            Definicao.MdiParent = this.MdiParent;
            Definicao.Show();
        }

        private void btnCabecalho1_Click(object sender, EventArgs e)
        {
            FrmDefinicao Definicao = new FrmDefinicao();
            Definicao.MdiParent = this.MdiParent;
            Definicao.Show();
        }

        public void FechaForms()
        {
            for (int i = Application.OpenForms.Count - 1; i >= 0; i--)
            {
                if (Application.OpenForms[i].IsMdiChild)
                {
                    Application.OpenForms[i].Close();
                }
            }
        }

        private void btnConsultar_Click(object sender, EventArgs e)
        {
            FrmConsultas Consultas = new FrmConsultas();
            Consultas.Show();
        }

        private void btnConsultar1_Click(object sender, EventArgs e)
        {
            FrmConsultas Consultas = new FrmConsultas();
            Consultas.Show();
        }

        private void btnDefinicao_Click(object sender, EventArgs e)
        {
            FrmAtivarClientesPrestadores Ativa = new FrmAtivarClientesPrestadores();

            Ativa.Show();
        }

        private void btnDefinicao1_Click(object sender, EventArgs e)
        {
            FrmAtivarClientesPrestadores Ativa = new FrmAtivarClientesPrestadores();

            Ativa.Show();
        }

        private void btnPopulacao_Click(object sender, EventArgs e)
        {
            FrmPopulacional1 Populacao = new FrmPopulacional1();
                Populacao.Show();
        }
    }
}
