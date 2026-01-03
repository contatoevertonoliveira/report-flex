namespace WindowsFormsApp1
{
    partial class FrmConsultas
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmConsultas));
            this.GroupBox1 = new System.Windows.Forms.GroupBox();
            this.Label27 = new System.Windows.Forms.Label();
            this.Label26 = new System.Windows.Forms.Label();
            this.cboObter = new System.Windows.Forms.ComboBox();
            this.cboPesquisa = new System.Windows.Forms.ComboBox();
            this.gpbDescobrirCard = new System.Windows.Forms.GroupBox();
            this.cboDados = new System.Windows.Forms.ComboBox();
            this.btnBuscar = new System.Windows.Forms.Button();
            this.txtBusca = new System.Windows.Forms.TextBox();
            this.Label25 = new System.Windows.Forms.Label();
            this.Label24 = new System.Windows.Forms.Label();
            this.btnConsultar = new System.Windows.Forms.Button();
            this.PictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnSair = new System.Windows.Forms.Button();
            this.lblSQL = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label18 = new System.Windows.Forms.Label();
            this.txtPesquisa = new System.Windows.Forms.TextBox();
            this.txtDataFim = new System.Windows.Forms.MaskedTextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.txtDataInicio = new System.Windows.Forms.MaskedTextBox();
            this.lblMsg = new System.Windows.Forms.Label();
            this.Ativa = new System.Windows.Forms.RadioButton();
            this.Desativa = new System.Windows.Forms.RadioButton();
            this.btnFiltrar = new System.Windows.Forms.Button();
            //this.rptDados = new Microsoft.Reporting.WinForms.ReportViewer();
            this.bsDados = new System.Windows.Forms.BindingSource(this.components);
            this.bsCliente = new System.Windows.Forms.BindingSource(this.components);
            this.bsPrestador = new System.Windows.Forms.BindingSource(this.components);
            this.rdbAtivo = new System.Windows.Forms.RadioButton();
            this.rdbInativo = new System.Windows.Forms.RadioButton();
            this.rdbExpirado = new System.Windows.Forms.RadioButton();
            this.rdbInvalido = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.GroupBox1.SuspendLayout();
            this.gpbDescobrirCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bsDados)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bsCliente)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bsPrestador)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvDados
            // 
            this.dgvDados = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDados)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvDados
            // 
            this.dgvDados.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDados.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDados.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDados.Location = new System.Drawing.Point(22, 251);
            this.dgvDados.Name = "dgvDados";
            this.dgvDados.Size = new System.Drawing.Size(1136, 443);
            this.dgvDados.TabIndex = 15;
            //this.dgvDados.DataSource = this.bsDados;
            
            // 
            // GroupBox1
            // 
            this.GroupBox1.Controls.Add(this.Label27);
            this.GroupBox1.Controls.Add(this.Label26);
            this.GroupBox1.Controls.Add(this.cboObter);
            this.GroupBox1.Controls.Add(this.cboPesquisa);
            this.GroupBox1.Location = new System.Drawing.Point(308, 20);
            this.GroupBox1.Name = "GroupBox1";
            this.GroupBox1.Size = new System.Drawing.Size(622, 71);
            this.GroupBox1.TabIndex = 19;
            this.GroupBox1.TabStop = false;
            this.GroupBox1.Text = "Defina a forma de consulta:";
            // 
            // Label27
            // 
            this.Label27.AutoSize = true;
            this.Label27.Location = new System.Drawing.Point(310, 17);
            this.Label27.Name = "Label27";
            this.Label27.Size = new System.Drawing.Size(36, 13);
            this.Label27.TabIndex = 3;
            this.Label27.Text = "Obter:";
            // 
            // Label26
            // 
            this.Label26.AutoSize = true;
            this.Label26.Location = new System.Drawing.Point(18, 20);
            this.Label26.Name = "Label26";
            this.Label26.Size = new System.Drawing.Size(116, 13);
            this.Label26.TabIndex = 2;
            this.Label26.Text = "Efetuar a pesquisa por:";
            // 
            // cboObter
            // 
            this.cboObter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboObter.FormattingEnabled = true;
            this.cboObter.Location = new System.Drawing.Point(313, 33);
            this.cboObter.Name = "cboObter";
            this.cboObter.Size = new System.Drawing.Size(285, 21);
            this.cboObter.TabIndex = 1;
            this.cboObter.SelectedIndexChanged += new System.EventHandler(this.cboObter_SelectedIndexChanged);
            // 
            // cboPesquisa
            // 
            this.cboPesquisa.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPesquisa.FormattingEnabled = true;
            this.cboPesquisa.Location = new System.Drawing.Point(21, 33);
            this.cboPesquisa.Name = "cboPesquisa";
            this.cboPesquisa.Size = new System.Drawing.Size(286, 21);
            this.cboPesquisa.TabIndex = 0;
            this.cboPesquisa.SelectedIndexChanged += new System.EventHandler(this.cboPesquisa_SelectedIndexChanged);
            // 
            // gpbDescobrirCard
            // 
            this.gpbDescobrirCard.Controls.Add(this.lblStatus);
            this.gpbDescobrirCard.Controls.Add(this.cboDados);
            this.gpbDescobrirCard.Controls.Add(this.btnBuscar);
            this.gpbDescobrirCard.Controls.Add(this.txtBusca);
            this.gpbDescobrirCard.Controls.Add(this.Label25);
            this.gpbDescobrirCard.Controls.Add(this.Label24);
            this.gpbDescobrirCard.Location = new System.Drawing.Point(936, 20);
            this.gpbDescobrirCard.Name = "gpbDescobrirCard";
            this.gpbDescobrirCard.Size = new System.Drawing.Size(224, 195);
            this.gpbDescobrirCard.TabIndex = 27;
            this.gpbDescobrirCard.TabStop = false;
            this.gpbDescobrirCard.Text = "Descobrir crachá de acesso:";
            // 
            // cboDados
            // 
            this.cboDados.BackColor = System.Drawing.Color.ForestGreen;
            this.cboDados.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDados.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboDados.FormattingEnabled = true;
            this.cboDados.Location = new System.Drawing.Point(30, 156);
            this.cboDados.Name = "cboDados";
            this.cboDados.Size = new System.Drawing.Size(168, 24);
            this.cboDados.TabIndex = 3;
            this.cboDados.SelectedIndexChanged += new System.EventHandler(this.cboDados_SelectedIndexChanged);
            this.cboDados.Click += new System.EventHandler(this.cboDados_Click);
            // 
            // btnBuscar
            // 
            this.btnBuscar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnBuscar.Enabled = false;
            this.btnBuscar.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBuscar.ForeColor = System.Drawing.Color.White;
            this.btnBuscar.Location = new System.Drawing.Point(30, 79);
            this.btnBuscar.Name = "btnBuscar";
            this.btnBuscar.Size = new System.Drawing.Size(168, 40);
            this.btnBuscar.TabIndex = 4;
            this.btnBuscar.Text = "Buscar Crachá";
            this.btnBuscar.UseVisualStyleBackColor = false;
            this.btnBuscar.Click += new System.EventHandler(this.btnBuscar_Click);
            this.btnBuscar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnBuscar_MouseDown);
            this.btnBuscar.MouseEnter += new System.EventHandler(this.btnBuscar_MouseEnter);
            this.btnBuscar.MouseLeave += new System.EventHandler(this.btnBuscar_MouseLeave);
            // 
            // txtBusca
            // 
            this.txtBusca.Location = new System.Drawing.Point(30, 53);
            this.txtBusca.MaxLength = 11;
            this.txtBusca.Name = "txtBusca";
            this.txtBusca.Size = new System.Drawing.Size(168, 20);
            this.txtBusca.TabIndex = 2;
            this.txtBusca.TextChanged += new System.EventHandler(this.txtBusca_TextChanged);
            this.txtBusca.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtBusca_KeyPress);
            // 
            // Label25
            // 
            this.Label25.AutoSize = true;
            this.Label25.ForeColor = System.Drawing.Color.Maroon;
            this.Label25.Location = new System.Drawing.Point(27, 36);
            this.Label25.Name = "Label25";
            this.Label25.Size = new System.Drawing.Size(100, 13);
            this.Label25.TabIndex = 2;
            this.Label25.Text = "Consultar pelo CPF:";
            // 
            // Label24
            // 
            this.Label24.AutoSize = true;
            this.Label24.Location = new System.Drawing.Point(27, 134);
            this.Label24.Name = "Label24";
            this.Label24.Size = new System.Drawing.Size(44, 13);
            this.Label24.TabIndex = 1;
            this.Label24.Text = "Crachá:";
            // 
            // btnConsultar
            // 
            this.btnConsultar.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnConsultar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnConsultar.Enabled = false;
            this.btnConsultar.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnConsultar.ForeColor = System.Drawing.Color.White;
            this.btnConsultar.Location = new System.Drawing.Point(8, 11);
            this.btnConsultar.Name = "btnConsultar";
            this.btnConsultar.Size = new System.Drawing.Size(149, 64);
            this.btnConsultar.TabIndex = 10;
            this.btnConsultar.Text = "Efetuar a consulta";
            this.btnConsultar.UseVisualStyleBackColor = false;
            this.btnConsultar.Click += new System.EventHandler(this.btnConsultar_Click);
            this.btnConsultar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnConsultar_MouseDown);
            this.btnConsultar.MouseEnter += new System.EventHandler(this.btnConsultar_MouseEnter);
            this.btnConsultar.MouseLeave += new System.EventHandler(this.btnConsultar_MouseLeave);
            // 
            // PictureBox1
            // 
            this.PictureBox1.ErrorImage = ((System.Drawing.Image)(resources.GetObject("PictureBox1.ErrorImage")));
            this.PictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("PictureBox1.Image")));
            this.PictureBox1.InitialImage = ((System.Drawing.Image)(resources.GetObject("PictureBox1.InitialImage")));
            this.PictureBox1.Location = new System.Drawing.Point(15, -2);
            this.PictureBox1.Name = "PictureBox1";
            this.PictureBox1.Size = new System.Drawing.Size(269, 108);
            this.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.PictureBox1.TabIndex = 24;
            this.PictureBox1.TabStop = false;
            // 
            // btnSair
            // 
            this.btnSair.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSair.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSair.Location = new System.Drawing.Point(938, 700);
            this.btnSair.Name = "btnSair";
            this.btnSair.Size = new System.Drawing.Size(218, 50);
            this.btnSair.TabIndex = 17;
            this.btnSair.Text = "Sair";
            this.btnSair.UseVisualStyleBackColor = true;
            this.btnSair.Click += new System.EventHandler(this.btnSair_Click);
            // 
            // lblSQL
            // 
            this.lblSQL.AutoSize = true;
            this.lblSQL.Location = new System.Drawing.Point(17, 235);
            this.lblSQL.Name = "lblSQL";
            this.lblSQL.Size = new System.Drawing.Size(140, 13);
            this.lblSQL.TabIndex = 20;
            this.lblSQL.Text = "Resultado da consulta SQL:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.groupBox4);
            this.groupBox2.Controls.Add(this.groupBox3);
            this.groupBox2.Location = new System.Drawing.Point(20, 97);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(910, 118);
            this.groupBox2.TabIndex = 28;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Parâmetros:";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.btnConsultar);
            this.groupBox4.Location = new System.Drawing.Point(730, 20);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(164, 80);
            this.groupBox4.TabIndex = 1;
            this.groupBox4.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label18);
            this.groupBox3.Controls.Add(this.txtPesquisa);
            this.groupBox3.Controls.Add(this.txtDataFim);
            this.groupBox3.Controls.Add(this.label17);
            this.groupBox3.Controls.Add(this.txtDataInicio);
            this.groupBox3.Controls.Add(this.lblMsg);
            this.groupBox3.Controls.Add(this.Ativa);
            this.groupBox3.Controls.Add(this.Desativa);
            this.groupBox3.Location = new System.Drawing.Point(14, 20);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(710, 80);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(567, 28);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(65, 13);
            this.label18.TabIndex = 5;
            this.label18.Text = "Data e Hora";
            // 
            // txtPesquisa
            // 
            this.txtPesquisa.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtPesquisa.Enabled = false;
            this.txtPesquisa.Location = new System.Drawing.Point(19, 44);
            this.txtPesquisa.Name = "txtPesquisa";
            this.txtPesquisa.Size = new System.Drawing.Size(281, 20);
            this.txtPesquisa.TabIndex = 5;
            // 
            // txtDataFim
            // 
            this.txtDataFim.Enabled = false;
            this.txtDataFim.Location = new System.Drawing.Point(569, 43);
            this.txtDataFim.Mask = "0000/00/00 00:00:00";
            this.txtDataFim.Name = "txtDataFim";
            this.txtDataFim.Size = new System.Drawing.Size(123, 20);
            this.txtDataFim.TabIndex = 9;
            this.txtDataFim.ValidatingType = typeof(System.DateTime);
            this.txtDataFim.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtDataFim_KeyPress);
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(428, 28);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(65, 13);
            this.label17.TabIndex = 4;
            this.label17.Text = "Data e Hora";
            // 
            // txtDataInicio
            // 
            this.txtDataInicio.Enabled = false;
            this.txtDataInicio.Location = new System.Drawing.Point(430, 43);
            this.txtDataInicio.Mask = "0000/00/00 00:00:00";
            this.txtDataInicio.Name = "txtDataInicio";
            this.txtDataInicio.Size = new System.Drawing.Size(123, 20);
            this.txtDataInicio.TabIndex = 8;
            this.txtDataInicio.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtDataInicio_KeyPress);
            // 
            // lblMsg
            // 
            this.lblMsg.AutoSize = true;
            this.lblMsg.Location = new System.Drawing.Point(17, 25);
            this.lblMsg.Name = "lblMsg";
            this.lblMsg.Size = new System.Drawing.Size(56, 13);
            this.lblMsg.TabIndex = 0;
            this.lblMsg.Text = "Pesquisar:";
            // 
            // Ativa
            // 
            this.Ativa.AutoSize = true;
            this.Ativa.Enabled = false;
            this.Ativa.Location = new System.Drawing.Point(321, 25);
            this.Ativa.Name = "Ativa";
            this.Ativa.Size = new System.Drawing.Size(78, 17);
            this.Ativa.TabIndex = 6;
            this.Ativa.TabStop = true;
            this.Ativa.Text = "Ativa datas";
            this.Ativa.UseVisualStyleBackColor = true;
            this.Ativa.CheckedChanged += new System.EventHandler(this.Ativa_CheckedChanged);
            // 
            // Desativa
            // 
            this.Desativa.AutoSize = true;
            this.Desativa.Enabled = false;
            this.Desativa.Location = new System.Drawing.Point(321, 48);
            this.Desativa.Name = "Desativa";
            this.Desativa.Size = new System.Drawing.Size(67, 17);
            this.Desativa.TabIndex = 7;
            this.Desativa.TabStop = true;
            this.Desativa.Text = "Desativa";
            this.Desativa.UseVisualStyleBackColor = true;
            this.Desativa.CheckedChanged += new System.EventHandler(this.Desativa_CheckedChanged);
            // 
            // btnFiltrar
            // 
            this.btnFiltrar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnFiltrar.Location = new System.Drawing.Point(20, 700);
            this.btnFiltrar.Name = "btnFiltrar";
            this.btnFiltrar.Size = new System.Drawing.Size(218, 50);
            this.btnFiltrar.TabIndex = 16;
            this.btnFiltrar.Text = "Filtrar Dados";
            this.btnFiltrar.UseVisualStyleBackColor = true;
            this.btnFiltrar.Click += new System.EventHandler(this.btnFiltrar_Click);
            // 
            // rptDados
            // 
            /*this.rptDados.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rptDados.LocalReport.EnableExternalImages = true;
            this.rptDados.Location = new System.Drawing.Point(20, 251);
            this.rptDados.Name = "rptDados";
            this.rptDados.Size = new System.Drawing.Size(1136, 443);
            this.rptDados.TabIndex = 15;*/
            // 
            // bsDados
            // 
            // this.bsDados.CurrentChanged += new System.EventHandler(this.bsDados_CurrentChanged);
            // 
            // rdbAtivo
            // 
            this.rdbAtivo.AutoSize = true;
            this.rdbAtivo.Enabled = false;
            this.rdbAtivo.ForeColor = System.Drawing.Color.DarkRed;
            this.rdbAtivo.Location = new System.Drawing.Point(919, 233);
            this.rdbAtivo.Name = "rdbAtivo";
            this.rdbAtivo.Size = new System.Drawing.Size(49, 17);
            this.rdbAtivo.TabIndex = 11;
            this.rdbAtivo.TabStop = true;
            this.rdbAtivo.Text = "Ativo";
            this.rdbAtivo.UseVisualStyleBackColor = true;
            this.rdbAtivo.CheckedChanged += new System.EventHandler(this.rdbAtivo_CheckedChanged);
            // 
            // rdbInativo
            // 
            this.rdbInativo.AutoSize = true;
            this.rdbInativo.Enabled = false;
            this.rdbInativo.ForeColor = System.Drawing.Color.DarkRed;
            this.rdbInativo.Location = new System.Drawing.Point(971, 233);
            this.rdbInativo.Name = "rdbInativo";
            this.rdbInativo.Size = new System.Drawing.Size(57, 17);
            this.rdbInativo.TabIndex = 12;
            this.rdbInativo.TabStop = true;
            this.rdbInativo.Text = "Inativo";
            this.rdbInativo.UseVisualStyleBackColor = true;
            this.rdbInativo.CheckedChanged += new System.EventHandler(this.rdbInativo_CheckedChanged);
            // 
            // rdbExpirado
            // 
            this.rdbExpirado.AutoSize = true;
            this.rdbExpirado.Enabled = false;
            this.rdbExpirado.ForeColor = System.Drawing.Color.DarkRed;
            this.rdbExpirado.Location = new System.Drawing.Point(1031, 233);
            this.rdbExpirado.Name = "rdbExpirado";
            this.rdbExpirado.Size = new System.Drawing.Size(66, 17);
            this.rdbExpirado.TabIndex = 13;
            this.rdbExpirado.TabStop = true;
            this.rdbExpirado.Text = "Expirado";
            this.rdbExpirado.UseVisualStyleBackColor = true;
            this.rdbExpirado.CheckedChanged += new System.EventHandler(this.rdbExpirado_CheckedChanged);
            // 
            // rdbInvalido
            // 
            this.rdbInvalido.AutoSize = true;
            this.rdbInvalido.Enabled = false;
            this.rdbInvalido.ForeColor = System.Drawing.Color.DarkRed;
            this.rdbInvalido.Location = new System.Drawing.Point(1100, 233);
            this.rdbInvalido.Name = "rdbInvalido";
            this.rdbInvalido.Size = new System.Drawing.Size(62, 17);
            this.rdbInvalido.TabIndex = 14;
            this.rdbInvalido.TabStop = true;
            this.rdbInvalido.Text = "Inválido";
            this.rdbInvalido.UseVisualStyleBackColor = true;
            this.rdbInvalido.CheckedChanged += new System.EventHandler(this.rdbInvalido_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.DarkRed;
            this.label1.Location = new System.Drawing.Point(839, 235);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 34;
            this.label1.Text = "Filtrar por  --->>";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.Color.ForestGreen;
            this.lblStatus.Location = new System.Drawing.Point(137, 139);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(61, 14);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "Sucesso!";
            this.lblStatus.Visible = false;
            // 
            // FrmConsultas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1180, 762);
            this.Controls.Add(this.dgvDados);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.rdbInvalido);
            this.Controls.Add(this.rdbExpirado);
            this.Controls.Add(this.rdbInativo);
            this.Controls.Add(this.rdbAtivo);
            this.Controls.Add(this.btnFiltrar);
            //this.Controls.Add(this.rptDados);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.GroupBox1);
            this.Controls.Add(this.gpbDescobrirCard);
            this.Controls.Add(this.btnSair);
            this.Controls.Add(this.PictureBox1);
            this.Controls.Add(this.lblSQL);
            this.KeyPreview = true;
            this.Name = "FrmConsultas";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Consultas";
            this.Load += new System.EventHandler(this.FrmConsultas_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmConsultas_KeyDown);
            this.GroupBox1.ResumeLayout(false);
            this.GroupBox1.PerformLayout();
            this.gpbDescobrirCard.ResumeLayout(false);
            this.gpbDescobrirCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bsDados)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bsCliente)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bsPrestador)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDados)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        internal System.Windows.Forms.Label Label27;
        internal System.Windows.Forms.Label Label26;
        internal System.Windows.Forms.ComboBox cboObter;
        internal System.Windows.Forms.ComboBox cboPesquisa;
        internal System.Windows.Forms.GroupBox gpbDescobrirCard;
        internal System.Windows.Forms.Button btnBuscar;
        internal System.Windows.Forms.TextBox txtBusca;
        internal System.Windows.Forms.Label Label25;
        internal System.Windows.Forms.Label Label24;
        internal System.Windows.Forms.Button btnConsultar;
        internal System.Windows.Forms.PictureBox PictureBox1;
        internal System.Windows.Forms.Button btnSair;
        internal System.Windows.Forms.Label lblSQL;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.RadioButton Desativa;
        private System.Windows.Forms.RadioButton Ativa;
        private System.Windows.Forms.TextBox txtPesquisa;
        private System.Windows.Forms.Label lblMsg;
        private System.Windows.Forms.ComboBox cboDados;
        //private Microsoft.Reporting.WinForms.ReportViewer rptDados;
        private System.Windows.Forms.BindingSource bsDados;
        private System.Windows.Forms.Button btnFiltrar;
        private System.Windows.Forms.BindingSource bsCliente;
        private System.Windows.Forms.BindingSource bsPrestador;
        public System.Windows.Forms.GroupBox GroupBox1;
        public System.Windows.Forms.GroupBox groupBox3;
        public System.Windows.Forms.MaskedTextBox txtDataFim;
        public System.Windows.Forms.MaskedTextBox txtDataInicio;
        private System.Windows.Forms.RadioButton rdbAtivo;
        private System.Windows.Forms.RadioButton rdbInativo;
        private System.Windows.Forms.RadioButton rdbExpirado;
        private System.Windows.Forms.RadioButton rdbInvalido;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataGridView dgvDados;
    }
}