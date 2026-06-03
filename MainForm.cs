using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Data.SQLite;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TempHumidityMonitor.Properties;

namespace TempHumidityMonitor
{
    public partial class MainForm : Form
    {
        // ==================== 运行时字段 ====================
        private int nSend = 0, nReceive = 0, nError = 0;
        private int maxChartPoint = 30;
        private Queue<float> tempQueue, humiQueue;
        private Queue<DateTime> timeQueue;
        private float tempMin = float.MaxValue, tempMax = float.MinValue, tempSum = 0;
        private float humiMin = float.MaxValue, humiMax = float.MinValue, humiSum = 0;
        private int dataCount = 0;
        private List<byte> receiveBuffer = new List<byte>();
        private string logFilePath;
        private string GetDbPath() { return Path.Combine(Application.StartupPath, "TempHumidityData.db"); }
        private bool isComOpen = false;
        private bool isSimMode = false;
        private DateTime lastReceiveTime = DateTime.MinValue;
        private Random simRandom = new Random();
        private float simTemp = 25.0f, simHumi = 55.0f;
        private float lastTemp = 0, lastHumi = 0;

        // ==================== 构造函数 ====================
        public MainForm()
        {
            tempQueue = new Queue<float>();
            humiQueue = new Queue<float>();
            timeQueue = new Queue<DateTime>();
            InitializeComponent();
            AddHeaderLabels();
            InitAfterDesign();
        }

        private void AddHeaderLabels()
        {
            Font labelFont = new Font("微软雅黑", 9F);
            // 串口设置面板标签
            gbSerial.Controls.Add(new Label() { Text = "串口号:", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight, Location = new Point(8, 27), Size = new Size(55, 23) });
            gbSerial.Controls.Add(new Label() { Text = "波特率:", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight, Location = new Point(8, 59), Size = new Size(55, 23) });

            // 采集设置TLP标签
            tlpCollect.Controls.Add(new Label() { Text = "读取模式:", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            tlpCollect.Controls.Add(new Label() { Text = "间隔(ms):", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            tlpCollect.Controls.Add(new Label() { Text = "最大点数:", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight }, 0, 2);

            // 报警设置TLP标签
            tlpAlarm.Controls.Add(new Label() { Text = "温度上限:", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            tlpAlarm.Controls.Add(new Label() { Text = "温度下限:", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            tlpAlarm.Controls.Add(new Label() { Text = "湿度上限:", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            tlpAlarm.Controls.Add(new Label() { Text = "湿度下限:", Font = labelFont, ForeColor = Color.FromArgb(31, 41, 55), TextAlign = ContentAlignment.MiddleRight }, 0, 4);
        }

        private void InitAfterDesign()
        {
            initControls();
            LoadSettings();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
        }

        // ==================== 初始化控件状态 ====================
        private void initControls()
        {
            try { RefreshComPorts(); }
            catch (Exception ex) { cbComPort.Items.Add("无可用串口"); cbComPort.SelectedIndex = 0; LogError("初始化串口列表失败: " + ex.Message); }

            try
            {
                if (cbBaudRate.Items.Contains(Settings.Default.BaudRate.ToString()))
                    cbBaudRate.Text = Settings.Default.BaudRate.ToString();
                nudInterval.Value = Settings.Default.SampleInterval;
                nudMaxPoints.Value = Settings.Default.MaxChartPoints;
                cbReadMode.SelectedIndex = Settings.Default.ReadMode;
                nudTempHigh.Value = (decimal)Settings.Default.TempHighAlarm;
                nudTempLow.Value = (decimal)Settings.Default.TempLowAlarm;
                nudHumiHigh.Value = (decimal)Settings.Default.HumiHighAlarm;
                nudHumiLow.Value = (decimal)Settings.Default.HumiLowAlarm;
                chkEnableAlarm.Checked = Settings.Default.EnableAlarm;
                chkDataLog.Checked = Settings.Default.EnableDataLog;
                maxChartPoint = Settings.Default.MaxChartPoints;
            }
            catch { }

            InitLogFile();
            InitDatabase();

            Timer timeTimer = new Timer { Interval = 1000 };
            timeTimer.Tick += (s, ev) => { tsslTime.Text = DateTime.Now.ToString("HH:mm:ss"); };
            timeTimer.Start();
        }

        // ==================== 串口操作 ====================
        private void RefreshComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            cbComPort.Items.Clear();
            if (ports.Length > 0)
            {
                cbComPort.Items.AddRange(ports);
                cbComPort.SelectedIndex = 0;
            }
            else { cbComPort.Items.Add("无可用串口"); cbComPort.SelectedIndex = 0; }
        }

        private void btnRefreshPorts_Click(object sender, EventArgs e)
        {
            try { if (!isComOpen) RefreshComPorts(); else ShowTip("请先关闭串口再刷新端口列表"); }
            catch (Exception ex) { ShowTip("刷新串口失败: " + ex.Message); }
        }

        private void btnOpenCloseCom_Click(object sender, EventArgs e)
        { if (isComOpen) closeComPort(); else openComPort(); }

        private void openComPort()
        {
            try
            {
                string portName = cbComPort.Text;
                if (string.IsNullOrEmpty(portName) || portName == "无可用串口") { ShowTip("请选择有效的串口号"); return; }
                serialPort1.PortName = portName;
                serialPort1.BaudRate = int.Parse(cbBaudRate.Text);
                serialPort1.DataBits = 8; serialPort1.StopBits = StopBits.One; serialPort1.Parity = Parity.None;
                serialPort1.ReadTimeout = 500; serialPort1.WriteTimeout = 500;
                serialPort1.Open();
                isComOpen = true;
                btnOpenCloseCom.Text = "关闭串口"; btnOpenCloseCom.BackColor = Color.LightCoral;
                tsslStatus.Text = "● 串口已打开 - " + portName; tsslStatus.ForeColor = Color.Green;
                lblDeviceStatus.Text = "● 串口已连接     ● 正在采集     ● 数据正常";
                lblDeviceStatus.ForeColor = Color.FromArgb(31, 41, 55);
                lblCardStatusValue.Text = "在线"; lblCardStatusValue.ForeColor = Color.FromArgb(82, 196, 26);
                timer1.Interval = (int)nudInterval.Value; timer1.Start();
                cbComPort.Enabled = false; btnRefreshPorts.Enabled = false; cbBaudRate.Enabled = false;
                lblStatus.Text = "串口已打开，正在采集数据..."; lblStatus.ForeColor = Color.Green;
                receiveBuffer.Clear();
            }
            catch (Exception ex) { ShowTip("打开串口失败: " + ex.Message); LogError("打开串口失败: " + ex.Message); }
        }

        private void closeComPort()
        {
            try
            {
                timer1.Stop(); if (serialPort1.IsOpen) serialPort1.Close();
                isComOpen = false; btnOpenCloseCom.Text = "打开串口"; btnOpenCloseCom.BackColor = SystemColors.Control;
                tsslStatus.Text = "● 串口已关闭"; tsslStatus.ForeColor = Color.Gray;
                lblDeviceStatus.Text = "○ 串口未打开     ○ 等待采集     ○ 数据就绪";
                lblDeviceStatus.ForeColor = Color.FromArgb(31, 41, 55);
                lblCardStatusValue.Text = "离线"; lblCardStatusValue.ForeColor = Color.FromArgb(156, 163, 175);
                cbComPort.Enabled = true; btnRefreshPorts.Enabled = true; cbBaudRate.Enabled = true;
                lblStatus.Text = "串口已关闭"; lblStatus.ForeColor = Color.Gray;
            }
            catch (Exception ex) { LogError("关闭串口失败: " + ex.Message); }
        }

        // ==================== 定时器 ====================
        private void timer1_Tick(object sender, EventArgs e) { sendData(); }

        // ==================== 发送数据 ====================
        private void sendData()
        {
            try
            {
                if (isSimMode) { SimulateData(); return; }
                if (!isComOpen || !serialPort1.IsOpen) return;
                serialPort1.Write(GetModbusCommand(), 0, 8);
                nSend++; tsslSend.Text = "发送: " + nSend;
            }
            catch (Exception ex) { nError++; tsslError.Text = "错误: " + nError; LogError("发送数据失败: " + ex.Message); }
        }

        private byte[] GetModbusCommand()
        {
            switch (cbReadMode.SelectedIndex)
            {
                case 0: return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x44, 0x09 };
                case 1: return new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x04, 0x45, 0xE1 };
                case 2: return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B };
                case 3: return new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02, 0x65, 0xCB };
                case 4: return new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x01, 0x85, 0xE2 };
                case 5: return new byte[] { 0x01, 0x03, 0x00, 0x81, 0x00, 0x01, 0xD4, 0x22 };
                default: return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x44, 0x09 };
            }
        }

        private void SimulateData()
        {
            simTemp += (float)(simRandom.NextDouble() * 3.0 - 1.5);
            simTemp = Math.Max(-20, Math.Min(80, simTemp));
            simHumi += (float)(simRandom.NextDouble() * 6.0 - 3.0);
            simHumi = Math.Max(5, Math.Min(98, simHumi));
            nSend++; nReceive++;
            tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive;
            lastReceiveTime = DateTime.Now;
            AddDataPoint(simTemp, simHumi);
            if (chkEnableAlarm.Checked) CheckAlarm(simTemp, simHumi);
            if (chkDataLog.Checked) LogDataToFile(simTemp, simHumi);
            SaveToDatabase(simTemp, simHumi, true);
            this.BeginInvoke(new Action(() => updateUI(simTemp, simHumi)));
        }

        private void btnManualSend_Click(object sender, EventArgs e)
        {
            if (!isSimMode && (!isComOpen || !serialPort1.IsOpen)) { ShowTip("请先打开串口或启用模拟模式"); return; }
            sendData();
        }

        private void chkSimMode_CheckedChanged(object sender, EventArgs e)
        {
            isSimMode = chkSimMode.Checked;
            if (isSimMode)
            {
                cbComPort.Enabled = false; btnRefreshPorts.Enabled = false; cbBaudRate.Enabled = false;
                if (isComOpen) closeComPort();
                btnOpenCloseCom.Enabled = false;
                timer1.Interval = (int)nudInterval.Value; timer1.Start();
                tsslStatus.Text = "● 模拟模式 - 演示数据"; tsslStatus.ForeColor = Color.Orange;
                lblStatus.Text = "模拟模式运行中"; lblStatus.ForeColor = Color.Orange;
            }
            else
            {
                timer1.Stop(); btnOpenCloseCom.Enabled = true;
                cbComPort.Enabled = true; btnRefreshPorts.Enabled = true; cbBaudRate.Enabled = true;
                tsslStatus.Text = "● 串口未打开"; tsslStatus.ForeColor = Color.Gray;
                lblStatus.Text = "就绪"; lblStatus.ForeColor = Color.Gray;
            }
        }

        // ==================== 接收数据 ====================
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int n = serialPort1.BytesToRead;
                if (n <= 0) return;
                byte[] buf = new byte[n];
                serialPort1.Read(buf, 0, n);
                receiveBuffer.AddRange(buf);
                while (receiveBuffer.Count >= 5)
                {
                    if (receiveBuffer[0] != 0x01 || receiveBuffer[1] != 0x03) { receiveBuffer.RemoveAt(0); continue; }
                    int dLen = receiveBuffer[2];
                    int tLen = 3 + dLen + 2;
                    if (receiveBuffer.Count < tLen) break;
                    byte[] frame = new byte[tLen];
                    receiveBuffer.CopyTo(0, frame, 0, tLen);
                    receiveBuffer.RemoveRange(0, tLen);
                    ProcessFrame(frame);
                }
            }
            catch (Exception ex) { LogError("接收异常: " + ex.Message); nError++; UpdateStatus(); }
        }

        private void serialPort1_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        { nError++; tsslError.Text = "错误: " + nError; LogError("串口错误: " + e.EventType); }

        private void ProcessFrame(byte[] buffer)
        {
            nReceive++; tsslRecv.Text = "接收: " + nReceive; lastReceiveTime = DateTime.Now;
            try
            {
                if (!checkData(buffer)) { nError++; tsslError.Text = "错误: " + nError; return; }
                float t, h;
                getTempHumi(buffer, out t, out h);
                if (t < -40 || t > 125 || h < 0 || h > 100)
                { LogError(string.Format("数据超范围: T={0:F1} H={1:F1}", t, h)); return; }
                AddDataPoint(t, h);
                if (chkEnableAlarm.Checked) CheckAlarm(t, h);
                if (chkDataLog.Checked) LogDataToFile(t, h);
                SaveToDatabase(t, h, false);
                this.BeginInvoke(new Action(() => updateUI(t, h)));
                this.BeginInvoke(new Action(() =>
                { lblStatus.Text = "数据正常 - " + lastReceiveTime.ToString("HH:mm:ss"); lblStatus.ForeColor = Color.Green; }));
            }
            catch (Exception ex)
            {
                nError++; tsslError.Text = "错误: " + nError; LogError("帧处理异常: " + ex.Message);
                this.BeginInvoke(new Action(() => { lblStatus.Text = "解析错误: " + ex.Message; lblStatus.ForeColor = Color.Red; }));
            }
        }

        private void AddDataPoint(float t, float h)
        {
            while (tempQueue.Count >= maxChartPoint) { tempQueue.Dequeue(); humiQueue.Dequeue(); if (timeQueue.Count > 0) timeQueue.Dequeue(); }
            tempQueue.Enqueue(t); humiQueue.Enqueue(h); timeQueue.Enqueue(DateTime.Now);
            if (t < tempMin) tempMin = t; if (t > tempMax) tempMax = t;
            if (h < humiMin) humiMin = h; if (h > humiMax) humiMax = h;
            tempSum += t; humiSum += h; dataCount++;
        }

        private void updateUI(float t, float h)
        {
            // 卡片数据
            lblCardTempValue.Text = string.Format("{0:F1}℃", t);
            lblCardHumiValue.Text = string.Format("{0:F1}%", h);
            lblUpdateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lblClockTop.Text = DateTime.Now.ToString("HH:mm:ss");
            UpdateChart();
            // 统计标签和卡片范围
            if (dataCount > 0)
            {
                lblCardTempRange.Text = string.Format("↑{0:F1}  ↓{1:F1}", tempMax, tempMin);
                lblCardHumiRange.Text = string.Format("↑{0:F1}  ↓{1:F1}", humiMax, humiMin);
                lblTempMin.Text = string.Format("最低:{0:F1}", tempMin);
                lblTempMax.Text = string.Format("最高:{0:F1}", tempMax);
                lblTempAvg.Text = string.Format("平均:{0:F1}", tempSum / dataCount);
                lblHumiMin.Text = string.Format("最低:{0:F1}", humiMin);
                lblHumiMax.Text = string.Format("最高:{0:F1}", humiMax);
                lblHumiAvg.Text = string.Format("平均:{0:F1}", humiSum / dataCount);
            }
            // DataGridView 添加行
            string status = "正常";
            if (chkEnableAlarm.Checked)
            {
                float tH = (float)nudTempHigh.Value, tL = (float)nudTempLow.Value;
                float hH = (float)nudHumiHigh.Value, hL = (float)nudHumiLow.Value;
                if (t > tH || t < tL || h > hH || h < hL) status = "报警";
            }
            dgvData.Rows.Insert(0, DateTime.Now.ToString("HH:mm:ss"), string.Format("{0:F1}", t), string.Format("{0:F1}", h), status);
            while (dgvData.Rows.Count > 100) dgvData.Rows.RemoveAt(dgvData.Rows.Count - 1);
        }

        private void UpdateChart()
        {
            chart1.Series["温度"].Points.Clear();
            chart1.Series["湿度"].Points.Clear();
            float[] ts = tempQueue.ToArray(), hs = humiQueue.ToArray();
            for (int i = 0; i < ts.Length; i++)
            { chart1.Series["温度"].Points.AddXY(i + 1, ts[i]); chart1.Series["湿度"].Points.AddXY(i + 1, hs[i]); }
            if (ts.Length > 0 && hs.Length > 0)
            {
                float min = Math.Min(ts.Min(), hs.Min()) - 5, max = Math.Max(ts.Max(), hs.Max()) + 5;
                chart1.ChartAreas["MainArea"].AxisY.Minimum = Math.Max(-50, min);
                chart1.ChartAreas["MainArea"].AxisY.Maximum = Math.Min(150, max);
                chart1.ChartAreas["MainArea"].RecalculateAxesScale();
            }
        }

        // ==================== 数据解析 ====================
        private void getTempHumi(byte[] buf, out float t, out float h)
        {
            t = lastTemp; h = lastHumi;
            switch (cbReadMode.SelectedIndex)
            {
                case 0:
                    { byte[] a = { buf[6], buf[5], buf[4], buf[3] }, b = { buf[10], buf[9], buf[8], buf[7] }; t = BitConverter.ToSingle(a, 0); h = BitConverter.ToSingle(b, 0); }
                    break;
                case 1:
                    { t = ((buf[3] << 8) | buf[4]) / 10.0f; h = ((buf[5] << 8) | buf[6]) / 10.0f; }
                    break;
                case 2:
                    { byte[] a = { buf[6], buf[5], buf[4], buf[3] }; t = BitConverter.ToSingle(a, 0); }
                    break;
                case 3:
                    { byte[] b = { buf[6], buf[5], buf[4], buf[3] }; h = BitConverter.ToSingle(b, 0); }
                    break;
                case 4:
                    { t = ((buf[3] << 8) | buf[4]) / 10.0f; }
                    break;
                case 5:
                    { h = ((buf[3] << 8) | buf[4]) / 10.0f; }
                    break;
            }
            lastTemp = t; lastHumi = h;
        }

        private bool checkData(byte[] buf)
        {
            if (buf == null || buf.Length < 5) return false;
            if (buf[0] != 0x01 || buf[1] != 0x03) return false;
            if (buf.Length != buf[2] + 5) return false;
            return checkCRC(buf);
        }

        private bool checkCRC(byte[] buf)
        {
            if (buf.Length < 2) return false;
            byte[] d = new byte[buf.Length - 2];
            Array.Copy(buf, d, d.Length);
            byte[] c = calcCRC(d);
            return c[0] == buf[buf.Length - 2] && c[1] == buf[buf.Length - 1];
        }

        private byte[] calcCRC(byte[] p)
        {
            uint crc = 0xffff;
            for (int i = 0; i < p.Length; i++)
            {
                crc ^= (uint)p[i] & 0x00ff;
                for (int j = 0; j < 8; j++)
                {
                    uint flag = crc & 0x01;
                    crc >>= 1;
                    if (flag != 0) crc ^= 0x0a001;
                }
            }
            return BitConverter.GetBytes(crc);
        }

        // ==================== 报警 ====================
        private void CheckAlarm(float t, float h)
        {
            bool alarm = false;
            List<string> list = new List<string>();
            float tH = (float)nudTempHigh.Value, tL = (float)nudTempLow.Value;
            float hH = (float)nudHumiHigh.Value, hL = (float)nudHumiLow.Value;
            if (t > tH) { list.Add(string.Format("温度过高:{0:F1}>{1:F1}", t, tH)); alarm = true; }
            if (t < tL) { list.Add(string.Format("温度过低:{0:F1}<{1:F1}", t, tL)); alarm = true; }
            if (h > hH) { list.Add(string.Format("湿度过高:{0:F1}>{1:F1}", h, hH)); alarm = true; }
            if (h < hL) { list.Add(string.Format("湿度过低:{0:F1}<{1:F1}", h, hL)); alarm = true; }
            if (alarm)
            {
                string msg = string.Join(";", list);
                this.BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = "报警: " + msg; lblStatus.ForeColor = Color.Red;
                    lblCardAlarmValue.Text = "报警"; lblCardAlarmValue.ForeColor = Color.FromArgb(255, 77, 79);
                }));
                LogAlarmToFile(msg);
            }
            else
            {
                this.BeginInvoke(new Action(() =>
                {
                    lblCardAlarmValue.Text = "正常"; lblCardAlarmValue.ForeColor = Color.FromArgb(82, 196, 26);
                }));
            }
        }

        // ==================== 数据记录 ====================
        private void InitLogFile()
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                logFilePath = Path.Combine(dir, string.Format("data_{0:yyyyMMdd}.csv", DateTime.Now));
            }
            catch (Exception ex) { LogError("初始化日志失败: " + ex.Message); }
        }

        private void LogDataToFile(float t, float h)
        {
            try
            {
                string dir = Path.GetDirectoryName(logFilePath);
                string f = Path.Combine(dir, string.Format("data_{0:yyyyMMdd}.csv", DateTime.Now));
                bool newFile = f != logFilePath;
                logFilePath = f;
                using (StreamWriter sw = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    if (newFile || new FileInfo(logFilePath).Length == 0)
                        sw.WriteLine("时间,温度(℃),湿度(%)");
                    sw.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss},{1:F1},{2:F1}", DateTime.Now, t, h));
                }
            }
            catch { }
        }

        private void LogAlarmToFile(string msg)
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, string.Format("alarm_{0:yyyyMMdd}.log", DateTime.Now)),
                    string.Format("[{0:HH:mm:ss}] {1}\n", DateTime.Now, msg), Encoding.UTF8);
            }
            catch { }
        }

        private void LogError(string msg)
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, string.Format("error_{0:yyyyMMdd}.log", DateTime.Now)),
                    string.Format("[{0:HH:mm:ss}] {1}\n", DateTime.Now, msg), Encoding.UTF8);
            }
            catch { }
        }

        // ==================== 数据库存储 ====================
        private void InitDatabase()
        {
            try
            {
                string dbPath = GetDbPath();
                using (var conn = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;"))
                {
                    conn.Open();
                    string sql = @"CREATE TABLE IF NOT EXISTS sensor_data (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        timestamp TEXT NOT NULL,
                        temperature REAL,
                        humidity REAL,
                        read_mode TEXT,
                        is_simulated INTEGER DEFAULT 0,
                        is_alarm INTEGER DEFAULT 0,
                        alarm_msg TEXT);";
                    using (var cmd = new SQLiteCommand(sql, conn))
                        cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private void SaveToDatabase(float temp, float humi, bool isSim)
        {
            try
            {
                bool isAlarm = false;
                string alarmMsg = null;
                if (chkEnableAlarm.Checked)
                {
                    float tH = (float)nudTempHigh.Value, tL = (float)nudTempLow.Value;
                    float hH = (float)nudHumiHigh.Value, hL = (float)nudHumiLow.Value;
                    if (temp > tH || temp < tL || humi > hH || humi < hL)
                    { isAlarm = true; alarmMsg = string.Format("T:{0:F1}/H:{1:F1}", temp, humi); }
                }

                using (var conn = new SQLiteConnection("Data Source=" + GetDbPath() + ";Version=3;"))
                {
                    conn.Open();
                    string sql = @"INSERT INTO sensor_data (timestamp, temperature, humidity, read_mode, is_simulated, is_alarm, alarm_msg)
                                   VALUES (@ts, @t, @h, @mode, @sim, @alarm, @msg)";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@t", temp);
                        cmd.Parameters.AddWithValue("@h", humi);
                        cmd.Parameters.AddWithValue("@mode", cbReadMode.Text);
                        cmd.Parameters.AddWithValue("@sim", isSim ? 1 : 0);
                        cmd.Parameters.AddWithValue("@alarm", isAlarm ? 1 : 0);
                        cmd.Parameters.AddWithValue("@msg", (object)alarmMsg ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        // ==================== 导出CSV ====================
        private void btnExportCSV_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV文件 (*.csv)|*.csv"; sfd.FileName = string.Format("温湿度_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                        {
                            sw.WriteLine("序号,温度(℃),湿度(%)");
                            float[] ts = tempQueue.ToArray(), hs = humiQueue.ToArray();
                            for (int i = 0; i < ts.Length; i++) sw.WriteLine("{0},{1:F1},{2:F1}", i + 1, ts[i], hs[i]);
                        }
                        ShowTip(string.Format("已导出{0}条记录", tempQueue.Count));
                    }
                }
            }
            catch (Exception ex) { ShowTip("导出失败: " + ex.Message); }
        }

        // ==================== 清除 ====================
        private void btnClearChart_Click(object sender, EventArgs e) { ClearAllData(); }
        private void btnClearStats_Click(object sender, EventArgs e) { ClearStats(); }

        private void ClearAllData()
        {
            tempQueue.Clear(); humiQueue.Clear(); timeQueue.Clear(); ClearStats();
            chart1.Series["温度"].Points.Clear(); chart1.Series["湿度"].Points.Clear();
            dgvData.Rows.Clear();
            lblCardTempValue.Text = "--.-℃"; lblCardHumiValue.Text = "--.-%";
        }

        private void ClearStats()
        {
            dataCount = 0;
            tempMin = float.MaxValue; tempMax = float.MinValue;
            humiMin = float.MaxValue; humiMax = float.MinValue;
            tempSum = 0; humiSum = 0;
            lblTempMin.Text = "最低:--"; lblTempMax.Text = "最高:--"; lblTempAvg.Text = "平均:--";
            lblHumiMin.Text = "最低:--"; lblHumiMax.Text = "最高:--"; lblHumiAvg.Text = "平均:--";
        }

        // ==================== 设置变更 ====================
        private void cbReadMode_SelectedIndexChanged(object sender, EventArgs e)
        { Settings.Default.ReadMode = cbReadMode.SelectedIndex; Settings.Default.Save(); }

        private void nudInterval_ValueChanged(object sender, EventArgs e)
        { Settings.Default.SampleInterval = (int)nudInterval.Value; Settings.Default.Save(); if (timer1 != null && isComOpen) timer1.Interval = (int)nudInterval.Value; }

        private void nudMaxPoints_ValueChanged(object sender, EventArgs e)
        {
            maxChartPoint = (int)nudMaxPoints.Value; Settings.Default.MaxChartPoints = maxChartPoint; Settings.Default.Save();
            while (tempQueue.Count > maxChartPoint) { tempQueue.Dequeue(); humiQueue.Dequeue(); if (timeQueue.Count > 0) timeQueue.Dequeue(); }
        }

        private void chkEnableAlarm_CheckedChanged(object sender, EventArgs e)
        { Settings.Default.EnableAlarm = chkEnableAlarm.Checked; Settings.Default.Save(); }

        private void chkDataLog_CheckedChanged(object sender, EventArgs e)
        { Settings.Default.EnableDataLog = chkDataLog.Checked; Settings.Default.Save(); }

        // ==================== 设置持久化 ====================
        private void LoadSettings()
        { try { Settings.Default.Upgrade(); Settings.Default.Reload(); } catch { } }

        private void SaveAllSettings()
        {
            try
            {
                Settings.Default.BaudRate = int.Parse(cbBaudRate.Text);
                Settings.Default.SampleInterval = (int)nudInterval.Value;
                Settings.Default.MaxChartPoints = (int)nudMaxPoints.Value;
                Settings.Default.ReadMode = cbReadMode.SelectedIndex;
                Settings.Default.TempHighAlarm = (float)nudTempHigh.Value;
                Settings.Default.TempLowAlarm = (float)nudTempLow.Value;
                Settings.Default.HumiHighAlarm = (float)nudHumiHigh.Value;
                Settings.Default.HumiLowAlarm = (float)nudHumiLow.Value;
                Settings.Default.EnableAlarm = chkEnableAlarm.Checked;
                Settings.Default.EnableDataLog = chkDataLog.Checked;
                Settings.Default.Save();
            }
            catch { }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        { SaveAllSettings(); if (isComOpen) closeComPort(); }

        // ==================== 辅助 ====================
        private void ShowTip(string msg) { MessageBox.Show(msg, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); }

        private void UpdateStatus()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => { tsslError.Text = "错误: " + nError; tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive; }));
            else { tsslError.Text = "错误: " + nError; tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive; }
        }
    }
}
