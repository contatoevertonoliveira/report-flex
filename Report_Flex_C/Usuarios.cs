using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FrmUsuarios : Form
    {
        public FrmUsuarios()
        {
            InitializeComponent();
            RegisterFocusEvents(Controls);
        }

        private SqlConnection con = null;
        private SqlCommand cmd = null;
        private int counter = 100;

        int progress = 0;

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

        private void Usuarios_Load(object sender, EventArgs e)
        {
            desabilitaControles();
            PreencheCombos();
            btnLimpar.Text = "Excluir Usuário";
            filterRecords("");
        }


        public void PreencheCombos()
        {
            cboNivel.Items.Add("Selecione...");
            cboNivel.Items.Add("Administrador");
            cboNivel.Items.Add("Padrão");
            cboNivel.Items.Add("Básico");
            cboNivel.SelectedIndex = 0;

            cboStatus.Items.Add("Selecione...");
            cboStatus.Items.Add("Habilitado");
            cboStatus.Items.Add("Desabilitado");
            cboStatus.SelectedIndex = 0;
        }

        private void btnSalvar_Click(object sender, EventArgs e)
        {


            if (btnSalvar.Text == "Novo")
            {
                progress = 0;
                dgvDados.Enabled = false;
                lblProcesso.Visible = false;
                btnSair.Text = "Cancelar Cadastro";
                btnSalvar.Text = "Salvar";
                btnLimpar.Text = "Limpar";
                habilitaControles();
                btnEditar.Enabled = false;
                txtNome.Focus();
                lblMensagem.Visible = true;
                lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 9, FontStyle.Regular);
                lblMensagem.ForeColor = Color.Black;
                lblMensagem.Text = "* Cadastre usuários para gerar relatórios ou fazer alterações no sistema.";
                pgbProgressoOperacao.Visible = false;
                Clear();
                txtNome.Focus();
            }
            else if (btnSalvar.Text == "Salvar")
            {
                if (VerificarCamposEmBranco(this))
                {
                    MessageBox.Show("Existem campos em branco, verifique!", "Report Flex | Erro!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    btnSair.Enabled = false;
                    pgbProgressoOperacao.Visible = true;
                    tCadastro.Enabled = true;
                    tCadastro.Interval = 50;

                    lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 10, FontStyle.Regular);
                    lblMensagem.ForeColor = Color.DarkRed;
                    lblMensagem.Text = "Cadastrando...";

                    string querySalvar = "insert into Login (NOME, SOBRENOME, CARGO, EMPRESA, NIVEL, STATUS, USUARIO, SENHA, EMAIL) values (@nome,@sobrenome,@cargo,@empresa,@nivel,@status,@usuario,@senha,@email)";
                    executeQuery(querySalvar);
                    desabilitaControles();

                }
            }
        }

        public void desabilitaControles()
        {
            txtNome.Enabled = false;
            txtSobreNome.Enabled = false;
            txtCargo.Enabled = false;
            txtEmpresa.Enabled = false;
            cboNivel.Enabled = false;
            cboStatus.Enabled = false;
            txtUsuario.Enabled = false;
            txtSenha.Enabled = false;
            txtEmail.Enabled = false;
            btnLimpar.Enabled = false;

            corCamposDesabilitados();
        }

        public void habilitaControles()
        {
            txtNome.Enabled = true;
            txtSobreNome.Enabled = true;
            txtCargo.Enabled = true;
            txtEmpresa.Enabled = true;
            cboNivel.Enabled = true;
            cboStatus.Enabled = true;
            txtUsuario.Enabled = true;
            txtSenha.Enabled = true;
            txtEmail.Enabled = true;

            btnEditar.Enabled = true;

            corCamposHabilitados();
        }

        public void VerificarCampos()
        {
            if (txtNome.Text == "")
            {
                txtNome.Focus();
            }
            else if (txtSobreNome.Text == "")
            {
                txtSobreNome.Focus();
            }
            else if (txtCargo.Text == "")
            {
                txtCargo.Focus();
            }
            else if (txtEmpresa.Text == "")
            {
                txtEmpresa.Focus();
            }
            else if (cboNivel.SelectedIndex == 0)
            {
                cboNivel.Focus();
            }
            else if (cboStatus.SelectedIndex == 0)
            {
                cboStatus.Focus();
            }
            else if (txtUsuario.Text == "")
            {
                txtUsuario.Focus();
            }
            else if (txtSenha.Text == "")
            {
                txtSenha.Focus();
            }
            else if (txtEmail.Text == "")
            {
                txtEmail.Focus();
            }
        }

        public void Clear()
        {
            txtNome.Clear();
            txtNome.BackColor = Color.White;
            txtSobreNome.Clear();
            txtSobreNome.BackColor = Color.White;
            txtCargo.Clear();
            txtCargo.BackColor = Color.White;
            txtEmpresa.Clear();
            txtEmpresa.BackColor = Color.White;
            txtUsuario.Clear();
            txtUsuario.BackColor = Color.White;
            txtSenha.Clear();
            txtSenha.BackColor = Color.White;
            txtEmail.Clear();
            txtEmail.BackColor = Color.White;

            cboStatus.SelectedIndex = 0;
            cboStatus.BackColor = Color.White;
            cboNivel.SelectedIndex = 0;
            cboNivel.BackColor = Color.White;
        }

        public void corCamposDesabilitados()
        {
            txtNome.BackColor = Color.Silver;
            txtSobreNome.BackColor = Color.Silver;
            txtCargo.BackColor = Color.Silver;
            txtEmpresa.BackColor = Color.Silver;
            txtUsuario.BackColor = Color.Silver;
            txtSenha.BackColor = Color.Silver;
            txtEmail.BackColor = Color.Silver;
            cboStatus.BackColor = Color.Silver;
            cboNivel.BackColor = Color.Silver;
        }

        public void corCamposHabilitados()
        {
            txtNome.BackColor = Color.White;
            txtSobreNome.BackColor = Color.White;
            txtCargo.BackColor = Color.White;
            txtEmpresa.BackColor = Color.White;
            txtUsuario.BackColor = Color.White;
            txtSenha.BackColor = Color.White;
            txtEmail.BackColor = Color.White;
            cboStatus.BackColor = Color.White;
            cboNivel.BackColor = Color.White;
        }

        private void executaTrabalho(BackgroundWorker processoAtivo)
        {




            for (int contador = 1; contador <= 10; contador++)
            {
                if ((processoAtivo.CancellationPending == true))
                {
                    break;
                }

                System.Threading.Thread.Sleep(2000);

                processoAtivo.ReportProgress(contador * 10);
            }
        }

        public void executeQuery(string Query)
        {
            con = getConexaoBD();
            con.Open();

            SqlCommand cmd = new SqlCommand(Query, con);

            con.Close();
            //cmd.Parameters.AddWithValue("@id", null);
            cmd.Parameters.AddWithValue("@nome", txtNome.Text);
            cmd.Parameters.AddWithValue("@sobrenome", txtSobreNome.Text);
            cmd.Parameters.AddWithValue("@cargo", txtCargo.Text);
            cmd.Parameters.AddWithValue("@empresa", txtEmpresa.Text);
            cmd.Parameters.AddWithValue("@nivel", cboNivel.Text);
            cmd.Parameters.AddWithValue("@status", cboStatus.Text);
            cmd.Parameters.AddWithValue("@usuario", txtUsuario.Text);
            cmd.Parameters.AddWithValue("@senha", txtSenha.Text);
            cmd.Parameters.AddWithValue("@email", txtEmail.Text);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public void filterRecords(string search)
        {
            con = getConexaoBD();
            con.Open();
            string query = "select * from dbo.Login";
            cmd = new SqlCommand(query, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dgvDados.DataSource = dt;
        }

        public void CarregaGrid()
        {
            arrumaGRID();
            try
            {
                con = getConexaoBD();
                con.Open();
                string sql = "select * from Login";
                SqlCommand cmd = new SqlCommand(sql, con)
                {
                    Connection = con,
                    CommandText = sql
                };

                SqlDataAdapter adapter = new SqlDataAdapter
                {
                    SelectCommand = cmd
                };

                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet);

                dgvDados.DataSource = dataSet;
                dgvDados.DataMember = dataSet.Tables[0].TableName;
                con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
            }
        }

        private void arrumaGRID()
        {
            dgvDados.AutoResizeColumns();

            dgvDados.Columns.Add("@id", "Id");
            dgvDados.Columns.Add("@nome", "Nome");
            dgvDados.Columns.Add("@sobrenome", "Sobrenome");
            dgvDados.Columns.Add("@cargo", "Cargo");
            dgvDados.Columns.Add("@empresa", "Empresa");
            dgvDados.Columns.Add("@nivel", "Nivel");
            dgvDados.Columns.Add("@status", "Status");
            dgvDados.Columns.Add("@usuario", "Usuario");
            dgvDados.Columns.Add("@senha", "Senha");
            dgvDados.Columns.Add("@email", "Email");

            dgvDados.AllowUserToAddRows = false;
            dgvDados.EditMode = DataGridViewEditMode.EditProgrammatically;
        }

        private void dgvDados_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (dgvDados.RowCount == 0)
            {
                dgvDados.Enabled = false;
                btnLimpar.Enabled = false;

            }
            else
            {
                btnLimpar.Enabled = true;
                btnLimpar.Text = "Excluir Usuário";
                int index = e.RowIndex;
                DataGridViewRow selectedRow = dgvDados.Rows[index];
                txtNome.Text = selectedRow.Cells[1].Value.ToString();
                txtSobreNome.Text = selectedRow.Cells[2].Value.ToString();
                txtCargo.Text = selectedRow.Cells[3].Value.ToString();
                txtEmpresa.Text = selectedRow.Cells[4].Value.ToString();
                cboNivel.Text = selectedRow.Cells[5].Value.ToString();
                txtUsuario.Text = selectedRow.Cells[6].Value.ToString();
                txtSenha.Text = selectedRow.Cells[7].Value.ToString();
                cboStatus.Text = selectedRow.Cells[8].Value.ToString();
                txtEmail.Text = selectedRow.Cells[9].Value.ToString();


                lblProcesso.Visible = false;
                pgbProgressoOperacao.Visible = false;
                pgbProgressoOperacao.Value = 0;
                lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 9, FontStyle.Regular);
                lblMensagem.ForeColor = Color.Black;
                lblMensagem.Text = "* Cadastre usuários para gerar relatórios ou fazer alterações no sistema.";
            }
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            if (btnSair.Text == "Cancelar Cadastro")
            {
                dgvDados.Enabled = true;
                Clear();
                con.Close();
                desabilitaControles();
                btnSair.Text = "Sair do Cadastro";
                btnSalvar.Text = "Novo";
                btnLimpar.Text = "Excluir Usuário";
                btnSalvar.Enabled = true;
                btnEditar.Enabled = false;
                btnLimpar.Enabled = false;
                lblMensagem.Visible = true;
                lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 9, FontStyle.Regular);
                lblMensagem.ForeColor = Color.Black;
                lblMensagem.Text = "* Cadastre usuários para gerar relatórios ou fazer alterações no sistema.";
                pgbProgressoOperacao.Visible = false;
            }
            else if (btnSair.Text == "Cancelar Edição")
            {
                dgvDados.Enabled = true;
                Clear();
                con.Close();
                desabilitaControles();
                btnSair.Text = "Sair do Cadastro";
                btnLimpar.Text = "Excluir Usuário";
                btnSalvar.Text = "Novo";
                btnSalvar.Enabled = true;
                btnEditar.Enabled = false;
                btnLimpar.Enabled = false;
                lblMensagem.Visible = true;
                lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 9, FontStyle.Regular);
                lblMensagem.ForeColor = Color.Black;
                lblMensagem.Text = "* Cadastre usuários para gerar relatórios ou fazer alterações no sistema.";
                pgbProgressoOperacao.Visible = false;
            }
            else
            {
                Close();
            }
        }

        private void dgvDados_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (MessageBox.Show("Deseja editar este usuário ?", "Report Flex 1.0 - Editar...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                progress = 0;
                habilitaControles();
                pgbProgressoOperacao.Visible = false;
                btnSair.Text = "Cancelar Edição";
                btnSalvar.Enabled = false;
                btnEditar.Enabled = true;
                btnLimpar.Enabled = true;
                btnLimpar.Text = "Limpar";
                lblProcesso.Visible = false;
                lblMensagem.Text = "";
                lblMensagem.Text = "Editando usuário...";
                txtNome.Focus();

                int index = e.RowIndex;
                DataGridViewRow selectedRow = dgvDados.Rows[index];
                txtNome.Text = selectedRow.Cells[1].Value.ToString();
                txtSobreNome.Text = selectedRow.Cells[2].Value.ToString();
                txtCargo.Text = selectedRow.Cells[3].Value.ToString();
                txtEmpresa.Text = selectedRow.Cells[4].Value.ToString();
                cboNivel.Text = selectedRow.Cells[5].Value.ToString();
                txtUsuario.Text = selectedRow.Cells[6].Value.ToString();
                txtSenha.Text = selectedRow.Cells[7].Value.ToString();
                cboStatus.Text = selectedRow.Cells[8].Value.ToString();
                txtEmail.Text = selectedRow.Cells[9].Value.ToString();
                dgvDados.Enabled = false;
            }
        }

        private void btnLimpar_Click(object sender, EventArgs e)
        {
            string TempID;
            string TempNome;
            DialogResult Perguntar;

            if (btnLimpar.Text == "Limpar")
            {
                Clear();
                btnLimpar.Enabled = false;
                txtNome.Focus();
            }
            else if (btnLimpar.Text == "Excluir Usuário")
            {
                TempNome = dgvDados.SelectedCells[1].Value.ToString();
                TempID = dgvDados.SelectedCells[0].Value.ToString();

                Perguntar = MessageBox.Show("Deseja excluir este usuário => '" + TempNome + "'?", "Report Flex 1.0 - Excluindo...", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (Perguntar == DialogResult.Yes)
                {
                    if (btnLimpar.Text == "Excluir Usuário")
                    {
                        con = getConexaoBD();
                        con.Open();
                        SqlCommand cmSQL = new SqlCommand("DELETE FROM dbo.Login WHERE ID=" + TempID, con);
                        cmSQL.ExecuteNonQuery();
                        lblMensagem.Visible = true;
                        lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 10, FontStyle.Regular);
                        lblMensagem.ForeColor = Color.DarkRed;
                        lblMensagem.Text = "** Usuário excluido com sucesso! **";
                        Clear();
                        desabilitaControles();
                        filterRecords("");
                    }
                }
            }

            con.Close();
        }

        private void btnEditar_Click(object sender, EventArgs e)
        {
            dgvDados.Enabled = false;
            string TempID;
            try
            {
                if (VerificarCamposEmBranco(this))
                {
                    MessageBox.Show("Existem campos em branco, verifique!", "Report Flex | Erro!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                btnEditar.Enabled = false;
                btnSair.Enabled = false;
                desabilitaControles();
                pgbProgressoOperacao.Visible = true;

                //tEditar.Start();
                tEditar.Enabled = true;
                tEditar.Interval = 50;
                lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 10, FontStyle.Regular);
                lblMensagem.ForeColor = Color.DarkRed;
                lblMensagem.Text = "Editando...";

                TempID = dgvDados.SelectedCells[0].Value.ToString();

                string query = "UPDATE dbo.Login SET NOME=@nome, SOBRENOME=@sobrenome, CARGO=@cargo, EMPRESA=@empresa, NIVEL=@nivel, STATUS=@status, USUARIO=@usuario, SENHA=@senha, EMAIL=@email WHERE ID='" + TempID + "'";

                con = getConexaoBD();
                con.Open();
                cmd = new SqlCommand(query, con);
                //cmd.Parameters.AddWithValue("@id", null);
                cmd.Parameters.AddWithValue("@NOME", txtNome.Text);
                cmd.Parameters.AddWithValue("@sobrenome", txtSobreNome.Text);
                cmd.Parameters.AddWithValue("@cargo", txtCargo.Text);
                cmd.Parameters.AddWithValue("@empresa", txtEmpresa.Text);
                cmd.Parameters.AddWithValue("@nivel", cboNivel.Text);
                cmd.Parameters.AddWithValue("@status", cboStatus.Text);
                cmd.Parameters.AddWithValue("@usuario", txtUsuario.Text);
                cmd.Parameters.AddWithValue("@senha", txtSenha.Text);
                cmd.Parameters.AddWithValue("@email", txtEmail.Text);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }

        private bool VerificarCamposEmBranco(Control ctrl)
        {
            bool retorno = false;
            foreach (Control c in ctrl.Controls)
            {
                if (c is TextBox)
                {
                    if (((TextBox)c).Text.Length == 0)
                    {
                        retorno = true;
                        ((TextBox)c).BackColor = System.Drawing.Color.Red;
                    }
                    else
                    {
                        ((TextBox)c).BackColor = System.Drawing.Color.LightGreen;
                    }
                }
                else if (c.HasChildren)
                {
                    if (VerificarCamposEmBranco(c))
                    {
                        retorno = true;
                    }
                }
            }
            return retorno;
        }

        private void FrmUsuarios_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
                e.SuppressKeyPress = true;
            }
        }

        private void tCadastro_Tick(object sender, EventArgs e)
        {     
            progress += 1;

            if (progress >= 100)
            {
                lblMensagem.Visible = true;
                lblMensagem.Text = "";
                lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 10, FontStyle.Bold);
                lblMensagem.ForeColor = Color.Green;
                lblMensagem.Text = "* Usuário cadastrado com sucesso! *";
                btnLimpar.Text = "Excluir Usuário";
                btnSair.Text = "Sair do Cadastro";
                btnSalvar.Text = "Novo";
                dgvDados.Enabled = true;
                lblProcesso.Visible = true;
                filterRecords("");
                btnSair.Enabled = true;
                tCadastro.Enabled = false;
                tCadastro.Stop();
            }
            con.Close();
            pgbProgressoOperacao.Value = progress;
        }

        private void tEditar_Tick(object sender, EventArgs e)
        {         
            progress += 1;

            if (progress >= 100)
            {
                lblMensagem.Visible = true;
                lblMensagem.Font = new Font(lblMensagem.Font.FontFamily, 10, FontStyle.Regular);
                lblMensagem.ForeColor = Color.Green;
                lblMensagem.Text = "** Usuário editado com sucesso! **";
                lblProcesso.Visible = true;
                btnSair.Text = "Sair do Cadastro";
                btnLimpar.Text = "Excluir Usuário";
                btnSalvar.Text = "Novo";
                btnSalvar.Enabled = true;
                dgvDados.Enabled = true;
                filterRecords("");
                btnSair.Enabled = true;
                tEditar.Enabled = false;
                tEditar.Stop();
            }
            con.Close();
            pgbProgressoOperacao.Value = progress;
        }

        private void txtNome_TextChanged(object sender, EventArgs e)
        {
            if (txtNome.Text.Trim().Length == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }

        private void txtSobreNome_TextChanged(object sender, EventArgs e)
        {
            if (txtSobreNome.Text.Trim().Length == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }

        private void txtCargo_TextChanged(object sender, EventArgs e)
        {
            if (txtCargo.Text.Trim().Length == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }

        private void txtEmpresa_TextChanged(object sender, EventArgs e)
        {
            if (txtEmpresa.Text.Trim().Length == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }

        private void txtUsuario_TextChanged(object sender, EventArgs e)
        {
            if (txtUsuario.Text.Trim().Length == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }

        private void txtSenha_TextChanged(object sender, EventArgs e)
        {
            if (txtSenha.Text.Trim().Length == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }

        private void txtEmail_TextChanged(object sender, EventArgs e)
        {
            if (txtEmail.Text.Trim().Length == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }

        private void cboNivel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboNivel.SelectedIndex == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }

        private void cboStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStatus.SelectedIndex == 0)
            {
                btnLimpar.Enabled = false;
            }
            else
            {
                btnLimpar.Enabled = true;
            }
        }
    }
}
