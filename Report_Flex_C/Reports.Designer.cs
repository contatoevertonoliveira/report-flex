namespace WindowsFormsApp1
{
    partial class FrmReports
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
            //this.rptRelatorios = new Microsoft.Reporting.WinForms.ReportViewer();
            this.SuspendLayout();
            // 
            // rptRelatorios
            // 
            /*this.rptRelatorios.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rptRelatorios.LocalReport.EnableExternalImages = true;
            this.rptRelatorios.Location = new System.Drawing.Point(0, 0);
            this.rptRelatorios.Name = "rptRelatorios";
            this.rptRelatorios.Size = new System.Drawing.Size(800, 450);
            this.rptRelatorios.TabIndex = 0;*/
            // 
            // FrmReports
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            //this.Controls.Add(this.rptRelatorios);
            this.Name = "FrmReports";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Reports";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Reports_Load);
            this.ResumeLayout(false);

        }

        #endregion
        
        //public Microsoft.Reporting.WinForms.ReportViewer rptRelatorios;
    }
}