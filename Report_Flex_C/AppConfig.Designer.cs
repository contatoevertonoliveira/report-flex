namespace WindowsFormsApp1
{
    partial class FrmAppConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAppConfig));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.txtBd = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cboServer = new System.Windows.Forms.ComboBox();
            this.btnNovo = new System.Windows.Forms.Button();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.gpbNovo = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtBanco = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtLogin = new System.Windows.Forms.TextBox();
            this.txtSenha = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btnSalvar = new System.Windows.Forms.Button();
            this.btnSair = new System.Windows.Forms.Button();
            this.txtConexao = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.lblConexoes = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.gpbNovo.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblConexoes);
            this.groupBox1.Controls.Add(this.gpbNovo);
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.Controls.Add(this.txtBd);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cboServer);
            this.groupBox1.Location = new System.Drawing.Point(26, 27);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(608, 226);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Caminho do Banco de Dados:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.ErrorImage = global::WindowsFormsApp1.Properties.Resources.Database;
            this.pictureBox1.Image = global::WindowsFormsApp1.Properties.Resources.Database;
            this.pictureBox1.InitialImage = global::WindowsFormsApp1.Properties.Resources.Database;
            this.pictureBox1.Location = new System.Drawing.Point(568, 34);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 24);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 8;
            this.pictureBox1.TabStop = false;
            // 
            // txtBd
            // 
            this.txtBd.Location = new System.Drawing.Point(104, 67);
            this.txtBd.Name = "txtBd";
            this.txtBd.Size = new System.Drawing.Size(450, 20);
            this.txtBd.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Dados Conexão:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Conexões:";
            // 
            // cboServer
            // 
            this.cboServer.FormattingEnabled = true;
            this.cboServer.Location = new System.Drawing.Point(70, 37);
            this.cboServer.Name = "cboServer";
            this.cboServer.Size = new System.Drawing.Size(484, 21);
            this.cboServer.TabIndex = 0;
            this.cboServer.SelectedIndexChanged += new System.EventHandler(this.cboServer_SelectedIndexChanged);
            // 
            // btnNovo
            // 
            this.btnNovo.Location = new System.Drawing.Point(26, 259);
            this.btnNovo.Name = "btnNovo";
            this.btnNovo.Size = new System.Drawing.Size(147, 38);
            this.btnNovo.TabIndex = 5;
            this.btnNovo.Text = "Nova Conexão";
            this.btnNovo.UseVisualStyleBackColor = true;
            this.btnNovo.Click += new System.EventHandler(this.btnSalvar_Click);
            // 
            // txtServer
            // 
            this.txtServer.Location = new System.Drawing.Point(12, 84);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(120, 20);
            this.txtServer.TabIndex = 9;
            // 
            // gpbNovo
            // 
            this.gpbNovo.Controls.Add(this.label7);
            this.gpbNovo.Controls.Add(this.txtConexao);
            this.gpbNovo.Controls.Add(this.label6);
            this.gpbNovo.Controls.Add(this.label5);
            this.gpbNovo.Controls.Add(this.txtSenha);
            this.gpbNovo.Controls.Add(this.txtLogin);
            this.gpbNovo.Controls.Add(this.label4);
            this.gpbNovo.Controls.Add(this.txtBanco);
            this.gpbNovo.Controls.Add(this.label3);
            this.gpbNovo.Controls.Add(this.txtServer);
            this.gpbNovo.Location = new System.Drawing.Point(6, 103);
            this.gpbNovo.Name = "gpbNovo";
            this.gpbNovo.Size = new System.Drawing.Size(596, 117);
            this.gpbNovo.TabIndex = 10;
            this.gpbNovo.TabStop = false;
            this.gpbNovo.Text = "Nova Conexão:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 68);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Servidor:";
            // 
            // txtBanco
            // 
            this.txtBanco.Location = new System.Drawing.Point(171, 84);
            this.txtBanco.Name = "txtBanco";
            this.txtBanco.Size = new System.Drawing.Size(120, 20);
            this.txtBanco.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(169, 68);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Banco de Dados:";
            // 
            // txtLogin
            // 
            this.txtLogin.Location = new System.Drawing.Point(322, 84);
            this.txtLogin.Name = "txtLogin";
            this.txtLogin.Size = new System.Drawing.Size(120, 20);
            this.txtLogin.TabIndex = 13;
            // 
            // txtSenha
            // 
            this.txtSenha.Location = new System.Drawing.Point(466, 84);
            this.txtSenha.Name = "txtSenha";
            this.txtSenha.Size = new System.Drawing.Size(120, 20);
            this.txtSenha.TabIndex = 14;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(464, 68);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 13);
            this.label5.TabIndex = 15;
            this.label5.Text = "Senha:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(321, 68);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(36, 13);
            this.label6.TabIndex = 16;
            this.label6.Text = "Login:";
            // 
            // btnSalvar
            // 
            this.btnSalvar.Location = new System.Drawing.Point(259, 259);
            this.btnSalvar.Name = "btnSalvar";
            this.btnSalvar.Size = new System.Drawing.Size(147, 38);
            this.btnSalvar.TabIndex = 6;
            this.btnSalvar.Text = "Salvar";
            this.btnSalvar.UseVisualStyleBackColor = true;
            this.btnSalvar.Click += new System.EventHandler(this.btnSalvar_Click_1);
            // 
            // btnSair
            // 
            this.btnSair.Location = new System.Drawing.Point(487, 259);
            this.btnSair.Name = "btnSair";
            this.btnSair.Size = new System.Drawing.Size(147, 38);
            this.btnSair.TabIndex = 7;
            this.btnSair.Text = "Sair";
            this.btnSair.UseVisualStyleBackColor = true;
            this.btnSair.Click += new System.EventHandler(this.button2_Click);
            // 
            // txtConexao
            // 
            this.txtConexao.Location = new System.Drawing.Point(324, 29);
            this.txtConexao.Name = "txtConexao";
            this.txtConexao.Size = new System.Drawing.Size(262, 20);
            this.txtConexao.TabIndex = 17;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(191, 29);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(127, 13);
            this.label7.TabIndex = 18;
            this.label7.Text = "Nome da Nova Conexão:";
            // 
            // lblConexoes
            // 
            this.lblConexoes.AutoSize = true;
            this.lblConexoes.Location = new System.Drawing.Point(380, 22);
            this.lblConexoes.Name = "lblConexoes";
            this.lblConexoes.Size = new System.Drawing.Size(175, 13);
            this.lblConexoes.TabIndex = 11;
            this.lblConexoes.Text = "Existem no App.Config: 0 Conexões";
            // 
            // FrmAppConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(658, 319);
            this.Controls.Add(this.btnSair);
            this.Controls.Add(this.btnSalvar);
            this.Controls.Add(this.btnNovo);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FrmAppConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Report Flex | Configurar acesso ao Banco de Dados";
            this.Load += new System.EventHandler(this.FrmAppConfig_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.gpbNovo.ResumeLayout(false);
            this.gpbNovo.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox txtBd;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboServer;
        private System.Windows.Forms.Button btnNovo;
        private System.Windows.Forms.GroupBox gpbNovo;
        private System.Windows.Forms.TextBox txtBanco;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtSenha;
        private System.Windows.Forms.TextBox txtLogin;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnSalvar;
        private System.Windows.Forms.Button btnSair;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtConexao;
        private System.Windows.Forms.Label lblConexoes;
    }
}