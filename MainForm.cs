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
using TempHumidityMonitor.Services;

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
        private string lastAlarmMsg = "";
        private DateTime lastAlarmSound = DateTime.MinValue;
        private string lastPortName = "";
        private bool isReconnecting = false;
        private int connectVersion = 0;
        private Timer reconnectTimer;
        private DatabaseService dbService;
        private LogService logService;
        private ApiService apiService;
        private int apiPort = 8090;

        // ==================== 子窗体 ====================
        private SensorDetailForm tempDetailForm;
        private SensorDetailForm humiDetailForm;
        private SensorDetailForm pressureDetailForm;

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
            InitDetailForms();
            LoadSettings();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            // 预置起始点确保坐标轴始终可见
            chart1.Series["温度"].Points.AddXY(1, 0);
            chart1.Series["湿度"].Points.AddXY(1, 0);
            chart1.Series["气压"].Points.AddXY(1, 0);

            // 在右侧 GroupBox 右边缘叠加实线 Panel，消除虚线边框
            AddSolidRightBorder(gbCurrent);
            AddSolidRightBorder(gbStatsTemp);
            AddSolidRightBorder(gbStatsHumi);
            AddSolidRightBorder(gbStatsPress);
        }

        private void AddSolidRightBorder(GroupBox gb)
        {
            var line = new Panel
            {
                Width = 1,
                Height = gb.Height,
                BackColor = SystemColors.ControlDark,
                Location = new Point(gb.Width - 1, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            gb.Controls.Add(line);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance = 310;
            splitContainer1.Panel1MinSize = 290;
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
            string configPath = Path.Combine(Application.StartupPath, "modbus_config.json");
            if (!File.Exists(configPath))
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\modbus_config.json");
            ModbusService.LoadConfig(configPath);
            dbService = new DatabaseService(GetDbPath());
            logService = new LogService(Application.StartupPath);
            try { dbService.CleanOldData((int)nudRetainDays.Value); } catch { }
            dtpStart.Value = DateTime.Today;
            dtpEnd.Value = DateTime.Now;
            SwitchTab(true);

            Timer timeTimer = new Timer { Interval = 1000 };
            timeTimer.Tick += (s, ev) => { tsslTime.Text = DateTime.Now.ToString("HH:mm:ss"); };
            timeTimer.Start();

            // 修复窗体图标和托盘图标
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "app_icon.ico");
                if (!File.Exists(iconPath))
                    iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\app_icon.ico");
                if (File.Exists(iconPath))
                {
                    var appIcon = new Icon(iconPath);
                    this.Icon = appIcon;
                    notifyIcon1.Icon = appIcon;
                }
            }
            catch { }

            // 网页服务事件绑定（控件在设计器中已创建）
            try
            {
                nudWebPort.ValueChanged += (s, ev) =>
                {
                    apiPort = (int)nudWebPort.Value;
                    lblStatus.Text = "网页端口已更改为 " + apiPort + "（需重启程序生效）";
                    lblStatus.ForeColor = Color.Orange;
                };
                btnOpenWeb.Click += (s, ev) =>
                {
                    System.Diagnostics.Process.Start("http://localhost:" + apiPort + "/");
                };
            }
            catch { }

            // 启动 HTTP API 服务
            try
            {
                string webRoot = Path.Combine(Application.StartupPath, "web");
                if (!Directory.Exists(webRoot))
                    webRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\web"));
                apiService = new ApiService(apiPort, webRoot, dbService);
                apiService.Start();
                lblStatus.Text = "API:" + apiPort + " | " + lblStatus.Text;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "API启动失败(端口" + apiPort + "): " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }

            // 启动云端推送服务
            try
            {
                string cloudUrl = ReadCloudUrl(configPath);
                if (!string.IsNullOrEmpty(cloudUrl))
                {
                    CloudPushService.Start(cloudUrl);
                    lblStatus.Text = lblStatus.Text + " | 云:" + cloudUrl.Replace("http://", "").Replace(":8080","");
                }
            }
            catch (Exception ex)
            {
                LogService.Warn("[CloudPush] 启动失败: " + ex.Message);
            }
        }

        // ==================== 详情子窗体 ====================
        private void InitDetailForms()
        {
            tempDetailForm = new SensorDetailForm("温度", "℃", Color.Red, maxChartPoint);
            humiDetailForm = new SensorDetailForm("湿度", "%", Color.Blue, maxChartPoint);
            pressureDetailForm = new SensorDetailForm("气压", "kPa", Color.Green, maxChartPoint);

            btnTempDetail.Click += (s, e) => { tempDetailForm.Show(); tempDetailForm.BringToFront(); };
            btnHumiDetail.Click += (s, e) => { humiDetailForm.Show(); humiDetailForm.BringToFront(); };
            btnPressureDetail.Click += (s, e) => { pressureDetailForm.Show(); pressureDetailForm.BringToFront(); };

            // 曲线显示/隐藏切换
            chkShowTemp.CheckedChanged += (s2, e2) =>
            {
                chart1.Series["温度"].Enabled = chkShowTemp.Checked;
                var c = chkShowTemp.Checked ? Color.FromArgb(255, 230, 230) : SystemColors.Control;
                chkShowTemp.BackColor = c;
            };
            chkShowHumi.CheckedChanged += (s2, e2) =>
            {
                chart1.Series["湿度"].Enabled = chkShowHumi.Checked;
                var c = chkShowHumi.Checked ? Color.FromArgb(220, 230, 255) : SystemColors.Control;
                chkShowHumi.BackColor = c;
            };
            chkShowPressure.CheckedChanged += (s2, e2) =>
            {
                chart1.Series["气压"].Enabled = chkShowPressure.Checked;
                var c = chkShowPressure.Checked ? Color.FromArgb(220, 255, 220) : SystemColors.Control;
                chkShowPressure.BackColor = c;
            };

            nudMaxPoints.ValueChanged += (s2, e2) =>
            {
                tempDetailForm.MaxChartPoints = maxChartPoint;
                humiDetailForm.MaxChartPoints = maxChartPoint;
                pressureDetailForm.MaxChartPoints = maxChartPoint;
            };
        }

        public void ShowTempDetail() { tempDetailForm?.Show(); tempDetailForm?.BringToFront(); }
        public void ShowHumiDetail() { humiDetailForm?.Show(); humiDetailForm?.BringToFront(); }
        public void ShowPressureDetail() { pressureDetailForm?.Show(); pressureDetailForm?.BringToFront(); }
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
        {
            if (isReconnecting) { isReconnecting = false; CancelTimer(); closeComPort(); return; }
            if (isComOpen) closeComPort(); else openComPort();
        }

        private void openComPort()
        {
            try
            {
                string portName = cbComPort.Text;
                if (string.IsNullOrEmpty(portName) || portName == "无可用串口") { ShowTip("请选择有效的串口号"); return; }
                serialPort1.PortName = portName;
                lastPortName = portName;
                serialPort1.BaudRate = int.Parse(cbBaudRate.Text);
                serialPort1.DataBits = 8; serialPort1.StopBits = StopBits.One; serialPort1.Parity = Parity.None;
                serialPort1.ReadTimeout = 500; serialPort1.WriteTimeout = 500;
                serialPort1.Open();
                isComOpen = true;
                if (!isReconnecting)
                {
                    btnOpenCloseCom.Text = "关闭串口"; btnOpenCloseCom.BackColor = Color.LightCoral;
                }
                btnOpenCloseCom.Enabled = true;
                if (!isReconnecting)
                {
                    _retryCount = 0; isReconnecting = false;
                }
                CancelTimer();
                tsslStatus.Text = "● 串口已打开 - " + portName; tsslStatus.ForeColor = Color.Green;
                timer1.Interval = (int)nudInterval.Value;
                isReading = true; UpdateToggleButton();
                btnToggleRead.Text = "■ 停止采集"; btnToggleRead.BackColor = Color.LightCoral;
                cbComPort.Enabled = false; btnRefreshPorts.Enabled = false; cbBaudRate.Enabled = false;
                lblStatus.Text = "串口已打开，正在采集数据..."; lblStatus.ForeColor = Color.Green;
                ApiService.UpdateStatus(true, false, true, false, "串口已打开，正在采集数据...");
                lastReceiveTime = DateTime.Now; // 启动超时计时：15秒无有效数据 → 触发重连
                receiveBuffer.Clear();
            }
            catch (Exception ex) { if (!isReconnecting) ShowTip("打开串口失败: " + ex.Message); LogError("打开串口失败: " + ex.Message); }
        }

        private void closeComPort()
        {
            isReconnecting = false;
            CancelTimer();
            timer1.Stop();
            try { if (serialPort1.IsOpen) serialPort1.Close(); } catch { }
            try { serialPort1.Dispose(); } catch { }
            serialPort1 = new System.IO.Ports.SerialPort();
            serialPort1.DataReceived += serialPort1_DataReceived;
            serialPort1.ErrorReceived += serialPort1_ErrorReceived;
            isComOpen = false; isReading = false;
            btnOpenCloseCom.Text = "打开串口"; btnOpenCloseCom.BackColor = SystemColors.Control;
            btnOpenCloseCom.Enabled = true;
            btnToggleRead.Enabled = false; btnToggleRead.Text = "▶ 开始采集";
            btnToggleRead.BackColor = SystemColors.Control;
            tsslStatus.Text = "● 串口已关闭"; tsslStatus.ForeColor = Color.Gray;
            cbComPort.Enabled = true; btnRefreshPorts.Enabled = true; cbBaudRate.Enabled = true;
            lblStatus.Text = "串口已关闭"; lblStatus.ForeColor = Color.Gray;
            ApiService.UpdateStatus(false, false, false, false, "串口已关闭");
        }

        // ==================== 定时器 ====================
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!isComOpen || isSimMode)
            {
                if (isReading) sendData();
                return;
            }

            // 15 秒无有效数据
            if (lastReceiveTime > DateTime.MinValue &&
                (DateTime.Now - lastReceiveTime).TotalSeconds > 15)
            {
                TryReconnect("连接断开"); return;
            }
            if (isReading) sendData();
        }

        // ==================== 发送数据 ====================
        private void sendData()
        {
            try
            {
                if (isSimMode) { SimulateData(); return; }
                if (!isComOpen || !serialPort1.IsOpen) return;
                byte[] cmd = ModbusService.GetCommand(readModeIndex);
                serialPort1.Write(cmd, 0, cmd.Length);
                nSend++; tsslSend.Text = "发送: " + nSend;
                ApiService.UpdateCounters(nSend, nReceive, nError);
            }
            catch (Exception ex)
            {
                nError++; tsslError.Text = "错误: " + nError;
                ApiService.UpdateCounters(nSend, nReceive, nError);
                LogError("发送数据失败: " + ex.Message);
                if (isComOpen) TryReconnect("连接断开");
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
            ApiService.UpdateCounters(nSend, nReceive, nError);
            lastReceiveTime = DateTime.Now;
            AddDataPoint(simTemp, simHumi, simPressure);
            if (alarmEnabled) CheckAlarm(simTemp, simHumi, simPressure);
            if (dataLogEnabled) LogDataToFile(simTemp, simHumi, simPressure);
            SaveToDatabase(simTemp, simHumi, simPressure);
            ApiService.BroadcastSensorData(simTemp, simHumi, simPressure, true);
            PushToDetailForms(simTemp, simHumi, simPressure);
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
                ApiService.UpdateStatus(false, true, true, false, "模拟模式运行中");
            }
            else
            {
                timer1.Stop(); isReading = false; btnOpenCloseCom.Enabled = true;
                btnToggleRead.Enabled = false; btnToggleRead.Text = "▶ 开始采集";
                btnToggleRead.BackColor = SystemColors.Control;
                cbComPort.Enabled = true; btnRefreshPorts.Enabled = true; cbBaudRate.Enabled = true;
                tsslStatus.Text = "● 串口未打开"; tsslStatus.ForeColor = Color.Gray;
                lblStatus.Text = "就绪"; lblStatus.ForeColor = Color.Gray;
                ApiService.UpdateStatus(false, false, false, false, "就绪");
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
            catch (Exception ex) { LogError("接收异常: " + ex.Message); nError++; UpdateStatus(); if (isComOpen) this.BeginInvoke(new Action(() => TryReconnect("连接断开"))); }
        }

        private void serialPort1_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            nError++;
            LogError("串口错误: " + e.EventType);
            this.BeginInvoke(new Action(() =>
            {
                tsslError.Text = "错误: " + nError;
            }));
        }
        private string _reconnectReason = "";
        private int _retryCount = 0;

        // ==================== 重连逻辑 ====================

        private void TryReconnect(string reason)
        {
            if (isSimMode) return;
            _reconnectReason = reason;

            // 关闭旧端口
            timer1.Stop();
            try { if (serialPort1.IsOpen) serialPort1.Close(); } catch { }
            try { serialPort1.Dispose(); } catch { }
            serialPort1 = new System.IO.Ports.SerialPort();
            serialPort1.DataReceived += serialPort1_DataReceived;
            serialPort1.ErrorReceived += serialPort1_ErrorReceived;
            isComOpen = false; isReading = false;
            CancelTimer();

            // 进入重连状态
            isReconnecting = true;
            connectVersion++;
            lastAlarmMsg = "";

            btnOpenCloseCom.Enabled = true;
            btnOpenCloseCom.Text = "取消重连";
            btnOpenCloseCom.BackColor = Color.Orange;
            btnToggleRead.Enabled = false;
            btnToggleRead.Text = "▶ 开始采集";
            btnToggleRead.BackColor = SystemColors.Control;
            cbComPort.Enabled = false;
            btnRefreshPorts.Enabled = false;
            cbBaudRate.Enabled = false;
            tsslStatus.Text = "● " + reason + "，重连中";
            tsslStatus.ForeColor = Color.OrangeRed;
            ApiService.UpdateStatus(false, false, false, true, reason + "，重连中...");

            _retryCount++;
            if (_retryCount > 3) { GiveUp(); return; }
            lblStatus.Text = string.Format(reason + "，5秒后自动重连({0}/3)...", _retryCount);
            lblStatus.ForeColor = Color.OrangeRed;

            if (reconnectTimer != null) reconnectTimer.Dispose();
            reconnectTimer = new Timer { Interval = 5000 };
            reconnectTimer.Tick += (s, ev) =>
            {
                CancelTimer();
                if (!isReconnecting) return;
                RefreshComPorts();
                if (cbComPort.Items.Count == 0 || cbComPort.Items[0].ToString() == "无可用串口")
                {
                    if (_retryCount >= 3) { GiveUp(); return; }
                    lblStatus.Text = string.Format("未检测到串口，5秒后重试({0}/3)...", _retryCount);
                    lblStatus.ForeColor = Color.OrangeRed;
                    reconnectTimer = new Timer { Interval = 5000 };
                    reconnectTimer.Tick += (s2, ev2) => { CancelTimer(); if (isReconnecting) DoRetry(); };
                    reconnectTimer.Start();
                    return;
                }
                DoRetry();
            };
            reconnectTimer.Start();
        }

        private void DoRetry()
        {
            if (!isReconnecting) return;
            if (!string.IsNullOrEmpty(lastPortName) && cbComPort.Items.Contains(lastPortName))
                cbComPort.Text = lastPortName;
            else if (cbComPort.Items.Count > 0)
                cbComPort.SelectedIndex = 0;

            openComPort();
            if (!isComOpen) { TryReconnect(_reconnectReason); return; }

            // 端口打开了，等数据来触发 ConfirmSuccess
            lblStatus.Text = string.Format("重连{0}/3：端口已打开，等待数据...", _retryCount);
            lblStatus.ForeColor = Color.Orange;
        }

        private void ConfirmSuccess()
        {
            isReconnecting = false;
            _retryCount = 0;
            CancelTimer();
            btnOpenCloseCom.Text = "关闭串口";
            btnOpenCloseCom.BackColor = Color.LightCoral;
            lblStatus.Text = "数据已恢复，正在采集...";
            lblStatus.ForeColor = Color.Green;
            tsslStatus.Text = "● 串口已打开 - " + lastPortName;
            tsslStatus.ForeColor = Color.Green;
            ApiService.UpdateStatus(true, false, true, false, "数据已恢复");
        }

        private void GiveUp()
        {
            isReconnecting = false;
            _retryCount = 0;
            CancelTimer();
            timer1.Stop();
            try { if (serialPort1.IsOpen) serialPort1.Close(); } catch { }
            try { serialPort1.Dispose(); } catch { }
            serialPort1 = new System.IO.Ports.SerialPort();
            serialPort1.DataReceived += serialPort1_DataReceived;
            serialPort1.ErrorReceived += serialPort1_ErrorReceived;
            isComOpen = false; isReading = false;
            cbComPort.Enabled = true; btnRefreshPorts.Enabled = true; cbBaudRate.Enabled = true;
            btnOpenCloseCom.Text = "打开串口";
            btnOpenCloseCom.BackColor = SystemColors.Control;
            btnOpenCloseCom.Enabled = true;
            btnToggleRead.Enabled = false;
            btnToggleRead.Text = "▶ 开始采集";
            btnToggleRead.BackColor = SystemColors.Control;
            tsslStatus.Text = "● 串口未打开";
            tsslStatus.ForeColor = Color.Gray;
            lblStatus.Text = "自动重连失败(已重试3次)，请检查设备或更换端口后手动打开";
            lblStatus.ForeColor = Color.Red;
            ApiService.UpdateStatus(false, false, false, false, "重连失败");
        }

        private void CancelTimer()
        {
            if (reconnectTimer != null) { reconnectTimer.Stop(); reconnectTimer.Dispose(); reconnectTimer = null; }
        }

        private void ProcessFrame(byte[] buffer)
        {
            nReceive++;
            ApiService.UpdateCounters(nSend, nReceive, nError);
            this.BeginInvoke(new Action(() => { tsslRecv.Text = "接收: " + nReceive; }));
            try
            {
                if (!ModbusService.CheckFrame(buffer))
                {
                    nError++;
                    ApiService.UpdateCounters(nSend, nReceive, nError);
                    this.BeginInvoke(new Action(() => { tsslError.Text = "错误: " + nError; }));
                    return;
                }
                lastReceiveTime = DateTime.Now;
                var data = ModbusService.ParseSensorData(buffer, readModeIndex, lastTemp, lastHumi, lastPressure);
                float t = data.Temperature, h = data.Humidity, p = data.Pressure;
                lastTemp = t; lastHumi = h; lastPressure = p;
                if (t < -40 || t > 125 || h < 0 || h > 100 || p < 50 || p > 200)
                { LogError(string.Format("数据超范围: T={0:F1} H={1:F1} P={2:F1}", t, h, p)); return; }
                if (isReconnecting)
                    this.BeginInvoke(new Action(() => ConfirmSuccess()));
                AddDataPoint(t, h, p);
                if (alarmEnabled) CheckAlarm(t, h, p);
                if (dataLogEnabled) LogDataToFile(t, h, p);
                SaveToDatabase(t, h, p);
                ApiService.BroadcastSensorData(t, h, p, false);
                bool hasAlarm = alarmEnabled && (t > alarmTempH || t < alarmTempL || h > alarmHumiH || h < alarmHumiL || p > alarmPressH || p < alarmPressL);
                this.BeginInvoke(new Action(() => updateUI(t, h, p)));
                if (!hasAlarm)
                {
                    int ver2 = connectVersion;
                    this.BeginInvoke(new Action(() =>
                    { if (ver2 == connectVersion) { lblStatus.Text = "数据正常 - " + lastReceiveTime.ToString("HH:mm:ss"); lblStatus.ForeColor = Color.Green; } }));
                }
            }
            catch (Exception ex)
            {
                nError++;
                ApiService.UpdateCounters(nSend, nReceive, nError);
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
            ApiService.UpdateStats(dataCount, tempMin, tempMax, tempSum,
                humiMin, humiMax, humiSum, pressureMin, pressureMax, pressureSum);
        }

        private void updateUI(float t, float h, float p)
        {
            lblTempValue.Text = string.Format("{0:F1} ℃", t);
            lblHumiValue.Text = string.Format("{0:F1} %", h);
            lblPressureValue.Text = string.Format("{0:F1} kPa", p);
            lblUpdateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UpdateChart();
            PushToDetailForms(t, h, p);
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
                double minTH = Math.Floor(Math.Min(ts.Min(), hs.Min()) - 5);
                double maxTH = Math.Ceiling(Math.Max(ts.Max(), hs.Max()) + 5);
                chart1.ChartAreas["MainArea"].AxisY.Minimum = Math.Max(-50, minTH);
                chart1.ChartAreas["MainArea"].AxisY.Maximum = Math.Min(120, maxTH);
                double minP = Math.Floor(ps.Min() - 1);
                double maxP = Math.Ceiling(ps.Max() + 1);
                chart1.ChartAreas["MainArea"].AxisY2.Minimum = Math.Max(85, minP);
                chart1.ChartAreas["MainArea"].AxisY2.Maximum = Math.Min(115, maxP);
                chart1.ChartAreas["MainArea"].AxisY2.Interval = Math.Max(1, Math.Ceiling((maxP - minP) / 4));
                chart1.ChartAreas["MainArea"].RecalculateAxesScale();
            }
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
            string msg = alarm ? string.Join(";", list) : "";
            lastAlarmMsg = msg;
            ApiService.UpdateAlarmMsg(msg);
            if (alarm)
            {
                ApiService.IncrementAlarmCount();
                if ((DateTime.Now - lastAlarmSound).TotalSeconds > 60)
                {
                    lastAlarmSound = DateTime.Now;
                    this.BeginInvoke(new Action(() => PlayBeep()));
                }
                int ver = connectVersion;
                this.BeginInvoke(new Action(() =>
                { if (ver == connectVersion) { lblStatus.Text = "报警: " + msg; lblStatus.ForeColor = Color.Red; } }));
                LogAlarmToFile(msg);
            }
        }

        // ==================== 数据记录 ====================
        private void InitLogFile() { }
        private void LogDataToFile(float t, float h, float p) { logService.LogData(t, h, p); }

        private string GetDbPath()
        {
            string dbPath = Path.Combine(Application.StartupPath, "TempHumidityData.db");
            if (!File.Exists(dbPath))
            {
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\TempHumidityData.db");
                if (File.Exists(templatePath))
                    File.Copy(templatePath, dbPath);
            }
            return dbPath;
        }

        /// <summary>从 modbus_config.json 读取 cloud_url 配置</summary>
        private string ReadCloudUrl(string configPath)
        {
            if (!File.Exists(configPath)) return null;
            string json = File.ReadAllText(configPath, Encoding.UTF8);
            // 简单手动解析: "cloud_url": "http://..."
            int idx = json.IndexOf("\"cloud_url\"");
            if (idx < 0) return null;
            idx = json.IndexOf('"', idx + 12);
            if (idx < 0) return null;
            int end = json.IndexOf('"', idx + 1);
            if (end < 0) return null;
            return json.Substring(idx + 1, end - idx - 1);
        }

        private void SaveToDatabase(float t, float h, float p)
        {
            try { dbService.SaveReading(t, h, p, readModeIndex, isSimMode, lastAlarmMsg); }
            catch (Exception ex) { LogError("数据库写入失败: " + ex.Message); }
        }

        private void LogAlarmToFile(string msg) { logService.LogAlarm(msg); }
        private void LogError(string msg) { logService.LogError(msg); }

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
        private void btnClearChart_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清除全部数据（图表+统计+队列）？", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                ClearAllData();
        }

        private void ClearAllData()
        {
            tempQueue.Clear(); humiQueue.Clear(); pressureQueue.Clear(); timeQueue.Clear(); ClearStats();
            chart1.Series["温度"].Points.Clear(); chart1.Series["湿度"].Points.Clear(); chart1.Series["气压"].Points.Clear();
            chart1.Series["温度"].Points.AddXY(1, 0);
            chart1.Series["湿度"].Points.AddXY(1, 0);
            chart1.Series["气压"].Points.AddXY(1, 0);
            chart1.ChartAreas["MainArea"].AxisY.Minimum = -10;
            chart1.ChartAreas["MainArea"].AxisY.Maximum = 100;
            chart1.ChartAreas["MainArea"].AxisY2.Minimum = 90;
            chart1.ChartAreas["MainArea"].AxisY2.Maximum = 110;
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

        private void PushToDetailForms(float t, float h, float p)
        {
            tempDetailForm?.PushData(t);
            humiDetailForm?.PushData(h);
            pressureDetailForm?.PushData(p);
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
        {
            alarmEnabled = chkEnableAlarm.Checked;
            Settings.Default.EnableAlarm = alarmEnabled; Settings.Default.Save();
            ApiService.UpdateThresholds(alarmTempH, alarmTempL, alarmHumiH, alarmHumiL, alarmPressH, alarmPressL, alarmEnabled);
        }

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
            ApiService.UpdateThresholds(alarmTempH, alarmTempL, alarmHumiH, alarmHumiL, alarmPressH, alarmPressL, alarmEnabled);
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.ShowBalloonTip(2000, "温湿度监控", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void trayShow_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void trayExit_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
            SaveAllSettings();
            if (isComOpen) closeComPort();
            try { apiService?.Shutdown(); } catch { }
            Application.Exit();
        }

        private void trayTempDetail_Click(object sender, EventArgs e) { ShowTempDetail(); }
        private void trayHumiDetail_Click(object sender, EventArgs e) { ShowHumiDetail(); }
        private void trayPressureDetail_Click(object sender, EventArgs e) { ShowPressureDetail(); }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }
            SaveAllSettings(); if (isComOpen) closeComPort();
            try { apiService?.Shutdown(); } catch { }
        }

        // ==================== 辅助 ====================
        private void PlayBeep()
        {
            try
            {
                int rate = 8000, freq = 800, dur = 300;
                int samples = rate * dur / 1000;
                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                    bw.Write(36 + samples);
                    bw.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                    bw.Write(16); bw.Write((short)1); bw.Write((short)1);
                    bw.Write(rate); bw.Write(rate); bw.Write((short)1); bw.Write((short)8);
                    bw.Write(new char[] { 'd', 'a', 't', 'a' });
                    bw.Write(samples);
                    for (int i = 0; i < samples; i++)
                        bw.Write((byte)(128 + 127 * Math.Sin(2 * Math.PI * freq * i / rate)));
                    bw.Flush(); ms.Position = 0;
                    new System.Media.SoundPlayer(ms).PlaySync();
                }
            }
            catch { }
        }

        private void ShowTip(string msg) { MessageBox.Show(msg, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); }

        private void UpdateStatus()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => { tsslError.Text = "错误: " + nError; tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive; }));
            else { tsslError.Text = "错误: " + nError; tsslSend.Text = "发送: " + nSend; tsslRecv.Text = "接收: " + nReceive; }
            ApiService.UpdateCounters(nSend, nReceive, nError);
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
                var dt = dbService.QueryHistory(dtpStart.Value, dtpEnd.Value, chkAlarmOnly.Checked);
                dgvHistory.DataSource = dt;
                lblStatus.Text = string.Format("查询到 {0} 条历史记录", dgvHistory.RowCount);
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex) { lblStatus.Text = "查询失败: " + ex.Message; lblStatus.ForeColor = Color.Red; }
        }

        private void btnCleanDB_Click(object sender, EventArgs e)
        {
            int days = (int)nudRetainDays.Value;
            if (MessageBox.Show(string.Format("将删除 {0} 天前的所有数据，确认？", days), "清理确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                try { dbService.CleanOldData(days); ShowTip("清理完成"); }
                catch (Exception ex) { ShowTip("清理失败: " + ex.Message); }
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
