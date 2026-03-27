using System;
using System.Drawing;
using System.Windows.Forms;
using ReportFlex.WinForms;

namespace WindowsFormsApp1
{
    public class ConsultasConfigForm : Form
    {
        FlowLayoutPanel panel;
        Button btnSalvar;
        Button btnFechar;
        CheckBox cbCpfInfo, cbCpfTodos, cbCpfCatracas;
        CheckBox cbTransitoPeriodo, cbCriticoPortas;
        CheckBox cbEmpresaInfo, cbEmpresaTodos;
        CheckBox cbCrachaInfo, cbCrachaTodos, cbCrachaCatracas;
        CheckBox cbMatriculaInfo, cbMatriculaTodos, cbMatriculaCatracas;
        CheckBox cbVisitDoc, cbVisitEmp;

        public ConsultasConfigForm()
        {
            Text = "Consultas Config";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(640, 520);

            panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.AutoScroll = true;
            panel.Padding = new Padding(12);
            panel.Size = new Size(640, 420);

            cbCpfInfo = CreateSwitch("CPF → Informação de Cadastro");
            cbCpfTodos = CreateSwitch("CPF → Todos Acessos");
            cbCpfCatracas = CreateSwitch("CPF → Somente Catracas");
            cbTransitoPeriodo = CreateSwitch("Trânsito → Por Período");
            cbCriticoPortas = CreateSwitch("Crítico (Portas) → Por Período");
            cbEmpresaInfo = CreateSwitch("Empresa → Informação de Cadastro");
            cbEmpresaTodos = CreateSwitch("Empresa → Todos os acessos");
            cbCrachaInfo = CreateSwitch("Crachá → Informação de Cadastro");
            cbCrachaTodos = CreateSwitch("Crachá → Todos Acessos");
            cbCrachaCatracas = CreateSwitch("Crachá → Somente Catracas");
            cbMatriculaInfo = CreateSwitch("Matrícula → Informação de Cadastro");
            cbMatriculaTodos = CreateSwitch("Matrícula → Todos Acessos");
            cbMatriculaCatracas = CreateSwitch("Matrícula → Somente Catracas");
            cbVisitDoc = CreateSwitch("Visitantes → Acessos por Documento");
            cbVisitEmp = CreateSwitch("Visitantes → Acessos por Empresa");

            panel.Controls.Add(cbCpfInfo);
            panel.Controls.Add(cbCpfTodos);
            panel.Controls.Add(cbCpfCatracas);
            panel.Controls.Add(cbTransitoPeriodo);
            panel.Controls.Add(cbCriticoPortas);
            panel.Controls.Add(cbEmpresaInfo);
            panel.Controls.Add(cbEmpresaTodos);
            panel.Controls.Add(cbCrachaInfo);
            panel.Controls.Add(cbCrachaTodos);
            panel.Controls.Add(cbCrachaCatracas);
            panel.Controls.Add(cbMatriculaInfo);
            panel.Controls.Add(cbMatriculaTodos);
            panel.Controls.Add(cbMatriculaCatracas);
            panel.Controls.Add(cbVisitDoc);
            panel.Controls.Add(cbVisitEmp);

            btnSalvar = new Button();
            btnSalvar.Text = "Salvar";
            btnSalvar.Width = 100;
            btnSalvar.Height = 36;
            btnSalvar.BackColor = Color.ForestGreen;
            btnSalvar.ForeColor = Color.White;
            btnSalvar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSalvar.Click += (s, e) => Save();

            btnFechar = new Button();
            btnFechar.Text = "Fechar";
            btnFechar.Width = 100;
            btnFechar.Height = 36;
            btnFechar.BackColor = Color.Gray;
            btnFechar.ForeColor = Color.White;
            btnFechar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnFechar.Click += (s, e) => Close();

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 60;
            bottom.Padding = new Padding(12);
            bottom.Controls.Add(btnSalvar);
            bottom.Controls.Add(btnFechar);
            btnFechar.Location = new Point(Width - 240, 12);
            btnSalvar.Location = new Point(Width - 350, 12);

            Controls.Add(panel);
            Controls.Add(bottom);

            LoadConfig();
        }

        CheckBox CreateSwitch(string label)
        {
            var cb = new CheckBox();
            cb.Text = label;
            cb.AutoSize = true;
            cb.Padding = new Padding(6);
            return cb;
        }

        void LoadConfig()
        {
            var cfg = QueriesConfigStore.Load();
            cbCpfInfo.Checked = cfg.CpfInfo;
            cbCpfTodos.Checked = cfg.CpfTodos;
            cbCpfCatracas.Checked = cfg.CpfCatracas;
            cbTransitoPeriodo.Checked = cfg.TransitoPeriodo;
            cbCriticoPortas.Checked = cfg.CriticoPortas;
            cbEmpresaInfo.Checked = cfg.EmpresaInfo;
            cbEmpresaTodos.Checked = cfg.EmpresaTodos;
            cbCrachaInfo.Checked = cfg.CrachaInfo;
            cbCrachaTodos.Checked = cfg.CrachaTodos;
            cbCrachaCatracas.Checked = cfg.CrachaCatracas;
            cbMatriculaInfo.Checked = cfg.MatriculaInfo;
            cbMatriculaTodos.Checked = cfg.MatriculaTodos;
            cbMatriculaCatracas.Checked = cfg.MatriculaCatracas;
            cbVisitDoc.Checked = cfg.VisitantesDocumento;
            cbVisitEmp.Checked = cfg.VisitantesEmpresa;
        }

        void Save()
        {
            var cfg = new QueriesConfig
            {
                CpfInfo = cbCpfInfo.Checked,
                CpfTodos = cbCpfTodos.Checked,
                CpfCatracas = cbCpfCatracas.Checked,
                TransitoPeriodo = cbTransitoPeriodo.Checked,
                CriticoPortas = cbCriticoPortas.Checked,
                EmpresaInfo = cbEmpresaInfo.Checked,
                EmpresaTodos = cbEmpresaTodos.Checked,
                CrachaInfo = cbCrachaInfo.Checked,
                CrachaTodos = cbCrachaTodos.Checked,
                CrachaCatracas = cbCrachaCatracas.Checked,
                MatriculaInfo = cbMatriculaInfo.Checked,
                MatriculaTodos = cbMatriculaTodos.Checked,
                MatriculaCatracas = cbMatriculaCatracas.Checked,
                VisitantesDocumento = cbVisitDoc.Checked,
                VisitantesEmpresa = cbVisitEmp.Checked
            };
            QueriesConfigStore.Save(cfg);
            MessageBox.Show("Configurações salvas. Reabra a tela de Consultas para aplicar.", "Report Flex", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
