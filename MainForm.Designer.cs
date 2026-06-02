using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TempHumidityMonitor
{
    partial class MainForm
    {
        // ==================== 容器 ====================
        private SplitContainer splitContainer1;
        private StatusStrip statusStrip1;

        // ==================== 串口设置 ====================
        private GroupBox gbSerial;
        private ComboBox cbComPort;
        private Button btnRefreshPorts;
        private ComboBox cbBaudRate;
        private Button btnOpenCloseCom;
        private Button btnManualSend;

        // ==================== 模拟模式 ====================
        private CheckBox chkSimMode;

        // ==================== 采集设置 ====================
        private GroupBox gbCollect;
        private ComboBox cbReadMode;
        private NumericUpDown nudInterval;
        private NumericUpDown nudMaxPoints;

        // ==================== 当前数据 ====================
        private GroupBox gbCurrent;
        private Label lblTempValue;
        private Label lblHumiValue;
        private Label lblUpdateTime;

        // ==================== 统计信息 ====================
        private GroupBox gbStats;
        private Label lblTempMin;
        private Label lblTempMax;
        private Label lblTempAvg;
        private Label lblHumiMin;
        private Label lblHumiMax;
        private Label lblHumiAvg;
        private Button btnClearStats;

        // ==================== 报警设置 ====================
        private GroupBox gbAlarm;
        private CheckBox chkEnableAlarm;
        private NumericUpDown nudTempHigh;
        private NumericUpDown nudTempLow;
        private NumericUpDown nudHumiHigh;
        private NumericUpDown nudHumiLow;

        // ==================== 数据管理 ====================
        private GroupBox gbData;
        private CheckBox chkDataLog;
        private Button btnExportCSV;
        private Button btnClearChart;

        // ==================== 状态 ====================
        private Label lblStatus;

        // ==================== Chart ====================
        private Chart chart1;

        // ==================== Timer & SerialPort ====================
        private Timer timer1;
        private SerialPort serialPort1;

        // ==================== StatusStrip 标签 ====================
        private ToolStripStatusLabel tsslStatus;
        private ToolStripStatusLabel tsslSend;
        private ToolStripStatusLabel tsslRecv;
        private ToolStripStatusLabel tsslError;
        private ToolStripStatusLabel tsslTime;

        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            statusStrip1 = new StatusStrip();
            tsslStatus = new ToolStripStatusLabel();
            tsslSend = new ToolStripStatusLabel();
            tsslRecv = new ToolStripStatusLabel();
            tsslError = new ToolStripStatusLabel();
            tsslTime = new ToolStripStatusLabel();

            // ==================== 串口设置 ====================
            gbSerial = new GroupBox();
            cbComPort = new ComboBox();
            btnRefreshPorts = new Button();
            cbBaudRate = new ComboBox();
            btnOpenCloseCom = new Button();
            btnManualSend = new Button();

            // ==================== 模拟模式 ====================
            chkSimMode = new CheckBox();

            // ==================== 采集设置 ====================
            gbCollect = new GroupBox();
            cbReadMode = new ComboBox();
            nudInterval = new NumericUpDown();
            nudMaxPoints = new NumericUpDown();

            // ==================== 当前数据 ====================
            gbCurrent = new GroupBox();
            lblTempValue = new Label();
            lblHumiValue = new Label();
            lblUpdateTime = new Label();

            // ==================== 统计信息 ====================
            gbStats = new GroupBox();
            lblTempMin = new Label();
            lblTempMax = new Label();
            lblTempAvg = new Label();
            lblHumiMin = new Label();
            lblHumiMax = new Label();
            lblHumiAvg = new Label();
            btnClearStats = new Button();

            // ==================== 报警设置 ====================
            gbAlarm = new GroupBox();
            chkEnableAlarm = new CheckBox();
            nudTempHigh = new NumericUpDown();
            nudTempLow = new NumericUpDown();
            nudHumiHigh = new NumericUpDown();
            nudHumiLow = new NumericUpDown();

            // ==================== 数据管理 ====================
            gbData = new GroupBox();
            chkDataLog = new CheckBox();
            btnExportCSV = new Button();
            btnClearChart = new Button();

            // ==================== 状态 ====================
            lblStatus = new Label();

            // ==================== Chart ====================
            chart1 = new Chart();

            // ==================== Timer & SerialPort ====================
            timer1 = new Timer();
            serialPort1 = new SerialPort();

            // ==================== 窗体设置 ====================
            ((System.ComponentModel.ISupportInitialize)(splitContainer1)).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.SuspendLayout();
            statusStrip1.SuspendLayout();
            this.SuspendLayout();
            gbSerial.SuspendLayout();
            gbCollect.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(nudInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudMaxPoints)).BeginInit();
            gbCurrent.SuspendLayout();
            gbStats.SuspendLayout();
            gbAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(nudTempHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudTempLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudHumiHigh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudHumiLow)).BeginInit();
            gbData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(chart1)).BeginInit();

            // ---- Form ----
            this.Text = "温湿度传感器监控程序";
            this.Size = new Size(1100, 800);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ---- SplitContainer ----
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.SplitterDistance = 285;
            splitContainer1.Panel1MinSize = 260;
            splitContainer1.Panel2MinSize = 200;

            // ---- StatusStrip ----
            statusStrip1.Dock = DockStyle.Bottom;
            tsslStatus.Text = "● 串口未打开";
            tsslStatus.ForeColor = Color.Gray;
            tsslStatus.Width = 140;
            tsslSend.Text = "发送: 0";
            tsslRecv.Text = "接收: 0";
            tsslError.Text = "错误: 0";
            tsslError.ForeColor = Color.Orange;
            tsslTime.Text = "00:00:00";
            statusStrip1.Items.AddRange(new ToolStripItem[] {
                tsslStatus, tsslSend, tsslRecv, tsslError,
                new ToolStripStatusLabel("  "), tsslTime
            });

            // ==================== 左侧 Panel1 控件 ====================
            Panel pnlLeft = splitContainer1.Panel1;
            pnlLeft.Padding = new Padding(4);
            int top = 4;

            // ---- 模拟模式 CheckBox（最顶部） ----
            chkSimMode.Text = "模拟模式（无需硬件即可演示）";
            chkSimMode.Location = new Point(4, top);
            chkSimMode.Size = new Size(270, 22);
            pnlLeft.Controls.Add(chkSimMode);
            top += 26;

            // ---- 串口设置 GroupBox ----
            gbSerial.Text = "串口设置";
            gbSerial.Location = new Point(4, top);
            gbSerial.Size = new Size(272, 110);
            top += 114;
            {
                TableLayoutPanel tlp = new TableLayoutPanel();
                tlp.Dock = DockStyle.Fill;
                tlp.ColumnCount = 2;
                tlp.RowCount = 3;
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                gbSerial.Controls.Add(tlp);

                Label lbl = new Label() { Text = "串口号:", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 0);
                Panel row1 = new Panel() { Dock = DockStyle.Fill };
                cbComPort.DropDownStyle = ComboBoxStyle.DropDownList;
                cbComPort.Width = 130;
                cbComPort.Location = new Point(0, 2);
                btnRefreshPorts.Text = "刷新";
                btnRefreshPorts.Size = new Size(48, 22);
                btnRefreshPorts.Location = new Point(136, 2);
                row1.Controls.Add(cbComPort);
                row1.Controls.Add(btnRefreshPorts);
                tlp.Controls.Add(row1, 1, 0);

                lbl = new Label() { Text = "波特率:", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 1);
                cbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
                cbBaudRate.Items.AddRange(new object[] { "4800", "9600", "19200", "38400", "57600", "115200" });
                cbBaudRate.SelectedIndex = 1;
                cbBaudRate.Dock = DockStyle.Fill;
                tlp.Controls.Add(cbBaudRate, 1, 1);

                Panel row2 = new Panel() { Dock = DockStyle.Fill };
                btnOpenCloseCom.Text = "打开串口";
                btnOpenCloseCom.Size = new Size(105, 24);
                btnOpenCloseCom.Location = new Point(0, 1);
                btnManualSend.Text = "手动采集";
                btnManualSend.Size = new Size(80, 24);
                btnManualSend.Location = new Point(110, 1);
                row2.Controls.Add(btnOpenCloseCom);
                row2.Controls.Add(btnManualSend);
                tlp.Controls.Add(row2, 1, 2);
            }
            pnlLeft.Controls.Add(gbSerial);

            // ---- 采集设置 GroupBox ----
            gbCollect.Text = "采集设置";
            gbCollect.Location = new Point(4, top);
            gbCollect.Size = new Size(272, 95);
            top += 99;
            {
                TableLayoutPanel tlp = new TableLayoutPanel();
                tlp.Dock = DockStyle.Fill;
                tlp.ColumnCount = 2;
                tlp.RowCount = 3;
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                gbCollect.Controls.Add(tlp);

                Label lbl = new Label() { Text = "读取模式:", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 0);
                cbReadMode.DropDownStyle = ComboBoxStyle.DropDownList;
                cbReadMode.Items.AddRange(new object[] { "一次读取（浮点格式）", "一次读取（整型格式）", "单独读取温度（浮点）", "单独读取湿度（浮点）", "单独读取温度（整型）", "单独读取湿度（整型）" });
                cbReadMode.SelectedIndex = 0;
                cbReadMode.Dock = DockStyle.Fill;
                tlp.Controls.Add(cbReadMode, 1, 0);

                lbl = new Label() { Text = "间隔(ms):", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 1);
                nudInterval.Minimum = 200; nudInterval.Maximum = 60000; nudInterval.Increment = 100; nudInterval.Value = 1000;
                nudInterval.Dock = DockStyle.Fill;
                tlp.Controls.Add(nudInterval, 1, 1);

                lbl = new Label() { Text = "最大点数:", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 2);
                nudMaxPoints.Minimum = 10; nudMaxPoints.Maximum = 500; nudMaxPoints.Increment = 10; nudMaxPoints.Value = 30;
                nudMaxPoints.Dock = DockStyle.Fill;
                tlp.Controls.Add(nudMaxPoints, 1, 2);
            }
            pnlLeft.Controls.Add(gbCollect);

            // ---- 当前数据 GroupBox ----
            gbCurrent.Text = "当前数据";
            gbCurrent.Location = new Point(4, top);
            gbCurrent.Size = new Size(272, 90);
            top += 94;
            {
                TableLayoutPanel tlp = new TableLayoutPanel();
                tlp.Dock = DockStyle.Fill;
                tlp.ColumnCount = 2;
                tlp.RowCount = 3;
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
                gbCurrent.Controls.Add(tlp);

                tlp.Controls.Add(new Label() { Text = "温度:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
                lblTempValue.Text = "--.- ℃";
                lblTempValue.TextAlign = ContentAlignment.MiddleLeft;
                lblTempValue.Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold);
                lblTempValue.ForeColor = Color.Red;
                tlp.Controls.Add(lblTempValue, 1, 0);

                tlp.Controls.Add(new Label() { Text = "湿度:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
                lblHumiValue.Text = "--.- %";
                lblHumiValue.TextAlign = ContentAlignment.MiddleLeft;
                lblHumiValue.Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold);
                lblHumiValue.ForeColor = Color.Blue;
                tlp.Controls.Add(lblHumiValue, 1, 1);

                tlp.Controls.Add(new Label() { Text = "更新:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
                lblUpdateTime.Text = "--";
                lblUpdateTime.TextAlign = ContentAlignment.MiddleLeft;
                tlp.Controls.Add(lblUpdateTime, 1, 2);
            }
            pnlLeft.Controls.Add(gbCurrent);

            // ---- 统计信息 GroupBox ----
            gbStats.Text = "统计信息";
            gbStats.Location = new Point(4, top);
            gbStats.Size = new Size(272, 105);
            top += 109;
            {
                TableLayoutPanel tlp = new TableLayoutPanel();
                tlp.Dock = DockStyle.Fill;
                tlp.ColumnCount = 4;
                tlp.RowCount = 3;
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                gbStats.Controls.Add(tlp);

                tlp.Controls.Add(new Label() { Text = "温度:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
                lblTempMin.Text = "最小:--"; lblTempMin.ForeColor = Color.DarkRed; lblTempMin.TextAlign = ContentAlignment.MiddleLeft;
                lblTempMax.Text = "最大:--"; lblTempMax.ForeColor = Color.DarkRed; lblTempMax.TextAlign = ContentAlignment.MiddleLeft;
                lblTempAvg.Text = "平均:--"; lblTempAvg.ForeColor = Color.DarkRed; lblTempAvg.TextAlign = ContentAlignment.MiddleLeft;
                tlp.Controls.Add(lblTempMin, 1, 0);
                tlp.Controls.Add(lblTempMax, 2, 0);
                tlp.Controls.Add(lblTempAvg, 3, 0);

                tlp.Controls.Add(new Label() { Text = "湿度:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
                lblHumiMin.Text = "最小:--"; lblHumiMin.ForeColor = Color.DarkBlue; lblHumiMin.TextAlign = ContentAlignment.MiddleLeft;
                lblHumiMax.Text = "最大:--"; lblHumiMax.ForeColor = Color.DarkBlue; lblHumiMax.TextAlign = ContentAlignment.MiddleLeft;
                lblHumiAvg.Text = "平均:--"; lblHumiAvg.ForeColor = Color.DarkBlue; lblHumiAvg.TextAlign = ContentAlignment.MiddleLeft;
                tlp.Controls.Add(lblHumiMin, 1, 1);
                tlp.Controls.Add(lblHumiMax, 2, 1);
                tlp.Controls.Add(lblHumiAvg, 3, 1);

                btnClearStats.Text = "重置统计"; btnClearStats.Size = new Size(70, 22);
                tlp.Controls.Add(btnClearStats, 0, 2);
                tlp.SetColumnSpan(btnClearStats, 4);
            }
            pnlLeft.Controls.Add(gbStats);

            // ---- 报警设置 GroupBox ----
            gbAlarm.Text = "报警设置";
            gbAlarm.Location = new Point(4, top);
            gbAlarm.Size = new Size(272, 130);
            top += 134;
            {
                TableLayoutPanel tlp = new TableLayoutPanel();
                tlp.Dock = DockStyle.Fill;
                tlp.ColumnCount = 2;
                tlp.RowCount = 5;
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                gbAlarm.Controls.Add(tlp);

                chkEnableAlarm.Text = "启用报警"; chkEnableAlarm.Dock = DockStyle.Fill; chkEnableAlarm.TextAlign = ContentAlignment.MiddleLeft;
                tlp.Controls.Add(chkEnableAlarm, 1, 0);

                Label lbl = new Label() { Text = "温度上限:", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 1);
                nudTempHigh.Minimum = -50; nudTempHigh.Maximum = 150; nudTempHigh.Increment = 0.5M; nudTempHigh.Value = 40; nudTempHigh.DecimalPlaces = 1;
                nudTempHigh.Dock = DockStyle.Fill; tlp.Controls.Add(nudTempHigh, 1, 1);

                lbl = new Label() { Text = "温度下限:", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 2);
                nudTempLow.Minimum = -50; nudTempLow.Maximum = 150; nudTempLow.Increment = 0.5M; nudTempLow.Value = 0; nudTempLow.DecimalPlaces = 1;
                nudTempLow.Dock = DockStyle.Fill; tlp.Controls.Add(nudTempLow, 1, 2);

                lbl = new Label() { Text = "湿度上限:", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 3);
                nudHumiHigh.Minimum = 0; nudHumiHigh.Maximum = 100; nudHumiHigh.Increment = 1; nudHumiHigh.Value = 80;
                nudHumiHigh.Dock = DockStyle.Fill; tlp.Controls.Add(nudHumiHigh, 1, 3);

                lbl = new Label() { Text = "湿度下限:", TextAlign = ContentAlignment.MiddleRight };
                tlp.Controls.Add(lbl, 0, 4);
                nudHumiLow.Minimum = 0; nudHumiLow.Maximum = 100; nudHumiLow.Increment = 1; nudHumiLow.Value = 20;
                nudHumiLow.Dock = DockStyle.Fill; tlp.Controls.Add(nudHumiLow, 1, 4);
            }
            pnlLeft.Controls.Add(gbAlarm);

            // ---- 数据管理 GroupBox ----
            gbData.Text = "数据管理";
            gbData.Location = new Point(4, top);
            gbData.Size = new Size(272, 85);
            top += 89;
            {
                chkDataLog.Text = "启用数据记录"; chkDataLog.Checked = true;
                chkDataLog.Dock = DockStyle.Fill; chkDataLog.TextAlign = ContentAlignment.MiddleLeft;
                btnExportCSV.Text = "导出CSV文件"; btnExportCSV.Dock = DockStyle.Fill;
                btnClearChart.Text = "清除图表"; btnClearChart.Dock = DockStyle.Fill;

                TableLayoutPanel tlp = new TableLayoutPanel();
                tlp.Dock = DockStyle.Fill;
                tlp.ColumnCount = 1;
                tlp.RowCount = 3;
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
                gbData.Controls.Add(tlp);
                tlp.Controls.Add(chkDataLog, 0, 0);
                tlp.Controls.Add(btnExportCSV, 0, 1);
                tlp.Controls.Add(btnClearChart, 0, 2);
            }
            pnlLeft.Controls.Add(gbData);

            // ---- 状态 Label ----
            lblStatus.Text = "就绪";
            lblStatus.Location = new Point(4, top);
            lblStatus.Size = new Size(270, 18);
            lblStatus.ForeColor = Color.Gray;
            pnlLeft.Controls.Add(lblStatus);

            // ---- Chart（右侧 Panel2） ----
            chart1.Dock = DockStyle.Fill;
            {
                ChartArea area = new ChartArea("MainArea");
                area.AxisX.Title = "采样点序号";
                area.AxisX.TitleFont = new Font("Microsoft YaHei", 9);
                area.AxisX.MajorGrid.LineColor = Color.LightGray;
                area.AxisY.Title = "数值";
                area.AxisY.TitleFont = new Font("Microsoft YaHei", 9);
                area.AxisY.MajorGrid.LineColor = Color.LightGray;
                area.AxisY.Minimum = -10;
                area.AxisY.Maximum = 100;
                area.AxisY.Interval = 10;
                area.CursorX.IsUserEnabled = true;
                area.CursorX.IsUserSelectionEnabled = true;
                area.AxisX.ScrollBar.Enabled = true;
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
            }
            splitContainer1.Panel2.Controls.Add(chart1);

            // ==================== 组装主布局 ====================
            this.Controls.Add(splitContainer1);
            this.Controls.Add(statusStrip1);

            // ---- Timer ----
            timer1.Interval = 1000;

            // ---- 恢复布局 ----
            ((System.ComponentModel.ISupportInitialize)(chart1)).EndInit();
            gbData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(nudHumiLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudHumiHigh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudTempLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudTempHigh)).EndInit();
            gbAlarm.ResumeLayout(false);
            gbStats.ResumeLayout(false);
            gbCurrent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(nudMaxPoints)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudInterval)).EndInit();
            gbCollect.ResumeLayout(false);
            gbSerial.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(splitContainer1)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
