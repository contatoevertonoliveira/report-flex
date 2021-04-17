using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FrmDefinicao : Form
    {
        public FrmDefinicao()
        {
            InitializeComponent();
            RegisterFocusEvents(Controls);
        }

        private SqlConnection con = null;
        private SqlCommand cmd = null;
        private Image ImageAUsar = null;
        public string ImagemOriginal = null;
        public int ImagemOriginalNew = 0;
        public string ExtensaoArquivo = "";
        public int NomeSemExtensao = 0;
        public string NomeGerado = "";
        public string UltimaImagem = null;
        public string ImagemDestino = null;
        public string ImageLocaliza = null;

        public int NomeGeradoAtualizado = 0;

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

        private void controlFocus_Leave(object sender, EventArgs e)
        {
            (sender as Control).BackColor = Color.White;
        }

        private void controlFocus_Enter(object sender, EventArgs e)
        {
            (sender as Control).BackColor = Color.Yellow;
        }
        private void frmDefinicoes_Load(object sender, EventArgs e)
        {
            filterRecords("");
            filterRecords2("");
        }

        private void LimpaControles()
        {
            txtCliente.Clear();
            txtEndCliente.Clear();
            txtFoneCliente.Clear();
            txtEmailCliente.Clear();
            txtSiteCliente.Clear();
            imagemPadrao();
        }

        private void LimpaControles2()
        {
            txtPrestador.Clear();
            txtPrestadorEnd.Clear();
            txtPrestadorFone.Clear();
            txtPrestadorEmail.Clear();
            txtPrestadorSite.Clear();
            imagemPadrao2();
        }

        private void btnRemover_Click(object sender, EventArgs e)
        {
            imagemPadrao();
        }

        private void btnRemover1_Click(object sender, EventArgs e)
        {
            imagemPadrao2();
        }

        public void imagemPadrao()
        {
            picFoto.Image = null;
        }

        public void imagemPadrao2()
        {

        }

        private void frmDefinicoes_Closing(object sender, CancelEventArgs e)
        {
            DialogResult result;
            result = MessageBox.Show("Deseja realmente sair?", "Report Flex 1.0 | Definição de Cabeçalhos e Rodapés - Saindo...", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if ((result == DialogResult.No))
            {
                e.Cancel = true;
            }
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            if (btnSair.Text == "Cancelar")
            {
                if (ImagemDestino == null)
                {
                    ImagemDestino = null;
                    ImagemOriginal = null;
                }
                else
                {
                    System.IO.File.Delete(ImagemDestino);
                    ImagemDestino = null;
                    ImagemOriginal = null;
                }
                ativarbuttonPrestadores();
                ativarbuttonClientes();
                btnSair.Text = "Sair";
                gpbClientes.Enabled = false;
                gpbPrestadores.Enabled = false;
                btnEditarCli.Enabled = false;
                btnEditarPrest.Enabled = false;
                btnExcluirCli.Enabled = false;
                btnExcluirPrest.Enabled = false;
                btnLogoCli.Enabled = false;
                btnLogoPrest.Enabled = false;
                btnLogoCli.Enabled = false;
                btnSalvarCli.Text = "Novo";
                btnSalvarCli.Enabled = true;
                btnSalvarprest.Text = "Novo";
                btnSalvarprest.Enabled = true;
                dgvClientes.Enabled = true;
                LimpaControles();
                LimpaControles2();
            }
            else
            {
                con.Close();
                Close();
            }
        }

        private void btnSalvarCli_Click(object sender, EventArgs e)
        {
            if (btnSalvarCli.Text == "Novo")
            {
                desativarbuttonPrestadores();
                LimpaControles();
                txtCliente.Focus();
                btnSalvarCli.Text = "Salvar Cliente";
                btnEditarCli.Enabled = false;
                btnSalvarprest.Enabled = false;
                btnSair.Text = "Cancelar";
                btnExcluirCli.Enabled = false;
                gpbClientes.Enabled = true;
                btnLogoPrest.Enabled = false;
                btnLogoCli.Enabled = true;
                dgvClientes.Enabled = false;
                txtCliente.Focus();
            }
            else if (btnSalvarCli.Text == "Salvar Cliente")
            {
                btnSair.Enabled = false;
                if (txtCliente.Text == "")
                {
                    MessageBox.Show("Você deve digitar o nome da EMPRESA!", "Report Flex | Nome do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtCliente.Focus();
                }
                else if (txtEndCliente.Text == "")
                {
                    MessageBox.Show("Você deve digitar o ENDEREÇO da empresa!", "Report Flex | ENDEREÇO do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtEndCliente.Focus();
                }
                else if (txtFoneCliente.Text == "")
                {
                    MessageBox.Show("Você deve digitar o TELEFONE da empresa!", "Report Flex | TELEFONE do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtFoneCliente.Focus();
                }
                else if (txtEmailCliente.Text == "")
                {
                    MessageBox.Show("Você deve digitar o EMAIL da empresa!", "Report Flex | EMAIL do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtEmailCliente.Focus();
                }
                else if (txtSiteCliente.Text == "")
                {
                    MessageBox.Show("Você deve digitar o SITE da empresa!", "Report Flex | SITE do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtSiteCliente.Focus();
                }
                else if (ImageAUsar == null)
                {
                    MessageBox.Show("Você deve acidionar um logotipo antes de avançar!", "Report Flex | Logotipo !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    picFoto.Focus();
                }
                else
                {
                    gpbClientes.Enabled = false;
                    desativarbuttonClientes();
                    tTempo.Start();
                    btnSalvarCli.Text = "Cadastrando...";
                }
            }
        }

        public void executeQuery()
        {
            string querySalvar = "INSERT INTO CLIENTES (NOME, ENDERECO, FONE, EMAIL, SITE, ATIVO, CAMINHOIMG) values (@nome,@endereco,@fone,@email,@site,'0',@caminhoimg)";

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand(querySalvar, con);

            cmd.Parameters.AddWithValue("@nome", txtCliente.Text);
            cmd.Parameters.AddWithValue("@endereco", txtEndCliente.Text);
            cmd.Parameters.AddWithValue("@fone", txtFoneCliente.Text);
            cmd.Parameters.AddWithValue("@email", txtEmailCliente.Text);
            cmd.Parameters.AddWithValue("@site", txtSiteCliente.Text);
            cmd.Parameters.AddWithValue("@caminhoimg", ImagemDestino);

            cmd.ExecuteNonQuery();

        }

        public void executeQuery2()
        {
            string querySalvar = "INSERT INTO PRESTADORES (NOME, ENDERECO, FONE, EMAIL, SITE, ATIVO, CAMINHOIMG) values (@nome,@endereco,@fone,@email,@site,'0',@caminhoimg)";
            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand(querySalvar, con);
            cmd.Parameters.AddWithValue("@nome", txtPrestador.Text);
            cmd.Parameters.AddWithValue("@endereco", txtPrestadorEnd.Text);
            cmd.Parameters.AddWithValue("@fone", txtPrestadorFone.Text);
            cmd.Parameters.AddWithValue("@email", txtPrestadorEmail.Text);
            cmd.Parameters.AddWithValue("@site", txtPrestadorSite.Text);
            cmd.Parameters.AddWithValue("@caminhoimg", ImagemDestino);

            cmd.ExecuteNonQuery();
        }

        public void Clear()
        {
            txtCliente.Clear();
            txtCliente.BackColor = Color.White;
            txtEndCliente.Clear();
            txtEndCliente.BackColor = Color.White;
            txtFoneCliente.Clear();
            txtFoneCliente.BackColor = Color.White;
            txtEmailCliente.Clear();
            txtEmailCliente.BackColor = Color.White;
            txtSiteCliente.Clear();
            txtSiteCliente.BackColor = Color.White;
        }

        public void corCamposDesabilitados()
        {
            txtCliente.BackColor = Color.Silver;
            txtEndCliente.BackColor = Color.Silver;
            txtFoneCliente.BackColor = Color.Silver;
            txtEmailCliente.BackColor = Color.Silver;
            txtSiteCliente.BackColor = Color.Silver;
        }

        public void corCamposHabilitados()
        {
            txtCliente.BackColor = Color.White;
            txtEndCliente.BackColor = Color.White;
            txtFoneCliente.BackColor = Color.White;
            txtEmailCliente.BackColor = Color.White;
            txtSiteCliente.BackColor = Color.White;
        }

        public void desabilitaControles()
        {
            txtCliente.Enabled = false;
            txtEndCliente.Enabled = false;
            txtFoneCliente.Enabled = false;
            txtEmailCliente.Enabled = false;
            txtSiteCliente.Enabled = false;

            corCamposDesabilitados();
        }

        public void habilitaControles()
        {
            txtCliente.Enabled = true;
            txtEndCliente.Enabled = true;
            txtFoneCliente.Enabled = true;
            txtEmailCliente.Enabled = true;
            txtSiteCliente.Enabled = true;

            corCamposHabilitados();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog OFD = new OpenFileDialog() { Filter = "Image File(*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png" })
            {
                if (OFD.ShowDialog() == DialogResult.OK)
                {
                    ImagemOriginal = OFD.FileName;
                    ImageAUsar = Image.FromFile(OFD.FileName);
                    picFoto.Image = ImageAUsar;
                    ImagemDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"Logos", Path.GetFileName(ImagemOriginal));
                }
            }
        }

        public void filterRecords(string search)
        {
            con = getConexaoBD();
            con.Open();
            string query = "SELECT SBID as [Código], Nome, Endereco as [Endereço], Fone, Email as [E-mail], Site, CAMINHOIMG as [Imagem] from dbo.Clientes";
            cmd = new SqlCommand(query, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dgvClientes.DataSource = dt;
        }

        public void filterRecords2(string search)
        {
            con = getConexaoBD();
            con.Open();
            string query = "select SBID as [Código], Nome, Endereco as [Endereço], Fone, Email as [E-mail], Site, CAMINHOIMG as [Imagem] from dbo.Prestadores";
            cmd = new SqlCommand(query, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dgvPrestadores.DataSource = dt;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog OFD = new OpenFileDialog() { Filter = "Image File(*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png" })
            {
                if (OFD.ShowDialog() == DialogResult.OK)
                {
                    ImagemOriginal = OFD.FileName;
                    ImageAUsar = Image.FromFile(OFD.FileName);
                    picFoto2.Image = ImageAUsar;
                    ImagemDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"Logos", Path.GetFileName(ImagemOriginal));
                }
            }
        }

        private void btnSalvarPrest_Click(object sender, EventArgs e)
        {
            if (btnSalvarprest.Text == "Novo")
            {
                dgvPrestadores.Enabled = false;
                desativarbuttonClientes();
                LimpaControles();
                LimpaControles2();
                btnSalvarprest.Enabled = true;
                txtPrestador.Focus();
                btnSalvarprest.Text = "Salvar Prestador";
                btnEditarPrest.Enabled = false;
                btnSalvarCli.Enabled = false;
                btnSair.Text = "Cancelar";
                btnExcluirPrest.Enabled = false;
                gpbPrestadores.Enabled = true;
                btnLogoCli.Enabled = false;

                btnLogoPrest.Enabled = true;
                txtPrestador.Focus();
            }
            else if (btnSalvarprest.Text == "Salvar Prestador")
            {
                btnSair.Enabled = false;
                if (txtPrestador.Text == "")
                {
                    MessageBox.Show("Você deve digitar o nome da EMPRESA!", "Report Flex | Nome do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtPrestador.Focus();
                }
                else if (txtPrestadorEnd.Text == "")
                {
                    MessageBox.Show("Você deve digitar o ENDEREÇO da empresa!", "Report Flex | ENDEREÇO do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtPrestadorEnd.Focus();
                }
                else if (txtPrestadorFone.Text == "")
                {
                    MessageBox.Show("Você deve digitar o TELEFONE da empresa!", "Report Flex | TELEFONE do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtPrestadorFone.Focus();
                }
                else if (txtPrestadorEmail.Text == "")
                {
                    MessageBox.Show("Você deve digitar o EMAIL da empresa!", "Report Flex | EMAIL do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtPrestadorEmail.Focus();
                }
                else if (txtPrestadorSite.Text == "")
                {
                    MessageBox.Show("Você deve digitar o SITE da empresa!", "Report Flex | SITE do Cliente !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtPrestadorSite.Focus();
                }
                else if (ImageAUsar == null)
                {
                    MessageBox.Show("Você deve acidionar um logotipo antes de avançar!", "Report Flex | Logotipo !!!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    picFoto2.Focus();
                }
                else
                {
                    gpbPrestadores.Enabled = false;
                    desativarbuttonPrestadores();
                    tTempo3.Start();
                    btnSalvarprest.Text = "Cadastrando...";
                }
            }
        }

        private void arrumaGRID()
        {

            dgvClientes.Columns.Add("@id", "Código");
            dgvClientes.Columns.Add("@nome", "Nome");
            dgvClientes.Columns.Add("@endereco", "Endereço");
            dgvClientes.Columns.Add("@email", "E-mail");

            dgvClientes.AllowUserToAddRows = false;
            dgvClientes.EditMode = DataGridViewEditMode.EditProgrammatically;
        }

        private void ListarClientes()
        {
            SqlDataReader dr = null;
            try
            {
                con = getConexaoBD();
                con.Open();
                string sql = "SELECT SBID as [Código], NOME, ENDERECO, FONE, EMAIL, SITE, CAMINHOIMG as [Imagem] FROM dbo.Clientes WHERE SBID=" + dgvClientes.SelectedCells[0].Value.ToString();
                SqlCommand cmd = new SqlCommand(sql, con);

                dr = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (dr.HasRows)
                {
                    dr.Read();

                    txtCliente.Text = dr["nome"].ToString();
                    txtEndCliente.Text = dr["endereco"].ToString();
                    txtFoneCliente.Text = dr["fone"].ToString();
                    txtEmailCliente.Text = dr["email"].ToString();
                    txtSiteCliente.Text = dr["site"].ToString();
                    picFoto.ImageLocation = dr["imagem"].ToString();

                    ImageLocaliza = picFoto.ImageLocation;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ListarClientes2()
        {
            SqlDataReader dr = null;
            try
            {
                con = getConexaoBD();
                con.Open();
                string sql = "SELECT SBID as [Código], NOME, ENDERECO, FONE, EMAIL, SITE, CAMINHOIMG as [Imagem] FROM dbo.Prestadores WHERE SBID=" + dgvPrestadores.SelectedCells[0].Value.ToString();
                SqlCommand cmd = new SqlCommand(sql, con);

                dr = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (dr.HasRows)
                {
                    dr.Read();

                    txtPrestador.Text = dr["nome"].ToString();
                    txtPrestadorEnd.Text = dr["endereco"].ToString();
                    txtPrestadorFone.Text = dr["fone"].ToString();
                    txtPrestadorEmail.Text = dr["email"].ToString();
                    txtPrestadorSite.Text = dr["site"].ToString();
                    picFoto2.ImageLocation = dr["Imagem"].ToString();

                    ImageLocaliza = picFoto2.ImageLocation;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void dgvClientes_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (MessageBox.Show("Deseja editar este Cliente ?", "Report Flex 1.0 - Editar...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                dgvClientes.Enabled = false;
                btnSalvarCli.Enabled = false;
                btnExcluirCli.Enabled = false;
                btnEditarCli.Enabled = true;
                btnSair.Text = "Cancelar";
                gpbClientes.Enabled = true;
                txtCliente.Focus();
                btnLogoCli.Enabled = true;
                desativarbuttonPrestadores();
            }
        }



        private void btnExcluirCli_Click(object sender, EventArgs e)
        {
            string TempID;
            string TempNome;

            TempNome = dgvClientes.SelectedCells[2].Value.ToString();
            TempID = dgvClientes.SelectedCells[0].Value.ToString();

            if (MessageBox.Show("Deseja excluir este cliente => '" + TempNome + "'?", "Report Flex 1.0 - Excluindo...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (ImageLocaliza != "")
                {
                    System.IO.File.Delete(ImageLocaliza);
                    con = getConexaoBD();
                    con.Open();
                    SqlCommand cmSQL = new SqlCommand("DELETE FROM dbo.clientes WHERE SBID=" + TempID, con);
                    cmSQL.ExecuteNonQuery();
                    MessageBox.Show("Cliente excluído com sucesso!", "Report Flex 1.0 | Cliente excluído !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpaControles();
                    filterRecords("");
                    btnExcluirCli.Enabled = false;
                    ImageLocaliza = null;
                }
                else
                {
                    con = getConexaoBD();
                    con.Open();
                    SqlCommand cmSQL = new SqlCommand("DELETE FROM dbo.clientes WHERE SBID=" + TempID, con);
                    cmSQL.ExecuteNonQuery();
                    MessageBox.Show("Cliente excluído com sucesso!", "Report Flex 1.0 | Cliente excluído !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpaControles();
                    filterRecords("");
                    btnExcluirCli.Enabled = false;
                    ImageLocaliza = null;
                }
            }
        }

        private void tTempo_Tick(object sender, EventArgs e)
        {


            pgbCadastro.Visible = true;
            pgbCadastro.Increment(1);

            if (pgbCadastro.Value < 100)
            {
                btnSalvarCli.Enabled = false;
                btnSalvarCli.Text = "Cadastrando...";
                gpbClientes.Enabled = false;
                btnLogoCli.Enabled = false;

            }
            else
            {
                tTempo.Stop();
                executeQuery();
                btnSalvarCli.Text = "* Cadastrado!! *";
                gpbClientes.Enabled = false;
                LimpaControles();
                filterRecords("");
                MessageBox.Show("Cliente cadastrado com sucesso!", "Report Flex 1.0 | Cliente cadastrado !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnSalvarCli.Enabled = true;
                btnSalvarCli.Text = "Novo";
                btnSair.Text = "Sair";
                btnSair.Enabled = true;
                pgbCadastro.Value = 0;
                pgbCadastro.Visible = false;
                ativarbuttonPrestadores();
                btnEditarPrest.Enabled = false;
                btnExcluirPrest.Enabled = false;
                btnEditarCli.Enabled = false;
                btnExcluirCli.Enabled = false;
                btnLogoCli.Enabled = false;
                btnLogoPrest.Enabled = false;
                dgvClientes.Enabled = true;
                ImagemDestino = null;
                ImagemOriginal = null;
                ImageAUsar = null;
            }
        }

        private void tTempo2_Tick(object sender, EventArgs e)
        {
            pgbCadastro.Visible = true;
            pgbCadastro.Increment(1);

            if (pgbCadastro.Value < 100)
            {
                btnEditarCli.Enabled = false;
                btnEditarCli.Text = "Editando...";
                btnSalvarCli.Enabled = false;
                gpbClientes.Enabled = false;
                btnLogoCli.Enabled = false;
            }
            else
            {
                string TempID;
                con = getConexaoBD();
                con.Open();
                TempID = dgvClientes.SelectedCells[0].Value.ToString();
                string query = "UPDATE dbo.Clientes SET NOME=@nome, ENDERECO=@endereco, FONE=@fone, EMAIL=@email, SITE=@site, CAMINHOIMG=@caminhoimg WHERE SBID='" + TempID + "'";
                cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nome", txtCliente.Text);
                cmd.Parameters.AddWithValue("@endereco", txtEndCliente.Text);
                cmd.Parameters.AddWithValue("@fone", txtFoneCliente.Text);
                cmd.Parameters.AddWithValue("@email", txtEmailCliente.Text);
                cmd.Parameters.AddWithValue("@site", txtSiteCliente.Text);
                cmd.Parameters.AddWithValue("@caminhoimg", ImageLocaliza);
                tTempo2.Stop();
                cmd.ExecuteNonQuery();
                MessageBox.Show("Edição efetuada com sucesso!", "Report Flex 1.0 | Cliente editado !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnEditarCli.Enabled = false;
                btnEditarCli.Text = "Editar Cliente";
                LimpaControles();
                gpbClientes.Enabled = false;
                btnSalvarCli.Enabled = true;
                dgvClientes.Enabled = true;
                dgvPrestadores.Enabled = true;
                btnSalvarprest.Enabled = true;

                btnLogoCli.Enabled = false;
                btnSair.Text = "Sair";
                btnSair.Enabled = true;
                pgbCadastro.Value = 0;
                pgbCadastro.Visible = false;
                ImagemDestino = null;
                ImagemOriginal = null;
                ImageAUsar = null;
                con.Close();
            }
        }

        private void btnEditarPrest_Click(object sender, EventArgs e)
        {
            btnSair.Enabled = false;
            tTempo4.Start();
        }

        private void btnExcluirPrest_Click(object sender, EventArgs e)
        {
            string TempID;
            string TempNome;

            TempNome = dgvPrestadores.SelectedCells[2].Value.ToString();
            TempID = dgvPrestadores.SelectedCells[0].Value.ToString();

            if (MessageBox.Show("Deseja excluir este prestador => '" + TempNome + "'?", "Report Flex 1.0 - Excluindo...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (ImageLocaliza != "")
                {
                    System.IO.File.Delete(ImageLocaliza);
                    con = getConexaoBD();
                    con.Open();
                    SqlCommand cmSQL = new SqlCommand("DELETE FROM dbo.Prestadores WHERE SBID=" + TempID, con);
                    cmSQL.ExecuteNonQuery();
                    MessageBox.Show("Prestador excluído com sucesso!", "Report Flex 1.0 | Prestador excluído !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpaControles2();
                    filterRecords2("");
                    btnExcluirPrest.Enabled = false;
                }
                else
                {
                    con = getConexaoBD();
                    con.Open();
                    SqlCommand cmSQL = new SqlCommand("DELETE FROM dbo.Prestadores WHERE SBID=" + TempID, con);
                    cmSQL.ExecuteNonQuery();
                    MessageBox.Show("Prestador excluído com sucesso!", "Report Flex 1.0 | Prestador excluído !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpaControles2();
                    filterRecords2("");
                    btnExcluirPrest.Enabled = false;
                }
            }
        }

        private void tTempo3_Tick(object sender, EventArgs e)
        {
            pgbCadastro1.Visible = true;
            pgbCadastro1.Increment(1);

            if (pgbCadastro1.Value < 100)
            {
                btnSalvarprest.Enabled = false;
                btnSalvarprest.Text = "Cadastrando...";
                gpbClientes.Enabled = false;
                btnLogoPrest.Enabled = false;

            }
            else
            {
                tTempo3.Stop();
                executeQuery2();
                btnSalvarprest.Text = "* Cadastrado!! *";
                gpbPrestadores.Enabled = false;
                LimpaControles2();
                filterRecords2("");
                MessageBox.Show("Prestador cadastrado com sucesso!", "Report Flex 1.0 | Prestador cadastrado !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnSalvarprest.Enabled = true;
                btnSalvarprest.Text = "Novo";
                btnSair.Text = "Sair";
                btnSair.Enabled = true;
                pgbCadastro1.Value = 0;
                pgbCadastro1.Visible = false;
                ativarbuttonClientes();
                ativarbuttonPrestadores();
                btnEditarCli.Enabled = false;
                btnExcluirCli.Enabled = false;
                btnEditarPrest.Enabled = false;
                btnExcluirPrest.Enabled = false;
                btnLogoCli.Enabled = false;
                btnLogoPrest.Enabled = false;
                dgvPrestadores.Enabled = true;
                ImagemDestino = null;
                ImagemOriginal = null;
                ImageAUsar = null;
            }
        }

        private void tTempo4_Tick(object sender, EventArgs e)
        {
            pgbCadastro1.Visible = true;
            pgbCadastro1.Increment(1);
            if (pgbCadastro1.Value < 100)
            {
                btnEditarPrest.Enabled = false;
                btnEditarPrest.Text = "Editando...";
                btnSalvarprest.Enabled = false;
                gpbPrestadores.Enabled = false;
                btnLogoPrest.Enabled = false;
            }
            else
            {
                string TempID;
                con = getConexaoBD();
                con.Open();
                TempID = dgvPrestadores.SelectedCells[0].Value.ToString();
                string query = "UPDATE dbo.Prestadores SET NOME=@nome, ENDERECO=@endereco, FONE=@fone, EMAIL=@email, SITE=@site, CAMINHOIMG=@caminhoimg  WHERE SBID='" + TempID + "'";
                cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nome", txtPrestador.Text);
                cmd.Parameters.AddWithValue("@endereco", txtPrestadorEnd.Text);
                cmd.Parameters.AddWithValue("@fone", txtPrestadorFone.Text);
                cmd.Parameters.AddWithValue("@email", txtPrestadorEmail.Text);
                cmd.Parameters.AddWithValue("@site", txtPrestadorSite.Text);
                cmd.Parameters.AddWithValue("@caminhoimg", ImageLocaliza);
                tTempo4.Stop();
                cmd.ExecuteNonQuery();
                MessageBox.Show("Edição efetuada com sucesso!", "Report Flex 1.0 | Prestador editado !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnEditarPrest.Enabled = false;
                btnEditarPrest.Text = "Editar Prestador";
                LimpaControles2();
                gpbPrestadores.Enabled = false;
                btnSalvarprest.Enabled = true;
                dgvPrestadores.Enabled = true;
                btnLogoPrest.Enabled = false;
                btnSair.Text = "Sair";
                btnSair.Enabled = true;
                con.Close();
                pgbCadastro1.Value = 0;
                pgbCadastro1.Visible = false;
                btnSalvarCli.Enabled = true;
                btnSalvarprest.Enabled = true;
                btnSalvarprest.Text = "Novo";
                dgvClientes.Enabled = true;
                imagemPadrao2();
                ImagemDestino = null;
                ImagemOriginal = null;
                ImageAUsar = null;
                con.Close();
            }
        }

        public void desativarbuttonPrestadores()
        {
            btnSalvarprest.Enabled = false;
            btnEditarPrest.Enabled = false;
            btnExcluirPrest.Enabled = false;
            btnLogoPrest.Enabled = false;

            dgvPrestadores.Enabled = false;
            imagemPadrao2();
        }

        public void ativarbuttonPrestadores()
        {
            btnSalvarprest.Enabled = true;
            btnEditarPrest.Enabled = true;
            btnExcluirPrest.Enabled = true;

            dgvPrestadores.Enabled = true;
        }

        public void ativarbuttonClientes()
        {
            btnSalvarCli.Enabled = true;
            btnEditarCli.Enabled = true;
            btnExcluirCli.Enabled = true;

            dgvClientes.Enabled = true;
        }

        public void desativarbuttonClientes()
        {
            btnSalvarCli.Enabled = false;
            btnEditarCli.Enabled = false;
            btnExcluirCli.Enabled = false;
            btnLogoCli.Enabled = false;

            dgvClientes.Enabled = false;
        }

        private void dgvPrestadores_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (MessageBox.Show("Deseja editar este Prestador ?", "Report Flex 1.0 - Editar...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                dgvPrestadores.Enabled = false;
                btnSalvarprest.Enabled = false;
                btnExcluirPrest.Enabled = false;
                btnEditarPrest.Enabled = true;
                btnSair.Text = "Cancelar";
                gpbPrestadores.Enabled = true;
                txtPrestador.Focus();
                btnLogoPrest.Enabled = true;
                desativarbuttonClientes();
            }
        }

        private void btnLogoCli_Click(object sender, EventArgs e)
        {
            if (txtCliente.Text == "")
            {
                MessageBox.Show("Escreva o nome da EMPRESA primeiramente!", "Report Flex | Logotipo...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCliente.Focus();
            }
            else
            {
                using (OpenFileDialog OFD = new OpenFileDialog() { Filter = "Image File(*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png" })
                {
                    if (OFD.ShowDialog() == DialogResult.OK)
                    {
                        ImagemOriginal = OFD.FileName;
                        ImageAUsar = Image.FromFile(OFD.FileName);
                        picFoto.Image = ImageAUsar;

                        string nomeCliCompleto = txtCliente.Text;
                        string CliPrimeiroNome = "";
                        string[] arrayPrestNome = nomeCliCompleto.Split(' ');
                        CliPrimeiroNome = arrayPrestNome[0];
                        string nomeCliente = CliPrimeiroNome + ".jpg";

                        ImagemDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"\Logos\Inativos\", nomeCliente);

                        if (!File.Exists(ImagemDestino))
                        {
                            File.Copy(ImagemOriginal, ImagemDestino);
                        }
                        else
                        {
                            System.IO.File.Delete(ImagemDestino);
                            File.Copy(ImagemOriginal, ImagemDestino);
                        }
                    }
                }
            }
        }

        private void btnEditarCli_Click(object sender, EventArgs e)
        {
            btnSair.Enabled = false;
            tTempo2.Start();
        }

        private void btnRemoveLogCli_Click(object sender, EventArgs e)
        {
            imagemPadrao();
        }

        private void btnRemoveLogPrest_Click(object sender, EventArgs e)
        {
            imagemPadrao2();
        }

        private void btnLogoPrest_Click(object sender, EventArgs e)
        {
            if (txtPrestador.Text == "")
            {
                MessageBox.Show("Escreva o nome da EMPRESA primeiramente!", "Report Flex | Logotipo...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPrestador.Focus();
            }
            else
                using (OpenFileDialog OFD1 = new OpenFileDialog() { Filter = "Image File(*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png" })
            {
                if (OFD1.ShowDialog() == DialogResult.OK)
                {
                    ImagemOriginal = OFD1.FileName;
                    ImageAUsar = Image.FromFile(OFD1.FileName);
                    picFoto2.Image = ImageAUsar;


                    string nomePrestCompleto = txtPrestador.Text;
                    string PrestPrimeiroNome = "";
                    string[] arrayPrestNome = nomePrestCompleto.Split(' ');
                    PrestPrimeiroNome = arrayPrestNome[0];
                    string nomePrestador = PrestPrimeiroNome + ".jpg";

                    ImagemDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"\Logos\Inativos\", nomePrestador);

                    if (!File.Exists(ImagemDestino))
                    {
                        File.Copy(ImagemOriginal, ImagemDestino);
                    }
                    else
                    {
                        System.IO.File.Delete(ImagemDestino);
                        File.Copy(ImagemOriginal, ImagemDestino);
                    }
                }
            }
        }

        private void FrmDefinicao_Load(object sender, EventArgs e)
        {
            filterRecords("");
            filterRecords2("");
        }

        private void dgvClientes_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            ImageLocaliza = null;
            LimpaControles();
            if (dgvClientes.Rows.Count > 0)
            {
                ListarClientes();
                btnExcluirCli.Enabled = true;
                btnExcluirPrest.Enabled = false;
                btnEditarPrest.Enabled = false;
                btnLogoCli.Enabled = false;
            }
            else
            {
                btnExcluirCli.Enabled = false;
            }
        }

        private void FrmDefinicao_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
                e.SuppressKeyPress = true;
            }
        }

        private void dgvPrestadores_CellContentDoubleClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (MessageBox.Show("Deseja editar este Prestador ?", "Report Flex 1.0 - Editar...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                dgvPrestadores.Enabled = false;
                btnSalvarprest.Enabled = false;
                btnExcluirPrest.Enabled = false;
                btnEditarPrest.Enabled = true;
                btnSair.Text = "Cancelar";
                gpbPrestadores.Enabled = true;
                txtPrestador.Focus();
                btnLogoPrest.Enabled = true;
                desativarbuttonClientes();
            }
        }

        private void dgvPrestadores_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            ImageLocaliza = null;
            LimpaControles2();
            if (dgvPrestadores.Rows.Count > 0)
            {
                ListarClientes2();
                btnExcluirPrest.Enabled = true;
                btnExcluirCli.Enabled = false;
                btnEditarCli.Enabled = false;
            }
            else
            {
                btnExcluirPrest.Enabled = false;
            }
        }

        private void PegarUltimoArquivo()
        {
            var diretorio = new System.IO.DirectoryInfo(Application.StartupPath + @"\Logos\");
            var arquivos = diretorio.GetFiles();
            var arquivosOrdenados = arquivos.OrderBy(x => x.CreationTime);
            var primeiroArquivo = arquivosOrdenados.First();
            var ultimoArquivo = arquivosOrdenados.Last();

            UltimaImagem = ultimoArquivo.Name;

            var fileNameWithOutExtension = Path.GetFileNameWithoutExtension(UltimaImagem);
            var fileExtension = Path.GetExtension(UltimaImagem);

            var i = 0;
            NomeGerado = fileNameWithOutExtension + i++ + fileExtension;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            PegarUltimoArquivo();
        }
    }
}
