using System;
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
        private Queue<float> tempQueue, humiQueue, pressureQueue;
        private Queue<DateTime> timeQueue;
        private float tempMin = float.MaxValue, tempMax = float.MinValue, tempSum = 0;
        private float humiMin = float.MaxValue, humiMax = float.MinValue, humiSum = 0;
        private float pressureMin = float.MaxValue, pressureMax = float.MinValue, pressureSum = 0;
        private int dataCount = 0;
        private List<byte> receiveBuffer = new List<byte>();
        private string logFilePath;
        private bool isComOpen = false;
        private bool isSimMode = false;
        private DateTime lastReceiveTime = DateTime.MinValue;
        private Random simRandom = new Random();
        private float simTemp = 25.0f, simHumi = 55.0f, simPressure = 101.3f;
        private float lastTemp = 0, lastHumi = 0, lastPressure = 101.3f;
        // 缓存报警阈值，避免后台线程直接访问 NumericUpDown
        private int readModeIndex = 0;
        private float alarmTempH = 40, alarmTempL = 0;
        private float alarmHumiH = 80, alarmHumiL = 20;
        private float alarmPressH = 110, alarmPressL = 90;
        private bool alarmEnabled = false, dataLogEnabled = true;
        private bool isReading = false;

        // ==================== 构造函数 ====================
        public MainForm()
        {
            tempQueue = new Queue<float>();
            humiQueue = new Queue<float>();
            pressureQueue = new Queue<float>();
            timeQueue = new Queue<DateTime>();

            // 所有控件和事件在 Designer.cs InitializeComponent() 中创建/绑定
            InitializeComponent();

            // 运行时：Chart + Timer/SerialPort + 初始化
            if (!IsDesignMode())
            {
                InitRuntime();
            }
        }

        private bool IsDesignMode()
        {
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
                return true;
            if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv")
                return true;
            return DesignMode;
        }


        private void InitRuntime()
        {
            initControls();
            LoadSettings();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            // 预置起始点确保坐标轴始终可见
            chart1.Series["温度"].Points.AddXY(1, 0);
            chart1.Series["湿度"].Points.AddXY(1, 0);
            chart1.Series["气压"].Points.AddXY(1, 0);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance = 300;
            splitContainer1.Panel1MinSize = 260;
            splitContainer1.Panel2MinSize = 200;
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
                readModeIndex = Settings.Default.ReadMode;
                cbReadMode.SelectedIndex = readModeIndex;
                nudTempHigh.Value = (decimal)Settings.Default.TempHighAlarm;
                nudTempLow.Value = (decimal)Settings.Default.TempLowAlarm;
                nudHumiHigh.Value = (decimal)Settings.Default.HumiHighAlarm;
                nudHumiLow.Value = (decimal)Settings.Default.HumiLowAlarm;
                nudPressureHigh.Value = (decimal)Settings.Default.PressureHighAlarm;
                nudPressureLow.Value = (decimal)Settings.Default.PressureLowAlarm;
                chkEnableAlarm.Checked = Settings.Default.EnableAlarm;
                alarmEnabled = chkEnableAlarm.Checked;
                chkDataLog.Checked = Settings.Default.EnableDataLog;
                dataLogEnabled = chkDataLog.Checked;
                alarmTempH = (float)nudTempHigh.Value;
                alarmTempL = (float)nudTempLow.Value;
                alarmHumiH = (float)nudHumiHigh.Value;
                alarmHumiL = (float)nudHumiLow.Value;
                alarmPressH = (float)nudPressureHigh.Value;
                alarmPressL = (float)nudPressureLow.Value;
                maxChartPoint = Settings.Default.MaxChartPoints;
            }
            catch { }

            InitLogFile();
            dtpStart.Value = DateTime.Today;
            dtpEnd.Value = DateTime.Now;
            SwitchTab(true);

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
                timer1.Interval = (int)nudInterval.Value;
                isReading = true; UpdateToggleButton();
                btnToggleRead.Text = "■ 停止采集"; btnToggleRead.BackColor = Color.LightCoral;
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
                isComOpen = false; isReading = false;
                btnOpenCloseCom.Text = "打开串口"; btnOpenCloseCom.BackColor = SystemColors.Control;
                btnToggleRead.Enabled = false; btnToggleRead.Text = "▶ 开始采集";
                btnToggleRead.BackColor = SystemColors.Control;
                tsslStatus.Text = "● 串口已关闭"; tsslStatus.ForeColor = Color.Gray;
                cbComPort.Enabled = true; btnRefreshPorts.Enabled = true; cbBaudRate.Enabled = true;
                lblStatus.Text = "串口已关闭"; lblStatus.ForeColor = Color.Gray;
            }
            catch (Exception ex) { LogError("关闭串口失败: " + ex.Message); }
        }

        // ==================== 定时器 ====================
        private void timer1_Tick(object sender, EventArgs e) { if (isReading) sendData(); }

        // ==================== 发送数据 ====================
        private void sendData()
        {
            try
            {
                if (isSimMode) { SimulateData(); return; }
                if (!isComOpen || !serialPort1.IsOpen) return;
                byte[] cmd = GetModbusCommand();
                serialPort1.Write(cmd, 0, cmd.Length);
                nSend++; tsslSend.Text = "发送: " + nSend;
            }
            catch (Exception ex) { nError++; tsslError.Text = "错误: " + nError; LogError("发送数据失败: " + ex.Message); }
        }

        private byte[] GetModbusCommand()
        {
            switch (readModeIndex)
            {
                case 0: return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x44, 0x09 };
                case 1: return new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x04, 0x45, 0xE1 };
                case 2: return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B };
                case 3: return new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02, 0x65, 0xCB };
                case 4: return new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x01, 0x85, 0xE2 };
                case 5: return new byte[] { 0x01, 0x03, 0x00, 0x81, 0x00, 0x01, 0xD4, 0x22 };
                case 6: return new byte[] { 0x01, 0x03, 0x00, 0x04, 0x00, 0x02, 0x85, 0xCA };
                case 7: return new byte[] { 0x01, 0x03, 0x00, 0x82, 0x00, 0x01, 0x24, 0x22 };
                case 8: return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x06, 0xC5, 0xC8 };
                case 9: return new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x04, 0x45, 0xE1 };
                default: return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x44, 0x09 };
            }
        }

        private void SimulateData()
        {
            simTemp += (float)(simRandom.NextDouble() * 3.0 - 1.5);
            simTemp = Math.Max(-20, Math.Min(80, simTemp));
            simHumi += (float)(simRandom.NextDouble() * 6.0 - 3.0);
            simHumi = Math.Max(5, Math.Min(98, simHumi));
            simPressure += (float)(simRandom.NextDouble() * 2.0 - 1.0);
            simPressure = Math.Max(95, Math.Min(110, simPressure));
            nSend++; nReceive++;
            tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive;
            lastReceiveTime = DateTime.Now;
            AddDataPoint(simTemp, simHumi, simPressure);
            if (alarmEnabled) CheckAlarm(simTemp, simHumi, simPressure);
            if (dataLogEnabled) LogDataToFile(simTemp, simHumi, simPressure);
            SaveToDatabase(simTemp, simHumi, simPressure);
            this.BeginInvoke(new Action(() => updateUI(simTemp, simHumi, simPressure)));
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
                timer1.Interval = (int)nudInterval.Value;
                isReading = true; UpdateToggleButton();
                btnToggleRead.Text = "■ 停止采集"; btnToggleRead.BackColor = Color.LightCoral;
                tsslStatus.Text = "● 模拟模式 - 演示数据"; tsslStatus.ForeColor = Color.Orange;
                lblStatus.Text = "模拟模式运行中"; lblStatus.ForeColor = Color.Orange;
            }
            else
            {
                timer1.Stop(); isReading = false; btnOpenCloseCom.Enabled = true;
                btnToggleRead.Enabled = false; btnToggleRead.Text = "▶ 开始采集";
                btnToggleRead.BackColor = SystemColors.Control;
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
        {
            nError++;
            LogError("串口错误: " + e.EventType);
            this.BeginInvoke(new Action(() => { tsslError.Text = "错误: " + nError; }));
        }

        private void ProcessFrame(byte[] buffer)
        {
            nReceive++; lastReceiveTime = DateTime.Now;
            this.BeginInvoke(new Action(() => { tsslRecv.Text = "接收: " + nReceive; }));
            try
            {
                if (!checkData(buffer))
                {
                    nError++;
                    this.BeginInvoke(new Action(() => { tsslError.Text = "错误: " + nError; }));
                    return;
                }
                float t, h, p;
                getSensorData(buffer, out t, out h, out p);
                if (t < -40 || t > 125 || h < 0 || h > 100 || p < 50 || p > 200)
                { LogError(string.Format("数据超范围: T={0:F1} H={1:F1} P={2:F1}", t, h, p)); return; }
                AddDataPoint(t, h, p);
                if (alarmEnabled) CheckAlarm(t, h, p);
                if (dataLogEnabled) LogDataToFile(t, h, p);
                SaveToDatabase(t, h, p);
                this.BeginInvoke(new Action(() => updateUI(t, h, p)));
                this.BeginInvoke(new Action(() =>
                { lblStatus.Text = "数据正常 - " + lastReceiveTime.ToString("HH:mm:ss"); lblStatus.ForeColor = Color.Green; }));
            }
            catch (Exception ex)
            {
                nError++;
                LogError("帧处理异常: " + ex.Message);
                this.BeginInvoke(new Action(() =>
                {
                    tsslError.Text = "错误: " + nError;
                    lblStatus.Text = "解析错误: " + ex.Message;
                    lblStatus.ForeColor = Color.Red;
                }));
            }
        }

        private void AddDataPoint(float t, float h, float p)
        {
            while (tempQueue.Count >= maxChartPoint) { tempQueue.Dequeue(); humiQueue.Dequeue(); pressureQueue.Dequeue(); if (timeQueue.Count > 0) timeQueue.Dequeue(); }
            tempQueue.Enqueue(t); humiQueue.Enqueue(h); pressureQueue.Enqueue(p); timeQueue.Enqueue(DateTime.Now);
            if (t < tempMin) tempMin = t; if (t > tempMax) tempMax = t;
            if (h < humiMin) humiMin = h; if (h > humiMax) humiMax = h;
            if (p < pressureMin) pressureMin = p; if (p > pressureMax) pressureMax = p;
            tempSum += t; humiSum += h; pressureSum += p; dataCount++;
        }

        private void updateUI(float t, float h, float p)
        {
            lblTempValue.Text = string.Format("{0:F1} ℃", t);
            lblHumiValue.Text = string.Format("{0:F1} %", h);
            lblPressureValue.Text = string.Format("{0:F1} kPa", p);
            lblUpdateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UpdateChart();
            if (dataCount > 0)
            {
                lblTempMin.Text = string.Format("{0:F1} ℃", tempMin);
                lblTempMax.Text = string.Format("{0:F1} ℃", tempMax);
                lblTempAvg.Text = string.Format("{0:F1} ℃", tempSum / dataCount);
                lblHumiMin.Text = string.Format("{0:F1} %", humiMin);
                lblHumiMax.Text = string.Format("{0:F1} %", humiMax);
                lblHumiAvg.Text = string.Format("{0:F1} %", humiSum / dataCount);
                lblPressureMin.Text = string.Format("{0:F1} kPa", pressureMin);
                lblPressureMax.Text = string.Format("{0:F1} kPa", pressureMax);
                lblPressureAvg.Text = string.Format("{0:F1} kPa", pressureSum / dataCount);
            }
        }

        private void UpdateChart()
        {
            chart1.Series["温度"].Points.Clear();
            chart1.Series["湿度"].Points.Clear();
            chart1.Series["气压"].Points.Clear();
            float[] ts = tempQueue.ToArray(), hs = humiQueue.ToArray(), ps = pressureQueue.ToArray();
            for (int i = 0; i < ts.Length; i++)
            {
                chart1.Series["温度"].Points.AddXY(i + 1, ts[i]);
                chart1.Series["湿度"].Points.AddXY(i + 1, hs[i]);
                chart1.Series["气压"].Points.AddXY(i + 1, ps[i]);
            }
            if (ts.Length > 0)
            {
                float min = Math.Min(Math.Min(ts.Min(), hs.Min()), ps.Min()) - 5;
                float max = Math.Max(Math.Max(ts.Max(), hs.Max()), ps.Max()) + 5;
                chart1.ChartAreas["MainArea"].AxisY.Minimum = Math.Max(-50, min);
                chart1.ChartAreas["MainArea"].AxisY.Maximum = Math.Min(200, max);
                chart1.ChartAreas["MainArea"].RecalculateAxesScale();
            }
        }

        // ==================== 数据解析 ====================
        private void getSensorData(byte[] buf, out float t, out float h, out float p)
        {
            t = lastTemp; h = lastHumi; p = lastPressure;
            switch (readModeIndex)
            {
                case 0: // 温湿浮点
                    { byte[] a = { buf[6], buf[5], buf[4], buf[3] }, b = { buf[10], buf[9], buf[8], buf[7] }; t = BitConverter.ToSingle(a, 0); h = BitConverter.ToSingle(b, 0); }
                    break;
                case 1: // 温湿整型
                    { t = ((buf[3] << 8) | buf[4]) / 10.0f; h = ((buf[5] << 8) | buf[6]) / 10.0f; }
                    break;
                case 2: // 温度浮点
                    { byte[] a = { buf[6], buf[5], buf[4], buf[3] }; t = BitConverter.ToSingle(a, 0); }
                    break;
                case 3: // 湿度浮点
                    { byte[] b = { buf[6], buf[5], buf[4], buf[3] }; h = BitConverter.ToSingle(b, 0); }
                    break;
                case 4: // 温度整型
                    { t = ((buf[3] << 8) | buf[4]) / 10.0f; }
                    break;
                case 5: // 湿度整型
                    { h = ((buf[3] << 8) | buf[4]) / 10.0f; }
                    break;
                case 6: // 气压浮点
                    { byte[] c = { buf[6], buf[5], buf[4], buf[3] }; p = BitConverter.ToSingle(c, 0); }
                    break;
                case 7: // 气压整型
                    { p = ((buf[3] << 8) | buf[4]); }
                    break;
                case 8: // 温湿压浮点
                    { byte[] a = { buf[6], buf[5], buf[4], buf[3] }, b = { buf[10], buf[9], buf[8], buf[7] }, c = { buf[14], buf[13], buf[12], buf[11] }; t = BitConverter.ToSingle(a, 0); h = BitConverter.ToSingle(b, 0); p = BitConverter.ToSingle(c, 0); }
                    break;
                case 9: // 温湿压整型
                    { t = ((buf[3] << 8) | buf[4]) / 10.0f; h = ((buf[5] << 8) | buf[6]) / 10.0f; p = ((buf[7] << 8) | buf[8]); }
                    break;
            }
            lastTemp = t; lastHumi = h; lastPressure = p;
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
                    if (flag != 0) crc ^= 0xA001;
                }
            }
            return BitConverter.GetBytes(crc);
        }

        // ==================== 报警 ====================
        private void CheckAlarm(float t, float h, float p)
        {
            if (!alarmEnabled) return;
            bool alarm = false;
            List<string> list = new List<string>();
            if (t > alarmTempH) { list.Add(string.Format("温度高:{0:F1}", t, alarmTempH)); alarm = true; }
            if (t < alarmTempL) { list.Add(string.Format("温度低:{0:F1}", t, alarmTempL)); alarm = true; }
            if (h > alarmHumiH) { list.Add(string.Format("湿度高:{0:F1}", h, alarmHumiH)); alarm = true; }
            if (h < alarmHumiL) { list.Add(string.Format("湿度低:{0:F1}", h, alarmHumiL)); alarm = true; }
            if (p > alarmPressH) { list.Add(string.Format("气压高:{0:F1}", p, alarmPressH)); alarm = true; }
            if (p < alarmPressL) { list.Add(string.Format("气压低:{0:F1}", p, alarmPressL)); alarm = true; }
            if (alarm)
            {
                string msg = string.Join(";", list);
                this.BeginInvoke(new Action(() =>
                { lblStatus.Text = "报警: " + msg; lblStatus.ForeColor = Color.Red; }));
                LogAlarmToFile(msg);
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

        private void LogDataToFile(float t, float h, float p)
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
                        sw.WriteLine("时间,温度(℃),湿度(%),气压(kPa)");
                    sw.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss},{1:F1},{2:F1},{3:F1}", DateTime.Now, t, h, p));
                }
            }
            catch { }
        }

        private string GetDbPath()
        {
            string dbPath = Path.Combine(Application.StartupPath, "TempHumidityData.db");
            // 首次运行或clean build后从项目根目录复制模板数据库
            if (!File.Exists(dbPath))
            {
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\TempHumidityData.db");
                if (File.Exists(templatePath))
                    File.Copy(templatePath, dbPath);
            }
            return dbPath;
        }

        private void SaveToDatabase(float t, float h, float p)
        {
            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + GetDbPath() + ";Version=3;"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO sensor_data (timestamp, temperature, humidity, pressure, read_mode, is_simulated, is_alarm, alarm_msg)
                                           VALUES (@ts, @t, @h, @p, @mode, @sim, 0, '')";
                        cmd.Parameters.AddWithValue("@ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@t", t);
                        cmd.Parameters.AddWithValue("@h", h);
                        cmd.Parameters.AddWithValue("@p", p);
                        cmd.Parameters.AddWithValue("@mode", readModeIndex.ToString());
                        cmd.Parameters.AddWithValue("@sim", isSimMode ? 1 : 0);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("数据库写入失败: " + ex.Message);
            }
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
                            sw.WriteLine("序号,温度(℃),湿度(%),气压(kPa)");
                            float[] ts = tempQueue.ToArray(), hs = humiQueue.ToArray(), ps = pressureQueue.ToArray();
                            for (int i = 0; i < ts.Length; i++) sw.WriteLine("{0},{1:F1},{2:F1},{3:F1}", i + 1, ts[i], hs[i], ps[i]);
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
            tempQueue.Clear(); humiQueue.Clear(); pressureQueue.Clear(); timeQueue.Clear(); ClearStats();
            chart1.Series["温度"].Points.Clear(); chart1.Series["湿度"].Points.Clear(); chart1.Series["气压"].Points.Clear();
            lblTempValue.Text = "--.- ℃"; lblHumiValue.Text = "--.- %"; lblPressureValue.Text = "---.- kPa"; lblUpdateTime.Text = "--";
        }

        private void ClearStats()
        {
            dataCount = 0;
            tempMin = float.MaxValue; tempMax = float.MinValue;
            humiMin = float.MaxValue; humiMax = float.MinValue;
            pressureMin = float.MaxValue; pressureMax = float.MinValue;
            tempSum = 0; humiSum = 0; pressureSum = 0;
            lblTempMin.Text = "--.- ℃"; lblTempMax.Text = "--.- ℃"; lblTempAvg.Text = "--.- ℃";
            lblHumiMin.Text = "--.- %"; lblHumiMax.Text = "--.- %"; lblHumiAvg.Text = "--.- %";
            lblPressureMin.Text = "--.- kPa"; lblPressureMax.Text = "--.- kPa"; lblPressureAvg.Text = "--.- kPa";
        }

        // ==================== 设置变更 ====================
        private void cbReadMode_SelectedIndexChanged(object sender, EventArgs e)
        { readModeIndex = cbReadMode.SelectedIndex; Settings.Default.ReadMode = readModeIndex; Settings.Default.Save(); }

        private void nudInterval_ValueChanged(object sender, EventArgs e)
        { Settings.Default.SampleInterval = (int)nudInterval.Value; Settings.Default.Save(); if (timer1 != null && isComOpen) timer1.Interval = (int)nudInterval.Value; }

        private void nudMaxPoints_ValueChanged(object sender, EventArgs e)
        {
            maxChartPoint = (int)nudMaxPoints.Value; Settings.Default.MaxChartPoints = maxChartPoint; Settings.Default.Save();
            while (tempQueue.Count > maxChartPoint) { tempQueue.Dequeue(); humiQueue.Dequeue(); pressureQueue.Dequeue(); if (timeQueue.Count > 0) timeQueue.Dequeue(); }
        }

        private void chkEnableAlarm_CheckedChanged(object sender, EventArgs e)
        { alarmEnabled = chkEnableAlarm.Checked; Settings.Default.EnableAlarm = alarmEnabled; Settings.Default.Save(); }

        private void chkDataLog_CheckedChanged(object sender, EventArgs e)
        { dataLogEnabled = chkDataLog.Checked; Settings.Default.EnableDataLog = dataLogEnabled; Settings.Default.Save(); }

        private void SyncAlarmThresholds(object sender, EventArgs e)
        {
            alarmTempH = (float)nudTempHigh.Value;
            alarmTempL = (float)nudTempLow.Value;
            alarmHumiH = (float)nudHumiHigh.Value;
            alarmHumiL = (float)nudHumiLow.Value;
            alarmPressH = (float)nudPressureHigh.Value;
            alarmPressL = (float)nudPressureLow.Value;
        }

        // ==================== 设置持久化 ====================
        private void LoadSettings()
        { try { Settings.Default.Reload(); } catch { } }

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
                Settings.Default.PressureHighAlarm = (float)nudPressureHigh.Value;
                Settings.Default.PressureLowAlarm = (float)nudPressureLow.Value;
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

        // ==================== Tab 切换 ====================
        private void btnTabCurrent_Click(object sender, EventArgs e) { SwitchTab(true); }
        private void btnTabHistory_Click(object sender, EventArgs e) { SwitchTab(false); }

        private void SwitchTab(bool isCurrent)
        {
            btnTabCurrent.BackColor = isCurrent ? Color.SteelBlue : SystemColors.Control;
            btnTabCurrent.ForeColor = isCurrent ? Color.White : Color.Black;
            btnTabHistory.BackColor = !isCurrent ? Color.SteelBlue : SystemColors.Control;
            btnTabHistory.ForeColor = !isCurrent ? Color.White : Color.Black;
            chart1.Visible = isCurrent;
            gbCurrent.Visible = isCurrent;
            gbStatsTemp.Visible = isCurrent;
            gbStatsHumi.Visible = isCurrent;
            gbStatsPress.Visible = isCurrent;
            btnClearStats.Visible = isCurrent;
            gbHistory.Visible = !isCurrent;
        }

        // ==================== 启停采集 ====================
        private void btnToggleRead_Click(object sender, EventArgs e)
        {
            if (!isComOpen && !isSimMode) return;
            isReading = !isReading;
            if (isReading)
            {
                btnToggleRead.Text = "■ 停止采集";
                btnToggleRead.BackColor = Color.LightCoral;
                timer1.Start();
                lblStatus.Text = "正在采集数据...";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                btnToggleRead.Text = "▶ 开始采集";
                btnToggleRead.BackColor = Color.LightGreen;
                timer1.Stop();
                lblStatus.Text = "采集已停止";
                lblStatus.ForeColor = Color.Gray;
            }
        }

        private void UpdateToggleButton()
        {
            btnToggleRead.Enabled = (isComOpen || isSimMode);
            if (isReading && !timer1.Enabled)
                timer1.Start();
        }

        // ==================== 历史查询 ====================
        private void btnQueryHistory_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + GetDbPath() + ";Version=3;"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT timestamp as 时间, temperature as 温度, humidity as 湿度,
                            pressure as 气压, CASE WHEN is_simulated=1 THEN '模拟' ELSE '实时' END as 来源
                            FROM sensor_data WHERE timestamp BETWEEN @s AND @e ORDER BY timestamp DESC LIMIT 2000";
                        cmd.Parameters.AddWithValue("@s", dtpStart.Value.ToString("yyyy-MM-dd") + " 00:00:00");
                        cmd.Parameters.AddWithValue("@e", dtpEnd.Value.ToString("yyyy-MM-dd") + " 23:59:59");
                        using (var adapter = new System.Data.SQLite.SQLiteDataAdapter(cmd))
                        {
                            var dt = new System.Data.DataTable();
                            adapter.Fill(dt);
                            dgvHistory.DataSource = dt;
                        }
                    }
                }
                lblStatus.Text = string.Format("查询到 {0} 条历史记录", dgvHistory.RowCount);
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "查询失败: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void btnExportHistory_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvHistory.Rows.Count == 0) { ShowTip("没有可导出的数据"); return; }
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV文件 (*.csv)|*.csv";
                    sfd.FileName = string.Format("历史数据_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                        {
                            for (int i = 0; i < dgvHistory.Columns.Count; i++)
                            {
                                sw.Write(dgvHistory.Columns[i].HeaderText);
                                if (i < dgvHistory.Columns.Count - 1) sw.Write(",");
                            }
                            sw.WriteLine();
                            foreach (DataGridViewRow row in dgvHistory.Rows)
                            {
                                for (int i = 0; i < dgvHistory.Columns.Count; i++)
                                {
                                    sw.Write(row.Cells[i].Value);
                                    if (i < dgvHistory.Columns.Count - 1) sw.Write(",");
                                }
                                sw.WriteLine();
                            }
                        }
                        ShowTip("已导出 " + dgvHistory.RowCount + " 条记录");
                    }
                }
            }
            catch (Exception ex) { ShowTip("导出失败: " + ex.Message); }
        }
    }
}
