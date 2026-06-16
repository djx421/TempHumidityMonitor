using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TempHumidityMonitor
{
    /// <summary>
    /// 单个传感器数据的独立图表子窗体，支持手动调整 Y 轴分度。
    /// 通过事件接收主窗体推送的实时数据。
    /// </summary>
    public partial class SensorDetailForm : Form
    {
        // ==================== 控件声明 ====================
        private Chart detailChart;
        private Series dataSeries;
        private ChartArea chartArea;
        private NumericUpDown nudYMin, nudYMax;
        private Button btnApplyRange, btnAutoRange;
        private Label lblRange, lblYMinLabel, lblYMaxLabel;
        private Button btnClear;
        private Label lblLatest;

        // ==================== 数据字段 ====================
        private readonly Queue<double> dataQueue = new Queue<double>();
        private readonly Queue<int> indexQueue = new Queue<int>();
        private int pointIndex = 0;
        private int maxPoints;
        private readonly string sensorName;
        private readonly string unit;
        private readonly Color seriesColor;
        private readonly Color titleColor;
        private bool autoRange = true;
        private double currentValue = 0;

        // ==================== 属性 ====================
        public int MaxChartPoints
        {
            get => maxPoints;
            set { maxPoints = Math.Max(10, Math.Min(2000, value)); TrimQueue(); }
        }

        // ==================== 构造 ====================
        public SensorDetailForm(string sensorName, string unit, Color seriesColor, int maxPoints = 500)
        {
            this.sensorName = sensorName;
            this.unit = unit;
            this.seriesColor = seriesColor;
            this.titleColor = ControlPaint.Light(seriesColor, 0.3f);
            this.maxPoints = maxPoints;

            InitializeForm();
            InitializeChart();
            InitializeControls();
        }

        // ==================== 窗体初始化 ====================
        private void InitializeForm()
        {
            this.Text = sensorName + "实时曲线";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(500, 350);
            this.Icon = System.Drawing.SystemIcons.Information;
            this.FormClosing += SensorDetailForm_FormClosing;
            this.BackColor = Color.White;
        }

        private void InitializeChart()
        {
            detailChart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            chartArea = new ChartArea("DetailArea")
            {
                BackColor = Color.White,
                BorderColor = Color.LightGray,
                BorderDashStyle = ChartDashStyle.Solid
            };
            chartArea.AxisX.Title = "采样点序号";
            chartArea.AxisX.TitleFont = new Font("微软雅黑", 9F);
            chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(230, 230, 230);
            chartArea.AxisX.LabelStyle.Format = "0";
            chartArea.AxisX.Minimum = 0;
            chartArea.AxisX.Interval = 50;
            chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;

            chartArea.AxisY.Title = sensorName + " (" + unit + ")";
            chartArea.AxisY.TitleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            chartArea.AxisY.TitleForeColor = seriesColor;
            chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(230, 230, 230);
            chartArea.AxisY.LabelStyle.Format = "F1";
            chartArea.AxisY.Interval = 10;

            // 游标支持缩放
            chartArea.CursorX.IsUserEnabled = true;
            chartArea.CursorX.IsUserSelectionEnabled = true;
            chartArea.CursorX.AutoScroll = true;
            chartArea.AxisX.ScaleView.Zoomable = true;
            chartArea.AxisY.ScaleView.Zoomable = true;
            chartArea.AxisX.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
            chartArea.AxisX.ScrollBar.Size = 12;
            chartArea.AxisY.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
            chartArea.AxisY.ScrollBar.Size = 12;

            detailChart.ChartAreas.Add(chartArea);

            var legend = new Legend("Legend")
            {
                Docking = Docking.Top,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                IsTextAutoFit = false
            };
            detailChart.Legends.Add(legend);

            dataSeries = new Series(sensorName)
            {
                ChartType = SeriesChartType.Spline,
                Color = seriesColor,
                BorderWidth = 3,
                Legend = "Legend",
                LegendText = sensorName + " (" + unit + ")",
                MarkerColor = seriesColor,
                MarkerSize = 0,
                MarkerStyle = MarkerStyle.None,
                ToolTip = sensorName + ": #VAL{F1} " + unit,
                XValueType = ChartValueType.Int32,
                YValueType = ChartValueType.Double
            };
            detailChart.Series.Add(dataSeries);

            // 鼠标移动显示交线
            detailChart.MouseMove += DetailChart_MouseMove;

            this.Controls.Add(detailChart);
        }

        private void InitializeControls()
        {
            int topPanelH = 50;
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = topPanelH,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(8, 7, 8, 4)
            };

            // 最新值显示
            lblLatest = new Label
            {
                Text = sensorName + ": -- " + unit,
                Font = new Font("微软雅黑", 13F, FontStyle.Bold),
                ForeColor = seriesColor,
                AutoSize = true,
                Location = new Point(10, 8)
            };
            topPanel.Controls.Add(lblLatest);

            // Y轴下限
            nudYMin = new NumericUpDown
            {
                Location = new Point(300, 12),
                Width = 80,
                DecimalPlaces = 2,
                Minimum = -1000,
                Maximum = 10000,
                TextAlign = HorizontalAlignment.Center
            };
            lblYMinLabel = new Label
            {
                Text = "Y轴下限:",
                AutoSize = true,
                Location = new Point(225, 14),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Y轴上限
            nudYMax = new NumericUpDown
            {
                Location = new Point(500, 12),
                Width = 80,
                DecimalPlaces = 2,
                Minimum = -999,
                Maximum = 10001,
                TextAlign = HorizontalAlignment.Center
            };
            lblYMaxLabel = new Label
            {
                Text = "Y轴上限:",
                AutoSize = true,
                Location = new Point(425, 14),
                TextAlign = ContentAlignment.MiddleRight
            };

            btnApplyRange = new Button
            {
                Text = "应用",
                Location = new Point(590, 10),
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.SteelBlue,
                ForeColor = Color.White
            };
            btnApplyRange.Click += (s, e) => { autoRange = false; ApplyManualRange(); };

            btnAutoRange = new Button
            {
                Text = "自适应",
                Location = new Point(655, 10),
                Size = new Size(65, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGreen,
                ForeColor = Color.Black
            };
            btnAutoRange.Click += (s, e) => { autoRange = true; UpdateRange(); };

            btnClear = new Button
            {
                Text = "清除曲线",
                Location = new Point(730, 10),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightCoral,
                ForeColor = Color.Black
            };
            btnClear.Click += (s, e) => ClearData();

            topPanel.Controls.AddRange(new Control[] {
                nudYMin, nudYMax, lblYMinLabel, lblYMaxLabel,
                btnApplyRange, btnAutoRange, btnClear
            });

            detailChart.Top = topPanelH;
            detailChart.Height = this.ClientSize.Height - topPanelH;

            this.Controls.Add(topPanel);
            topPanel.BringToFront();
        }

        // ==================== 公开方法：主窗体推送数据 ====================
        public void PushData(double value)
        {
            if (this.IsDisposed) return;

            currentValue = value;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<double>(PushDataInternal), value);
            }
            else
            {
                PushDataInternal(value);
            }
        }

        private void PushDataInternal(double value)
        {
            pointIndex++;
            dataQueue.Enqueue(value);
            indexQueue.Enqueue(pointIndex);
            TrimQueue();

            // 更新最新值标签
            lblLatest.Text = string.Format("{0}: {1:F1} {2}", sensorName, value, unit);

            // 更新图表
            UpdateChart();

            // 自适应 Y 轴范围
            if (autoRange) UpdateRange();
        }

        private void TrimQueue()
        {
            while (dataQueue.Count > maxPoints)
            {
                dataQueue.Dequeue();
                indexQueue.Dequeue();
            }
        }

        private void ClearData()
        {
            dataQueue.Clear();
            indexQueue.Clear();
            pointIndex = 0;
            dataSeries.Points.Clear();
            lblLatest.Text = sensorName + ": -- " + unit;
        }

        // ==================== 图表更新 ====================
        private void UpdateChart()
        {
            dataSeries.Points.Clear();
            double[] values = dataQueue.ToArray();
            int[] indices = indexQueue.ToArray();

            for (int i = 0; i < values.Length; i++)
            {
                dataSeries.Points.AddXY(indices[i], values[i]);
            }

            // 滚动 X 轴视口
            if (indexQueue.Count > 0)
            {
                int start = Math.Max(0, pointIndex - maxPoints);
                chartArea.AxisX.Minimum = start;
                chartArea.AxisX.Maximum = Math.Max(10, pointIndex + 5);
            }
        }

        private void UpdateRange()
        {
            if (dataQueue.Count == 0) return;

            double min = dataQueue.Min();
            double max = dataQueue.Max();
            double range = max - min;

            // 扩展 10% 边距
            double margin = Math.Max(range * 0.1, 0.5);
            double newMin = Math.Floor((min - margin) * 10) / 10;
            double newMax = Math.Ceiling((max + margin) * 10) / 10;

            // 至少 5 个单位范围
            if (newMax - newMin < 5)
            {
                newMin = Math.Floor(min) - 2;
                newMax = Math.Ceiling(max) + 2;
            }

            // 避免最小值和最大值相等
            if (Math.Abs(newMax - newMin) < 0.1)
            {
                newMin -= 1;
                newMax += 1;
            }

            chartArea.AxisY.Minimum = newMin;
            chartArea.AxisY.Maximum = newMax;

            // 更新数值输入框显示当前范围
            nudYMin.Value = (decimal)newMin;
            nudYMax.Value = (decimal)newMax;

            // 自动间隔
            double fullRange = newMax - newMin;
            if (fullRange > 100) chartArea.AxisY.Interval = Math.Ceiling(fullRange / 10);
            else if (fullRange > 10) chartArea.AxisY.Interval = Math.Ceiling(fullRange / 8);
            else chartArea.AxisY.Interval = Math.Ceiling(fullRange / 5);
            if (chartArea.AxisY.Interval < 0.5) chartArea.AxisY.Interval = 0.5;
        }

        private void ApplyManualRange()
        {
            double min = (double)nudYMin.Value;
            double max = (double)nudYMax.Value;
            if (min >= max)
            {
                MessageBox.Show("Y轴下限必须小于上限", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            chartArea.AxisY.Minimum = min;
            chartArea.AxisY.Maximum = max;

            double fullRange = max - min;
            if (fullRange > 100) chartArea.AxisY.Interval = Math.Ceiling(fullRange / 10);
            else if (fullRange > 10) chartArea.AxisY.Interval = Math.Ceiling(fullRange / 8);
            else chartArea.AxisY.Interval = Math.Ceiling(fullRange / 5);
            if (chartArea.AxisY.Interval < 0.1) chartArea.AxisY.Interval = 0.1;

            // 更新自适应按钮状态
            autoRange = false;
        }

        // ==================== 交互 ====================
        private void DetailChart_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var hit = detailChart.HitTest(e.X, e.Y);
                if (hit.ChartElementType == ChartElementType.DataPoint ||
                    hit.PointIndex >= 0)
                {
                    var dp = dataSeries.Points[hit.PointIndex];
                    string tip = string.Format("{0}: {1:F2} {2}\n采样点: {3}",
                        sensorName, dp.YValues[0], unit, dp.XValue);
                    detailChart.Titles.Clear();
                    var title = new Title(tip, Docking.Top,
                        new Font("微软雅黑", 9F), Color.Black)
                    {
                        BackColor = Color.FromArgb(255, 255, 225),
                        BorderColor = Color.Gray,
                        BorderWidth = 1,
                        Visible = true
                    };
                    detailChart.Titles.Add(title);
                }
                else
                {
                    detailChart.Titles.Clear();
                }
            }
            catch { }
        }

        private void SensorDetailForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
}
