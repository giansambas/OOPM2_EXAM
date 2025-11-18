using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace EquipSense
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private TabControl tabControl;
        private TabPage tabLive;
        private TabPage tabDashboard;
        private TabPage tabManual;
        private TabPage tabMaintenance;
        private DataGridView dgvLive;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartTopEquipment;
        private Label lblTotalUsage;
        private Label lblMostUsed;
        private ComboBox cbManualEquipment;
        private NumericUpDown nudManualMinutes;
        private Button btnAddManual;
        private DataGridView dgvMaintenance;
        private Button btnMarkMaint;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.tabControl = new TabControl();
            this.tabLive = new TabPage();
            this.tabDashboard = new TabPage();
            this.tabManual = new TabPage();
            this.tabMaintenance = new TabPage();
            this.dgvLive = new DataGridView();
            this.chartTopEquipment = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.lblTotalUsage = new Label();
            this.lblMostUsed = new Label();
            this.cbManualEquipment = new ComboBox();
            this.nudManualMinutes = new NumericUpDown();
            this.btnAddManual = new Button();
            this.dgvMaintenance = new DataGridView();
            this.btnMarkMaint = new Button();

            this.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ClientSize = new System.Drawing.Size(1100, 700);
            this.Text = "EquipSense - Smart Gym Tracker";

            this.tabControl.Dock = DockStyle.Fill;
            this.tabControl.Controls.AddRange(new TabPage[] { this.tabLive, this.tabDashboard, this.tabManual, this.tabMaintenance });

            // Live Tracking tab
            this.tabLive.Text = "Live Tracking";
            this.tabLive.Controls.Add(this.dgvLive);
            this.dgvLive.Dock = DockStyle.Fill;
            this.dgvLive.ReadOnly = true;
            this.dgvLive.RowHeadersVisible = false;
            this.dgvLive.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvLive.BackgroundColor = System.Drawing.Color.White;
            this.dgvLive.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(30, 58, 138);
            this.dgvLive.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgvLive.EnableHeadersVisualStyles = false;

            // Dashboard
            this.tabDashboard.Text = "Dashboard";
            this.tabDashboard.Padding = new Padding(15);
            this.tabDashboard.BackColor = System.Drawing.Color.WhiteSmoke;
            this.chartTopEquipment.Dock = DockStyle.Top;
            this.chartTopEquipment.Height = 300;
            this.lblTotalUsage.Top = 320;
            this.lblTotalUsage.Left = 30;
            this.lblMostUsed.Top = 350;
            this.lblMostUsed.Left = 30;
            this.lblTotalUsage.Font = new System.Drawing.Font("Segoe UI Semibold", 11F);
            this.lblMostUsed.Font = new System.Drawing.Font("Segoe UI Semibold", 11F);
            this.tabDashboard.Controls.Add(this.chartTopEquipment);
            this.tabDashboard.Controls.Add(this.lblTotalUsage);
            this.tabDashboard.Controls.Add(this.lblMostUsed);

            // Manual Entry
            this.tabManual.Text = "Manual Entry";
            this.cbManualEquipment.Left = 30;
            this.cbManualEquipment.Top = 30;
            this.cbManualEquipment.Width = 250;
            this.nudManualMinutes.Left = 300;
            this.nudManualMinutes.Top = 30;
            this.nudManualMinutes.Width = 100;
            this.btnAddManual.Text = "Add Minutes";
            this.btnAddManual.Left = 420;
            this.btnAddManual.Top = 30;
            this.btnAddManual.Width = 150;
            this.btnAddManual.Height = 40;
            this.btnAddManual.FlatStyle = FlatStyle.Flat;
            this.btnAddManual.FlatAppearance.BorderSize = 0;
            this.btnAddManual.BackColor = System.Drawing.Color.FromArgb(30, 58, 138);
            this.btnAddManual.ForeColor = System.Drawing.Color.White;
            this.btnAddManual.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.tabManual.Controls.AddRange(new Control[] { this.cbManualEquipment, this.nudManualMinutes, this.btnAddManual });

            // Maintenance
            this.tabMaintenance.Text = "Maintenance";
            this.dgvMaintenance.Dock = DockStyle.Top;
            this.dgvMaintenance.Height = 300;
            this.dgvMaintenance.BackgroundColor = System.Drawing.Color.White;
            this.dgvMaintenance.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(30, 58, 138);
            this.dgvMaintenance.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgvMaintenance.EnableHeadersVisualStyles = false;
            this.btnMarkMaint.Text = "Mark Maintenance Done";
            this.btnMarkMaint.Top = 320;
            this.btnMarkMaint.Left = 30;
            this.btnMarkMaint.Width = 220;
            this.btnMarkMaint.Height = 40;
            this.btnMarkMaint.FlatStyle = FlatStyle.Flat;
            this.btnMarkMaint.FlatAppearance.BorderSize = 0;
            this.btnMarkMaint.BackColor = System.Drawing.Color.FromArgb(30, 58, 138);
            this.btnMarkMaint.ForeColor = System.Drawing.Color.White;
            this.tabMaintenance.Controls.AddRange(new Control[] { this.dgvMaintenance, this.btnMarkMaint });

            this.Controls.Add(this.tabControl);
        }
    }
}
