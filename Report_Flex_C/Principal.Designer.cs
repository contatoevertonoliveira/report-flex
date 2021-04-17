namespace WindowsFormsApp1
{
    partial class frmPrincipal
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmPrincipal));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.btnConectar = new System.Windows.Forms.ToolStripMenuItem();
            this.btnConsultar = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCadastro = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCabecalho = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.btnSair = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.btnBanco = new System.Windows.Forms.ToolStripMenuItem();
            this.btnDefinicao = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.btnContent = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.btnSobre = new System.Windows.Forms.ToolStripMenuItem();
            this.finalizarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLogoff = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnConectar1 = new System.Windows.Forms.ToolStripButton();
            this.btnCadastro1 = new System.Windows.Forms.ToolStripButton();
            this.btnCabecalho1 = new System.Windows.Forms.ToolStripButton();
            this.btnConsultar1 = new System.Windows.Forms.ToolStripButton();
            this.btnBanco1 = new System.Windows.Forms.ToolStripButton();
            this.btnDefinicao1 = new System.Windows.Forms.ToolStripButton();
            this.btnLogoff1 = new System.Windows.Forms.ToolStripButton();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblConexao = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblNivel = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtContador = new System.Windows.Forms.TextBox();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.btnPopulacao = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.toolsMenu,
            this.helpMenu,
            this.finalizarToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(800, 24);
            this.menuStrip.TabIndex = 1;
            this.menuStrip.Text = "MenuStrip";
            // 
            // fileMenu
            // 
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnConectar,
            this.btnConsultar,
            this.btnCadastro,
            this.btnCabecalho,
            this.toolStripSeparator3,
            this.btnSair});
            this.fileMenu.ImageTransparentColor = System.Drawing.SystemColors.ActiveBorder;
            this.fileMenu.Name = "fileMenu";
            this.fileMenu.Size = new System.Drawing.Size(61, 20);
            this.fileMenu.Text = "&Arquivo";
            // 
            // btnConectar
            // 
            this.btnConectar.Image = global::WindowsFormsApp1.Properties.Resources.Key;
            this.btnConectar.Name = "btnConectar";
            this.btnConectar.Size = new System.Drawing.Size(258, 22);
            this.btnConectar.Text = "Con&ectar";
            this.btnConectar.Click += new System.EventHandler(this.btnConectar_Click);
            // 
            // btnConsultar
            // 
            this.btnConsultar.Enabled = false;
            this.btnConsultar.Image = ((System.Drawing.Image)(resources.GetObject("btnConsultar.Image")));
            this.btnConsultar.ImageTransparentColor = System.Drawing.Color.Black;
            this.btnConsultar.Name = "btnConsultar";
            this.btnConsultar.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.btnConsultar.Size = new System.Drawing.Size(258, 22);
            this.btnConsultar.Text = "&Consultar";
            this.btnConsultar.Click += new System.EventHandler(this.btnConsultar_Click);
            // 
            // btnCadastro
            // 
            this.btnCadastro.Enabled = false;
            this.btnCadastro.Image = ((System.Drawing.Image)(resources.GetObject("btnCadastro.Image")));
            this.btnCadastro.ImageTransparentColor = System.Drawing.Color.Black;
            this.btnCadastro.Name = "btnCadastro";
            this.btnCadastro.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
            this.btnCadastro.Size = new System.Drawing.Size(258, 22);
            this.btnCadastro.Text = "Cadastro de Usu&ários";
            this.btnCadastro.Click += new System.EventHandler(this.btnCadastro_Click);
            // 
            // btnCabecalho
            // 
            this.btnCabecalho.Enabled = false;
            this.btnCabecalho.Image = ((System.Drawing.Image)(resources.GetObject("btnCabecalho.Image")));
            this.btnCabecalho.Name = "btnCabecalho";
            this.btnCabecalho.Size = new System.Drawing.Size(258, 22);
            this.btnCabecalho.Text = "Cadastro de Cabeçalhos e Rodap&és";
            this.btnCabecalho.Click += new System.EventHandler(this.btnCabecalho_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(255, 6);
            // 
            // btnSair
            // 
            this.btnSair.Image = ((System.Drawing.Image)(resources.GetObject("btnSair.Image")));
            this.btnSair.Name = "btnSair";
            this.btnSair.Size = new System.Drawing.Size(258, 22);
            this.btnSair.Text = "Sai&r";
            this.btnSair.Click += new System.EventHandler(this.btnSair_Click);
            // 
            // toolsMenu
            // 
            this.toolsMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnBanco,
            this.btnDefinicao,
            this.btnPopulacao});
            this.toolsMenu.Name = "toolsMenu";
            this.toolsMenu.Size = new System.Drawing.Size(84, 20);
            this.toolsMenu.Text = "&Ferramentas";
            // 
            // btnBanco
            // 
            this.btnBanco.Enabled = false;
            this.btnBanco.Image = ((System.Drawing.Image)(resources.GetObject("btnBanco.Image")));
            this.btnBanco.Name = "btnBanco";
            this.btnBanco.Size = new System.Drawing.Size(220, 22);
            this.btnBanco.Text = "&Definir B&anco de Dados";
            // 
            // btnDefinicao
            // 
            this.btnDefinicao.Enabled = false;
            this.btnDefinicao.Image = ((System.Drawing.Image)(resources.GetObject("btnDefinicao.Image")));
            this.btnDefinicao.Name = "btnDefinicao";
            this.btnDefinicao.Size = new System.Drawing.Size(220, 22);
            this.btnDefinicao.Text = "Definir Cabeçalho e Rodap&é";
            this.btnDefinicao.Click += new System.EventHandler(this.btnDefinicao_Click);
            // 
            // helpMenu
            // 
            this.helpMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnContent,
            this.toolStripSeparator8,
            this.btnSobre});
            this.helpMenu.Name = "helpMenu";
            this.helpMenu.Size = new System.Drawing.Size(50, 20);
            this.helpMenu.Text = "&Ajuda";
            // 
            // btnContent
            // 
            this.btnContent.Enabled = false;
            this.btnContent.Image = ((System.Drawing.Image)(resources.GetObject("btnContent.Image")));
            this.btnContent.Name = "btnContent";
            this.btnContent.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F1)));
            this.btnContent.Size = new System.Drawing.Size(173, 22);
            this.btnContent.Text = "&Conteúdo";
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(170, 6);
            // 
            // btnSobre
            // 
            this.btnSobre.Enabled = false;
            this.btnSobre.Image = ((System.Drawing.Image)(resources.GetObject("btnSobre.Image")));
            this.btnSobre.Name = "btnSobre";
            this.btnSobre.Size = new System.Drawing.Size(173, 22);
            this.btnSobre.Text = "&Sobre ...";
            // 
            // finalizarToolStripMenuItem
            // 
            this.finalizarToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLogoff});
            this.finalizarToolStripMenuItem.Name = "finalizarToolStripMenuItem";
            this.finalizarToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.finalizarToolStripMenuItem.Text = "Finaliz&ar";
            // 
            // btnLogoff
            // 
            this.btnLogoff.Enabled = false;
            this.btnLogoff.Image = ((System.Drawing.Image)(resources.GetObject("btnLogoff.Image")));
            this.btnLogoff.Name = "btnLogoff";
            this.btnLogoff.Size = new System.Drawing.Size(109, 22);
            this.btnLogoff.Text = "Logoff";
            this.btnLogoff.Click += new System.EventHandler(this.btnLogoff_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnConectar1,
            this.btnCadastro1,
            this.btnCabecalho1,
            this.btnConsultar1,
            this.btnBanco1,
            this.btnDefinicao1,
            this.btnLogoff1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(800, 25);
            this.toolStrip1.TabIndex = 5;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnConectar1
            // 
            this.btnConectar1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnConectar1.Image = ((System.Drawing.Image)(resources.GetObject("btnConectar1.Image")));
            this.btnConectar1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnConectar1.Name = "btnConectar1";
            this.btnConectar1.Size = new System.Drawing.Size(23, 22);
            this.btnConectar1.Text = "toolStripButton7";
            this.btnConectar1.ToolTipText = "Login";
            this.btnConectar1.Click += new System.EventHandler(this.btnConectar1_Click);
            // 
            // btnCadastro1
            // 
            this.btnCadastro1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnCadastro1.Enabled = false;
            this.btnCadastro1.Image = ((System.Drawing.Image)(resources.GetObject("btnCadastro1.Image")));
            this.btnCadastro1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCadastro1.Name = "btnCadastro1";
            this.btnCadastro1.Size = new System.Drawing.Size(23, 22);
            this.btnCadastro1.Text = "toolStripButton1";
            this.btnCadastro1.ToolTipText = "Cadastro de Usuários";
            this.btnCadastro1.Click += new System.EventHandler(this.btnCadastro1_Click);
            // 
            // btnCabecalho1
            // 
            this.btnCabecalho1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnCabecalho1.Enabled = false;
            this.btnCabecalho1.Image = ((System.Drawing.Image)(resources.GetObject("btnCabecalho1.Image")));
            this.btnCabecalho1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCabecalho1.Name = "btnCabecalho1";
            this.btnCabecalho1.Size = new System.Drawing.Size(23, 22);
            this.btnCabecalho1.Text = "toolStripButton2";
            this.btnCabecalho1.ToolTipText = "Cabeçalhos e Rodapés";
            this.btnCabecalho1.Click += new System.EventHandler(this.btnCabecalho1_Click);
            // 
            // btnConsultar1
            // 
            this.btnConsultar1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnConsultar1.Enabled = false;
            this.btnConsultar1.Image = ((System.Drawing.Image)(resources.GetObject("btnConsultar1.Image")));
            this.btnConsultar1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnConsultar1.Name = "btnConsultar1";
            this.btnConsultar1.Size = new System.Drawing.Size(23, 22);
            this.btnConsultar1.Text = "toolStripButton3";
            this.btnConsultar1.ToolTipText = "Consultar";
            this.btnConsultar1.Click += new System.EventHandler(this.btnConsultar1_Click);
            // 
            // btnBanco1
            // 
            this.btnBanco1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnBanco1.Enabled = false;
            this.btnBanco1.Image = ((System.Drawing.Image)(resources.GetObject("btnBanco1.Image")));
            this.btnBanco1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnBanco1.Name = "btnBanco1";
            this.btnBanco1.Size = new System.Drawing.Size(23, 22);
            this.btnBanco1.Text = "toolStripButton4";
            this.btnBanco1.ToolTipText = "Definir Banco";
            // 
            // btnDefinicao1
            // 
            this.btnDefinicao1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnDefinicao1.Enabled = false;
            this.btnDefinicao1.Image = ((System.Drawing.Image)(resources.GetObject("btnDefinicao1.Image")));
            this.btnDefinicao1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnDefinicao1.Name = "btnDefinicao1";
            this.btnDefinicao1.Size = new System.Drawing.Size(23, 22);
            this.btnDefinicao1.Text = "toolStripButton5";
            this.btnDefinicao1.ToolTipText = "Definir Cabeçalho";
            this.btnDefinicao1.Click += new System.EventHandler(this.btnDefinicao1_Click);
            // 
            // btnLogoff1
            // 
            this.btnLogoff1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnLogoff1.Enabled = false;
            this.btnLogoff1.Image = ((System.Drawing.Image)(resources.GetObject("btnLogoff1.Image")));
            this.btnLogoff1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLogoff1.Name = "btnLogoff1";
            this.btnLogoff1.Size = new System.Drawing.Size(23, 22);
            this.btnLogoff1.Text = "toolStripButton6";
            this.btnLogoff1.ToolTipText = "Logoff";
            this.btnLogoff1.Click += new System.EventHandler(this.btnLogoff1_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblConexao,
            this.lblNivel});
            this.statusStrip.Location = new System.Drawing.Point(0, 428);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(800, 22);
            this.statusStrip.TabIndex = 6;
            this.statusStrip.Text = "StatusStrip";
            // 
            // lblStatus
            // 
            this.lblStatus.Enabled = false;
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(40, 17);
            this.lblStatus.Text = "Nome";
            // 
            // lblConexao
            // 
            this.lblConexao.ForeColor = System.Drawing.Color.DarkRed;
            this.lblConexao.Name = "lblConexao";
            this.lblConexao.Size = new System.Drawing.Size(711, 17);
            this.lblConexao.Spring = true;
            this.lblConexao.Text = "Desconectado";
            // 
            // lblNivel
            // 
            this.lblNivel.Enabled = false;
            this.lblNivel.Name = "lblNivel";
            this.lblNivel.Size = new System.Drawing.Size(34, 17);
            this.lblNivel.Text = "Nivel";
            // 
            // txtContador
            // 
            this.txtContador.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtContador.Location = new System.Drawing.Point(725, 395);
            this.txtContador.Name = "txtContador";
            this.txtContador.Size = new System.Drawing.Size(63, 23);
            this.txtContador.TabIndex = 10;
            this.txtContador.Visible = false;
            this.txtContador.TextChanged += new System.EventHandler(this.txtContador_TextChanged);
            // 
            // picLogo
            // 
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.ErrorImage = global::WindowsFormsApp1.Properties.Resources.Logo_Principal_Fundo;
            this.picLogo.Image = global::WindowsFormsApp1.Properties.Resources.Logo_Principal_Fundo;
            this.picLogo.InitialImage = global::WindowsFormsApp1.Properties.Resources.Logo_Principal_Fundo;
            this.picLogo.Location = new System.Drawing.Point(182, 135);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(335, 119);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLogo.TabIndex = 8;
            this.picLogo.TabStop = false;
            // 
            // btnPopulacao
            // 
            this.btnPopulacao.Enabled = false;
            this.btnPopulacao.Image = global::WindowsFormsApp1.Properties.Resources.User_group;
            this.btnPopulacao.Name = "btnPopulacao";
            this.btnPopulacao.Size = new System.Drawing.Size(220, 22);
            this.btnPopulacao.Text = "População";
            this.btnPopulacao.Click += new System.EventHandler(this.btnPopulacao_Click);
            // 
            // frmPrincipal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtContador);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.picLogo);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.Name = "frmPrincipal";
            this.Text = "Report Flex 1.0";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Activated += new System.EventHandler(this.frmPrincipal_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmPrincipal_FormClosing);
            this.Load += new System.EventHandler(this.frmPrincipal_Load);
            this.Leave += new System.EventHandler(this.frmPrincipal_Leave);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileMenu;
        private System.Windows.Forms.ToolStripMenuItem btnConsultar;
        private System.Windows.Forms.ToolStripMenuItem btnCadastro;
        private System.Windows.Forms.ToolStripMenuItem btnCabecalho;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem btnSair;
        private System.Windows.Forms.ToolStripMenuItem toolsMenu;
        private System.Windows.Forms.ToolStripMenuItem btnBanco;
        private System.Windows.Forms.ToolStripMenuItem btnDefinicao;
        private System.Windows.Forms.ToolStripMenuItem helpMenu;
        private System.Windows.Forms.ToolStripMenuItem btnContent;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem btnSobre;
        private System.Windows.Forms.ToolStripMenuItem finalizarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem btnLogoff;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnCadastro1;
        private System.Windows.Forms.ToolStripButton btnCabecalho1;
        private System.Windows.Forms.ToolStripButton btnConsultar1;
        private System.Windows.Forms.ToolStripButton btnBanco1;
        private System.Windows.Forms.ToolStripButton btnDefinicao1;
        private System.Windows.Forms.ToolStripButton btnLogoff1;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.ToolStripMenuItem btnConectar;
        private System.Windows.Forms.ToolStripButton btnConectar1;
        public System.Windows.Forms.StatusStrip statusStrip;
        public System.Windows.Forms.ToolStripStatusLabel lblStatus;
        public System.Windows.Forms.ToolStripStatusLabel lblConexao;
        public System.Windows.Forms.ToolStripStatusLabel lblNivel;
        private System.Windows.Forms.TextBox txtContador;
        private System.Windows.Forms.ToolStripMenuItem btnPopulacao;
    }
}