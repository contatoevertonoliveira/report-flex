namespace WindowsFormsApp1
{
    partial class FrmAtivarClientesPrestadores
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
            this.gpbDefinir = new System.Windows.Forms.GroupBox();
            this.btnSair = new System.Windows.Forms.Button();
            this.btnDefinir = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.cboPrestadores = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.picCliente = new System.Windows.Forms.PictureBox();
            this.cboClientes = new System.Windows.Forms.ComboBox();
            this.gpbDefinir.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCliente)).BeginInit();
            this.SuspendLayout();
            // 
            // gpbDefinir
            // 
            this.gpbDefinir.Controls.Add(this.btnSair);
            this.gpbDefinir.Controls.Add(this.btnDefinir);
            this.gpbDefinir.Controls.Add(this.groupBox2);
            this.gpbDefinir.Controls.Add(this.groupBox1);
            this.gpbDefinir.Location = new System.Drawing.Point(23, 25);
            this.gpbDefinir.Name = "gpbDefinir";
            this.gpbDefinir.Size = new System.Drawing.Size(468, 228);
            this.gpbDefinir.TabIndex = 0;
            this.gpbDefinir.TabStop = false;
            this.gpbDefinir.Text = "Defina seu Cabeçalho e Rodapé:";
            // 
            // btnSair
            // 
            this.btnSair.Image = global::WindowsFormsApp1.Properties.Resources.Turn_off;
            this.btnSair.Location = new System.Drawing.Point(270, 181);
            this.btnSair.Name = "btnSair";
            this.btnSair.Size = new System.Drawing.Size(176, 41);
            this.btnSair.TabIndex = 4;
            this.btnSair.UseVisualStyleBackColor = true;
            this.btnSair.Click += new System.EventHandler(this.btnSair_Click);
            // 
            // btnDefinir
            // 
            this.btnDefinir.Image = global::WindowsFormsApp1.Properties.Resources.Yes;
            this.btnDefinir.Location = new System.Drawing.Point(26, 181);
            this.btnDefinir.Name = "btnDefinir";
            this.btnDefinir.Size = new System.Drawing.Size(176, 41);
            this.btnDefinir.TabIndex = 3;
            this.btnDefinir.UseVisualStyleBackColor = true;
            this.btnDefinir.Click += new System.EventHandler(this.btnDefinir_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.picLogo);
            this.groupBox2.Controls.Add(this.cboPrestadores);
            this.groupBox2.Location = new System.Drawing.Point(26, 106);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(420, 68);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Prestador Principal:";
            // 
            // picLogo
            // 
            this.picLogo.Location = new System.Drawing.Point(306, 7);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(114, 59);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picLogo.TabIndex = 2;
            this.picLogo.TabStop = false;
            // 
            // cboPrestadores
            // 
            this.cboPrestadores.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPrestadores.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboPrestadores.ForeColor = System.Drawing.Color.DarkRed;
            this.cboPrestadores.FormattingEnabled = true;
            this.cboPrestadores.Location = new System.Drawing.Point(23, 32);
            this.cboPrestadores.Name = "cboPrestadores";
            this.cboPrestadores.Size = new System.Drawing.Size(262, 24);
            this.cboPrestadores.TabIndex = 1;
            this.cboPrestadores.SelectedIndexChanged += new System.EventHandler(this.cboPrestadores_SelectedIndexChanged);
            this.cboPrestadores.SelectedValueChanged += new System.EventHandler(this.cboPrestadores_SelectedValueChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.picCliente);
            this.groupBox1.Controls.Add(this.cboClientes);
            this.groupBox1.Location = new System.Drawing.Point(26, 33);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(420, 67);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Cliente Principal:";
            // 
            // picCliente
            // 
            this.picCliente.Location = new System.Drawing.Point(306, 7);
            this.picCliente.Name = "picCliente";
            this.picCliente.Size = new System.Drawing.Size(114, 59);
            this.picCliente.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picCliente.TabIndex = 1;
            this.picCliente.TabStop = false;
            // 
            // cboClientes
            // 
            this.cboClientes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboClientes.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboClientes.ForeColor = System.Drawing.Color.DarkRed;
            this.cboClientes.FormattingEnabled = true;
            this.cboClientes.Location = new System.Drawing.Point(24, 31);
            this.cboClientes.Name = "cboClientes";
            this.cboClientes.Size = new System.Drawing.Size(261, 24);
            this.cboClientes.TabIndex = 0;
            this.cboClientes.SelectedIndexChanged += new System.EventHandler(this.cboClientes_SelectedIndexChanged);
            this.cboClientes.SelectedValueChanged += new System.EventHandler(this.cboClientes_SelectedValueChanged);
            // 
            // FrmAtivarClientesPrestadores
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(516, 277);
            this.Controls.Add(this.gpbDefinir);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FrmAtivarClientesPrestadores";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AtivarClientesPrestadores";
            this.Load += new System.EventHandler(this.FrmAtivarClientesPrestadores_Load);
            this.gpbDefinir.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picCliente)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gpbDefinir;
        private System.Windows.Forms.Button btnSair;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cboPrestadores;
        private System.Windows.Forms.ComboBox cboClientes;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.PictureBox picCliente;
        public System.Windows.Forms.Button btnDefinir;
    }
}