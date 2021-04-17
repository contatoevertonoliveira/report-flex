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
    public partial class FrmPopulacional : Form
    {
        public FrmPopulacional()
        {
            InitializeComponent();
        }

        private SqlConnection con = null;
        private SqlCommand cmd = null;
        private SqlConnection con2 = null;
        private SqlCommand cmd2 = null;
        private SqlConnection con3 = null;
        private SqlCommand cmd3 = null;

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

        private void FrmPopulacional_Load(object sender, EventArgs e)
        {
            FilterRecords("");
            FilterRecords2("");
            CartoesAtivos();
            CartoesInativos();
            Funcionarios("");
            Terceiros("");
            FuncionariosHoje("");
            TerceirosHoje("");
            VisitantesHoje("");
            DgvPopulacional.Sort(DgvPopulacional.Columns[0], ListSortDirection.Ascending);
            VerificaHoras();
        }

        public void FilterRecords(string search)
        {
            con = getConexaoBD();
            con.Open();
            string query = @"SELECT EmployeeUserFields.UF2 AS [EMPRESA], COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) AS [QUANTIDADE] FROM HA_TRANSIT 
                                INNER JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                            WHERE
	                            HA_TRANSIT.STR_DIRECTION = 'Entry' and
                            TRANSIT_DATE 
                            BETWEEN 
		                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' GROUP BY EmployeeUserFields.UF2
                        UNION
                            SELECT ExternalRegularUserFields.UF2, COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT 
	                            INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                            WHERE
	                            HA_TRANSIT.STR_DIRECTION = 'Entry'
                            AND
                            TRANSIT_DATE BETWEEN 
		                        CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59' GROUP BY ExternalRegularUserFields.UF2";

            cmd = new SqlCommand(query, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            DgvPopulacional.DataSource = dt;
        }

        public void FilterRecords2(string search)
        {
            con3 = getConexaoBD();
            con3.Open();
            string query = @"DECLARE
                            @FUNCIONARIOS INT,
                            @TERCEIROS INT,
                            @TOTAL INT

                            SET @FUNCIONARIOS = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT INNER JOIN EmployeeUserFields ON EmployeeUserFields.SbiID = HA_TRANSIT.SBI_ID
                                                 WHERE
						                            HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                                    TRANSIT_DATE 
                                                        BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59')
 
                            SET @TERCEIROS = (SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) FROM HA_TRANSIT INNER JOIN ExternalRegularUserFields ON ExternalRegularUserFields.SbiID = HA_TRANSIT .SBI_ID
                                                WHERE
					                                HA_TRANSIT.STR_DIRECTION = 'Entry' AND
                                                    TRANSIT_DATE 
							                            BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59')
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

        public void FuncionariosHoje(string search)
        {
            con2 = getConexaoBD();
            con2.Open();
            string query = @"SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) AS [QUANTIDADE] FROM HA_TRANSIT 
                                INNER JOIN Employee ON Employee.SbiID = HA_TRANSIT.SBI_ID
                            WHERE
	                            HA_TRANSIT.STR_DIRECTION = 'Entry' and
                            TRANSIT_DATE 
                                BETWEEN 
		                            CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59'";

            cmd2 = new SqlCommand(query, con2);

            SqlDataReader dtReader = cmd2.ExecuteReader();

            lblFuncHoje.Text = "";
            if (dtReader.HasRows)
            {
                while ((dtReader.Read()))
                {
                    lblFuncHoje.Text = dtReader["QUANTIDADE"].ToString();
                }
            }
        }


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

        public void TerceirosHoje(string search)
        {
            con2 = getConexaoBD();
            con2.Open();
            string query = @"SELECT COUNT(DISTINCT(DBO.HA_TRANSIT.SBI_ID)) AS [QUANTIDADE] FROM HA_TRANSIT 
	                            INNER JOIN ExternalRegular ON ExternalRegular.SbiID = HA_TRANSIT .SBI_ID
                            WHERE
	                            HA_TRANSIT.STR_DIRECTION = 'Entry'
                            AND
                                TRANSIT_DATE BETWEEN 
		                            CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59'";

            cmd2 = new SqlCommand(query, con2);

            SqlDataReader dtReader = cmd2.ExecuteReader();

            lblTercHoje.Text = "";
            if (dtReader.HasRows)
            {
                while ((dtReader.Read()))
                {
                    lblTercHoje.Text = dtReader["QUANTIDADE"].ToString();
                }
            }
        }

        public void VisitantesHoje(string search)
        {
            con2 = getConexaoBD();
            con2.Open();
            string query = @"SELECT COUNT(DISTINCT(HA_VISIT.SBI_ID)) AS [QUANTIDADE] FROM HA_VISIT
                                WHERE
	                                HA_VISIT.VISIT_START BETWEEN CONVERT(CHAR(10), GETDATE(), 120) + ' 00:00:01' AND CONVERT(CHAR(10), GETDATE(), 120) + ' 23:59:59'";

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

        public void VerificaHoras()
        {
                con = getConexaoBD();
                con.Open();
                string query = @"
DECLARE
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
		@HORA6 AS [06:00],@HORA7 AS [07:00],@HORA8 AS [08:00],@HORA9 AS [09:00],@HORA10 AS [10:00],@HORA11 AS [11:00],
		@HORA12 AS [12:00],@HORA13 AS [13:00],@HORA14 AS [14:00],@HORA15 AS [15:00],@HORA16 AS [16:00],@HORA17 AS [17:00],
		@HORA18 AS [18:00],@HORA19 AS [19:00],@HORA20 AS [20:00],@HORA21 AS [21:00],@HORA22 AS [22:00],@HORA23 AS [23:00]";

            cmd = new SqlCommand(query, con);
            //SqlDataReader dtReader = cmd.ExecuteReader();

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            DgvHoras.DataSource = dt;

            //if (dtReader.HasRows)
            //{
            //    while ((dtReader.Read()))
            //    {
            //        cPopulacao.Series["População"].ChartType = SeriesChartType.Column;
            //        cPopulacao.Series["População"].Points.AddY(5);
            //        cPopulacao.Series["População"].ChartArea = "ChartArea1";
            //    }
            //}
            cPopulacao.Series.Clear();
            cPopulacao.DataBindTable(((DataTable)DgvHoras.DataSource).AsDataView());
            cPopulacao.Series.Add("Populacional");
            cPopulacao.Series["Populacional"]["PointWidth"] = "1";



        }
    }
}
