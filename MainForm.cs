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

            // 设计器安全的最小初始化
            InitializeComponent();

            // 运行时：构建完整UI + 初始化
            if (!IsDesignMode())
            {
                BuildFullUI();
                WireUpEvents();
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

        // ==================== 完整UI构建（仅运行时） ====================
        private void BuildFullUI()
        {
            // 创建顶层容器（Designer.cs中只声明未实例化）
            splitContainer1 = new SplitContainer();
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;

            statusStrip1 = new StatusStrip();
            statusStrip1.Dock = DockStyle.Bottom;
            tsslStatus = new ToolStripStatusLabel("● 串口未打开") { ForeColor = System.Drawing.Color.Gray, Width = 140 };
            tsslSend = new ToolStripStatusLabel("发送: 0");
            tsslRecv = new ToolStripStatusLabel("接收: 0");
            tsslError = new ToolStripStatusLabel("错误: 0") { ForeColor = System.Drawing.Color.Orange };
            tsslTime = new ToolStripStatusLabel("00:00:00");
            statusStrip1.Items.AddRange(new ToolStripItem[] { tsslStatus, tsslSend, tsslRecv, tsslError, new ToolStripStatusLabel("  "), tsslTime });

            this.SuspendLayout();
            splitContainer1.SuspendLayout();

            // Chart（右侧Panel2）
            chart1 = new Chart();
            chart1.Dock = DockStyle.Fill;
            {
                ChartArea area = new ChartArea("MainArea");
                area.AxisX.Title = "采样点序号";
                area.AxisX.TitleFont = new Font("Microsoft YaHei", 9);
                area.AxisX.MajorGrid.LineColor = Color.LightGray;
                area.AxisY.Title = "数值";
                area.AxisY.TitleFont = new Font("Microsoft YaHei", 9);
                area.AxisY.MajorGrid.LineColor = Color.LightGray;
                area.AxisY.Minimum = -10; area.AxisY.Maximum = 100; area.AxisY.Interval = 10;
                area.CursorX.IsUserEnabled = true; area.CursorX.IsUserSelectionEnabled = true;
                area.AxisX.ScrollBar.Enabled = true; area.AxisX.ScaleView.Zoomable = true;
                chart1.ChartAreas.Add(area);

                Legend legend = new Legend("Legend") { Docking = Docking.Top, Font = new Font("Microsoft YaHei", 9) };
                chart1.Legends.Add(legend);

                Series t = new Series("温度")
                {
                    ChartType = SeriesChartType.Spline, Color = Color.Red, BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Circle, MarkerSize = 6, MarkerColor = Color.Red, LegendText = "温度 (℃)"
                };
                chart1.Series.Add(t);

                Series h = new Series("湿度")
                {
                    ChartType = SeriesChartType.Spline, Color = Color.Blue, BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Diamond, MarkerSize = 6, MarkerColor = Color.Blue, LegendText = "湿度 (%)"
                };
                chart1.Series.Add(h);
            }
            splitContainer1.Panel2.Controls.Add(chart1);

            // 左侧Panel1
            Panel pnlLeft = splitContainer1.Panel1;
            pnlLeft.Padding = new Padding(4);
            int top = 4;
            int gbWidth = 283;  // 统一GroupBox宽度
            int pad = 4;        // 面板间距

            // 模拟模式 CheckBox
            chkSimMode = new CheckBox();
            chkSimMode.Text = "模拟模式（无需硬件演示）";
            chkSimMode.Location = new Point(4, top);
            chkSimMode.Size = new Size(gbWidth, 20);
            pnlLeft.Controls.Add(chkSimMode);
            top += 22;

            // 串口设置
            gbSerial = new GroupBox();
            gbSerial.Text = "串口设置";
            gbSerial.Location = new Point(4, top);
            gbSerial.Size = new Size(gbWidth, 122);
            {
                // 绝对定位替代TLP，消除边框裁剪问题
                int lx = 60; // 标签列宽
                int ly = 20; // 起始Y（GroupBox标题下方）
                int lh = 24; // 行高
                Label lblPort = new Label() { Text = "串口号:", TextAlign = ContentAlignment.MiddleRight, Location = new Point(4, ly), Size = new Size(52, lh) };
                gbSerial.Controls.Add(lblPort);
                cbComPort = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 165, Location = new Point(lx, ly) };
                gbSerial.Controls.Add(cbComPort);
                btnRefreshPorts = new Button() { Text = "刷新", Size = new Size(52, lh), Location = new Point(lx + 170, ly) };
                gbSerial.Controls.Add(btnRefreshPorts);

                ly += 30;
                Label lblBaud = new Label() { Text = "波特率:", TextAlign = ContentAlignment.MiddleRight, Location = new Point(4, ly), Size = new Size(52, lh) };
                gbSerial.Controls.Add(lblBaud);
                cbBaudRate = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Location = new Point(lx, ly) };
                cbBaudRate.Items.AddRange(new object[] { "4800", "9600", "19200", "38400", "57600", "115200" });
                cbBaudRate.SelectedIndex = 1;
                gbSerial.Controls.Add(cbBaudRate);

                ly += 30;
                btnOpenCloseCom = new Button() { Text = "打开串口", Size = new Size(110, lh + 2), Location = new Point(28, ly) };
                gbSerial.Controls.Add(btnOpenCloseCom);
                btnManualSend = new Button() { Text = "手动采集", Size = new Size(110, lh + 2), Location = new Point(144, ly) };
                gbSerial.Controls.Add(btnManualSend);
            }
            pnlLeft.Controls.Add(gbSerial);
            top += 122 + pad;

            // 采集设置
            gbCollect = new GroupBox();
            gbCollect.Text = "采集设置";
            gbCollect.Location = new Point(4, top);
            gbCollect.Size = new Size(gbWidth, 100);
            {
                TableLayoutPanel tlp = TLP(2, 3, 28, 70);
                gbCollect.Controls.Add(tlp);

                tlp.Controls.Add(Lbl("读取模式:"), 0, 0);
                cbReadMode = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
                cbReadMode.Items.AddRange(new object[] { "一次读取（浮点格式）", "一次读取（整型格式）", "单独读取温度（浮点）", "单独读取湿度（浮点）", "单独读取温度（整型）", "单独读取湿度（整型）" });
                cbReadMode.SelectedIndex = 0;
                tlp.Controls.Add(cbReadMode, 1, 0);

                tlp.Controls.Add(Lbl("间隔(ms):"), 0, 1);
                nudInterval = new NumericUpDown() { Minimum = 200, Maximum = 60000, Increment = 100, Value = 1000, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill };
                tlp.Controls.Add(nudInterval, 1, 1);

                tlp.Controls.Add(Lbl("最大点数:"), 0, 2);
                nudMaxPoints = new NumericUpDown() { Minimum = 10, Maximum = 500, Increment = 10, Value = 30, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill };
                tlp.Controls.Add(nudMaxPoints, 1, 2);
            }
            pnlLeft.Controls.Add(gbCollect);
            top += 100 + pad;

            // 当前数据
            gbCurrent = new GroupBox();
            gbCurrent.Text = "当前数据";
            gbCurrent.Location = new Point(4, top);
            gbCurrent.Size = new Size(gbWidth, 100);
            {
                TableLayoutPanel tlp = TLP(2, 3, 26, 50);
                gbCurrent.Controls.Add(tlp);

                tlp.Controls.Add(Lbl("温度:"), 0, 0);
                lblTempValue = new Label() { Text = "--.- ℃", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold), ForeColor = Color.Red };
                tlp.Controls.Add(lblTempValue, 1, 0);

                tlp.Controls.Add(Lbl("湿度:"), 0, 1);
                lblHumiValue = new Label() { Text = "--.- %", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold), ForeColor = Color.Blue };
                tlp.Controls.Add(lblHumiValue, 1, 1);

                tlp.Controls.Add(Lbl("更新:"), 0, 2);
                lblUpdateTime = new Label() { Text = "--", TextAlign = ContentAlignment.MiddleCenter };
                tlp.Controls.Add(lblUpdateTime, 1, 2);
            }
            pnlLeft.Controls.Add(gbCurrent);
            top += 100 + pad;

            // 统计信息
            gbStats = new GroupBox();
            gbStats.Text = "统计信息";
            gbStats.Location = new Point(4, top);
            gbStats.Size = new Size(gbWidth, 105);
            {
                TableLayoutPanel tlp = TLP(4, 3, 24, 42);
                gbStats.Controls.Add(tlp);

                tlp.Controls.Add(Lbl("温度:"), 0, 0);
                lblTempMin = new Label() { Text = "最小:--", ForeColor = Color.DarkRed, TextAlign = ContentAlignment.MiddleCenter };
                lblTempMax = new Label() { Text = "最大:--", ForeColor = Color.DarkRed, TextAlign = ContentAlignment.MiddleCenter };
                lblTempAvg = new Label() { Text = "平均:--", ForeColor = Color.DarkRed, TextAlign = ContentAlignment.MiddleCenter };
                tlp.Controls.Add(lblTempMin, 1, 0); tlp.Controls.Add(lblTempMax, 2, 0); tlp.Controls.Add(lblTempAvg, 3, 0);

                tlp.Controls.Add(Lbl("湿度:"), 0, 1);
                lblHumiMin = new Label() { Text = "最小:--", ForeColor = Color.DarkBlue, TextAlign = ContentAlignment.MiddleCenter };
                lblHumiMax = new Label() { Text = "最大:--", ForeColor = Color.DarkBlue, TextAlign = ContentAlignment.MiddleCenter };
                lblHumiAvg = new Label() { Text = "平均:--", ForeColor = Color.DarkBlue, TextAlign = ContentAlignment.MiddleCenter };
                tlp.Controls.Add(lblHumiMin, 1, 1); tlp.Controls.Add(lblHumiMax, 2, 1); tlp.Controls.Add(lblHumiAvg, 3, 1);

                btnClearStats = new Button() { Text = "重置统计", Size = new Size(70, 22) };
                tlp.Controls.Add(btnClearStats, 0, 2);
                tlp.SetColumnSpan(btnClearStats, 4);
            }
            pnlLeft.Controls.Add(gbStats);
            top += 105 + pad;

            // 报警设置
            gbAlarm = new GroupBox();
            gbAlarm.Text = "报警设置";
            gbAlarm.Location = new Point(4, top);
            gbAlarm.Size = new Size(gbWidth, 158);
            {
                TableLayoutPanel tlp = TLP(2, 5, 24, 70);
                gbAlarm.Controls.Add(tlp);

                chkEnableAlarm = new CheckBox() { Text = "启用报警", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                tlp.Controls.Add(chkEnableAlarm, 1, 0);

                tlp.Controls.Add(Lbl("温度上限:"), 0, 1);
                nudTempHigh = new NumericUpDown() { Minimum = -50, Maximum = 150, Increment = 0.5M, Value = 40, DecimalPlaces = 1, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill };
                tlp.Controls.Add(nudTempHigh, 1, 1);

                tlp.Controls.Add(Lbl("温度下限:"), 0, 2);
                nudTempLow = new NumericUpDown() { Minimum = -50, Maximum = 150, Increment = 0.5M, Value = 0, DecimalPlaces = 1, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill };
                tlp.Controls.Add(nudTempLow, 1, 2);

                tlp.Controls.Add(Lbl("湿度上限:"), 0, 3);
                nudHumiHigh = new NumericUpDown() { Minimum = 0, Maximum = 100, Increment = 1, Value = 80, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill };
                tlp.Controls.Add(nudHumiHigh, 1, 3);

                tlp.Controls.Add(Lbl("湿度下限:"), 0, 4);
                nudHumiLow = new NumericUpDown() { Minimum = 0, Maximum = 100, Increment = 1, Value = 20, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill };
                tlp.Controls.Add(nudHumiLow, 1, 4);
            }
            pnlLeft.Controls.Add(gbAlarm);
            top += 158 + pad;

            // 数据管理
            gbData = new GroupBox();
            gbData.Text = "数据管理";
            gbData.Location = new Point(4, top);
            gbData.Size = new Size(gbWidth, 105);
            {
                chkDataLog = new CheckBox() { Text = "启用数据记录", Checked = true, TextAlign = ContentAlignment.MiddleLeft };
                btnExportCSV = new Button() { Text = "导出CSV", TextAlign = ContentAlignment.MiddleCenter };
                btnClearChart = new Button() { Text = "清除图表", TextAlign = ContentAlignment.MiddleCenter };

                TableLayoutPanel tlp = TLP(1, 3, 28, 0);
                gbData.Controls.Add(tlp);
                tlp.Controls.Add(chkDataLog, 0, 0); chkDataLog.Dock = DockStyle.Fill;
                tlp.Controls.Add(btnExportCSV, 0, 1); btnExportCSV.Dock = DockStyle.Fill;
                tlp.Controls.Add(btnClearChart, 0, 2); btnClearChart.Dock = DockStyle.Fill;
            }
            pnlLeft.Controls.Add(gbData);
            top += 105 + pad;

            // 状态Label
            lblStatus = new Label();
            lblStatus.Text = "就绪"; lblStatus.ForeColor = Color.Gray;
            lblStatus.Location = new Point(4, top); lblStatus.Size = new Size(270, 18);
            pnlLeft.Controls.Add(lblStatus);

            // 组件
            timer1 = new Timer();
            serialPort1 = new SerialPort();

            // 添加到窗体
            this.Controls.Add(splitContainer1);
            this.Controls.Add(statusStrip1);

            splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // 辅助：快速创建 Label
        private static Label Lbl(string text)
        {
            return new Label() { Text = text, TextAlign = ContentAlignment.MiddleRight };
        }

        // 辅助：快速创建 TableLayoutPanel
        private static TableLayoutPanel TLP(int cols, int rows, int rowH, int col0w)
        {
            TableLayoutPanel tlp = new TableLayoutPanel();
            tlp.Dock = DockStyle.Fill;
            tlp.ColumnCount = cols;
            tlp.RowCount = rows;
            tlp.Padding = new Padding(4, 0, 4, 0);
            if (col0w > 0)
            {
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, col0w));
                for (int i = 1; i < cols; i++)
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / (cols - 1)));
            }
            for (int i = 0; i < rows; i++)
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));
            return tlp;
        }

        // ==================== 事件绑定 ====================
        private void WireUpEvents()
        {
            this.Load += MainForm_Load;
            this.Shown += MainForm_Shown;
            this.FormClosing += MainForm_FormClosing;

            chkSimMode.CheckedChanged += chkSimMode_CheckedChanged;
            btnRefreshPorts.Click += btnRefreshPorts_Click;
            btnOpenCloseCom.Click += btnOpenCloseCom_Click;
            btnManualSend.Click += btnManualSend_Click;
            btnClearStats.Click += btnClearStats_Click;
            btnExportCSV.Click += btnExportCSV_Click;
            btnClearChart.Click += btnClearChart_Click;
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
            lblTempValue.Text = string.Format("{0:F1} ℃", t);
            lblHumiValue.Text = string.Format("{0:F1} %", h);
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
