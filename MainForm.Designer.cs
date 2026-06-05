using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TempHumidityMonitor
{
    partial class MainForm
    {
        private IContainer components = null;

        // ==================== 主容器 ====================
        private SplitContainer splitContainer1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tsslStatus, tsslSend, tsslRecv, tsslError, tsslTime;

        // ==================== 左侧控件 ====================
        private CheckBox chkSimMode;
        private GroupBox gbSerial, gbCollect, gbAlarm, gbData;
        private ComboBox cbComPort, cbBaudRate, cbReadMode;
        private Button btnRefreshPorts, btnOpenCloseCom, btnManualSend;
        private Button btnExportCSV, btnClearChart, btnCleanDB;
        private NumericUpDown nudRetainDays;
        private Label lblRetainDays;
        private CheckBox chkEnableAlarm, chkDataLog;
        private NumericUpDown nudInterval, nudMaxPoints;
        private NumericUpDown nudTempHigh, nudTempLow, nudHumiHigh, nudHumiLow;
        private NumericUpDown nudPressureHigh, nudPressureLow;
        private Label lblTempValue, lblHumiValue, lblPressureValue, lblUpdateTime;
        private Label lblTempMin, lblTempMax, lblTempAvg;
        private Label lblHumiMin, lblHumiMax, lblHumiAvg;
        private Label lblPressureMin, lblPressureMax, lblPressureAvg;
        private Label lblStatus;

        // ==================== 右侧控件 ====================
        private Button btnTabCurrent, btnTabHistory;
        private Button btnToggleRead;
        private Chart chart1;
        private GroupBox gbCurrent, gbStatsTemp, gbStatsHumi, gbStatsPress;
        private GroupBox gbHistory;
        private Button btnQueryHistory, btnExportHistory;
        private CheckBox chkAlarmOnly;
        private DateTimePicker dtpStart, dtpEnd;
        private DataGridView dgvHistory;

        // ==================== 非可视组件 ====================
        private Timer timer1;
        private System.IO.Ports.SerialPort serialPort1;
        private NotifyIcon notifyIcon1;
        private ContextMenuStrip cmsTray;

        // ==================== Dispose ====================
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        // ==================== InitializeComponent ====================
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.chkSimMode = new System.Windows.Forms.CheckBox();
            this.gbSerial = new System.Windows.Forms.GroupBox();
            this.lblPortLabel = new System.Windows.Forms.Label();
            this.lblBaudLabel = new System.Windows.Forms.Label();
            this.cbComPort = new System.Windows.Forms.ComboBox();
            this.btnRefreshPorts = new System.Windows.Forms.Button();
            this.cbBaudRate = new System.Windows.Forms.ComboBox();
            this.btnOpenCloseCom = new System.Windows.Forms.Button();
            this.btnManualSend = new System.Windows.Forms.Button();
            this.gbCollect = new System.Windows.Forms.GroupBox();
            this.lblReadMode = new System.Windows.Forms.Label();
            this.lblInterval = new System.Windows.Forms.Label();
            this.lblMaxPoints = new System.Windows.Forms.Label();
            this.cbReadMode = new System.Windows.Forms.ComboBox();
            this.nudInterval = new System.Windows.Forms.NumericUpDown();
            this.nudMaxPoints = new System.Windows.Forms.NumericUpDown();
            this.gbCurrent = new System.Windows.Forms.GroupBox();
            this.lblTempLabel = new System.Windows.Forms.Label();
            this.lblHumiLabel = new System.Windows.Forms.Label();
            this.lblPressureLabel = new System.Windows.Forms.Label();
            this.lblUpdateLabel = new System.Windows.Forms.Label();
            this.lblTempValue = new System.Windows.Forms.Label();
            this.lblHumiValue = new System.Windows.Forms.Label();
            this.lblPressureValue = new System.Windows.Forms.Label();
            this.lblUpdateTime = new System.Windows.Forms.Label();
            this.gbStatsTemp = new System.Windows.Forms.GroupBox();
            this.gbStatsHumi = new System.Windows.Forms.GroupBox();
            this.gbStatsPress = new System.Windows.Forms.GroupBox();
            this.lblMinT = new System.Windows.Forms.Label();
            this.lblMaxT = new System.Windows.Forms.Label();
            this.lblAvgT = new System.Windows.Forms.Label();
            this.lblMinH = new System.Windows.Forms.Label();
            this.lblMaxH = new System.Windows.Forms.Label();
            this.lblAvgH = new System.Windows.Forms.Label();
            this.lblMinP = new System.Windows.Forms.Label();
            this.lblMaxP = new System.Windows.Forms.Label();
            this.lblAvgP = new System.Windows.Forms.Label();
            this.lblTempMin = new System.Windows.Forms.Label();
            this.lblTempMax = new System.Windows.Forms.Label();
            this.lblTempAvg = new System.Windows.Forms.Label();
            this.lblHumiMin = new System.Windows.Forms.Label();
            this.lblHumiMax = new System.Windows.Forms.Label();
            this.lblHumiAvg = new System.Windows.Forms.Label();
            this.lblPressureMin = new System.Windows.Forms.Label();
            this.lblPressureMax = new System.Windows.Forms.Label();
            this.lblPressureAvg = new System.Windows.Forms.Label();
            this.gbAlarm = new System.Windows.Forms.GroupBox();
            this.lblTempHigh = new System.Windows.Forms.Label();
            this.lblTempLow = new System.Windows.Forms.Label();
            this.lblHumiHigh = new System.Windows.Forms.Label();
            this.lblHumiLow = new System.Windows.Forms.Label();
            this.lblPressHigh = new System.Windows.Forms.Label();
            this.lblPressLow = new System.Windows.Forms.Label();
            this.chkEnableAlarm = new System.Windows.Forms.CheckBox();
            this.nudTempHigh = new System.Windows.Forms.NumericUpDown();
            this.nudTempLow = new System.Windows.Forms.NumericUpDown();
            this.nudHumiHigh = new System.Windows.Forms.NumericUpDown();
            this.nudHumiLow = new System.Windows.Forms.NumericUpDown();
            this.nudPressureHigh = new System.Windows.Forms.NumericUpDown();
            this.nudPressureLow = new System.Windows.Forms.NumericUpDown();
            this.gbData = new System.Windows.Forms.GroupBox();
            this.chkDataLog = new System.Windows.Forms.CheckBox();
            this.btnExportCSV = new System.Windows.Forms.Button();
            this.btnClearChart = new System.Windows.Forms.Button();
            this.btnCleanDB = new System.Windows.Forms.Button();
            this.nudRetainDays = new System.Windows.Forms.NumericUpDown();
            this.lblRetainDays = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnTabCurrent = new System.Windows.Forms.Button();
            this.btnTabHistory = new System.Windows.Forms.Button();
            this.btnToggleRead = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.gbHistory = new System.Windows.Forms.GroupBox();
            this.lblStart = new System.Windows.Forms.Label();
            this.lblEnd = new System.Windows.Forms.Label();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.btnQueryHistory = new System.Windows.Forms.Button();
            this.btnExportHistory = new System.Windows.Forms.Button();
            this.chkAlarmOnly = new System.Windows.Forms.CheckBox();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslSend = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslRecv = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslError = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.cmsTray = new System.Windows.Forms.ContextMenuStrip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.gbSerial.SuspendLayout();
            this.gbCollect.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPoints)).BeginInit();
            this.gbCurrent.SuspendLayout();
            this.gbStatsTemp.SuspendLayout();
            this.gbStatsHumi.SuspendLayout();
            this.gbStatsPress.SuspendLayout();
            this.gbHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.gbAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPressureHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPressureLow)).BeginInit();
            this.gbData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRetainDays)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.chkSimMode);
            this.splitContainer1.Panel1.Controls.Add(this.gbSerial);
            this.splitContainer1.Panel1.Controls.Add(this.gbCollect);
            this.splitContainer1.Panel1.Controls.Add(this.gbAlarm);
            this.splitContainer1.Panel1.Controls.Add(this.gbData);
            this.splitContainer1.Panel1.Controls.Add(this.lblStatus);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.btnTabCurrent);
            this.splitContainer1.Panel2.Controls.Add(this.btnTabHistory);
            this.splitContainer1.Panel2.Controls.Add(this.btnToggleRead);
            this.splitContainer1.Panel2.Controls.Add(this.chart1);
            this.splitContainer1.Panel2.Controls.Add(this.gbCurrent);
            this.splitContainer1.Panel2.Controls.Add(this.gbStatsTemp);
            this.splitContainer1.Panel2.Controls.Add(this.gbStatsHumi);
            this.splitContainer1.Panel2.Controls.Add(this.gbStatsPress);
            this.splitContainer1.Panel2.Controls.Add(this.gbHistory);
            this.splitContainer1.Size = new System.Drawing.Size(1222, 877);
            this.splitContainer1.SplitterDistance = 407;
            this.splitContainer1.TabIndex = 0;
            // 
            // chkSimMode
            // 
            this.chkSimMode.Location = new System.Drawing.Point(12, 6);
            this.chkSimMode.Name = "chkSimMode";
            this.chkSimMode.Size = new System.Drawing.Size(280, 22);
            this.chkSimMode.TabIndex = 0;
            this.chkSimMode.Text = "模拟模式（无需硬件即可演示）";
            // 
            // gbSerial
            // 
            this.gbSerial.Controls.Add(this.lblPortLabel);
            this.gbSerial.Controls.Add(this.lblBaudLabel);
            this.gbSerial.Controls.Add(this.cbComPort);
            this.gbSerial.Controls.Add(this.btnRefreshPorts);
            this.gbSerial.Controls.Add(this.cbBaudRate);
            this.gbSerial.Controls.Add(this.btnOpenCloseCom);
            this.gbSerial.Controls.Add(this.btnManualSend);
            this.gbSerial.Location = new System.Drawing.Point(12, 35);
            this.gbSerial.Name = "gbSerial";
            this.gbSerial.Size = new System.Drawing.Size(280, 120);
            this.gbSerial.TabIndex = 1;
            this.gbSerial.TabStop = false;
            this.gbSerial.Text = "串口设置";
            // 
            // lblPortLabel
            // 
            this.lblPortLabel.Location = new System.Drawing.Point(8, 22);
            this.lblPortLabel.Name = "lblPortLabel";
            this.lblPortLabel.Size = new System.Drawing.Size(52, 22);
            this.lblPortLabel.TabIndex = 0;
            this.lblPortLabel.Text = "串口号:";
            this.lblPortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblBaudLabel
            // 
            this.lblBaudLabel.Location = new System.Drawing.Point(8, 52);
            this.lblBaudLabel.Name = "lblBaudLabel";
            this.lblBaudLabel.Size = new System.Drawing.Size(52, 22);
            this.lblBaudLabel.TabIndex = 1;
            this.lblBaudLabel.Text = "波特率:";
            this.lblBaudLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbComPort
            // 
            this.cbComPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbComPort.Location = new System.Drawing.Point(66, 22);
            this.cbComPort.Name = "cbComPort";
            this.cbComPort.Size = new System.Drawing.Size(148, 23);
            this.cbComPort.TabIndex = 2;
            // 
            // btnRefreshPorts
            // 
            this.btnRefreshPorts.Location = new System.Drawing.Point(220, 22);
            this.btnRefreshPorts.Name = "btnRefreshPorts";
            this.btnRefreshPorts.Size = new System.Drawing.Size(52, 23);
            this.btnRefreshPorts.TabIndex = 3;
            this.btnRefreshPorts.Text = "刷新";
            // 
            // cbBaudRate
            // 
            this.cbBaudRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBaudRate.Items.AddRange(new object[] {
            "4800",
            "9600",
            "19200",
            "38400",
            "57600",
            "115200"});
            this.cbBaudRate.Location = new System.Drawing.Point(66, 52);
            this.cbBaudRate.Name = "cbBaudRate";
            this.cbBaudRate.Size = new System.Drawing.Size(210, 23);
            this.cbBaudRate.TabIndex = 4;
            // 
            // btnOpenCloseCom
            // 
            this.btnOpenCloseCom.Location = new System.Drawing.Point(30, 82);
            this.btnOpenCloseCom.Name = "btnOpenCloseCom";
            this.btnOpenCloseCom.Size = new System.Drawing.Size(110, 27);
            this.btnOpenCloseCom.TabIndex = 5;
            this.btnOpenCloseCom.Text = "打开串口";
            // 
            // btnManualSend
            // 
            this.btnManualSend.Location = new System.Drawing.Point(150, 82);
            this.btnManualSend.Name = "btnManualSend";
            this.btnManualSend.Size = new System.Drawing.Size(110, 27);
            this.btnManualSend.TabIndex = 6;
            this.btnManualSend.Text = "手动采集";
            // 
            // gbCollect
            // 
            this.gbCollect.Controls.Add(this.lblReadMode);
            this.gbCollect.Controls.Add(this.lblInterval);
            this.gbCollect.Controls.Add(this.lblMaxPoints);
            this.gbCollect.Controls.Add(this.cbReadMode);
            this.gbCollect.Controls.Add(this.nudInterval);
            this.gbCollect.Controls.Add(this.nudMaxPoints);
            this.gbCollect.Location = new System.Drawing.Point(12, 175);
            this.gbCollect.Name = "gbCollect";
            this.gbCollect.Size = new System.Drawing.Size(280, 105);
            this.gbCollect.TabIndex = 2;
            this.gbCollect.TabStop = false;
            this.gbCollect.Text = "采集设置";
            // 
            // lblReadMode
            // 
            this.lblReadMode.Location = new System.Drawing.Point(4, 22);
            this.lblReadMode.Name = "lblReadMode";
            this.lblReadMode.Size = new System.Drawing.Size(60, 22);
            this.lblReadMode.TabIndex = 0;
            this.lblReadMode.Text = "读取模式:";
            this.lblReadMode.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblInterval
            // 
            this.lblInterval.Location = new System.Drawing.Point(4, 50);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(60, 22);
            this.lblInterval.TabIndex = 1;
            this.lblInterval.Text = "间隔(ms):";
            this.lblInterval.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMaxPoints
            // 
            this.lblMaxPoints.Location = new System.Drawing.Point(4, 78);
            this.lblMaxPoints.Name = "lblMaxPoints";
            this.lblMaxPoints.Size = new System.Drawing.Size(60, 22);
            this.lblMaxPoints.TabIndex = 2;
            this.lblMaxPoints.Text = "最大点数:";
            this.lblMaxPoints.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbReadMode
            // 
            this.cbReadMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbReadMode.Items.AddRange(new object[] {
            "一次读温湿(浮点)",
            "一次读温湿(整型)",
            "单独读温度(浮点)",
            "单独读湿度(浮点)",
            "单独读气压(浮点)",
            "单独读温度(整型)",
            "单独读湿度(整型)",
            "单独读气压(整型)",
            "一次读温湿压(浮点)",
            "一次读温湿压(整型)"});
            this.cbReadMode.Location = new System.Drawing.Point(68, 20);
            this.cbReadMode.Name = "cbReadMode";
            this.cbReadMode.Size = new System.Drawing.Size(202, 23);
            this.cbReadMode.TabIndex = 3;
            // 
            // nudInterval
            // 
            this.nudInterval.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudInterval.Location = new System.Drawing.Point(68, 48);
            this.nudInterval.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.nudInterval.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudInterval.Name = "nudInterval";
            this.nudInterval.Size = new System.Drawing.Size(202, 25);
            this.nudInterval.TabIndex = 4;
            this.nudInterval.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudInterval.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // nudMaxPoints
            // 
            this.nudMaxPoints.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudMaxPoints.Location = new System.Drawing.Point(68, 76);
            this.nudMaxPoints.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudMaxPoints.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudMaxPoints.Name = "nudMaxPoints";
            this.nudMaxPoints.Size = new System.Drawing.Size(202, 25);
            this.nudMaxPoints.TabIndex = 5;
            this.nudMaxPoints.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudMaxPoints.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // gbCurrent
            // 
            this.gbCurrent.Controls.Add(this.lblTempLabel);
            this.gbCurrent.Controls.Add(this.lblHumiLabel);
            this.gbCurrent.Controls.Add(this.lblPressureLabel);
            this.gbCurrent.Controls.Add(this.lblUpdateLabel);
            this.gbCurrent.Controls.Add(this.lblTempValue);
            this.gbCurrent.Controls.Add(this.lblHumiValue);
            this.gbCurrent.Controls.Add(this.lblPressureValue);
            this.gbCurrent.Controls.Add(this.lblUpdateTime);
            this.gbCurrent.Location = new System.Drawing.Point(672, 86);
            this.gbCurrent.Name = "gbCurrent";
            this.gbCurrent.Size = new System.Drawing.Size(240, 200);
            this.gbCurrent.TabIndex = 3;
            this.gbCurrent.TabStop = false;
            this.gbCurrent.Text = "当前数据";
            // 
            // lblTempLabel
            // 
            this.lblTempLabel.Location = new System.Drawing.Point(4, 24);
            this.lblTempLabel.Name = "lblTempLabel";
            this.lblTempLabel.Size = new System.Drawing.Size(40, 32);
            this.lblTempLabel.TabIndex = 0;
            this.lblTempLabel.Text = "温度:";
            this.lblTempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHumiLabel
            // 
            this.lblHumiLabel.Location = new System.Drawing.Point(4, 64);
            this.lblHumiLabel.Name = "lblHumiLabel";
            this.lblHumiLabel.Size = new System.Drawing.Size(40, 32);
            this.lblHumiLabel.TabIndex = 1;
            this.lblHumiLabel.Text = "湿度:";
            this.lblHumiLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPressureLabel
            // 
            this.lblPressureLabel.Location = new System.Drawing.Point(4, 104);
            this.lblPressureLabel.Name = "lblPressureLabel";
            this.lblPressureLabel.Size = new System.Drawing.Size(40, 32);
            this.lblPressureLabel.TabIndex = 2;
            this.lblPressureLabel.Text = "气压:";
            this.lblPressureLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblUpdateLabel
            // 
            this.lblUpdateLabel.Location = new System.Drawing.Point(4, 150);
            this.lblUpdateLabel.Name = "lblUpdateLabel";
            this.lblUpdateLabel.Size = new System.Drawing.Size(40, 26);
            this.lblUpdateLabel.TabIndex = 3;
            this.lblUpdateLabel.Text = "更新:";
            this.lblUpdateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTempValue
            // 
            this.lblTempValue.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.lblTempValue.ForeColor = System.Drawing.Color.Red;
            this.lblTempValue.Location = new System.Drawing.Point(48, 22);
            this.lblTempValue.Name = "lblTempValue";
            this.lblTempValue.Size = new System.Drawing.Size(184, 34);
            this.lblTempValue.TabIndex = 4;
            this.lblTempValue.Text = "--.- ℃";
            this.lblTempValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHumiValue
            // 
            this.lblHumiValue.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.lblHumiValue.ForeColor = System.Drawing.Color.Blue;
            this.lblHumiValue.Location = new System.Drawing.Point(48, 62);
            this.lblHumiValue.Name = "lblHumiValue";
            this.lblHumiValue.Size = new System.Drawing.Size(184, 34);
            this.lblHumiValue.TabIndex = 5;
            this.lblHumiValue.Text = "--.- %";
            this.lblHumiValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPressureValue
            // 
            this.lblPressureValue.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.lblPressureValue.ForeColor = System.Drawing.Color.Green;
            this.lblPressureValue.Location = new System.Drawing.Point(48, 102);
            this.lblPressureValue.Name = "lblPressureValue";
            this.lblPressureValue.Size = new System.Drawing.Size(184, 34);
            this.lblPressureValue.TabIndex = 6;
            this.lblPressureValue.Text = "---.- kPa";
            this.lblPressureValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblUpdateTime
            // 
            this.lblUpdateTime.Location = new System.Drawing.Point(48, 150);
            this.lblUpdateTime.Name = "lblUpdateTime";
            this.lblUpdateTime.Size = new System.Drawing.Size(184, 26);
            this.lblUpdateTime.TabIndex = 7;
            this.lblUpdateTime.Text = "--";
            this.lblUpdateTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // gbStatsTemp
            //
            this.gbStatsTemp.Controls.Add(this.lblMinT);
            this.gbStatsTemp.Controls.Add(this.lblTempMin);
            this.gbStatsTemp.Controls.Add(this.lblMaxT);
            this.gbStatsTemp.Controls.Add(this.lblTempMax);
            this.gbStatsTemp.Controls.Add(this.lblAvgT);
            this.gbStatsTemp.Controls.Add(this.lblTempAvg);
            this.gbStatsTemp.Location = new System.Drawing.Point(672, 296);
            this.gbStatsTemp.Name = "gbStatsTemp";
            this.gbStatsTemp.Size = new System.Drawing.Size(240, 140);
            this.gbStatsTemp.TabIndex = 4;
            this.gbStatsTemp.TabStop = false;
            this.gbStatsTemp.Text = "温度统计";
            //
            // lblMinT
            //
            this.lblMinT.Location = new System.Drawing.Point(4, 20);
            this.lblMinT.Name = "lblMinT";
            this.lblMinT.Size = new System.Drawing.Size(46, 26);
            this.lblMinT.TabIndex = 0;
            this.lblMinT.Text = "最小:";
            this.lblMinT.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblTempMin
            //
            this.lblTempMin.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblTempMin.ForeColor = System.Drawing.Color.Red;
            this.lblTempMin.Location = new System.Drawing.Point(54, 20);
            this.lblTempMin.Name = "lblTempMin";
            this.lblTempMin.Size = new System.Drawing.Size(178, 26);
            this.lblTempMin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblTempMin.TabIndex = 1;
            this.lblTempMin.Text = "--.- ℃";
            //
            // lblMaxT
            //
            this.lblMaxT.Location = new System.Drawing.Point(4, 54);
            this.lblMaxT.Name = "lblMaxT";
            this.lblMaxT.Size = new System.Drawing.Size(46, 26);
            this.lblMaxT.TabIndex = 2;
            this.lblMaxT.Text = "最大:";
            this.lblMaxT.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblTempMax
            //
            this.lblTempMax.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblTempMax.ForeColor = System.Drawing.Color.Red;
            this.lblTempMax.Location = new System.Drawing.Point(54, 54);
            this.lblTempMax.Name = "lblTempMax";
            this.lblTempMax.Size = new System.Drawing.Size(178, 26);
            this.lblTempMax.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblTempMax.TabIndex = 3;
            this.lblTempMax.Text = "--.- ℃";
            //
            // lblAvgT
            //
            this.lblAvgT.Location = new System.Drawing.Point(4, 88);
            this.lblAvgT.Name = "lblAvgT";
            this.lblAvgT.Size = new System.Drawing.Size(46, 26);
            this.lblAvgT.TabIndex = 4;
            this.lblAvgT.Text = "平均:";
            this.lblAvgT.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblTempAvg
            //
            this.lblTempAvg.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblTempAvg.ForeColor = System.Drawing.Color.Red;
            this.lblTempAvg.Location = new System.Drawing.Point(54, 88);
            this.lblTempAvg.Name = "lblTempAvg";
            this.lblTempAvg.Size = new System.Drawing.Size(178, 26);
            this.lblTempAvg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblTempAvg.TabIndex = 5;
            this.lblTempAvg.Text = "--.- ℃";
            //
            // gbStatsHumi
            //
            this.gbStatsHumi.Controls.Add(this.lblMinH);
            this.gbStatsHumi.Controls.Add(this.lblHumiMin);
            this.gbStatsHumi.Controls.Add(this.lblMaxH);
            this.gbStatsHumi.Controls.Add(this.lblHumiMax);
            this.gbStatsHumi.Controls.Add(this.lblAvgH);
            this.gbStatsHumi.Controls.Add(this.lblHumiAvg);
            this.gbStatsHumi.Location = new System.Drawing.Point(672, 450);
            this.gbStatsHumi.Name = "gbStatsHumi";
            this.gbStatsHumi.Size = new System.Drawing.Size(240, 140);
            this.gbStatsHumi.TabIndex = 5;
            this.gbStatsHumi.TabStop = false;
            this.gbStatsHumi.Text = "湿度统计";
            //
            // lblMinH
            //
            this.lblMinH.Location = new System.Drawing.Point(4, 20);
            this.lblMinH.Name = "lblMinH";
            this.lblMinH.Size = new System.Drawing.Size(46, 26);
            this.lblMinH.TabIndex = 0;
            this.lblMinH.Text = "最小:";
            this.lblMinH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblHumiMin
            //
            this.lblHumiMin.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblHumiMin.ForeColor = System.Drawing.Color.Blue;
            this.lblHumiMin.Location = new System.Drawing.Point(54, 20);
            this.lblHumiMin.Name = "lblHumiMin";
            this.lblHumiMin.Size = new System.Drawing.Size(178, 26);
            this.lblHumiMin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHumiMin.TabIndex = 1;
            this.lblHumiMin.Text = "--.- %";
            //
            // lblMaxH
            //
            this.lblMaxH.Location = new System.Drawing.Point(4, 54);
            this.lblMaxH.Name = "lblMaxH";
            this.lblMaxH.Size = new System.Drawing.Size(46, 26);
            this.lblMaxH.TabIndex = 2;
            this.lblMaxH.Text = "最大:";
            this.lblMaxH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblHumiMax
            //
            this.lblHumiMax.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblHumiMax.ForeColor = System.Drawing.Color.Blue;
            this.lblHumiMax.Location = new System.Drawing.Point(54, 54);
            this.lblHumiMax.Name = "lblHumiMax";
            this.lblHumiMax.Size = new System.Drawing.Size(178, 26);
            this.lblHumiMax.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHumiMax.TabIndex = 3;
            this.lblHumiMax.Text = "--.- %";
            //
            // lblAvgH
            //
            this.lblAvgH.Location = new System.Drawing.Point(4, 88);
            this.lblAvgH.Name = "lblAvgH";
            this.lblAvgH.Size = new System.Drawing.Size(46, 26);
            this.lblAvgH.TabIndex = 4;
            this.lblAvgH.Text = "平均:";
            this.lblAvgH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblHumiAvg
            //
            this.lblHumiAvg.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblHumiAvg.ForeColor = System.Drawing.Color.Blue;
            this.lblHumiAvg.Location = new System.Drawing.Point(54, 88);
            this.lblHumiAvg.Name = "lblHumiAvg";
            this.lblHumiAvg.Size = new System.Drawing.Size(178, 26);
            this.lblHumiAvg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHumiAvg.TabIndex = 5;
            this.lblHumiAvg.Text = "--.- %";
            //
            // gbStatsPress
            //
            this.gbStatsPress.Controls.Add(this.lblMinP);
            this.gbStatsPress.Controls.Add(this.lblPressureMin);
            this.gbStatsPress.Controls.Add(this.lblMaxP);
            this.gbStatsPress.Controls.Add(this.lblPressureMax);
            this.gbStatsPress.Controls.Add(this.lblAvgP);
            this.gbStatsPress.Controls.Add(this.lblPressureAvg);
            this.gbStatsPress.Location = new System.Drawing.Point(672, 604);
            this.gbStatsPress.Name = "gbStatsPress";
            this.gbStatsPress.Size = new System.Drawing.Size(240, 140);
            this.gbStatsPress.TabIndex = 6;
            this.gbStatsPress.TabStop = false;
            this.gbStatsPress.Text = "气压统计";
            //
            // lblMinP
            //
            this.lblMinP.Location = new System.Drawing.Point(4, 20);
            this.lblMinP.Name = "lblMinP";
            this.lblMinP.Size = new System.Drawing.Size(46, 26);
            this.lblMinP.TabIndex = 0;
            this.lblMinP.Text = "最小:";
            this.lblMinP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblPressureMin
            //
            this.lblPressureMin.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblPressureMin.ForeColor = System.Drawing.Color.Green;
            this.lblPressureMin.Location = new System.Drawing.Point(54, 20);
            this.lblPressureMin.Name = "lblPressureMin";
            this.lblPressureMin.Size = new System.Drawing.Size(178, 26);
            this.lblPressureMin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblPressureMin.TabIndex = 1;
            this.lblPressureMin.Text = "--.- kPa";
            //
            // lblMaxP
            //
            this.lblMaxP.Location = new System.Drawing.Point(4, 54);
            this.lblMaxP.Name = "lblMaxP";
            this.lblMaxP.Size = new System.Drawing.Size(46, 26);
            this.lblMaxP.TabIndex = 2;
            this.lblMaxP.Text = "最大:";
            this.lblMaxP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblPressureMax
            //
            this.lblPressureMax.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblPressureMax.ForeColor = System.Drawing.Color.Green;
            this.lblPressureMax.Location = new System.Drawing.Point(54, 54);
            this.lblPressureMax.Name = "lblPressureMax";
            this.lblPressureMax.Size = new System.Drawing.Size(178, 26);
            this.lblPressureMax.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblPressureMax.TabIndex = 3;
            this.lblPressureMax.Text = "--.- kPa";
            //
            // lblAvgP
            //
            this.lblAvgP.Location = new System.Drawing.Point(4, 88);
            this.lblAvgP.Name = "lblAvgP";
            this.lblAvgP.Size = new System.Drawing.Size(46, 26);
            this.lblAvgP.TabIndex = 4;
            this.lblAvgP.Text = "平均:";
            this.lblAvgP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblPressureAvg
            //
            this.lblPressureAvg.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblPressureAvg.ForeColor = System.Drawing.Color.Green;
            this.lblPressureAvg.Location = new System.Drawing.Point(54, 88);
            this.lblPressureAvg.Name = "lblPressureAvg";
            this.lblPressureAvg.Size = new System.Drawing.Size(178, 26);
            this.lblPressureAvg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblPressureAvg.TabIndex = 5;
            this.lblPressureAvg.Text = "--.- kPa";
            //
            // gbHistory
            //
            this.gbHistory.Controls.Add(this.lblStart);
            this.gbHistory.Controls.Add(this.dtpStart);
            this.gbHistory.Controls.Add(this.lblEnd);
            this.gbHistory.Controls.Add(this.dtpEnd);
            this.gbHistory.Controls.Add(this.btnQueryHistory);
            this.gbHistory.Controls.Add(this.btnExportHistory);
            this.gbHistory.Controls.Add(this.chkAlarmOnly);
            this.gbHistory.Controls.Add(this.dgvHistory);
            this.gbHistory.Location = new System.Drawing.Point(4, 80);
            this.gbHistory.Name = "gbHistory";
            this.gbHistory.Size = new System.Drawing.Size(910, 790);
            this.gbHistory.TabIndex = 13;
            this.gbHistory.TabStop = false;
            this.gbHistory.Text = "历史数据查询";
            this.gbHistory.Visible = false;
            //
            // lblStart
            //
            this.lblStart.Location = new System.Drawing.Point(10, 25);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(70, 22);
            this.lblStart.TabIndex = 0;
            this.lblStart.Text = "开始日期:";
            this.lblStart.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // dtpStart
            //
            this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpStart.Location = new System.Drawing.Point(84, 22);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(150, 25);
            this.dtpStart.TabIndex = 1;
            //
            // lblEnd
            //
            this.lblEnd.Location = new System.Drawing.Point(244, 25);
            this.lblEnd.Name = "lblEnd";
            this.lblEnd.Size = new System.Drawing.Size(70, 22);
            this.lblEnd.TabIndex = 2;
            this.lblEnd.Text = "结束日期:";
            this.lblEnd.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // dtpEnd
            //
            this.dtpEnd.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpEnd.Location = new System.Drawing.Point(318, 22);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(150, 25);
            this.dtpEnd.TabIndex = 3;
            //
            // btnQueryHistory
            //
            this.btnQueryHistory.Location = new System.Drawing.Point(476, 20);
            this.btnQueryHistory.Name = "btnQueryHistory";
            this.btnQueryHistory.Size = new System.Drawing.Size(80, 28);
            this.btnQueryHistory.TabIndex = 4;
            this.btnQueryHistory.Text = "查询";
            this.btnQueryHistory.UseVisualStyleBackColor = true;
            //
            // btnExportHistory
            //
            this.btnExportHistory.Location = new System.Drawing.Point(562, 20);
            this.btnExportHistory.Name = "btnExportHistory";
            this.btnExportHistory.Size = new System.Drawing.Size(80, 28);
            this.btnExportHistory.TabIndex = 5;
            this.btnExportHistory.Text = "导出CSV";
            this.btnExportHistory.UseVisualStyleBackColor = true;
            //
            // chkAlarmOnly
            //
            this.chkAlarmOnly.Location = new System.Drawing.Point(648, 22);
            this.chkAlarmOnly.Name = "chkAlarmOnly";
            this.chkAlarmOnly.Size = new System.Drawing.Size(100, 22);
            this.chkAlarmOnly.TabIndex = 7;
            this.chkAlarmOnly.Text = "仅报警记录";
            this.chkAlarmOnly.UseVisualStyleBackColor = true;
            //
            // dgvHistory
            //
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHistory.Location = new System.Drawing.Point(10, 56);
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = true;
            this.dgvHistory.RowHeadersVisible = false;
            this.dgvHistory.Size = new System.Drawing.Size(890, 724);
            this.dgvHistory.TabIndex = 6;
            //
            // gbAlarm
            // 
            this.gbAlarm.Controls.Add(this.lblTempHigh);
            this.gbAlarm.Controls.Add(this.lblTempLow);
            this.gbAlarm.Controls.Add(this.lblHumiHigh);
            this.gbAlarm.Controls.Add(this.lblHumiLow);
            this.gbAlarm.Controls.Add(this.lblPressHigh);
            this.gbAlarm.Controls.Add(this.lblPressLow);
            this.gbAlarm.Controls.Add(this.chkEnableAlarm);
            this.gbAlarm.Controls.Add(this.nudTempHigh);
            this.gbAlarm.Controls.Add(this.nudTempLow);
            this.gbAlarm.Controls.Add(this.nudHumiHigh);
            this.gbAlarm.Controls.Add(this.nudHumiLow);
            this.gbAlarm.Controls.Add(this.nudPressureHigh);
            this.gbAlarm.Controls.Add(this.nudPressureLow);
            this.gbAlarm.Location = new System.Drawing.Point(12, 310);
            this.gbAlarm.Name = "gbAlarm";
            this.gbAlarm.Size = new System.Drawing.Size(280, 320);
            this.gbAlarm.TabIndex = 5;
            this.gbAlarm.TabStop = false;
            this.gbAlarm.Text = "报警设置";
            // 
            // lblTempHigh
            // 
            this.lblTempHigh.Location = new System.Drawing.Point(4, 58);
            this.lblTempHigh.Name = "lblTempHigh";
            this.lblTempHigh.Size = new System.Drawing.Size(60, 22);
            this.lblTempHigh.TabIndex = 0;
            this.lblTempHigh.Text = "温度上限:";
            this.lblTempHigh.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTempLow
            // 
            this.lblTempLow.Location = new System.Drawing.Point(4, 98);
            this.lblTempLow.Name = "lblTempLow";
            this.lblTempLow.Size = new System.Drawing.Size(60, 22);
            this.lblTempLow.TabIndex = 1;
            this.lblTempLow.Text = "温度下限:";
            this.lblTempLow.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHumiHigh
            // 
            this.lblHumiHigh.Location = new System.Drawing.Point(4, 138);
            this.lblHumiHigh.Name = "lblHumiHigh";
            this.lblHumiHigh.Size = new System.Drawing.Size(60, 22);
            this.lblHumiHigh.TabIndex = 2;
            this.lblHumiHigh.Text = "湿度上限:";
            this.lblHumiHigh.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHumiLow
            // 
            this.lblHumiLow.Location = new System.Drawing.Point(4, 178);
            this.lblHumiLow.Name = "lblHumiLow";
            this.lblHumiLow.Size = new System.Drawing.Size(60, 22);
            this.lblHumiLow.TabIndex = 3;
            this.lblHumiLow.Text = "湿度下限:";
            this.lblHumiLow.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPressHigh
            // 
            this.lblPressHigh.Location = new System.Drawing.Point(4, 218);
            this.lblPressHigh.Name = "lblPressHigh";
            this.lblPressHigh.Size = new System.Drawing.Size(60, 22);
            this.lblPressHigh.TabIndex = 4;
            this.lblPressHigh.Text = "气压上限:";
            this.lblPressHigh.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPressLow
            // 
            this.lblPressLow.Location = new System.Drawing.Point(4, 258);
            this.lblPressLow.Name = "lblPressLow";
            this.lblPressLow.Size = new System.Drawing.Size(60, 22);
            this.lblPressLow.TabIndex = 5;
            this.lblPressLow.Text = "气压下限:";
            this.lblPressLow.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkEnableAlarm
            // 
            this.chkEnableAlarm.Location = new System.Drawing.Point(10, 20);
            this.chkEnableAlarm.Name = "chkEnableAlarm";
            this.chkEnableAlarm.Size = new System.Drawing.Size(120, 22);
            this.chkEnableAlarm.TabIndex = 6;
            this.chkEnableAlarm.Text = "启用报警";
            // 
            // nudTempHigh
            // 
            this.nudTempHigh.DecimalPlaces = 1;
            this.nudTempHigh.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.nudTempHigh.Location = new System.Drawing.Point(68, 58);
            this.nudTempHigh.Maximum = new decimal(new int[] {
            150,
            0,
            0,
            0});
            this.nudTempHigh.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            -2147483648});
            this.nudTempHigh.Name = "nudTempHigh";
            this.nudTempHigh.Size = new System.Drawing.Size(202, 25);
            this.nudTempHigh.TabIndex = 7;
            this.nudTempHigh.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudTempHigh.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // nudTempLow
            // 
            this.nudTempLow.DecimalPlaces = 1;
            this.nudTempLow.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.nudTempLow.Location = new System.Drawing.Point(68, 98);
            this.nudTempLow.Maximum = new decimal(new int[] {
            150,
            0,
            0,
            0});
            this.nudTempLow.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            -2147483648});
            this.nudTempLow.Name = "nudTempLow";
            this.nudTempLow.Size = new System.Drawing.Size(202, 25);
            this.nudTempLow.TabIndex = 8;
            this.nudTempLow.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // nudHumiHigh
            // 
            this.nudHumiHigh.Location = new System.Drawing.Point(68, 138);
            this.nudHumiHigh.Name = "nudHumiHigh";
            this.nudHumiHigh.Size = new System.Drawing.Size(202, 25);
            this.nudHumiHigh.TabIndex = 9;
            this.nudHumiHigh.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudHumiHigh.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});
            // 
            // nudHumiLow
            // 
            this.nudHumiLow.Location = new System.Drawing.Point(68, 178);
            this.nudHumiLow.Name = "nudHumiLow";
            this.nudHumiLow.Size = new System.Drawing.Size(202, 25);
            this.nudHumiLow.TabIndex = 10;
            this.nudHumiLow.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudHumiLow.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // nudPressureHigh
            // 
            this.nudPressureHigh.Location = new System.Drawing.Point(68, 218);
            this.nudPressureHigh.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudPressureHigh.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudPressureHigh.Name = "nudPressureHigh";
            this.nudPressureHigh.Size = new System.Drawing.Size(202, 25);
            this.nudPressureHigh.TabIndex = 11;
            this.nudPressureHigh.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudPressureHigh.Value = new decimal(new int[] {
            110,
            0,
            0,
            0});
            // 
            // nudPressureLow
            // 
            this.nudPressureLow.Location = new System.Drawing.Point(68, 258);
            this.nudPressureLow.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudPressureLow.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudPressureLow.Name = "nudPressureLow";
            this.nudPressureLow.Size = new System.Drawing.Size(202, 25);
            this.nudPressureLow.TabIndex = 12;
            this.nudPressureLow.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudPressureLow.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
            // 
            // gbData
            // 
            this.gbData.Controls.Add(this.chkDataLog);
            this.gbData.Controls.Add(this.btnExportCSV);
            this.gbData.Controls.Add(this.btnClearChart);
            this.gbData.Controls.Add(this.lblRetainDays);
            this.gbData.Controls.Add(this.nudRetainDays);
            this.gbData.Controls.Add(this.btnCleanDB);
            this.gbData.Location = new System.Drawing.Point(12, 660);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(280, 145);
            this.gbData.TabIndex = 6;
            this.gbData.TabStop = false;
            this.gbData.Text = "数据管理";
            // 
            // chkDataLog
            // 
            this.chkDataLog.Checked = true;
            this.chkDataLog.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDataLog.Location = new System.Drawing.Point(10, 18);
            this.chkDataLog.Name = "chkDataLog";
            this.chkDataLog.Size = new System.Drawing.Size(260, 22);
            this.chkDataLog.TabIndex = 0;
            this.chkDataLog.Text = "启用数据记录";
            // 
            // btnExportCSV
            // 
            this.btnExportCSV.Location = new System.Drawing.Point(30, 36);
            this.btnExportCSV.Name = "btnExportCSV";
            this.btnExportCSV.Size = new System.Drawing.Size(220, 27);
            this.btnExportCSV.TabIndex = 1;
            this.btnExportCSV.Text = "导出CSV文件";
            //
            // btnClearChart
            //
            this.btnClearChart.Location = new System.Drawing.Point(30, 66);
            this.btnClearChart.Name = "btnClearChart";
            this.btnClearChart.Size = new System.Drawing.Size(110, 27);
            this.btnClearChart.TabIndex = 2;
            this.btnClearChart.Text = "全部清除";
            //
            // lblRetainDays
            //
            this.lblRetainDays.Location = new System.Drawing.Point(10, 100);
            this.lblRetainDays.Name = "lblRetainDays";
            this.lblRetainDays.Size = new System.Drawing.Size(72, 22);
            this.lblRetainDays.TabIndex = 3;
            this.lblRetainDays.Text = "保留天数:";
            this.lblRetainDays.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // nudRetainDays
            //
            this.nudRetainDays.Location = new System.Drawing.Point(86, 99);
            this.nudRetainDays.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            this.nudRetainDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudRetainDays.Name = "nudRetainDays";
            this.nudRetainDays.Size = new System.Drawing.Size(56, 25);
            this.nudRetainDays.TabIndex = 4;
            this.nudRetainDays.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudRetainDays.Value = new decimal(new int[] { 90, 0, 0, 0 });
            //
            // btnCleanDB
            //
            this.btnCleanDB.Location = new System.Drawing.Point(150, 96);
            this.btnCleanDB.Name = "btnCleanDB";
            this.btnCleanDB.Size = new System.Drawing.Size(110, 27);
            this.btnCleanDB.TabIndex = 5;
            this.btnCleanDB.Text = "清理旧数据";
            this.btnCleanDB.UseVisualStyleBackColor = true;
            //
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(12, 778);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(280, 40);
            this.lblStatus.TabIndex = 7;
            this.lblStatus.Text = "就绪";
            // 
            //
            // btnTabCurrent
            //
            this.btnTabCurrent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabCurrent.Location = new System.Drawing.Point(4, 6);
            this.btnTabCurrent.Name = "btnTabCurrent";
            this.btnTabCurrent.Size = new System.Drawing.Size(100, 28);
            this.btnTabCurrent.TabIndex = 10;
            this.btnTabCurrent.Text = "当前数据";
            this.btnTabCurrent.UseVisualStyleBackColor = true;
            //
            // btnTabHistory
            //
            this.btnTabHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabHistory.Location = new System.Drawing.Point(108, 6);
            this.btnTabHistory.Name = "btnTabHistory";
            this.btnTabHistory.Size = new System.Drawing.Size(100, 28);
            this.btnTabHistory.TabIndex = 11;
            this.btnTabHistory.Text = "历史查询";
            this.btnTabHistory.UseVisualStyleBackColor = true;
            //
            // btnToggleRead
            //
            this.btnToggleRead.Enabled = false;
            this.btnToggleRead.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleRead.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.btnToggleRead.Location = new System.Drawing.Point(4, 40);
            this.btnToggleRead.Name = "btnToggleRead";
            this.btnToggleRead.Size = new System.Drawing.Size(140, 30);
            this.btnToggleRead.TabIndex = 12;
            this.btnToggleRead.Text = "▶ 开始采集";
            this.btnToggleRead.UseVisualStyleBackColor = true;
            //
            // chart1
            //
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisX.Title = "采样点序号";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("微软雅黑", 9F);
            chartArea1.AxisY.Interval = 10D;
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisY.Maximum = 100D;
            chartArea1.AxisY.Minimum = -10D;
            chartArea1.AxisY.LabelStyle.ForeColor = System.Drawing.Color.Black;
            chartArea1.AxisY.LabelStyle.Format = "0";
            chartArea1.AxisY.Title = "温度/湿度";
            chartArea1.AxisY.TitleFont = new System.Drawing.Font("微软雅黑", 9F);
            chartArea1.AxisY2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;
            chartArea1.AxisY2.Interval = 5D;
            chartArea1.AxisY2.LabelStyle.ForeColor = System.Drawing.Color.Black;
            chartArea1.AxisY2.LabelStyle.Format = "0.#";
            chartArea1.AxisY2.MajorGrid.Enabled = false;
            chartArea1.AxisY2.MajorTickMark.Enabled = true;
            chartArea1.AxisY2.Maximum = 110D;
            chartArea1.AxisY2.Minimum = 90D;
            chartArea1.AxisY2.Title = "气压 (kPa)";
            chartArea1.AxisY2.TitleFont = new System.Drawing.Font("微软雅黑", 9F);
            chartArea1.AxisY2.TitleForeColor = System.Drawing.Color.Black;
            chartArea1.CursorX.IsUserEnabled = true;
            chartArea1.CursorX.IsUserSelectionEnabled = true;
            chartArea1.Name = "MainArea";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Dock = System.Windows.Forms.DockStyle.None;
            legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top;
            legend1.Font = new System.Drawing.Font("微软雅黑", 9F);
            legend1.IsTextAutoFit = false;
            legend1.Name = "Legend";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(0, 80);
            this.chart1.Name = "chart1";
            series1.BorderWidth = 2;
            series1.ChartArea = "MainArea";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series1.Color = System.Drawing.Color.Red;
            series1.Legend = "Legend";
            series1.LegendText = "温度 (℃)";
            series1.MarkerColor = System.Drawing.Color.Red;
            series1.MarkerSize = 6;
            series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
            series1.Name = "温度";
            series1.ToolTip = "温度: #VAL{F1} ℃";
            series2.BorderWidth = 2;
            series2.ChartArea = "MainArea";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series2.Color = System.Drawing.Color.Blue;
            series2.Legend = "Legend";
            series2.LegendText = "湿度 (%)";
            series2.MarkerColor = System.Drawing.Color.Blue;
            series2.MarkerSize = 6;
            series2.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Diamond;
            series2.Name = "湿度";
            series2.ToolTip = "湿度: #VAL{F1} %";
            series3.BorderWidth = 2;
            series3.ChartArea = "MainArea";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series3.Color = System.Drawing.Color.Green;
            series3.Legend = "Legend";
            series3.LegendText = "气压 (kPa)";
            series3.MarkerColor = System.Drawing.Color.Green;
            series3.MarkerSize = 6;
            series3.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Triangle;
            series3.Name = "气压";
            series3.ToolTip = "气压: #VAL{F1} kPa";
            series3.YAxisType = System.Windows.Forms.DataVisualization.Charting.AxisType.Secondary;
            this.chart1.Series.Add(series1);
            this.chart1.Series.Add(series2);
            this.chart1.Series.Add(series3);
            this.chart1.Size = new System.Drawing.Size(665, 797);
            this.chart1.TabIndex = 0;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslStatus,
            this.tsslSend,
            this.tsslRecv,
            this.tsslError,
            this.tsslTime});
            this.statusStrip1.Location = new System.Drawing.Point(0, 877);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1222, 26);
            this.statusStrip1.TabIndex = 1;
            // 
            // tsslStatus
            // 
            this.tsslStatus.Name = "tsslStatus";
            this.tsslStatus.Size = new System.Drawing.Size(84, 20);
            this.tsslStatus.Text = "串口未打开";
            // 
            // tsslSend
            // 
            this.tsslSend.Name = "tsslSend";
            this.tsslSend.Size = new System.Drawing.Size(37, 20);
            this.tsslSend.Text = "发:0";
            // 
            // tsslRecv
            // 
            this.tsslRecv.Name = "tsslRecv";
            this.tsslRecv.Size = new System.Drawing.Size(37, 20);
            this.tsslRecv.Text = "收:0";
            // 
            // tsslError
            // 
            this.tsslError.Name = "tsslError";
            this.tsslError.Size = new System.Drawing.Size(37, 20);
            this.tsslError.Text = "错:0";
            // 
            // tsslTime
            // 
            this.tsslTime.Name = "tsslTime";
            this.tsslTime.Size = new System.Drawing.Size(49, 20);
            this.tsslTime.Text = "00:00";
            // 
            // timer1
            //
            this.timer1.Interval = 1000;
            //
            // cmsTray
            //
            this.cmsTray.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            new System.Windows.Forms.ToolStripMenuItem("显示窗口", null, new System.EventHandler(this.trayShow_Click)),
            new System.Windows.Forms.ToolStripMenuItem("退出程序", null, new System.EventHandler(this.trayExit_Click))});
            this.cmsTray.Name = "cmsTray";
            this.cmsTray.Size = new System.Drawing.Size(120, 48);
            //
            // notifyIcon1
            //
            this.notifyIcon1.ContextMenuStrip = this.cmsTray;
            this.notifyIcon1.Icon = System.Drawing.SystemIcons.Application;
            this.notifyIcon1.Text = "温湿度传感器监控";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            //
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1222, 903);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.MinimumSize = new System.Drawing.Size(960, 700);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "温湿度传感器监控程序";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.gbSerial.ResumeLayout(false);
            this.gbCollect.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPoints)).EndInit();
            this.gbCurrent.ResumeLayout(false);
            this.gbStatsTemp.ResumeLayout(false);
            this.gbStatsHumi.ResumeLayout(false);
            this.gbStatsPress.ResumeLayout(false);
            this.gbHistory.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.gbAlarm.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudTempHigh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiHigh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPressureHigh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPressureLow)).EndInit();
            this.gbData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudRetainDays)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

            this.Load += MainForm_Load;
            this.Shown += MainForm_Shown;
            this.FormClosing += MainForm_FormClosing;
            this.chkSimMode.CheckedChanged += chkSimMode_CheckedChanged;
            this.btnRefreshPorts.Click += btnRefreshPorts_Click;
            this.btnOpenCloseCom.Click += btnOpenCloseCom_Click;
            this.btnManualSend.Click += btnManualSend_Click;
            this.btnExportCSV.Click += btnExportCSV_Click;
            this.btnClearChart.Click += btnClearChart_Click;
            this.chkEnableAlarm.CheckedChanged += chkEnableAlarm_CheckedChanged;
            this.chkDataLog.CheckedChanged += chkDataLog_CheckedChanged;
            this.cbReadMode.SelectedIndexChanged += cbReadMode_SelectedIndexChanged;
            this.nudInterval.ValueChanged += nudInterval_ValueChanged;
            this.nudMaxPoints.ValueChanged += nudMaxPoints_ValueChanged;
            this.nudTempHigh.ValueChanged += SyncAlarmThresholds;
            this.nudTempLow.ValueChanged += SyncAlarmThresholds;
            this.nudHumiHigh.ValueChanged += SyncAlarmThresholds;
            this.nudHumiLow.ValueChanged += SyncAlarmThresholds;
            this.nudPressureHigh.ValueChanged += SyncAlarmThresholds;
            this.nudPressureLow.ValueChanged += SyncAlarmThresholds;
            this.timer1.Tick += timer1_Tick;
            this.serialPort1.DataReceived += serialPort1_DataReceived;
            this.serialPort1.ErrorReceived += serialPort1_ErrorReceived;
            this.btnTabCurrent.Click += btnTabCurrent_Click;
            this.btnTabHistory.Click += btnTabHistory_Click;
            this.btnToggleRead.Click += btnToggleRead_Click;
            this.btnQueryHistory.Click += btnQueryHistory_Click;
            this.btnExportHistory.Click += btnExportHistory_Click;
            this.btnCleanDB.Click += btnCleanDB_Click;

        }

        private Label lblPortLabel;
        private Label lblBaudLabel;
        private Label lblReadMode;
        private Label lblInterval;
        private Label lblMaxPoints;
        private Label lblTempLabel;
        private Label lblHumiLabel;
        private Label lblPressureLabel;
        private Label lblUpdateLabel;
        private Label lblTempHigh;
        private Label lblTempLow;
        private Label lblHumiHigh;
        private Label lblHumiLow;
        private Label lblPressHigh;
        private Label lblPressLow;
        private Label lblStart;
        private Label lblEnd;
        private Label lblMinT, lblMaxT, lblAvgT;
        private Label lblMinH, lblMaxH, lblAvgH;
        private Label lblMinP, lblMaxP, lblAvgP;
    }
}
