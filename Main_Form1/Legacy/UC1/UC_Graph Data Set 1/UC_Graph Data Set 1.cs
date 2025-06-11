using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection; // สำหรับ DoubleBuffered
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting; // สำหรับ Chart
using Timer = System.Windows.Forms.Timer;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    public partial class UC_Graph_Data_Set_1 : UserControl
    {
        // Timer สำหรับอัปเดต Label + กราฟ real-time
        private Timer updateTimer;
        // Timer สำหรับ Debounce Scroll เมื่อเลื่อนค้าง (ลด flicker)
        private Timer scrollDebounceTimer;
        // ค่า Scroll ล่าสุด (int)
        private int lastScrollValue = 0;

        // เก็บ Period (1,5,10 นาที)
        private static string savedPeriod = "1 min";

        // ScrollBar ภายนอก
        private double chartXMin = 0;  // ค่าต่ำสุด (OADate) ของข้อมูล
        private double chartXMax = 0;  // ค่าสูงสุด (OADate) ของข้อมูล
        private double chartXRange = 0; // ช่วงเวลาที่จะแสดง (ตาม Period)
        private double scrollbarScale = 1000; // เพิ่มความละเอียดการเลื่อน

        // autoScroll = true => ติดขอบขวา real-time
        private bool autoScroll = true;

        private bool isUpdatingChart = false;

        public UC_Graph_Data_Set_1()
        {
            InitializeComponent();

            But_GoHome1.Click += But_GoHome1_Click;
            combo_Graph1.SelectedIndexChanged += combo_Graph1_SelectedIndexChanged;
            dateTime_Graph1.ValueChanged += dateTime_Graph1_ValueChanged;

            // ScrollBar event
            hScrollBar_Graph1.Scroll += hScrollBar_Graph1_Scroll;

            InitializeChart();
            EnableDoubleBuffering(chart1);

            // Debounce Timer สำหรับ ScrollBar
            scrollDebounceTimer = new Timer();
            scrollDebounceTimer.Interval = 50;
            scrollDebounceTimer.Tick += scrollDebounceTimer_Tick;

            // โหลดข้อมูลวันนี้ (ถ้าวันนี้)
            LoadHistoricalData();

            updateTimer = new Timer();
            updateTimer.Interval = 500;
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            SetupPeriodComboItems();
        }

        // เปิด Double Buffer เพื่อลด flicker
        private void EnableDoubleBuffering(Control ctrl)
        {
            try
            {
                var pi = ctrl.GetType().GetProperty("DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (pi != null) pi.SetValue(ctrl, true, null);
            }
            catch { /* ignore */ }
        }

        private void InitializeChart()
        {
            if (chart1 == null) return;

            chart1.Series.Clear();

            var seriesTR1 = new Series("Series_TR1")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                XValueType = ChartValueType.DateTime
            };
            var seriesTRTJ1 = new Series("Series_TR_TJ1")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                XValueType = ChartValueType.DateTime
            };
            var seriesTJ1 = new Series("Series_TJ1")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                XValueType = ChartValueType.DateTime
            };

            chart1.Series.Add(seriesTR1);
            chart1.Series.Add(seriesTRTJ1);
            chart1.Series.Add(seriesTJ1);

            ChartArea area = chart1.ChartAreas[0];
            area.AxisX.LabelStyle.Format = "HH:mm:ss";
            area.AxisX.IntervalType = DateTimeIntervalType.Seconds;
            area.AxisX.Interval = 10;
            area.AxisX.ScrollBar.Enabled = false;
        }

        private void SetupPeriodComboItems()
        {
            combo_Graph1.Items.Clear();
            combo_Graph1.Items.Add("1 min");
            combo_Graph1.Items.Add("5 min");
            combo_Graph1.Items.Add("10 min");

            if (combo_Graph1.Items.Contains(savedPeriod))
                combo_Graph1.SelectedItem = savedPeriod;
            else
                combo_Graph1.SelectedIndex = 0;
        }

        // อัปเดตขอบเขต (chartXMin, chartXMax) => ScrollBar => Zoom
        private void UpdateChartBoundAndScrollBar()
        {
            var s = chart1.Series["Series_TR1"];
            if (s.Points.Count > 0)
            {
                chartXMin = s.Points.First().XValue;
                chartXMax = s.Points.Last().XValue;
                if (chartXMax < chartXMin)
                    chartXMax = chartXMin;
            }
            else
            {
                chartXMin = 0;
                chartXMax = 0;
            }
            SetupScrollBarAndZoom();
        }

        private void SetupScrollBarAndZoom()
        {
            if (chartXMax <= chartXMin)
            {
                hScrollBar_Graph1.Value = 0;
                hScrollBar_Graph1.Maximum = 0;
                chartXRange = 0;
                return;
            }

            double totalRange = chartXMax - chartXMin;
            double minutes = 1;
            if (savedPeriod == "5 min") minutes = 5;
            else if (savedPeriod == "10 min") minutes = 10;

            chartXRange = TimeSpan.FromMinutes(minutes).TotalDays;
            if (chartXRange > totalRange)
                chartXRange = totalRange;

            double maxScroll = totalRange - chartXRange;
            if (maxScroll < 0) maxScroll = 0;

            int scrollMax = (int)Math.Ceiling(maxScroll * scrollbarScale);
            if (scrollMax < 1)
                scrollMax = 1;

            int oldVal = hScrollBar_Graph1.Value;

            hScrollBar_Graph1.Minimum = 0;
            hScrollBar_Graph1.Maximum = scrollMax;
            hScrollBar_Graph1.LargeChange = 1;
            hScrollBar_Graph1.SmallChange = 1;

            // ถ้า autoScroll => เลื่อนไปขวาสุด
            if (autoScroll)
                oldVal = scrollMax;
            if (oldVal > scrollMax)
                oldVal = scrollMax;

            hScrollBar_Graph1.Value = oldVal;

            double offset = oldVal / scrollbarScale;
            double newMin = chartXMin + offset;
            double newMax = newMin + chartXRange;

            // Clamp: ถ้า newMin < chartXMin => ปรับ
            if (newMin < chartXMin)
            {
                newMin = chartXMin;
                newMax = newMin + chartXRange;
            }
            if (newMax > chartXMax)
            {
                newMax = chartXMax;
                newMin = newMax - chartXRange;
                if (newMin < chartXMin)
                    newMin = chartXMin;
            }

            // ใช้ BeginInit/EndInit + Suspend/Resume ลด flicker
            chart1.BeginInit();
            chart1.SuspendLayout();
            chart1.ChartAreas[0].AxisX.ScaleView.Zoom(newMin, newMax);
            chart1.ResumeLayout(false);
            chart1.EndInit();
        }

        // เมื่อ ScrollBar มี Scroll Event => Debounce + ตรวจ EndScroll
        private void hScrollBar_Graph1_Scroll(object sender, ScrollEventArgs e)
        {
            lastScrollValue = e.NewValue;

            // ถ้า Value < max => user เลื่อนเอง => autoScroll=false
            if (e.NewValue < hScrollBar_Graph1.Maximum)
                autoScroll = false;
            else if (e.NewValue == hScrollBar_Graph1.Maximum)
                autoScroll = true;

            // EndScroll => อัปเดตทันที
            if (e.Type == ScrollEventType.EndScroll)
            {
                scrollDebounceTimer.Stop();
                UpdateZoomFromScroll();
            }
            else
            {
                // ThumbTrack / increment => debounce
                scrollDebounceTimer.Stop();
                scrollDebounceTimer.Interval = 50;
                scrollDebounceTimer.Start();
            }
        }

        // เมื่อ debounce timer หมด => UpdateZoomFromScroll
        private void scrollDebounceTimer_Tick(object sender, EventArgs e)
        {
            scrollDebounceTimer.Stop();
            UpdateZoomFromScroll();
        }

        // อัปเดต Zoom จาก lastScrollValue
        private void UpdateZoomFromScroll()
        {
            if (chartXMax <= chartXMin || chartXRange <= 0)
                return;

            double offset = lastScrollValue / scrollbarScale;
            double newMin = chartXMin + offset;
            double newMax = newMin + chartXRange;

            // Clamp
            if (newMin < chartXMin)
            {
                newMin = chartXMin;
                newMax = newMin + chartXRange;
            }
            if (newMax > chartXMax)
            {
                newMax = chartXMax;
                newMin = newMax - chartXRange;
                if (newMin < chartXMin)
                    newMin = chartXMin;
            }

            chart1.BeginInit();
            chart1.SuspendLayout();
            chart1.ChartAreas[0].AxisX.ScaleView.Zoom(newMin, newMax);
            chart1.ResumeLayout(false);
            chart1.EndInit();
        }

        // Load data real-time (ถ้าวันที่เป็นวันนี้)
        private void LoadHistoricalData()
        {
            if (chart1 == null) return;
            if (dateTime_Graph1.Value.Date != DateTime.Today)
                return;

            chart1.Series["Series_TR1"].Points.Clear();
            chart1.Series["Series_TR_TJ1"].Points.Clear();
            chart1.Series["Series_TJ1"].Points.Clear();

            foreach (var p in GraphDataStore1.DataPoints)
            {
                chart1.Series["Series_TR1"].Points.AddXY(p.Timestamp, p.TR1);
                chart1.Series["Series_TR_TJ1"].Points.AddXY(p.Timestamp, p.TR_TJ1);
                chart1.Series["Series_TJ1"].Points.AddXY(p.Timestamp, p.TJ1);
            }
            chart1.ResetAutoValues();
            UpdateChartBoundAndScrollBar();
        }

        // Load data ย้อนหลัง (ไม่ใช่วันนี้ => CSV)
        private void ReloadHistoryData()
        {
            if (chart1 == null) return;
            isUpdatingChart = true;

            chart1.Series["Series_TR1"].Points.Clear();
            chart1.Series["Series_TR_TJ1"].Points.Clear();
            chart1.Series["Series_TJ1"].Points.Clear();

            DateTime selDate = dateTime_Graph1.Value.Date;
            string filePath = $"data_log_set1_{selDate:yyyy-MM-dd}.csv";
            if (!File.Exists(filePath))
            {
                isUpdatingChart = false;
                UpdateChartBoundAndScrollBar();
                return;
            }

            var lines = File.ReadAllLines(filePath);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var parts = line.Split(',');
                if (parts.Length < 4)
                    continue;
                if (!DateTime.TryParse(parts[0], out DateTime timestamp))
                    continue;
                if (!double.TryParse(parts[1], out double tr1))
                    continue;
                if (!double.TryParse(parts[2], out double tr_tj1))
                    continue;
                if (!double.TryParse(parts[3], out double tj1))
                    continue;

                chart1.Series["Series_TR1"].Points.AddXY(timestamp, tr1);
                chart1.Series["Series_TR_TJ1"].Points.AddXY(timestamp, tr_tj1);
                chart1.Series["Series_TJ1"].Points.AddXY(timestamp, tj1);
            }
            chart1.ResetAutoValues();
            isUpdatingChart = false;
            UpdateChartBoundAndScrollBar();
        }

        // UpdateTimer => เพิ่มจุด real-time ทุก 500ms (วันนี้)
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // ถ้าไม่ใช่วันนี้ ข้าม
            if (dateTime_Graph1.Value.Date != DateTime.Today)
                return;

            try
            {
                double[] vals = SerialPortManager.Instance.CurrentValues;
                // ต้องมีอย่างน้อย 2 ค่า: TR1, TJ1
                if (vals != null && vals.Length >= 2)
                {
                    double tr1 = vals[0];
                    double tj1 = vals[1];
                    double tr_tj1 = tr1 - tj1;
                    DateTime now = DateTime.Now;

                    // อัปเดต Label
                    label_TR1.Text = tr1.ToString("F2");
                    label_TJ1.Text = tj1.ToString("F2");
                    label_TR_TJ1.Text = tr_tj1.ToString("F2");

                    // Debug Output
                    Debug.WriteLine($"[UC_Graph1] UpdateTimer_Tick => TR1={tr1:F2}, TJ1={tj1:F2}, Δ={tr_tj1:F2}");

                    // บันทึกและ plot ข้อมูล
                    DataLogger1.LogData(tr1, tr_tj1, tj1);
                    GraphDataStore1.AddData(tr1, tr_tj1, tj1);

                    chart1.Series["Series_TR1"].Points.AddXY(now, tr1);
                    chart1.Series["Series_TR_TJ1"].Points.AddXY(now, tr_tj1);
                    chart1.Series["Series_TJ1"].Points.AddXY(now, tj1);

                    chart1.ResetAutoValues();

                    double newX = chart1.Series["Series_TR1"].Points.Last().XValue;
                    if (newX > chartXMax)
                        chartXMax = newX;

                    if (autoScroll && chartXRange > 0 && chartXMax > chartXMin)
                        SetupScrollBarAndZoom();
                    else
                    {
                        int oldVal = hScrollBar_Graph1.Value;
                        SetupScrollBarAndZoom();
                        if (oldVal > hScrollBar_Graph1.Maximum)
                            oldVal = hScrollBar_Graph1.Maximum;
                        hScrollBar_Graph1.Value = oldVal;

                        double offset = oldVal / scrollbarScale;
                        double newMin = chartXMin + offset;
                        if (newMin < chartXMin)
                            newMin = chartXMin;

                        chart1.BeginInit();
                        chart1.SuspendLayout();
                        chart1.ChartAreas[0].AxisX.ScaleView.Zoom(newMin, newMin + chartXRange);
                        chart1.ResumeLayout(false);
                        chart1.EndInit();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UpdateTimer_Tick error: " + ex.Message);
            }
        }

        private void combo_Graph1_SelectedIndexChanged(object sender, EventArgs e)
        {
            savedPeriod = combo_Graph1.SelectedItem.ToString();
            SetupScrollBarAndZoom();
        }

        private void dateTime_Graph1_ValueChanged(object sender, EventArgs e)
        {
            if (dateTime_Graph1.Value.Date != DateTime.Today)
                ReloadHistoryData();
            else
                LoadHistoricalData();
        }

        private void But_GoHome1_Click(object? sender, EventArgs e)
        {
            Main_Form1? mainForm = this.ParentForm as Main_Form1;
            if (mainForm != null)
                mainForm.LoadUserControl(new UC_CONTROL_SET_1());
            else
                MessageBox.Show("ไม่พบ Main_Form1");
        }
    }
}
