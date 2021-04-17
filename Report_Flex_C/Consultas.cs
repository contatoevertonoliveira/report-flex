using Microsoft.Reporting.WinForms;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FrmConsultas : Form
    {
        public int CodigoCli = 0;
        public string Cliente = "";
        public int CodigoPrest = 0;
        public string Prestador = "";

        public FrmConsultas()
        {
            InitializeComponent();
            RegisterFocusEvents(Controls);
        }

        private SqlConnection con = null;
        private SqlCommand cmd = null;
        private SqlConnection con2 = null;
        private SqlCommand cmd2 = null;
        private SqlConnection con3 = null;
        private SqlCommand cmd3 = null;
        public string Imagem = null;
        public Image img;
        public string ImagemCliente = "";
        public string Resultado = "";

        private SqlConnection getConexaoBD()
        {
            string strConexao = ConfigurationManager.ConnectionStrings["StringConexao1"].ConnectionString;
            return new SqlConnection(strConexao);
        }

        private SqlConnection getConexaoBD1()
        {
            string strConexao = ConfigurationManager.ConnectionStrings["StringConexao"].ConnectionString;
            return new SqlConnection(strConexao);
        }

        private SqlConnection getConexaoBD2()
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

        private void cboPesquisa_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboPesquisa.SelectedIndex == 0)
            {
                cboObter.Enabled = false;
                gpbDescobrirCard.Enabled = true;
            }
            else
            {
                cboObter.Enabled = true;
                gpbDescobrirCard.Enabled = false;
                btnBuscar.BackColor = Color.Gray;
                btnSair.Text = "Cancelar Operação";

                //CPF ---------------------------------------------------------------------------
                if (cboPesquisa.Text == "Cpf")
                {
                    lblMsg.Text = "Digite um CPF para efetuar a pesquisa:";
                    lblSQL.Text = "Resultado da consulta SQL: pelo 'Cpf'";
                    cboObter.Items.Clear();
                    cboObter.Items.Add("Selecione...");
                    cboObter.Items.Add("Informação de Cadastro");
                    cboObter.Items.Add("Todos Acessos");
                    cboObter.Items.Add("Somente Catracas");
                    //cboObter.Items.Add("Somente Cancelas");
                    //cboObter.Items.Add("Primeira Entrada e Última Saída de cada dia");

                }

                //MATRÍCULA ---------------------------------------------------------------------------
                else if (cboPesquisa.Text == "Matrícula")
                {
                    lblMsg.Text = "Digite uma MATRICULA para efetuar a pesquisa:";
                    lblSQL.Text = "Resultado da consulta SQL: pela 'Matrícula'";
                    cboObter.Items.Clear();
                    cboObter.Items.Add("Selecione...");
                    cboObter.Items.Add("Informação de Cadastro");
                    cboObter.Items.Add("Todos Acessos");
                    cboObter.Items.Add("Somente Catracas");
                    //cboObter.Items.Add("Somente Cancelas");
                    //cboObter.Items.Add("Primeira Entrada e Última Saída de cada dia");
                }

                //EMPRESA ---------------------------------------------------------------------------
                else if (cboPesquisa.Text == "Empresa")
                {
                    lblMsg.Text = "Digite o nome de uma EMPRESA para efetuar a pesquisa:";
                    lblSQL.Text = "Resultado da consulta SQL: pela 'Empresa'";
                    cboObter.Items.Clear();
                    cboObter.Items.Add("Selecione...");
                    cboObter.Items.Add("Informação de Cadastro");
                    //cboObter.Items.Add("Todos Acessos");
                    //cboObter.Items.Add("Somente Catracas");
                    //cboObter.Items.Add("Somente Cancelas");
                    //cboObter.Items.Add("Primeira Entrada e Última Saída de cada dia");
                }

                //CRACHÁ ---------------------------------------------------------------------------
                else if (cboPesquisa.Text == "Crachá")
                {
                    lblMsg.Text = "Digite um CRACHÁ para efetuar a pesquisa:";
                    lblSQL.Text = "Resultado da consulta SQL: pelo 'Crachá'";
                    cboObter.Items.Clear();
                    cboObter.Items.Add("Selecione...");
                    cboObter.Items.Add("Informação de Cadastro");
                    cboObter.Items.Add("Todos Acessos");
                    cboObter.Items.Add("Somente Catracas");
                    //cboObter.Items.Add("Somente Cancelas");
                    //cboObter.Items.Add("Primeira Entrada e Última Saída de cada dia");
                }

                //VEÍCULOS ---------------------------------------------------------------------------
                //else if (cboPesquisa.Text == "Veículos")
                //{
                //    lblMsg.Text = "Digite a TAG de Estacionamento para efetuar a pesquisa:";
                //    lblSQL.Text = "Resultado da consulta SQL: por 'TAG'";
                //    cboObter.Items.Clear();
                //    cboObter.Items.Add("Selecione...");
                //    cboObter.Items.Add("Informação de Cadastro");
                //    cboObter.Items.Add("Acessos");
                //}

                //NÍVEL DE ACESSO ---------------------------------------------------------------------------
                else if (cboPesquisa.Text == "Nível de Acesso")
                {
                    lblMsg.Text = "Digite algo para efetuar o filtro na pesquisa:";
                    lblSQL.Text = "Resultado da consulta SQL: pelo 'Nível de Acesso'";
                    cboObter.Items.Clear();
                    cboObter.Items.Add("Selecione...");
                    cboObter.Items.Add("Todos os Niveis");
                }

                //VISITANTES ---------------------------------------------------------------------------
                else if (cboPesquisa.Text == "Visitantes")
                {
                    lblMsg.Text = "Digite o documento do VISITANTE a ser pesquisado:";
                    lblSQL.Text = "Resultado da consulta SQL: pelo 'Visitante'";
                    cboObter.Items.Clear();
                    cboObter.Items.Add("Selecione...");
                    cboObter.Items.Add("Acessos por Documento");
                    cboObter.Items.Add("Acessos por Empresa");
                }
            }
        }

        private void cboObter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboPesquisa.SelectedIndex > 0)
            {
                //NIVEL DE ACESSO -------------------------------------------------------
                if (cboPesquisa.Text == "Nível de Acesso")
                {
                    if (cboObter.Text == "Todos os Niveis")
                    {
                        Ativa.Enabled = false;
                        Desativa.Enabled = false;
                        btnConsultar.Enabled = true;
                    }
                }
                //CPF ------------------------------------------------------------------
                else if (cboPesquisa.Text == "Cpf")
                {
                    if (cboObter.Text == "Informação de Cadastro")
                    {
                        Ativa.Enabled = false;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        btnConsultar.Enabled = true;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Todos os acessos")
                    {
                        Ativa.Enabled = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                }
                //EMPRESA ------------------------------------------------------------------
                else if (cboPesquisa.Text == "Empresa")
                {
                    if (cboObter.Text == "Informação de Cadastro")
                    {
                        Ativa.Enabled = false;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        btnConsultar.Enabled = true;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Todos os acessos")
                    {
                        Ativa.Enabled = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                }

                //CRACHÁ --------------------------------------------------------------
                else if (cboPesquisa.Text == "Crachá")
                {
                    if (cboObter.Text == "Informação de Cadastro")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = false;
                        Ativa.Checked = false;
                        Desativa.Enabled = false;
                        Desativa.Checked = true;
                        btnFiltrar.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Todos Acessos")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Checked = false;
                        Ativa.Enabled = false;
                        Desativa.Checked = false;
                        Desativa.Enabled = false;
                        txtDataInicio.Enabled = false;
                        txtDataFim.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Somente Catracas")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = true;
                        Ativa.Checked = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Somente Cancelas")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = true;
                        Ativa.Checked = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Primeira Entrada e Última Saída de cada dia")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = true;
                        Ativa.Checked = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                }
                //MATRICULA --------------------------------------------------------------
                else if (cboPesquisa.Text == "Matrícula")
                {
                    if (cboObter.Text == "Informação de Cadastro")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = false;
                        Ativa.Checked = false;
                        Desativa.Enabled = false;
                        Desativa.Checked = true;
                        btnFiltrar.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Todos Acessos")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Checked = false;
                        Ativa.Enabled = false;
                        Desativa.Checked = false;
                        Desativa.Enabled = false;
                        txtDataInicio.Enabled = false;
                        txtDataFim.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Somente Catracas")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = true;
                        Ativa.Checked = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Somente Cancelas")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = true;
                        Ativa.Checked = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                    else if (cboObter.Text == "Primeira Entrada e Última Saída de cada dia")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = true;
                        Ativa.Checked = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                }

                //VISITANTES --------------------------------------------------------------
                else if (cboPesquisa.Text == "Visitantes")
                {
                    if (cboObter.Text == "Acessos por Documento")
                    {
                        btnConsultar.Enabled = true;
                        Ativa.Enabled = true;
                        Ativa.Checked = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }  
                    else if (cboObter.Text == "Acessos por Empresa")
                    {
                        btnConsultar.Enabled = true;
                        Desativa.Enabled = false;
                        txtPesquisa.Enabled = true;
                        txtPesquisa.BackColor = Color.White;
                        txtPesquisa.Focus();
                    }
                }              
                    cboPesquisa.Enabled = false;
                    cboObter.Enabled = false;            
            }
        }

        private void FrmConsultas_Load(object sender, EventArgs e)
        {
            btnConsultar.BackColor = Color.ForestGreen;
            btnBuscar.BackColor = Color.Gray;
            con = getConexaoBD();
            con.Open();
            carregaCombos();
        }

        private void btnConsultar_Click(object sender, EventArgs e)
        {
            btnConsultar.Enabled = false;
            Resultado = "";
            if (cboPesquisa.Text == "Nível de Acesso")
            {
                if (cboObter.Text == "Todos os Niveis")
                {
                    consulta_TODOS_Nivel_Acesso();

                    if (bsDados.Count > 0)
                    {
                        txtPesquisa.Enabled = true;
                        btnConsultar.Enabled = false;
                    }
                    else
                    {
                        txtPesquisa.Enabled = false;
                        btnConsultar.Enabled = true;
                    }
                }
            }
            else if (cboPesquisa.Text == "Cpf")
            {
                if (cboObter.Text == "Informação de Cadastro")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        bool isNumeric = int.TryParse(txtPesquisa.Text, out int num);
                        if (isNumeric)
                        {
                            BuscaINFOCad_CPF();
                        }
                        else
                        {
                            MessageBox.Show("Por favor digite um número de CPF!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            btnConsultar.Enabled = true;
                            txtPesquisa.Clear();
                            txtPesquisa.Focus();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite algum número de CPF!", "Report Flex | Digitar CPF !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                    }
                }
            }
            //CONSULTA SE A OPÇÃO SELECIONADA FOR -------------------------------------->>> EMPRESA
            else if (cboPesquisa.Text == "Empresa")
            {
                if (cboObter.Text == "Informação de Cadastro")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        Consulta_EMPRESA_InfoCad();
                        Resultado = txtPesquisa.Text;
                        btnFiltrar.Enabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite o nome da EMPRESA!", "Report Flex | Digitar nome da empresa !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                        txtPesquisa.Clear();
                        txtPesquisa.Focus();
                    }
                }
            }

            //CONSULTA SE A OPÇÃO SELECIONADA FOR -------------------------------------->>> CRACHÁ
            else if (cboPesquisa.Text == "Crachá")
            {
                if (cboObter.Text == "Informação de Cadastro")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        Consulta_CRACHA_InfoCad();
                        Resultado = txtPesquisa.Text;
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite um número de Crachá!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                        txtPesquisa.Clear();
                        txtPesquisa.Focus();
                    }
                }
                else if (cboObter.Text == "Todos Acessos")
                {
                    if (txtPesquisa.Text.Trim().Length != 0)
                    {
                        Consulta_CRACHA_TDS_ACESSOS();
                        Resultado = txtPesquisa.Text;
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite algum número de Crachá!", "Report Flex | Digitar Crachá !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                    }
                }
                else if (cboObter.Text == "Somente Catracas")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        if (!txtDataInicio.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataInicio.Focus();
                        }
                        else if (!txtDataFim.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataFim.Focus();
                        }
                        else
                        {
                            consulta_CRACHA_ACESSOS();
                            Resultado = txtPesquisa.Text;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite um número de Crachá!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                        txtPesquisa.Clear();
                        txtPesquisa.Focus();
                    }
                }
                else if (cboObter.Text == "Somente Cancelas")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        if (!txtDataInicio.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataInicio.Focus();
                        }
                        else if (!txtDataFim.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataFim.Focus();
                        }
                        else
                        {
                            consulta_CRACHA_ACESSOS_CANCELAS();
                            Resultado = txtPesquisa.Text;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite um número de Crachá!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                        txtPesquisa.Clear();
                        txtPesquisa.Focus();
                    }
                }
                else if (cboObter.Text == "Primeira Entrada e Última Saída de cada dia")
                {

                }
            }
            //CONSULTA SE A OPÇÃO SELECIONADA FOR -------------------------------------->>> MATRICULA
            else if (cboPesquisa.Text == "Matrícula")
            {
                if (cboObter.Text == "Informação de Cadastro")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        bool isNumeric = int.TryParse(txtPesquisa.Text, out int num);
                        if (isNumeric)
                        {
                            Consulta_MATRICULA_InfoCad();
                            Resultado = txtPesquisa.Text;
                        }
                        else
                        {
                            MessageBox.Show("Por favor digite um número de MATRICULA!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            btnConsultar.Enabled = true;
                            txtPesquisa.Clear();
                            txtPesquisa.Focus();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite algum número de MATRICULA!", "Report Flex | Digitar Crachá !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                    }
                }
                else if (cboObter.Text == "Todos Acessos")
                {
                    if (txtPesquisa.Text.Trim().Length != 0)
                    {
                        Consulta_MATRICULA_TDS_ACESSOS();
                        Resultado = txtPesquisa.Text;
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite algum número de MATRICULA!", "Report Flex | Digitar Crachá !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                    }

                }
                else if (cboObter.Text == "Somente Catracas")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        if (!txtDataInicio.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtDataInicio.Focus();
                            btnConsultar.Enabled = true;
                        }
                        else if (!txtDataFim.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtDataFim.Focus();
                            btnConsultar.Enabled = true;
                        }
                        else
                        {
                            consulta_MATRICULA_ACESSOS_CATRACAS();
                            Resultado = txtPesquisa.Text;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite um número de MATRICULA!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                        txtPesquisa.Clear();
                        txtPesquisa.Focus();
                    }
                }
                else if (cboObter.Text == "Somente Cancelas")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        if (!txtDataInicio.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataInicio.Focus();
                        }
                        else if (!txtDataFim.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataFim.Focus();
                        }
                        else
                        {
                           
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite um número de MATRICULA!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                        txtPesquisa.Clear();
                        txtPesquisa.Focus();
                    }
                }
                else if (cboObter.Text == "Primeira Entrada e Última Saída de cada dia")
                {

                }
            }
            //CONSULTA SE A OPÇÃO SELECIONADA FOR -------------------------------------->>> VISITANTES
            else if (cboPesquisa.Text == "Visitantes")
            {
                if (cboObter.Text == "Acessos por Documento")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        if (!txtDataInicio.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataInicio.Focus();
                        }
                        else if (!txtDataFim.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataFim.Focus();
                        }
                        else
                        {
                            consulta_VISITANTES_ACESSOS();
                            Resultado = txtPesquisa.Text;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite um número de DOCUMENTO!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                        txtPesquisa.Clear();
                        txtPesquisa.Focus();
                    }
                }
                else if (cboObter.Text == "Acessos por Empresa")
                {
                    if (txtPesquisa.Text.Trim().Length > 0)
                    {
                        if (!txtDataInicio.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataInicio.Focus();
                        }
                        else if (!txtDataFim.MaskCompleted)
                        {
                            MessageBox.Show("Você precisa digitar uma data!", "Report Flex | Data...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnConsultar.Enabled = true;
                            txtDataFim.Focus();
                        }
                        else
                        {
                            consulta_VISITANTES_EMPRESA();
                            Resultado = txtPesquisa.Text;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor digite um número de DOCUMENTO!", "Report Flex | Digitar somente Números !!!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnConsultar.Enabled = true;
                        txtPesquisa.Clear();
                        txtPesquisa.Focus();
                    }
                }
            }
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            if (btnSair.Text == "Cancelar Operação")
            {
                btnSair.Text = "Sair";
                LimparControles();
                DesativaControles();
                gpbDescobrirCard.Enabled = true;
                cboPesquisa.Enabled = true;
                rdbAtivo.Enabled = false;
                rdbInativo.Enabled = false;
                rdbExpirado.Enabled = false;
                rdbInvalido.Enabled = false;
                cboPesquisa.SelectedIndex = 0;
                con.Close();
            }
            else
            {
                con.Close();
                Close();
            }
        }

        private void btnPdf_Click(object sender, EventArgs e)
        {
            if (cboPesquisa.Text == "Nível de Acesso")
            {
                if (cboObter.Text == "Todos os Niveis")
                {

                }
            }
            else if (cboPesquisa.Text == "Cpf")
            {
                if (cboObter.Text == "Informação de Cadastro")
                {

                }
            }
            else if (cboPesquisa.Text == "Crachá")
            {
                if (cboObter.Text == "Somente Catracas")
                {

                }
                else if (cboObter.Text == "Informação de Cadastro")
                {

                }
            }
        }

        private void btnConsultar_MouseLeave(object sender, EventArgs e)
        {
            btnConsultar.BackColor = Color.ForestGreen;
            btnConsultar.ForeColor = Color.White;
        }

        private void btnConsultar_MouseEnter(object sender, EventArgs e)
        {
            btnConsultar.BackColor = Color.YellowGreen;
            btnConsultar.ForeColor = Color.Black;
        }

        private void btnConsultar_MouseDown(object sender, MouseEventArgs e)
        {
            btnConsultar.BackColor = Color.Yellow;
            btnConsultar.ForeColor = Color.DarkRed;
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            descobrir_CRACHA();

            if(cboDados.Items.Count == 1)
            {
                lblStatus.Visible = false;
            }
            else
            {
                cboDados.SelectedIndex = 1;
                lblStatus.Visible = true;
                txtPesquisa.Text = cboDados.Text;
            }
        }

        private void btnBuscar_MouseEnter(object sender, EventArgs e)
        {
            btnBuscar.BackColor = Color.YellowGreen;
            btnBuscar.ForeColor = Color.Black;
        }

        private void btnBuscar_MouseLeave(object sender, EventArgs e)
        {
            btnBuscar.BackColor = Color.ForestGreen;
            btnBuscar.ForeColor = Color.White;
        }

        private void btnBuscar_MouseDown(object sender, MouseEventArgs e)
        {
            btnBuscar.BackColor = Color.Yellow;
            btnBuscar.ForeColor = Color.DarkRed;
        }


        private void txtBusca_TextChanged(object sender, EventArgs e)
        {
            if (txtBusca.Text.Trim().Length != 0)
            {
                btnBuscar.BackColor = Color.ForestGreen;
                btnBuscar.Enabled = true;

                if (cboDados.Items.Count != 0)
                {
                    if(cboDados.Text == "Sem Crachá !!!")
                        lblStatus.Visible = false;
                    else
                        lblStatus.Visible = true;
                }
                else
                {
                    lblStatus.Visible = false;
                }
            }
            else
            {
                btnBuscar.BackColor = Color.Gray;
                btnBuscar.Enabled = false;
                cboDados.Items.Clear();
                lblStatus.Visible = false;
            }
        }

        private void txtBusca_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar.ToString() != "%" && e.KeyChar != (char)8)
            {
                e.Handled = true;
            }
        }

        private void Ativa_CheckedChanged(object sender, EventArgs e)
        {
            Desativa.Enabled = true;
            txtDataInicio.Enabled = true;
            txtDataFim.Enabled = true;
        }

        private void Desativa_CheckedChanged(object sender, EventArgs e)
        {
            txtDataInicio.Enabled = false;
            txtDataFim.Enabled = false;
        }


        //-------------------------------------------------------------Funções ------------------------------------------------------------------------------------------------------

        //-------------------------------------------------------------A CONSULTA TRAZ TODOS OS NÍVEIS (COMBOS)
        public void consulta_Nivel_Acesso()
        {
            string strsql;
            SqlCommand cmd = new SqlCommand();

            try
            {
                cboObter.Items.Clear();
                strsql = "SELECT dbo.AC_BEHAVIOR.DESCRIPTION as [ACESSOS] from dbo.AC_BEHAVIOR";
                cmd.Connection = con;
                cmd.CommandText = strsql;
                SqlDataReader dtReader = cmd.ExecuteReader();
                string str = dtReader.ToString();
                cboObter.Items.Add("Selecione...");
                cboObter.Items.Add("Todos Níveis...");
                while ((dtReader.Read()))
                {
                    cboObter.Items.Add(dtReader["ACESSOS"]);
                }
                cboObter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao efetuar a conexão com o Banco de Dados: " + ex.Message, "Buscar Acessos");
            }
            finally
            {
                con.Close();
            }
        }


        //A CONSULTA TRAZ TODOS OS FUNCIONÁRIOS DE TODOS OS NÍVEIS ----------------------------------------------------------------------------
        public void consulta_TODOS_Nivel_Acesso()
        {
            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT Employee.SbiID as [CÓDIGO], 
                      Employee.Name+' '+Employee.Surname as [NOME], 
                      Employee.PreferredName as [CPF],
                      Employee.Identifier as [MATRICULA],
                      EmployeeUserFields.UF2 as [EMPRESA],
                      AC_BEHAVIOR.DESCRIPTION as [NÍVEL],
                      Case Employee.StateID
                        When 3 Then 'Inativo'
	                    When 0 Then 'Ativo'
	                    When 1 Then 'Ativo'
	                    When 4 Then 'Expirado'
	                    When 5 Then 'Invalido'
                      End As [STATUS]
                      from  Employee
                      inner join EmployeeUserFields on EmployeeUserFields.SbiID = Employee.SbiID
                      inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = Employee.SbiID
                      inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior  
UNION
                      SELECT ExternalRegular.SbiID, 
                      ExternalRegular.Name+' '+ExternalRegular.Surname,
                      ExternalRegular.Identifier,
                      ExternalRegular.PreferredName,
                      ExternalRegularUserFields.UF2,
                      AC_BEHAVIOR.DESCRIPTION,
                      Case ExternalRegular.StateID
                        When 3 Then 'Inativo'
                        When 0 Then 'Ativo'
                        When 1 Then 'Ativo'
                        When 4 Then 'Expirado'
                        When 5 Then 'Invalido'
                      End
                from  ExternalRegular
                        inner join ExternalRegularUserFields on ExternalRegularUserFields.SbiID = ExternalRegular.SbiID
                        inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = ExternalRegular.SbiID
                        inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior";

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados níveis. Verifique se realmente existe!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsNiveis", bsDados);
                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;
                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\NRelatorios.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);
                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        //-------------------------------------------------------------A CONSULTA PELO DE CRACHA INFORMAÇÕES CADASTRO ------------------------------
        public void Consulta_CRACHA_InfoCad()
        {
            string Cracha = txtPesquisa.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT DISTINCT (dbo.ExternalRegular.SbiID) as [CÓDIGO],
                            dbo.ExternalRegular.Name+' '+dbo.ExternalRegular.Surname as [NOME],
                            dbo.ExternalRegular.PreferredName as [CPF],
                            dbo.ExternalRegular.Identifier as [MATRICULA],
                            dbo.Card.CardNumber as [CRACHÁ],
                            dbo.ExternalRegularUserFields.uf2 as [EMPRESA],
                            dbo.ExternalRegularUserFields.uf21 as [TIPO],
                            dbo.ExternalRegular.CommencementDateTime as [CADASTRO],
                            dbo.ExternalRegular.ExpiryDateTime as [EXPIRA]
 
                        FROM[dbo].[ExternalRegular]
                            inner join dbo.ExternalRegularUserFields on dbo.ExternalRegularUserFields.SbiID = dbo.ExternalRegular.SbiID
                            inner join dbo.Card on dbo.ExternalRegular.SbiID = dbo.Card.SbiID 
                        WHERE
                            dbo.Card.CardNumber = @Cracha
                        UNION ALL
                        SELECT DISTINCT (dbo.Employee.SbiID),
                            dbo.Employee.Name+' '+dbo.Employee.Surname,
                            dbo.Employee.PreferredName,
                            dbo.Employee.Identifier,
                            dbo.card.CardNumber,
                            dbo.EmployeeUserFields.UF2,
                            dbo.EmployeeUserFields.UF21,
                            dbo.Employee.CommencementDateTime,
                            dbo.employee.ExpiryDateTime
                        FROM[dbo].[Employee]
                            inner join dbo.EmployeeUserFields on dbo.EmployeeUserFields.SbiID = dbo.Employee.SbiID
                            inner join dbo.Card on dbo.Employee.SbiID = dbo.Card.SbiID
                        WHERE
                            dbo.Card.CardNumber = @Cracha";

                cmd.Parameters.AddWithValue("@Cracha", Cracha);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para este CRACHÁ. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsInfoCadastro", bsDados);

                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\InfoRelatorios.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);
                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        //-------------------------------------------------------------A CONSULTA PELO DE CRACHA TODOS OS ACESSOS
        public void Consulta_CRACHA_TDS_ACESSOS()
        {
            string Cracha = txtPesquisa.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT DISTINCT (dbo.Employee.SbiID) as [CÓDIGO], dbo.Employee.Name+' '+Employee.Surname as [NOME],
                            dbo.Employee.PreferredName as [CPF], dbo.Employee.Identifier as [MATRICULA], dbo.EmployeeUserFields.UF2 as [EMPRESA],
                            [dbo].[Card].[CardNumber] as [CARTÃO],
                            Case [dbo].[HA_TRANSIT].[STR_DIRECTION]
                                When 'Entry' Then 'ENTRADA'
                                When 'Exit' Then 'SAÍDA'
	                        End As 'DIREÇÃO',
                            CASE USER_TYPE
	                            When 'External Personnel' Then 'TERCEIRO'
                                When 'Employee' Then 'FUNCIONÁRIO' 
	                        End as 'TIPO',
                            TERMINAL as [TERMINAL],
                            dbo.AC_VTERMINAL.DESCRIPTION as [DESCRIÇÃO],
                            dbo.HA_TRANSIT.TRANSIT_DATE as 'TRÂNSITO'
                            FROM Employee
                            inner join EmployeeUserFields on EmployeeUserFields.SbiID = dbo.Employee.SbiID
                            inner join [dbo].[Card] on [dbo].[Card].[SbiID] = [dbo].Employee.[SbiID]
                            inner join dbo.HA_TRANSIT on dbo.Employee.SbiID = dbo.HA_TRANSIT.SBI_ID
                            inner join dbo.AC_VTERMINAL on dbo.HA_TRANSIT.TERMINAL = dbo.AC_VTERMINAL.VTERMINAL_KEY
                            WHERE 
	                        [dbo].[Card].[CardNumber] = @Cracha
UNION ALL

                            SELECT DISTINCT (dbo.ExternalRegular.SbiID), dbo.ExternalRegular.Name+' '+ExternalRegular.Surname,
                            dbo.ExternalRegular.PreferredName, dbo.ExternalRegular.Identifier, dbo.ExternalRegularUserFields.UF2,
                            [dbo].[Card].[CardNumber], 
                            Case [dbo].[HA_TRANSIT].[STR_DIRECTION]
                                When 'Entry' Then 'ENTRADA'
                                When 'Exit' Then 'SAÍDA' End As 'DIREÇÃO',
                            CASE USER_TYPE
	                            When 'External Personnel' Then 'TERCEIRO'
                                When 'Employee' Then 'FUNCIONÁRIO' End as 'TIPO', 
                            TERMINAL, dbo.AC_VTERMINAL.DESCRIPTION, dbo.HA_TRANSIT.TRANSIT_DATE
                            FROM ExternalRegular
                            inner join dbo.ExternalRegularUserFields on dbo.ExternalRegularUserFields.SbiID = dbo.ExternalRegular.SbiID
                            inner join [dbo].[Card] on [dbo].[Card].[SbiID] = [dbo].[ExternalRegular].[SbiID]
                            inner join dbo.HA_TRANSIT on dbo.ExternalRegular.SbiID = dbo.HA_TRANSIT.SBI_ID
                            inner join dbo.AC_VTERMINAL on dbo.HA_TRANSIT.TERMINAL = dbo.AC_VTERMINAL.VTERMINAL_KEY
                            WHERE 
	                        [dbo].[Card].[CardNumber] = @Cracha

                            ORDER BY TRÂNSITO ASC";

                cmd.Parameters.AddWithValue("@Cracha", Cracha);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para este CRACHÁ. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsTdsAcessos", bsDados);
                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\TdsAcessosRelatorio.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);

                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        //------------------------------------------------------------- CONSULTA PELO DE CRACHA SOMENTE ACESSOS NAS CATRACAS POR DATA
        public void consulta_CRACHA_ACESSOS()
        {
            string Cracha = txtPesquisa.Text;
            string DataInicio = txtDataInicio.Text;
            string DataFim = txtDataFim.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT DISTINCT (dbo.Employee.SbiID) as [CÓDIGO],
                            dbo.Employee.Name+' '+Employee.Surname as [NOME],
                            dbo.Employee.PreferredName as [CPF],
                            dbo.Employee.Identifier as [MATRICULA],
                            dbo.EmployeeUserFields.UF2 as [EMPRESA],
                            [dbo].[Card].[CardNumber] as [CARTÃO],
                        Case [dbo].[HA_TRANSIT].[STR_DIRECTION]
                            When 'Entry' Then 'ENTRADA'
                            When 'Exit' Then 'SAÍDA' End as [DIREÇÃO],
                        CASE dbo.HA_TRANSIT.USER_TYPE
                            When 'Employee' Then 'FUNCIONÁRIO' End as [TIPO],
                        TERMINAL,
                        dbo.AC_VTERMINAL.DESCRIPTION as [DESCRIÇÃO],
                        dbo.HA_TRANSIT.TRANSIT_DATE as [TRÂNSITO]
                    FROM Employee
                    inner join EmployeeUserFields on EmployeeUserFields.SbiID = dbo.Employee.SbiID
                    inner join [dbo].[Card] on [dbo].[Card].[SbiID] = [dbo].Employee.[SbiID]
                    inner join dbo.HA_TRANSIT on dbo.Employee.SbiID = dbo.HA_TRANSIT.SBI_ID
                    inner join dbo.AC_VTERMINAL on dbo.HA_TRANSIT.TERMINAL = dbo.AC_VTERMINAL.VTERMINAL_KEY
                    WHERE 
	                    dbo.Card.CardNumber = @Cracha
                    AND
                        TRANSIT_DATE BETWEEN @DataInicio AND @DataFim 
                    AND 
                        (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B')
                    ORDER BY TRÂNSITO ASC";

                cmd.Parameters.AddWithValue("@Cracha", Cracha);
                cmd.Parameters.AddWithValue("@DataInicio", DataInicio);
                cmd.Parameters.AddWithValue("@DataFim", DataFim);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para este CRACHÁ. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsFiltros", bsDados);
                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\FiltroAcessosRelatorio.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);

                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        //------------------------------------------------------------- CONSULTA PELO DE CRACHA SOMENTE ACESSOS CANCELAS DE ENTRADA E SAÍDA
        public void consulta_CRACHA_ACESSOS_CANCELAS()
        {
            string Cracha = txtPesquisa.Text;
            string DataInicio = txtDataInicio.Text;
            string DataFim = txtDataFim.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT DISTINCT (dbo.Employee.SbiID) as [CÓDIGO],
                            dbo.Employee.Name+' '+Employee.Surname as [NOME],
                            dbo.Employee.PreferredName as [CPF],
                            dbo.Employee.Identifier as [MATRICULA],
                            dbo.EmployeeUserFields.UF2 as [EMPRESA],
                            [dbo].[Card].[CardNumber] as [CARTÃO],
                        Case [dbo].[HA_TRANSIT].[STR_DIRECTION]
                            When 'Entry' Then 'ENTRADA'
                            When 'Exit' Then 'SAÍDA' End as [DIREÇÃO],
                        CASE dbo.HA_TRANSIT.USER_TYPE
                            When 'Employee' Then 'FUNCIONÁRIO' End as [TIPO],
                        TERMINAL,
                        dbo.AC_VTERMINAL.DESCRIPTION as [DESCRIÇÃO],
                        dbo.HA_TRANSIT.TRANSIT_DATE as [TRÂNSITO]
                    FROM Employee
                    inner join EmployeeUserFields on EmployeeUserFields.SbiID = dbo.Employee.SbiID
                    inner join [dbo].[Card] on [dbo].[Card].[SbiID] = [dbo].Employee.[SbiID]
                    inner join dbo.HA_TRANSIT on dbo.Employee.SbiID = dbo.HA_TRANSIT.SBI_ID
                    inner join dbo.AC_VTERMINAL on dbo.HA_TRANSIT.TERMINAL = dbo.AC_VTERMINAL.VTERMINAL_KEY
                    WHERE 
	                    dbo.Card.CardNumber = @Cracha
                    AND
                        TRANSIT_DATE BETWEEN @DataInicio AND @DataFim 
                    AND 
                        (TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')
                    ORDER BY TRÂNSITO ASC";

                cmd.Parameters.AddWithValue("@Cracha", Cracha);
                cmd.Parameters.AddWithValue("@DataInicio", DataInicio);
                cmd.Parameters.AddWithValue("@DataFim", DataFim);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para este CRACHÁ. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsFiltros", bsDados);
                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\FiltroAcessosRelatorio.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);

                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }




        //------------------------------------------------------------- CONSULTA TODOS OS ACESSOS PELA MATRÍCULA
        public void Consulta_MATRICULA_TDS_ACESSOS()
        {
            string Matricula = txtPesquisa.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT DISTINCT (dbo.Employee.SbiID) as [CÓDIGO], dbo.Employee.Name+' '+Employee.Surname as [NOME],
                            dbo.Employee.PreferredName as [CPF], dbo.Employee.Identifier as [MATRICULA], dbo.EmployeeUserFields.UF2 as [EMPRESA],
                            [dbo].[Card].[CardNumber] as [CARTÃO],
                            Case [dbo].[HA_TRANSIT].[STR_DIRECTION]
                                When 'Entry' Then 'ENTRADA'
                                When 'Exit' Then 'SAÍDA'
	                        End As 'DIREÇÃO',
                            CASE USER_TYPE
	                            When 'External Personnel' Then 'TERCEIRO'
                                When 'Employee' Then 'FUNCIONÁRIO' 
	                        End as 'TIPO',
                            TERMINAL as [TERMINAL],
                            dbo.AC_VTERMINAL.DESCRIPTION as [DESCRIÇÃO],
                            dbo.HA_TRANSIT.TRANSIT_DATE as 'TRÂNSITO'
                            FROM Employee
                            inner join EmployeeUserFields on EmployeeUserFields.SbiID = dbo.Employee.SbiID
                            inner join [dbo].[Card] on [dbo].[Card].[SbiID] = [dbo].Employee.[SbiID]
                            inner join dbo.HA_TRANSIT on dbo.Employee.SbiID = dbo.HA_TRANSIT.SBI_ID
                            inner join dbo.AC_VTERMINAL on dbo.HA_TRANSIT.TERMINAL = dbo.AC_VTERMINAL.VTERMINAL_KEY
                            WHERE 
	                        dbo.Employee.Identifier = @Matricula
UNION ALL

                            SELECT DISTINCT (dbo.ExternalRegular.SbiID), dbo.ExternalRegular.Name+' '+ExternalRegular.Surname,
                            dbo.ExternalRegular.PreferredName, dbo.ExternalRegular.Identifier, dbo.ExternalRegularUserFields.UF2,
                            [dbo].[Card].[CardNumber], 
                            Case [dbo].[HA_TRANSIT].[STR_DIRECTION]
                                When 'Entry' Then 'ENTRADA'
                                When 'Exit' Then 'SAÍDA' End As 'DIREÇÃO',
                            CASE USER_TYPE
	                            When 'External Personnel' Then 'TERCEIRO'
                                When 'Employee' Then 'FUNCIONÁRIO' End as 'TIPO', 
                            TERMINAL, dbo.AC_VTERMINAL.DESCRIPTION, dbo.HA_TRANSIT.TRANSIT_DATE
                            FROM ExternalRegular
                            inner join dbo.ExternalRegularUserFields on dbo.ExternalRegularUserFields.SbiID = dbo.ExternalRegular.SbiID
                            inner join [dbo].[Card] on [dbo].[Card].[SbiID] = [dbo].[ExternalRegular].[SbiID]
                            inner join dbo.HA_TRANSIT on dbo.ExternalRegular.SbiID = dbo.HA_TRANSIT.SBI_ID
                            inner join dbo.AC_VTERMINAL on dbo.HA_TRANSIT.TERMINAL = dbo.AC_VTERMINAL.VTERMINAL_KEY
                            WHERE 
	                        dbo.ExternalRegular.Identifier = @Matricula

                            ORDER BY TRÂNSITO ASC";

                cmd.Parameters.AddWithValue("@Matricula", Matricula);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para esta MATRICULA. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsTdsAcessos", bsDados);
                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\TdsAcessosRelatorio.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);

                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        //------------------------------------------------------------- CONSULTA PELA MATRICULA SOMENTE ACESSOS NAS CATRACAS POR DATA
        public void consulta_MATRICULA_ACESSOS_CATRACAS()
        {
            string Matricula = txtPesquisa.Text;
            string DataInicio = txtDataInicio.Text;
            string DataFim = txtDataFim.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT DISTINCT (dbo.Employee.SbiID) as [CÓDIGO],
                            dbo.Employee.Name+' '+Employee.Surname as [NOME],
                            dbo.Employee.PreferredName as [CPF],
                            dbo.Employee.Identifier as [MATRICULA],
                            dbo.EmployeeUserFields.UF2 as [EMPRESA],
                            [dbo].[Card].[CardNumber] as [CARTÃO],
                        Case [dbo].[HA_TRANSIT].[STR_DIRECTION]
                            When 'Entry' Then 'ENTRADA'
                            When 'Exit' Then 'SAÍDA' End as [DIREÇÃO],
                        CASE dbo.HA_TRANSIT.USER_TYPE
                            When 'Employee' Then 'FUNCIONÁRIO' End as [TIPO],
                        TERMINAL,
                        dbo.AC_VTERMINAL.DESCRIPTION as [DESCRIÇÃO],
                        dbo.HA_TRANSIT.TRANSIT_DATE as [TRÂNSITO]
                    FROM Employee
                    inner join EmployeeUserFields on EmployeeUserFields.SbiID = dbo.Employee.SbiID
                    inner join [dbo].[Card] on [dbo].[Card].[SbiID] = [dbo].Employee.[SbiID]
                    inner join dbo.HA_TRANSIT on dbo.Employee.SbiID = dbo.HA_TRANSIT.SBI_ID
                    inner join dbo.AC_VTERMINAL on dbo.HA_TRANSIT.TERMINAL = dbo.AC_VTERMINAL.VTERMINAL_KEY
                    WHERE 
	                    dbo.Employee.Identifier = @Matricula
                    AND
                        TRANSIT_DATE BETWEEN @DataInicio AND @DataFim 
                    AND 
                        (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B')
                    ORDER BY TRÂNSITO ASC";

                cmd.Parameters.AddWithValue("@Matricula", Matricula);
                cmd.Parameters.AddWithValue("@DataInicio", DataInicio);
                cmd.Parameters.AddWithValue("@DataFim", DataFim);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para esta MATRICULA. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsFiltros", bsDados);
                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\FiltroAcessosRelatorio.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);

                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        //------------------------------------------------------------- CONSULTA PELA DOCUMENTO, VISITANTES
        public void consulta_VISITANTES_ACESSOS()
        {
            string Documento = txtPesquisa.Text;
            string DataInicio = txtDataInicio.Text;
            string DataFim = txtDataFim.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT HA_VISIT.Name+''+HA_VISIT.Surname as [NOME],VISIT_DOCUMENT AS [DOCUMENTO],CONTACT_NAME+' '+CONTACT_SURNAME as [CONTATO],
	                        HA_VISIT.SOCIETY AS [VISITOU],Visitor.Telephone AS [TELEFONE],Visitor.EMail AS [EMAIL],VISIT_START as [ENTRADA],VISIT_END as [SAIDA]    
                        FROM HA_VISIT
                            INNER JOIN DBO.Visitor ON HA_VISIT.SBI_ID = DBO.Visitor.SbiID
                        WHERE
                            VISIT_DOCUMENT LIKE @Documento AND
                            VISIT_START BETWEEN @DataInicio AND @DataFim";

                cmd.Parameters.AddWithValue("@Documento", Documento);
                cmd.Parameters.AddWithValue("@DataInicio", DataInicio);
                cmd.Parameters.AddWithValue("@DataFim", DataFim);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados visitantes para este DOCUMENTO. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnConsultar.Enabled = true;
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsVisitantes", bsDados);
                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\VisitaRelatorio.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);

                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
            }
            finally
            {
                con.Close();
            }
        }

        //------------------------------------------------------------- CONSULTA PELA EMPRESA, VISITANTES
        public void consulta_VISITANTES_EMPRESA()
        {
            string Empresa = txtPesquisa.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT HA_VISIT.Name+''+HA_VISIT.Surname as [NOME],VISIT_DOCUMENT AS [DOCUMENTO],CONTACT_NAME+' '+CONTACT_SURNAME as [CONTATO],
	                        HA_VISIT.SOCIETY AS [VISITOU],Visitor.Telephone AS [TELEFONE],Visitor.EMail AS [EMAIL],VISIT_START as [ENTRADA],VISIT_END as [SAIDA]    
                        FROM HA_VISIT
                            INNER JOIN DBO.Visitor ON HA_VISIT.SBI_ID = DBO.Visitor.SbiID
                        WHERE
                            HA_VISIT.SOCIETY LIKE @Empresa";

                cmd.Parameters.AddWithValue("@Empresa", Empresa);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados visitantes para este DOCUMENTO. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnConsultar.Enabled = true;
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsVisitantes", bsDados);
                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\VisitaRelatorio.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);

                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
            }
            finally
            {
                con.Close();
            }
        }

        //-------------------------------------------------------------A CONSULTA PELA EMPRESA INFORMAÇÕES CADASTRO ------------------------------
        public void Consulta_EMPRESA_InfoCad()
        {
            string Empresa = txtPesquisa.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT DISTINCT (dbo.ExternalRegular.SbiID) as [CÓDIGO],
                            dbo.ExternalRegular.Name+' '+dbo.ExternalRegular.Surname as [NOME],
                            dbo.ExternalRegular.PreferredName as [CPF],
                            dbo.ExternalRegular.Identifier as [MATRICULA],
                            dbo.Card.CardNumber as [CRACHÁ],
                            dbo.ExternalRegularUserFields.uf2 as [EMPRESA],
                            dbo.ExternalRegularUserFields.uf21 as [TIPO],
                            dbo.ExternalRegular.CommencementDateTime as [CADASTRO],
                            dbo.ExternalRegular.ExpiryDateTime as [EXPIRA]
 
                        FROM[dbo].[ExternalRegular]
                            inner join dbo.ExternalRegularUserFields on dbo.ExternalRegularUserFields.SbiID = dbo.ExternalRegular.SbiID
                            inner join dbo.Card on dbo.ExternalRegular.SbiID = dbo.Card.SbiID 
                        WHERE
                            dbo.ExternalRegularUserFields.uf2 = @Empresa
                        UNION ALL
                        SELECT DISTINCT (dbo.Employee.SbiID),
                            dbo.Employee.Name+' '+dbo.Employee.Surname,
                            dbo.Employee.PreferredName,
                            dbo.Employee.Identifier,
                            dbo.card.CardNumber,
                            dbo.EmployeeUserFields.UF2,
                            dbo.EmployeeUserFields.UF21,
                            dbo.Employee.CommencementDateTime,
                            dbo.employee.ExpiryDateTime
                        FROM[dbo].[Employee]
                            inner join dbo.EmployeeUserFields on dbo.EmployeeUserFields.SbiID = dbo.Employee.SbiID
                            inner join dbo.Card on dbo.Employee.SbiID = dbo.Card.SbiID
                        WHERE
                            dbo.EmployeeUserFields.UF2 = @Empresa";

                cmd.Parameters.AddWithValue("@Empresa", Empresa);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para esta EMPRESA. Verifique se realmente existe essa empresa!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsInfoCadastro", bsDados);

                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\InfoRelatorios.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);
                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }




        //-------------------------------------------------------------DESCOBRINDO O CRACHÁ PELO CPF
        public void descobrir_CRACHA()
        {
            string DescobrirCracha = txtBusca.Text;

            string strsql;
            SqlCommand cmd = new SqlCommand();

            con = getConexaoBD();
            con.Open();

            try
            {
                cboDados.Items.Clear();
                strsql = @"SELECT DBO.Card.CardNumber as [CRACHA] FROM [dbo].[ExternalRegular]
		                        inner join dbo.ExternalRegularUserFields on dbo.ExternalRegularUserFields.SbiID = dbo.ExternalRegular.SbiID
		                        inner join dbo.Card on dbo.ExternalRegular.SbiID = dbo.Card.SbiID 
                            WHERE
                                dbo.ExternalRegular.PreferredName LIKE @DescobrirCracha
                            UNION
                            SELECT dbo.card.CardNumber FROM [dbo].[Employee]
			                    inner join dbo.EmployeeUserFields on dbo.EmployeeUserFields.SbiID = dbo.Employee.SbiID
			                    inner join dbo.Card on dbo.Employee.SbiID = dbo.Card.SbiID
                            WHERE
                                Employee.PreferredName LIKE @DescobrirCracha";

                cmd.Parameters.AddWithValue("@DescobrirCracha", DescobrirCracha);
                cmd.Connection = con;
                cmd.CommandText = strsql;
                SqlDataReader dtReader = cmd.ExecuteReader();

                cboDados.Items.Add("Cartões Carregados!");

                if (dtReader.HasRows)
                {
                    while ((dtReader.Read()))
                    {
                        cboDados.Items.Add(dtReader["CRACHA"]);
                    }
                }
                else
                {
                    cboDados.Items.Clear();
                    cboDados.Items.Add("Sem Crachá !!!");
                }
                cboDados.SelectedIndex = 0;
                con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao efetuar a conexão com o Banco de Dados: " + ex.Message, "Buscar Cartões de Acesso...");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        //-------------------------------------------------------------BUSCA INFORMAÇÕES PELO CRACHÁ
        private void BuscaINFOCad_Cracha()
        {

            string Pesquisa = txtPesquisa.Text;

            string strsql;
            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT
                            DISTINCT(
                            dbo.ExternalRegular.SbiID) as [CÓDIGO],
                            dbo.ExternalRegular.Name+' '+dbo.ExternalRegular.Surname as [NOME],
                            dbo.ExternalRegular.PreferredName as [CPF],
                            dbo.ExternalRegular.Identifier as [MATRICULA],
                            dbo.Card.CardNumber as [CARTÃO],
                            dbo.ExternalRegularUserFields.uf2 as [EMPRESA],
                            dbo.ExternalRegularUserFields.uf21 as [TIPO],
                            dbo.ExternalRegular.CommencementDateTime as [CADASTRO],
                            dbo.ExternalRegular.ExpiryDateTime as [EXPIRA]
 
                        FROM[dbo].[ExternalRegular]
                            inner join dbo.ExternalRegularUserFields on dbo.ExternalRegularUserFields.SbiID = dbo.ExternalRegular.SbiID
                            inner join dbo.Card on dbo.ExternalRegular.SbiID = dbo.Card.SbiID 

                        WHERE
                            dbo.Card.CardNumber = @Pesquisa
                        UNION ALL
                        SELECT
                            DISTINCT (
                            dbo.Employee.SbiID),
                            dbo.Employee.Name+' '+dbo.Employee.Surname,
                            dbo.Employee.PreferredName,
                            dbo.Employee.Identifier,
                            dbo.card.CardNumber,
                            dbo.EmployeeUserFields.UF2,
                            dbo.EmployeeUserFields.UF21,
                            dbo.Employee.CommencementDateTime,
                            dbo.employee.ExpiryDateTime

                        FROM[dbo].[Employee]
                            inner join dbo.EmployeeUserFields on dbo.EmployeeUserFields.SbiID = dbo.Employee.SbiID
                            inner join dbo.Card on dbo.Employee.SbiID = dbo.Card.SbiID

                        WHERE
                            dbo.Card.CardNumber = @Pesquisa";

                cmd.Parameters.AddWithValue("@Pesquisa", Pesquisa);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para este CRACHÁ. Verifique se realmente existe!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = tabela;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        //-------------------------------------------------------------BUSCA INFORMAÇÕES PELO MATRICULA
        public void Consulta_MATRICULA_InfoCad()
        {
            string Matricula = txtPesquisa.Text;

            string strsql;

            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT DISTINCT (dbo.ExternalRegular.SbiID) as [CÓDIGO],
                            dbo.ExternalRegular.Name+' '+dbo.ExternalRegular.Surname as [NOME],
                            dbo.ExternalRegular.PreferredName as [CPF],
                            dbo.ExternalRegular.Identifier as [MATRICULA],
                            dbo.Card.CardNumber as [CRACHÁ],
                            dbo.ExternalRegularUserFields.uf2 as [EMPRESA],
                            dbo.ExternalRegularUserFields.uf21 as [TIPO],
                            dbo.ExternalRegular.CommencementDateTime as [CADASTRO],
                            dbo.ExternalRegular.ExpiryDateTime as [EXPIRA]
 
                        FROM[dbo].[ExternalRegular]
                            inner join dbo.ExternalRegularUserFields on dbo.ExternalRegularUserFields.SbiID = dbo.ExternalRegular.SbiID
                            inner join dbo.Card on dbo.ExternalRegular.SbiID = dbo.Card.SbiID 
                        WHERE
                            dbo.ExternalRegular.Identifier = @Matricula
                        UNION ALL
                        SELECT DISTINCT (dbo.Employee.SbiID),
                            dbo.Employee.Name+' '+dbo.Employee.Surname,
                            dbo.Employee.PreferredName,
                            dbo.Employee.Identifier,
                            dbo.card.CardNumber,
                            dbo.EmployeeUserFields.UF2,
                            dbo.EmployeeUserFields.UF21,
                            dbo.Employee.CommencementDateTime,
                            dbo.employee.ExpiryDateTime
                        FROM[dbo].[Employee]
                            inner join dbo.EmployeeUserFields on dbo.EmployeeUserFields.SbiID = dbo.Employee.SbiID
                            inner join dbo.Card on dbo.Employee.SbiID = dbo.Card.SbiID
                        WHERE
                            dbo.Employee.Identifier = @Matricula";

                cmd.Parameters.AddWithValue("@Matricula", Matricula);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para esta MATRICULA. Verifique se realmente houve acessos!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = null;
                        bsDados.DataSource = tabela;

                        System.Drawing.Printing.PageSettings ps = new System.Drawing.Printing.PageSettings
                        {
                            Landscape = true,
                            PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1170)
                            {
                                RawKind = (int)System.Drawing.Printing.PaperKind.A4
                            },
                            Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10)
                        };
                        rptDados.SetPageSettings(ps);
                        rptDados.SetDisplayMode(DisplayMode.PrintLayout);
                        rptDados.ZoomMode = ZoomMode.PageWidth;

                        rptDados.LocalReport.DataSources.Clear();
                        ReportDataSource rds = new ReportDataSource("DsInfoCadastro", bsDados);

                        rptDados.ProcessingMode = ProcessingMode.Local;
                        rptDados.LocalReport.EnableExternalImages = true;

                        rptDados.LocalReport.ReportPath = @"C:\Report_Flex\Report_Flex_C\Relatorios\InfoRelatorios.rdlc";
                        rptDados.LocalReport.DataSources.Add(rds);
                        rptDados.RefreshReport();
                        btnConsultar.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
                btnConsultar.Enabled = true;
            }
            finally
            {
                con.Close();
            }
        }

        private void BuscaINFOCad_CPF()
        {

            string Pesquisa = txtPesquisa.Text;

            string strsql;
            con = getConexaoBD();
            con.Open();
            SqlCommand cmd = new SqlCommand();
            DataTable tabela = new DataTable();

            try
            {
                strsql = @"SELECT
DISTINCT (
dbo.ExternalRegular.SbiID),
dbo.ExternalRegular.Name+' '+dbo.ExternalRegular.Surname as [Nome],
dbo.ExternalRegular.PreferredName as [Cpf],
dbo.ExternalRegular.Identifier as [Matrícula],
dbo.Card.CardNumber as [Crachá],
dbo.ExternalRegularUserFields.uf2 as [Empresa],
dbo.ExternalRegularUserFields.uf21 as [Tipo],
dbo.ExternalRegular.CommencementDateTime as [Cadastrado em],
dbo.ExternalRegular.ExpiryDateTime as [Expira em]
 
FROM[dbo].[ExternalRegular]
inner join dbo.ExternalRegularUserFields on dbo.ExternalRegularUserFields.SbiID = dbo.ExternalRegular.SbiID
inner join dbo.Card on dbo.ExternalRegular.SbiID = dbo.Card.SbiID 

WHERE
dbo.ExternalRegular.PreferredName = @Pesquisa


UNION ALL

SELECT
DISTINCT (
dbo.Employee.SbiID),
dbo.Employee.Name+' '+dbo.Employee.Surname as [Nome],
dbo.Employee.PreferredName as [Cpf],
dbo.Employee.Identifier as [Matrícula],
dbo.card.CardNumber as [Crachá],
dbo.EmployeeUserFields.UF2 as [Empresa],
dbo.EmployeeUserFields.UF21 as [Tipo],
dbo.Employee.CommencementDateTime as [Cadastrado em],
dbo.employee.ExpiryDateTime as [Expira em]

FROM[dbo].[Employee]
inner join dbo.EmployeeUserFields on dbo.EmployeeUserFields.SbiID = dbo.Employee.SbiID
inner join dbo.Card on dbo.Employee.SbiID = dbo.Card.SbiID

WHERE
dbo.Employee.PreferredName = @Pesquisa";

                cmd.Parameters.AddWithValue("@Pesquisa", Pesquisa);

                cmd.Connection = con;
                cmd.CommandText = strsql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    tabela.Load(reader);
                    if (tabela.Rows.Count == 0)
                    {
                        MessageBox.Show("Não foram encontrados dados para este CPF. Verifique se realmente existe!", "REPORT Flex 1.0 | Informativo !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        tabela.Load(reader);
                        bsDados.DataSource = tabela;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Consultar Registros");
            }
            finally
            {
                con.Close();
            }
        }

        //FILTRO DO CAMPO PESQUISA -----------------------------------------------------------------------------
        public void searchData(string valueToSearch)
        {
            con = getConexaoBD();
            con.Open();
            string query = @"SELECT Employee.SbiID as [CÓDIGO], 
                                Employee.Name + ' ' + Employee.Surname as [NOME], 
                                Employee.PreferredName as [CPF],
                                EmployeeUserFields.UF2 as [EMPRESA],
                                AC_BEHAVIOR.DESCRIPTION as [NÍVEL],
                             Case Employee.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End As[STATUS]
                          FROM Employee
                              inner join EmployeeUserFields on EmployeeUserFields.SbiID = Employee.SbiID
                              inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = Employee.SbiID
                              inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                              WHERE CONCAT(Employee.Name, Employee.PreferredName, Employee.Surname, EmployeeUserFields.UF2,
                              AC_BEHAVIOR.DESCRIPTION) LIKE '%" + valueToSearch + "%'" +
                              @"UNION " +
                              @"select ExternalRegular.SbiID, 
                              ExternalRegular.Name + ' ' + ExternalRegular.Surname, 
                              ExternalRegular.PreferredName,
                              ExternalRegularUserFields.UF2,
                              AC_BEHAVIOR.DESCRIPTION,
                              Case ExternalRegular.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End
                        from  ExternalRegular
                            inner join ExternalRegularUserFields on ExternalRegularUserFields.SbiID = ExternalRegular.SbiID
                            inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = ExternalRegular.SbiID
                            inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                        WHERE
                            CONCAT(ExternalRegular.Name, ExternalRegular.Surname, ExternalRegular.PreferredName, ExternalRegularUserFields.UF2, AC_BEHAVIOR.DESCRIPTION) LIKE '%" + valueToSearch + "%'";

            SqlCommand command = new SqlCommand(query, con);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable Table = new DataTable();
            adapter.Fill(Table);
            bsDados.DataSource = Table;
            rptDados.RefreshReport();
            con.Close();
        }

        

        //FILTRO DO CAMPO PESQUISA -----------------------------------------------------------------------------
        public void searchData1()
        {
            con = getConexaoBD();
            con.Open();
            string query = @"SELECT Employee.SbiID as [CÓDIGO], 
                                Employee.Name + ' ' + Employee.Surname as [NOME], 
                                Employee.PreferredName as [CPF],
                                EmployeeUserFields.UF2 as [EMPRESA],
                                AC_BEHAVIOR.DESCRIPTION as [NÍVEL],
                             Case Employee.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End As [STATUS]
                          FROM Employee
                              inner join EmployeeUserFields on EmployeeUserFields.SbiID = Employee.SbiID
                              inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = Employee.SbiID
                              inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                          WHERE 
                                DBO.Employee.StateID = '0'
                              UNION
                              SELECT ExternalRegular.SbiID, 
                              ExternalRegular.Name + ' ' + ExternalRegular.Surname, 
                              ExternalRegular.PreferredName,
                              ExternalRegularUserFields.UF2,
                              AC_BEHAVIOR.DESCRIPTION,
                              Case ExternalRegular.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End
                        from  ExternalRegular
                            inner join ExternalRegularUserFields on ExternalRegularUserFields.SbiID = ExternalRegular.SbiID
                            inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = ExternalRegular.SbiID
                            inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                        WHERE
                            DBO.ExternalRegular.StateID = '0'";

            SqlCommand command = new SqlCommand(query, con);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable Table = new DataTable();
            adapter.Fill(Table);
            bsDados.DataSource = Table;
            rptDados.RefreshReport();
            con.Close();
        }


        //FILTRO DO CAMPO PESQUISA -----------------------------------------------------------------------------
        public void searchData2()
        {
            con = getConexaoBD();
            con.Open();
            string query = @"SELECT Employee.SbiID as [CÓDIGO], 
                                Employee.Name + ' ' + Employee.Surname as [NOME], 
                                Employee.PreferredName as [CPF],
                                EmployeeUserFields.UF2 as [EMPRESA],
                                AC_BEHAVIOR.DESCRIPTION as [NÍVEL],
                             Case Employee.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End As [STATUS]
                          FROM Employee
                              inner join EmployeeUserFields on EmployeeUserFields.SbiID = Employee.SbiID
                              inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = Employee.SbiID
                              inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                          WHERE 
                                DBO.Employee.StateID = '3'
                              UNION
                              select ExternalRegular.SbiID, 
                              ExternalRegular.Name + ' ' + ExternalRegular.Surname, 
                              ExternalRegular.PreferredName,
                              ExternalRegularUserFields.UF2,
                              AC_BEHAVIOR.DESCRIPTION,
                              Case ExternalRegular.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End
                        from  ExternalRegular
                            inner join ExternalRegularUserFields on ExternalRegularUserFields.SbiID = ExternalRegular.SbiID
                            inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = ExternalRegular.SbiID
                            inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                        WHERE
                            DBO.ExternalRegular.StateID = '3'";

            SqlCommand command = new SqlCommand(query, con);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable Table = new DataTable();
            adapter.Fill(Table);
            bsDados.DataSource = Table;
            rptDados.RefreshReport();
            con.Close();
        }

        //FILTRO DO CAMPO PESQUISA -----------------------------------------------------------------------------
        public void searchData3()
        {
            con = getConexaoBD();
            con.Open();
            string query = @"SELECT Employee.SbiID as [CÓDIGO], 
                                Employee.Name + ' ' + Employee.Surname as [NOME], 
                                Employee.PreferredName as [CPF],
                                EmployeeUserFields.UF2 as [EMPRESA],
                                AC_BEHAVIOR.DESCRIPTION as [NÍVEL],
                             Case Employee.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End As [STATUS]
                          FROM Employee
                              inner join EmployeeUserFields on EmployeeUserFields.SbiID = Employee.SbiID
                              inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = Employee.SbiID
                              inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                          WHERE 
                                DBO.Employee.StateID = '4'
                              UNION
                              select ExternalRegular.SbiID, 
                              ExternalRegular.Name + ' ' + ExternalRegular.Surname, 
                              ExternalRegular.PreferredName,
                              ExternalRegularUserFields.UF2,
                              AC_BEHAVIOR.DESCRIPTION,
                              Case ExternalRegular.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End
                        from  ExternalRegular
                            inner join ExternalRegularUserFields on ExternalRegularUserFields.SbiID = ExternalRegular.SbiID
                            inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = ExternalRegular.SbiID
                            inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                        WHERE
                            DBO.ExternalRegular.StateID = '4'";

            SqlCommand command = new SqlCommand(query, con);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable Table = new DataTable();
            adapter.Fill(Table);
            bsDados.DataSource = Table;
            rptDados.RefreshReport();
            con.Close();
        }

        //FILTRO DO CAMPO PESQUISA -----------------------------------------------------------------------------
        public void searchData4()
        {
            con = getConexaoBD();
            con.Open();
            string query = @"SELECT Employee.SbiID as [CÓDIGO], 
                                Employee.Name + ' ' + Employee.Surname as [NOME], 
                                Employee.PreferredName as [CPF],
                                EmployeeUserFields.UF2 as [EMPRESA],
                                AC_BEHAVIOR.DESCRIPTION as [NÍVEL],
                             Case Employee.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End As [STATUS]
                          FROM Employee
                              inner join EmployeeUserFields on EmployeeUserFields.SbiID = Employee.SbiID
                              inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = Employee.SbiID
                              inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                          WHERE 
                                DBO.Employee.StateID = '5'
                              UNION
                              select ExternalRegular.SbiID, 
                              ExternalRegular.Name + ' ' + ExternalRegular.Surname, 
                              ExternalRegular.PreferredName,
                              ExternalRegularUserFields.UF2,
                              AC_BEHAVIOR.DESCRIPTION,
                              Case ExternalRegular.StateID
                                When 3 Then 'INATIVO'
                                When 0 Then 'ATIVO'
                                When 1 Then 'ATIVO'
                                When 4 Then 'EXPIRADO'
                                When 5 Then 'INVALIDO'
                              End
                        from  ExternalRegular
                            inner join ExternalRegularUserFields on ExternalRegularUserFields.SbiID = ExternalRegular.SbiID
                            inner join SbiSiteBehavior on SbiSiteBehavior.SbiID = ExternalRegular.SbiID
                            inner join AC_BEHAVIOR on AC_BEHAVIOR.BEHAVIOR_ID = SbiSiteBehavior.Behavior
                        WHERE
                            DBO.ExternalRegular.StateID = '5'";

            SqlCommand command = new SqlCommand(query, con);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable Table = new DataTable();
            adapter.Fill(Table);
            bsDados.DataSource = Table;
            rptDados.RefreshReport();
            con.Close();
        }

        //FILTRO DO CAMPO PESQUISA -----------------------------------------------------------------------------
        public void searchData5(string valueToSearch)
        {
            con = getConexaoBD();
            con.Open();
            string query = @"SELECT DISTINCT (dbo.Employee.SbiID) as [CÓDIGO],
                            dbo.Employee.Name+' '+Employee.Surname as [NOME],
                            dbo.Employee.PreferredName as [CPF],
                            dbo.Employee.Identifier as [MATRICULA],
                            dbo.EmployeeUserFields.UF2 as [EMPRESA],
                            [dbo].[Card].[CardNumber] as [CARTÃO],
                        Case [dbo].[HA_TRANSIT].[STR_DIRECTION]
                            When 'Entry' Then 'ENTRADA'
                            When 'Exit' Then 'SAÍDA' End as [DIREÇÃO],
                        CASE dbo.HA_TRANSIT.USER_TYPE
                            When 'Employee' Then 'FUNCIONÁRIO' End as [TIPO],
                        TERMINAL,
                        dbo.AC_VTERMINAL.DESCRIPTION as [DESCRIÇÃO],
                        dbo.HA_TRANSIT.TRANSIT_DATE as [TRÂNSITO]
                    FROM Employee
                    inner join EmployeeUserFields on EmployeeUserFields.SbiID = dbo.Employee.SbiID
                    inner join [dbo].[Card] on [dbo].[Card].[SbiID] = [dbo].Employee.[SbiID]
                    inner join dbo.HA_TRANSIT on dbo.Employee.SbiID = dbo.HA_TRANSIT.SBI_ID
                    inner join dbo.AC_VTERMINAL on dbo.HA_TRANSIT.TERMINAL = dbo.AC_VTERMINAL.VTERMINAL_KEY
                    WHERE 
                        dbo.Employee.Identifier = '" + Resultado + "' AND "+
	                    @"CONCAT(dbo.HA_TRANSIT.STR_DIRECTION, TERMINAL) LIKE  '%" + valueToSearch + "%'" +
                    @"AND
                        TRANSIT_DATE BETWEEN '" + txtDataInicio.Text + "' AND '" + txtDataFim.Text + "'" +
                    @"AND 
                        (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B')
                    ORDER BY TRÂNSITO ASC";

            SqlCommand command = new SqlCommand(query, con);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable Table = new DataTable();
            adapter.Fill(Table);
            bsDados.DataSource = Table;
            rptDados.RefreshReport();
            con.Close();
        }


        //-------------------------------------------------------------CARREGAR COMBOS
        public void carregaCombos()
        {
            cboPesquisa.Items.Add("Selecione...");
            //cboPesquisa.Items.Add("Cpf");
            cboPesquisa.Items.Add("Matrícula");
            cboPesquisa.Items.Add("Empresa");
            cboPesquisa.Items.Add("Crachá");
            //cboPesquisa.Items.Add("Veículos");
            cboPesquisa.Items.Add("Nível de Acesso");
            cboPesquisa.Items.Add("Visitantes");
            cboPesquisa.SelectedIndex = 0;
        }

        private void btnFiltrar_Click(object sender, EventArgs e)
        {
            if (cboPesquisa.Text == "Crachá")
            {
                string valueToSearch = txtPesquisa.Text.ToString();
                searchData5(valueToSearch);
            }
            else if (cboPesquisa.Text == "Matrícula")
            {
                string valueToSearch = txtPesquisa.Text.ToString();
                searchData5(valueToSearch);
            }
            else if (cboPesquisa.Text == "Empresa")
            {
                string valueToSearch = txtPesquisa.Text.ToString();
                searchData5(valueToSearch);
            }
            else if (cboPesquisa.Text == "Nível de Acesso")
            {
                string valueToSearch = txtPesquisa.Text.ToString();
                searchData(valueToSearch);
            }
            
            rdbAtivo.Checked = false;
            rdbInativo.Checked = false;
            rdbExpirado.Checked = false;
            rdbInvalido.Checked = false;
        }

        private void DesativaControles()
        {
            cboObter.Enabled = false;
            txtPesquisa.Enabled = false;
            txtPesquisa.BackColor = Color.LightGray;
            Ativa.Enabled = false;
            Desativa.Enabled = false;
            txtDataInicio.Enabled = false;
            txtDataInicio.BackColor = Color.LightGray;
            txtDataFim.Enabled = false;
            txtDataFim.BackColor = Color.LightGray;
            btnConsultar.Enabled = false;
            btnFiltrar.Enabled = false;
        }

        private void LimparControles()
        {
            txtPesquisa.Clear();
            txtDataInicio.Clear();
            txtDataFim.Clear();
            txtBusca.Clear();
            Ativa.Checked = false;
            Desativa.Checked = false;
            cboDados.Items.Clear();
            cboObter.Items.Clear();
            bsDados.DataSource = null;
            rptDados.LocalReport.ReportPath = null;
            rptDados.RefreshReport();
        }

        private void txtDataInicio_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (txtDataInicio.MaskCompleted)
            {
                txtDataFim.Focus();
            }
        }

        private void txtDataFim_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (txtDataFim.MaskCompleted)
            {
                btnConsultar.Focus();
            }
        }

        private void bsDados_CurrentChanged(object sender, EventArgs e)
        {
            if (bsDados.DataSource != null)
            {
                if (cboPesquisa.Text == "Nível de Acesso")
                {
                    rdbAtivo.Enabled = true;
                    rdbInativo.Enabled = true;
                    rdbExpirado.Enabled = true;
                    rdbInvalido.Enabled = true;
                    btnFiltrar.Enabled = true;
                }
            }
        }

        private void rdbAtivo_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbAtivo.Checked == true)
            {
                searchData1();
            }
        }

        private void rdbInativo_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbInativo.Checked == true)
            {
                searchData2();
            }
        }

        private void rdbExpirado_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbExpirado.Checked == true)
            {
                searchData3();
            }
        }

        private void rdbInvalido_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbInvalido.Checked == true)
            {
                searchData4();
            }
        }

        private void FrmConsultas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
                e.SuppressKeyPress = true;
            }
        }

        private void cboDados_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboDados.SelectedIndex > 0)
            {
                txtPesquisa.Text = cboDados.Text;
            }
        }

        private void cboDados_Click(object sender, EventArgs e)
        {
            if (cboDados.SelectedIndex > 0)
            {
                txtPesquisa.Text = cboDados.Text;
            }
        }
    }
}