using System.Windows.Forms;

namespace TempHumidityMonitor
{
    partial class MainForm
    {
        // 所有控件的字段声明（由构造函数动态创建）
        private SplitContainer splitContainer1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tsslStatus, tsslSend, tsslRecv, tsslError, tsslTime;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private GroupBox gbSerial, gbCollect, gbCurrent, gbStats, gbAlarm, gbData;
        private ComboBox cbComPort, cbBaudRate, cbReadMode;
        private Button btnRefreshPorts, btnOpenCloseCom, btnManualSend;
        private Button btnClearStats, btnExportCSV, btnClearChart;
        private CheckBox chkSimMode, chkEnableAlarm, chkDataLog;
        private NumericUpDown nudInterval, nudMaxPoints;
        private NumericUpDown nudTempHigh, nudTempLow, nudHumiHigh, nudHumiLow;
        private Label lblTempValue, lblHumiValue, lblUpdateTime;
        private Label lblTempMin, lblTempMax, lblTempAvg;
        private Label lblHumiMin, lblHumiMax, lblHumiAvg;
        private Label lblStatus;
        private Timer timer1;
        private System.IO.Ports.SerialPort serialPort1;

        /// <summary>
        /// 设计器需要此方法存在，但所有控件在BuildFullUI()中动态创建
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "温湿度传感器监控程序";
            this.Size = new System.Drawing.Size(1240, 830);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
        }
    }
}
