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
using Timer = System.Windows.Forms.Timer; // ใช้ alias เพื่อไม่ปะทะกับ System.Threading

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    public partial class UC_Graph_Data_Set_2 : UserControl
    {
        // Timer สำหรับอัปเดต Label + กราฟ real-time
        private Timer updateTimer;
        // Timer สำหรับ Debounce Scroll (ลดการ redraw ถี่เกินไปขณะลาก scrollbar)
        private Timer scrollDebounceTimer;
        // ค่าล่าสุดของ ScrollBar
        private int lastScrollValue = 0;

        // ระบุ Period (1,5,10)
        private static string savedPeriod = "1 min";
        #nullable enable
        // ตัวแปรสำหรับ ScrollBar ภายนอก (hScrollBar_Graph2)
        private double chartXMin = 0;
        private double chartXMax = 0;
        private double chartXRange = 0;
        private double scrollbarScale = 1000; // ความละเอียด

        // autoScroll = true => ติดขอบขวา (real-time)
        private bool autoScroll = true;

        // ป้องกันการอัปเดตซ้อน
        private bool isUpdatingChart = false;

        public UC_Graph_Data_Set_2()
        {
            InitializeComponent();

            // สมัคร Event Handler
            But_GoHome2.Click += But_GoHome2_Click;
            combo_Graph2.SelectedIndexChanged += combo_Graph2_SelectedIndexChanged;
            dateTime_Graph2.ValueChanged += dateTime_Graph2_ValueChanged;
            hScrollBar_Graph2.Scroll += hScrollBar_Graph2_Scroll;

            InitializeChart();
            EnableDoubleBuffering(chart2);

            // ตั้ง Timer สำหรับ Debounce Scroll
            scrollDebounceTimer = new Timer();
            scrollDebounceTimer.Interval = 50; // 50 ms
            scrollDebounceTimer.Tick += scrollDebounceTimer_Tick;

            // โหลดข้อมูลชุด 2 (วันนี้ ถ้า DateTimePicker เป็นวันนี้)
            LoadHistoricalData();

            // Timer update real-time
            updateTimer = new Timer();
            updateTimer.Interval = 500;
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            SetupPeriodComboItems();
        }

        // เปิด DoubleBuffer ให้ Chart เพื่อลดการกระพริบ
        private void EnableDoubleBuffering(Control ctrl)
        {
            try
            {
                var pi = ctrl.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pi != null)
                    pi.SetValue(ctrl, true, null);
            }
            catch { }
        }

        private void InitializeChart()
        {
            if (chart2 == null) return;

            chart2.Series.Clear();

            var seriesTR2 = new Series("Series_TR2")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                XValueType = ChartValueType.DateTime
            };
            var seriesTRTJ2 = new Series("Series_TR_TJ2")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                XValueType = ChartValueType.DateTime
            };
            var seriesTJ2 = new Series("Series_TJ2")
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                XValueType = ChartValueType.DateTime
            };

            chart2.Series.Add(seriesTR2);
            chart2.Series.Add(seriesTRTJ2);
            chart2.Series.Add(seriesTJ2);

            ChartArea area = chart2.ChartAreas[0];
            area.AxisX.LabelStyle.Format = "HH:mm:ss";
            area.AxisX.IntervalType = DateTimeIntervalType.Seconds;
            area.AxisX.Interval = 10;
            // ปิด ScrollBar ภายใน
            area.AxisX.ScrollBar.Enabled = false;
        }

        private void SetupPeriodComboItems()
        {
            combo_Graph2.Items.Clear();
            combo_Graph2.Items.Add("1 min");
            combo_Graph2.Items.Add("5 min");
            combo_Graph2.Items.Add("10 min");

            if (combo_Graph2.Items.Contains(savedPeriod))
                combo_Graph2.SelectedItem = savedPeriod;
            else
                combo_Graph2.SelectedIndex = 0;
        }

        // เรียกหลังใส่ data ลง Chart => อัปเดต min/max => ScrollBar => Zoom
        private void UpdateChartBoundAndScrollBar()
        {
            var s = chart2.Series["Series_TR2"];
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

        // คำนวณ chartXRange ตาม Period => ตั้ง ScrollBar => ถ้า autoScroll => ไปขวาสุด
        private void SetupScrollBarAndZoom()
        {
            if (chartXMax <= chartXMin)
            {
                hScrollBar_Graph2.Value = 0;
                hScrollBar_Graph2.Maximum = 0;
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

            int oldVal = hScrollBar_Graph2.Value;

            hScrollBar_Graph2.Minimum = 0;
            hScrollBar_Graph2.Maximum = scrollMax;
            hScrollBar_Graph2.LargeChange = 1;
            hScrollBar_Graph2.SmallChange = 1;

            // ถ้า autoScroll => ไปขวาสุด
            if (autoScroll)
                oldVal = scrollMax;
            if (oldVal > scrollMax)
                oldVal = scrollMax;
            hScrollBar_Graph2.Value = oldVal;

            double offset = oldVal / scrollbarScale;
            double newMin = chartXMin + offset;
            double newMax = newMin + chartXRange;
            if (newMax > chartXMax)
            {
                newMax = chartXMax;
                newMin = newMax - chartXRange;
                if (newMin < chartXMin)
                    newMin = chartXMin;
            }

            // ลดการ flicker ด้วย BeginInit/EndInit + Suspend/ResumeLayout
            chart2.BeginInit();
            chart2.SuspendLayout();
            chart2.ChartAreas[0].AxisX.ScaleView.Zoom(newMin, newMax);
            chart2.ResumeLayout(false);
            chart2.EndInit();
        }

        // ScrollBar Event + Debounce
        private void hScrollBar_Graph2_Scroll(object sender, ScrollEventArgs e)
        {
            lastScrollValue = e.NewValue;

            // ถ้า Value < Maximum => user เลื่อน => autoScroll=false
            if (e.NewValue < hScrollBar_Graph2.Maximum)
                autoScroll = false;
            else if (e.NewValue == hScrollBar_Graph2.Maximum)
                autoScroll = true;

            // EndScroll => อัปเดตทันที
            if (e.Type == ScrollEventType.EndScroll)
            {
                scrollDebounceTimer.Stop();
                UpdateZoomFromScroll();
            }
            else
            {
                // ThumbTrack หรือ increment => debounce 50ms
                scrollDebounceTimer.Stop();
                scrollDebounceTimer.Interval = 50;
                scrollDebounceTimer.Start();
            }
        }

        private void scrollDebounceTimer_Tick(object sender, EventArgs e)
        {
            scrollDebounceTimer.Stop();
            UpdateZoomFromScroll();
        }

        private void UpdateZoomFromScroll()
        {
            if (chartXMax <= chartXMin || chartXRange <= 0)
                return;

            double offset = lastScrollValue / scrollbarScale;
            double newMin = chartXMin + offset;
            double newMax = newMin + chartXRange;
            if (newMax > chartXMax)
            {
                newMax = chartXMax;
                newMin = newMax - chartXRange;
                if (newMin < chartXMin)
                    newMin = chartXMin;
            }

            chart2.BeginInit();
            chart2.SuspendLayout();
            chart2.ChartAreas[0].AxisX.ScaleView.Zoom(newMin, newMax);
            chart2.ResumeLayout(false);
            chart2.EndInit();
        }

        // โหลดข้อมูล real-time (เฉพาะวันนี้)
        private void LoadHistoricalData()
        {
            if (chart2 == null) return;
            if (dateTime_Graph2.Value.Date != DateTime.Today)
                return;

            chart2.Series["Series_TR2"].Points.Clear();
            chart2.Series["Series_TR_TJ2"].Points.Clear();
            chart2.Series["Series_TJ2"].Points.Clear();

            // ดึงจาก GraphDataStore2
            foreach (var p in GraphDataStore2.DataPoints)
            {
                chart2.Series["Series_TR2"].Points.AddXY(p.Timestamp, p.TR2);
                chart2.Series["Series_TR_TJ2"].Points.AddXY(p.Timestamp, p.TR_TJ2);
                chart2.Series["Series_TJ2"].Points.AddXY(p.Timestamp, p.TJ2);
            }
            chart2.ResetAutoValues();
            UpdateChartBoundAndScrollBar();
        }

        // โหลด CSV ย้อนหลังสำหรับชุด 2
        private void ReloadHistoryData()
        {
            if (chart2 == null) return;
            isUpdatingChart = true;

            chart2.Series["Series_TR2"].Points.Clear();
            chart2.Series["Series_TR_TJ2"].Points.Clear();
            chart2.Series["Series_TJ2"].Points.Clear();

            DateTime selDate = dateTime_Graph2.Value.Date;
            // สำหรับ DataLogger2 => "data_log_set2_yyyy-MM-dd.csv" (ปรับตามโปรแกรมจริง)
            string filePath = $"data_log_set2_{selDate:yyyy-MM-dd}.csv";
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
                if (!double.TryParse(parts[1], out double tr2))
                    continue;
                if (!double.TryParse(parts[2], out double tr_tj2))
                    continue;
                if (!double.TryParse(parts[3], out double tj2))
                    continue;

                chart2.Series["Series_TR2"].Points.AddXY(timestamp, tr2);
                chart2.Series["Series_TR_TJ2"].Points.AddXY(timestamp, tr_tj2);
                chart2.Series["Series_TJ2"].Points.AddXY(timestamp, tj2);
            }
            chart2.ResetAutoValues();
            isUpdatingChart = false;
            UpdateChartBoundAndScrollBar();
        }

        // Timer อัปเดต real-time
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (dateTime_Graph2.Value.Date != DateTime.Today)
                return;

            try
            {
                double[] vals = SerialPortManager.Instance.CurrentValues;
                if (vals != null && vals.Length >= 9)
                {
                    double tr2 = vals[7];
                    double tj2 = vals[8];
                    double diff = tr2 - tj2;
                    DateTime now = DateTime.Now;

                    // อัปเดต Label
                    label_TR2.Text = tr2.ToString("F2");
                    label_TJ2.Text = tj2.ToString("F2");
                    label_TR_TJ2.Text = diff.ToString("F2");

                    // Debug Output
                    Debug.WriteLine($"[UC_Graph2] UpdateTimer_Tick => TR2={tr2:F2}, TJ2={tj2:F2}, Δ={diff:F2}");

                    // บันทึกและ plot ข้อมูล
                    DataLogger2.LogData_2(tr2, diff, tj2);
                    GraphDataStore2.AddData(tr2, diff, tj2);

                    chart2.Series["Series_TR2"].Points.AddXY(now, tr2);
                    chart2.Series["Series_TR_TJ2"].Points.AddXY(now, diff);
                    chart2.Series["Series_TJ2"].Points.AddXY(now, tj2);
                    chart2.ResetAutoValues();

                    double newX = chart2.Series["Series_TR2"].Points.Last().XValue;
                    if (newX > chartXMax)
                        chartXMax = newX;

                    if (autoScroll && chartXRange > 0 && chartXMax > chartXMin)
                    {
                        SetupScrollBarAndZoom();
                    }
                    else
                    {
                        int oldVal = hScrollBar_Graph2.Value;
                        SetupScrollBarAndZoom();
                        if (oldVal > hScrollBar_Graph2.Maximum)
                            oldVal = hScrollBar_Graph2.Maximum;
                        hScrollBar_Graph2.Value = oldVal;

                        double offset = oldVal / scrollbarScale;
                        double newMin = chartXMin + offset;
                        double newMax = newMin + chartXRange;
                        if (newMax > chartXMax)
                        {
                            newMax = chartXMax;
                            newMin = newMax - chartXRange;
                            if (newMin < chartXMin)
                                newMin = chartXMin;
                        }

                        chart2.BeginInit();
                        chart2.SuspendLayout();
                        chart2.ChartAreas[0].AxisX.ScaleView.Zoom(newMin, newMin + chartXRange);
                        chart2.ResumeLayout(false);
                        chart2.EndInit();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }


        // เมื่อเปลี่ยน Period 
        private void combo_Graph2_SelectedIndexChanged(object sender, EventArgs e)
        {
            savedPeriod = combo_Graph2.SelectedItem.ToString();
            SetupScrollBarAndZoom();
        }

        // เมื่อเปลี่ยน DateTimePicker => โหลดข้อมูลย้อนหลัง ถ้าไม่ใช่วันนี้
        private void dateTime_Graph2_ValueChanged(object sender, EventArgs e)
        {
            if (dateTime_Graph2.Value.Date != DateTime.Today)
                ReloadHistoryData();
            else
                LoadHistoricalData();
        }

        // ปุ่มกลับหน้า Home
        private void But_GoHome2_Click(object? sender, EventArgs e)
        {
            Main_Form1? mainForm = this.ParentForm as Main_Form1;
            if (mainForm != null)
                mainForm.LoadUserControl(new UC_CONTROL_SET_2());
            else
                MessageBox.Show("ไม่พบ Main_Form1");
        }
    }
}
