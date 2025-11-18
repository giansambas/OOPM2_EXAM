using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting; // ✅ Required for charts
using GymTracker;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace EquipSense
{
    public partial class Form1 : Form
    {
        private readonly string connStr = "server=localhost;user id=root;password=;database=gym_tracker;";
        private TcpListener tcpListener;
        private Thread listenerThread;
        private System.Windows.Forms.Timer uiTimer;

        // Equipment data model
        private class EquipmentData
        {
            public string Name;
            public double TotalSeconds;
            public double SessionSeconds;
            public DateTime? LastDetected;
            public bool IsActive;
            public DateTime? LastMaintenance;
        }

        // List of recognized equipment
        private readonly string[] equipmentNames = new string[]
        {
            "ab roller", "adjustable bench", "assault bike", "barbell", "dumbbell",
            "flat bench", "kettlebell", "medicine ball", "punching bag", "resistance bands",
            "squat rack", "stationary bike", "treadmill", "trx straps", "weight plates", "yoga ball"
        };

        private Dictionary<string, EquipmentData> equipment = new Dictionary<string, EquipmentData>(StringComparer.OrdinalIgnoreCase);
        private MySqlConnection dbConn;

        // Dashboard detailed report and export
        private DataGridView dgvReport;
        private Button btnExportTxt;

        public Form1()
        {

            InitializeComponent();
            InitEquipmentDictionary();
            SetupUIBindings();
            ConnectDatabase();
            LoadDatabaseTotals();
            StartTcpListener();
            StartUiTimer();
            PopulateManualDropdown();
            LoadMaintenanceTable();
            CreateReportTable();
            CreateExportTxtButton();
        }

        #region Initialization & UI
        private void InitEquipmentDictionary()
        {
            foreach (var n in equipmentNames)
            {
                equipment[n] = new EquipmentData
                {
                    Name = n,
                    TotalSeconds = 0,
                    SessionSeconds = 0,
                    LastDetected = null,
                    IsActive = false,
                    LastMaintenance = null
                };
            }
        }

        private void SetupUIBindings()
        {
            dgvLive.Columns.Clear();
            dgvLive.Columns.Add("Equipment", "Equipment");
            dgvLive.Columns.Add("Status", "Status");
            dgvLive.Columns.Add("Usage", "Total (MM:SS)");
            dgvLive.Columns.Add("Session", "Session (MM:SS)");
            dgvLive.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            chartTopEquipment.Series.Clear();
            var series = new Series("Top")
            {
                ChartType = SeriesChartType.Bar,
                IsValueShownAsLabel = true
            };
            chartTopEquipment.Series.Add(series);
            chartTopEquipment.ChartAreas.Clear();
            chartTopEquipment.ChartAreas.Add(new ChartArea("Main"));
            chartTopEquipment.Legends.Clear();

            // Wire events
            btnAddManual.Click += BtnAddManual_Click;
            btnMarkMaint.Click += BtnMarkMaint_Click;
        }

        private void CreateReportTable()
        {
            dgvReport = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 250,
                ReadOnly = true,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };

            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 58, 138);
            dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            dgvReport.Columns.Add("Equipment", "Equipment");
            dgvReport.Columns.Add("TotalUsage", "Total Usage (mins)");
            dgvReport.Columns.Add("SessionUsage", "Session Usage (mins)");
            dgvReport.Columns.Add("LastMaint", "Last Maintenance");
            dgvReport.Columns.Add("NextMaint", "Next Due");

            tabDashboard.Controls.Add(dgvReport);
        }

        private void CreateExportTxtButton()
        {
            btnExportTxt = new Button
            {
                Text = "Export Full Report",
                Width = 200,
                Height = 40,
                Top = 390,
                Left = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 58, 138),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnExportTxt.FlatAppearance.BorderSize = 0;
            btnExportTxt.Click += BtnExportTxt_Click;
            tabDashboard.Controls.Add(btnExportTxt);
        }

        private void PopulateManualDropdown()
        {
            cbManualEquipment.Items.Clear();
            foreach (var n in equipmentNames) cbManualEquipment.Items.Add(n);
            if (cbManualEquipment.Items.Count > 0) cbManualEquipment.SelectedIndex = 0;
        }
        #endregion

        #region Database
        private void ConnectDatabase()
        {
            try
            {
                dbConn = new MySqlConnection(connStr);
                dbConn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB connect failed: " + ex.Message);
            }
        }

        private void LoadDatabaseTotals()
        {
            if (dbConn == null) return;

            string q = "SELECT equipment_name, total_duration, last_maintenance_date FROM equipment_usage";
            using (var cmd = new MySqlCommand(q, dbConn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    string name = rdr.GetString("equipment_name");
                    double dur = rdr.IsDBNull(rdr.GetOrdinal("total_duration")) ? 0 : rdr.GetDouble("total_duration");
                    DateTime? lastMaint = rdr.IsDBNull(rdr.GetOrdinal("last_maintenance_date")) ? (DateTime?)null : rdr.GetDateTime("last_maintenance_date");

                    if (equipment.ContainsKey(name))
                    {
                        equipment[name].TotalSeconds = dur;
                        equipment[name].LastMaintenance = lastMaint;
                    }
                }
            }

            RefreshLiveGrid();
            RefreshDashboard();
            RefreshReportTable();
        }

        private void LoadMaintenanceTable()
        {
            if (dbConn == null) return;

            dgvMaintenance.Columns.Clear();
            dgvMaintenance.Columns.Add("Equipment", "Equipment");
            dgvMaintenance.Columns.Add("LastMaintenance", "Last Maintenance");
            dgvMaintenance.Columns.Add("NextDue", "Next Due (approx)");
            dgvMaintenance.Rows.Clear();

            foreach (var kv in equipment.Values)
            {
                DateTime? lm = kv.LastMaintenance;
                string lmStr = lm?.ToString("yyyy-MM-dd") ?? "—";
                string nextStr = lm.HasValue ? lm.Value.AddMonths(6).ToString("yyyy-MM-dd") : "—";
                dgvMaintenance.Rows.Add(kv.Name, lmStr, nextStr);
            }
        }
        #endregion

        #region Manual + Maintenance Buttons
        private void BtnAddManual_Click(object sender, EventArgs e)
        {
            if (cbManualEquipment.SelectedItem == null) return;
            string name = cbManualEquipment.SelectedItem.ToString();
            int minutes = (int)nudManualMinutes.Value;
            double addSeconds = minutes * 60;

            if (equipment.ContainsKey(name))
            {
                equipment[name].TotalSeconds += addSeconds;
                equipment[name].SessionSeconds += addSeconds;
                RefreshLiveGrid();
                RefreshDashboard();
                RefreshReportTable();
                MessageBox.Show($"Added {minutes} minutes to {Capitalize(name)}.");
            }
        }

        private void BtnMarkMaint_Click(object sender, EventArgs e)
        {
            if (dgvMaintenance.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a row first.");
                return;
            }

            foreach (DataGridViewRow r in dgvMaintenance.SelectedRows)
            {
                string name = r.Cells[0].Value.ToString();
                DateTime now = DateTime.Now;

                if (equipment.ContainsKey(name))
                    equipment[name].LastMaintenance = now;
            }

            LoadMaintenanceTable();
            RefreshReportTable();
            MessageBox.Show("Maintenance updated successfully.");
        }
        #endregion

        #region TCP + Detection
        private void StartTcpListener()
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5051);
            tcpListener.Start();
            listenerThread = new Thread(TcpListenLoop) { IsBackground = true };
            listenerThread.Start();
        }

        private void TcpListenLoop()
        {
            while (true)
            {
                try
                {
                    using (var client = tcpListener.AcceptTcpClient())
                    using (var stream = client.GetStream())
                    {
                        while (client.Connected)
                        {
                            byte[] len = new byte[4];
                            int r = stream.Read(len, 0, 4);
                            if (r == 0) break;
                            int msgLen = BitConverter.ToInt32(len.Reverse().ToArray(), 0);

                            byte[] buf = new byte[msgLen];
                            int read = 0;
                            while (read < msgLen)
                            {
                                int rr = stream.Read(buf, read, msgLen - read);
                                if (rr == 0) break;
                                read += rr;
                            }

                            string json = Encoding.UTF8.GetString(buf);
                            var list = JsonConvert.DeserializeObject<List<PythonDetection>>(json);
                            HandleDetections(list);
                        }
                    }
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }
        }

        private class PythonDetection
        {
            public string label { get; set; }
        }

        private void HandleDetections(List<PythonDetection> detections)
        {
            var now = DateTime.Now;
            var set = new HashSet<string>(detections.Select(d => d.label.ToLower()));

            foreach (var kv in equipment)
            {
                kv.Value.IsActive = set.Contains(kv.Key.ToLower());
                if (kv.Value.IsActive)
                    kv.Value.LastDetected = now;
            }

            this.Invoke((MethodInvoker)(() =>
            {
                RefreshLiveGrid();
                RefreshReportTable();
            }));
        }
        #endregion

        #region Dashboard Updates
        private void StartUiTimer()
        {
            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 1000;
            uiTimer.Tick += UiTimer_Tick;
            uiTimer.Start();
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            bool changed = false;

            foreach (var kv in equipment.Values)
            {
                bool active = kv.LastDetected.HasValue && (now - kv.LastDetected.Value).TotalSeconds <= 2;
                kv.IsActive = active;

                if (active)
                {
                    kv.TotalSeconds += 1;
                    kv.SessionSeconds += 1;
                    changed = true;
                }
            }

            if (changed)
            {
                RefreshLiveGrid();
                RefreshDashboard();
                RefreshReportTable();
            }
        }

        private void RefreshLiveGrid()
        {
            dgvLive.Rows.Clear();
            foreach (var e in equipment.Values.OrderByDescending(x => x.TotalSeconds))
            {
                int row = dgvLive.Rows.Add(
                    Capitalize(e.Name),
                    e.IsActive ? "Active" : "Idle",
                    FormatTime(e.TotalSeconds),
                    FormatTime(e.SessionSeconds)
                );

                dgvLive.Rows[row].DefaultCellStyle.BackColor = e.IsActive ? Color.FromArgb(50, 130, 200) : Color.White;
                dgvLive.Rows[row].DefaultCellStyle.ForeColor = e.IsActive ? Color.White : Color.Black;
            }
        }

        private void RefreshDashboard()
        {
            var top5 = equipment.Values.OrderByDescending(x => x.TotalSeconds).Take(5).ToList();
            chartTopEquipment.Series["Top"].Points.Clear();
            foreach (var t in top5)
                chartTopEquipment.Series["Top"].Points.AddXY(Capitalize(t.Name), Math.Round(t.TotalSeconds / 60.0, 2));

            lblTotalUsage.Text = $"Total session usage: {FormatTime(equipment.Values.Sum(x => x.SessionSeconds))}";
            lblMostUsed.Text = top5.Count > 0 ? $"Most used: {Capitalize(top5[0].Name)}" : "Most used: —";
        }

        private void RefreshReportTable()
        {
            if (dgvReport == null) return;
            dgvReport.Rows.Clear();
            foreach (var kv in equipment.Values.OrderByDescending(x => x.TotalSeconds))
            {
                string lm = kv.LastMaintenance?.ToString("yyyy-MM-dd") ?? "—";
                string next = kv.LastMaintenance.HasValue ? kv.LastMaintenance.Value.AddMonths(6).ToString("yyyy-MM-dd") : "—";
                dgvReport.Rows.Add(Capitalize(kv.Name), Math.Round(kv.TotalSeconds / 60.0, 2),
                    Math.Round(kv.SessionSeconds / 60.0, 2), lm, next);
            }
        }
        #endregion

        #region Export Full Report
        private void BtnExportTxt_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== EQUIPSENSE FULL EQUIPMENT REPORT ===");
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"{"Equipment",-20}{"Total(mins)",15}{"Session(mins)",15}{"Last Maint.",15}{"Next Due",15}");
            sb.AppendLine("----------------------------------------");

            foreach (var kv in equipment.Values.OrderByDescending(x => x.TotalSeconds))
            {
                string lm = kv.LastMaintenance?.ToString("yyyy-MM-dd") ?? "—";
                string next = kv.LastMaintenance.HasValue ? kv.LastMaintenance.Value.AddMonths(6).ToString("yyyy-MM-dd") : "—";
                sb.AppendLine($"{Capitalize(kv.Name),-20}{Math.Round(kv.TotalSeconds / 60.0, 2),15}{Math.Round(kv.SessionSeconds / 60.0, 2),15}{lm,15}{next,15}");
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Text Files (*.txt)|*.txt";
                dlg.FileName = $"EquipSense_Report_{DateTime.Now:yyyyMMdd}.txt";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
                    MessageBox.Show("Full report exported successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        #endregion

        #region Helpers
        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
        }

        private static string FormatTime(double totalSeconds)
        {
            int m = (int)(totalSeconds / 60);
            int s = (int)(totalSeconds % 60);
            return $"{m:D2}:{s:D2}";
        }
        #endregion
    }
}
