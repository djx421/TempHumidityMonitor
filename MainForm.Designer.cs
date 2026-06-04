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
        private GroupBox gbSerial, gbCollect, gbCurrent, gbStats, gbAlarm, gbData;
        private ComboBox cbComPort, cbBaudRate, cbReadMode;
        private Button btnRefreshPorts, btnOpenCloseCom, btnManualSend;
        private Button btnClearStats, btnExportCSV, btnClearChart;
        private CheckBox chkEnableAlarm, chkDataLog;
        private NumericUpDown nudInterval, nudMaxPoints;
        private NumericUpDown nudTempHigh, nudTempLow, nudHumiHigh, nudHumiLow;
        private NumericUpDown nudPressureHigh, nudPressureLow;
        private Label lblTempValue, lblHumiValue, lblPressureValue, lblUpdateTime;
        private Label lblTempMin, lblTempMax, lblTempAvg;
        private Label lblHumiMin, lblHumiMax, lblHumiAvg;
        private Label lblPressureMin, lblPressureMax, lblPressureAvg;
        private Label lblStatus;

        // ==================== 右侧图表 ====================
        private Chart chart1;

        // ==================== 非可视组件 ====================
        private Timer timer1;
        private System.IO.Ports.SerialPort serialPort1;

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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series5 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series6 = new System.Windows.Forms.DataVisualization.Charting.Series();
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
            this.gbStats = new System.Windows.Forms.GroupBox();
            this.lblStatsTemp = new System.Windows.Forms.Label();
            this.lblStatsHumi = new System.Windows.Forms.Label();
            this.lblStatsPress = new System.Windows.Forms.Label();
            this.lblTempMin = new System.Windows.Forms.Label();
            this.lblTempMax = new System.Windows.Forms.Label();
            this.lblTempAvg = new System.Windows.Forms.Label();
            this.lblHumiMin = new System.Windows.Forms.Label();
            this.lblHumiMax = new System.Windows.Forms.Label();
            this.lblHumiAvg = new System.Windows.Forms.Label();
            this.lblPressureMin = new System.Windows.Forms.Label();
            this.lblPressureMax = new System.Windows.Forms.Label();
            this.lblPressureAvg = new System.Windows.Forms.Label();
            this.btnClearStats = new System.Windows.Forms.Button();
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
            this.lblStatus = new System.Windows.Forms.Label();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslSend = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslRecv = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslError = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsslTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.gbSerial.SuspendLayout();
            this.gbCollect.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPoints)).BeginInit();
            this.gbCurrent.SuspendLayout();
            this.gbStats.SuspendLayout();
            this.gbAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPressureHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPressureLow)).BeginInit();
            this.gbData.SuspendLayout();
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
            this.splitContainer1.Panel1.Controls.Add(this.gbCurrent);
            this.splitContainer1.Panel1.Controls.Add(this.gbStats);
            this.splitContainer1.Panel1.Controls.Add(this.gbAlarm);
            this.splitContainer1.Panel1.Controls.Add(this.gbData);
            this.splitContainer1.Panel1.Controls.Add(this.lblStatus);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.chart1);
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
            this.gbSerial.Location = new System.Drawing.Point(12, 34);
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
            this.gbCollect.Location = new System.Drawing.Point(12, 160);
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
            this.gbCurrent.Location = new System.Drawing.Point(12, 274);
            this.gbCurrent.Name = "gbCurrent";
            this.gbCurrent.Size = new System.Drawing.Size(280, 120);
            this.gbCurrent.TabIndex = 3;
            this.gbCurrent.TabStop = false;
            this.gbCurrent.Text = "当前数据";
            // 
            // lblTempLabel
            // 
            this.lblTempLabel.Location = new System.Drawing.Point(4, 16);
            this.lblTempLabel.Name = "lblTempLabel";
            this.lblTempLabel.Size = new System.Drawing.Size(46, 22);
            this.lblTempLabel.TabIndex = 0;
            this.lblTempLabel.Text = "温度:";
            this.lblTempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHumiLabel
            // 
            this.lblHumiLabel.Location = new System.Drawing.Point(4, 40);
            this.lblHumiLabel.Name = "lblHumiLabel";
            this.lblHumiLabel.Size = new System.Drawing.Size(46, 22);
            this.lblHumiLabel.TabIndex = 1;
            this.lblHumiLabel.Text = "湿度:";
            this.lblHumiLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPressureLabel
            // 
            this.lblPressureLabel.Location = new System.Drawing.Point(4, 64);
            this.lblPressureLabel.Name = "lblPressureLabel";
            this.lblPressureLabel.Size = new System.Drawing.Size(46, 22);
            this.lblPressureLabel.TabIndex = 2;
            this.lblPressureLabel.Text = "气压:";
            this.lblPressureLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblUpdateLabel
            // 
            this.lblUpdateLabel.Location = new System.Drawing.Point(4, 90);
            this.lblUpdateLabel.Name = "lblUpdateLabel";
            this.lblUpdateLabel.Size = new System.Drawing.Size(46, 22);
            this.lblUpdateLabel.TabIndex = 3;
            this.lblUpdateLabel.Text = "更新:";
            this.lblUpdateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTempValue
            // 
            this.lblTempValue.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.lblTempValue.ForeColor = System.Drawing.Color.Red;
            this.lblTempValue.Location = new System.Drawing.Point(54, 16);
            this.lblTempValue.Name = "lblTempValue";
            this.lblTempValue.Size = new System.Drawing.Size(218, 22);
            this.lblTempValue.TabIndex = 4;
            this.lblTempValue.Text = "--.- ℃";
            this.lblTempValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHumiValue
            // 
            this.lblHumiValue.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.lblHumiValue.ForeColor = System.Drawing.Color.Blue;
            this.lblHumiValue.Location = new System.Drawing.Point(54, 40);
            this.lblHumiValue.Name = "lblHumiValue";
            this.lblHumiValue.Size = new System.Drawing.Size(218, 22);
            this.lblHumiValue.TabIndex = 5;
            this.lblHumiValue.Text = "--.- %";
            this.lblHumiValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPressureValue
            // 
            this.lblPressureValue.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.lblPressureValue.ForeColor = System.Drawing.Color.Green;
            this.lblPressureValue.Location = new System.Drawing.Point(54, 64);
            this.lblPressureValue.Name = "lblPressureValue";
            this.lblPressureValue.Size = new System.Drawing.Size(218, 22);
            this.lblPressureValue.TabIndex = 6;
            this.lblPressureValue.Text = "---.- kPa";
            this.lblPressureValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblUpdateTime
            // 
            this.lblUpdateTime.Location = new System.Drawing.Point(54, 90);
            this.lblUpdateTime.Name = "lblUpdateTime";
            this.lblUpdateTime.Size = new System.Drawing.Size(218, 22);
            this.lblUpdateTime.TabIndex = 7;
            this.lblUpdateTime.Text = "--";
            this.lblUpdateTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // gbStats
            // 
            this.gbStats.Controls.Add(this.lblStatsTemp);
            this.gbStats.Controls.Add(this.lblStatsHumi);
            this.gbStats.Controls.Add(this.lblStatsPress);
            this.gbStats.Controls.Add(this.lblTempMin);
            this.gbStats.Controls.Add(this.lblTempMax);
            this.gbStats.Controls.Add(this.lblTempAvg);
            this.gbStats.Controls.Add(this.lblHumiMin);
            this.gbStats.Controls.Add(this.lblHumiMax);
            this.gbStats.Controls.Add(this.lblHumiAvg);
            this.gbStats.Controls.Add(this.lblPressureMin);
            this.gbStats.Controls.Add(this.lblPressureMax);
            this.gbStats.Controls.Add(this.lblPressureAvg);
            this.gbStats.Controls.Add(this.btnClearStats);
            this.gbStats.Location = new System.Drawing.Point(12, 398);
            this.gbStats.Name = "gbStats";
            this.gbStats.Size = new System.Drawing.Size(280, 120);
            this.gbStats.TabIndex = 4;
            this.gbStats.TabStop = false;
            this.gbStats.Text = "统计信息";
            // 
            // lblStatsTemp
            // 
            this.lblStatsTemp.Location = new System.Drawing.Point(4, 14);
            this.lblStatsTemp.Name = "lblStatsTemp";
            this.lblStatsTemp.Size = new System.Drawing.Size(42, 18);
            this.lblStatsTemp.TabIndex = 0;
            this.lblStatsTemp.Text = "温度:";
            this.lblStatsTemp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblStatsHumi
            // 
            this.lblStatsHumi.Location = new System.Drawing.Point(4, 34);
            this.lblStatsHumi.Name = "lblStatsHumi";
            this.lblStatsHumi.Size = new System.Drawing.Size(42, 18);
            this.lblStatsHumi.TabIndex = 1;
            this.lblStatsHumi.Text = "湿度:";
            this.lblStatsHumi.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblStatsPress
            // 
            this.lblStatsPress.Location = new System.Drawing.Point(4, 54);
            this.lblStatsPress.Name = "lblStatsPress";
            this.lblStatsPress.Size = new System.Drawing.Size(42, 18);
            this.lblStatsPress.TabIndex = 2;
            this.lblStatsPress.Text = "气压:";
            this.lblStatsPress.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTempMin
            // 
            this.lblTempMin.ForeColor = System.Drawing.Color.DarkRed;
            this.lblTempMin.Location = new System.Drawing.Point(48, 14);
            this.lblTempMin.Name = "lblTempMin";
            this.lblTempMin.Size = new System.Drawing.Size(76, 18);
            this.lblTempMin.TabIndex = 3;
            this.lblTempMin.Text = "最小:--";
            this.lblTempMin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTempMax
            // 
            this.lblTempMax.Location = new System.Drawing.Point(126, 14);
            this.lblTempMax.Name = "lblTempMax";
            this.lblTempMax.Size = new System.Drawing.Size(76, 18);
            this.lblTempMax.TabIndex = 4;
            this.lblTempMax.Text = "最大:--";
            this.lblTempMax.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTempAvg
            // 
            this.lblTempAvg.Location = new System.Drawing.Point(204, 14);
            this.lblTempAvg.Name = "lblTempAvg";
            this.lblTempAvg.Size = new System.Drawing.Size(72, 18);
            this.lblTempAvg.TabIndex = 5;
            this.lblTempAvg.Text = "平均:--";
            this.lblTempAvg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHumiMin
            // 
            this.lblHumiMin.Location = new System.Drawing.Point(48, 34);
            this.lblHumiMin.Name = "lblHumiMin";
            this.lblHumiMin.Size = new System.Drawing.Size(76, 18);
            this.lblHumiMin.TabIndex = 6;
            this.lblHumiMin.Text = "最小:--";
            this.lblHumiMin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHumiMax
            // 
            this.lblHumiMax.Location = new System.Drawing.Point(126, 34);
            this.lblHumiMax.Name = "lblHumiMax";
            this.lblHumiMax.Size = new System.Drawing.Size(76, 18);
            this.lblHumiMax.TabIndex = 7;
            this.lblHumiMax.Text = "最大:--";
            this.lblHumiMax.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHumiAvg
            // 
            this.lblHumiAvg.Location = new System.Drawing.Point(204, 34);
            this.lblHumiAvg.Name = "lblHumiAvg";
            this.lblHumiAvg.Size = new System.Drawing.Size(72, 18);
            this.lblHumiAvg.TabIndex = 8;
            this.lblHumiAvg.Text = "平均:--";
            this.lblHumiAvg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPressureMin
            // 
            this.lblPressureMin.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblPressureMin.Location = new System.Drawing.Point(48, 54);
            this.lblPressureMin.Name = "lblPressureMin";
            this.lblPressureMin.Size = new System.Drawing.Size(76, 18);
            this.lblPressureMin.TabIndex = 9;
            this.lblPressureMin.Text = "最小:--";
            this.lblPressureMin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPressureMax
            // 
            this.lblPressureMax.Location = new System.Drawing.Point(126, 54);
            this.lblPressureMax.Name = "lblPressureMax";
            this.lblPressureMax.Size = new System.Drawing.Size(76, 18);
            this.lblPressureMax.TabIndex = 10;
            this.lblPressureMax.Text = "最大:--";
            this.lblPressureMax.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPressureAvg
            // 
            this.lblPressureAvg.Location = new System.Drawing.Point(204, 54);
            this.lblPressureAvg.Name = "lblPressureAvg";
            this.lblPressureAvg.Size = new System.Drawing.Size(72, 18);
            this.lblPressureAvg.TabIndex = 11;
            this.lblPressureAvg.Text = "平均:--";
            this.lblPressureAvg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnClearStats
            // 
            this.btnClearStats.Location = new System.Drawing.Point(80, 80);
            this.btnClearStats.Name = "btnClearStats";
            this.btnClearStats.Size = new System.Drawing.Size(120, 27);
            this.btnClearStats.TabIndex = 12;
            this.btnClearStats.Text = "重置统计";
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
            this.gbAlarm.Location = new System.Drawing.Point(12, 525);
            this.gbAlarm.Name = "gbAlarm";
            this.gbAlarm.Size = new System.Drawing.Size(280, 196);
            this.gbAlarm.TabIndex = 5;
            this.gbAlarm.TabStop = false;
            this.gbAlarm.Text = "报警设置";
            // 
            // lblTempHigh
            // 
            this.lblTempHigh.Location = new System.Drawing.Point(4, 44);
            this.lblTempHigh.Name = "lblTempHigh";
            this.lblTempHigh.Size = new System.Drawing.Size(60, 22);
            this.lblTempHigh.TabIndex = 0;
            this.lblTempHigh.Text = "温度上限:";
            this.lblTempHigh.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTempLow
            // 
            this.lblTempLow.Location = new System.Drawing.Point(4, 68);
            this.lblTempLow.Name = "lblTempLow";
            this.lblTempLow.Size = new System.Drawing.Size(60, 22);
            this.lblTempLow.TabIndex = 1;
            this.lblTempLow.Text = "温度下限:";
            this.lblTempLow.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHumiHigh
            // 
            this.lblHumiHigh.Location = new System.Drawing.Point(4, 92);
            this.lblHumiHigh.Name = "lblHumiHigh";
            this.lblHumiHigh.Size = new System.Drawing.Size(60, 22);
            this.lblHumiHigh.TabIndex = 2;
            this.lblHumiHigh.Text = "湿度上限:";
            this.lblHumiHigh.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHumiLow
            // 
            this.lblHumiLow.Location = new System.Drawing.Point(4, 116);
            this.lblHumiLow.Name = "lblHumiLow";
            this.lblHumiLow.Size = new System.Drawing.Size(60, 22);
            this.lblHumiLow.TabIndex = 3;
            this.lblHumiLow.Text = "湿度下限:";
            this.lblHumiLow.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPressHigh
            // 
            this.lblPressHigh.Location = new System.Drawing.Point(4, 140);
            this.lblPressHigh.Name = "lblPressHigh";
            this.lblPressHigh.Size = new System.Drawing.Size(60, 22);
            this.lblPressHigh.TabIndex = 4;
            this.lblPressHigh.Text = "气压上限:";
            this.lblPressHigh.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPressLow
            // 
            this.lblPressLow.Location = new System.Drawing.Point(4, 164);
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
            this.nudTempHigh.Location = new System.Drawing.Point(68, 44);
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
            this.nudTempLow.Location = new System.Drawing.Point(68, 68);
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
            // 
            // nudHumiHigh
            // 
            this.nudHumiHigh.Location = new System.Drawing.Point(68, 92);
            this.nudHumiHigh.Name = "nudHumiHigh";
            this.nudHumiHigh.Size = new System.Drawing.Size(202, 25);
            this.nudHumiHigh.TabIndex = 9;
            this.nudHumiHigh.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});
            // 
            // nudHumiLow
            // 
            this.nudHumiLow.Location = new System.Drawing.Point(68, 116);
            this.nudHumiLow.Name = "nudHumiLow";
            this.nudHumiLow.Size = new System.Drawing.Size(202, 25);
            this.nudHumiLow.TabIndex = 10;
            this.nudHumiLow.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // nudPressureHigh
            // 
            this.nudPressureHigh.Location = new System.Drawing.Point(68, 140);
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
            this.nudPressureHigh.Value = new decimal(new int[] {
            110,
            0,
            0,
            0});
            // 
            // nudPressureLow
            // 
            this.nudPressureLow.Location = new System.Drawing.Point(68, 164);
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
            this.gbData.Location = new System.Drawing.Point(12, 728);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(280, 105);
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
            this.btnExportCSV.Location = new System.Drawing.Point(30, 44);
            this.btnExportCSV.Name = "btnExportCSV";
            this.btnExportCSV.Size = new System.Drawing.Size(220, 27);
            this.btnExportCSV.TabIndex = 1;
            this.btnExportCSV.Text = "导出CSV文件";
            // 
            // btnClearChart
            // 
            this.btnClearChart.Location = new System.Drawing.Point(30, 76);
            this.btnClearChart.Name = "btnClearChart";
            this.btnClearChart.Size = new System.Drawing.Size(220, 27);
            this.btnClearChart.TabIndex = 2;
            this.btnClearChart.Text = "清除图表";
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(12, 844);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(280, 22);
            this.lblStatus.TabIndex = 7;
            this.lblStatus.Text = "就绪";
            // 
            // chart1
            // 
            chartArea2.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea2.AxisX.Title = "采样点序号";
            chartArea2.AxisX.TitleFont = new System.Drawing.Font("微软雅黑", 9F);
            chartArea2.AxisY.Interval = 10D;
            chartArea2.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea2.AxisY.Maximum = 100D;
            chartArea2.AxisY.Minimum = -10D;
            chartArea2.AxisY.Title = "数值";
            chartArea2.AxisY.TitleFont = new System.Drawing.Font("微软雅黑", 9F);
            chartArea2.CursorX.IsUserEnabled = true;
            chartArea2.CursorX.IsUserSelectionEnabled = true;
            chartArea2.Name = "MainArea";
            this.chart1.ChartAreas.Add(chartArea2);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top;
            legend2.Font = new System.Drawing.Font("微软雅黑", 9F);
            legend2.IsTextAutoFit = false;
            legend2.Name = "Legend";
            this.chart1.Legends.Add(legend2);
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.Name = "chart1";
            series4.BorderWidth = 2;
            series4.ChartArea = "MainArea";
            series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series4.Color = System.Drawing.Color.Red;
            series4.Legend = "Legend";
            series4.LegendText = "温度 (℃)";
            series4.MarkerColor = System.Drawing.Color.Red;
            series4.MarkerSize = 6;
            series4.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
            series4.Name = "温度";
            series5.BorderWidth = 2;
            series5.ChartArea = "MainArea";
            series5.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series5.Color = System.Drawing.Color.Blue;
            series5.Legend = "Legend";
            series5.LegendText = "湿度 (%)";
            series5.MarkerColor = System.Drawing.Color.Blue;
            series5.MarkerSize = 6;
            series5.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Diamond;
            series5.Name = "湿度";
            series6.BorderWidth = 2;
            series6.ChartArea = "MainArea";
            series6.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series6.Color = System.Drawing.Color.Green;
            series6.Legend = "Legend";
            series6.LegendText = "气压 (kPa)";
            series6.MarkerColor = System.Drawing.Color.Green;
            series6.MarkerSize = 6;
            series6.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Triangle;
            series6.Name = "气压";
            this.chart1.Series.Add(series4);
            this.chart1.Series.Add(series5);
            this.chart1.Series.Add(series6);
            this.chart1.Size = new System.Drawing.Size(811, 877);
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
            this.gbStats.ResumeLayout(false);
            this.gbAlarm.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudTempHigh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiHigh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHumiLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPressureHigh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPressureLow)).EndInit();
            this.gbData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

            // ---- 事件绑定 ----
            this.Load += MainForm_Load;
            this.Shown += MainForm_Shown;
            this.FormClosing += MainForm_FormClosing;
            this.chkSimMode.CheckedChanged += chkSimMode_CheckedChanged;
            this.btnRefreshPorts.Click += btnRefreshPorts_Click;
            this.btnOpenCloseCom.Click += btnOpenCloseCom_Click;
            this.btnManualSend.Click += btnManualSend_Click;
            this.btnClearStats.Click += btnClearStats_Click;
            this.btnExportCSV.Click += btnExportCSV_Click;
            this.btnClearChart.Click += btnClearChart_Click;
            this.chkEnableAlarm.CheckedChanged += chkEnableAlarm_CheckedChanged;
            this.chkDataLog.CheckedChanged += chkDataLog_CheckedChanged;
            this.cbReadMode.SelectedIndexChanged += cbReadMode_SelectedIndexChanged;
            this.nudInterval.ValueChanged += nudInterval_ValueChanged;
            this.nudMaxPoints.ValueChanged += nudMaxPoints_ValueChanged;
            this.timer1.Tick += timer1_Tick;
            this.serialPort1.DataReceived += serialPort1_DataReceived;
            this.serialPort1.ErrorReceived += serialPort1_ErrorReceived;

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
        private Label lblStatsTemp;
        private Label lblStatsHumi;
        private Label lblStatsPress;
        private Label lblTempHigh;
        private Label lblTempLow;
        private Label lblHumiHigh;
        private Label lblHumiLow;
        private Label lblPressHigh;
        private Label lblPressLow;
    }
}
