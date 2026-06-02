using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

            // 绑定所有事件处理器
            WireUpEvents();

            // 设计模式下跳过运行时初始化
            if (!IsDesignMode())
            {
                InitAfterDesign();
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

        private void WireUpEvents()
        {
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
            btnRefreshPorts.Click += btnRefreshPorts_Click;
            btnOpenCloseCom.Click += btnOpenCloseCom_Click;
            btnManualSend.Click += btnManualSend_Click;
            btnClearStats.Click += btnClearStats_Click;
            btnExportCSV.Click += btnExportCSV_Click;
            btnClearChart.Click += btnClearChart_Click;
            chkSimMode.CheckedChanged += chkSimMode_CheckedChanged;
            chkEnableAlarm.CheckedChanged += chkEnableAlarm_CheckedChanged;
            chkDataLog.CheckedChanged += chkDataLog_CheckedChanged;
            cbReadMode.SelectedIndexChanged += cbReadMode_SelectedIndexChanged;
            nudInterval.ValueChanged += nudInterval_ValueChanged;
            nudMaxPoints.ValueChanged += nudMaxPoints_ValueChanged;
            timer1.Tick += timer1_Tick;
            serialPort1.DataReceived += serialPort1_DataReceived;
            serialPort1.ErrorReceived += serialPort1_ErrorReceived;
        }

        private void InitAfterDesign()
        {
            initControls();
            LoadSettings();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance = 285;
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

            Timer timeTimer = new Timer();
            timeTimer.Interval = 1000;
            timeTimer.Tick += (s, ev) => { tsslTime.Text = DateTime.Now.ToString("HH:mm:ss"); };
            timeTimer.Start();
        }

        // ==================== 串口操作 ====================
        private void RefreshComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            string prevSelection = cbComPort.SelectedItem?.ToString();
            cbComPort.Items.Clear();
            if (ports.Length > 0)
            {
                cbComPort.Items.AddRange(ports);
                cbComPort.Text = prevSelection != null && cbComPort.Items.Contains(prevSelection) ? prevSelection : cbComPort.Items[0].ToString();
            }
            else { cbComPort.Items.Add("无可用串口"); cbComPort.SelectedIndex = 0; }
        }

        private void btnRefreshPorts_Click(object sender, EventArgs e)
        {
            try { if (!isComOpen) RefreshComPorts(); else ShowTip("请先关闭串口再刷新端口列表"); }
            catch (Exception ex) { ShowTip("刷新串口失败: " + ex.Message); }
        }

        private void btnOpenCloseCom_Click(object sender, EventArgs e)
        {
            if (isComOpen) closeComPort(); else openComPort();
        }

        private void openComPort()
        {
            try
            {
                string portName = cbComPort.Text;
                if (string.IsNullOrEmpty(portName) || portName == "无可用串口") { ShowTip("请选择有效的串口号"); return; }

                serialPort1.PortName = portName;
                serialPort1.BaudRate = int.Parse(cbBaudRate.Text);
                serialPort1.DataBits = 8;
                serialPort1.StopBits = StopBits.One;
                serialPort1.Parity = Parity.None;
                serialPort1.ReadTimeout = 500;
                serialPort1.WriteTimeout = 500;
                serialPort1.Open();
                isComOpen = true;
                btnOpenCloseCom.Text = "关闭串口";
                btnOpenCloseCom.BackColor = Color.LightCoral;
                tsslStatus.Text = "● 串口已打开 - " + portName;
                tsslStatus.ForeColor = Color.Green;
                timer1.Interval = (int)nudInterval.Value;
                timer1.Start();
                cbComPort.Enabled = false;
                btnRefreshPorts.Enabled = false;
                cbBaudRate.Enabled = false;
                lblStatus.Text = "串口已打开，正在采集数据...";
                lblStatus.ForeColor = Color.Green;
                receiveBuffer.Clear();
            }
            catch (Exception ex) { ShowTip("打开串口失败: " + ex.Message); LogError("打开串口失败: " + ex.Message); }
        }

        private void closeComPort()
        {
            try
            {
                timer1.Stop();
                if (serialPort1.IsOpen) serialPort1.Close();
                isComOpen = false;
                btnOpenCloseCom.Text = "打开串口";
                btnOpenCloseCom.BackColor = SystemColors.Control;
                tsslStatus.Text = "● 串口已关闭";
                tsslStatus.ForeColor = Color.Gray;
                cbComPort.Enabled = true;
                btnRefreshPorts.Enabled = true;
                cbBaudRate.Enabled = true;
                lblStatus.Text = "串口已关闭";
                lblStatus.ForeColor = Color.Gray;
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
                byte[] cmd = GetModbusCommand();
                serialPort1.Write(cmd, 0, cmd.Length);
                nSend++;
                tsslSend.Text = "发送: " + nSend;
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
            float deltaT = (float)(simRandom.NextDouble() * 3.0 - 1.5);
            simTemp += deltaT; simTemp = Math.Max(-20, Math.Min(80, simTemp));
            float deltaH = (float)(simRandom.NextDouble() * 6.0 - 3.0);
            simHumi += deltaH; simHumi = Math.Max(5, Math.Min(98, simHumi));
            nSend++; nReceive++;
            tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive;
            lastReceiveTime = DateTime.Now;
            AddDataPoint(simTemp, simHumi);
            if (chkEnableAlarm.Checked) CheckAlarm(simTemp, simHumi);
            if (chkDataLog.Checked) LogDataToFile(simTemp, simHumi);
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
                lblStatus.Text = "模拟模式运行中，数据为随机模拟值"; lblStatus.ForeColor = Color.Orange;
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
                int bytesToRead = serialPort1.BytesToRead;
                if (bytesToRead <= 0) return;
                byte[] incoming = new byte[bytesToRead];
                serialPort1.Read(incoming, 0, bytesToRead);
                receiveBuffer.AddRange(incoming);
                while (receiveBuffer.Count >= 5)
                {
                    if (receiveBuffer[0] != 0x01 || receiveBuffer[1] != 0x03) { receiveBuffer.RemoveAt(0); continue; }
                    int dataLength = receiveBuffer[2];
                    int totalLength = 3 + dataLength + 2;
                    if (receiveBuffer.Count < totalLength) break;
                    byte[] frame = new byte[totalLength];
                    receiveBuffer.CopyTo(0, frame, 0, totalLength);
                    receiveBuffer.RemoveRange(0, totalLength);
                    ProcessFrame(frame);
                }
            }
            catch (Exception ex) { LogError("接收数据处理异常: " + ex.Message); nError++; UpdateStatus(); }
        }

        private void serialPort1_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            nError++; tsslError.Text = "错误: " + nError; LogError("串口错误: " + e.EventType);
        }

        // ==================== 数据处理 ====================
        private void ProcessFrame(byte[] buffer)
        {
            nReceive++; tsslRecv.Text = "接收: " + nReceive; lastReceiveTime = DateTime.Now;
            try
            {
                if (!checkData(buffer)) { nError++; tsslError.Text = "错误: " + nError; return; }
                float temp, humi;
                getTempHumi(buffer, out temp, out humi);
                if (temp < -40 || temp > 125 || humi < 0 || humi > 100)
                {
                    LogError(string.Format("数据超出合理范围: 温度={0:F1}, 湿度={1:F1}", temp, humi)); return;
                }
                AddDataPoint(temp, humi);
                if (chkEnableAlarm.Checked) CheckAlarm(temp, humi);
                if (chkDataLog.Checked) LogDataToFile(temp, humi);
                this.BeginInvoke(new Action(() => updateUI(temp, humi)));
                this.BeginInvoke(new Action(() => { lblStatus.Text = string.Format("数据正常 - {0}", lastReceiveTime.ToString("HH:mm:ss")); lblStatus.ForeColor = Color.Green; }));
            }
            catch (Exception ex)
            {
                nError++; tsslError.Text = "错误: " + nError; LogError("数据帧处理异常: " + ex.Message);
                this.BeginInvoke(new Action(() => { lblStatus.Text = "数据解析错误: " + ex.Message; lblStatus.ForeColor = Color.Red; }));
            }
        }

        private void AddDataPoint(float temp, float humi)
        {
            while (tempQueue.Count >= maxChartPoint) { tempQueue.Dequeue(); humiQueue.Dequeue(); if (timeQueue.Count > 0) timeQueue.Dequeue(); }
            tempQueue.Enqueue(temp); humiQueue.Enqueue(humi); timeQueue.Enqueue(DateTime.Now);
            if (temp < tempMin) tempMin = temp;
            if (temp > tempMax) tempMax = temp;
            if (humi < humiMin) humiMin = humi;
            if (humi > humiMax) humiMax = humi;
            tempSum += temp; humiSum += humi; dataCount++;
        }

        private void updateUI(float temp, float humi)
        {
            lblTempValue.Text = string.Format("{0:F1} ℃", temp);
            lblHumiValue.Text = string.Format("{0:F1} %", humi);
            lblUpdateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UpdateChart();
            if (dataCount > 0)
            {
                lblTempMin.Text = string.Format("最小: {0:F1}", tempMin);
                lblTempMax.Text = string.Format("最大: {0:F1}", tempMax);
                lblTempAvg.Text = string.Format("平均: {0:F1}", tempSum / dataCount);
                lblHumiMin.Text = string.Format("最小: {0:F1}", humiMin);
                lblHumiMax.Text = string.Format("最大: {0:F1}", humiMax);
                lblHumiAvg.Text = string.Format("平均: {0:F1}", humiSum / dataCount);
            }
        }

        // ==================== Chart 更新 ====================
        private void UpdateChart()
        {
            chart1.Series["温度"].Points.Clear();
            chart1.Series["湿度"].Points.Clear();
            float[] temps = tempQueue.ToArray();
            float[] humis = humiQueue.ToArray();
            for (int i = 0; i < temps.Length; i++)
            {
                chart1.Series["温度"].Points.AddXY(i + 1, temps[i]);
                chart1.Series["湿度"].Points.AddXY(i + 1, humis[i]);
            }
            if (temps.Length > 0 && humis.Length > 0)
            {
                float minVal = Math.Min(temps.Min(), humis.Min()) - 5;
                float maxVal = Math.Max(temps.Max(), humis.Max()) + 5;
                chart1.ChartAreas["MainArea"].AxisY.Minimum = Math.Max(-50, minVal);
                chart1.ChartAreas["MainArea"].AxisY.Maximum = Math.Min(150, maxVal);
                chart1.ChartAreas["MainArea"].RecalculateAxesScale();
            }
        }

        // ==================== 数据解析 ====================
        private void getTempHumi(byte[] buffer, out float temp, out float humi)
        {
            temp = lastTemp; humi = lastHumi;
            switch (cbReadMode.SelectedIndex)
            {
                case 0:
                    { byte[] a = new byte[4], b = new byte[4]; Array.Copy(buffer, 3, a, 0, 4); Array.Copy(buffer, 7, b, 0, 4); Array.Reverse(a); Array.Reverse(b); temp = BitConverter.ToSingle(a, 0); humi = BitConverter.ToSingle(b, 0); }
                    break;
                case 1:
                    { temp = ((buffer[3] << 8) | buffer[4]) / 10.0f; humi = ((buffer[5] << 8) | buffer[6]) / 10.0f; }
                    break;
                case 2:
                    { byte[] a = new byte[4]; Array.Copy(buffer, 3, a, 0, 4); Array.Reverse(a); temp = BitConverter.ToSingle(a, 0); }
                    break;
                case 3:
                    { byte[] b = new byte[4]; Array.Copy(buffer, 3, b, 0, 4); Array.Reverse(b); humi = BitConverter.ToSingle(b, 0); }
                    break;
                case 4:
                    { temp = ((buffer[3] << 8) | buffer[4]) / 10.0f; }
                    break;
                case 5:
                    { humi = ((buffer[3] << 8) | buffer[4]) / 10.0f; }
                    break;
            }
            lastTemp = temp; lastHumi = humi;
        }

        // ==================== 数据校验 ====================
        private bool checkData(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 5) return false;
            if (buffer[0] != 0x01 || buffer[1] != 0x03) return false;
            int dataLen = buffer[2];
            if (buffer.Length != dataLen + 5) return false;
            if (!checkCRC(buffer)) return false;
            return true;
        }

        private bool checkCRC(byte[] buffer)
        {
            if (buffer.Length < 2) return false;
            byte[] data = new byte[buffer.Length - 2];
            Array.Copy(buffer, data, data.Length);
            byte[] crc = calc_CRC(data);
            return crc[0] == buffer[buffer.Length - 2] && crc[1] == buffer[buffer.Length - 1];
        }

        private byte[] calc_CRC(byte[] ptbuf)
        {
            uint crc16 = 0xffff;
            uint temp, flag;
            for (int i = 0; i < ptbuf.Length; i++)
            {
                temp = (uint)ptbuf[i] & 0x00ff;
                crc16 = crc16 ^ temp;
                for (uint c = 0; c < 8; c++)
                {
                    flag = crc16 & 0x01;
                    crc16 = crc16 >> 1;
                    if (flag != 0) crc16 = crc16 ^ 0x0a001;
                }
            }
            return BitConverter.GetBytes(crc16);
        }

        // ==================== 报警检查 ====================
        private void CheckAlarm(float temp, float humi)
        {
            bool alarm = false;
            System.Collections.Generic.List<string> alarms = new System.Collections.Generic.List<string>();
            float tH = (float)nudTempHigh.Value, tL = (float)nudTempLow.Value;
            float hH = (float)nudHumiHigh.Value, hL = (float)nudHumiLow.Value;
            if (temp > tH) { alarms.Add(string.Format("温度过高: {0:F1}℃ > {1:F1}℃", temp, tH)); alarm = true; }
            if (temp < tL) { alarms.Add(string.Format("温度过低: {0:F1}℃ < {1:F1}℃", temp, tL)); alarm = true; }
            if (humi > hH) { alarms.Add(string.Format("湿度过高: {0:F1}% > {1:F1}%", humi, hH)); alarm = true; }
            if (humi < hL) { alarms.Add(string.Format("湿度过低: {0:F1}% < {1:F1}%", humi, hL)); alarm = true; }
            if (alarm)
            {
                this.BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = "⚠ 报警: " + string.Join("; ", alarms);
                    lblStatus.ForeColor = Color.Red;
                    lblTempValue.BackColor = (temp > tH || temp < tL) ? Color.LightPink : SystemColors.Control;
                    lblHumiValue.BackColor = (humi > hH || humi < hL) ? Color.LightBlue : SystemColors.Control;
                }));
                LogAlarmToFile(string.Join(", ", alarms));
            }
            else
            {
                this.BeginInvoke(new Action(() => { lblTempValue.BackColor = SystemColors.Control; lblHumiValue.BackColor = SystemColors.Control; }));
            }
        }

        // ==================== 数据记录 ====================
        private void InitLogFile()
        {
            try
            {
                string dataDir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
                logFilePath = Path.Combine(dataDir, string.Format("sensor_data_{0:yyyyMMdd}.csv", DateTime.Now));
                if (!File.Exists(logFilePath))
                    File.WriteAllText(logFilePath, "时间,温度(℃),湿度(%),校验状态,备注\n", Encoding.UTF8);
            }
            catch (Exception ex) { LogError("初始化日志文件失败: " + ex.Message); }
        }

        private void LogDataToFile(float temp, float humi)
        {
            try
            {
                string todayFile = Path.Combine(Path.GetDirectoryName(logFilePath), string.Format("sensor_data_{0:yyyyMMdd}.csv", DateTime.Now));
                if (todayFile != logFilePath) { logFilePath = todayFile; if (!File.Exists(logFilePath)) File.WriteAllText(logFilePath, "时间,温度(℃),湿度(%),校验状态,备注\n", Encoding.UTF8); }
                using (StreamWriter sw = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    string status = "正常", note = "";
                    if (chkEnableAlarm.Checked)
                    {
                        float tH = (float)nudTempHigh.Value, tL = (float)nudTempLow.Value;
                        float hH = (float)nudHumiHigh.Value, hL = (float)nudHumiLow.Value;
                        if (temp > tH || temp < tL || humi > hH || humi < hL) { status = "报警"; note = string.Format("T:{0:F1}/H:{1:F1}", temp, humi); }
                    }
                    sw.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss},{1:F1},{2:F1},{3},{4}", DateTime.Now, temp, humi, status, note));
                }
            }
            catch (Exception ex) { LogError("记录数据到文件失败: " + ex.Message); }
        }

        private void LogAlarmToFile(string alarmMsg)
        {
            try
            {
                string alarmDir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(alarmDir)) Directory.CreateDirectory(alarmDir);
                string alarmFile = Path.Combine(alarmDir, string.Format("alarm_{0:yyyyMMdd}.log", DateTime.Now));
                using (StreamWriter sw = new StreamWriter(alarmFile, true, Encoding.UTF8))
                    sw.WriteLine("[{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, alarmMsg);
            }
            catch { }
        }

        private void LogError(string msg)
        {
            try
            {
                string errDir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(errDir)) Directory.CreateDirectory(errDir);
                string errFile = Path.Combine(errDir, string.Format("error_{0:yyyyMMdd}.log", DateTime.Now));
                using (StreamWriter sw = new StreamWriter(errFile, true, Encoding.UTF8))
                    sw.WriteLine("[{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, msg);
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
                    sfd.Filter = "CSV文件 (*.csv)|*.csv"; sfd.DefaultExt = "csv";
                    sfd.FileName = string.Format("温湿度数据导出_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                        {
                            sw.WriteLine("序号,温度(℃),湿度(%)");
                            float[] temps = tempQueue.ToArray(), humis = humiQueue.ToArray();
                            for (int i = 0; i < temps.Length; i++)
                                sw.WriteLine(string.Format("{0},{1:F1},{2:F1}", i + 1, temps[i], humis[i]));
                        }
                        ShowTip(string.Format("已导出 {0} 条记录到:\n{1}", tempQueue.Count, sfd.FileName));
                    }
                }
            }
            catch (Exception ex) { ShowTip("导出失败: " + ex.Message); }
        }

        // ==================== 清除数据 ====================
        private void btnClearChart_Click(object sender, EventArgs e) { ClearAllData(); }
        private void btnClearStats_Click(object sender, EventArgs e) { ClearStats(); }

        private void ClearAllData()
        {
            tempQueue.Clear(); humiQueue.Clear(); timeQueue.Clear(); ClearStats();
            chart1.Series["温度"].Points.Clear(); chart1.Series["湿度"].Points.Clear();
            lblTempValue.Text = "--.- ℃"; lblHumiValue.Text = "--.- %"; lblUpdateTime.Text = "--";
        }

        private void ClearStats()
        {
            dataCount = 0;
            tempMin = float.MaxValue; tempMax = float.MinValue;
            humiMin = float.MaxValue; humiMax = float.MinValue;
            tempSum = 0; humiSum = 0;
            lblTempMin.Text = "最小:--"; lblTempMax.Text = "最大:--"; lblTempAvg.Text = "平均:--";
            lblHumiMin.Text = "最小:--"; lblHumiMax.Text = "最大:--"; lblHumiAvg.Text = "平均:--";
        }

        // ==================== 设置变更响应 ====================
        private void cbReadMode_SelectedIndexChanged(object sender, EventArgs e) { Settings.Default.ReadMode = cbReadMode.SelectedIndex; Settings.Default.Save(); }
        private void nudInterval_ValueChanged(object sender, EventArgs e) { Settings.Default.SampleInterval = (int)nudInterval.Value; Settings.Default.Save(); if (timer1 != null && isComOpen) timer1.Interval = (int)nudInterval.Value; }
        private void nudMaxPoints_ValueChanged(object sender, EventArgs e) { maxChartPoint = (int)nudMaxPoints.Value; Settings.Default.MaxChartPoints = maxChartPoint; Settings.Default.Save(); while (tempQueue.Count > maxChartPoint) { tempQueue.Dequeue(); humiQueue.Dequeue(); if (timeQueue.Count > 0) timeQueue.Dequeue(); } }
        private void chkEnableAlarm_CheckedChanged(object sender, EventArgs e) { Settings.Default.EnableAlarm = chkEnableAlarm.Checked; Settings.Default.Save(); }
        private void chkDataLog_CheckedChanged(object sender, EventArgs e) { Settings.Default.EnableDataLog = chkDataLog.Checked; Settings.Default.Save(); }

        // ==================== 设置持久化 ====================
        private void LoadSettings()
        {
            try { Settings.Default.Upgrade(); Settings.Default.Reload(); } catch { }
        }

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
            catch (Exception ex) { LogError("保存设置失败: " + ex.Message); }
        }

        // ==================== 窗体关闭 ====================
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAllSettings();
            if (isComOpen) closeComPort();
        }

        // ==================== 辅助方法 ====================
        private void ShowTip(string msg) { MessageBox.Show(msg, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); }

        private void UpdateStatus()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => { tsslError.Text = "错误: " + nError; tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive; }));
            else
            { tsslError.Text = "错误: " + nError; tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive; }
        }
    }
}
