using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApp1
{
    public partial class FrmPopulacional1 : Form
    {
        public FrmPopulacional1()
        {
            InitializeComponent();
        }

        private SqlConnection con = null;
        private SqlCommand cmd = null;
        private SqlConnection con2 = null;
        private SqlCommand cmd2 = null;
        private SqlConnection con3 = null;
        private SqlCommand cmd3 = null;
        DataTable dt = new DataTable();
        string Empresa = "";
        int QUANTIDADEFUNC = 0;
        int QUANTIDADETERC = 0;
        int QUANTIDADE = 0;
        string DADOSGRIDVIEW;

        private SqlConnection getConexaoBD()
        {
            string strConexao = ConfigurationManager.ConnectionStrings["StringConexao1"].ConnectionString;
            return new SqlConnection(strConexao);
        }
        private SqlConnection getConexaoBD2()
        {
            string strConexao = ConfigurationManager.ConnectionStrings["StringConexao"].ConnectionString;
            return new SqlConnection(strConexao);
        }

        private void FrmPopulacional1_Load(object sender, EventArgs e)
        {
            FilterRecords("");
            //VerificaHorasFUNC();
            CartoesAtivos();
            CartoesInativos();
            Funcionarios("");
            Terceiros("");
            //FuncionariosHoje("");
            //TerceirosHoje("");
            VisitantesHoje("");
            VERIFICA_ENTRY_EXIT_FUNC();
            VERIFICA_ENTRY_EXIT_TERC();
            //VERIFICA_ENTRY_EXIT_VISIT();

            RdbFunc.Checked = true;

            DgvPopulacional.Sort(DgvPopulacional.Columns[0], ListSortDirection.Ascending);
            foreach (DataGridViewColumn column in DgvPopulacional.Columns)
            {
                if (column.DataPropertyName == "EMPRESA")
                    column.Width = 150;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            DgvPopulacional.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            DgvPopulacional.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            DgvPopulacional.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        public void FilterRecords(string search)
        {
            //******************************************************************* Funcionários ****************************************************************************
            if (RdbFunc.Checked == true)
            {
                FuncionariosChecked();
                FilterFUNC("");
                CarregarComboFUNC("");
                CARREGA_GRAFICO_HorasFUNC();
            }
            //******************************************************************* Terceiros *******************************************************************************
            else if (RdbTerc.Checked == true)
            {
                TerceirosChecked();
                FilterTERC("");
                CarregarComboTERC("");
                CARREGA_GRAFICO_HorasTERC();
            }
            //******************************************************************* Todos **********************************************************************************
            else if (RdbTodos.Checked == true)
            {
                TodosChecked();
                CarregarComboTDS("");
                FilterTODOS("");
                VerificaHorasTODOS();
            }

            //DgvPopulacional.Sort(DgvPopulacional.Columns[0], ListSortDirection.Ascending);
            //foreach (DataGridViewColumn column in DgvPopulacional.Columns)
            //{
            //    if (column.DataPropertyName == "EMPRESA")
            //        column.Width = 150;
            //    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            //}

            //DgvPopulacional.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            //DgvPopulacional.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            //DgvPopulacional.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;

        }

        //************************************************* FILTRA TODOS *******************************************************************************************
        public void FilterTODOS(string search)
        {
            con3 = getConexaoBD();
            con3.Open();
            string query = @"DECLARE
                            @FUNCIONARIOS INT,
                            @TERCEIROS INT,
                            @TOTAL INT

                            SET @FUNCIONARIOS = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                                 WHERE
						                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                                    TRANSIT_DATE 
							                            BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B'))

                            SET @TERCEIROS = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                                WHERE
					                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
						                            TRANSIT_DATE 
							                            BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))

                            SET @TOTAL = @FUNCIONARIOS + @TERCEIROS

                            SELECT @TOTAL AS [QUANTIDADE]";

            cmd3 = new SqlCommand(query, con3);

            SqlDataReader dtReader1 = cmd3.ExecuteReader();

            lblTotal1.Text = "";
            if (dtReader1.HasRows)
            {
                while ((dtReader1.Read()))
                {
                    lblTotal1.Text = dtReader1["QUANTIDADE"].ToString();
                }
            }
        }

        //************************************************* FILTRA SOMENTE FUNCIONÁRIOS ****************************************************************************
        public void FilterFUNC(string search)
        {
            con3 = getConexaoBD();
            con3.Open();
            string query = @"DECLARE
                            @FUNCIONARIOS INT,
                            @TOTAL INT

                            SET @FUNCIONARIOS = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                                 WHERE
						                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                                    TRANSIT_DATE 
                                                        BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
 
                            SET @TOTAL = @FUNCIONARIOS
                            SELECT @TOTAL AS [QUANTIDADE]";

            cmd3 = new SqlCommand(query, con3);

            SqlDataReader dtReader1 = cmd3.ExecuteReader();

            lblTotal1.Text = "";
            if (dtReader1.HasRows)
            {
                while ((dtReader1.Read()))
                {
                    lblTotal1.Text = dtReader1["QUANTIDADE"].ToString();
                }
            }
        }

        //************************************************* FILTRA SOMENTE TERCEIROS *******************************************************************************
        public void FilterTERC(string search)
        {
            con3 = getConexaoBD();
            con3.Open();
            string query = @"SELECT COUNT(DISTINCT(DBO.ExternalRegularUserFields.SbiID)) AS [QUANTIDADE] FROM ExternalRegularUserFields LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                             WHERE
					            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                             TRANSIT_DATE 
							    BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')";

            cmd3 = new SqlCommand(query, con3);

            SqlDataReader dtReader1 = cmd3.ExecuteReader();

            lblTotal1.Text = "";
            if (dtReader1.HasRows)
            {
                while ((dtReader1.Read()))
                {
                    lblTotal1.Text = dtReader1["QUANTIDADE"].ToString();
                }
            }
        }

        //************************************************* CARREGA COMBO COM TODOS FUNCIONÁRIOS *******************************************************************
        public void CarregarComboFUNC(string search)
        {
            con3 = getConexaoBD();
            con3.Open();
            string query = @"SELECT EmployeeUserFields.UF2 AS [EMPRESA] FROM HA_TRANSIT 
                                INNER JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                            WHERE
	                            HA_TRANSIT.STR_DIRECTION = 'Entry' and
                            TRANSIT_DATE 
                            BETWEEN 
		                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' GROUP BY EmployeeUserFields.UF2";

            cmd3 = new SqlCommand(query, con3);

            SqlDataReader dtReader1 = cmd3.ExecuteReader();

            cboFiltro.Items.Clear();

            if (dtReader1.HasRows)
            {
                while ((dtReader1.Read()))
                {
                    cboFiltro.Items.Add(dtReader1["EMPRESA"].ToString());
                }
            }
        }

        //************************************************* CARREGA COMBO COM TODOS TERCEIROS **********************************************************************
        public void CarregarComboTERC(string search)
        {
            con3 = getConexaoBD();
            con3.Open();
            string query = @"SELECT ExternalRegularUserFields.UF2 as [EMPRESA] FROM HA_TRANSIT 
	                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                            WHERE
	                            HA_TRANSIT.STR_DIRECTION = 'Entry'
                            AND
                            TRANSIT_DATE BETWEEN 
		                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' GROUP BY ExternalRegularUserFields.UF2";

            cmd3 = new SqlCommand(query, con3);

            SqlDataReader dtReader1 = cmd3.ExecuteReader();

            cboFiltro.Items.Clear();

            if (dtReader1.HasRows)
            {
                while ((dtReader1.Read()))
                {
                    cboFiltro.Items.Add(dtReader1["EMPRESA"].ToString());
                }
            }
        }

        //************************************************* CARREGA COMBO COM TODAS EMPRESAS ***********************************************************************
        public void CarregarComboTDS(string search)
        {
            con3 = getConexaoBD();
            con3.Open();
            string query = @"SELECT EmployeeUserFields.UF2 AS [EMPRESA] FROM HA_TRANSIT 
                                INNER JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                            WHERE
	                            HA_TRANSIT.STR_DIRECTION = 'Entry' and
                            TRANSIT_DATE 
                            BETWEEN 
		                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' GROUP BY EmployeeUserFields.UF2
                        UNION
                            SELECT ExternalRegularUserFields.UF2 FROM HA_TRANSIT 
	                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                            WHERE
	                            HA_TRANSIT.STR_DIRECTION = 'Entry'
                            AND
                            TRANSIT_DATE BETWEEN 
		                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' GROUP BY ExternalRegularUserFields.UF2";

            cmd3 = new SqlCommand(query, con3);

            SqlDataReader dtReader1 = cmd3.ExecuteReader();

            cboFiltro.Items.Clear();

            if (dtReader1.HasRows)
            {
                while ((dtReader1.Read()))
                {
                    cboFiltro.Items.Add(dtReader1["EMPRESA"].ToString());
                }
            }
        }

        //************************************************* CARREGA QUANTIDADE DE FUNCIONÁRIOS *********************************************************************
        public void Funcionarios(string search)
        {
            con2 = getConexaoBD();
            con2.Open();
            string query = @"SELECT COUNT(Employee.SbiID) AS [QUANTIDADE] FROM Employee
	                            LEFT JOIN CARD ON Employee.SbiID = Card.SbiID
                            WHERE
	                            Card.StateID LIKE '0' OR Card.StateID LIKE '1'";

            cmd2 = new SqlCommand(query, con2);

            SqlDataReader dtReader = cmd2.ExecuteReader();

            lblFuncionarios.Text = "";
            if (dtReader.HasRows)
            {
                while ((dtReader.Read()))
                {
                    lblFuncionarios.Text = dtReader["QUANTIDADE"].ToString();
                }
            }
        }

        //************************************************* CARREGA QUANTIDADE DE FUNCIONÁRIOS NA DATA *************************************************************
        public void FuncionariosHoje(string search)
        {

        }

        //************************************************* CARREGA QUANTIDADE DE TERCEIROS ************************************************************************
        public void Terceiros(string search)
        {
            con2 = getConexaoBD();
            con2.Open();
            string query = @"SELECT COUNT(ExternalRegular.SbiID) AS [QUANTIDADE] FROM ExternalRegular
	                            LEFT JOIN Card ON ExternalRegular.SbiID = Card.SbiID
                            WHERE
	                            Card.StateID LIKE '0' OR Card.StateID LIKE '1'";

            cmd2 = new SqlCommand(query, con2);

            SqlDataReader dtReader = cmd2.ExecuteReader();

            lblTerceiros.Text = "";
            if (dtReader.HasRows)
            {
                while ((dtReader.Read()))
                {
                    lblTerceiros.Text = dtReader["QUANTIDADE"].ToString();
                }
            }
        }

        //************************************************* CARREGA QUANTIDADE DE TERCEIROS NA DATA ****************************************************************
        public void TerceirosHoje(string search)
        {

        }

        //************************************************* CARREGA QUANTIDADE DE VISITANTES NA DATA ***************************************************************
        public void VisitantesHoje(string search)
        {
            con2 = getConexaoBD();
            con2.Open();
            string query = @"DECLARE
                                @ENTRADA INT,
                                @SAIDA INT,
                                @TOTAL INT

                            SET @ENTRADA = (SELECT COUNT(DISTINCT(Visitor.SbiID)) AS QUANTIDADE FROM Visitor left join HA_TRANSIT on Visitor.SbiID = HA_TRANSIT.SBI_ID
	                                        WHERE
												HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            HA_TRANSIT.TRANSIT_DATE BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59')

                            SET @SAIDA = (SELECT COUNT(DISTINCT(Visitor.SbiID)) AS QUANTIDADE FROM Visitor left join HA_TRANSIT on Visitor.SbiID = HA_TRANSIT.SBI_ID
	                                        WHERE
												HA_TRANSIT.STR_DIRECTION = 'Exit' AND
                                            HA_TRANSIT.TRANSIT_DATE BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59')

                            SET @TOTAL = @ENTRADA - @SAIDA

                            SELECT @TOTAL AS [QUANTIDADE]";

            cmd2 = new SqlCommand(query, con2);

            SqlDataReader dtReader = cmd2.ExecuteReader();

            lblVisitantes.Text = "";
            if (dtReader.HasRows)
            {
                while ((dtReader.Read()))
                {
                    lblVisitantes.Text = dtReader["QUANTIDADE"].ToString();
                }
            }
        }

        //************************************************* CARREGA CARTÕES ATIVOS *********************************************************************************
        public void CartoesAtivos()
        {
            con2 = getConexaoBD();
            con2.Open();
            string query = @"DECLARE
                            @FUNCIONARIOS INT,
                            @TERCEIROS INT,
                            @TOTAL INT

                            SET @FUNCIONARIOS = (SELECT COUNT(Employee.SbiID) AS [QUANTIDADE] FROM Employee
	                            LEFT JOIN CARD ON Employee.SbiID = Card.SbiID
                            WHERE
	                            Card.StateID LIKE '0' OR Card.StateID LIKE '1')
                            SET @TERCEIROS = (SELECT COUNT(ExternalRegular.SbiID) FROM ExternalRegular
	                            LEFT JOIN Card ON ExternalRegular.SbiID = Card.SbiID
                            WHERE
	                            Card.StateID LIKE '0' OR Card.StateID LIKE '1')
                            SET @TOTAL = @FUNCIONARIOS + @TERCEIROS
                            SELECT @TOTAL AS [QUANTIDADE]";

            cmd2 = new SqlCommand(query, con2);

            SqlDataReader dtReader = cmd2.ExecuteReader();

            lblAtivos.Text = "";
            if (dtReader.HasRows)
            {
                while ((dtReader.Read()))
                {
                    lblAtivos.Text = dtReader["QUANTIDADE"].ToString();
                }
            }
        }

        //************************************************* CARREGA CARTÕES INATIVOS *******************************************************************************
        public void CartoesInativos()
        {
            con2 = getConexaoBD();
            con2.Open();
            string query = @"DECLARE
                            @FUNCIONARIOS INT,
                            @TERCEIROS INT,
                            @TOTAL INT

                            SET @FUNCIONARIOS = (SELECT COUNT(Employee.SbiID) AS [QUANTIDADE] FROM Employee
	                            LEFT JOIN CARD ON Employee.SbiID = Card.SbiID
                            WHERE
	                            Card.StateID LIKE '3' OR Card.StateID LIKE '4' OR Card.StateID LIKE '5')
                            SET @TERCEIROS = (SELECT COUNT(ExternalRegular.SbiID) FROM ExternalRegular
	                            LEFT JOIN Card ON ExternalRegular.SbiID = Card.SbiID
                            WHERE
	                            Card.StateID LIKE '3' OR Card.StateID LIKE '4' OR Card.StateID LIKE '5')
                            SET @TOTAL = @FUNCIONARIOS + @TERCEIROS
                            SELECT @TOTAL AS [QUANTIDADE]";
            cmd2 = new SqlCommand(query, con2);

            SqlDataReader dtReader = cmd2.ExecuteReader();

            lblInativos.Text = "";
            if (dtReader.HasRows)
            {
                while ((dtReader.Read()))
                {
                    lblInativos.Text = dtReader["QUANTIDADE"].ToString();
                }
            }

            con2.Close();
            con2.Dispose();
            cmd2 = null;
        }

        //************************************************* CARREGA GRÁFICO COM FUNCIONÁRIOS ***********************************************************************
        public void CARREGA_GRAFICO_HorasFUNC()
        {
            con = getConexaoBD();
            con.Open();
            string query = @"DECLARE
                            @HORA0 INT,
                            @HORA1 INT,
                            @HORA2 INT,
                            @HORA3 INT,
                            @HORA4 INT,
                            @HORA5 INT,
                            @HORA6 INT,
                            @HORA7 INT,
                            @HORA8 INT,
                            @HORA9 INT,
                            @HORA10 INT,
                            @HORA11 INT,
                            @HORA12 INT,
                            @HORA13 INT,
                            @HORA14 INT,
                            @HORA15 INT,
                            @HORA16 INT,
                            @HORA17 INT,
                            @HORA18 INT,
                            @HORA19 INT,
                            @HORA20 INT,
                            @HORA21 INT,
                            @HORA22 INT,
                            @HORA23 INT,
                            @HORA24 INT

                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 00:00 */
                            SET @HORA0 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 00:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 01:00 */
                            SET @HORA1 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 01:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 01:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 02:00 */
                            SET @HORA2 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 02:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 02:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 03:00 */
                            SET @HORA3 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 03:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 03:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 04:00 */
                            SET @HORA4 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 04:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 04:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 05:00 */
                            SET @HORA5 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 05:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 05:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 06:00 */
                            SET @HORA6 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 06:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 06:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 07:00 */
                            SET @HORA7 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 07:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 07:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 08:00 */
                            SET @HORA8 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 08:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 08:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 09:00 */
                            SET @HORA9 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 09:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 09:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 10:00 */
                            SET @HORA10 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 10:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 10:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 11:00 */
                            SET @HORA11 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 11:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 11:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 12:00 */
                            SET @HORA12 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 12:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 12:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 13:00 */
                            SET @HORA13 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 13:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 13:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 14:00 */
                            SET @HORA14 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 14:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 14:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 15:00 */
                            SET @HORA15 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 15:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 15:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 16:00 */
                            SET @HORA16 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 16:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 16:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 17:00 */
                            SET @HORA17 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 17:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 17:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 18:00 */
                            SET @HORA18 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 18:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 18:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 19:00 */
                            SET @HORA19 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 19:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 19:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 20:00 */
                            SET @HORA20 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 20:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 20:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 21:00 */
                            SET @HORA21 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 21:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 21:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 22:00 */
                            SET @HORA22 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 22:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 22:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 23:00 */
                            SET @HORA23 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                            INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                                                        WHERE
	                                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' and
                                                        TRANSIT_DATE 
                                                        BETWEEN 
		                                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 23:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59')

                            SELECT	@HORA0 AS [00:00],@HORA1 AS [01:00],@HORA2 AS [02:00],@HORA3 AS [03:00],@HORA4 AS [04:00],@HORA5 AS [05:00], 
                                    @HORA6 AS [06:00],@HORA7 AS [07:00],@HORA8 AS [08:00],@HORA9 AS [09:00],@HORA10 AS [10:00], @HORA11 AS [11:00],
		                            @HORA12 AS [12:00],@HORA13 AS [13:00],@HORA14 AS [14:00],@HORA15 AS [15:00], @HORA16 AS [16:00],@HORA17 AS [17:00],
		                            @HORA18 AS [18:00],@HORA19 AS [19:00],@HORA20 AS [20:00],@HORA21 AS [21:00],@HORA22 AS [22:00],@HORA23 AS [23:00]";

            cmd = new SqlCommand(query, con);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            DgvHoras.DataSource = dt;

            Cpopulacao.Series[0].Points.Clear();

            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    int p = Cpopulacao.Series[i].Points.AddXY(DgvHoras.Columns[j].HeaderText, DgvHoras[j, i].Value);
                }
            }
            Cpopulacao.DataBind();

            //Cpopulacao.Series[0].Points.Clear();
            //for (int i = 0; i < dt.Rows.Count; i++)
            //{
            //    for (int j = 0; j < dt.Columns.Count; j++)
            //    { 
            //        Cpopulacao.Series[i].Points.AddXY(dt.Rows[i].ToString(),dt.Columns[j].ToString());
            //    }
            //}
            //Cpopulacao.DataBind();
        }

        //************************************************* CARREGA GRÁFICO COM TODOS ******************************************************************************
        public void VerificaHorasTODOS()
        {
            con = getConexaoBD();
            con.Open();
            string query = @"DECLARE
                            @HORA0 INT,
                            @HORA1 INT,
                            @HORA2 INT,
                            @HORA3 INT,
                            @HORA4 INT,
                            @HORA5 INT,
                            @HORA6 INT,
                            @HORA7 INT,
                            @HORA8 INT,
                            @HORA9 INT,
                            @HORA10 INT,
                            @HORA11 INT,
                            @HORA12 INT,
                            @HORA13 INT,
                            @HORA14 INT,
                            @HORA15 INT,
                            @HORA16 INT,
                            @HORA17 INT,
                            @HORA18 INT,
                            @HORA19 INT,
                            @HORA20 INT,
                            @HORA21 INT,
                            @HORA22 INT,
                            @HORA23 INT,
                            @HORA24 INT

                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 00:00 */
                            SET @HORA0 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 00:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 00:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 01:00 */
                            SET @HORA1 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 01:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 01:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 01:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 01:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 02:00 */
                            SET @HORA2 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 02:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 02:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 02:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 02:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 03:00 */
                            SET @HORA3 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 03:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 03:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 03:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 03:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 04:00 */
                            SET @HORA4 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 04:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 04:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 04:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 04:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 05:00 */
                            SET @HORA5 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 05:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 05:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 05:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 05:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 06:00 */
                            SET @HORA6 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 06:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 06:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 06:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 06:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 07:00 */
                            SET @HORA7 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 07:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 07:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 07:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 07:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 08:00 */
                            SET @HORA8 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 08:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 08:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 08:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 08:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 09:00 */
                            SET @HORA9 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 09:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 09:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 09:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 09:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 10:00 */
                            SET @HORA10 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 10:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 10:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 10:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 10:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 11:00 */
                            SET @HORA11 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 11:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 11:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 11:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 11:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 12:00 */
                            SET @HORA12 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 12:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 12:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 12:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 12:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 13:00 */
                            SET @HORA13 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 13:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 13:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 13:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 13:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 14:00 */
                            SET @HORA14 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 14:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 14:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 14:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 14:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 15:00 */
                            SET @HORA15 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 15:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 15:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 15:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 15:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 16:00 */
                            SET @HORA16 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 16:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 16:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 16:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 16:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 17:00 */
                            SET @HORA17 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 17:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 17:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 17:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 17:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 18:00 */
                            SET @HORA18 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 18:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 18:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 18:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 18:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 19:00 */
                            SET @HORA19 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 19:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 19:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 19:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 19:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 20:00 */
                            SET @HORA20 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 20:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 20:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 20:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 20:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 21:00 */
                            SET @HORA21 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 21:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 21:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 21:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 21:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 22:00 */
                            SET @HORA22 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 22:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 22:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 22:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 22:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 23:00 */
                            SET @HORA23 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
                                                LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE 
                                                BETWEEN 
		                                            CONVERT(CHAR(10), GETDATE(), 120) + ' 23:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59'
                                         UNION
                                         SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 23:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59')

                            SELECT	@HORA0 AS [00:00], @HORA1 AS [01:00], @HORA2 AS [02:00], @HORA3 AS [03:00], @HORA4 AS [04:00], @HORA5 AS [05:00], 
                                    @HORA6 AS [06:00], @HORA7 AS [07:00], @HORA8 AS [08:00], @HORA9 AS [09:00], @HORA10 AS [10:00], @HORA11 AS [11:00],
		                            @HORA12 AS [12:00], @HORA13 AS [13:00], @HORA14 AS [14:00], @HORA15 AS [15:00], @HORA16 AS [16:00], @HORA17 AS [17:00],
		                            @HORA18 AS [18:00], @HORA19 AS [19:00], @HORA20 AS [20:00], @HORA21 AS [21:00], @HORA22 AS [22:00], @HORA23 AS [23:00]";

            cmd = new SqlCommand(query, con);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            DgvHoras.DataSource = null;
            DgvHoras.DataSource = dt;

            Cpopulacao.Series[0].Points.Clear();

            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    int p = Cpopulacao.Series[i].Points.AddXY(DgvHoras.Columns[j].HeaderText, DgvHoras[j, i].Value);
                }
            }
            Cpopulacao.DataBind();
        }

        //************************************************* CARREGA GRÁFICO COM TERCEIROS **************************************************************************
        public void CARREGA_GRAFICO_HorasTERC()
        {
            con = getConexaoBD();
            con.Open();
            string query = @"DECLARE
                            @HORA0 INT,
                            @HORA1 INT,
                            @HORA2 INT,
                            @HORA3 INT,
                            @HORA4 INT,
                            @HORA5 INT,
                            @HORA6 INT,
                            @HORA7 INT,
                            @HORA8 INT,
                            @HORA9 INT,
                            @HORA10 INT,
                            @HORA11 INT,
                            @HORA12 INT,
                            @HORA13 INT,
                            @HORA14 INT,
                            @HORA15 INT,
                            @HORA16 INT,
                            @HORA17 INT,
                            @HORA18 INT,
                            @HORA19 INT,
                            @HORA20 INT,
                            @HORA21 INT,
                            @HORA22 INT,
                            @HORA23 INT

                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 00:00 */
                            SET @HORA0 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 00:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 01:00 */
                            SET @HORA1 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 01:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 01:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 02:00 */
                            SET @HORA2 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 02:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 02:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 03:00 */
                            SET @HORA3 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 03:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 03:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 04:00 */
                            SET @HORA4 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 04:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 04:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 05:00 */
                            SET @HORA5 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 05:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 05:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 06:00 */
                            SET @HORA6 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 06:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 06:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 07:00 */
                            SET @HORA7 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 07:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 07:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 08:00 */
                            SET @HORA8 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 08:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 08:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 09:00 */
                            SET @HORA9 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 09:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 09:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 10:00 */
                            SET @HORA10 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 10:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 10:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 11:00 */
                            SET @HORA11 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 11:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 11:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 12:00 */
                            SET @HORA12 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 12:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 12:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 13:00 */
                            SET @HORA13 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 13:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 13:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 14:00 */
                            SET @HORA14 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 14:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 14:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 15:00 */
                            SET @HORA15 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 15:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 15:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 16:00 */
                            SET @HORA16 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 16:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 16:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 17:00 */
                            SET @HORA17 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 17:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 17:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 18:00 */
                            SET @HORA18 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 18:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 18:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 19:00 */
                            SET @HORA19 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 19:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 19:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 20:00 */
                            SET @HORA20 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 20:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 20:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 21:00 */
                            SET @HORA21 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 21:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 21:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 22:00 */
                            SET @HORA22 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 22:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 22:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 23:00 */
                            SET @HORA23 = (SELECT COUNT(DISTINCT(ExternalRegularUserFields.SbiID)) FROM ExternalRegularUserFields
                                                LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                            WHERE
                                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                            TRANSIT_DATE BETWEEN
                                                CONVERT(CHAR(10), GETDATE(), 120) + ' 23:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) +' 23:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))

                            SELECT @HORA0 AS[00:00],@HORA1 AS[01:00],@HORA2 AS[02:00],@HORA3 AS[03:00],@HORA4 AS[04:00],@HORA5 AS[05:00], 
                                    @HORA6 AS[06:00],@HORA7 AS[07:00],@HORA8 AS[08:00],@HORA9 AS[09:00],@HORA10 AS[10:00], @HORA11 AS[11:00],
		                            @HORA12 AS[12:00],@HORA13 AS[13:00],@HORA14 AS[14:00],@HORA15 AS[15:00], @HORA16 AS[16:00],@HORA17 AS[17:00],
		                            @HORA18 AS[18:00],@HORA19 AS[19:00],@HORA20 AS[20:00],@HORA21 AS[21:00],@HORA22 AS[22:00],@HORA23 AS[23:00]";

            cmd = new SqlCommand(query, con);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            DgvHoras.DataSource = null;
            DgvHoras.DataSource = dt;

            Cpopulacao.Series[0].Points.Clear();

            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    int p = Cpopulacao.Series[i].Points.AddXY(DgvHoras.Columns[j].HeaderText, DgvHoras[j, i].Value);
                }
            }
            Cpopulacao.DataBind();
        }

        public void CARREGA_GRAFICO_HorasCOMBO_Terc()
        {
            con = getConexaoBD();
            con.Open();
            string query = @"DECLARE
                            @HORA0 INT,
                            @HORA1 INT,
                            @HORA2 INT,
                            @HORA3 INT,
                            @HORA4 INT,
                            @HORA5 INT,
                            @HORA6 INT,
                            @HORA7 INT,
                            @HORA8 INT,
                            @HORA9 INT,
                            @HORA10 INT,
                            @HORA11 INT,
                            @HORA12 INT,
                            @HORA13 INT,
                            @HORA14 INT,
                            @HORA15 INT,
                            @HORA16 INT,
                            @HORA17 INT,
                            @HORA18 INT,
                            @HORA19 INT,
                            @HORA20 INT,
                            @HORA21 INT,
                            @HORA22 INT,
                            @HORA23 INT

                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 00:00 */
                            SET @HORA0 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 00:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 01:00 */
                            SET @HORA1 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 01:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 01:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 02:00 */
                            SET @HORA2 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 02:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 02:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 03:00 */
                            SET @HORA3 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 03:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 03:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 04:00 */
                            SET @HORA4 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 04:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 04:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 05:00 */
                            SET @HORA5 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 05:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 05:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 06:00 */
                            SET @HORA6 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 06:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 06:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 07:00 */
                            SET @HORA7 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 07:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 07:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 08:00 */
                            SET @HORA8 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 08:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 08:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 09:00 */
                            SET @HORA9 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 09:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 09:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 10:00 */
                            SET @HORA10 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 10:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 10:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 11:00 */
                            SET @HORA11 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 11:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 11:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 12:00 */
                            SET @HORA12 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 12:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 12:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 13:00 */
                            SET @HORA13 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 13:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 13:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 14:00 */
                            SET @HORA14 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 14:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 14:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 15:00 */
                            SET @HORA15 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 15:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 15:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 16:00 */
                            SET @HORA16 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 16:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 16:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 17:00 */
                            SET @HORA17 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 17:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 17:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 18:00 */
                            SET @HORA18 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 18:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 18:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 19:00 */
                            SET @HORA19 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 19:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 19:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 20:00 */
                            SET @HORA20 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 20:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 20:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 21:00 */
                            SET @HORA21 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 21:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 21:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 22:00 */
                            SET @HORA22 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 22:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 22:59:59')
                            /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- 23:00 */
                            SET @HORA23 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                            WHERE
	                                            ExternalRegularUserFields.UF2 LIKE '" + DADOSGRIDVIEW + @"' AND
	                                            HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                            TRANSIT_DATE BETWEEN 
		                                        CONVERT(CHAR(10), GETDATE(), 120) + ' 23:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59')

                            SELECT	@HORA0 AS [00:00],@HORA1 AS [01:00],@HORA2 AS [02:00],@HORA3 AS [03:00],@HORA4 AS [04:00],@HORA5 AS [05:00], 
                                    @HORA6 AS [06:00],@HORA7 AS [07:00],@HORA8 AS [08:00],@HORA9 AS [09:00],@HORA10 AS [10:00], @HORA11 AS [11:00],
		                            @HORA12 AS [12:00],@HORA13 AS [13:00],@HORA14 AS [14:00],@HORA15 AS [15:00], @HORA16 AS [16:00],@HORA17 AS [17:00],
		                            @HORA18 AS [18:00],@HORA19 AS [19:00],@HORA20 AS [20:00],@HORA21 AS [21:00],@HORA22 AS [22:00],@HORA23 AS [23:00]";

            cmd = new SqlCommand(query, con);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            DgvHoras.DataSource = dt;

            Cpopulacao.Series[0].Points.Clear();

            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    int p = Cpopulacao.Series[i].Points.AddXY(DgvHoras.Columns[j].HeaderText, DgvHoras[j, i].Value);
                }
            }
            Cpopulacao.DataBind();
        }

        private void btnSair_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cboFiltro_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboFiltro.SelectedIndex != -1)
            {
                if (RdbFunc.Checked == true)
                {
                    Empresa = "";
                    Empresa = cboFiltro.SelectedItem.ToString();
                    CARREGA_GRAFICO_HorasFUNC();
                }
                else if (RdbTerc.Checked == true)
                {
                    Empresa = "";
                    Empresa = cboFiltro.SelectedItem.ToString();
                    CARREGA_GRAFICO_HorasTERC();

                }
                else if (RdbTodos.Checked == true)
                {
                    Empresa = "";
                    Empresa = cboFiltro.SelectedItem.ToString();
                    VerificaHorasTODOS();
                }
            }
        }

        private void RdbFunc_CheckedChanged(object sender, EventArgs e)
        {
            if (RdbFunc.Checked == true)
            {
                lblTotal.Text = "Funcionários";
            }

            FilterRecords("");

            DgvPopulacional.Sort(DgvPopulacional.Columns[0], ListSortDirection.Ascending);
            foreach (DataGridViewColumn column in DgvPopulacional.Columns)
            {
                if (column.DataPropertyName == "EMPRESA")
                    column.Width = 150;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            DgvPopulacional.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            DgvPopulacional.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            DgvPopulacional.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void RdbTerc_CheckedChanged(object sender, EventArgs e)
        {
            if (RdbTerc.Checked == true)
            {
                lblTotal.Text = "Terceiros";
                FilterRecords("");
            }

            DgvPopulacional.Sort(DgvPopulacional.Columns[0], ListSortDirection.Ascending);
            foreach (DataGridViewColumn column in DgvPopulacional.Columns)
            {
                if (column.DataPropertyName == "EMPRESA")
                    column.Width = 150;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            DgvPopulacional.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            DgvPopulacional.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            DgvPopulacional.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void RdbTodos_CheckedChanged(object sender, EventArgs e)
        {
            if (RdbTodos.Checked == true)
            {
                lblTotal.Text = "Funcionários | Terceiros";
                FilterRecords("");
            }

            DgvPopulacional.Sort(DgvPopulacional.Columns[0], ListSortDirection.Ascending);
            foreach (DataGridViewColumn column in DgvPopulacional.Columns)
            {
                if (column.DataPropertyName == "EMPRESA")
                    column.Width = 150;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            DgvPopulacional.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            DgvPopulacional.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            DgvPopulacional.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        public void VERIFICA_ENTRY_EXIT_FUNC()
        {
            con2 = getConexaoBD();
            con2.Open();

            con3 = getConexaoBD();
            con3.Open();

            string queryENTRADA = @"SELECT DBO.Employee.SbiID as [CODIGO] 
                                        FROM HA_TRANSIT LEFT JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE 
                                        HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                        DBO.Employee.SbiID != '' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')";
            string querySAIDA = @"SELECT DBO.Employee.SbiID as [CODIGO]
                                        FROM HA_TRANSIT LEFT JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE 
                                        HA_TRANSIT.STR_DIRECTION LIKE 'Exit' AND
                                        DBO.Employee.SbiID != '' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')";

            cmd2 = new SqlCommand(queryENTRADA, con2);
            cmd3 = new SqlCommand(querySAIDA, con3);
            SqlDataAdapter da1 = new SqlDataAdapter(cmd2);
            SqlDataAdapter da2 = new SqlDataAdapter(cmd3);
            DataTable dt1 = new DataTable();
            DataTable dt2 = new DataTable();
            da1.Fill(dt1);
            da2.Fill(dt2);

            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                for (int j = 0; j < dt2.Rows.Count; j++)
                {
                    if (Convert.ToInt32(dt1.Rows[i]["CODIGO"]) == Convert.ToInt32(dt2.Rows[j]["CODIGO"]))
                    {
                        dt1.Rows[i].Delete();
                        break;
                    }
                }
            }
            dt1.AcceptChanges();

            lblFuncHoje.Text = dt1.Rows.Count.ToString();
        }

        public void VERIFICA_ENTRY_EXIT_TERC()
        {
            con = getConexaoBD();
            con.Open();

            con2 = getConexaoBD();
            con2.Open();

            string queryENTRADA = @"SELECT DBO.ExternalRegular.SbiID AS [CODIGO], HA_TRANSIT.STR_DIRECTION AS [DIRECAO] 
                                        FROM HA_TRANSIT LEFT JOIN ExternalRegular ON ExternalRegular.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND DBO.ExternalRegular.SbiID != '' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')";
            string querySAIDA = @"SELECT DBO.ExternalRegular.SbiID AS [CODIGO], HA_TRANSIT.STR_DIRECTION AS [DIRECAO] 
                                        FROM HA_TRANSIT LEFT JOIN ExternalRegular ON ExternalRegular.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Exit' AND DBO.ExternalRegular.SbiID != '' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')";

            cmd = new SqlCommand(queryENTRADA, con);
            cmd2 = new SqlCommand(querySAIDA, con2);
            SqlDataAdapter da3 = new SqlDataAdapter(cmd);
            SqlDataAdapter da4 = new SqlDataAdapter(cmd2);
            DataTable dt3 = new DataTable();
            DataTable dt4 = new DataTable();
            da3.Fill(dt3);
            da4.Fill(dt4);

            for (int i = 0; i < dt3.Rows.Count; i++)
            {
                for (int j = 0; j < dt4.Rows.Count; j++)
                {
                    if (Convert.ToInt32(dt3.Rows[i]["CODIGO"]) == Convert.ToInt32(dt4.Rows[j]["CODIGO"]))
                    {
                        dt3.Rows[i].Delete();
                        break;
                    }
                }
            }
            dt3.AcceptChanges();

            lblTercHoje.Text = dt3.Rows.Count.ToString();
        }



        public void VERIFICA_ENTRY_EXIT_TODOS()
        {
            con2 = getConexaoBD();
            con2.Open();

            con3 = getConexaoBD();
            con3.Open();

            string queryENTRADA = @"SELECT DISTINCT(DBO.ExternalRegular.SbiID) AS [CODIGO] 
                                        FROM HA_TRANSIT INNER JOIN ExternalRegular ON ExternalRegular.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01') 
                                    UNION
                                    SELECT DISTINCT(DBO.Employee.SbiID) 
                                        FROM HA_TRANSIT INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B')";
            string querySAIDA = @"SELECT DISTINCT(DBO.ExternalRegular.SbiID) AS [CODIGO] 
                                        FROM HA_TRANSIT INNER JOIN ExternalRegular ON ExternalRegular.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Exit' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01')
                                    UNION
                                    SELECT DISTINCT(DBO.Employee.SbiID) 
                                        FROM HA_TRANSIT INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Exit' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B')";

            cmd2 = new SqlCommand(queryENTRADA, con2);
            cmd3 = new SqlCommand(querySAIDA, con3);
            SqlDataAdapter da1 = new SqlDataAdapter(cmd2);
            SqlDataAdapter da2 = new SqlDataAdapter(cmd3);
            DataTable dt1 = new DataTable();
            DataTable dt2 = new DataTable();
            da1.Fill(dt1);
            da2.Fill(dt2);

            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                for (int j = 0; j < dt2.Rows.Count; j++)
                {
                    if (Convert.ToInt32(dt1.Rows[i]["CODIGO"]) == Convert.ToInt32(dt2.Rows[j]["CODIGO"]))
                    {
                        dt1.Rows[i].Delete();
                        break;
                    }
                }
            }
            dt1.AcceptChanges();

            lblTotal1.Text = dt1.Rows.Count.ToString();
        }

        public void VERIFICA_ENTRY_EXIT()
        {
            con2 = getConexaoBD();
            con2.Open();

            con3 = getConexaoBD();
            con3.Open();

            string queryENTRADA = @"SELECT DBO.ExternalRegular.SbiID AS [CODIGO] 
                                        FROM HA_TRANSIT INNER JOIN ExternalRegular ON ExternalRegular.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01') 
                                    UNION
                                    SELECT DBO.Employee.SbiID
                                        FROM HA_TRANSIT INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Entry' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B')";
            string querySAIDA = @"SELECT DBO.ExternalRegular.SbiID AS [CODIGO] 
                                        FROM HA_TRANSIT INNER JOIN ExternalRegular ON ExternalRegular.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Exit' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01')
                                    UNION
                                    SELECT DBO.Employee.SbiID 
                                        FROM HA_TRANSIT INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
	                                WHERE HA_TRANSIT.STR_DIRECTION LIKE 'Exit' AND
                                        TRANSIT_DATE BETWEEN 
		                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:00' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B')";

            cmd2 = new SqlCommand(queryENTRADA, con2);
            cmd3 = new SqlCommand(querySAIDA, con3);
            SqlDataAdapter da1 = new SqlDataAdapter(cmd2);
            SqlDataAdapter da2 = new SqlDataAdapter(cmd3);
            DataTable dt1 = new DataTable();
            DataTable dt2 = new DataTable();
            da1.Fill(dt1);
            da2.Fill(dt2);

            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                for (int j = 0; j < dt2.Rows.Count; j++)
                {
                    if (Convert.ToInt32(dt1.Rows[i]["CODIGO"]) == Convert.ToInt32(dt2.Rows[j]["CODIGO"]))
                    {
                        dt1.Rows[i].Delete();
                        break;
                    }
                }
            }
            dt1.AcceptChanges();
        }

        public void FuncionariosChecked()
        {
            con2 = getConexaoBD();
            con2.Open();

            con3 = getConexaoBD();
            con3.Open();

            string queryENTRADA = @"DECLARE
                                    @CLARO1 INT,
                                    @CLARO2 INT,
                                    @CLARO3 INT,
                                    @CLAROTOTAL INT,
                                    @CLARONAME VARCHAR(MAX) = 'CLARO BRASIL'

                                    SET @CLARO1 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) 
				                    FROM HA_TRANSIT LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
				                    WHERE 
					                    HA_TRANSIT.STR_DIRECTION = 'Entry' AND
					                        EmployeeUserFields.UF2 IS NULL AND
				                    TRANSIT_DATE BETWEEN	
					                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))

                                    SET @CLARO2 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) 
				                    FROM HA_TRANSIT LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
				                    WHERE 
				                    HA_TRANSIT.STR_DIRECTION = 'Entry' AND
				                    EmployeeUserFields.UF2 = '' AND
				                    TRANSIT_DATE BETWEEN	
				                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))

                                    SET @CLARO3 = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) 
				                    FROM HA_TRANSIT LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
				                    WHERE 
				                    HA_TRANSIT.STR_DIRECTION = 'Entry' AND
				                    EmployeeUserFields.UF2 IS NOT NULL AND
				                    EmployeeUserFields.UF2 NOT LIKE '' AND
				                    TRANSIT_DATE BETWEEN	
				                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05'))

                                    SET @CLAROTOTAL = @CLARO1 + @CLARO2 + @CLARO3

                                    SELECT @CLARONAME AS [EMPRESA], @CLAROTOTAL AS [QUANTIDADE]";

            cmd2 = new SqlCommand(queryENTRADA, con2);

            SqlDataAdapter da1 = new SqlDataAdapter(cmd2);

            DataTable dt1 = new DataTable();

            da1.Fill(dt1);

            DgvPopulacional.DataSource = null;
            DgvPopulacional.DataSource = dt1;
        }

        public void TerceirosChecked()
        {
            con2 = getConexaoBD();
            con2.Open();

            con3 = getConexaoBD();
            con3.Open();

            string queryENTRADA = @"SELECT ExternalRegularUserFields.UF2 AS [EMPRESA], COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) AS [QUANTIDADE] 
                                    FROM ExternalRegularUserFields LEFT JOIN HA_TRANSIT ON HA_TRANSIT.SBI_ID = ExternalRegularUserFields.SbiID
                                    WHERE 
                                    HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                    TRANSIT_DATE BETWEEN	
                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')
                                    GROUP BY ExternalRegularUserFields.UF2";

            cmd2 = new SqlCommand(queryENTRADA, con2);

            SqlDataAdapter da1 = new SqlDataAdapter(cmd2);

            DataTable dt1 = new DataTable();

            da1.Fill(dt1);

            DgvPopulacional.DataSource = null;
            DgvPopulacional.DataSource = dt1;
        }

        public void TodosChecked()
        {
            con2 = getConexaoBD();
            con2.Open();

            con3 = getConexaoBD();
            con3.Open();

            string queryENTRADA = @"SELECT EmployeeUserFields.UF2 AS [EMPRESA], COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) AS [QUANTIDADE] 
                                    FROM HA_TRANSIT LEFT JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                    WHERE 
                                    HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                    TRANSIT_DATE BETWEEN	
                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='TK2ACA1A' OR TERMINAL='TK2ACA1B' OR TERMINAL='TK2ACA2A' OR TERMINAL='TK2ACA2B' OR TERMINAL='TK2ACA3A' OR TERMINAL='TK2ACA3B' OR TERMINAL='TK2ACA4A' OR TERMINAL='TK2ACA4B' OR TERMINAL='TK2BCA1A' OR TERMINAL='TK2BCA1B' OR TERMINAL='TK2BCA2A' OR TERMINAL='TK2BCA2B' OR TERMINAL='TK2BCA3A' OR TERMINAL='TK2BCA3B' OR TERMINAL='TK2BCA4A' OR TERMINAL='TK2BCA4B' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')
                                    GROUP BY EmployeeUserFields.UF2

                                    UNION

                                    SELECT ExternalRegularUserFields.UF2, COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) 
                                    FROM HA_TRANSIT LEFT JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT.SBI_ID
                                    WHERE 
                                    HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                    TRANSIT_DATE BETWEEN	
                                    CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' AND (TERMINAL='VDTB1S01' OR TERMINAL='VDTA1S01' OR TERMINAL='VDTA1S02' OR TERMINAL='VDTA1S03' OR TERMINAL='VDTA1S04' OR TERMINAL='VDTA1S05')
                                    GROUP BY ExternalRegularUserFields.UF2";

            cmd2 = new SqlCommand(queryENTRADA, con2);

            SqlDataAdapter da1 = new SqlDataAdapter(cmd2);

            DataTable dt1 = new DataTable();

            da1.Fill(dt1);

            DgvPopulacional.DataSource = null;
            DgvPopulacional.DataSource = dt1;
        }

        private void DgvPopulacional_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

            DADOSGRIDVIEW = "";
            DADOSGRIDVIEW = DgvPopulacional.CurrentCell.Value.ToString();

            CARREGA_GRAFICO_HorasCOMBO_Terc();
            cboFiltro.Text = DADOSGRIDVIEW;

        }
    }
}
