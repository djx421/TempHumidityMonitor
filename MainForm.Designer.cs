using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TempHumidityMonitor
{
    partial class MainForm
    {
        // ==================== 顶层容器 ====================
        private SplitContainer splitContainer1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tsslStatus, tsslSend, tsslRecv, tsslError, tsslTime;

        // ==================== 顶部状态栏 ====================
        private Panel pnlTopBar;
        private Label lblDeviceStatus, lblClockTop;

        // ==================== 数据卡片 ====================
        private Panel pnlCards, pnlCardTemp, pnlCardHumi, pnlCardStatus, pnlCardAlarm;
        private Label lblCardTempTitle, lblCardTempValue, lblCardTempRange;
        private Label lblCardHumiTitle, lblCardHumiValue, lblCardHumiRange;
        private Label lblCardStatusTitle, lblCardStatusValue;
        private Label lblCardAlarmTitle, lblCardAlarmValue;

        // ==================== 数据表格 ====================
        private DataGridView dgvData;
        private DataGridViewTextBoxColumn colTime, colTemp, colHumi, colStatus;

        // ==================== Chart ====================
        private Chart chart1;

        // ==================== 左侧面板 ====================
        private GroupBox gbSerial, gbCollect, gbAlarm, gbData;
        private ComboBox cbComPort, cbBaudRate;
        private Button btnRefreshPorts, btnOpenCloseCom, btnManualSend;
        private ComboBox cbReadMode;
        private NumericUpDown nudInterval, nudMaxPoints;
        private TableLayoutPanel tlpCollect;
        private Label lblTempValue, lblHumiValue, lblUpdateTime;
        private Label lblTempMin, lblTempMax, lblTempAvg;
        private Label lblHumiMin, lblHumiMax, lblHumiAvg;
        private Button btnClearStats;
        private CheckBox chkEnableAlarm;
        private NumericUpDown nudTempHigh, nudTempLow, nudHumiHigh, nudHumiLow;
        private TableLayoutPanel tlpAlarm;
        private CheckBox chkDataLog;
        private Button btnExportCSV, btnClearChart;
        private TableLayoutPanel tlpData;
        private CheckBox chkSimMode;
        private Label lblStatus;
        private System.Windows.Forms.Timer timer1;
        private System.IO.Ports.SerialPort serialPort1;
        private System.ComponentModel.IContainer components;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ==================== 组件 ====================
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);

            // ==================== 顶层容器 ====================
            this.pnlTopBar = new Panel();
            this.lblDeviceStatus = new Label();
            this.lblClockTop = new Label();
            this.pnlCards = new Panel();
            this.pnlCardTemp = new Panel();
            this.lblCardTempTitle = new Label();
            this.lblCardTempValue = new Label();
            this.pnlCardHumi = new Panel();
            this.lblCardHumiTitle = new Label();
            this.lblCardHumiValue = new Label();
            this.pnlCardStatus = new Panel();
            this.pnlCardAlarm = new Panel();
            this.lblCardAlarmTitle = new Label();
            this.lblCardAlarmValue = new Label();
            this.dgvData = new DataGridView();
            this.colTime = new DataGridViewTextBoxColumn();
            this.colTemp = new DataGridViewTextBoxColumn();
            this.colHumi = new DataGridViewTextBoxColumn();
            this.colStatus = new DataGridViewTextBoxColumn();
            this.splitContainer1 = new SplitContainer();
            this.statusStrip1 = new StatusStrip();
            this.tsslStatus = new ToolStripStatusLabel();
            this.tsslSend = new ToolStripStatusLabel();
            this.tsslRecv = new ToolStripStatusLabel();
            this.tsslError = new ToolStripStatusLabel();
            this.tsslTime = new ToolStripStatusLabel();

            // ==================== 模拟模式 ====================
            this.chkSimMode = new CheckBox();

            // ==================== 左侧面板控件 ====================
            this.gbSerial = new GroupBox();
            this.cbComPort = new ComboBox();
            this.btnRefreshPorts = new Button();
            this.cbBaudRate = new ComboBox();
            this.btnOpenCloseCom = new Button();
            this.btnManualSend = new Button();
            this.gbCollect = new GroupBox();
            this.tlpCollect = new TableLayoutPanel();
            this.cbReadMode = new ComboBox();
            this.nudInterval = new NumericUpDown();
            this.nudMaxPoints = new NumericUpDown();
            this.lblTempValue = new Label();
            this.lblHumiValue = new Label();
            this.lblUpdateTime = new Label();
            this.lblTempMin = new Label();
            this.lblTempMax = new Label();
            this.lblTempAvg = new Label();
            this.lblHumiMin = new Label();
            this.lblHumiMax = new Label();
            this.lblHumiAvg = new Label();
            this.btnClearStats = new Button();
            this.gbAlarm = new GroupBox();
            this.tlpAlarm = new TableLayoutPanel();
            this.chkEnableAlarm = new CheckBox();
            this.nudTempHigh = new NumericUpDown();
            this.nudTempLow = new NumericUpDown();
            this.nudHumiHigh = new NumericUpDown();
            this.nudHumiLow = new NumericUpDown();
            this.gbData = new GroupBox();
            this.tlpData = new TableLayoutPanel();
            this.chkDataLog = new CheckBox();
            this.btnExportCSV = new Button();
            this.btnClearChart = new Button();
            this.lblStatus = new Label();

            // ==================== Chart ====================
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new ChartArea("MainArea");
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new Legend("Legend");
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new Series("温度");
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new Series("湿度");
            this.chart1 = new Chart();

            // ==================== 开始布局 ====================
            this.SuspendLayout();
            this.pnlTopBar.SuspendLayout();
            this.pnlCards.SuspendLayout();
            this.pnlCardTemp.SuspendLayout();
            this.pnlCardHumi.SuspendLayout();
            this.pnlCardStatus.SuspendLayout();
            this.pnlCardAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.gbSerial.SuspendLayout();
            this.gbCollect.SuspendLayout();
            this.tlpCollect.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPoints)).BeginInit();
            this.gbAlarm.SuspendLayout();
            this.tlpAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiLow)).BeginInit();
            this.gbData.SuspendLayout();
            this.tlpData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();

            // ==================== 表单属性 ====================
            this.AutoScaleDimensions = new SizeF(120F, 120F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = Color.FromArgb(240, 244, 248);
            this.ClientSize = new Size(1400, 900);
            this.MinimumSize = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "温湿度传感器监控系统";
            this.FormClosing += new FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new EventHandler(this.MainForm_Load);
            this.Shown += new EventHandler(this.MainForm_Shown);

            // ==================== 顶部状态栏 ====================
            this.pnlTopBar.BackColor = Color.White;
            this.pnlTopBar.Dock = DockStyle.Top;
            this.pnlTopBar.Height = 56;
            this.pnlTopBar.Padding = new Padding(16, 0, 16, 0);

            this.lblDeviceStatus.Text = "○ 串口未打开     ○ 等待采集     ○ 数据就绪";
            this.lblDeviceStatus.Font = new Font("微软雅黑", 10F);
            this.lblDeviceStatus.ForeColor = Color.FromArgb(31, 41, 55);
            this.lblDeviceStatus.Location = new Point(16, 16);
            this.lblDeviceStatus.Size = new Size(700, 24);
            this.pnlTopBar.Controls.Add(this.lblDeviceStatus);

            this.lblClockTop.Text = "2026-06-03 00:00:00";
            this.lblClockTop.Font = new Font("微软雅黑", 10F);
            this.lblClockTop.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblClockTop.TextAlign = ContentAlignment.MiddleRight;
            this.lblClockTop.Dock = DockStyle.Right;
            this.lblClockTop.Width = 220;
            this.pnlTopBar.Controls.Add(this.lblClockTop);

            // ==================== SplitContainer ====================
            this.splitContainer1.Dock = DockStyle.Fill;
            this.splitContainer1.FixedPanel = FixedPanel.Panel1;
            this.splitContainer1.BackColor = Color.FromArgb(229, 231, 235);
            this.splitContainer1.Size = new Size(1400, 818);
            this.splitContainer1.SplitterDistance = 228;
            this.splitContainer1.SplitterWidth = 4;

            // ---- Panel1 (左侧面板) ----
            this.splitContainer1.Panel1.BackColor = Color.FromArgb(245, 247, 250);
            this.splitContainer1.Panel1.Padding = new Padding(8);
            this.splitContainer1.Panel1.AutoScroll = true;

            Panel pnlLeft = this.splitContainer1.Panel1;
            int top = 8;

            // chkSimMode
            this.chkSimMode.Text = "模拟模式（无需硬件演示）";
            this.chkSimMode.Font = new Font("微软雅黑", 9F);
            this.chkSimMode.Location = new Point(8, top);
            this.chkSimMode.Size = new Size(212,24);
            pnlLeft.Controls.Add(this.chkSimMode);
            top += 30;

            // ---- gbSerial ----
            this.gbSerial.Text = "设备连接";
            this.gbSerial.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            this.gbSerial.Location = new Point(8, top);
            this.gbSerial.Size = new Size(212,140);
            this.cbComPort.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbComPort.Location = new Point(70, 24); this.cbComPort.Size = new Size(130, 23);
            this.btnRefreshPorts.Text = "刷新"; this.btnRefreshPorts.Location = new Point(206, 24); this.btnRefreshPorts.Size = new Size(48, 23);
            this.cbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbBaudRate.Items.AddRange(new object[] { "4800", "9600", "19200", "38400", "57600", "115200" });
            this.cbBaudRate.SelectedIndex = 1;
            this.cbBaudRate.Location = new Point(70, 56); this.cbBaudRate.Size = new Size(184, 23);
            this.btnOpenCloseCom.Text = "打开串口"; this.btnOpenCloseCom.Location = new Point(12, 96); this.btnOpenCloseCom.Size = new Size(115, 32);
            this.btnManualSend.Text = "手动采集"; this.btnManualSend.Location = new Point(133, 96); this.btnManualSend.Size = new Size(115, 32);
            this.gbSerial.Controls.Add(this.cbComPort);
            this.gbSerial.Controls.Add(this.btnRefreshPorts);
            this.gbSerial.Controls.Add(this.cbBaudRate);
            this.gbSerial.Controls.Add(this.btnOpenCloseCom);
            this.gbSerial.Controls.Add(this.btnManualSend);
            pnlLeft.Controls.Add(this.gbSerial);
            top += 148;

            // ---- gbCollect ----
            this.gbCollect.Text = "采集设置";
            this.gbCollect.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            this.gbCollect.Location = new Point(8, top);
            this.gbCollect.Size = new Size(212,110);
            this.tlpCollect.Dock = DockStyle.Fill;
            this.tlpCollect.ColumnCount = 2; this.tlpCollect.RowCount = 3;
            this.tlpCollect.Padding = new Padding(4, 2, 4, 2);
            this.tlpCollect.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            this.tlpCollect.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.tlpCollect.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            this.tlpCollect.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            this.tlpCollect.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            this.cbReadMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbReadMode.Items.AddRange(new object[] { "一次读取(浮点)", "一次读取(整型)", "单独读温度(浮点)", "单独读湿度(浮点)", "单独读温度(整型)", "单独读湿度(整型)" });
            this.cbReadMode.SelectedIndex = 0; this.cbReadMode.Dock = DockStyle.Fill;
            this.nudInterval.Minimum = 200; this.nudInterval.Maximum = 60000; this.nudInterval.Increment = 100; this.nudInterval.Value = 1000;
            this.nudInterval.TextAlign = HorizontalAlignment.Center; this.nudInterval.Dock = DockStyle.Fill;
            this.nudMaxPoints.Minimum = 10; this.nudMaxPoints.Maximum = 500; this.nudMaxPoints.Increment = 10; this.nudMaxPoints.Value = 30;
            this.nudMaxPoints.TextAlign = HorizontalAlignment.Center; this.nudMaxPoints.Dock = DockStyle.Fill;
            this.tlpCollect.Controls.Add(this.cbReadMode, 1, 0);
            this.tlpCollect.Controls.Add(this.nudInterval, 1, 1);
            this.tlpCollect.Controls.Add(this.nudMaxPoints, 1, 2);
            this.gbCollect.Controls.Add(this.tlpCollect);
            pnlLeft.Controls.Add(this.gbCollect);
            top += 118;

            // ---- gbAlarm ----
            this.gbAlarm.Text = "报警设置";
            this.gbAlarm.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            this.gbAlarm.Location = new Point(8, top);
            this.gbAlarm.Size = new Size(212,165);
            this.tlpAlarm.Dock = DockStyle.Fill;
            this.tlpAlarm.ColumnCount = 2; this.tlpAlarm.RowCount = 5;
            this.tlpAlarm.Padding = new Padding(4, 2, 4, 2);
            this.tlpAlarm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            this.tlpAlarm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.tlpAlarm.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            this.tlpAlarm.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            this.tlpAlarm.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            this.tlpAlarm.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            this.tlpAlarm.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            this.chkEnableAlarm.Text = "启用报警"; this.chkEnableAlarm.Dock = DockStyle.Fill;
            this.nudTempHigh.Minimum = -50; this.nudTempHigh.Maximum = 150; this.nudTempHigh.Increment = 0.5M; this.nudTempHigh.Value = 40;
            this.nudTempHigh.DecimalPlaces = 1; this.nudTempHigh.TextAlign = HorizontalAlignment.Center; this.nudTempHigh.Dock = DockStyle.Fill;
            this.nudTempLow.Minimum = -50; this.nudTempLow.Maximum = 150; this.nudTempLow.Increment = 0.5M; this.nudTempLow.Value = 0;
            this.nudTempLow.DecimalPlaces = 1; this.nudTempLow.TextAlign = HorizontalAlignment.Center; this.nudTempLow.Dock = DockStyle.Fill;
            this.nudHumiHigh.Minimum = 0; this.nudHumiHigh.Maximum = 100; this.nudHumiHigh.Increment = 1; this.nudHumiHigh.Value = 80;
            this.nudHumiHigh.TextAlign = HorizontalAlignment.Center; this.nudHumiHigh.Dock = DockStyle.Fill;
            this.nudHumiLow.Minimum = 0; this.nudHumiLow.Maximum = 100; this.nudHumiLow.Increment = 1; this.nudHumiLow.Value = 20;
            this.nudHumiLow.TextAlign = HorizontalAlignment.Center; this.nudHumiLow.Dock = DockStyle.Fill;
            this.tlpAlarm.Controls.Add(this.chkEnableAlarm, 1, 0);
            this.tlpAlarm.Controls.Add(this.nudTempHigh, 1, 1);
            this.tlpAlarm.Controls.Add(this.nudTempLow, 1, 2);
            this.tlpAlarm.Controls.Add(this.nudHumiHigh, 1, 3);
            this.tlpAlarm.Controls.Add(this.nudHumiLow, 1, 4);
            this.gbAlarm.Controls.Add(this.tlpAlarm);
            pnlLeft.Controls.Add(this.gbAlarm);
            top += 173;

            // ---- gbData ----
            this.gbData.Text = "数据管理";
            this.gbData.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            this.gbData.Location = new Point(8, top);
            this.gbData.Size = new Size(212,105);
            this.tlpData.Dock = DockStyle.Fill;
            this.tlpData.ColumnCount = 1; this.tlpData.RowCount = 3;
            this.tlpData.Padding = new Padding(4, 2, 4, 2);
            this.tlpData.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.tlpData.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            this.tlpData.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            this.tlpData.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            this.chkDataLog.Text = "启用数据记录"; this.chkDataLog.Checked = true; this.chkDataLog.Dock = DockStyle.Fill;
            this.btnExportCSV.Text = "导出CSV"; this.btnExportCSV.Dock = DockStyle.Fill;
            this.btnClearChart.Text = "清除图表"; this.btnClearChart.Dock = DockStyle.Fill;
            this.tlpData.Controls.Add(this.chkDataLog, 0, 0);
            this.tlpData.Controls.Add(this.btnExportCSV, 0, 1);
            this.tlpData.Controls.Add(this.btnClearChart, 0, 2);
            this.gbData.Controls.Add(this.tlpData);
            pnlLeft.Controls.Add(this.gbData);
            top += 113;

            // lblStatus
            this.lblStatus.Text = "就绪";
            this.lblStatus.Font = new Font("微软雅黑", 8F);
            this.lblStatus.ForeColor = Color.FromArgb(156, 163, 175);
            this.lblStatus.Location = new Point(8, top + 4);
            this.lblStatus.Size = new Size(212,20);
            pnlLeft.Controls.Add(this.lblStatus);

            // ==================== Panel2 (Dashboard) ====================
            Panel pnlRight = this.splitContainer1.Panel2;
            pnlRight.BackColor = Color.FromArgb(245, 247, 250);
            pnlRight.Padding = new Padding(12);

            // --- 数据卡片行 ---
            this.pnlCards.BackColor = Color.Transparent;
            this.pnlCards.Dock = DockStyle.Top;
            this.pnlCards.Height = 115;
            this.pnlCards.Padding = new Padding(0, 0, 0, 0);

            int cardW = 268; int cardH = 110; int gap = 10;
            int cx = 0;
            // 温度卡片
            this.pnlCardTemp.BackColor = Color.White;
            this.pnlCardTemp.Location = new Point(cx, 2); this.pnlCardTemp.Size = new Size(cardW, cardH);
            this.lblCardTempTitle.Text = "当前温度"; this.lblCardTempTitle.Font = new Font("微软雅黑", 9F); this.lblCardTempTitle.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblCardTempTitle.Location = new Point(12, 10); this.lblCardTempTitle.Size = new Size(120, 18);
            this.lblCardTempValue.Text = "--.-℃"; this.lblCardTempValue.Font = new Font("微软雅黑", 26F, FontStyle.Bold); this.lblCardTempValue.ForeColor = Color.FromArgb(255, 77, 79);
            this.lblCardTempValue.Location = new Point(12, 32); this.lblCardTempValue.Size = new Size(160, 42);
            this.lblCardTempRange = new Label() { Text = "↑--  ↓--", Font = new Font("微软雅黑", 8F), ForeColor = Color.FromArgb(156, 163, 175), Location = new Point(12, 78), Size = new Size(240, 20) };
            this.pnlCardTemp.Controls.Add(this.lblCardTempTitle);
            this.pnlCardTemp.Controls.Add(this.lblCardTempValue);
            this.pnlCardTemp.Controls.Add(this.lblCardTempRange);
            this.pnlCards.Controls.Add(this.pnlCardTemp);
            cx += cardW + gap;

            // 湿度卡片
            this.pnlCardHumi.BackColor = Color.White;
            this.pnlCardHumi.Location = new Point(cx, 2); this.pnlCardHumi.Size = new Size(cardW, cardH);
            this.lblCardHumiTitle.Text = "当前湿度"; this.lblCardHumiTitle.Font = new Font("微软雅黑", 9F); this.lblCardHumiTitle.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblCardHumiTitle.Location = new Point(12, 10); this.lblCardHumiTitle.Size = new Size(120, 18);
            this.lblCardHumiValue.Text = "--.-%"; this.lblCardHumiValue.Font = new Font("微软雅黑", 26F, FontStyle.Bold); this.lblCardHumiValue.ForeColor = Color.FromArgb(22, 119, 255);
            this.lblCardHumiValue.Location = new Point(12, 32); this.lblCardHumiValue.Size = new Size(160, 42);
            this.lblCardHumiRange = new Label() { Text = "↑--  ↓--", Font = new Font("微软雅黑", 8F), ForeColor = Color.FromArgb(156, 163, 175), Location = new Point(12, 78), Size = new Size(240, 20) };
            this.pnlCardHumi.Controls.Add(this.lblCardHumiTitle);
            this.pnlCardHumi.Controls.Add(this.lblCardHumiValue);
            this.pnlCardHumi.Controls.Add(this.lblCardHumiRange);
            this.pnlCards.Controls.Add(this.pnlCardHumi);
            cx += cardW + gap;

            // 设备状态卡片
            this.pnlCardStatus.BackColor = Color.White;
            this.pnlCardStatus.Location = new Point(cx, 2); this.pnlCardStatus.Size = new Size(cardW, cardH);
            this.lblCardStatusTitle = new Label() { Text = "设备状态", Font = new Font("微软雅黑", 9F), ForeColor = Color.FromArgb(107, 114, 128), Location = new Point(12, 10), Size = new Size(120, 18) };
            this.lblCardStatusValue = new Label() { Text = "离线", Font = new Font("微软雅黑", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(156, 163, 175), Location = new Point(12, 42), Size = new Size(200, 36) };
            this.pnlCardStatus.Controls.Add(this.lblCardStatusTitle);
            this.pnlCardStatus.Controls.Add(this.lblCardStatusValue);
            this.pnlCards.Controls.Add(this.pnlCardStatus);
            cx += cardW + gap;

            // 报警状态卡片
            this.pnlCardAlarm.BackColor = Color.White;
            this.pnlCardAlarm.Location = new Point(cx, 2); this.pnlCardAlarm.Size = new Size(cardW, cardH);
            this.lblCardAlarmTitle.Text = "报警状态"; this.lblCardAlarmTitle.Font = new Font("微软雅黑", 9F); this.lblCardAlarmTitle.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblCardAlarmTitle.Location = new Point(12, 10); this.lblCardAlarmTitle.Size = new Size(120, 18);
            this.lblCardAlarmValue.Text = "正常"; this.lblCardAlarmValue.Font = new Font("微软雅黑", 18F, FontStyle.Bold); this.lblCardAlarmValue.ForeColor = Color.FromArgb(82, 196, 26);
            this.lblCardAlarmValue.Location = new Point(12, 42); this.lblCardAlarmValue.Size = new Size(200, 36);
            this.pnlCardAlarm.Controls.Add(this.lblCardAlarmTitle);
            this.pnlCardAlarm.Controls.Add(this.lblCardAlarmValue);
            this.pnlCards.Controls.Add(this.pnlCardAlarm);

            pnlRight.Controls.Add(this.pnlCards);

            // --- 统计标签行（卡片下方） ---
            Panel pnlStats = new Panel();
            pnlStats.BackColor = Color.Transparent;
            pnlStats.Dock = DockStyle.Top;
            pnlStats.Height = 36;
            this.lblTempMin.Text = "最低:--"; this.lblTempMin.Font = new Font("微软雅黑", 9F); this.lblTempMin.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblTempMin.Location = new Point(0, 6); this.lblTempMin.Size = new Size(130, 24);
            this.lblTempMax.Text = "最高:--"; this.lblTempMax.Font = new Font("微软雅黑", 9F); this.lblTempMax.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblTempMax.Location = new Point(140, 6); this.lblTempMax.Size = new Size(130, 24);
            this.lblTempAvg.Text = "平均:--"; this.lblTempAvg.Font = new Font("微软雅黑", 9F); this.lblTempAvg.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblTempAvg.Location = new Point(280, 6); this.lblTempAvg.Size = new Size(130, 24);
            this.lblHumiMin.Text = "最低:--"; this.lblHumiMin.Font = new Font("微软雅黑", 9F); this.lblHumiMin.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblHumiMin.Location = new Point(450, 6); this.lblHumiMin.Size = new Size(130, 24);
            this.lblHumiMax.Text = "最高:--"; this.lblHumiMax.Font = new Font("微软雅黑", 9F); this.lblHumiMax.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblHumiMax.Location = new Point(590, 6); this.lblHumiMax.Size = new Size(130, 24);
            this.lblHumiAvg.Text = "平均:--"; this.lblHumiAvg.Font = new Font("微软雅黑", 9F); this.lblHumiAvg.ForeColor = Color.FromArgb(107, 114, 128);
            this.lblHumiAvg.Location = new Point(730, 6); this.lblHumiAvg.Size = new Size(130, 24);
            this.btnClearStats.Text = "重置"; this.btnClearStats.Size = new Size(60, 24); this.btnClearStats.Location = new Point(900, 4);
            pnlStats.Controls.Add(this.lblTempMin); pnlStats.Controls.Add(this.lblTempMax); pnlStats.Controls.Add(this.lblTempAvg);
            pnlStats.Controls.Add(this.lblHumiMin); pnlStats.Controls.Add(this.lblHumiMax); pnlStats.Controls.Add(this.lblHumiAvg);
            pnlStats.Controls.Add(this.btnClearStats);
            pnlRight.Controls.Add(pnlStats);

            // --- Chart ---
            this.chartArea1_AxisSetup(chartArea1);
            legend1.Docking = Docking.Top; legend1.Font = new Font("微软雅黑", 9F);
            series1.ChartType = SeriesChartType.Spline; series1.Color = Color.FromArgb(255, 77, 79); series1.BorderWidth = 3;
            series1.MarkerStyle = MarkerStyle.Circle; series1.MarkerSize = 4; series1.MarkerColor = Color.FromArgb(255, 77, 79);
            series1.LegendText = "温度 (℃)";
            series2.ChartType = SeriesChartType.Spline; series2.Color = Color.FromArgb(22, 119, 255); series2.BorderWidth = 3;
            series2.MarkerStyle = MarkerStyle.Diamond; series2.MarkerSize = 4; series2.MarkerColor = Color.FromArgb(22, 119, 255);
            series2.LegendText = "湿度 (%)";
            // --- DataGridView (必须在Chart之前添加,否则Dock=Fill的Chart会覆盖它) ---
            this.dgvData.BackgroundColor = Color.White;
            this.dgvData.BorderStyle = BorderStyle.None;
            this.dgvData.Dock = DockStyle.Bottom;
            this.dgvData.Height = 180;
            this.dgvData.AllowUserToAddRows = false;
            this.dgvData.ReadOnly = true;
            this.dgvData.RowHeadersVisible = false;
            this.dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvData.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 119, 255);
            this.dgvData.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            this.dgvData.ColumnHeadersDefaultCellStyle.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            this.dgvData.DefaultCellStyle.Font = new Font("微软雅黑", 9F);
            this.dgvData.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 244, 255);
            this.dgvData.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
            this.colTime.HeaderText = "时间"; this.colTime.FillWeight = 35F;
            this.colTemp.HeaderText = "温度(℃)"; this.colTemp.FillWeight = 20F;
            this.colHumi.HeaderText = "湿度(%)"; this.colHumi.FillWeight = 20F;
            this.colStatus.HeaderText = "状态"; this.colStatus.FillWeight = 25F;
            this.dgvData.Columns.AddRange(new DataGridViewColumn[] { this.colTime, this.colTemp, this.colHumi, this.colStatus });
            pnlRight.Controls.Add(this.dgvData);

            // --- Chart (Dock=Fill最后添加,接收剩余空间) ---
            this.chart1.BackColor = Color.White;
            this.chart1.Dock = DockStyle.Fill;
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Legends.Add(legend1);
            this.chart1.Series.Add(series1);
            this.chart1.Series.Add(series2);
            pnlRight.Controls.Add(this.chart1);

            // ==================== StatusStrip ====================
            this.statusStrip1.Dock = DockStyle.Bottom;
            this.statusStrip1.BackColor = Color.White;
            this.tsslStatus.Text = "● 串口未打开"; this.tsslStatus.ForeColor = Color.Gray; this.tsslStatus.Width = 140;
            this.tsslSend.Text = "发送: 0"; this.tsslRecv.Text = "接收: 0";
            this.tsslError.Text = "错误: 0"; this.tsslError.ForeColor = Color.Orange;
            this.tsslTime.Text = "00:00:00";
            this.statusStrip1.Items.AddRange(new ToolStripItem[] { this.tsslStatus, this.tsslSend, this.tsslRecv, this.tsslError, new ToolStripStatusLabel("  "), this.tsslTime });

            // ==================== 添加顶层到 Form ====================
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.pnlTopBar);

            // ==================== 事件绑定 ====================
            this.chkSimMode.CheckedChanged += new EventHandler(this.chkSimMode_CheckedChanged);
            this.btnRefreshPorts.Click += new EventHandler(this.btnRefreshPorts_Click);
            this.btnOpenCloseCom.Click += new EventHandler(this.btnOpenCloseCom_Click);
            this.btnManualSend.Click += new EventHandler(this.btnManualSend_Click);
            this.btnClearStats.Click += new EventHandler(this.btnClearStats_Click);
            this.btnExportCSV.Click += new EventHandler(this.btnExportCSV_Click);
            this.btnClearChart.Click += new EventHandler(this.btnClearChart_Click);
            this.chkEnableAlarm.CheckedChanged += new EventHandler(this.chkEnableAlarm_CheckedChanged);
            this.chkDataLog.CheckedChanged += new EventHandler(this.chkDataLog_CheckedChanged);
            this.cbReadMode.SelectedIndexChanged += new EventHandler(this.cbReadMode_SelectedIndexChanged);
            this.nudInterval.ValueChanged += new EventHandler(this.nudInterval_ValueChanged);
            this.nudMaxPoints.ValueChanged += new EventHandler(this.nudMaxPoints_ValueChanged);
            this.timer1.Tick += new EventHandler(this.timer1_Tick);
            this.serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
            this.serialPort1.ErrorReceived += new System.IO.Ports.SerialErrorReceivedEventHandler(this.serialPort1_ErrorReceived);

            // ==================== 恢复布局 ====================
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvData)).EndInit();
            this.tlpData.ResumeLayout(false); this.gbData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiHigh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempHigh)).EndInit();
            this.tlpAlarm.ResumeLayout(false); this.gbAlarm.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPoints)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).EndInit();
            this.tlpCollect.ResumeLayout(false); this.gbCollect.ResumeLayout(false);
            this.gbSerial.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false); this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.pnlCardAlarm.ResumeLayout(false);
            this.pnlCardStatus.ResumeLayout(false);
            this.pnlCardHumi.ResumeLayout(false);
            this.pnlCardTemp.ResumeLayout(false);
            this.pnlCards.ResumeLayout(false);
            this.pnlTopBar.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void chartArea1_AxisSetup(ChartArea area)
        {
            area.AxisX.Title = "采样点序号";
            area.AxisX.TitleFont = new Font("微软雅黑", 9F);
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(229, 231, 235);
            area.AxisX.LabelStyle.Font = new Font("微软雅黑", 8F);
            area.AxisY.Title = "数值";
            area.AxisY.TitleFont = new Font("微软雅黑", 9F);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(229, 231, 235);
            area.AxisY.Minimum = -10; area.AxisY.Maximum = 100; area.AxisY.Interval = 10;
            area.AxisY.LabelStyle.Font = new Font("微软雅黑", 8F);
            area.CursorX.IsUserEnabled = true; area.CursorX.IsUserSelectionEnabled = true;
            area.AxisX.ScrollBar.Enabled = true;
            area.AxisX.ScaleView.Zoomable = true;
            area.BackColor = Color.White;
        }
    }
}
