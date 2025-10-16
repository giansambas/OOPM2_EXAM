using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using Newtonsoft.Json; // ✅ Use Newtonsoft.Json for older .NET

namespace GymTracker
{
    public partial class Form1 : Form
    {
        private TcpListener listener;
        private Thread listenThread;
        private Dictionary<string, DateTime> lastSeen = new Dictionary<string, DateTime>();
        private Dictionary<string, TimeSpan> usageTime = new Dictionary<string, TimeSpan>();

        public Form1()
        {
            InitializeComponent();
            StartServer();
        }

        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5050);
            listener.Start();
            listenThread = new Thread(ListenForClients);
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        private void ListenForClients()
        {
            while (true)
            {
                var client = listener.AcceptTcpClient();
                var thread = new Thread(() => HandleClient(client));
                thread.IsBackground = true;
                thread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var reader = new BinaryReader(stream);

            while (true)
            {
                try
                {
                    int msgLength = ReadInt(reader);
                    byte[] jsonData = reader.ReadBytes(msgLength);
                    string jsonString = System.Text.Encoding.UTF8.GetString(jsonData);

                    var detections = JsonConvert.DeserializeObject<List<Detection>>(jsonString);
                    if (detections == null) continue;

                    HashSet<string> detectedNow = new HashSet<string>(detections.Select(d => d.label));

                    // ✅ Update active detections
                    foreach (var label in detectedNow)
                    {
                        if (!lastSeen.ContainsKey(label))
                        {
                            lastSeen[label] = DateTime.Now;
                            usageTime[label] = TimeSpan.Zero;
                        }
                        else
                        {
                            usageTime[label] += DateTime.Now - lastSeen[label];
                            lastSeen[label] = DateTime.Now;
                        }
                    }

                    // ✅ Remove items not seen for 3+ seconds
                    List<string> toRemove = new List<string>();
                    foreach (var label in lastSeen.Keys)
                    {
                        if (!detectedNow.Contains(label) &&
                            (DateTime.Now - lastSeen[label]).TotalSeconds > 3)
                        {
                            toRemove.Add(label);
                        }
                    }

                    foreach (var label in toRemove)
                        lastSeen.Remove(label);

                    UpdateGridSafe(usageTime);
                }
                catch
                {
                    break;
                }
            }

            client.Close();
        }

        private int ReadInt(BinaryReader reader)
        {
            byte[] intBytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return BitConverter.ToInt32(intBytes, 0);
        }

        private void UpdateGridSafe(Dictionary<string, TimeSpan> usage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateGridSafe(usage)));
                return;
            }

            dataGridView1.Rows.Clear();
            foreach (var kv in usage)
            {
                string label = kv.Key;
                string timeStr = $"{kv.Value.Minutes:D2}:{kv.Value.Seconds:D2}";
                dataGridView1.Rows.Add(label, timeStr);
            }
        }
    }

    public class Detection
    {
        public string label { get; set; }
        public double conf { get; set; }
        public double[] bbox { get; set; }
    }
}
