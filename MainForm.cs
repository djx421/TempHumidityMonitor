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
        // ==================== 字段 ====================
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

        // ==================== UI 控件声明 ====================
        private ComboBox cbComPort, cbBaudRate, cbReadMode;
        private Button btnRefreshPorts, btnOpenCloseCom, btnExportCSV, btnClearChart, btnClearStats;
        private Button btnManualSend;
        private Label lblTempValue, lblHumiValue, lblUpdateTime;
        private Label lblTempMin, lblTempMax, lblTempAvg;
        private Label lblHumiMin, lblHumiMax, lblHumiAvg;
        private Label lblStatus;
        private CheckBox chkEnableAlarm, chkDataLog, chkSimMode;
        private NumericUpDown nudTempHigh, nudTempLow, nudHumiHigh, nudHumiLow;
        private NumericUpDown nudInterval, nudMaxPoints;
        private Chart chart1;
        private Timer timer1;
        private SerialPort serialPort1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tsslStatus, tsslSend, tsslRecv, tsslError, tsslTime;
        private SplitContainer splitContainer1;

        // ==================== 构造函数 ====================
        public MainForm()
        {
            tempQueue = new Queue<float>();
            humiQueue = new Queue<float>();
            timeQueue = new Queue<DateTime>();
            InitializeComponent();
            this.Load += MainForm_Load;
            // 设计模式下跳过运行时初始化，避免空引用异常
            if (!IsDesignMode())
            {
                initControls();
                LoadSettings();
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance = 285;
            splitContainer1.Panel1MinSize = 260;
            splitContainer1.Panel2MinSize = 200;
        }

        // ==================== UI 初始化 ====================
        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ---- 窗体设置 ----
            this.Text = "温湿度传感器监控程序";
            this.Size = new Size(1100, 800);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;

            // ---- SplitContainer 主布局 ----
            splitContainer1 = new SplitContainer();
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.IsSplitterFixed = false;

            // ---- 左侧 Panel1：直接添加控件，不用额外 Panel 包装 ----
            Panel pnlLeft = splitContainer1.Panel1;
            pnlLeft.Padding = new Padding(4);
            int ctrlTop = 4;

            // ===== 顶部：模拟模式开关（最显眼位置） =====
            chkSimMode = new CheckBox();
            chkSimMode.Text = "模拟模式（无需硬件即可演示）";
            chkSimMode.Location = new Point(4, ctrlTop);
            chkSimMode.Size = new Size(270, 22);
            chkSimMode.CheckedChanged += chkSimMode_CheckedChanged;
            pnlLeft.Controls.Add(chkSimMode);
            ctrlTop += 26;

            // ---- 右侧 Chart ----
            chart1 = new Chart();
            chart1.Dock = DockStyle.Fill;
            InitChart();

            // ---- StatusStrip ----
            statusStrip1 = new StatusStrip();
            statusStrip1.Dock = DockStyle.Bottom;
            tsslStatus = new ToolStripStatusLabel("● 串口未打开");
            tsslStatus.ForeColor = Color.Gray;
            tsslStatus.Width = 140;
            tsslSend = new ToolStripStatusLabel("发送: 0");
            tsslRecv = new ToolStripStatusLabel("接收: 0");
            tsslError = new ToolStripStatusLabel("错误: 0");
            tsslError.ForeColor = Color.Orange;
            tsslTime = new ToolStripStatusLabel(DateTime.Now.ToString("HH:mm:ss"));
            statusStrip1.Items.AddRange(new ToolStripItem[] {
                tsslStatus, tsslSend, tsslRecv, tsslError,
                new ToolStripStatusLabel("  "), tsslTime
            });

            // ---- 串口设置 GroupBox ----
            GroupBox gbSerial = CreateGroupBox("串口设置", ref ctrlTop, 110, pnlLeft);
            TableLayoutPanel tlpSerial = CreateTLP(2, 3, 28, 60);
            gbSerial.Controls.Add(tlpSerial);

            Label lblPort = new Label() { Text = "串口号:", TextAlign = ContentAlignment.MiddleRight };
            cbComPort = new ComboBox() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            btnRefreshPorts = new Button() { Text = "刷新", Size = new Size(48, 22) };
            btnRefreshPorts.Click += btnRefreshPorts_Click;
            Label lblBaud = new Label() { Text = "波特率:", TextAlign = ContentAlignment.MiddleRight };
            cbBaudRate = new ComboBox() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cbBaudRate.Items.AddRange(new object[] { "4800", "9600", "19200", "38400", "57600", "115200" });
            cbBaudRate.SelectedIndex = 1;
            btnOpenCloseCom = new Button() { Text = "打开串口" };
            btnOpenCloseCom.Click += btnOpenCloseCom_Click;
            btnManualSend = new Button() { Text = "手动采集" };
            btnManualSend.Click += btnManualSend_Click;

            Panel pnlPortRow = new Panel() { Dock = DockStyle.Fill };
            cbComPort.Dock = DockStyle.None;
            cbComPort.Width = 130;
            cbComPort.Location = new Point(0, 2);
            btnRefreshPorts.Location = new Point(136, 2);
            pnlPortRow.Controls.Add(cbComPort);
            pnlPortRow.Controls.Add(btnRefreshPorts);

            // 两个按钮并排放在同一行
            Panel pnlBtnRow = new Panel() { Dock = DockStyle.Fill };
            btnOpenCloseCom.Size = new Size(105, 24);
            btnOpenCloseCom.Location = new Point(0, 1);
            btnManualSend.Size = new Size(80, 24);
            btnManualSend.Location = new Point(110, 1);
            pnlBtnRow.Controls.Add(btnOpenCloseCom);
            pnlBtnRow.Controls.Add(btnManualSend);

            tlpSerial.Controls.Add(lblPort, 0, 0);
            tlpSerial.Controls.Add(pnlPortRow, 1, 0);
            tlpSerial.Controls.Add(lblBaud, 0, 1);
            tlpSerial.Controls.Add(cbBaudRate, 1, 1);
            tlpSerial.Controls.Add(pnlBtnRow, 1, 2);

            // ---- 采集设置 GroupBox ----
            GroupBox gbCollect = CreateGroupBox("采集设置", ref ctrlTop, 95, pnlLeft);
            TableLayoutPanel tlpCollect = CreateTLP(2, 2, 28, 70);
            gbCollect.Controls.Add(tlpCollect);

            Label lblReadMode = new Label() { Text = "读取模式:", TextAlign = ContentAlignment.MiddleRight };
            cbReadMode = new ComboBox() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cbReadMode.Items.AddRange(new object[] {
                "一次读取（浮点格式）", "一次读取（整型格式）",
                "单独读取温度（浮点）", "单独读取湿度（浮点）",
                "单独读取温度（整型）", "单独读取湿度（整型）"
            });
            cbReadMode.SelectedIndex = 0;
            cbReadMode.SelectedIndexChanged += cbReadMode_SelectedIndexChanged;
            Label lblInterval = new Label() { Text = "间隔(ms):", TextAlign = ContentAlignment.MiddleRight };
            nudInterval = new NumericUpDown() { Dock = DockStyle.Fill, Minimum = 200, Maximum = 60000, Increment = 100, Value = 1000 };
            nudInterval.ValueChanged += nudInterval_ValueChanged;
            Label lblMaxPts = new Label() { Text = "最大点数:", TextAlign = ContentAlignment.MiddleRight };
            nudMaxPoints = new NumericUpDown() { Dock = DockStyle.Fill, Minimum = 10, Maximum = 500, Increment = 10, Value = 30 };
            nudMaxPoints.ValueChanged += nudMaxPoints_ValueChanged;

            tlpCollect.Controls.Add(lblReadMode, 0, 0);
            tlpCollect.Controls.Add(cbReadMode, 1, 0);
            tlpCollect.Controls.Add(lblInterval, 0, 1);
            tlpCollect.Controls.Add(nudInterval, 1, 1);
            tlpCollect.Controls.Add(lblMaxPts, 0, 2);
            tlpCollect.Controls.Add(nudMaxPoints, 1, 2);

            // ---- 当前数据 GroupBox ----
            GroupBox gbCurrent = CreateGroupBox("当前数据", ref ctrlTop, 90, pnlLeft);
            TableLayoutPanel tlpCurrent = CreateTLP(2, 2, 26, 50);
            gbCurrent.Controls.Add(tlpCurrent);

            Label lblTempLabel = new Label() { Text = "温度:", TextAlign = ContentAlignment.MiddleRight };
            lblTempValue = new Label() { Text = "--.- ℃", TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold), ForeColor = Color.Red };
            Label lblHumiLabel = new Label() { Text = "湿度:", TextAlign = ContentAlignment.MiddleRight };
            lblHumiValue = new Label() { Text = "--.- %", TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold), ForeColor = Color.Blue };
            Label lblUpdateLabel = new Label() { Text = "更新:", TextAlign = ContentAlignment.MiddleRight };
            lblUpdateTime = new Label() { Text = "--", TextAlign = ContentAlignment.MiddleLeft };

            tlpCurrent.Controls.Add(lblTempLabel, 0, 0);
            tlpCurrent.Controls.Add(lblTempValue, 1, 0);
            tlpCurrent.Controls.Add(lblHumiLabel, 0, 1);
            tlpCurrent.Controls.Add(lblHumiValue, 1, 1);
            tlpCurrent.Controls.Add(lblUpdateLabel, 0, 2);
            tlpCurrent.Controls.Add(lblUpdateTime, 1, 2);

            // ---- 统计信息 GroupBox ----
            GroupBox gbStats = CreateGroupBox("统计信息", ref ctrlTop, 110, pnlLeft);
            TableLayoutPanel tlpStats = CreateTLP(4, 3, 24, 42);
            gbStats.Controls.Add(tlpStats);

            tlpStats.Controls.Add(new Label() { Text = "温度(℃):", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            lblTempMin = new Label() { Text = "最小:--", ForeColor = Color.DarkRed, TextAlign = ContentAlignment.MiddleLeft };
            lblTempMax = new Label() { Text = "最大:--", ForeColor = Color.DarkRed, TextAlign = ContentAlignment.MiddleLeft };
            lblTempAvg = new Label() { Text = "平均:--", ForeColor = Color.DarkRed, TextAlign = ContentAlignment.MiddleLeft };
            tlpStats.Controls.Add(lblTempMin, 1, 0);
            tlpStats.Controls.Add(lblTempMax, 2, 0);
            tlpStats.Controls.Add(lblTempAvg, 3, 0);

            tlpStats.Controls.Add(new Label() { Text = "湿度(%):", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            lblHumiMin = new Label() { Text = "最小:--", ForeColor = Color.DarkBlue, TextAlign = ContentAlignment.MiddleLeft };
            lblHumiMax = new Label() { Text = "最大:--", ForeColor = Color.DarkBlue, TextAlign = ContentAlignment.MiddleLeft };
            lblHumiAvg = new Label() { Text = "平均:--", ForeColor = Color.DarkBlue, TextAlign = ContentAlignment.MiddleLeft };
            tlpStats.Controls.Add(lblHumiMin, 1, 1);
            tlpStats.Controls.Add(lblHumiMax, 2, 1);
            tlpStats.Controls.Add(lblHumiAvg, 3, 1);

            btnClearStats = new Button() { Text = "重置统计", Size = new Size(70, 24) };
            btnClearStats.Click += btnClearStats_Click;
            tlpStats.Controls.Add(btnClearStats, 0, 2);
            tlpStats.SetColumnSpan(btnClearStats, 4);

            // ---- 报警设置 GroupBox ----
            GroupBox gbAlarm = CreateGroupBox("报警设置", ref ctrlTop, 130, pnlLeft);
            TableLayoutPanel tlpAlarm = CreateTLP(2, 5, 24, 70);
            gbAlarm.Controls.Add(tlpAlarm);

            chkEnableAlarm = new CheckBox() { Text = "启用报警", TextAlign = ContentAlignment.MiddleLeft };
            chkEnableAlarm.CheckedChanged += chkEnableAlarm_CheckedChanged;
            Label lblTempH = new Label() { Text = "温度上限:", TextAlign = ContentAlignment.MiddleRight };
            nudTempHigh = new NumericUpDown() { Dock = DockStyle.Fill, Minimum = -50, Maximum = 150, Increment = 0.5M, Value = 40, DecimalPlaces = 1 };
            Label lblTempL = new Label() { Text = "温度下限:", TextAlign = ContentAlignment.MiddleRight };
            nudTempLow = new NumericUpDown() { Dock = DockStyle.Fill, Minimum = -50, Maximum = 150, Increment = 0.5M, Value = 0, DecimalPlaces = 1 };
            Label lblHumiH = new Label() { Text = "湿度上限:", TextAlign = ContentAlignment.MiddleRight };
            nudHumiHigh = new NumericUpDown() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Increment = 1, Value = 80 };
            Label lblHumiL = new Label() { Text = "湿度下限:", TextAlign = ContentAlignment.MiddleRight };
            nudHumiLow = new NumericUpDown() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Increment = 1, Value = 20 };

            tlpAlarm.Controls.Add(chkEnableAlarm, 1, 0);
            tlpAlarm.Controls.Add(lblTempH, 0, 1); tlpAlarm.Controls.Add(nudTempHigh, 1, 1);
            tlpAlarm.Controls.Add(lblTempL, 0, 2); tlpAlarm.Controls.Add(nudTempLow, 1, 2);
            tlpAlarm.Controls.Add(lblHumiH, 0, 3); tlpAlarm.Controls.Add(nudHumiHigh, 1, 3);
            tlpAlarm.Controls.Add(lblHumiL, 0, 4); tlpAlarm.Controls.Add(nudHumiLow, 1, 4);

            // ---- 数据管理 GroupBox ----
            GroupBox gbData = CreateGroupBox("数据管理", ref ctrlTop, 90, pnlLeft);
            TableLayoutPanel tlpData = CreateTLP(1, 3, 26, 100);
            gbData.Controls.Add(tlpData);

            chkDataLog = new CheckBox() { Text = "启用数据记录", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            chkDataLog.Checked = true;
            chkDataLog.CheckedChanged += chkDataLog_CheckedChanged;
            btnExportCSV = new Button() { Text = "导出CSV文件", Dock = DockStyle.Fill };
            btnExportCSV.Click += btnExportCSV_Click;
            btnClearChart = new Button() { Text = "清除图表", Dock = DockStyle.Fill };
            btnClearChart.Click += btnClearChart_Click;

            tlpData.Controls.Add(chkDataLog, 0, 0);
            tlpData.Controls.Add(btnExportCSV, 0, 1);
            tlpData.Controls.Add(btnClearChart, 0, 2);

            // ---- lblStatus ----
            lblStatus = new Label();
            lblStatus.Location = new Point(4, ctrlTop);
            lblStatus.Size = new Size(270, 18);
            lblStatus.Text = "就绪";
            lblStatus.ForeColor = Color.Gray;
            pnlLeft.Controls.Add(lblStatus);

            // ---- 组装主布局 ----
            splitContainer1.Panel2.Controls.Add(chart1);
            this.Controls.Add(splitContainer1);
            this.Controls.Add(statusStrip1);

            // ---- Timer ----
            timer1 = new Timer();
            timer1.Tick += timer1_Tick;

            // ---- SerialPort ----
            serialPort1 = new SerialPort();
            serialPort1.DataReceived += serialPort1_DataReceived;
            serialPort1.ErrorReceived += serialPort1_ErrorReceived;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // ---- 辅助布局方法 ----
        private GroupBox CreateGroupBox(string title, ref int top, int height, Control parent)
        {
            GroupBox gb = new GroupBox();
            gb.Text = title;
            gb.Location = new Point(4, top);
            gb.Size = new Size(272, height);
            gb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            parent.Controls.Add(gb);
            top += height + 4;
            return gb;
        }

        private TableLayoutPanel CreateTLP(int cols, int rows, int rowHeight, int col0Width)
        {
            TableLayoutPanel tlp = new TableLayoutPanel();
            tlp.Dock = DockStyle.Fill;
            tlp.ColumnCount = cols;
            tlp.RowCount = rows;
            tlp.Padding = new Padding(4, 0, 4, 0);
            if (col0Width > 0)
            {
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, col0Width));
                for (int i = 1; i < cols; i++)
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / (cols - 1)));
            }
            else
            {
                for (int i = 0; i < cols; i++)
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / cols));
            }
            for (int i = 0; i < rows; i++)
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
            return tlp;
        }

        // ==================== 初始化控件状态 ====================
        private void initControls()
        {
            try
            {
                RefreshComPorts();
            }
            catch (Exception ex)
            {
                cbComPort.Items.Add("无可用串口");
                cbComPort.SelectedIndex = 0;
                LogError("初始化串口列表失败: " + ex.Message);
            }

            // 从Settings加载配置
            try
            {
                if (cbBaudRate.Items.Contains(Settings.Default.BaudRate.ToString()))
                {
                    cbBaudRate.Text = Settings.Default.BaudRate.ToString();
                }
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
            catch { /* 使用默认值 */ }

            UpdateChartSettings();
            InitLogFile();

            // 时间状态栏更新定时器
            Timer timeTimer = new Timer();
            timeTimer.Interval = 1000;
            timeTimer.Tick += (s, e) => { tsslTime.Text = DateTime.Now.ToString("HH:mm:ss"); };
            timeTimer.Start();
        }

        // ==================== Chart 初始化 ====================
        private void InitChart()
        {
            chart1.ChartAreas.Clear();
            chart1.Series.Clear();
            chart1.Legends.Clear();

            ChartArea area = new ChartArea("MainArea");
            area.AxisX.Title = "采样点序号";
            area.AxisX.TitleFont = new Font("Microsoft YaHei", 9);
            area.AxisX.MajorGrid.LineColor = Color.LightGray;
            area.AxisX.LabelStyle.Format = "0";
            area.AxisY.Title = "数值";
            area.AxisY.TitleFont = new Font("Microsoft YaHei", 9);
            area.AxisY.MajorGrid.LineColor = Color.LightGray;
            area.AxisY.Minimum = -10;
            area.AxisY.Maximum = 100;
            area.AxisY.Interval = 10;
            area.AxisY.LabelStyle.Format = "0.#";
            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = true;
            area.AxisX.ScrollBar.Enabled = true;
            area.AxisX.ScrollBar.BackColor = Color.LightGray;
            area.AxisX.ScrollBar.ButtonColor = Color.DarkGray;
            area.AxisX.ScaleView.Zoomable = true;
            chart1.ChartAreas.Add(area);

            Legend legend = new Legend("Legend");
            legend.Docking = Docking.Top;
            legend.Font = new Font("Microsoft YaHei", 9);
            chart1.Legends.Add(legend);

            Series tempSeries = new Series("温度");
            tempSeries.ChartType = SeriesChartType.Spline;
            tempSeries.Color = Color.Red;
            tempSeries.BorderWidth = 2;
            tempSeries.MarkerStyle = MarkerStyle.Circle;
            tempSeries.MarkerSize = 6;
            tempSeries.MarkerColor = Color.Red;
            tempSeries.LegendText = "温度 (℃)";
            chart1.Series.Add(tempSeries);

            Series humiSeries = new Series("湿度");
            humiSeries.ChartType = SeriesChartType.Spline;
            humiSeries.Color = Color.Blue;
            humiSeries.BorderWidth = 2;
            humiSeries.MarkerStyle = MarkerStyle.Diamond;
            humiSeries.MarkerSize = 6;
            humiSeries.MarkerColor = Color.Blue;
            humiSeries.LegendText = "湿度 (%)";
            chart1.Series.Add(humiSeries);

            chart1.AnnotationPositionChanged += (s, e) => { /* 处理缩放事件 */ };
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
                if (prevSelection != null && cbComPort.Items.Contains(prevSelection))
                    cbComPort.Text = prevSelection;
                else
                    cbComPort.SelectedIndex = 0;
            }
            else
            {
                cbComPort.Items.Add("无可用串口");
                cbComPort.SelectedIndex = 0;
            }
        }

        private void btnRefreshPorts_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isComOpen)
                    RefreshComPorts();
                else
                    ShowTip("请先关闭串口再刷新端口列表");
            }
            catch (Exception ex)
            {
                ShowTip("刷新串口失败: " + ex.Message);
            }
        }

        private void btnOpenCloseCom_Click(object sender, EventArgs e)
        {
            if (isComOpen)
            {
                closeComPort();
            }
            else
            {
                openComPort();
            }
        }

        private void openComPort()
        {
            try
            {
                string portName = cbComPort.Text;
                if (string.IsNullOrEmpty(portName) || portName == "无可用串口")
                {
                    ShowTip("请选择有效的串口号");
                    return;
                }

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

                // 启动定时器
                timer1.Interval = (int)nudInterval.Value;
                timer1.Start();

                // 禁用串口选择控件
                cbComPort.Enabled = false;
                btnRefreshPorts.Enabled = false;
                cbBaudRate.Enabled = false;

                lblStatus.Text = "串口已打开，正在采集数据...";
                lblStatus.ForeColor = Color.Green;

                // 初始化接收缓冲区
                receiveBuffer.Clear();
            }
            catch (Exception ex)
            {
                ShowTip("打开串口失败: " + ex.Message);
                LogError("打开串口失败: " + ex.Message);
            }
        }

        private void closeComPort()
        {
            try
            {
                timer1.Stop();
                if (serialPort1.IsOpen)
                    serialPort1.Close();

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
            catch (Exception ex)
            {
                LogError("关闭串口失败: " + ex.Message);
            }
        }

        // ==================== 定时器 ====================
        private void timer1_Tick(object sender, EventArgs e)
        {
            sendData();
        }

        // ==================== 发送数据 ====================
        private void sendData()
        {
            try
            {
                // 模拟模式：直接生成模拟数据
                if (isSimMode)
                {
                    SimulateData();
                    return;
                }

                if (!isComOpen || !serialPort1.IsOpen) return;

                byte[] cmd = GetModbusCommand();
                serialPort1.Write(cmd, 0, cmd.Length);
                nSend++;
                tsslSend.Text = "发送: " + nSend;
            }
            catch (Exception ex)
            {
                nError++;
                tsslError.Text = "错误: " + nError;
                LogError("发送数据失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 根据读取模式获取对应的MODBUS指令
        /// </summary>
        private byte[] GetModbusCommand()
        {
            switch (cbReadMode.SelectedIndex)
            {
                case 0: // 一次读取温湿度（浮点格式）
                    return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x44, 0x09 };
                case 1: // 一次读取温湿度（整型格式）
                    return new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x04, 0x45, 0xE1 };
                case 2: // 单独读取温度（浮点格式）
                    return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B };
                case 3: // 单独读取湿度（浮点格式）
                    return new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02, 0x65, 0xCB };
                case 4: // 单独读取温度（整型格式）
                    return new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x01, 0x85, 0xE2 };
                case 5: // 单独读取湿度（整型格式）
                    return new byte[] { 0x01, 0x03, 0x00, 0x81, 0x00, 0x01, 0xD4, 0x22 };
                default:
                    return new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x44, 0x09 };
            }
        }

        /// <summary>
        /// 模拟模式：生成随机温湿度数据用于演示和测试
        /// </summary>
        private void SimulateData()
        {
            // 温度在 ±1.5℃ 范围内随机波动
            float deltaT = (float)(simRandom.NextDouble() * 3.0 - 1.5);
            simTemp += deltaT;
            simTemp = Math.Max(-20, Math.Min(80, simTemp));

            // 湿度在 ±3% 范围内随机波动
            float deltaH = (float)(simRandom.NextDouble() * 6.0 - 3.0);
            simHumi += deltaH;
            simHumi = Math.Max(5, Math.Min(98, simHumi));

            nSend++;
            nReceive++;
            tsslSend.Text = "发送: " + nSend;
            tsslRecv.Text = "接收: " + nReceive;
            lastReceiveTime = DateTime.Now;

            AddDataPoint(simTemp, simHumi);

            if (chkEnableAlarm.Checked)
                CheckAlarm(simTemp, simHumi);

            if (chkDataLog.Checked)
                LogDataToFile(simTemp, simHumi);

            this.BeginInvoke(new Action(() => updateUI(simTemp, simHumi)));
        }

        /// <summary>
        /// 手动采集按钮
        /// </summary>
        private void btnManualSend_Click(object sender, EventArgs e)
        {
            if (!isSimMode && (!isComOpen || !serialPort1.IsOpen))
            {
                ShowTip("请先打开串口或启用模拟模式");
                return;
            }
            sendData();
        }

        /// <summary>
        /// 模拟模式切换
        /// </summary>
        private void chkSimMode_CheckedChanged(object sender, EventArgs e)
        {
            isSimMode = chkSimMode.Checked;
            if (isSimMode)
            {
                // 模拟模式下禁用串口相关控件
                cbComPort.Enabled = false;
                btnRefreshPorts.Enabled = false;
                cbBaudRate.Enabled = false;

                if (isComOpen) closeComPort();
                btnOpenCloseCom.Enabled = false;

                // 启动定时器用于模拟数据生成
                timer1.Interval = (int)nudInterval.Value;
                timer1.Start();

                tsslStatus.Text = "● 模拟模式 - 演示数据";
                tsslStatus.ForeColor = Color.Orange;
                lblStatus.Text = "模拟模式运行中，数据为随机模拟值";
                lblStatus.ForeColor = Color.Orange;
            }
            else
            {
                timer1.Stop();
                btnOpenCloseCom.Enabled = true;
                cbComPort.Enabled = true;
                btnRefreshPorts.Enabled = true;
                cbBaudRate.Enabled = true;
                tsslStatus.Text = "● 串口未打开";
                tsslStatus.ForeColor = Color.Gray;
                lblStatus.Text = "就绪";
                lblStatus.ForeColor = Color.Gray;
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

                // 尝试解析完整的数据帧
                while (receiveBuffer.Count >= 5)
                {
                    // 检查帧头：地址=0x01, 功能码=0x03
                    if (receiveBuffer[0] != 0x01 || receiveBuffer[1] != 0x03)
                    {
                        // 丢弃无效字节，寻找下一个可能的帧头
                        receiveBuffer.RemoveAt(0);
                        continue;
                    }

                    int dataLength = receiveBuffer[2];
                    int totalLength = 3 + dataLength + 2; // 地址+功能+长度 + 数据 + CRC

                    if (receiveBuffer.Count < totalLength)
                    {
                        // 数据不完整，等待更多数据
                        break;
                    }

                    // 提取完整数据帧
                    byte[] frame = new byte[totalLength];
                    receiveBuffer.CopyTo(0, frame, 0, totalLength);
                    receiveBuffer.RemoveRange(0, totalLength);

                    // 处理数据帧
                    ProcessFrame(frame);
                }
            }
            catch (Exception ex)
            {
                LogError("接收数据处理异常: " + ex.Message);
                nError++;
                UpdateStatus();
            }
        }

        private void serialPort1_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            nError++;
            tsslError.Text = "错误: " + nError;
            LogError("串口错误: " + e.EventType);
        }

        // ==================== 数据处理 ====================
        private void ProcessFrame(byte[] buffer)
        {
            nReceive++;
            tsslRecv.Text = "接收: " + nReceive;
            lastReceiveTime = DateTime.Now;

            try
            {
                // 检查数据
                if (!checkData(buffer))
                {
                    nError++;
                    tsslError.Text = "错误: " + nError;
                    return;
                }

                float temp, humi;
                getTempHumi(buffer, out temp, out humi);

                // 验证数据合理性
                if (temp < -40 || temp > 125 || humi < 0 || humi > 100)
                {
                    LogError(string.Format("数据超出合理范围: 温度={0:F1}, 湿度={1:F1}", temp, humi));
                    return;
                }

                // 更新队列
                AddDataPoint(temp, humi);

                // 检查报警
                if (chkEnableAlarm.Checked)
                {
                    CheckAlarm(temp, humi);
                }

                // 数据记录
                if (chkDataLog.Checked)
                {
                    LogDataToFile(temp, humi);
                }

                // 更新UI
                Action updateAction = new Action(() => updateUI(temp, humi));
                this.BeginInvoke(updateAction);

                lblStatus.Text = string.Format("数据正常 - {0}", lastReceiveTime.ToString("HH:mm:ss"));
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                nError++;
                tsslError.Text = "错误: " + nError;
                LogError("数据帧处理异常: " + ex.Message);

                Action errorAction = new Action(() =>
                {
                    lblStatus.Text = "数据解析错误: " + ex.Message;
                    lblStatus.ForeColor = Color.Red;
                });
                this.BeginInvoke(errorAction);
            }
        }

        private void AddDataPoint(float temp, float humi)
        {
            // 移除旧数据
            while (tempQueue.Count >= maxChartPoint)
            {
                tempQueue.Dequeue();
                humiQueue.Dequeue();
                if (timeQueue.Count > 0) timeQueue.Dequeue();
            }

            tempQueue.Enqueue(temp);
            humiQueue.Enqueue(humi);
            timeQueue.Enqueue(DateTime.Now);

            // 更新统计
            if (temp < tempMin) tempMin = temp;
            if (temp > tempMax) tempMax = temp;
            if (humi < humiMin) humiMin = humi;
            if (humi > humiMax) humiMax = humi;
            tempSum += temp;
            humiSum += humi;
            dataCount++;
        }

        private void updateUI(float temp, float humi)
        {
            // 当前值
            lblTempValue.Text = string.Format("{0:F1} ℃", temp);
            lblHumiValue.Text = string.Format("{0:F1} %", humi);
            lblUpdateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 更新图表
            UpdateChart();

            // 更新统计
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

            // 根据实际数据调整Y轴范围
            if (temps.Length > 0 && humis.Length > 0)
            {
                float minVal = Math.Min(temps.Min(), humis.Min()) - 5;
                float maxVal = Math.Max(temps.Max(), humis.Max()) + 5;
                chart1.ChartAreas["MainArea"].AxisY.Minimum = Math.Max(-50, minVal);
                chart1.ChartAreas["MainArea"].AxisY.Maximum = Math.Min(150, maxVal);
                chart1.ChartAreas["MainArea"].RecalculateAxesScale();
            }

            // 调整X轴范围
            chart1.ChartAreas["MainArea"].AxisX.Minimum = Math.Max(0, temps.Length - maxChartPoint);
            chart1.ChartAreas["MainArea"].AxisX.Maximum = Math.Max(maxChartPoint, temps.Length + 1);
        }

        private void UpdateChartSettings()
        {
            maxChartPoint = (int)nudMaxPoints.Value;

            // 调整队列大小
            while (tempQueue.Count > maxChartPoint)
            {
                tempQueue.Dequeue();
                humiQueue.Dequeue();
                if (timeQueue.Count > 0) timeQueue.Dequeue();
            }
        }

        // ==================== 数据解析 ====================
        private void getTempHumi(byte[] buffer, out float temp, out float humi)
        {
            temp = lastTemp;
            humi = lastHumi;

            switch (cbReadMode.SelectedIndex)
            {
                case 0: // 一次读取温湿度（浮点格式）
                    {
                        byte[] bTemp = new byte[4];
                        byte[] bHumi = new byte[4];
                        Array.Copy(buffer, 3, bTemp, 0, 4);
                        Array.Copy(buffer, 7, bHumi, 0, 4);
                        Array.Reverse(bTemp);
                        Array.Reverse(bHumi);
                        temp = BitConverter.ToSingle(bTemp, 0);
                        humi = BitConverter.ToSingle(bHumi, 0);
                    }
                    break;
                case 1: // 一次读取温湿度（整型格式）
                    {
                        int tempRaw = (buffer[3] << 8) | buffer[4];
                        int humiRaw = (buffer[5] << 8) | buffer[6];
                        temp = tempRaw / 10.0f;
                        humi = humiRaw / 10.0f;
                    }
                    break;
                case 2: // 单独读取温度（浮点格式）
                    {
                        byte[] bTemp = new byte[4];
                        Array.Copy(buffer, 3, bTemp, 0, 4);
                        Array.Reverse(bTemp);
                        temp = BitConverter.ToSingle(bTemp, 0);
                    }
                    break;
                case 3: // 单独读取湿度（浮点格式）
                    {
                        byte[] bHumi = new byte[4];
                        Array.Copy(buffer, 3, bHumi, 0, 4);
                        Array.Reverse(bHumi);
                        humi = BitConverter.ToSingle(bHumi, 0);
                    }
                    break;
                case 4: // 单独读取温度（整型格式）
                    {
                        int tempRaw = (buffer[3] << 8) | buffer[4];
                        temp = tempRaw / 10.0f;
                    }
                    break;
                case 5: // 单独读取湿度（整型格式）
                    {
                        int humiRaw = (buffer[3] << 8) | buffer[4];
                        humi = humiRaw / 10.0f;
                    }
                    break;
            }

            lastTemp = temp;
            lastHumi = humi;
        }

        // ==================== 数据校验 ====================
        private bool checkData(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 5)
                return false;

            // 检查地址和功能码
            if (buffer[0] != 0x01 || buffer[1] != 0x03)
                return false;

            // 检查数据长度是否匹配
            int dataLen = buffer[2];
            if (buffer.Length != dataLen + 5) // 地址+功能+长度 + 数据 + CRC(2)
                return false;

            // CRC校验
            if (!checkCRC(buffer))
                return false;

            return true;
        }

        private bool checkCRC(byte[] buffer)
        {
            if (buffer.Length < 2) return false;

            byte[] data = new byte[buffer.Length - 2];
            Array.Copy(buffer, data, data.Length);
            byte[] crc = calc_CRC(data);

            return (crc[0] == buffer[buffer.Length - 2] && crc[1] == buffer[buffer.Length - 1]);
        }

        private byte[] calc_CRC(byte[] ptbuf)
        {
            uint crc16 = 0xffff;
            uint temp;
            uint flag;
            int num = ptbuf.Length;

            for (int i = 0; i < num; i++)
            {
                temp = (uint)ptbuf[i];
                temp &= 0x00ff;
                crc16 = crc16 ^ temp;
                for (uint c = 0; c < 8; c++)
                {
                    flag = crc16 & 0x01;
                    crc16 = crc16 >> 1;
                    if (flag != 0)
                    {
                        crc16 = crc16 ^ 0x0a001;
                    }
                }
            }
            return BitConverter.GetBytes(crc16);
        }

        // ==================== 报警检查 ====================
        private void CheckAlarm(float temp, float humi)
        {
            bool alarm = false;
            List<string> alarms = new List<string>();

            float tempH = (float)nudTempHigh.Value;
            float tempL = (float)nudTempLow.Value;
            float humiH = (float)nudHumiHigh.Value;
            float humiL = (float)nudHumiLow.Value;

            if (temp > tempH) { alarms.Add(string.Format("温度过高: {0:F1}℃ > {1:F1}℃", temp, tempH)); alarm = true; }
            if (temp < tempL) { alarms.Add(string.Format("温度过低: {0:F1}℃ < {1:F1}℃", temp, tempL)); alarm = true; }
            if (humi > humiH) { alarms.Add(string.Format("湿度过高: {0:F1}% > {1:F1}%", humi, humiH)); alarm = true; }
            if (humi < humiL) { alarms.Add(string.Format("湿度过低: {0:F1}% < {1:F1}%", humi, humiL)); alarm = true; }

            if (alarm)
            {
                Action alarmAction = new Action(() =>
                {
                    lblStatus.Text = "⚠ 报警: " + string.Join("; ", alarms);
                    lblStatus.ForeColor = Color.Red;
                    lblTempValue.BackColor = (temp > tempH || temp < tempL) ? Color.LightPink : SystemColors.Control;
                    lblHumiValue.BackColor = (humi > humiH || humi < humiL) ? Color.LightBlue : SystemColors.Control;
                });
                this.BeginInvoke(alarmAction);

                // 记录报警到文件
                LogAlarmToFile(string.Join(", ", alarms));
            }
            else
            {
                Action resetAction = new Action(() =>
                {
                    lblTempValue.BackColor = SystemColors.Control;
                    lblHumiValue.BackColor = SystemColors.Control;
                });
                this.BeginInvoke(resetAction);
            }
        }

        // ==================== 数据记录 ====================
        private void InitLogFile()
        {
            try
            {
                string dataDir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir);

                logFilePath = Path.Combine(dataDir,
                    string.Format("sensor_data_{0:yyyyMMdd}.csv", DateTime.Now));

                if (!File.Exists(logFilePath))
                {
                    using (StreamWriter sw = new StreamWriter(logFilePath, false, Encoding.UTF8))
                    {
                        sw.WriteLine("时间,温度(℃),湿度(%),校验状态,备注");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("初始化日志文件失败: " + ex.Message);
            }
        }

        private void LogDataToFile(float temp, float humi)
        {
            try
            {
                // 每天新建一个日志文件
                string todayFile = Path.Combine(Path.GetDirectoryName(logFilePath),
                    string.Format("sensor_data_{0:yyyyMMdd}.csv", DateTime.Now));

                if (todayFile != logFilePath)
                {
                    logFilePath = todayFile;
                    if (!File.Exists(logFilePath))
                    {
                        using (StreamWriter sw = new StreamWriter(logFilePath, false, Encoding.UTF8))
                        {
                            sw.WriteLine("时间,温度(℃),湿度(%),校验状态,备注");
                        }
                    }
                }

                using (StreamWriter sw = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    string status = "正常";
                    string note = "";

                    // 检查报警状态
                    if (chkEnableAlarm.Checked)
                    {
                        float tempH = (float)nudTempHigh.Value;
                        float tempL = (float)nudTempLow.Value;
                        float humiH = (float)nudHumiHigh.Value;
                        float humiL = (float)nudHumiLow.Value;
                        if (temp > tempH || temp < tempL || humi > humiH || humi < humiL)
                        {
                            status = "报警";
                            note = string.Format("T:{0:F1}/H:{1:F1}", temp, humi);
                        }
                    }

                    sw.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss},{1:F1},{2:F1},{3},{4}",
                        DateTime.Now, temp, humi, status, note));
                }
            }
            catch (Exception ex)
            {
                LogError("记录数据到文件失败: " + ex.Message);
            }
        }

        private void LogAlarmToFile(string alarmMsg)
        {
            try
            {
                string alarmDir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(alarmDir))
                    Directory.CreateDirectory(alarmDir);

                string alarmFile = Path.Combine(alarmDir,
                    string.Format("alarm_{0:yyyyMMdd}.log", DateTime.Now));

                using (StreamWriter sw = new StreamWriter(alarmFile, true, Encoding.UTF8))
                {
                    sw.WriteLine("[{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, alarmMsg);
                }
            }
            catch { /* 报警日志记录失败不影响主流程 */ }
        }

        private void LogError(string msg)
        {
            try
            {
                string errDir = Path.Combine(Application.StartupPath, "DataLog");
                if (!Directory.Exists(errDir))
                    Directory.CreateDirectory(errDir);

                string errFile = Path.Combine(errDir,
                    string.Format("error_{0:yyyyMMdd}.log", DateTime.Now));

                using (StreamWriter sw = new StreamWriter(errFile, true, Encoding.UTF8))
                {
                    sw.WriteLine("[{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, msg);
                }
            }
            catch { /* 错误日志记录失败不影响主流程 */ }
        }

        // ==================== 导出CSV ====================
        private void btnExportCSV_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*";
                    sfd.DefaultExt = "csv";
                    sfd.FileName = string.Format("温湿度数据导出_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
                    sfd.Title = "导出温湿度数据";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                        {
                            sw.WriteLine("序号,温度(℃),湿度(%)");
                            float[] temps = tempQueue.ToArray();
                            float[] humis = humiQueue.ToArray();
                            for (int i = 0; i < temps.Length; i++)
                            {
                                sw.WriteLine(string.Format("{0},{1:F1},{2:F1}", i + 1, temps[i], humis[i]));
                            }
                        }
                        ShowTip(string.Format("数据已导出到: {0}\n共导出 {1} 条记录", sfd.FileName, tempQueue.Count));
                    }
                }
            }
            catch (Exception ex)
            {
                ShowTip("导出失败: " + ex.Message);
                LogError("导出CSV失败: " + ex.Message);
            }
        }

        // ==================== 清除图表 ====================
        private void btnClearChart_Click(object sender, EventArgs e)
        {
            ClearAllData();
        }

        private void ClearAllData()
        {
            tempQueue.Clear();
            humiQueue.Clear();
            timeQueue.Clear();
            dataCount = 0;
            tempMin = float.MaxValue;
            tempMax = float.MinValue;
            humiMin = float.MaxValue;
            humiMax = float.MinValue;
            tempSum = 0;
            humiSum = 0;

            chart1.Series["温度"].Points.Clear();
            chart1.Series["湿度"].Points.Clear();

            lblTempValue.Text = "--.- ℃";
            lblHumiValue.Text = "--.- %";
            lblUpdateTime.Text = "--";
            lblTempMin.Text = "最小: --";
            lblTempMax.Text = "最大: --";
            lblTempAvg.Text = "平均: --";
            lblHumiMin.Text = "最小: --";
            lblHumiMax.Text = "最大: --";
            lblHumiAvg.Text = "平均: --";
        }

        // ==================== 重置统计 ====================
        private void btnClearStats_Click(object sender, EventArgs e)
        {
            dataCount = 0;
            tempMin = float.MaxValue;
            tempMax = float.MinValue;
            humiMin = float.MaxValue;
            humiMax = float.MinValue;
            tempSum = 0;
            humiSum = 0;

            lblTempMin.Text = "最小: --";
            lblTempMax.Text = "最大: --";
            lblTempAvg.Text = "平均: --";
            lblHumiMin.Text = "最小: --";
            lblHumiMax.Text = "最大: --";
            lblHumiAvg.Text = "平均: --";
        }

        // ==================== 设置变更响应 ====================
        private void cbReadMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings.Default.ReadMode = cbReadMode.SelectedIndex;
            Settings.Default.Save();
        }

        private void nudInterval_ValueChanged(object sender, EventArgs e)
        {
            Settings.Default.SampleInterval = (int)nudInterval.Value;
            Settings.Default.Save();
            if (timer1 != null && isComOpen)
            {
                timer1.Interval = (int)nudInterval.Value;
            }
        }

        private void nudMaxPoints_ValueChanged(object sender, EventArgs e)
        {
            Settings.Default.MaxChartPoints = (int)nudMaxPoints.Value;
            Settings.Default.Save();
            UpdateChartSettings();
            if (!isComOpen)
            {
                UpdateChart();
            }
        }

        private void chkEnableAlarm_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.EnableAlarm = chkEnableAlarm.Checked;
            Settings.Default.Save();
        }

        private void chkDataLog_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.EnableDataLog = chkDataLog.Checked;
            Settings.Default.Save();
        }

        // ==================== 设置持久化 ====================
        private void LoadSettings()
        {
            try
            {
                Settings.Default.Upgrade();
                Settings.Default.Reload();
            }
            catch { /* 首次运行或配置损坏时使用默认值 */ }
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
            catch (Exception ex)
            {
                LogError("保存设置失败: " + ex.Message);
            }
        }

        // ==================== 窗体关闭 ====================
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAllSettings();
            if (isComOpen)
            {
                closeComPort();
            }
        }

        // ==================== 辅助方法 ====================
        private void ShowTip(string msg)
        {
            MessageBox.Show(msg, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateStatus()
        {
            // 线程安全的错误计数更新
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() =>
                {
                    tsslError.Text = "错误: " + nError;
                    tsslSend.Text = "发送: " + nSend;
                    tsslRecv.Text = "接收: " + nReceive;
                }));
            }
            else
            {
                tsslError.Text = "错误: " + nError;
                tsslSend.Text = "发送: " + nSend;
                tsslRecv.Text = "接收: " + nReceive;
            }
        }
    }
}
