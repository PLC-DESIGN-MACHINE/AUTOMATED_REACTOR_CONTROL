using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics; // สำหรับ Debug.WriteLine()
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading; // สำหรับ Thread.Sleep
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using Timer = System.Windows.Forms.Timer; // ใช้ alias สำหรับ Windows Forms Timer

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    public partial class UC_PROGRAM_CONTROL_SET_2 : UserControl
    {
        // ------------------ ส่วนที่เกี่ยวกับ Lamp Status ------------------
       
                                                                        // -------------------------------------------------------------------
        private TextBox[] tempBoxes_2, rpmBoxes_2, dosingBoxes_2;
        private TextBox[] timeHrBoxes_2, timeMinBoxes_2;

        private static bool hasLoadedSettings_2 = false;
        private Timer updateTimer_2;
        private static Timer countdownTimer_2;
        private Timer settingsUpdateTimer_2 = new Timer();
        private static int previousCountdown_2 = -1;
        private static int globalTotalStepSeconds_2 = 0;
        private static DateTime lastCountdownTick_2;
        private DateTime lastSetpointUpdateTime_2 = DateTime.MinValue;
        private int totalStepSeconds_2 = 0;
        private Data_Set2 settings_2;
        private Timer thresholdCheckTimer_2;
        // ตัวแปรสำหรับโหมด Wait/Run
        private bool isWaitingForTemperature_2 = false;
        private bool isRunningCountdown_2 = false;
        private double targetTemperature_2 = 0;
        private bool useTemperatureTj_2 = false;
        private int activeStep_2 = 0;
        // สำหรับ Step 1
        private float Target2_Tr1;
        private float Target2_Tj1;
        private int Target2_RPM1;
        private float Target2_Dosing1;
        private int Target2_Hr1;
        private int Target2_Min1;

        // สำหรับ Step 2-8
        private float Target2_Tr2, Target2_Tj2;
        private float Target2_Tr3, Target2_Tj3;
        private float Target2_Tr4, Target2_Tj4;
        private float Target2_Tr5, Target2_Tj5;
        private float Target2_Tr6, Target2_Tj6;
        private float Target2_Tr7, Target2_Tj7;
        private float Target2_Tr8, Target2_Tj8;



        // Local state
        private int currentStep_2 = 0;
        private int remainingSeconds_2 = 0;
        private bool isStarted_2 = false;
        private bool isPaused_2 = false;
        private bool isNextStepProcessing_2 = false;
        private static List<int> completedSteps_2 = new List<int>();

        // ปุ่ม Emergency (ตรวจสอบให้มีอยู่ใน UI)
        private Button btnEmergency_2;

        // ประกาศ ErrorProvider สำหรับตรวจสอบ Input ใน TextBox
        private ErrorProvider errorProvider_2 = new ErrorProvider();

        private DeviceSettings_2 deviceSettings_2;
        private float temp_2;
        private string mode_2;
        private int rpm_2;
        private float dosing_2;
        private int firstStep_2;
        private int stepSec_2;

        private enum StepMode { WAIT, RUN }
        private StepMode currentMode_2 = StepMode.WAIT;
        private int totalStepSeconds2 = 0;

        private bool waitingForThreshold_2;
        private int waitStep_2;
        private enum StepState_2
        {
            Idle,
            WaitingForThreshold,
            CountingDown
        }

        // เพิ่มตัวแปรใหม่
        private StepState_2 currentStepState = StepState_2.Idle;

        // --------------------- ฟังก์ชันสำหรับ Lamp ---------------------

        private static readonly string[] persistedLampStatuses_2 =
     Enumerable.Repeat("Wait", 9).ToArray();   // index 0 ไม่ใช้

        public object DeviceSettings2 { get; private set; }

        private void UpdateLampStatus_2()
        {
            for (int i = 1; i <= 8; i++)
            {
                string status;
                if (completedSteps_2.Contains(i))
                    status = "Done";
                else if (i == currentStep_2)
                    status = (ProgramState_2.IsStarted_2 && !ProgramState_2.IsPaused_2) ? "Run" : "Wait";
                else
                    status = persistedLampStatuses_2[i] ?? "Wait";   // กัน null

                SetLampForStep_2(i, status);
            }
            SaveLampState_2();
        }

        private void UpdateTimerDisplay_2()
        {
            // คำนวณ TimeSpan จาก RemainingSeconds2
            TimeSpan remaining = TimeSpan.FromSeconds(ProgramState_2.RemainingSeconds_2);

            // หา Label สำหรับแสดงเวลาเหลือ (ปรับชื่อตามชื่อ Label จริงในฟอร์ม)
            var lblTimeLeft = Controls
                .Find($"label2_TimeLeft{currentStep_2}", true)
                .FirstOrDefault() as Label;
            if (lblTimeLeft != null)
            {
                lblTimeLeft.Text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }

            // ถ้ามี Label แสดงเวลา “ผ่านไป” (Elapsed) ก็สามารถเพิ่มได้เช่นกัน
            var lblTimePassed = Controls.Find($"label2_TimePassed{currentStep_2}", true).FirstOrDefault() as Label;
            if (lblTimePassed != null)
                lblTimePassed.Text = $"{(int)(totalStepSeconds_2 - ProgramState_2.RemainingSeconds_2) / 3600:D2}:...";
        }
        /// <summary>
        /// ควบคุมการทำงานของ timer ทั้งหมดแบบรวมศูนย์
        /// </summary>
        private void SetTimerState_2(StepState_2 newState)
        {
            // หยุด timers ทั้งหมดก่อน
            if (thresholdCheckTimer_2.Enabled) thresholdCheckTimer_2.Stop();
            if (countdownTimer_2.Enabled) countdownTimer_2.Stop();

            // อัปเดตสถานะปัจจุบัน
            currentStepState = newState;
            Debug.WriteLine($"[UC2] Step state changed to: {newState}");

            // เริ่ม timer ตามสถานะใหม่
            switch (newState)
            {
                case StepState_2.WaitingForThreshold:
                    waitingForThreshold_2 = true;
                    thresholdCheckTimer_2.Start();
                    Debug.WriteLine($"[UC2] WAITING for temperature threshold before countdown");
                    break;

                case StepState_2.CountingDown:
                    waitingForThreshold_2 = false;
                    countdownTimer_2.Start();
                    Debug.WriteLine($"[UC2] STARTING countdown timer");
                    break;

                case StepState_2.Idle:
                    waitingForThreshold_2 = false;
                    Debug.WriteLine($"[UC2] System idle");
                    break;
            }

            // อัปเดต UI
            UpdateLampStatus_2();
        }
        private void countdownTimer2_Tick(object sender, EventArgs e)
        {
            try
            {
                // ตรวจสอบสถานะพื้นฐานเท่าที่จำเป็น
                if (!ProgramState_2.IsStarted_2 || ProgramState_2.IsPaused_2)
                {
                    Debug.WriteLine("[UC2] Timer tick: Program not in countdown state");
                    return;
                }

                // ลดเวลาและอัพเดต UI
                if (ProgramState_2.RemainingSeconds_2 > 0)
                {
                    ProgramState_2.RemainingSeconds_2--;

                    // อัพเดตแค่ฟังก์ชันเดียว ไม่เรียกทั้งสองอัน
                    UpdateCountdownLabels_2();

                    // Log สถานะปัจจุบัน
                    int rem = ProgramState_2.RemainingSeconds_2;
                    int hr = rem / 3600;
                    int mn = (rem % 3600) / 60;
                    int sc = rem % 60;
                    Debug.WriteLine($"[UC2] Countdown: {hr}:{mn:D2}:{sc:D2} remaining");
                }
                else
                {
                    // เวลาหมด - หยุด timer และไปยัง step ถัดไป
                    countdownTimer_2.Stop();
                    Debug.WriteLine($"[UC2] Step {currentStep_2} countdown completed");
                    NextStep_2();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] countdownTimer2_Tick: {ex.Message}");
            }
        }
        private bool IsStepTargetTJ_2(int step)
        {
            switch (step)
            {
                case 1: return switch_Target2_1?.IsOn ?? false;
                case 2: return switch_Target2_2?.IsOn ?? false;
                case 3: return switch_Target2_3?.IsOn ?? false;
                case 4: return switch_Target2_4?.IsOn ?? false;
                case 5: return switch_Target2_5?.IsOn ?? false;
                case 6: return switch_Target2_6?.IsOn ?? false;
                case 7: return switch_Target2_7?.IsOn ?? false;
                case 8: return switch_Target2_8?.IsOn ?? false;
                default: return false;
            }
        }
        private bool IsStepTimeRun_2(int step)
        {
            switch (step)
            {
                case 1: return switch_Time2_1?.IsOn ?? false;
                case 2: return switch_Time2_2?.IsOn ?? false;
                case 3: return switch_Time2_3?.IsOn ?? false;
                case 4: return switch_Time2_4?.IsOn ?? false;
                case 5: return switch_Time2_5?.IsOn ?? false;
                case 6: return switch_Time2_6?.IsOn ?? false;
                case 7: return switch_Time2_7?.IsOn ?? false;
                case 8: return switch_Time2_8?.IsOn ?? false;
                default: return false;
            }
        }
        private float GetTargetTemp_2(int step)
        {
            try
            {
                var tb = Controls.Find($"text2_Temp{step}", true).FirstOrDefault() as TextBox;
                if (tb == null)
                {
                    Debug.WriteLine($"[UC2][ERROR] GetTargetTemp2: Cannot find text2_Temp{step} control");
                    return 0;
                }

                if (!float.TryParse(tb.Text, out float val))
                {
                    Debug.WriteLine($"[UC2][ERROR] GetTargetTemp2: Invalid value in text2_Temp{step}: '{tb.Text}'");
                    return 0;
                }

                Debug.WriteLine($"[UC2] GetTargetTemp2: Step {step} target temperature = {val:F2}");
                return val;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] GetTargetTemp2: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// ตรวจสอบทันทีว่าอุณหภูมิปัจจุบันถึงเป้าหมายหรือไม่ (สำหรับโหมด WAIT)
        /// </summary>
        /// <summary>
        /// ตรวจสอบทันทีว่าอุณหภูมิปัจจุบันถึงเป้าหมายหรือไม่ (สำหรับโหมด WAIT)
        /// </summary>

        private void UpdateCountdownLabels_2()
        {
            try
            {
                // ไม่อนุญาตให้มีค่าติดลบ
                int rem = Math.Max(ProgramState_2.RemainingSeconds_2, 0);

                // คำนวณชั่วโมง:นาที:วินาที
                int hr = rem / 3600;
                int mn = (rem % 3600) / 60;
                int sc = rem % 60;

                // กำหนดค่าส่วนประกอบแยกชัดเจน (สำหรับหน้าจอหลัก)
                label_Process2Hr.Text = hr.ToString();
                label_Process2Min.Text = mn.ToString("D2");  // แสดงเป็น 2 หลักเสมอ
                label_Process2Sec.Text = sc.ToString("D2");  // แสดงเป็น 2 หลักเสมอ

                // สำหรับหน้าจอรวม "00:00:00"
                label_Process2Time_Seg.Text = $"{hr}:{mn:D2}:{sc:D2}";

                // แสดงเวลาที่ผ่านไปด้วย (หาก totalStepSeconds2 มีค่า)
                if (totalStepSeconds_2 > 0)
                {
                    int elapsed = totalStepSeconds_2 - rem;
                    int eHr = elapsed / 3600;
                    int eMin = (elapsed % 3600) / 60;
                    int eSec = elapsed % 60;
                    label_Process2Time_Left.Text = $"{eHr}:{eMin:D2}:{eSec:D2}";
                }

                Debug.WriteLine($"[UC2] Timer updated: {hr}:{mn:D2}:{sc:D2} remaining");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] UpdateCountdownLabels: {ex.Message}");
            }
        }

        private void UpdateTimerDisplay2()
        {
            try
            {
                // ต้องแน่ใจว่าไม่มีค่าติดลบ
                int rem = Math.Max(remainingSeconds_2, 0);

                // คำนวณชั่วโมง:นาที:วินาที
                int hr = rem / 3600;
                int min = (rem % 3600) / 60;
                int sec = rem % 60;

                // อัพเดต Label ที่แสดงเวลาแบบ "00:00:00"
                label_Process2Time_Seg.Text = $"{hr}:{min:D2}:{sec:D2}";

                // อัพเดตส่วนประกอบแยก
                label_Process2Hr.Text = hr.ToString();
                label_Process2Min.Text = min.ToString("D2");
                label_Process2Sec.Text = sec.ToString("D2");

                // คำนวณเวลาที่ผ่านไป (elapsed time)
                if (totalStepSeconds_2 > 0)
                {
                    int elapsed = totalStepSeconds_2 - rem;
                    int eHr = elapsed / 3600;
                    int eMin = (elapsed % 3600) / 60;
                    int eSec = elapsed % 60;
                    label_Process2Time_Left.Text = $"{eHr}:{eMin:D2}:{eSec:D2}";
                }

                Debug.WriteLine($"[UC2] Timer display updated: {hr}:{min:D2}:{sec:D2}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] UpdateTimerDisplay: {ex.Message}");
            }
        }
        private void SetLampForStep_2(int step, string status)
        {
            // ป้องกันค่าผิด
            if (status != "Wait" && status != "Run" && status != "Done")
                status = "Wait";

            persistedLampStatuses_2[step] = status;

            switch (step)
            {
                case 1: lamp_Process21.Mode = status; break;
                case 2: lamp_Process22.Mode = status; break;
                case 3: lamp_Process23.Mode = status; break;
                case 4: lamp_Process24.Mode = status; break;
                case 5: lamp_Process25.Mode = status; break;
                case 6: lamp_Process26.Mode = status; break;
                case 7: lamp_Process27.Mode = status; break;
                case 8: lamp_Process28.Mode = status; break;
            }
        }
        private void StartStepCountdown_2(int step)
        {
            try
            {
                // หยุดระบบทั้งหมดก่อน
                if (countdownTimer_2.Enabled) countdownTimer_2.Stop();
                if (thresholdCheckTimer_2.Enabled) thresholdCheckTimer_2.Stop();

                Debug.WriteLine($"[UC2] StartStepCountdown2: Starting step {step}");

                // 1) ตรวจสอบเวลา
                int secs = GetStepCountdownSeconds_2(step);
                if (secs <= 0)
                {
                    Debug.WriteLine($"[UC2][ERROR] Step {step}: invalid countdown ({secs}s), abort.");
                    MessageBox.Show($"ไม่สามารถเริ่มนับเวลาสำหรับ Step {step} ได้ กรุณาตรวจสอบค่าเวลาที่ป้อน", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2) ตั้งค่าสถานะ
                ProgramState_2.CurrentStep_2 = step;
                ProgramState_2.RemainingSeconds_2 = secs;
                totalStepSeconds_2 = secs;
                currentStep_2 = step;
                waitStep_2 = step;
                label_Process2Step.Text = step.ToString();

                // 3) ส่งค่าไปยัง BoxControl (ถ้ายังไม่ได้ส่ง)
                SendStepSetpointsToSerialAndDisplay_2(step);

                // 4) อัพเดตการแสดงเวลา
                int dispHr = secs / 3600;
                int dispMn = (secs % 3600) / 60;
                int dispSc = secs % 60;
                label_Process2Time_Seg.Text = $"{dispHr}:{dispMn:D2}:{dispSc:D2}";
                label_Process2Hr.Text = dispHr.ToString();
                label_Process2Min.Text = dispMn.ToString();
                label_Process2Sec.Text = dispSc.ToString();
                label_Process2Time_Left.Text = $"0:00:00";

                // 5) ตรวจสอบโหมดและเริ่มการทำงานตามที่เหมาะสม
                bool isRunMode = IsStepTimeRun_2(step);
                bool isTJmode = IsStepTargetTJ_2(step);
                Debug.WriteLine($"[UC2] Step {step}: Mode={(isRunMode ? "RUN" : "WAIT")}, Channel={(isTJmode ? "TJ" : "TR")}");

                // ตั้งค่าสถานะการทำงาน
                isStarted_2 = true;
                waitingForThreshold_2 = !isRunMode;  // true เมื่อเป็นโหมด WAIT

                // สำคัญ! ถ้าเป็นโหมด RUN, เริ่ม countdownTimer2 ทันที
                // ถ้าเป็นโหมด WAIT, เริ่ม thresholdCheckTimer2 เพื่อรอให้อุณหภูมิถึงค่าที่ต้องการ
                if (isRunMode)
                {
                    // โหมด RUN - เริ่มนับถอยหลังทันที
                    Debug.WriteLine($"[UC2] Step {step}: RUN mode - starting countdown immediately");
                    currentStepState = StepState_2.CountingDown;
                    countdownTimer_2.Start();
                }
                else
                {
                    // โหมด WAIT - ตรวจสอบอุณหภูมิก่อน
                    Debug.WriteLine($"[UC2] Step {step}: WAIT mode - checking temperature threshold");

                    // ตรวจสอบอุณหภูมิปัจจุบันทันที
                    double targetTemp = GetTargetTemp_2(step);
                    var values = SerialPortManager.Instance.UI2Values;
                    double currentTemp = 0;

                    if (values != null && values.Length >= 2)
                    {
                        currentTemp = isTJmode ? values[1] : values[0]; // [0]=TR2, [1]=TJ2
                        Debug.WriteLine($"[UC2] Current temperature from UI2Values: {(isTJmode ? "TJ" : "TR")}={currentTemp:F2}, Target={targetTemp:F2}");

                        if (currentTemp >= targetTemp)
                        {
                            // ถ้าอุณหภูมิถึงแล้ว เริ่มนับถอยหลังทันที
                            Debug.WriteLine($"[UC2] Temperature already above threshold, starting countdown immediately");
                            currentStepState = StepState_2.CountingDown;
                            countdownTimer_2.Start();
                        }
                        else
                        {
                            // ถ้ายังไม่ถึง เริ่ม thresholdCheckTimer2 เพื่อตรวจสอบต่อเนื่อง
                            Debug.WriteLine($"[UC2] Temperature below threshold, waiting... Current={currentTemp:F2}, Target={targetTemp:F2}");
                            currentStepState = StepState_2.WaitingForThreshold;
                            thresholdCheckTimer_2.Start();
                        }
                    }
                    else
                    {
                        // ถ้ายังไม่มีข้อมูลอุณหภูมิ เริ่ม thresholdCheckTimer2 เพื่อรอ
                        Debug.WriteLine($"[UC2] No temperature data available yet, starting threshold check timer");
                        currentStepState = StepState_2.WaitingForThreshold;
                        thresholdCheckTimer_2.Start();
                    }
                }

                // 6) อัพเดต UI
                UpdatePicTarget_2(step);
                UpdatePicModeTime_2(step);
                UpdateLampStatus_2();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] StartStepCountdown2: {ex.Message}");
                MessageBox.Show($"เกิดข้อผิดพลาดในการเริ่ม Step {step}: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ตรวจสอบว่าอุณหภูมิปัจจุบันสูงกว่าหรือเท่ากับค่าเป้าหมายหรือไม่
        /// </summary>
        private bool IsTemperatureAboveThreshold_2(int step)
        {
            try
            {
                double targetTemp = GetTargetTemp_2(step);
                bool isTjMode = IsStepTargetTJ_2(step);
                double currentTemp = GetCurrentTemperature_2(isTjMode);

                // ตรวจสอบและแสดงค่าเพื่อการวิเคราะห์
                Debug.WriteLine($"[UC2] Temperature check: Step={step}, {(isTjMode ? "TJ" : "TR")}={currentTemp:F2}, Target={targetTemp:F2}");

                // ป้องกันการเทียบค่าที่ผิดพลาด
                if (double.IsNaN(currentTemp) || double.IsInfinity(currentTemp) || currentTemp > 1000)
                {
                    Debug.WriteLine($"[UC2][WARNING] Invalid temperature reading: {currentTemp}");
                    return false;
                }

                // ตรวจสอบว่าอุณหภูมิถึงเป้าหมายหรือไม่
                bool isAboveThreshold = currentTemp >= targetTemp;

                // แสดงผลการตรวจสอบ
                if (isAboveThreshold)
                    Debug.WriteLine($"[UC2] Temperature threshold MET: {currentTemp:F2} >= {targetTemp:F2}");
                else
                    Debug.WriteLine($"[UC2] Temperature threshold NOT met: {currentTemp:F2} < {targetTemp:F2}");

                return isAboveThreshold;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] IsTemperatureAboveThreshold: {ex.Message}");
                return false;  // ถ้าเกิดข้อผิดพลาด ถือว่ายังไม่ถึงค่าเป้าหมาย
            }
        }
        private void SafeNavigateToControlSet_2()
        {
            try
            {
                // เปลี่ยนหน้าโดยใช้ NavigationHelper
                NavigationHelper.NavigateTo<UC_CONTROL_SET_2>(() => {
                    // หยุดการทำงานทั้งหมดก่อน
                    if (isStarted_2)
                    {
                        ProgramState_2.Reset_Set2(); // Just call the method directly
                        isStarted_2 = false;
                        isPaused_2 = false;
                    }

                    // หยุด timers
                    if (countdownTimer_2 != null) countdownTimer_2.Stop();
                    if (thresholdCheckTimer_2 != null) thresholdCheckTimer_2.Stop();
                    if (updateTimer_2 != null) updateTimer_2.Stop();
                    if (settingsUpdateTimer_2 != null) settingsUpdateTimer_2.Stop();

                    // ยกเลิกการลงทะเบียน event
                    if (SerialPortManager.Instance != null)
                    {
                        SerialPortManager.Instance.UI2DataReceivedEvent -= OnUI_2DataReceived;
                        SerialPortManager.Instance.DataReceivedEvent -= OnDataReceived_2;
                        Debug.WriteLine("[UC2] Unregistered serial port event handlers");
                    }

                    // บันทึกสถานะก่อนเปลี่ยนหน้า
                    SaveLampState_2();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] SafeNavigateToControlSet2: {ex.Message}");
                MessageBox.Show($"ไม่สามารถเปลี่ยนหน้าได้: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // เพิ่มเมธอดช่วยในการดึงค่าอุณหภูมิปัจจุบันให้ง่ายขึ้น
        private double GetCurrentTemperature_2(bool isTjMode = false)
        {
            double result = 0;

            // 1. ลองจาก UI2Values ก่อน (น่าจะมีค่าล่าสุด)
            var ui2Values = SerialPortManager.Instance.UI2Values;
            if (ui2Values != null && ui2Values.Length >= 2)
            {
                result = isTjMode ? ui2Values[1] : ui2Values[0]; // [0]=TR2, [1]=TJ2
                return result;
            }

            // 2. ถ้าไม่มี ลองจาก CurrentValues
            var currentValues = SerialPortManager.Instance.CurrentValues;
            if (currentValues != null && currentValues.Length >= 5)
            {
                result = isTjMode ? currentValues[4] : currentValues[3]; // [3]=TR2, [4]=TJ2
                return result;
            }

            // 3. ถ้ายังไม่มีค่า ลองดึงจาก label
            if (isTjMode && label_TJ2 != null && !string.IsNullOrEmpty(label_TJ2.Text))
            {
                double.TryParse(label_TJ2.Text, out result);
            }
            else if (!isTjMode && label_TR2 != null && !string.IsNullOrEmpty(label_TR2.Text))
            {
                double.TryParse(label_TR2.Text, out result);
            }

            return result;
        }
        // --------------------- สิ้นสุดส่วน Lamp ---------------------

        // ฟังก์ชันสำหรับบันทึก Lamp State ลงใน Data_Set1.xml
        private void SaveLampState_2()
        {
            if (settings_2 == null) return;
            if (lamp_Process21 != null) settings_2.LampState21 = lamp_Process21.Mode;
            if (lamp_Process22 != null) settings_2.LampState22 = lamp_Process22.Mode;
            if (lamp_Process23 != null) settings_2.LampState23 = lamp_Process23.Mode;
            if (lamp_Process24 != null) settings_2.LampState24 = lamp_Process24.Mode;
            if (lamp_Process25 != null) settings_2.LampState25 = lamp_Process25.Mode;
            if (lamp_Process26 != null) settings_2.LampState26 = lamp_Process26.Mode;
            if (lamp_Process27 != null) settings_2.LampState27 = lamp_Process27.Mode;
            if (lamp_Process28 != null) settings_2.LampState28 = lamp_Process28.Mode;
            settings_2.SaveToFile_2("Data_Set2.xml", "LampState");
            Debug.WriteLine("[UI2] SaveLampState completed");
        }

        // ฟังก์ชันสำหรับโหลด Lamp State จาก settings (ถ้ามี)
        private void LoadLampState_2()
        {
            if (!string.IsNullOrEmpty(settings_2.LampState21))
                lamp_Process21.Mode = settings_2.LampState21;
            if (!string.IsNullOrEmpty(settings_2.LampState22))
                lamp_Process22.Mode = settings_2.LampState22;
            if (!string.IsNullOrEmpty(settings_2.LampState23))
                lamp_Process23.Mode = settings_2.LampState23;
            if (!string.IsNullOrEmpty(settings_2.LampState24))
                lamp_Process24.Mode = settings_2.LampState24;
            if (!string.IsNullOrEmpty(settings_2.LampState25))
                lamp_Process25.Mode = settings_2.LampState25;
            if (!string.IsNullOrEmpty(settings_2.LampState26))
                lamp_Process26.Mode = settings_2.LampState26;
            if (!string.IsNullOrEmpty(settings_2.LampState27))
                lamp_Process27.Mode = settings_2.LampState27;
            if (!string.IsNullOrEmpty(settings_2.LampState28))
                lamp_Process28.Mode = settings_2.LampState28;
        }

        private void StaticCountdownTimer2_Tick(object sender, EventArgs e)
        {
            if (!ProgramState_2.IsStarted_2 || ProgramState_2.IsPaused_2) return;

            // ลดทีละ 1 วินาทีเสมอ
            if (ProgramState_2.RemainingSeconds_2 > 0)
                ProgramState_2.RemainingSeconds_2--;

            UpdateUIFromState();

            if (ProgramState_2.RemainingSeconds_2 == 0)
                ProgramState_2.OnCountdownFinished_2();
        }



        private void CheckTemperatureThreshold_2(int step)
        {
            try
            {
                if (!waitingForThreshold_2 || step <= 0)
                    return;

                // ดึงค่าเป้าหมายและโหมดการทำงาน
                double targetTemp = GetTargetTemp_2(step);
                bool isTjMode = IsStepTargetTJ_2(step);
                double currentTemp = 0;

                // ลองใช้ CurrentValues ก่อน
                var currentValues = SerialPortManager.Instance.CurrentValues;
                if (currentValues != null && currentValues.Length >= 9)
                {
                    // ใช้ index ที่ถูกต้องสำหรับ SET 2 (UI2) values
                    currentTemp = isTjMode ? currentValues[4] : currentValues[3]; // 4=TJ2, 3=TR2 in CurrentValues

                    Debug.WriteLine($"[UC2][Initial Check] Step {step}: {(isTjMode ? "TJ" : "TR")}={currentTemp:F2}, Target={targetTemp:F2}");

                    // หากอุณหภูมิถึงเป้าหมายหรือสูงกว่าแล้ว
                    if (currentTemp >= targetTemp)
                    {
                        // เปลี่ยนสถานะ
                        waitingForThreshold_2 = false;
                        Debug.WriteLine($"[UC2][Initial Check] Step {step}: temperature already above threshold, starting countdown immediately");

                        // หยุด threshold timer (ถ้ากำลังทำงาน)
                        if (thresholdCheckTimer_2.Enabled)
                        {
                            thresholdCheckTimer_2.Stop();
                        }

                        // เริ่มนับถอยหลัง
                        if (!countdownTimer_2.Enabled)
                        {
                            countdownTimer_2.Start();
                            Debug.WriteLine($"[UC2][Initial Check] Step {step}: countdown timer started");
                        }

                        // อัปเดต UI
                        UpdateLampStatus_2();
                        return;
                    }
                }

                // ถ้า CurrentValues ไม่มีข้อมูล ลองใช้ UI2Values
                var ui = SerialPortManager.Instance.UI2Values;
                if (ui != null && ui.Length >= 4)
                {
                    currentTemp = isTjMode ? ui[1] : ui[0]; // 1=TJ2, 0=TR2 in UI2Values

                    Debug.WriteLine($"[UC2][Initial Check] Step {step}: {(isTjMode ? "TJ" : "TR")}={currentTemp:F2}, Target={targetTemp:F2}");

                    // หากอุณหภูมิถึงเป้าหมายหรือสูงกว่าแล้ว
                    if (currentTemp >= targetTemp)
                    {
                        // เปลี่ยนสถานะ
                        waitingForThreshold_2 = false;
                        Debug.WriteLine($"[UC2][Initial Check] Step {step}: temperature already above threshold, starting countdown immediately");

                        // หยุด threshold timer (ถ้ากำลังทำงาน)
                        if (thresholdCheckTimer_2.Enabled)
                        {
                            thresholdCheckTimer_2.Stop();
                        }

                        // เริ่มนับถอยหลัง
                        if (!countdownTimer_2.Enabled)
                        {
                            countdownTimer_2.Start();
                            Debug.WriteLine($"[UC2][Initial Check] Step {step}: countdown timer started");
                        }

                        // อัปเดต UI
                        UpdateLampStatus_2();
                    }
                    else
                    {
                        // ยังไม่ถึงอุณหภูมิเป้าหมาย - ล็อกเพื่อตรวจสอบ
                        Debug.WriteLine($"[UC2][Initial Check] Step {step}: waiting for temperature to reach {targetTemp:F2} (current: {currentTemp:F2})");
                    }
                }
                else
                {
                    Debug.WriteLine($"[UC2][WARNING] CheckTemperatureThreshold2: No valid temperature data available");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] CheckTemperatureThreshold2: {ex.Message}");
            }
        }

        // --- Emergency Stop ---
        public UC_PROGRAM_CONTROL_SET_2()
        {
            // ───────────────────────── 1. สร้างคอนโทรล Designer ─────────────────────────
            InitializeComponent();
               // ผูก Auto/Manual Toggle ให้ใช้ตัวแปรกลาง
            switch_A_M2.IsOn = ProgramState_2.SwitchA_M2;
            switch_A_M2.ToggleChanged += (s, e) => ProgramState_2.SwitchA_M2 = switch_A_M2.IsOn;
            // เพิ่ม timer สำหรับตรวจสอบอุณหภูมิโดยเฉพาะ
            thresholdCheckTimer_2 = new Timer { Interval = 500 };  // ตรวจสอบทุก 0.5 วินาที
            thresholdCheckTimer_2.Tick += ThresholdCheckTimer2_Tick;
            // ซิงค์กลับเมื่อมีการเปลี่ยนบนอีกหน้า
            ProgramState_2.StateChanged_2 += ProgramState_SwitchChanged_2;

            settings_2 = Data_Set2.LoadFromFile_2("Data_Set2.xml") ?? new Data_Set2();
            Debug.WriteLine("Loaded settings2 from Data_Set2.xml");
            SerialPortManager.Instance.UI2DataReceivedEvent += OnUI_2DataReceived;
            SerialPortManager.Instance.DataReceivedEvent += OnDataReceived_2;
            // ** เพิ่มบรรทัดนี้ **
            TrySendInitialConfiguration_2();
            // ───────────────────────── 2. Logger / Debug Startup ─────────────────────────
            Logger.CurrentLogLevel = LogLevel.Debug;
            Debug.WriteLine("[Info] [UI2] Logger.CurrentLogLevel set to Debug");

            // ───────────────────────── 3. SerialPortManager ───────────────────────────────


            LoadSavedValues_2();
            // … ส่วนที่เหลือ …
        

        Debug.WriteLine("[Debug] [SPM] Registered UI tag [UI2] for groups [3,4]");
            Debug.WriteLine("[Debug] [SPM] Initializing");
            bool opened = SerialPortManager.Instance.Open();
            Debug.WriteLine(opened
                ? "[Info] [SPM] Port opened successfully"
                : "[Error] [SPM] Port open failed");
            Debug.WriteLine($"[Info] [UI2] SerialPortManager.Open() returned: {opened}");

            // ───────────────────────── 4. Load DeviceSettings ─────────────────────────────
            deviceSettings_2 = DeviceSettings_2.Reset_Set2("Settings_2.xml");
            Debug.WriteLine("[DeviceSettings] Loaded settings from 'Settings_2.xml'");


            // ───────────────────────── 5. Load Data_Set2 ─────────────────────────────────
            settings_2 = Data_Set2.LoadFromFile_2("Data_Set2.xml") ?? new Data_Set2();
            Debug.WriteLine("Loaded settings from Data_Set2.xml");

            // ───────────────────────── 6. Setup TextBox Arrays & Handlers ─────────────────
            SetupTextBoxArrays_2();
            RegisterTimeTextBoxes_2();
            RegisterSettingTextBoxes_2();
            RegisterToggleHandlers_2();

            // ───────────────────────── 7. ProgramState Events ─────────────────────────────
            ProgramState_2.StateChanged_2 += ProgramState_StateChanged_2;
            ProgramState_2.CountdownFinished_2 += ProgramState_CountdownFinished_2;

            // ───────────────────────── 8. Timers Configuration ────────────────────────────
            countdownTimer_2 = new Timer { Interval = 1000 };
            countdownTimer_2.Tick += countdownTimer2_Tick;
            updateTimer_2 = new Timer { Interval = 500 };
            updateTimer_2.Tick += UpdateTimer_Tick_2;
            settingsUpdateTimer_2 = new Timer { Interval = 500 };
            settingsUpdateTimer_2.Tick += (s, e) => UpdateSetpointLabels_2();

            // ───────────────────────── 9. Load Saved Values & Lamp State ─────────────────
            LoadSavedValues_2();
            LoadLampState_2();

            // ──────────────────────── 10. Restore Toggle / Switch States ──────────────────
            toggle_2SwitchControl1.IsOn = settings_2.Toggle_2SwitchControl1;
            toggle_2SwitchControl2.IsOn = settings_2.Toggle_2SwitchControl2;
            toggle_2SwitchControl3.IsOn = settings_2.Toggle_2SwitchControl3;
            toggle_2SwitchControl4.IsOn = settings_2.Toggle_2SwitchControl4;
            toggle_2SwitchControl5.IsOn = settings_2.Toggle_2SwitchControl5;
            toggle_2SwitchControl6.IsOn = settings_2.Toggle_2SwitchControl6;
            toggle_2SwitchControl7.IsOn = settings_2.Toggle_2SwitchControl7;
            toggle_2SwitchControl8.IsOn = settings_2.Toggle_2SwitchControl8;
            switch_Target2_1.IsOn = settings_2.Switch_Target2_1; switch_Time2_1.IsOn = settings_2.Switch_Time2_1;
            switch_Target2_2.IsOn = settings_2.Switch_Target2_2; switch_Time2_2.IsOn = settings_2.Switch_Time2_2;
            switch_Target2_3.IsOn = settings_2.Switch_Target2_3; switch_Time2_3.IsOn = settings_2.Switch_Time2_3;
            switch_Target2_4.IsOn = settings_2.Switch_Target2_4; switch_Time2_4.IsOn = settings_2.Switch_Time2_4;
            switch_Target2_5.IsOn = settings_2.Switch_Target2_5; switch_Time2_5.IsOn = settings_2.Switch_Time2_5;
            switch_Target2_6.IsOn = settings_2.Switch_Target2_6; switch_Time2_6.IsOn = settings_2.Switch_Time2_6;
            switch_Target2_7.IsOn = settings_2.Switch_Target2_7; switch_Time2_7.IsOn = settings_2.Switch_Time2_7;
            switch_Target2_8.IsOn = settings_2.Switch_Target2_8; switch_Time2_8.IsOn = settings_2.Switch_Time2_8;

            // ───────────────────────── 11. ToggleChanged Save Handlers ────────────────────
            switch_Target2_1.ToggleChanged += (s, e) => { settings_2.Switch_Target2_1 = switch_Target2_1.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step1"); };
            switch_Time2_1.ToggleChanged += (s, e) => { settings_2.Switch_Time2_1 = switch_Time2_1.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step1"); };
            switch_Target2_2.ToggleChanged += (s, e) => { settings_2.Switch_Target2_2 = switch_Target2_2.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step2"); };
            switch_Time2_2.ToggleChanged += (s, e) => { settings_2.Switch_Time2_2 = switch_Time2_2.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step2"); };
            switch_Target2_3.ToggleChanged += (s, e) => { settings_2.Switch_Target2_3 = switch_Target2_3.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step3"); };
            switch_Time2_3.ToggleChanged += (s, e) => { settings_2.Switch_Time2_3 = switch_Time2_3.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step3"); };
            switch_Target2_4.ToggleChanged += (s, e) => { settings_2.Switch_Target2_4 = switch_Target2_4.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step4"); };
            switch_Time2_4.ToggleChanged += (s, e) => { settings_2.Switch_Time2_4 = switch_Time2_4.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step4"); };
            switch_Target2_5.ToggleChanged += (s, e) => { settings_2.Switch_Target2_5 = switch_Target2_5.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step5"); };
            switch_Time2_5.ToggleChanged += (s, e) => { settings_2.Switch_Time2_5 = switch_Time2_5.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step5"); };
            switch_Target2_6.ToggleChanged += (s, e) => { settings_2.Switch_Target2_6 = switch_Target2_6.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step6"); };
            switch_Time2_6.ToggleChanged += (s, e) => { settings_2.Switch_Time2_6 = switch_Time2_6.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step6"); };
            switch_Target2_7.ToggleChanged += (s, e) => { settings_2.Switch_Target2_7 = switch_Target2_7.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step7"); };
            switch_Time2_7.ToggleChanged += (s, e) => { settings_2.Switch_Time2_7 = switch_Time2_7.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step7"); };
            switch_Target2_8.ToggleChanged += (s, e) => { settings_2.Switch_Target2_8 = switch_Target2_8.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step8"); };
            switch_Time2_8.ToggleChanged += (s, e) => { settings_2.Switch_Time2_8 = switch_Time2_8.IsOn; settings_2.SaveToFile_2("Data_Set2.xml", "Step8"); };

            // ───────────────────────── 12. Auto / Manual Mode ────────────────────────────
            switch_A_M2.IsOn = ProgramState_2.SwitchA_M2;
            switch_A_M2.ToggleChanged += switch_A_M_ToggleChanged;

            // ───────────────────────── 13. Start / Resume ────────────────────────────────
            switch_system2_1.Click += (s, e) =>
            {
                Debug.WriteLine("switch_system2_1 pressed. Preparing to start...");
                if (!CanStartSystem_2()) return;

                if (ProgramState_2.IsPaused_2)
                {
                    ProgramState_2.Pause_2();
                    switch_system2_1.IsOn = true; switch_system2_2.IsOn = false;

                    // เช็คสถานะก่อนเริ่มนับต่อ
                    if (currentStepState == StepState_2.CountingDown)
                        countdownTimer_2.Start();
                    else if (currentStepState == StepState_2.WaitingForThreshold)
                        thresholdCheckTimer_2.Start();

                    return;
                }

                if (!isStarted_2)
                {
                    ResetAllSteps_2();
                    int first = FindFirstEnabledStep_2();
                    if (first > 8)
                    {
                        MessageBox.Show("ยังไม่เปิด Step ใด", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int secs = GetStepCountdownSeconds_2(first);
                    if (secs <= 0)
                    {
                        MessageBox.Show($"Countdown ของ Step {first} ไม่ถูกต้อง", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // ตั้งค่าเบื้องต้น
                    ProgramState_2.Start_2(first, secs);
                    currentStep_2 = first;
                    remainingSeconds_2 = secs;
                    totalStepSeconds_2 = secs;
                    isStarted_2 = true;
                    isPaused_2 = false;
                    switch_system2_1.IsOn = true;
                    switch_system2_2.IsOn = false;

                    // เปลี่ยนจากการเรียก countdownTimer2.Start() ตรงๆ เป็นเรียก StartStepCountdown2
                    StartStepCountdown_2(first);
                }
            };

            // ───────────────────────── 14. Stop / Reset ─────────────────────────────────
            switch_system2_2.Click += (s, e) =>
            {
                if (!isStarted_2) return;
                countdownTimer_2.Stop();
                ProgramState_2.Reset_Set2();
                ResetAllSteps_2();
                currentStep_2 = remainingSeconds_2 = 0; isStarted_2 = false; isPaused_2 = false;
                label_Process2Hr.Text = label_Process2Min.Text = label_Process2Sec.Text =
                label_Process2Step.Text = label_Process2Time_Seg.Text = label_Process2Time_Left.Text = "0";
                switch_system2_1.IsOn = false; switch_system2_2.IsOn = true;
            };

            // ───────────────────────── 15. Skip ──────────────────────────────────────────
            switch_system2_3.Click += (s, e) =>
            {
                if (isStarted_2 && !ProgramState_2.IsPaused_2)
                {
                    countdownTimer_2.Stop();
                    NextStep_2();
                }
                switch_system2_3.IsOn = false;
            };

            // ───────────────────────── 16. Home ──────────────────────────────────────────
            But_GoHome4.Click += But_GoHome4_Click;

            // ───────────────────────── 17. Restore state if revisited ───────────────────
            if (ProgramState_2.CurrentStep_2 > 0)
            {
                currentStep_2 = ProgramState_2.CurrentStep_2;
                remainingSeconds_2 = ProgramState_2.RemainingSeconds_2;
                isStarted_2 = ProgramState_2.IsStarted_2;
                isPaused_2 = ProgramState_2.IsPaused_2;
            }

            // ───────────────────────── 18. Initial UI update ────────────────────────────
            UpdateUIFromState();

            // ───────────────────────── 19. Initial button states ────────────────────────
            if (!isStarted_2 && !isPaused_2) { switch_system2_1.IsOn = false; switch_system2_2.IsOn = true; }
            else if (isStarted_2 && !isPaused_2) { switch_system2_1.IsOn = true; switch_system2_2.IsOn = false; }
            else { switch_system2_1.IsOn = false; switch_system2_2.IsOn = true; }

            // ───────────────────────── 20. Start periodic timers ─────────────────────────
            updateTimer_2.Start();
            settingsUpdateTimer_2.Start();
            if (ProgramState_2.IsStarted_2 && !ProgramState_2.IsPaused_2)
                countdownTimer_2.Start();

            Debug.WriteLine("[UI2] Constructor completed.");
        }

        private void OnDataReceived_2(double[] values)
        {
            try
            {
                // ตรวจสอบข้อมูลที่รับมา
                if (values == null || values.Length < 9)
                {
                    Debug.WriteLine("[UC2][WARNING] OnDataReceived: Received invalid or incomplete data");
                    return;
                }

                // Log ค่าอุณหภูมิที่รับมา
                Debug.WriteLine($"[UC2] Received data: TR2={values[3]:F2}, TJ2={values[4]:F2}");

                // ตรวจสอบว่ากำลังรอ threshold หรือไม่
                if (isStarted_2 && waitingForThreshold_2 && waitStep_2 > 0)
                {
                    // ดึงค่าเป้าหมายและตรวจสอบโหมด
                    double targetTemp = GetTargetTemp_2(waitStep_2);
                    bool isTjMode = IsStepTargetTJ_2(waitStep_2);

                    // เลือกค่าอุณหภูมิตามโหมดที่เลือก (ใช้ปกติ index 3 และ 4 สำหรับ TR2/TJ2)
                    double currentTemp = isTjMode ? values[4] : values[3]; // 4=TJ2, 3=TR2

                    Debug.WriteLine($"[UC2][Temperature Check] Step {waitStep_2}: {(isTjMode ? "TJ" : "TR")}={currentTemp:F2}, Target={targetTemp:F2}");

                    // ป้องกันค่าอุณหภูมิที่ผิดปกติ
                    if (currentTemp > 500)
                    {
                        Debug.WriteLine($"[UC2][WARNING] Abnormal temperature reading: {currentTemp:F2}");
                        return;
                    }

                    // ตรวจสอบว่าถึงค่าเป้าหมายหรือไม่
                    if (currentTemp >= targetTemp)
                    {
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.BeginInvoke(new Action(() => {
                                if (waitingForThreshold_2)
                                {
                                    waitingForThreshold_2 = false;
                                    Debug.WriteLine($"[UC2][Temperature Check] Step {waitStep_2}: THRESHOLD REACHED! Starting countdown");

                                    if (thresholdCheckTimer_2.Enabled)
                                        thresholdCheckTimer_2.Stop();

                                    if (!countdownTimer_2.Enabled)
                                    {
                                        countdownTimer_2.Start();
                                        Debug.WriteLine($"[UC2] Countdown timer started for Step {waitStep_2}");
                                    }

                                    // อัพเดต UI
                                    UpdateLampStatus_2();
                                }
                            }));
                        }
                        else
                        {
                            Debug.WriteLine("[UC2] Handle not created yet or disposed, deferring action");
                        }
                    }
                }

                // ตรวจสอบ External PT100 (ถ้ามีการเปิดใช้งาน)
                if (!string.IsNullOrEmpty(deviceSettings_2.PT100Sensor_2) &&
                    deviceSettings_2.PT100Sensor_2 != "Disabled")
                {
                    double actualExtTemp = values[2];
                    if (actualExtTemp < deviceSettings_2.EmergencyMin_2 ||
                        actualExtTemp > deviceSettings_2.EmergencyMax_2)
                    {
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.BeginInvoke(new Action(() => {
                                // โค้ดสั่งหยุดฉุกเฉิน
                                Debug.WriteLine($"[UC2][EMERGENCY] External temperature out of range: {actualExtTemp:F2}");
                                // EmergencyStop();
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] OnDataReceived: {ex.Message}");
            }
        }

        private void ThresholdCheckTimer2_Tick(object sender, EventArgs e)
        {
            try
            {
                // ตรวจสอบสถานะพื้นฐาน
                if (!isStarted_2 || isPaused_2 || !waitingForThreshold_2 || waitStep_2 <= 0)
                {
                    Debug.WriteLine("[UC2] ThresholdCheckTimer2_Tick: Not in waiting state, stopping timer");
                    thresholdCheckTimer_2.Stop();
                    return;
                }

                // ตรวจสอบอุณหภูมิว่าถึงค่าที่กำหนดไว้หรือยัง
                double targetTemp = GetTargetTemp_2(waitStep_2);
                bool isTjMode = IsStepTargetTJ_2(waitStep_2);

                // ดึงค่าอุณหภูมิจาก UI2Values หรือ CurrentValues
                double currentTemp = 0;
                bool temperatureAvailable = false;

                // ลองดึงจาก UI2Values ก่อน (ค่าล่าสุดสำหรับ UI2)
                var ui2Values = SerialPortManager.Instance.UI2Values;
                if (ui2Values != null && ui2Values.Length >= 2)
                {
                    currentTemp = isTjMode ? ui2Values[1] : ui2Values[0]; // [0]=TR2, [1]=TJ2
                    temperatureAvailable = true;
                }
                // ถ้าไม่มีใน UI2Values ลองดึงจาก CurrentValues
                else
                {
                    var currentValues = SerialPortManager.Instance.CurrentValues;
                    if (currentValues != null && currentValues.Length >= 5)
                    {
                        currentTemp = isTjMode ? currentValues[4] : currentValues[3]; // [3]=TR2, [4]=TJ2
                        temperatureAvailable = true;
                    }
                }

                // ถ้ามีค่าอุณหภูมิ ตรวจสอบว่าถึงเป้าหมายหรือไม่
                if (temperatureAvailable)
                {
                    Debug.WriteLine($"[UC2] Threshold check: Step {waitStep_2}, {(isTjMode ? "TJ" : "TR")}={currentTemp:F2}, Target={targetTemp:F2}");

                    // ป้องกันค่าผิดปกติ
                    if (currentTemp > 500)
                    {
                        Debug.WriteLine($"[UC2][WARNING] Abnormal temperature: {currentTemp:F2}, ignoring");
                        return;
                    }

                    if (currentTemp >= targetTemp)
                    {
                        // ถึงค่าเป้าหมายแล้ว - เริ่มนับถอยหลัง
                        Debug.WriteLine($"[UC2] TEMPERATURE THRESHOLD REACHED! Starting countdown for Step {waitStep_2}");
                        waitingForThreshold_2 = false;
                        thresholdCheckTimer_2.Stop();
                        currentStepState = StepState_2.CountingDown;
                        countdownTimer_2.Start();
                        UpdateLampStatus_2();
                    }
                }
                else
                {
                    Debug.WriteLine($"[UC2] Waiting for temperature data for Step {waitStep_2}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] ThresholdCheckTimer2_Tick: {ex.Message}");
            }
        }
        private void ProgramState_SwitchChanged_2(object sender, EventArgs e)
        {
            if (InvokeRequired)
                BeginInvoke(new Action(() => switch_A_M2.IsOn = ProgramState_2.SwitchA_M2));
            else
                switch_A_M2.IsOn = ProgramState_2.SwitchA_M2;
        }
        private void ResetProgram_2()
        {
            // หยุด timers ทั้งหมด
            if (countdownTimer_2.Enabled)
                countdownTimer_2.Stop();
            if (thresholdCheckTimer_2.Enabled)
                thresholdCheckTimer_2.Stop();

            // รีเซ็ตตัวแปรสถานะ
            isStarted_2 = false;
            isPaused_2 = false;
            waitingForThreshold_2 = false;
            waitStep_2 = 0;
            ProgramState_2.IsStarted_2 = false;
            ProgramState_2.IsPaused_2 = false;
            ProgramState_2.CurrentStep_2 = 0;
            ProgramState_2.RemainingSeconds_2 = 0;

            // รีเซ็ตปุ่ม
            switch_system2_1.IsOn = false;
            switch_system2_2.IsOn = true;

            // รีเซ็ตสถานะหลอดไฟ
            ResetLampStatuses_2();
            UpdateLampStatus_2();

            Debug.WriteLine("[UC2] System reset completed");
        }

        /// <summary>รีเซ็ตสถานะหลอดไฟทุกสเต็ปเป็น “Wait”</summary>
        private void ResetLampStatuses_2()
        {
            completedSteps_2.Clear();
            for (int i = 1; i <= 8; i++)
                persistedLampStatuses_2[i] = "Wait";
        }
        private void TrySendInitialConfiguration_2()
        {
            // เช็ค deviceSettings2 ก่อน
            if (deviceSettings_2 == null)
                deviceSettings_2 = DeviceSettings_2.Load_2("Settings_2.xml");

            // หา control switch_Target2_1–2_2 จากชื่อ ถ้าไม่เจอให้เลิก
            var sw1 = Controls.Find("switch_Target2_1", true)
                              .FirstOrDefault() as ToggleSwitchControl;
            var sw2 = Controls.Find("switch_Target2_2", true)
                              .FirstOrDefault() as ToggleSwitchControl;
            if (sw1 == null || sw2 == null) return;

            // สร้าง frame Thermostat/TRTJ
            byte[] frame1 = CommandHelper.BuildDualThermostatTRTJ(
                sw1.IsOn, sw2.IsOn
            );
            SerialPortManager.Instance?.Send(frame1);

            // สร้าง frame Stirrer (สมมติเมธอดสากล)
            byte code2 = deviceSettings_2.GetStirrerCode_2(2);
            byte[] frame2 = CommandHelper.BuildSelectStirrerDevice(2, code2);
            SerialPortManager.Instance?.Send(frame2);
        }
        private void OnStepCompleted_2(int step)
        {
            completedSteps_2.Add(step);
            UpdateLampStatus_2();

            // หา Step ถัดไปที่เปิด toggle และยังไม่ทำ
            int next = Enumerable.Range(1, 8)
                        .FirstOrDefault(i =>
                            (Controls.Find($"toggle_2SwitchControl{i}", true)
                                     .First() as ToggleSwitchControl).IsOn
                            && !completedSteps_2.Contains(i));

            if (next > 0)
            {
                SendStepSetpointsToSerialAndDisplay_2(next);
                StartStepCountdown_2(next);
            }
            else
            {
                ResetProgram_2();
            }
        }
        private void OnUI_2DataReceived(double[] values)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnUI_2DataReceived(values)));
                return;
            }

            try
            {
                // อัปเดต UI จากค่าที่ได้รับ
                if (values != null && values.Length >= 4)
                {
                    // อัปเดตค่าที่แสดงบนหน้าจอ
                    // ...ส่วนอัปเดต UI ทั่วไป...

                    // เพิ่มส่วนนี้: บันทึกค่าล่าสุดและแสดง debug info
                    Debug.WriteLine($"[UC2][DATA] Received: TR2={values[0]:F2}, TJ2={values[1]:F2}, RPM2={values[2]:F0}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] OnUI2DataReceived: {ex.Message}");
            }
        }

        public static void ResetSet_2()
        {
            ProgramState_2.CurrentStep_2 = 0;
            ProgramState_2.RemainingSeconds_2 = 0;
            ProgramState_2.IsStarted_2 = false;
            ProgramState_2.IsPaused_2 = false;
          
            ProgramState_2.OnCountdownFinished_2();
        }





        // ──────────────────── helper: RegisterToggleHandlers ────────────────────
        private void RegisterToggleHandlers_2()
        {
            toggle_2SwitchControl1.Click += toggle_2SwitchControl1_Click;
            toggle_2SwitchControl2.Click += toggle_2SwitchControl2_Click;
            toggle_2SwitchControl3.Click += toggle_2SwitchControl3_Click;
            toggle_2SwitchControl4.Click += toggle_2SwitchControl4_Click;
            toggle_2SwitchControl5.Click += toggle_2SwitchControl5_Click;
            toggle_2SwitchControl6.Click += toggle_2SwitchControl6_Click;
            toggle_2SwitchControl7.Click += toggle_2SwitchControl7_Click;
            toggle_2SwitchControl8.Click += toggle_2SwitchControl8_Click;
        }

        /// <summary>
        /// ตรวจสอบค่า External PT100 ของ SET 2 เมื่อมีข้อมูลมาจาก SerialPort
        /// ถ้าอยู่นอกช่วง EmergencyMin2–EmergencyMax2 ให้สั่ง EmergencyStop()
        /// </summary>
      

        // ──────────────────── helper: SetupTextBoxArrays2 ────────────────────
        /// <summary>
        /// Must bind every Step-2 TextBox before any Validate call
        /// </summary>
        // ──────────────────── helper: SetupTextBoxArrays2 ────────────────────
        // ──────────────────── โซนแก้ไข SetupTextBoxArrays2 ────────────────────
        // ให้ใช้ชื่อ TextBox ตามที่มีใน Designer ของ UI2 เท่านั้น แล้วลบโค้ดเดิมที่อ้าง textBox_Target2_Tr* ออกทั้งหมด

        private void SetupTextBoxArrays_2()
        {
            tempBoxes_2 = new[]
            {
        text2_Temp1, text2_Temp2, text2_Temp3, text2_Temp4,
        text2_Temp5, text2_Temp6, text2_Temp7, text2_Temp8
    };

            rpmBoxes_2 = new[]
            {
        text2_RPM1, text2_RPM2, text2_RPM3, text2_RPM4,
        text2_RPM5, text2_RPM6, text2_RPM7, text2_RPM8
    };

            dosingBoxes_2 = new[]
            {
        text2_Dosing1, text2_Dosing2, text2_Dosing3, text2_Dosing4,
        text2_Dosing5, text2_Dosing6, text2_Dosing7, text2_Dosing8
    };

            timeHrBoxes_2 = new[]
            {
        text2_Hr1, text2_Hr2, text2_Hr3, text2_Hr4,
        text2_Hr5, text2_Hr6, text2_Hr7, text2_Hr8
    };

            timeMinBoxes_2 = new[]
            {
        text2_Min1, text2_Min2, text2_Min3, text2_Min4,
        text2_Min5, text2_Min6, text2_Min7, text2_Min8
    };
        }





        private bool CanStartSystem_2()
        {
            if (!switch_A_M2.IsOn)
            {
                MessageBox.Show("กรุณากดปุ่ม Auto เพื่อเริ่มระบบ", "คำเตือน",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        // ──────────────────── helper: FindFirstEnabledStep2 ────────────────────
        private int FindFirstEnabledStep_2()
        {
            for (int i = 1; i <= 8; i++)
                if (IsStepToggleOn_2(i))
                    return i;
            return 9; // ไม่มี Step ใดเปิด
        }

        private void RegisterSettingTextBoxes_2()
        {
            // สร้าง array ของ TextBox ที่ต้องลงทะเบียน
            TextBox[] boxes = new TextBox[]
            {
                // Temp
                text2_Temp1, text2_Temp2, text2_Temp3, text2_Temp4,
                text2_Temp5, text2_Temp6, text2_Temp7, text2_Temp8,
                // RPM
                text2_RPM1, text2_RPM2, text2_RPM3, text2_RPM4,
                text2_RPM5, text2_RPM6, text2_RPM7, text2_RPM8,
                // Dosing
                text2_Dosing1, text2_Dosing2, text2_Dosing3, text2_Dosing4,
                text2_Dosing5, text2_Dosing6, text2_Dosing7, text2_Dosing8
            };

            foreach (TextBox tb in boxes)
            {
                if (tb == null) continue;
                tb.MaxLength = 6; // จำกัดทศนิยมได้ตามต้องการ
                tb.TextChanged += (s, e) =>
                {
                    // อัปเดตค่าใน Data_Set1
                    UpdateDataSet1_2(tb.Name, tb.Text);
                    // บันทึกไฟล์ XML ทันที
                    settings_2.SaveToFile_2("Data_Set2.xml", "Combined");
                };
            }
        }

      

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateUIFromState();
        }

        private void ProgramState_StateChanged_2(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => UpdateUIFromState()));
            else
                UpdateUIFromState();
        }

        private void ProgramState_CountdownFinished_2(object sender, EventArgs e)
        {
            if (isStarted_2 && !ProgramState_2.IsPaused_2)
            {
                Debug.WriteLine("Countdown finished; moving to next step.");
                this.BeginInvoke(new Action(() => NextStep_2()));
            }
        }

        private void UpdateUIFromState()
        {
            settings_2.Toggle_2SwitchControl1 = toggle_2SwitchControl1.IsOn;
            settings_2.Toggle_2SwitchControl2 = toggle_2SwitchControl2.IsOn;
            settings_2.Toggle_2SwitchControl3 = toggle_2SwitchControl3.IsOn;
            settings_2.Toggle_2SwitchControl4 = toggle_2SwitchControl4.IsOn;
            settings_2.Toggle_2SwitchControl5 = toggle_2SwitchControl5.IsOn;
            settings_2.Toggle_2SwitchControl6 = toggle_2SwitchControl6.IsOn;
            settings_2.Toggle_2SwitchControl7 = toggle_2SwitchControl7.IsOn;
            settings_2.Toggle_2SwitchControl8 = toggle_2SwitchControl8.IsOn;

            switch_A_M2.IsOn = ProgramState_2.SwitchA_M2;

            if (!ProgramState_2.IsStarted_2 && !ProgramState_2.IsPaused_2)
            {
                switch_system2_1.IsOn = false;
                switch_system2_2.IsOn = true;
            }
            else if (ProgramState_2.IsStarted_2 && !ProgramState_2.IsPaused_2)
            {
                switch_system2_1.IsOn = true;
                switch_system2_2.IsOn = false;
            }
            else if (ProgramState_2.IsPaused_2)
            {
                switch_system2_1.IsOn = false;
                switch_system2_2.IsOn = true;
            }

            if (ProgramState_2.IsStarted_2 && !ProgramState_2.IsPaused_2)
            {
                TimeSpan gap = DateTime.Now - lastCountdownTick_2;
                int gapSeconds = (int)gap.TotalSeconds;
                if (gapSeconds > 0)
                {
                    ProgramState_2.RemainingSeconds_2 = Math.Max(ProgramState_2.RemainingSeconds_2 - gapSeconds, 0);
                    lastCountdownTick_2 = DateTime.Now;
                }
            }

            if (ProgramState_2.CurrentStep_2 > 0 && ProgramState_2.CurrentStep_2 != currentStep_2)
                currentStep_2 = ProgramState_2.CurrentStep_2;
            if (ProgramState_2.IsStarted_2)
                remainingSeconds_2 = ProgramState_2.RemainingSeconds_2;
            if (globalTotalStepSeconds_2 > 0)
                totalStepSeconds_2 = globalTotalStepSeconds_2;

            if (currentStep_2 > 0)
            {
                label_Process2Step.Text = currentStep_2.ToString();
                UpdatePicTarget_2(currentStep_2);
                UpdatePicModeTime_2(currentStep_2);
                UpdateSetpointLabels_2();
                Debug.WriteLine("UI restored from ProgramState: Step " + currentStep_2 + ", Remaining " + remainingSeconds_2 + " sec.");
            }
            UpdateLampStatus_2();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                // โหลด settings2, deviceSettings2 ให้พร้อม ถ้ายังไม่เคยโหลด
                if (deviceSettings_2 == null)
                    deviceSettings_2 = DeviceSettings_2.Load_2("Settings_2.xml");
                if (settings_2 == null)
                    settings_2 = Data_Set2.LoadFromFile_2("Data_Set2.xml")
                                ?? new Data_Set2();

                // สมัคร event ต่างๆ …
                SerialPortManager.Instance.UI2DataReceivedEvent += OnUI_2DataReceived;
                SerialPortManager.Instance.DataReceivedEvent += OnDataReceived_2;

                // ** เรียก Initial Configuration ที่นี่ แทนคอนสตรัคเตอร์ **
                TrySendInitialConfiguration_2();

                // … สตาร์ท timers, โหลด UI …
            }
            else
            {
                // … หยุด timers, save state …
            }
        }


        // ในไฟล์ UC_PROGRAM CONTROL SET 2.cs
        private void But_GoHome4_Click(object sender, EventArgs e)
        {
            try
            {
                // เปลี่ยนหน้าโดยใช้ NavigationHelper พร้อมส่ง cleanup action
                NavigationHelper.NavigateTo<UC_CONTROL_SET_2>(() => {
                    // หยุด timers ทั้งหมด
                    if (countdownTimer_2 != null) countdownTimer_2.Stop();
                    if (thresholdCheckTimer_2 != null) thresholdCheckTimer_2.Stop();
                    if (updateTimer_2 != null) updateTimer_2.Stop();
                    if (settingsUpdateTimer_2 != null) settingsUpdateTimer_2.Stop();

                    // ยกเลิกการลงทะเบียน event
                    if (SerialPortManager.Instance != null)
                    {
                        SerialPortManager.Instance.UI2DataReceivedEvent -= OnUI_2DataReceived;
                        SerialPortManager.Instance.DataReceivedEvent -= OnDataReceived_2;
                    }

                    // บันทึกสถานะ
                    SaveLampState_2();
                });

                Debug.WriteLine("[UC2] เปลี่ยนหน้าไปยัง UC_CONTROL_SET_2 สำเร็จ");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] But_GoHome4_Click: {ex.Message}");
                MessageBox.Show($"เกิดข้อผิดพลาดในการเปลี่ยนหน้า: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CleanupResources_2()
        {
            Debug.WriteLine("[UC2] CleanupResources started");

            try
            {
                // หยุดการทำงานทั้งหมดก่อน
                if (isStarted_2)
                {
                    ProgramState_2.Reset_Set2();
                    isStarted_2 = false;
                    isPaused_2 = false;
                }

                // หยุด timers
                if (countdownTimer_2 != null) countdownTimer_2.Stop();
                if (thresholdCheckTimer_2 != null) thresholdCheckTimer_2.Stop();
                if (updateTimer_2 != null) updateTimer_2.Stop();
                if (settingsUpdateTimer_2 != null) settingsUpdateTimer_2.Stop();

                // ยกเลิกการลงทะเบียน event
                if (SerialPortManager.Instance != null)
                {
                    SerialPortManager.Instance.UI2DataReceivedEvent -= OnUI_2DataReceived;
                    SerialPortManager.Instance.DataReceivedEvent -= OnDataReceived_2;
                }

                // บันทึกสถานะ
                SaveLampState_2();
                if (settings_2 != null)
                    settings_2.SaveToFile_2("Data_Set2.xml", "Combined");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] CleanupResources: {ex.Message}");
            }

            Debug.WriteLine("[UC2] CleanupResources completed");
        }
        private Main_Form1 FindMainForm_2()
        {
            try
            {
                // วิธีที่ 1: ตรวจสอบจาก ParentForm โดยตรง
                if (this.ParentForm is Main_Form1 mainForm1)
                    return mainForm1;

                // วิธีที่ 2: ตรวจสอบจาก Parent
                Control parent = this.Parent;
                while (parent != null)
                {
                    if (parent is Main_Form1 mainForm2)
                        return mainForm2;
                    parent = parent.Parent;
                }

                // วิธีที่ 3: ค้นหาจาก Application.OpenForms โดยใช้ fully qualified name
                foreach (Form form in System.Windows.Forms.Application.OpenForms)
                {
                    if (form is Main_Form1 mainForm3)
                        return mainForm3;
                }

                // วิธีที่ 4: ถ้ายังไม่พบ ลองค้นหาจาก Tag
                if (this.Tag is Main_Form1 mainForm4)
                    return mainForm4;

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] FindMainForm: {ex.Message}");
                return null;
            }
        }
        private void switch_A_M_ToggleChanged(object sender, EventArgs e)
        {
            ProgramState_2.SwitchA_M2 = switch_A_M2.IsOn;
        }



        // -----------------------------------------------------------------

        // *** ฟังก์ชันส่งคำสั่งโดยเรียงลำดับและเพิ่ม Delay ***
        // UC_PROGRAM_CONTROL_SET_2.cs
        // -----------------------------------------------------------------------------
        // เมธอด SendStepSetpointsToSerialAndDisplay ที่แก้ไขล่าสุด (UI2)
        // -----------------------------------------------------------------------------
        // ในไฟล์ UC_PROGRAM_CONTROL_SET_2.cs
        // ส่วน SendStepSetpointsToSerialAndDisplay ที่แก้ไข-เพิ่มเติมให้เหมือน UI1
        private void SendStepSetpointsToSerialAndDisplay_2(int step)
        {
            // --- อ่านค่าจาก UI2 (ชุด 2) ---
            float temp2 = 0f;
            int rpm2 = 0;
            bool thermo2IsTJ = false;
            switch (step)
            {
                case 1:
                    float.TryParse(text2_Temp1.Text, out temp2);
                    int.TryParse(text2_RPM1.Text, out rpm2);
                    thermo2IsTJ = switch_Target2_1.IsOn;
                    break;
                case 2:
                    float.TryParse(text2_Temp2.Text, out temp2);
                    int.TryParse(text2_RPM2.Text, out rpm2);
                    thermo2IsTJ = switch_Target2_2.IsOn;
                    break;
                case 3:
                    float.TryParse(text2_Temp3.Text, out temp2);
                    int.TryParse(text2_RPM3.Text, out rpm2);
                    thermo2IsTJ = switch_Target2_3.IsOn;
                    break;
                case 4:
                    float.TryParse(text2_Temp4.Text, out temp2);
                    int.TryParse(text2_RPM4.Text, out rpm2);
                    thermo2IsTJ = switch_Target2_4.IsOn;
                    break;
                case 5:
                    float.TryParse(text2_Temp5.Text, out temp2);
                    int.TryParse(text2_RPM5.Text, out rpm2);
                    thermo2IsTJ = switch_Target2_5.IsOn;
                    break;
                case 6:
                    float.TryParse(text2_Temp6.Text, out temp2);
                    int.TryParse(text2_RPM6.Text, out rpm2);
                    thermo2IsTJ = switch_Target2_6.IsOn;
                    break;
                case 7:
                    float.TryParse(text2_Temp7.Text, out temp2);
                    int.TryParse(text2_RPM7.Text, out rpm2);
                    thermo2IsTJ = switch_Target2_7.IsOn;
                    break;
                case 8:
                    float.TryParse(text2_Temp8.Text, out temp2);
                    int.TryParse(text2_RPM8.Text, out rpm2);
                    thermo2IsTJ = switch_Target2_8.IsOn;
                    break;
            }

            // --- ส่งคำสั่ง TR/TJ เฉพาะ UI2 (UI1 = false) ---
            byte[] trtjFrame = CommandHelper.BuildDualThermostatTRTJ(false, thermo2IsTJ);
            SerialPortManager.Instance.Send(trtjFrame);
            Debug.WriteLine($"[UI2] DualThermostatTRTJ Step{step} sent: {BitConverter.ToString(trtjFrame)}");
            Thread.Sleep(150);

            // --- แปลงค่า temp2, rpm2 → High/Low byte ---
            int t2 = (int)Math.Round(temp2);
            byte t2H = (byte)(t2 >> 8), t2L = (byte)(t2 & 0xFF);
            byte r2H = (byte)(rpm2 >> 8), r2L = (byte)(rpm2 & 0xFF);

            // เตรียมตัวแปรสำหรับเฟรม
            byte[] frame;
            byte[] rpmFrame;

            // --- 4) ส่ง SetTemp สำหรับ UI2 ตาม Step ---
            switch (step)
            {
                case 1:
                    frame = CommandHelper.BuildSet2_Temp2Step1(t2H, t2L);
                    SerialPortManager.Instance.Send(frame);
                    Debug.WriteLine($"[UI2] SetTemp  Step1 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 2:
                    frame = CommandHelper.BuildSet2_Temp2Step2(t2H, t2L);
                    SerialPortManager.Instance.Send(frame);
                    Debug.WriteLine($"[UI2] SetTemp  Step2 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 3:
                    frame = CommandHelper.BuildSet2_Temp2Step3(t2H, t2L);
                    SerialPortManager.Instance.Send(frame);
                    Debug.WriteLine($"[UI2] SetTemp  Step3 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 4:
                    frame = CommandHelper.BuildSet2_Temp2Step4(t2H, t2L);
                    SerialPortManager.Instance.Send(frame);
                    Debug.WriteLine($"[UI2] SetTemp  Step4 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 5:
                    frame = CommandHelper.BuildSet2_Temp2Step5(t2H, t2L);
                    SerialPortManager.Instance.Send(frame);
                    Debug.WriteLine($"[UI2] SetTemp  Step5 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 6:
                    frame = CommandHelper.BuildSet2_Temp2Step6(t2H, t2L);
                    SerialPortManager.Instance.Send(frame);
                    Debug.WriteLine($"[UI2] SetTemp  Step6 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 7:
                    frame = CommandHelper.BuildSet2_Temp2Step7(t2H, t2L);
                    SerialPortManager.Instance.Send(frame);
                    Debug.WriteLine($"[UI2] SetTemp  Step7 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                default: // case 8
                    frame = CommandHelper.BuildSet2_Temp2Step8(t2H, t2L);
                    SerialPortManager.Instance.Send(frame);
                    Debug.WriteLine($"[UI2] SetTemp  Step8 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
            }

            // --- 5) ส่ง SetRPM สำหรับ UI2 ตาม Step ---
            switch (step)
            {
                case 1:
                    rpmFrame = CommandHelper.BuildSet2_RPM2Step1(r2H, r2L);
                    SerialPortManager.Instance.Send(rpmFrame);
                    Debug.WriteLine($"[UI2] SetRPM   Step1 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 2:
                    rpmFrame = CommandHelper.BuildSet2_RPM2Step2(r2H, r2L);
                    SerialPortManager.Instance.Send(rpmFrame);
                    Debug.WriteLine($"[UI2] SetRPM   Step2 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 3:
                    rpmFrame = CommandHelper.BuildSet2_RPM2Step3(r2H, r2L);
                    SerialPortManager.Instance.Send(rpmFrame);
                    Debug.WriteLine($"[UI2] SetRPM   Step3 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 4:
                    rpmFrame = CommandHelper.BuildSet2_RPM2Step4(r2H, r2L);
                    SerialPortManager.Instance.Send(rpmFrame);
                    Debug.WriteLine($"[UI2] SetRPM   Step4 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 5:
                    rpmFrame = CommandHelper.BuildSet2_RPM2Step5(r2H, r2L);
                    SerialPortManager.Instance.Send(rpmFrame);
                    Debug.WriteLine($"[UI2] SetRPM   Step5 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 6:
                    rpmFrame = CommandHelper.BuildSet2_RPM2Step6(r2H, r2L);
                    SerialPortManager.Instance.Send(rpmFrame);
                    Debug.WriteLine($"[UI2] SetRPM   Step6 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 7:
                    rpmFrame = CommandHelper.BuildSet2_RPM2Step7(r2H, r2L);
                    SerialPortManager.Instance.Send(rpmFrame);
                    Debug.WriteLine($"[UI2] SetRPM   Step7 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                default: // case 8
                    rpmFrame = CommandHelper.BuildSet2_RPM2Step8(r2H, r2L);
                    SerialPortManager.Instance.Send(rpmFrame);
                    Debug.WriteLine($"[UI2] SetRPM   Step8 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
            }

            // --- 6) อัปเดต Label บน UI ---
            UpdateSetpointLabels_2();
        }
        // ──────────────────── ให้เหลือเมทอดนี้แค่ครั้งเดียว ────────────────────
       



        // -----------------------------------------------------------------------------

        private bool ValidateAllStepInputs_2()
        {
            bool valid = true;
            for (int step = 1; step <= 8; step++)
            {
                var tbTemp = (TextBox)Controls.Find($"text2_Temp{step}", true).FirstOrDefault();
                var tbRPM = (TextBox)Controls.Find($"text2_RPM{step}", true).FirstOrDefault();
                var tbDosing = (TextBox)Controls.Find($"text2_Dosing{step}", true).FirstOrDefault();

                if (tbTemp != null && float.TryParse(tbTemp.Text, out float temp))
                {
                    if (temp < deviceSettings_2.TempMin_2 || temp > deviceSettings_2.TempMax_2)
                    {
                        errorProvider_2.SetError(tbTemp,
                            $"อุณหภูมิต้องอยู่ระหว่าง {deviceSettings_2.TempMin_2} ถึง {deviceSettings_2.TempMax_2}");
                        valid = false;
                    }
                    else errorProvider_2.SetError(tbTemp, "");
                }
                if (tbRPM != null && int.TryParse(tbRPM.Text, out int rpm))
                {
                    if (rpm < deviceSettings_2.StirMin_2 || rpm > deviceSettings_2.StirMax_2)
                    {
                        errorProvider_2.SetError(tbRPM,
                            $"RPM ต้องอยู่ระหว่าง {deviceSettings_2.StirMin_2} ถึง {deviceSettings_2.StirMax_2}");
                        valid = false;
                    }
                    else errorProvider_2.SetError(tbRPM, "");
                }
                if (tbDosing != null && float.TryParse(tbDosing.Text, out float dosing))
                {
                    if (dosing < deviceSettings_2.DosingMin_2 || dosing > deviceSettings_2.DosingMax_2)
                    {
                        errorProvider_2.SetError(tbDosing,
                            $"Dosing ต้องอยู่ระหว่าง {deviceSettings_2.DosingMin_2} ถึง {deviceSettings_2.DosingMax_2}");
                        valid = false;
                    }
                    else errorProvider_2.SetError(tbDosing, "");
                }
            }
            return valid;
        }
        private void ResetAllSteps_2()
        {
            countdownTimer_2.Stop();
            ProgramState_2.Reset_Set2();    // แยกจาก Reset() ของ UI1 แต่โครงคล้ายกัน
            completedSteps_2.Clear();
            currentStep_2 = 0;
            remainingSeconds_2 = 0;
            UpdateUIFromState();
            Debug.WriteLine("[UI2] ResetAllSteps2 called");
        }


        // ───────────────────────────────────────────────────────────────────────────
        // 3) NextStep (UI2) – Mirror NextStep ของ UI1
        // ───────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// เลื่อนไปยัง Step ถัดไปที่ถูกเปิดไว้ หรือหยุดโปรแกรมถ้าไม่มี
        /// </summary>
        private void NextStep_2()
        {
            // ทำเครื่องหมายว่า step ปัจจุบันเสร็จแล้ว
            completedSteps_2.Add(currentStep_2);
            UpdateLampStatus_2();

            // ประกาศตัวแปร nextStep เพื่อค้นหา step ต่อไปที่ toggle เปิดอยู่และยังไม่เสร็จ
            int nextStep = Enumerable.Range(1, 8)
                .FirstOrDefault(i =>
                {
                    var toggle = Controls
                        .Find($"toggle_2SwitchControl{i}", true)
                        .FirstOrDefault() as ToggleSwitchControl;
                    return toggle != null
                           && toggle.IsOn
                           && !completedSteps_2.Contains(i);
                });

            if (nextStep > 0)
            {
                // เรียกใช้ StartStepCountdown2 เพื่อเซ็ต WAIT/RUN และเริ่มนับต่อ
                SendStepSetpointsToSerialAndDisplay_2(nextStep);
                StartStepCountdown_2(nextStep);
            }
            else
            {
                // ไม่มี step ถัดไป → รีเซ็ตโปรแกรมกลับสู่ Idle
                ResetProgram_2();
            }
        }


        private bool IsStepToggleOn_2(int step)
        {
            switch (step)
            {
                case 1: return toggle_2SwitchControl1.IsOn;
                case 2: return toggle_2SwitchControl2.IsOn;
                case 3: return toggle_2SwitchControl3.IsOn;
                case 4: return toggle_2SwitchControl4.IsOn;
                case 5: return toggle_2SwitchControl5.IsOn;
                case 6: return toggle_2SwitchControl6.IsOn;
                case 7: return toggle_2SwitchControl7.IsOn;
                case 8: return toggle_2SwitchControl8.IsOn;
                default: return false;
            }
        }

        private void DisplayStepsStatus_2()
        {
            for (int step = 1; step <= 8; step++)
            {
                if (IsStepToggleOn_2(step))
                {
                    int secs = (step == currentStep_2) ? ProgramState_2.RemainingSeconds_2 : GetStepCountdownSeconds_2(step);
                    Debug.WriteLine("Status: Step " + step + " countdown = " + secs + " seconds.");
                }
            }
        }

        // *** Modified GetStepCountdownSeconds function with ErrorProvider validation ***
        private int GetStepCountdownSeconds_2(int step)
        {

            int hr = 0, min = 0;
            switch (step)
            {
                case 1:
                    if (!int.TryParse(text2_Hr1.Text, out hr))
                    {
                        errorProvider_2.SetError(text2_Hr1, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                        Debug.WriteLine("Step 1: Failed to parse Hr from text1_Hr1. Using saved value: " + settings_2.Text2_Hr1);
                        int.TryParse(settings_2.Text2_Hr1, out hr);
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Hr1, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_2.SetError(text2_Hr1, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text2_Min2.Text, out min))
                    {
                        errorProvider_2.SetError(text2_Min1, "กรุณากรอกตัวเลขสำหรับนาที");
                        Debug.WriteLine("Step 1: Failed to parse Min from text1_Min1. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Min1, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_2.SetError(text2_Min1, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 2:
                    if (!int.TryParse(text2_Hr2.Text, out hr))
                    {
                        errorProvider_2.SetError(text2_Hr2, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                        Debug.WriteLine("Step 2: Failed to parse Hr from text2_Hr2. Using saved value: " + settings_2.Text2_Hr2);
                        int.TryParse(settings_2.Text2_Hr2, out hr);
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Hr2, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_2.SetError(text2_Hr2, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text2_Min2.Text, out min))
                    {
                        errorProvider_2.SetError(text2_Min2, "กรุณากรอกตัวเลขสำหรับนาที");
                        Debug.WriteLine("Step 2: Failed to parse Min from text2_Min2. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Min2, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_2.SetError(text2_Min2, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 3:
                    if (!int.TryParse(text2_Hr3.Text, out hr))
                    {
                        errorProvider_2.SetError(text2_Hr3, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                        Debug.WriteLine("Step 3: Failed to parse Hr from text1_Hr3. Using saved value: " + settings_2.Text2_Hr3);
                        int.TryParse(settings_2.Text2_Hr3, out hr);
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Hr3, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_2.SetError(text2_Hr3, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text2_Min3.Text, out min))
                    {
                        errorProvider_2.SetError(text2_Min3, "กรุณากรอกตัวเลขสำหรับนาที");
                        Debug.WriteLine("Step 3: Failed to parse Min from text1_Min3. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Min3, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_2.SetError(text2_Min3, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 4:
                    if (!int.TryParse(text2_Hr4.Text, out hr))
                    {
                        errorProvider_2.SetError(text2_Hr4, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                        Debug.WriteLine("Step 4: Failed to parse Hr from text1_Hr4. Using saved value: " + settings_2.Text2_Hr4);
                        int.TryParse(settings_2.Text2_Hr4, out hr);
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Hr4, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_2.SetError(text2_Hr4, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text2_Min4.Text, out min))
                    {
                        errorProvider_2.SetError(text2_Min4, "กรุณากรอกตัวเลขสำหรับนาที");
                        Debug.WriteLine("Step 4: Failed to parse Min from text1_Min4. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Min4, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_2.SetError(text2_Min4, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 5:
                    if (!int.TryParse(text2_Hr5.Text, out hr))
                    {
                        errorProvider_2.SetError(text2_Hr5, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                        Debug.WriteLine("Step 5: Failed to parse Hr from text1_Hr5. Using saved value: " + settings_2.Text2_Hr5);
                        int.TryParse(settings_2.Text2_Hr5, out hr);
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Hr5, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_2.SetError(text2_Hr5, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text2_Min5.Text, out min))
                    {
                        errorProvider_2.SetError(text2_Min5, "กรุณากรอกตัวเลขสำหรับนาที");
                        Debug.WriteLine("Step 5: Failed to parse Min from text1_Min5. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Min5, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_2.SetError(text2_Min5, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 6:
                    if (!int.TryParse(text2_Hr6.Text, out hr))
                    {
                        errorProvider_2.SetError(text2_Hr6, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                        Debug.WriteLine("Step 6: Failed to parse Hr from text1_Hr6. Using saved value: " + settings_2.Text2_Hr6);
                        int.TryParse(settings_2.Text2_Hr6, out hr);
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Hr6, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_2.SetError(text2_Hr6, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text2_Min6.Text, out min))
                    {
                        errorProvider_2.SetError(text2_Min6, "กรุณากรอกตัวเลขสำหรับนาที");
                        Debug.WriteLine("Step 6: Failed to parse Min from text1_Min6. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Min6, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_2.SetError(text2_Min6, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 7:
                    if (!int.TryParse(text2_Hr7.Text, out hr))
                    {
                        errorProvider_2.SetError(text2_Hr7, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                        Debug.WriteLine("Step 7: Failed to parse Hr from text1_Hr7. Using saved value: " + settings_2.Text2_Hr7);
                        int.TryParse(settings_2.Text2_Hr7, out hr);
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Hr7, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_2.SetError(text2_Hr7, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text2_Min7.Text, out min))
                    {
                        errorProvider_2.SetError(text2_Min7, "กรุณากรอกตัวเลขสำหรับนาที");
                        Debug.WriteLine("Step 7: Failed to parse Min from text1_Min7. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Min7, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_2.SetError(text2_Min7, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 8:
                    if (!int.TryParse(text2_Hr8.Text, out hr))
                    {
                        errorProvider_2.SetError(text2_Hr8, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                        Debug.WriteLine("Step 8: Failed to parse Hr from text1_Hr8. Using saved value: " + settings_2.Text2_Hr8);
                        int.TryParse(settings_2.Text2_Hr8, out hr);
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Hr8, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_2.SetError(text2_Hr8, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text2_Min8.Text, out min))
                    {
                        errorProvider_2.SetError(text2_Min8, "กรุณากรอกตัวเลขสำหรับนาที");
                        Debug.WriteLine("Step 8: Failed to parse Min from text1_Min8. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_2.SetError(text2_Min8, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_2.SetError(text2_Min8, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                default:
                    Debug.WriteLine("GetStepCountdownSeconds: Invalid step number " + step);
                    break;
            }
            int totalSeconds = hr * 3600 + min * 60;
            Debug.WriteLine($"Step {step}: Parsed Hr = {hr}, Min = {min} --> Total seconds = {totalSeconds}");
            return totalSeconds;
        }
        // --- End Modified GetStepCountdownSeconds ***

        private void UpdateSetpointLabels_2()
        {
            if ((DateTime.Now - lastSetpointUpdateTime_2).TotalMilliseconds < 1000)
                return;
            lastSetpointUpdateTime_2 = DateTime.Now;

            double temp = 0, rpm = 0, dosing = 0;
            switch (currentStep_2)
            {
                case 1:
                    temp = (settings_2.Target2_Tj1 != 0 ? settings_2.Target2_Tj1 : settings_2.Target2_Tr1);
                    double.TryParse(settings_2.Text2_RPM1, out rpm);
                    double.TryParse(settings_2.Text2_Dosing1, out dosing);
                    break;
                case 2:
                    temp = (settings_2.Target2_Tj2 != 0 ? settings_2.Target2_Tj2 : settings_2.Target2_Tr2);
                    double.TryParse(settings_2.Text2_RPM2, out rpm);
                    double.TryParse(settings_2.Text2_Dosing2, out dosing);
                    break;
                case 3:
                    temp = (settings_2.Target2_Tj3 != 0 ? settings_2.Target2_Tj3 : settings_2.Target2_Tr3);
                    double.TryParse(settings_2.Text2_RPM3, out rpm);
                    double.TryParse(settings_2.Text2_Dosing3, out dosing);
                    break;
                case 4:
                    temp = (settings_2.Target2_Tj4 != 0 ? settings_2.Target2_Tj4 : settings_2.Target2_Tr4);
                    double.TryParse(settings_2.Text2_RPM4, out rpm);
                    double.TryParse(settings_2.Text2_Dosing4, out dosing);
                    break;
                case 5:
                    temp = (settings_2.Target2_Tj5 != 0 ? settings_2.Target2_Tj5 : settings_2.Target2_Tr5);
                    double.TryParse(settings_2.Text2_RPM5, out rpm);
                    double.TryParse(settings_2.Text2_Dosing5, out dosing);
                    break;
                case 6:
                    temp = (settings_2.Target2_Tj6 != 0 ? settings_2.Target2_Tj6 : settings_2.Target2_Tr6);
                    double.TryParse(settings_2.Text2_RPM6, out rpm);
                    double.TryParse(settings_2.Text2_Dosing6, out dosing);
                    break;
                case 7:
                    temp = (settings_2.Target2_Tj7 != 0 ? settings_2.Target2_Tj7 : settings_2.Target2_Tr7);
                    double.TryParse(settings_2.Text2_RPM7, out rpm);
                    double.TryParse(settings_2.Text2_Dosing7, out dosing);
                    break;
                case 8:
                    temp = (settings_2.Target2_Tj8 != 0 ? settings_2.Target2_Tj8 : settings_2.Target2_Tr8);
                    double.TryParse(settings_2.Text2_RPM8, out rpm);
                    double.TryParse(settings_2.Text2_Dosing8, out dosing);
                    break;
            }
            label_Process2_TempStep.Text = temp.ToString("F2");
            label_Process2RPM_SP.Text = rpm.ToString("F2");
            label_Process2Dosing.Text = dosing.ToString("F2");
            Debug.WriteLine("UpdateSetpointLabels => currentStep2=" + currentStep_2);
        }

        private void UpdatePicTarget_2(int step)
        {
            bool isTj = false;
            switch (step)
            {
                case 1: isTj = switch_Target2_1.IsOn; break;
                case 2: isTj = switch_Target2_2.IsOn; break;
                case 3: isTj = switch_Target2_3.IsOn; break;
                case 4: isTj = switch_Target2_4.IsOn; break;
                case 5: isTj = switch_Target2_5.IsOn; break;
                case 6: isTj = switch_Target2_6.IsOn; break;
                case 7: isTj = switch_Target2_7.IsOn; break;
                case 8: isTj = switch_Target2_8.IsOn; break;
            }
            pic_Target2.Mode = isTj ? "TJ" : "TR";
        }

        private void UpdatePicModeTime_2(int step)
        {
            bool timeOn = false;
            switch (step)
            {
                case 1: timeOn = switch_Time2_1.IsOn; break;
                case 2: timeOn = switch_Time2_2.IsOn; break;
                case 3: timeOn = switch_Time2_3.IsOn; break;
                case 4: timeOn = switch_Time2_4.IsOn; break;
                case 5: timeOn = switch_Time2_5.IsOn; break;
                case 6: timeOn = switch_Time2_6.IsOn; break;
                case 7: timeOn = switch_Time2_7.IsOn; break;
                case 8: timeOn = switch_Time2_8.IsOn; break;
            }
            pic_ModeTime2.Mode = timeOn ? "Run" : "Wait";
        }

        // --- Update Functions สำหรับแต่ละ Step ---
        private void UpdateStep1_2()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text2_Temp1.Text, out tempVal);
            int.TryParse(text2_RPM1.Text, out rpmVal);
            float.TryParse(text2_Dosing1.Text, out dosingVal);
            int.TryParse(text2_Hr1.Text, out hrVal);
            int.TryParse(text2_Min1.Text, out minVal);
            settings_2.Text2_Temp1 = tempVal.ToString();
            settings_2.Text2_RPM1 = rpmVal.ToString();
            settings_2.Text2_Dosing1 = dosingVal.ToString();
            settings_2.Text2_Hr1 = hrVal.ToString();
            settings_2.Text2_Min1 = minVal.ToString();
            Debug.WriteLine("Step 1: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            Target2_Tr1 = (switch_Target2_1.IsOn) ? 0 : tempVal;
            Target2_Tj1 = (switch_Target2_1.IsOn) ? tempVal : 0;
        }

        private void UpdateStep2_2()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text2_Temp2.Text, out tempVal);
            int.TryParse(text2_RPM2.Text, out rpmVal);
            float.TryParse(text2_Dosing2.Text, out dosingVal);
            int.TryParse(text2_Hr2.Text, out hrVal);
            int.TryParse(text2_Min2.Text, out minVal);
            settings_2.Text2_Temp2 = tempVal.ToString();
            settings_2.Text2_RPM2 = rpmVal.ToString();
            settings_2.Text2_Dosing2 = dosingVal.ToString();
            settings_2.Text2_Hr2 = hrVal.ToString();
            settings_2.Text2_Min2 = minVal.ToString();
            Debug.WriteLine("Step 2: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target2_2.IsOn)
            {
                Target2_Tj2 = tempVal;
                Target2_Tr2 = 0;
            }
            else
            {
                Target2_Tr2 = tempVal;
                Target2_Tj2 = 0;
            }
        }

        private void UpdateStep3_2()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text2_Temp3.Text, out tempVal);
            int.TryParse(text2_RPM3.Text, out rpmVal);
            float.TryParse(text2_Dosing3.Text, out dosingVal);
            int.TryParse(text2_Hr3.Text, out hrVal);
            int.TryParse(text2_Min3.Text, out minVal);
            settings_2.Text2_Temp3 = tempVal.ToString();
            settings_2.Text2_RPM3 = rpmVal.ToString();
            settings_2.Text2_Dosing3 = dosingVal.ToString();
            settings_2.Text2_Hr3 = hrVal.ToString();
            settings_2.Text2_Min3 = minVal.ToString();
            Debug.WriteLine("Step 3: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target2_3.IsOn)
            {
                Target2_Tj3 = tempVal;
                Target2_Tr3 = 0;
            }
            else
            {
                Target2_Tr3 = tempVal;
                Target2_Tj3 = 0;
            }
        }

        private void UpdateStep4_2()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text2_Temp4.Text, out tempVal);
            int.TryParse(text2_RPM4.Text, out rpmVal);
            float.TryParse(text2_Dosing4.Text, out dosingVal);
            int.TryParse(text2_Hr4.Text, out hrVal);
            int.TryParse(text2_Min4.Text, out minVal);
            settings_2.Text2_Temp4 = tempVal.ToString();
            settings_2.Text2_RPM4 = rpmVal.ToString();
            settings_2.Text2_Dosing4 = dosingVal.ToString();
            settings_2.Text2_Hr4 = hrVal.ToString();
            settings_2.Text2_Min4 = minVal.ToString();
            Debug.WriteLine("Step 4: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target2_4.IsOn)
            {
                Target2_Tj4 = tempVal;
                Target2_Tr4 = 0;
            }
            else
            {
                Target2_Tr4 = tempVal;
                Target2_Tj4 = 0;
            }
        }

        private void UpdateStep5_2()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text2_Temp5.Text, out tempVal);
            int.TryParse(text2_RPM5.Text, out rpmVal);
            float.TryParse(text2_Dosing5.Text, out dosingVal);
            int.TryParse(text2_Hr5.Text, out hrVal);
            int.TryParse(text2_Min5.Text, out minVal);
            settings_2.Text2_Temp5 = tempVal.ToString();
            settings_2.Text2_RPM5 = rpmVal.ToString();
            settings_2.Text2_Dosing5 = dosingVal.ToString();
            settings_2.Text2_Hr5 = hrVal.ToString();
            settings_2.Text2_Min5 = minVal.ToString();
            Debug.WriteLine("Step 5: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target2_5.IsOn)
            {
                Target2_Tj5 = tempVal;
                Target2_Tr5 = 0;
            }
            else
            {
                Target2_Tr5 = tempVal;
                Target2_Tj5 = 0;
            }
        }

        private void UpdateStep6_2()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text2_Temp6.Text, out tempVal);
            int.TryParse(text2_RPM6.Text, out rpmVal);
            float.TryParse(text2_Dosing6.Text, out dosingVal);
            int.TryParse(text2_Hr6.Text, out hrVal);
            int.TryParse(text2_Min6.Text, out minVal);
            settings_2.Text2_Temp6 = tempVal.ToString();
            settings_2.Text2_RPM6 = rpmVal.ToString();
            settings_2.Text2_Dosing6 = dosingVal.ToString();
            settings_2.Text2_Hr6 = hrVal.ToString();
            settings_2.Text2_Min6 = minVal.ToString();
            Debug.WriteLine("Step 6: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target2_6.IsOn)
            {
                Target2_Tj6 = tempVal;
                Target2_Tr6 = 0;
            }
            else
            {
                Target2_Tr6 = tempVal;
                Target2_Tj6 = 0;
            }
        }

        private void UpdateStep7_2()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text2_Temp7.Text, out tempVal);
            int.TryParse(text2_RPM7.Text, out rpmVal);
            float.TryParse(text2_Dosing7.Text, out dosingVal);
            int.TryParse(text2_Hr7.Text, out hrVal);
            int.TryParse(text2_Min7.Text, out minVal);
            settings_2.Text2_Temp7 = tempVal.ToString();
            settings_2.Text2_RPM7 = rpmVal.ToString();
            settings_2.Text2_Dosing7 = dosingVal.ToString();
            settings_2.Text2_Hr7 = hrVal.ToString();
            settings_2.Text2_Min7 = minVal.ToString();
            Debug.WriteLine("Step 7: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target2_7.IsOn)
            {
                Target2_Tj7 = tempVal;
                Target2_Tr7 = 0;
            }
            else
            {
                Target2_Tr7 = tempVal;
                Target2_Tj7 = 0;
            }
        }

        private void UpdateStep8_2()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text2_Temp8.Text, out tempVal);
            int.TryParse(text2_RPM8.Text, out rpmVal);
            float.TryParse(text2_Dosing8.Text, out dosingVal);
            int.TryParse(text2_Hr8.Text, out hrVal);
            int.TryParse(text2_Min8.Text, out minVal);
            settings_2.Text2_Temp8 = tempVal.ToString();
            settings_2.Text2_RPM8 = rpmVal.ToString();
            settings_2.Text2_Dosing8 = dosingVal.ToString();
            settings_2.Text2_Hr8 = hrVal.ToString();
            settings_2.Text2_Min8 = minVal.ToString();
            Debug.WriteLine("Step 8: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target2_8.IsOn)
            {
                Target2_Tj8 = tempVal;
                Target2_Tr8 = 0;
            }
            else
            {
                Target2_Tr8 = tempVal;
                Target2_Tj8 = 0;
            }
        }
        // --- End Update Functions ---

        // --- Toggle event handlers สำหรับแต่ละ Step ---
        private void toggle_2SwitchControl1_Click(object sender, EventArgs e)
        {
            settings_2.Toggle_2SwitchControl1 = toggle_2SwitchControl1.IsOn;
            settings_2.Switch_Target2_1 = switch_Target2_1.IsOn;
            settings_2.Switch_Time2_1 = switch_Time2_1.IsOn;
            if (toggle_2SwitchControl1.IsOn)
            {
                UpdateStep1_2();
                settings_2.Target2_Tr1 = Target2_Tr1;
                settings_2.Target2_Tj1 = Target2_Tj1;
                Debug.WriteLine("Step 1 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl1 is OFF.");
            }
            settings_2.SaveToFile_2("Data_Set2.xml", "Step1");
        }

        private void toggle_2SwitchControl2_Click(object sender, EventArgs e)
        {
            settings_2.Toggle_2SwitchControl2 = toggle_2SwitchControl2.IsOn;
            settings_2.Switch_Target2_2 = switch_Target2_2.IsOn;
            settings_2.Switch_Time2_2 = switch_Time2_2.IsOn;
            if (toggle_2SwitchControl2.IsOn)
            {
                UpdateStep2_2();
                settings_2.Target2_Tr2 = Target2_Tr2;
                settings_2.Target2_Tj2 = Target2_Tj2;
                Debug.WriteLine("Step 2 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl2 is OFF.");
            }
            settings_2.SaveToFile_2("Data_Set2.xml", "Step2");
        }

        private void toggle_2SwitchControl3_Click(object sender, EventArgs e)
        {
            settings_2.Toggle_2SwitchControl3 = toggle_2SwitchControl3.IsOn;
            settings_2.Switch_Target2_3 = switch_Target2_3.IsOn;
            settings_2.Switch_Time2_3 = switch_Time2_3.IsOn;
            if (toggle_2SwitchControl3.IsOn)
            {
                UpdateStep3_2();
                settings_2.Target2_Tr3 = Target2_Tr3;
                settings_2.Target2_Tj3 = Target2_Tj3;
                Debug.WriteLine("Step 3 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl3 is OFF.");
            }
            settings_2.SaveToFile_2("Data_Set2.xml", "Step3");
        }

        private void toggle_2SwitchControl4_Click(object sender, EventArgs e)
        {
            settings_2.Toggle_2SwitchControl4 = toggle_2SwitchControl4.IsOn;
            settings_2.Switch_Target2_4 = switch_Target2_4.IsOn;
            settings_2.Switch_Time2_4 = switch_Time2_4.IsOn;
            if (toggle_2SwitchControl4.IsOn)
            {
                UpdateStep4_2();
                settings_2.Target2_Tr4 = Target2_Tr4;
                settings_2.Target2_Tj4 = Target2_Tj4;
                Debug.WriteLine("Step 4 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl4 is OFF.");
            }
            settings_2.SaveToFile_2("Data_Set2.xml", "Step4");
        }

        private void toggle_2SwitchControl5_Click(object sender, EventArgs e)
        {
            settings_2.Toggle_2SwitchControl5 = toggle_2SwitchControl5.IsOn;
            settings_2.Switch_Target2_5 = switch_Target2_5.IsOn;
            settings_2.Switch_Time2_5 = switch_Time2_5.IsOn;
            if (toggle_2SwitchControl5.IsOn)
            {
                UpdateStep5_2();
                settings_2.Target2_Tr5 = Target2_Tr5;
                settings_2.Target2_Tj5 = Target2_Tj5;
                Debug.WriteLine("Step 5 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl5 is OFF.");
            }
            settings_2.SaveToFile_2("Data_Set2.xml", "Step5");
        }

        private void toggle_2SwitchControl6_Click(object sender, EventArgs e)
        {
            settings_2.Toggle_2SwitchControl6 = toggle_2SwitchControl6.IsOn;
            settings_2.Switch_Target2_6 = switch_Target2_6.IsOn;
            settings_2.Switch_Time2_6 = switch_Time2_6.IsOn;
            if (toggle_2SwitchControl6.IsOn)
            {
                UpdateStep6_2();
                settings_2.Target2_Tr6 = Target2_Tr6;
                settings_2.Target2_Tj6 = Target2_Tj6;
                Debug.WriteLine("Step 6 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl6 is OFF.");
            }
            settings_2.SaveToFile_2("Data_Set2.xml", "Step6");
        }

        private void toggle_2SwitchControl7_Click(object sender, EventArgs e)
        {
            settings_2.Toggle_2SwitchControl7 = toggle_2SwitchControl7.IsOn;
            settings_2.Switch_Target2_7 = switch_Target2_7.IsOn;
            settings_2.Switch_Time2_7 = switch_Time2_7.IsOn;
            if (toggle_2SwitchControl7.IsOn)
            {
                UpdateStep7_2();
                settings_2.Target2_Tr7 = Target2_Tr7;
                settings_2.Target2_Tj7 = Target2_Tj7;
                Debug.WriteLine("Step 7 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl7 is OFF.");
            }
            settings_2.SaveToFile_2("Data_Set2.xml", "Step7");
        }

        private void toggle_2SwitchControl8_Click(object sender, EventArgs e)
        {
            settings_2.Toggle_2SwitchControl8 = toggle_2SwitchControl8.IsOn;
            settings_2.Switch_Target2_8 = switch_Target2_8.IsOn;
            settings_2.Switch_Time2_8 = switch_Time2_8.IsOn;
            if (toggle_2SwitchControl8.IsOn)
            {
                UpdateStep8_2();
                settings_2.Target2_Tr8 = Target2_Tr8;
                settings_2.Target2_Tj8 = Target2_Tj8;
                Debug.WriteLine("Step 8 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_2SwitchControl8 is OFF.");
            }
            settings_2.SaveToFile_2("Data_Set2.xml", "Step8");
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (this.ParentForm != null)
                this.ParentForm.FormClosing += ParentForm_FormClosing_2;
        }

        private void ParentForm_FormClosing_2(object sender, FormClosingEventArgs e)
        {
            // บันทึก Lamp State และ settings ทั้งหมดก่อนโปรแกรมปิด
            SaveLampState_2();
            settings_2.SaveToFile_2("Data_Set2.xml", "Combined");
            Debug.WriteLine("Settings saved on closing.");
        }

        private void UpdateTimer_Tick_2(object sender, EventArgs e)
        {
            try
            {
                // ดึงค่าจาก SerialPortManager
                var ui = SerialPortManager.Instance.UI2Values;
                if (ui != null && ui.Length >= 4)
                {
                    double tr2 = ui[0];   // TR2
                    double tj2 = ui[1];   // TJ2
                    double rpm2 = ui[2];   // RPM2
                    double diff = tr2 - tj2;

                    // อัพเดต UI เฉพาะส่วนอุณหภูมิและ RPM
                    label_TR2.Text = tr2.ToString("F2");
                    label_TJ2.Text = tj2.ToString("F2");
                    label_TR_TJ2.Text = diff.ToString("F2");
                    label_RPM2.Text = rpm2.ToString("F0");

                    // ตรวจสอบซ้ำในกรณีกำลังรอ threshold (เพิ่มความมั่นใจ)
                    if (currentStepState == StepState_2.WaitingForThreshold && IsTemperatureAboveThreshold_2(currentStep_2))
                    {
                        Debug.WriteLine($"[UC2][UpdateTimer] Temperature threshold reached from timer update");
                        SetTimerState_2(StepState_2.CountingDown);
                    }
                }

                // อัปเดตการแสดงเวลา
                if (isStarted_2 && totalStepSeconds_2 > 0)
                {
                    int remainingSec = ProgramState_2.RemainingSeconds_2;
                    int remHr = remainingSec / 3600;
                    int remMin = (remainingSec % 3600) / 60;
                    int remSec = remainingSec % 60;
                    label_Process2Time_Seg.Text = $"{remHr}:{remMin:D2}:{remSec:D2}";

                    int elapsed = totalStepSeconds_2 - remainingSec;
                    int elapsedHr = elapsed / 3600;
                    int elapsedMin = (elapsed % 3600) / 60;
                    int elapsedSec = elapsed % 60;
                    label_Process2Time_Left.Text = $"{elapsedHr}:{elapsedMin:D2}:{elapsedSec:D2}";

                    label_Process2Hr.Text = remHr.ToString();
                    label_Process2Min.Text = remMin.ToString();
                    label_Process2Sec.Text = remSec.ToString();
                }

                // อัปเดตสถานะหลอดไฟ
                UpdateLampStatus_2();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC2][ERROR] UpdateTimer_Tick: {ex.Message}");
            }
        }
        private void RegisterTimeTextBox_2(TextBox tb)
        {
            if (tb == null) return;
            tb.TextChanged += (s, e) =>
            {
                UpdateDataSet1_2(tb.Name, tb.Text);
                settings_2.SaveToFile_2("Data_Set2.xml", "Combined");
                Debug.WriteLine(tb.Name + " changed to: " + tb.Text);
            };
            tb.MaxLength = 3;
        }

        private void RegisterTimeTextBoxes_2()
        {
            RegisterTimeTextBox_2(text2_Hr1);
            RegisterTimeTextBox_2(text2_Hr2);
            RegisterTimeTextBox_2(text2_Hr3);
            RegisterTimeTextBox_2(text2_Hr4);
            RegisterTimeTextBox_2(text2_Hr5);
            RegisterTimeTextBox_2(text2_Hr6);
            RegisterTimeTextBox_2(text2_Hr7);
            RegisterTimeTextBox_2(text2_Hr8);

            RegisterTimeTextBox_2(text2_Min1);
            RegisterTimeTextBox_2(text2_Min2);
            RegisterTimeTextBox_2(text2_Min3);
            RegisterTimeTextBox_2(text2_Min4);
            RegisterTimeTextBox_2(text2_Min5);
            RegisterTimeTextBox_2(text2_Min6);
            RegisterTimeTextBox_2(text2_Min7);
            RegisterTimeTextBox_2(text2_Min8);
        }

     

        private void UpdateDataSet1_2(string key, string value)
        {
            switch (key)
            {
                case "text2_Temp1": settings_2.Text2_Temp1 = value; break;
                case "text2_Temp2": settings_2.Text2_Temp2 = value; break;
                case "text2_Temp3": settings_2.Text2_Temp3 = value; break;
                case "text2_Temp4": settings_2.Text2_Temp4 = value; break;
                case "text2_Temp5": settings_2.Text2_Temp5 = value; break;
                case "text2_Temp6": settings_2.Text2_Temp6 = value; break;
                case "text2_Temp7": settings_2.Text2_Temp7 = value; break;
                case "text2_Temp8": settings_2.Text2_Temp8 = value; break;

                case "text2_RPM1": settings_2.Text2_RPM1 = value; break;
                case "text2_RPM2": settings_2.Text2_RPM2 = value; break;
                case "text2_RPM3": settings_2.Text2_RPM3 = value; break;
                case "text2_RPM4": settings_2.Text2_RPM4 = value; break;
                case "text2_RPM5": settings_2.Text2_RPM5 = value; break;
                case "text2_RPM6": settings_2.Text2_RPM6 = value; break;
                case "text2_RPM7": settings_2.Text2_RPM7 = value; break;
                case "text2_RPM8": settings_2.Text2_RPM8 = value; break;

                case "text2_Dosing1": settings_2.Text2_Dosing1 = value; break;
                case "text2_Dosing2": settings_2.Text2_Dosing2 = value; break;
                case "text2_Dosing3": settings_2.Text2_Dosing3 = value; break;
                case "text2_Dosing4": settings_2.Text2_Dosing4 = value; break;
                case "text2_Dosing5": settings_2.Text2_Dosing5 = value; break;
                case "text2_Dosing6": settings_2.Text2_Dosing6 = value; break;
                case "text2_Dosing7": settings_2.Text2_Dosing7 = value; break;
                case "text2_Dosing8": settings_2.Text2_Dosing8 = value; break;

                case "text2_Hr1": settings_2.Text2_Hr1 = value; break;
                case "text2_Hr2": settings_2.Text2_Hr2 = value; break;
                case "text2_Hr3": settings_2.Text2_Hr3 = value; break;
                case "text2_Hr4": settings_2.Text2_Hr4 = value; break;
                case "text2_Hr5": settings_2.Text2_Hr5 = value; break;
                case "text2_Hr6": settings_2.Text2_Hr6 = value; break;
                case "text2_Hr7": settings_2.Text2_Hr7 = value; break;
                case "text2_Hr8": settings_2.Text2_Hr8 = value; break;

                case "text2_Min1": settings_2.Text2_Min1 = value; break;
                case "text2_Min2": settings_2.Text2_Min2 = value; break;
                case "text2_Min3": settings_2.Text2_Min3 = value; break;
                case "text2_Min4": settings_2.Text2_Min4 = value; break;
                case "text2_Min5": settings_2.Text2_Min5 = value; break;
                case "text2_Min6": settings_2.Text2_Min6 = value; break;
                case "text2_Min7": settings_2.Text2_Min7 = value; break;
                case "text2_Min8": settings_2.Text2_Min8 = value; break;
            }
        }

        private void LoadSavedValues_2()
        {
            // โหลดค่า settings ใหม่ที่ถูกบันทึกไว้
            text2_Temp1.Text = settings_2.Text2_Temp1;
            text2_Temp2.Text = settings_2.Text2_Temp2;
            text2_Temp3.Text = settings_2.Text2_Temp3;
            text2_Temp4.Text = settings_2.Text2_Temp4;
            text2_Temp5.Text = settings_2.Text2_Temp5;
            text2_Temp6.Text = settings_2.Text2_Temp6;
            text2_Temp7.Text = settings_2.Text2_Temp7;
            text2_Temp8.Text = settings_2.Text2_Temp8;

            text2_RPM1.Text = settings_2.Text2_RPM1;
            text2_RPM2.Text = settings_2.Text2_RPM2;
            text2_RPM3.Text = settings_2.Text2_RPM3;
            text2_RPM4.Text = settings_2.Text2_RPM4;
            text2_RPM5.Text = settings_2.Text2_RPM5;
            text2_RPM6.Text = settings_2.Text2_RPM6;
            text2_RPM7.Text = settings_2.Text2_RPM7;
            text2_RPM8.Text = settings_2.Text2_RPM8;

            text2_Dosing1.Text = settings_2.Text2_Dosing1;
            text2_Dosing2.Text = settings_2.Text2_Dosing2;
            text2_Dosing3.Text = settings_2.Text2_Dosing3;
            text2_Dosing4.Text = settings_2.Text2_Dosing4;
            text2_Dosing5.Text = settings_2.Text2_Dosing5;
            text2_Dosing6.Text = settings_2.Text2_Dosing6;
            text2_Dosing7.Text = settings_2.Text2_Dosing7;
            text2_Dosing8.Text = settings_2.Text2_Dosing8;

            text2_Hr1.Text = settings_2.Text2_Hr1;
            text2_Hr2.Text = settings_2.Text2_Hr2;
            text2_Hr3.Text = settings_2.Text2_Hr3;
            text2_Hr4.Text = settings_2.Text2_Hr4;
            text2_Hr5.Text = settings_2.Text2_Hr5;
            text2_Hr6.Text = settings_2.Text2_Hr6;
            text2_Hr7.Text = settings_2.Text2_Hr7;
            text2_Hr8.Text = settings_2.Text2_Hr8;

            text2_Min1.Text = settings_2.Text2_Min1;
            text2_Min2.Text = settings_2.Text2_Min2;
            text2_Min3.Text = settings_2.Text2_Min3;
            text2_Min4.Text = settings_2.Text2_Min4;
            text2_Min5.Text = settings_2.Text2_Min5;
            text2_Min6.Text = settings_2.Text2_Min6;
            text2_Min7.Text = settings_2.Text2_Min7;
            text2_Min8.Text = settings_2.Text2_Min8;

            // โหลดสถานะ Lamp จากไฟล์ XML (ถ้ามี)

        }
        private void RegisterDataTextBoxes_2()
        {
            // ทำซ้ำจาก 1 ถึง 8
            for (int i = 1; i <= 8; i++)
            {
                // Temp
                var tbTemp = this.Controls.Find($"text2_Temp{i}", true).FirstOrDefault() as TextBox;
                if (tbTemp != null) RegisterDataTextBox_2(tbTemp);

                // RPM
                var tbRpm = this.Controls.Find($"text2_RPM{i}", true).FirstOrDefault() as TextBox;
                if (tbRpm != null) RegisterDataTextBox_2(tbRpm);

                // Dosing
                var tbDos = this.Controls.Find($"text2_Dosing{i}", true).FirstOrDefault() as TextBox;
                if (tbDos != null) RegisterDataTextBox_2(tbDos);
            }
        }

        /// <summary>
        /// ผูก TextChanged ให้ TextBox ใดๆ ที่เรียกผ่านนี้ จะอัปเดต settings.Text1_* ตามชื่อ key
        /// แล้วบันทึกไฟล์ Data_Set1.xml
        /// </summary>
        private void RegisterDataTextBox_2(TextBox tb)
        {
            tb.TextChanged += (s, e) =>
            {
                // ใช้ฟังก์ชันเดิมในการแมป key->property
                UpdateDataSet1_2(tb.Name, tb.Text);
                settings_2.SaveToFile_2("Data_Set2.xml", "Combined");
                Debug.WriteLine($"{tb.Name} changed to {tb.Text} -> saved to Data_Set2.xml");
            };
            tb.MaxLength = 10; // หรือกำหนดตามต้องการ
        }
    }

    public static class ProgramControlSet1Data2
    {
        public static Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
    }
    // Fix for CS1061: Ensure that the `DeviceSettings2` class has a `LoadFromFile` method defined.
    // If `DeviceSettings2` is intended to be similar to `DeviceSettings`, we can add the missing method.

   
}