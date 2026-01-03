using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FrmAtivarClientesPrestadores : Form
    {
        public int CodigoCli = 0;
        public int CodigoCliAdquirido = 0;
        public int CodigoPrest = 0;
        public int CodigoPrestAdquirido = 0;
        public string ImageLocalizaCli = null;
        public string ImageLocalizaPrest = null;
        public string ImagemNomeCli = null;
        public string NomeCliAtualizado = null;
        public string ImagemNomePrest = null;
        public string NomePrestAtualizado = null;
        public string ClienteInativo = "";
        public string PrestadorInativo = "";
        public string NomeCliAtivo = "";
        public string NomePrestAtivo = "";

        public FrmAtivarClientesPrestadores()
        {
            InitializeComponent();
        }

        private SqlConnection con = null;
        private SqlCommand cmd = null;

        private SqlConnection getConexaoBD()
        {
            string strConexao = ConfigurationManager.ConnectionStrings["StringConexao"].ConnectionString;
            return new SqlConnection(strConexao);
        }

        private void FrmAtivarClientesPrestadores_Load(object sender, EventArgs e)
        {
            Carregar_Clientes();
            Carregar_Prestadores();
        }

        public void Carregar_Clientes()
        {
            string strsql;
            SqlCommand cmd = new SqlCommand();

            con = getConexaoBD();
            con.Open();

            try
            {
                cboClientes.Items.Clear();
                strsql = @"SELECT SBID, NOME as [Clientes], ATIVO as [Status], CAMINHOIMG FROM Clientes";

                cmd.Connection = con;
                cmd.CommandText = strsql;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dtResultado = new DataTable();
                da.Fill(dtResultado);

                cboClientes.DropDownStyle = ComboBoxStyle.DropDownList;
                cboClientes.DataSource = null;              
                cboClientes.DataSource = dtResultado;
                cboClientes.DisplayMember = "Clientes";
                cboClientes.ValueMember = "Status";
                cboClientes.Text = "";

                Seleciona_Clientes();

                if (cboClientes.SelectedIndex != -1)
                {
                    DataRowView drw = ((DataRowView)cboClientes.SelectedItem);
                    CodigoCli = Convert.ToInt32(drw["SBID"]);
                }

                NomeCliAtivo = cboClientes.Text;

                con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao efetuar a conexão com o Banco de Dados: " + ex.Message, "Buscar Clientes...");
            }
            finally
            {
                con.Close();
            }
        }

        public void Seleciona_Clientes()
        {
            cboClientes.SelectedValue = 1;
        }

        public void Carregar_Prestadores()
        {
            string strsql;
            SqlCommand cmd = new SqlCommand();

            con = getConexaoBD();
            con.Open();

            try
            {
                cboPrestadores.Items.Clear();
                strsql = @"SELECT SBID, NOME as [Prestadores], ATIVO as [Status], CAMINHOIMG FROM Prestadores";

                cmd.Connection = con;
                cmd.CommandText = strsql;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dtResultado = new DataTable();
                da.Fill(dtResultado);

                cboPrestadores.DropDownStyle = ComboBoxStyle.DropDownList;
                cboPrestadores.DataSource = null;             
                cboPrestadores.DataSource = dtResultado;
                cboPrestadores.DisplayMember = "Prestadores";
                cboPrestadores.ValueMember = "Status";
                cboPrestadores.Text = "";

                Seleciona_Prestadores();

                if (cboPrestadores.SelectedIndex != -1)
                {
                    DataRowView drw = ((DataRowView)cboPrestadores.SelectedItem);
                    CodigoPrest = Convert.ToInt32(drw["SBID"]);
                }

                NomePrestAtivo = cboPrestadores.Text;

                con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao efetuar a conexão com o Banco de Dados: " + ex.Message, "Buscar Prestadores...");
            }
            finally
            {
                con.Close();
            }
        }

        public void Seleciona_Prestadores()
        {
            cboPrestadores.SelectedValue = 1;
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            con.Close();
            this.Close();
        }

        private void cboClientes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboClientes.SelectedIndex != -1)
            {
                DataRowView drw = ((DataRowView)cboClientes.SelectedItem);
                CodigoCliAdquirido = Convert.ToInt32(drw["SBID"]);
                ImageLocalizaCli = "";
                ImageLocalizaCli = Convert.ToString(drw["CAMINHOIMG"]);
                picCliente.ImageLocation = ImageLocalizaCli;
            }
        }

        private void cboPrestadores_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboPrestadores.SelectedIndex != -1)
            {
                DataRowView drw = ((DataRowView)cboPrestadores.SelectedItem);
                CodigoPrestAdquirido = Convert.ToInt32(drw["SBID"]);
                ImageLocalizaPrest = "";
                ImageLocalizaPrest = Convert.ToString(drw["CAMINHOIMG"]);
                picLogo.ImageLocation = ImageLocalizaPrest;
            }
        }

        private void btnDefinir_Click(object sender, EventArgs e)
        {
            if (cboClientes.Items.Count > 0 || cboPrestadores.Items.Count > 0)
            {
                if (cboClientes.SelectedIndex != -1 || cboPrestadores.SelectedIndex != -1)
                {
                    // Processar Cliente se selecionado
                    if (cboClientes.SelectedIndex != -1)
                    {
                        if (File.Exists(Application.StartupPath + @"\Logos\Ativos\ClienteAtivo.jpg"))
                        {
                            File.Delete(Application.StartupPath + @"\Logos\Ativos\ClienteAtivo.jpg");
                            DesativaCliente();
                        }
                        AtivaCliente();
                    }

                    // Processar Prestador se selecionado
                    if (cboPrestadores.SelectedIndex != -1)
                    {
                        if (File.Exists(Application.StartupPath + @"\Logos\Ativos\PrestadorAtivo.jpg"))
                        {
                            File.Delete(Application.StartupPath + @"\Logos\Ativos\PrestadorAtivo.jpg");
                            DesativaPrestador();
                        }
                        AtivaPrestador();
                    }

                    MessageBox.Show("O Cliente e o Prestador foram definidos como Cabeçalho e Rodapé principal da aplicação!", "Report Flex | Definido com Sucesso !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Você precisa selecionar um Cliente e um Prestador para poder ativar!", "Report Flex | Informa!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    cboClientes.Focus();
                }     
            }
            else
            {
                MessageBox.Show("Não existem cabeçalhos ou rodapés cadastrados!", "Report Flex | Informa!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            this.Close();
        }

        private void AtivaCliente()
        {
            AtivaCliente2();
            con = getConexaoBD();
            con.Open();

            NomeCliAtualizado = "";
            NomeCliAtualizado = Application.StartupPath + @"\Logos\Ativos\ClienteAtivo.jpg";

            string query = "UPDATE CLIENTES SET ATIVO='1', CAMINHOIMG=@NomeCliAtualizado WHERE SBID='" + CodigoCliAdquirido + "'";
            cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@NomeCliAtualizado", NomeCliAtualizado);
            cmd.ExecuteNonQuery();
            cmd = null;
            CodigoCli = 0;
            NomeCliAtualizado = "";
            con.Close();
        }

        private void AtivaPrestador()
        {
            AtivaPrestador2();
            con = getConexaoBD();
            con.Open();
            NomePrestAtualizado = "";
            NomePrestAtualizado = Application.StartupPath + @"\Logos\Ativos\PrestadorAtivo.jpg";
            string query = "UPDATE PRESTADORES SET ATIVO='1', CAMINHOIMG=@NomePrestAtualizado WHERE SBID='" + CodigoPrestAdquirido + "'";
            cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@NomePrestAtualizado", NomePrestAtualizado);
            cmd.ExecuteNonQuery();
            cmd = null;
            CodigoPrest = 0;
            NomePrestAtualizado = "";
            con.Close();
        }

        private void DesativaCliente()
        {
            string nomeCliCompleto = NomeCliAtivo;
            string CliPrimeiroNome = "";
            string[] arrayCliNome = nomeCliCompleto.Split(' ');
            CliPrimeiroNome = arrayCliNome[0];
            string nomeCliente = CliPrimeiroNome + ".jpg";

            con = getConexaoBD();
            con.Open();

            string query = "UPDATE CLIENTES SET ATIVO='0', CAMINHOIMG=@clienteinativo WHERE SBID='" + CodigoCli + "'";
            cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@clienteinativo", Application.StartupPath + @"\Logos\Inativos\" + nomeCliente);
            cmd.ExecuteNonQuery();
            cmd = null;
            NomeCliAtivo = "";
            con.Close();
        }

        private void DesativaPrestador()
        {
            string nomePrestCompleto = NomePrestAtivo;
            string PrestPrimeiroNome = "";
            string[] arrayPrestNome = nomePrestCompleto.Split(' ');
            PrestPrimeiroNome = arrayPrestNome[0];
            string nomePrestador = PrestPrimeiroNome + ".jpg";

            con = getConexaoBD();
            con.Open();

            string query = "UPDATE PRESTADORES SET ATIVO='0', CAMINHOIMG=@nomePrestador WHERE SBID='" + CodigoPrest + "'";
            cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@nomePrestador", Application.StartupPath + @"\Logos\Inativos\" + nomePrestador);
            cmd.ExecuteNonQuery();
            cmd = null;
            NomePrestAtivo = "";
            con.Close();
        }

        private void AtivaCliente2()
        {
            string ClienteAtivo1 = "ClienteAtivo.jpg";
            CopiarFile(ImageLocalizaCli, Application.StartupPath + @"\Logos\Ativos\" + ClienteAtivo1);
        }

        public void CopiarFile(string originalName, string newName)
        {
            File.Copy(originalName, newName);
        }

        private void AtivaPrestador2()
        {
            string PrestadorAtivo1 = "PrestadorAtivo.jpg";
            CopiarFile(ImageLocalizaPrest, Application.StartupPath + @"\Logos\Ativos\" + PrestadorAtivo1);
        }

        private void cboClientes_SelectedValueChanged(object sender, EventArgs e)
        {
            if (cboClientes.SelectedIndex != -1)
            {
                DataRowView drw = ((DataRowView)cboClientes.SelectedItem);
                CodigoCliAdquirido = Convert.ToInt32(drw["SBID"]);
                ImageLocalizaCli = "";
                ImageLocalizaCli = Convert.ToString(drw["CAMINHOIMG"]);
                picCliente.ImageLocation = ImageLocalizaCli;
            }
        }

        private void cboPrestadores_SelectedValueChanged(object sender, EventArgs e)
        {
            if (cboPrestadores.SelectedIndex != -1)
            {
                DataRowView drw = ((DataRowView)cboPrestadores.SelectedItem);
                CodigoPrestAdquirido = Convert.ToInt32(drw["SBID"]);
                ImageLocalizaPrest = "";
                ImageLocalizaPrest = Convert.ToString(drw["CAMINHOIMG"]);
                picLogo.ImageLocation = ImageLocalizaPrest;
            }
        }
    }
}