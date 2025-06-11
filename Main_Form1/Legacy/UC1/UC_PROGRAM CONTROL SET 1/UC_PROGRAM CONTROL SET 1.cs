using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics; // สำหรับ Debug.WriteLine()
using Timer = System.Windows.Forms.Timer; // ใช้ alias สำหรับ Windows Forms Timer
using System.Threading; // สำหรับ Thread.Sleep

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    public partial class UC_PROGRAM_CONTROL_SET_1 : UserControl
    {
        // ------------------ ส่วนที่เกี่ยวกับ Lamp Status ------------------
        private static readonly string[] persistedLampStatuses_1 = new string[9]; // index 0 ไม่ได้ใช้
        // -------------------------------------------------------------------

        private static bool hasLoadedSettings_1 = false;
        private Timer updateTimer_1;
        private static Timer countdownTimer_1;
        private Timer settingsUpdateTimer_1 = new Timer();
        private static int previousCountdown_1 = -1;
        private static int globalTotalStepSeconds_1 = 0;
        private static DateTime lastCountdownTick_1;
        private DateTime lastSetpointUpdateTime_1 = DateTime.MinValue;
        private int totalStepSeconds_1 = 0;

        private Data_Set1 settings_1;

        /* ------------------------ ตัวแปรของ Step (unchanged) ------------------------ */
        // …

        /* ------------------------ Local state ------------------------ */
        private int currentStep_1 = 0;
        private int remainingSeconds_1 = 0;
        private bool isStarted_1 = false;
        private bool isPaused_1 = false;
        private bool isNextStepProcessing_1 = false;
        private static readonly List<int> completedSteps_1 = new();

        private TextBox[] tempBoxes;
        private TextBox[] rpmBoxes;
        private TextBox[] dosingBoxes;


        private readonly ErrorProvider errorProvider_1 = new();

        private DeviceSettings_1 deviceSettings_1;

        /* -------- ลดซ้ำซ้อน: เหลือเพียงชุดเดียว -------- */
        private double[] latestValues_1 = null;   // เซนเซอร์ล่าสุด
        private bool waitingForThreshold_1 = false; // รออุณหภูมิถึงค่า target
        private int waitStep_1 = 0;    // Step ที่กำลัง Wait
        private float Target_Tr_1;
        private float Target_Tj_1;
        private int Target_RPM_1;
        private float Target_Dosing_1;
        private int Target_Hr_1;
        private int Target_Min_1;

        // สำหรับ Steps 2–8 :contentReference[oaicite:8]{index=8}:contentReference[oaicite:9]{index=9}
        private float Target1_Tr2, Target1_Tj2;
        private float Target1_Tr3, Target1_Tj3;
        private float Target1_Tr4, Target1_Tj4;
        private float Target1_Tr5, Target1_Tj5;
        private float Target1_Tr6, Target1_Tj6;
        private float Target1_Tr7, Target1_Tj7;
        private float Target1_Tr8, Target1_Tj8;

        private static double staticTR_1 = 0.0;
        private static double staticTJ_1 = 0.0;
        private static double staticRPM_1 = 0.0;
        private static double staticExt_1 = 0.0;
        private static bool hasReceivedData_1 = false;

        // ตัวแปรท้องถิ่นเดิม
        private double lastTR_1 = 0.0;
        private double lastTJ_1 = 0.0;
        private double lastRPM_1 = 0.0;
        private double lastExt_1 = 0.0;

        private bool isRegistered_1 = false;
        static UC_PROGRAM_CONTROL_SET_1()
        {

            countdownTimer_1 = new Timer();
            countdownTimer_1.Interval = 1000; // 1 sec
            countdownTimer_1.Tick += StaticCountdownTimer_Tick_1;
            lastCountdownTick_1 = DateTime.Now;
            for (int i = 1; i <= 8; i++)
            {
                persistedLampStatuses_1[i] = "Wait";
            }
        }
        private bool IsStepTargetTJ_1(int step)
        {
            switch (step)
            {
                case 1: return switch_Target1_1.IsOn;
                case 2: return switch_Target1_2.IsOn;
                case 3: return switch_Target1_3.IsOn;
                case 4: return switch_Target1_4.IsOn;
                case 5: return switch_Target1_5.IsOn;
                case 6: return switch_Target1_6.IsOn;
                case 7: return switch_Target1_7.IsOn;
                case 8: return switch_Target1_8.IsOn;
                default: return false;
            }
        }
        private bool IsStepTimeRun_1(int step)
        {
            switch (step)
            {
                case 1: return switch_Time1_1.IsOn;
                case 2: return switch_Time1_2.IsOn;
                case 3: return switch_Time1_3.IsOn;
                case 4: return switch_Time1_4.IsOn;
                case 5: return switch_Time1_5.IsOn;
                case 6: return switch_Time1_6.IsOn;
                case 7: return switch_Time1_7.IsOn;
                case 8: return switch_Time1_8.IsOn;
                default: return false;
            }
        }
        // --------------------- ฟังก์ชันสำหรับ Lamp ---------------------
        private void ResetLampStatuses_1()
        {
            for (int i = 1; i <= 8; i++)
            {
                persistedLampStatuses_1[i] = "Wait";
            }
            completedSteps_1.Clear();
            UpdateLampStatus_1();
        }
        private void UpdateLampStatus_1()
        {
            for (int i = 1; i <= 8; i++)
            {
                string status;
                if (completedSteps_1.Contains(i))
                    status = "Done";
                else if (i == currentStep_1)
                    status = isStarted_1 && !isPaused_1 ? "Run" : "Wait";
                else
                    status = persistedLampStatuses_1[i];
                SetLampForStep_1(i, status);
            }
           // Debug.WriteLine("[UI1] Lamps updated");
        }

        private void SetLampForStep_1(int step, string status)
        {
            persistedLampStatuses_1[step] = status;
            // สมมุติว่า PictureBox lamp_Process11 ... lamp_Process18 มี property Mode
            switch (step)
            {
                case 1:
                    lamp_Process11.Mode = persistedLampStatuses_1[1];
                    break;
                case 2:
                    lamp_Process12.Mode = persistedLampStatuses_1[2];
                    break;
                case 3:
                    lamp_Process13.Mode = persistedLampStatuses_1[3];
                    break;
                case 4:
                    lamp_Process14.Mode = persistedLampStatuses_1[4];
                    break;
                case 5:
                    lamp_Process15.Mode = persistedLampStatuses_1[5];
                    break;
                case 6:
                    lamp_Process16.Mode = persistedLampStatuses_1[6];
                    break;
                case 7:
                    lamp_Process17.Mode = persistedLampStatuses_1[7];
                    break;
                case 8:
                    lamp_Process18.Mode = persistedLampStatuses_1[8];
                    break;
            }
        }
        // --------------------- สิ้นสุดส่วน Lamp ---------------------

        // ฟังก์ชันสำหรับบันทึก Lamp State ลงใน Data_Set1.xml
        private void SaveLampState_1()
        {
            // สมมติว่าใน Data_Set1 มี properties สำหรับ Lamp State (LampState1 ถึง LampState8)
            settings_1.LampState1 = lamp_Process11.Mode;
            settings_1.LampState2 = lamp_Process12.Mode;
            settings_1.LampState3 = lamp_Process13.Mode;
            settings_1.LampState4 = lamp_Process14.Mode;
            settings_1.LampState5 = lamp_Process15.Mode;
            settings_1.LampState6 = lamp_Process16.Mode;
            settings_1.LampState7 = lamp_Process17.Mode;
            settings_1.LampState8 = lamp_Process18.Mode;
            settings_1.SaveToFile_1("Data_Set1.xml", "LampState");
        }

        // ฟังก์ชันสำหรับโหลด Lamp State จาก settings (ถ้ามี)
        private void LoadLampState_1()
        {
            if (!string.IsNullOrEmpty(settings_1.LampState1))
                lamp_Process11.Mode = settings_1.LampState1;
            if (!string.IsNullOrEmpty(settings_1.LampState2))
                lamp_Process12.Mode = settings_1.LampState2;
            if (!string.IsNullOrEmpty(settings_1.LampState3))
                lamp_Process13.Mode = settings_1.LampState3;
            if (!string.IsNullOrEmpty(settings_1.LampState4))
                lamp_Process14.Mode = settings_1.LampState4;
            if (!string.IsNullOrEmpty(settings_1.LampState5))
                lamp_Process15.Mode = settings_1.LampState5;
            if (!string.IsNullOrEmpty(settings_1.LampState6))
                lamp_Process16.Mode = settings_1.LampState6;
            if (!string.IsNullOrEmpty(settings_1.LampState7))
                lamp_Process17.Mode = settings_1.LampState7;
            if (!string.IsNullOrEmpty(settings_1.LampState8))
                lamp_Process18.Mode = settings_1.LampState8;
        }

        private static void StaticCountdownTimer_Tick_1(object sender, EventArgs e)
        {
            if (ProgramState_1.IsStarted_1 && !ProgramState_1.IsPaused_1)
            {
                lastCountdownTick_1 = DateTime.Now;
                if (ProgramState_1.RemainingSeconds_1 > 0)
                {
                    previousCountdown_1 = ProgramState_1.RemainingSeconds_1;
                    ProgramState_1.RemainingSeconds_1--;
                  //  Debug.WriteLine("Countdown: " + ProgramState.RemainingSeconds + " sec remaining.");
                    if (ProgramState_1.RemainingSeconds_1 < 1)
                    {
                        countdownTimer_1.Stop();
                        ProgramState_1.OnCountdownFinished_1();
                    }
                }
            }
        }


        private bool CanStartSystem_1()
        {
            if (!switch_A_M1.IsOn)
            {
                MessageBox.Show("กรุณากดปุ่ม Auto เพื่อเริ่มระบบ");
                return false;
            }
            return true;
        }

        // --- Emergency Stop ---
        private void RestoreLatestSensorValues_1()
        {
            if (!hasReceivedData_1) return;

            lastTR_1 = staticTR_1;
            lastTJ_1 = staticTJ_1;
            lastRPM_1 = staticRPM_1;
            lastExt_1 = staticExt_1;
            UpdateProgramUI_1();            // วาดลง Label
        }

        public UC_PROGRAM_CONTROL_SET_1()
        {
            InitializeComponent();
            // โหลดค่าสถานะล่าสุดตอนเปิดหน้า
            switch_A_M1.IsOn = ProgramState_1.SwitchA_M1;

            // เมื่อผู้ใช้กดสวิตช์ ให้ปรับ ProgramState ตาม
            switch_A_M1.ToggleChanged += (s, e) =>
            {
                ProgramState_1.SwitchA_M1 = switch_A_M1.IsOn;
            };

            // สมัครรับอีเวนต์ StateChanged เพื่ออัปเดต UI ถ้ามีการเปลี่ยนสถานะที่อื่น
            ProgramState_1.StateChanged_1 += (s, e) =>
            {
                // เช็คว่า InvokeRequired ไหม (ถ้าเกิดจากเธรดอื่น)
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(() => switch_A_M1.IsOn = ProgramState_1.SwitchA_M1));
                else
                    switch_A_M1.IsOn = ProgramState_1.SwitchA_M1;
            };

            RegisterEvents_1();
            SerialPortManager.Instance.DataReceivedEvent += OnDataReceived_1;
            SerialPortManager.Instance.UI1DataReceivedEvent += OnUI1DataReceived_1;

            // ส่วนลงทะเบียน DataReceived ตามเดิม
            deviceSettings_1 = DeviceSettings_1.Load_1("Settings_1.xml");
            if (deviceSettings_1 == null)
            {
                MessageBox.Show(
                    "ไม่พบไฟล์ Settings_1.xml หรือโหลดค่าการตั้งค่าไม่สำเร็จ",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }


            // ส่งคำสั่งตั้งค่าครั้งแรกแบบปลอดภัย
            TrySendInitialConfiguration_1();
            // ตรวจสอบและสร้างปุ่ม Emergency ถ้ายังไม่มีใน Designer



            ProgramState_1.StateChanged_1 += ProgramState_StateChanged_1;
            ProgramState_1.CountdownFinished_1 += ProgramState_CountdownFinished_1;

            if (ProgramState_1.CurrentStep_1 > 0)
            {
                currentStep_1 = ProgramState_1.CurrentStep_1;
                remainingSeconds_1 = ProgramState_1.RemainingSeconds_1;
                isStarted_1 = ProgramState_1.IsStarted_1;
                isPaused_1 = ProgramState_1.IsPaused_1;
            }

            But_GoHome3.Click += But_GoHome3_Click;

            settings_1 = Data_Set1.LoadFromFile_1("Data_Set1.xml");
            // Debug.WriteLine("Loaded settings from Data_Set1.xml");
            // โหลดสถานะ Lamp จากไฟล์ XML (ถ้ามี)
            LoadLampState_1();

            if (!ValidateAllStepCountdownInputs_1())
            {
                MessageBox.Show(
                    "มีค่าที่ป้อนอยู่นอกขอบเขตที่กำหนด กรุณาตรวจสอบอีกครั้ง",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return; // ยุติ ไม่ส่งคำสั่งต่อ
            }

            LoadSavedValues_1();
            SetupTextBoxArrays_1();

            // ฟื้นฟูค่า toggle และ switch จาก settings
            toggle_1SwitchControl1.IsOn = settings_1.Toggle_1SwitchControl1;
            toggle_1SwitchControl2.IsOn = settings_1.Toggle_1SwitchControl2;
            toggle_1SwitchControl3.IsOn = settings_1.Toggle_1SwitchControl3;
            toggle_1SwitchControl4.IsOn = settings_1.Toggle_1SwitchControl4;
            toggle_1SwitchControl5.IsOn = settings_1.Toggle_1SwitchControl5;
            toggle_1SwitchControl6.IsOn = settings_1.Toggle_1SwitchControl6;
            toggle_1SwitchControl7.IsOn = settings_1.Toggle_1SwitchControl7;
            toggle_1SwitchControl8.IsOn = settings_1.Toggle_1SwitchControl8;

            // สมัคร event handler สำหรับ switch_Target และ switch_Time ของแต่ละ Step
            switch_Target1_1.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Target1_1 = switch_Target1_1.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step1");
            };
            switch_Time1_1.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Time1_1 = switch_Time1_1.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step1");
            };

            switch_Target1_2.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Target1_2 = switch_Target1_2.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step2");
            };
            switch_Time1_2.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Time1_2 = switch_Time1_2.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step2");
            };

            switch_Target1_3.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Target1_3 = switch_Target1_3.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step3");
            };
            switch_Time1_3.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Time1_3 = switch_Time1_3.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step3");
            };

            switch_Target1_4.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Target1_4 = switch_Target1_4.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step4");
            };
            switch_Time1_4.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Time1_4 = switch_Time1_4.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step4");
            };

            switch_Target1_5.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Target1_5 = switch_Target1_5.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step5");
            };
            switch_Time1_5.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Time1_5 = switch_Time1_5.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step5");
            };

            switch_Target1_6.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Target1_6 = switch_Target1_6.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step6");
            };
            switch_Time1_6.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Time1_6 = switch_Time1_6.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step6");
            };

            switch_Target1_7.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Target1_7 = switch_Target1_7.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step7");
            };
            switch_Time1_7.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Time1_7 = switch_Time1_7.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step7");
            };

            switch_Target1_8.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Target1_8 = switch_Target1_8.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step8");
            };
            switch_Time1_8.ToggleChanged += (s, e) =>
            {
                settings_1.Switch_Time1_8 = switch_Time1_8.IsOn;
                settings_1.SaveToFile_1("Data_Set1.xml", "Step8");
            };

            // ตั้งค่าจาก settings สำหรับ switch_Target และ switch_Time เมื่อโหลด UI
            switch_Target1_1.IsOn = settings_1.Switch_Target1_1;
            switch_Time1_1.IsOn = settings_1.Switch_Time1_1;
            switch_Target1_2.IsOn = settings_1.Switch_Target1_2;
            switch_Time1_2.IsOn = settings_1.Switch_Time1_2;
            switch_Target1_3.IsOn = settings_1.Switch_Target1_3;
            switch_Time1_3.IsOn = settings_1.Switch_Time1_3;
            switch_Target1_4.IsOn = settings_1.Switch_Target1_4;
            switch_Time1_4.IsOn = settings_1.Switch_Time1_4;
            switch_Target1_5.IsOn = settings_1.Switch_Target1_5;
            switch_Time1_5.IsOn = settings_1.Switch_Time1_5;
            switch_Target1_6.IsOn = settings_1.Switch_Target1_6;
            switch_Time1_6.IsOn = settings_1.Switch_Time1_6;
            switch_Target1_7.IsOn = settings_1.Switch_Target1_7;
            switch_Time1_7.IsOn = settings_1.Switch_Time1_7;
            switch_Target1_8.IsOn = settings_1.Switch_Target1_8;
            switch_Time1_8.IsOn = settings_1.Switch_Time1_8;

           // Debug.WriteLine("UI updated from file settings.");

            // สมัคร event handler สำหรับ toggle ของแต่ละ Step
            toggle_1SwitchControl1.Click += toggle_1SwitchControl1_Click;
            toggle_1SwitchControl2.Click += toggle_1SwitchControl2_Click;
            toggle_1SwitchControl3.Click += toggle_1SwitchControl3_Click;
            toggle_1SwitchControl4.Click += toggle_1SwitchControl4_Click;
            toggle_1SwitchControl5.Click += toggle_1SwitchControl5_Click;
            toggle_1SwitchControl6.Click += toggle_1SwitchControl6_Click;
            toggle_1SwitchControl7.Click += toggle_1SwitchControl7_Click;
            toggle_1SwitchControl8.Click += toggle_1SwitchControl8_Click;

            RegisterTimeTextBoxes_1();
            RegisterDataTextBoxes_1();
            updateTimer_1 = new Timer { Interval = 1000 };    // 1 sec
            updateTimer_1.Tick += UpdateTimer_Tick_1;
            updateTimer_1.Start();

            // ใช้ settingsUpdateTimer อัปเดต setpoint labels ทุก 1 วินาที
            settingsUpdateTimer_1 = new Timer { Interval = 1000 };
            settingsUpdateTimer_1.Tick += (s, e) => UpdateSetpointLabels_1();
            settingsUpdateTimer_1.Start();


            // ตั้งค่าเริ่มต้นปุ่ม Start/Stop เมื่อเปิดโปรแกรม
            if (!ProgramState_1.IsStarted_1 && !ProgramState_1.IsPaused_1)
            {
                switch_system1_1.IsOn = false;
                switch_system1_2.IsOn = true;
            }
            else if (ProgramState_1.IsStarted_1 && !ProgramState_1.IsPaused_1)
            {
                switch_system1_1.IsOn = true;
                switch_system1_2.IsOn = false;
            }
            else if (ProgramState_1.IsPaused_1)
            {
                switch_system1_1.IsOn = false;
                switch_system1_2.IsOn = true;
            }

            // Event handler ปุ่ม Start/Resume
            switch_system1_1.Click += (s, e) =>
            {
                if (!switch_A_M1.IsOn)
                {
                    MessageBox.Show("กรุณากดปุ่ม Auto เพื่อเริ่มระบบ");
                    return;
                }

                // ถ้าเพิ่งรีเซ็ตมาก่อนหน้า ให้เริ่มใหม่

                if (!isStarted_1)
                {
                    // make sure label-refresh timer กลับมาทำงาน
                    if (updateTimer_1 != null && !updateTimer_1.Enabled) updateTimer_1.Start();

                    ProgramState_1.Reset_1();
                    completedSteps_1.Clear();
                    ResetLampStatuses_1();
                    globalTotalStepSeconds_1 = 0;
                }

                // Resume กรณี pause
                if (ProgramState_1.IsPaused_1)
                {
                    ProgramState_1.IsPaused_1 = false;
                    lastCountdownTick_1 = DateTime.Now;
                  //  Debug.WriteLine($"Resuming system at Step {currentStep} with {remainingSeconds} seconds remaining.");
                    if (!countdownTimer_1.Enabled) countdownTimer_1.Start();
                    UpdateUIFromState_1();
                    return;
                }

                // ----------------- เริ่มระบบใหม่ (Fresh Run) -----------------
                if (!isStarted_1)
                {
                    int firstStep = 1;
                    while (firstStep <= 8 && !IsStepToggleOn_1(firstStep)) firstStep++;

                    if (firstStep > 8)
                    {
                      //  Debug.WriteLine("No Step is toggled ON. System not started.");
                        MessageBox.Show("No step is registered as active.");
                        return;
                    }

                    int stepSeconds = GetStepCountdownSeconds_1(firstStep);
                    if (stepSeconds <= 0)
                    {
                       // Debug.WriteLine($"Step {firstStep} countdown time is 0 or invalid.");
                        MessageBox.Show($"Countdown for Step {firstStep} is not set properly. Please enter a valid countdown time.");
                        return;
                    }

                    isStarted_1 = true;
                    ProgramState_1.IsStarted_1 = true;
                    ProgramState_1.IsPaused_1 = false;
                    currentStep_1 = firstStep;
                    completedSteps_1.Clear();
                    remainingSeconds_1 = stepSeconds;
                    ProgramState_1.CurrentStep_1 = currentStep_1;
                    ProgramState_1.RemainingSeconds_1 = remainingSeconds_1;

                    switch_system1_1.IsOn = true;
                    switch_system1_2.IsOn = false;

                    //  Debug.WriteLine($"switch_system1_1 pressed. Starting system steps from Step {currentStep}...");
                    SendStepSetpointsToSerialAndDisplay_1(currentStep_1);
                    StartStepCountdown_1(currentStep_1);
                }
            };

            // ---------------------- STOP/RESET BUTTON (switch_system1_2) ----------------------
            switch_system1_2.Click += (s, e) =>
            {
                switch_system1_2.IsOn = true;
                switch_system1_1.IsOn = false;

                remainingSeconds_1 = ProgramState_1.RemainingSeconds_1;
                lastCountdownTick_1 = DateTime.Now;
                if (countdownTimer_1.Enabled) countdownTimer_1.Stop();

                if (isStarted_1 && !ProgramState_1.IsPaused_1)
                {
                    ProgramState_1.IsPaused_1 = true;
                   // Debug.WriteLine($"System paused at Step {currentStep} with {remainingSeconds} seconds remaining.");
                }

                UpdateLampStatus_1();

                // ---------- FULL RESET ----------
                //  Debug.WriteLine($"[UI1] Stop pressed while at Step {currentStep}. Performing full reset.");
                ResetProgram_1();
            };

            // Event handler ปุ่ม Skip
            switch_system1_3.Click += (s, e) =>
            {
                if (isStarted_1 && !ProgramState_1.IsPaused_1)
                {
                   // Debug.WriteLine("Skip button pressed. Skipping current step " + currentStep + ".");
                    if (countdownTimer_1.Enabled)
                        countdownTimer_1.Stop();
                    NextStep_1();
                }
                switch_system1_3.IsOn = false;
            };

            switch_A_M1.ToggleChanged += switch_A_M_ToggleChanged_1;
            switch_A_M1.IsOn = ProgramState_1.SwitchA_M1;

            // สมัคร event handler สำหรับ toggle ของแต่ละ Step
            toggle_1SwitchControl1.Click += toggle_1SwitchControl1_Click;
            toggle_1SwitchControl2.Click += toggle_1SwitchControl2_Click;
            toggle_1SwitchControl3.Click += toggle_1SwitchControl3_Click;
            toggle_1SwitchControl4.Click += toggle_1SwitchControl4_Click;
            toggle_1SwitchControl5.Click += toggle_1SwitchControl5_Click;
            toggle_1SwitchControl6.Click += toggle_1SwitchControl6_Click;
            toggle_1SwitchControl7.Click += toggle_1SwitchControl7_Click;
            toggle_1SwitchControl8.Click += toggle_1SwitchControl8_Click;

            RegisterTimeTextBoxes_1();

            // ส่งคำสั่งตั้งค่าครั้งแรกให้ BoxControl (Thermostat, Stirrer, Dosing, Offset, Emergency)


            // สมัคร event handler, โหลด Data_Set1 ฯลฯ
            settings_1 = Data_Set1.LoadFromFile_1("Data_Set1.xml");
            LoadLampState_1();

            deviceSettings_1 = DeviceSettings_1.Load_1("Settings_1.xml");
            // --- เพิ่มตรวจสอบขีดจำกัดอุณหภูมิ (Temp Limit) ก่อนส่งคำสั่งเริ่มต้น ---
            // ตรวจสอบว่า TempLimit1 อยู่ในช่วง [TempMin1, TempMax1]
            if (deviceSettings_1.TempLimit_1 < deviceSettings_1.TempMin_1 ||
                deviceSettings_1.TempLimit_1 > deviceSettings_1.TempMax_1)
            {
                MessageBox.Show(
                    $"ค่า Temp Limit ({deviceSettings_1.TempLimit_1}) ไม่ถูกต้อง\n" +
                    $"ต้องอยู่ระหว่าง {deviceSettings_1.TempMin_1} ถึง {deviceSettings_1.TempMax_1}",
                    "ค่าขีดจำกัดอุณหภูมิไม่ถูกต้อง",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                // หยุดการทำงานต่อ ไม่ส่งคำสั่งเริ่มต้น
                return;
            }
            SerialPortManager.Instance.DataReceivedEvent += OnDataReceived_1;


        }

        // ฟังก์ชันรับข้อมูลเฉพาะสำหรับ UI1
        // ฟังก์ชันรับข้อมูลเฉพาะสำหรับ UI1
        /// <summary>
        /// รับข้อมูลจาก SerialPortManager แล้วอัปเดตค่าและ UI
        /// </summary>
        /// <summary>
        /// รับข้อมูลจาก SerialPortManager แล้วอัปเดตค่าและ UI
        /// </summary>
        private void OnUI1DataReceived_1(double[] values)
        {
            try
            {
                if (values == null || values.Length < 4)
                {
                   // Debug.WriteLine("[UI1] Received invalid data (null or insufficient length)");
                    return;
                }

                // ทำงานใน UI thread
                if (this.InvokeRequired)
                {
                    if (!IsDisposed) // ตรวจสอบว่าไม่ได้ถูก Dispose ไปแล้ว
                    {
                        this.BeginInvoke(new Action(() => UpdateValuesFromArray_1(values)));
                    }
                }
                else
                {
                    UpdateValuesFromArray_1(values);
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"[UI1] Error in OnUI1DataReceived: {ex.Message}");
            }
        }
        private void UpdateValuesFromArray_1(double[] values)
        {
            if (values == null || values.Length < 4 || IsDisposed) return;

            // อัปเดตค่าท้องถิ่น
            lastTR_1 = values[0];
            lastTJ_1 = values[1];
            lastRPM_1 = values[2];
            lastExt_1 = values[3];

            // อัปเดตค่า static
            staticTR_1 = values[0];
            staticTJ_1 = values[1];
            staticRPM_1 = values[2];
            staticExt_1 = values[3];
            hasReceivedData_1 = true;

            // บันทึก log
           // Debug.WriteLine($"[UI1] Values updated: TR1={lastTR1:F2}, TJ1={lastTJ1:F2}, RPM1={lastRPM1:F0}");

            // อัปเดตการแสดงผลเฉพาะเมื่อหน้านี้กำลังแสดงอยู่
            if (this.Visible && !IsDisposed)
            {
                UpdateProgramUI_1();
            }
        }
        /// <summary>
        /// ฟังก์ชันอัปเดต UI จากค่าท้องถิ่น
        /// </summary>
        private void UpdateProgramUI_1()
        {
            try
            {
                // คำนวณค่าความแตกต่าง
                double diff = lastTR_1 - lastTJ_1;

                // อัปเดต labels
                if (label_TR1 != null && !IsDisposed) label_TR1.Text = $"{lastTR_1:F2}";
                if (label_TJ1 != null && !IsDisposed) label_TJ1.Text = $"{lastTJ_1:F2}";
                if (label_TR_TJ1 != null && !IsDisposed) label_TR_TJ1.Text = $"{diff:F2}";
                if (label_RPM1 != null && !IsDisposed) label_RPM1.Text = $"{lastRPM_1:F0}";


                // อัปเดต Processing Labels ถ้าใช้งาน
                if (label_Process1_TempStep != null)
                {
                    double temp = 0;
                    switch (currentStep_1)
                    {
                        case 1: temp = (settings_1.Target_Tj != 0 ? settings_1.Target_Tj : settings_1.Target_Tr); break;
                        case 2: temp = (settings_1.Target1_Tj2 != 0 ? settings_1.Target1_Tj2 : settings_1.Target1_Tr2); break;
                        case 3: temp = (settings_1.Target1_Tj3 != 0 ? settings_1.Target1_Tj3 : settings_1.Target1_Tr3); break;
                        case 4: temp = (settings_1.Target1_Tj4 != 0 ? settings_1.Target1_Tj4 : settings_1.Target1_Tr4); break;
                        case 5: temp = (settings_1.Target1_Tj5 != 0 ? settings_1.Target1_Tj5 : settings_1.Target1_Tr5); break;
                        case 6: temp = (settings_1.Target1_Tj6 != 0 ? settings_1.Target1_Tj6 : settings_1.Target1_Tr6); break;
                        case 7: temp = (settings_1.Target1_Tj7 != 0 ? settings_1.Target1_Tj7 : settings_1.Target1_Tr7); break;
                        case 8: temp = (settings_1.Target1_Tj8 != 0 ? settings_1.Target1_Tj8 : settings_1.Target1_Tr8); break;
                    }
                    label_Process1_TempStep.Text = temp.ToString("F2");
                }

              //  Debug.WriteLine($"[UI1-Program] UpdateProgramUI: อัปเดตค่า labels เป็น TR1={lastTR1:F2}, TJ1={lastTJ1:F2}, TR-TJ={diff:F2}, RPM1={lastRPM1:F0}");
            }
            catch (Exception ex)
            {
              //  Debug.WriteLine($"[ERROR] UpdateProgramUI: {ex.Message}");
            }
        }
        private void RegisterEvents_1()
        {
            if (!isRegistered_1)
            {
                SerialPortManager.Instance.UI1DataReceivedEvent += OnUI1DataReceived_1;
                SerialPortManager.Instance.DataReceivedEvent += OnDataReceived_1;
                isRegistered_1 = true;
              //  Debug.WriteLine("[UI1] Event handlers registered");
            }
        }

        private void UnregisterEvents_1()
        {
            if (isRegistered_1)
            {
                SerialPortManager.Instance.UI1DataReceivedEvent -= OnUI1DataReceived_1;
                SerialPortManager.Instance.DataReceivedEvent -= OnDataReceived_1;
                isRegistered_1 = false;
               // Debug.WriteLine("[UI1] Event handlers unregistered");
            }
        }
        private void ResetProgram_1()
        {
            if (countdownTimer_1.Enabled) countdownTimer_1.Stop();
            // **อย่าหยุด updateTimer**  ปล่อยให้มันวิ่งต่อเพื่อให้ UI อัปเดตได้ทันทีที่เริ่มใหม่
          //  Debug.WriteLine("[UI1] ResetProgram(): System is now IDLE and ready for a fresh run.");

            isStarted_1 = false;
            isPaused_1 = false;
            currentStep_1 = 0;
            remainingSeconds_1 = 0;
            totalStepSeconds_1 = 0;
            globalTotalStepSeconds_1 = 0;
            completedSteps_1.Clear();

            ProgramState_1.Reset_1();
            ResetLampStatuses_1();

            label_Process1Step.Text = "0";
            label_Process1Time_Seg.Text = "0:00:00";
            label_Process1Time_Left.Text = "0:00:00";
            label_Process1Hr.Text = "0";
            label_Process1Min.Text = "0";
            label_Process1Sec.Text = "0";

            switch_system1_1.IsOn = false; // Start  OFF
            switch_system1_2.IsOn = true;  // Stop   ON
           // Debug.WriteLine("[UI1] ResetProgram(): System is now IDLE and ready for a fresh run.");
        }
        private void TrySendInitialConfiguration_1()
        {
            try
            {

            }
            catch (NullReferenceException ex)
            {
              //  Debug.WriteLine("Error in SendInitialConfiguration: " + ex);
                MessageBox.Show(
                    "เกิดข้อผิดพลาดในการส่งคำสั่งตั้งค่าครั้งแรก\n" +
                    "กรุณาตรวจสอบการตั้งค่าและการเชื่อมต่อ",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void OnDataReceived_1(double[] values)
        {
            if (values == null || values.Length < 9)
            {
              //  Debug.WriteLine("[WARNING] OnDataReceived: Received invalid or incomplete data");
                return;
            }

            // บันทึกข้อมูลลงตัวแปรท้องถิ่น
            latestValues_1 = values;

            // อัปเดตค่าท้องถิ่น
            lastTR_1 = values[0];    // TR1
            lastTJ_1 = values[1];    // TJ1
            lastRPM_1 = values[2];   // RPM1
            lastExt_1 = values[8];   // ExtTemp1

            // อัปเดตค่า static
            staticTR_1 = values[0];
            staticTJ_1 = values[1];
            staticRPM_1 = values[2];
            staticExt_1 = values[8];
            hasReceivedData_1 = true;

            // บันทึก log
           // Debug.WriteLine($"[UI1-Program] OnDataReceived: TR1={lastTR1:F2}, TJ1={lastTJ1:F2}, RPM1={lastRPM1:F0}, Ext1={lastExt1:F2}");

            // อัปเดต UI ถ้าหน้านี้กำลังแสดงอยู่
            if (this.Visible)
            {
                this.BeginInvoke(new Action(() => {
                    UpdateProgramUI_1();
                }));
            }

            // ตรวจสอบว่ากำลังรอ threshold หรือไม่
            if (isStarted_1 && waitingForThreshold_1 && waitStep_1 > 0)
            {
                // ดึงค่าเป้าหมายและตรวจสอบโหมด
                double targetTemp = GetTargetTemp_1(waitStep_1);
                bool isTjMode = IsStepTargetTJ_1(waitStep_1);

                // เลือกค่าอุณหภูมิตามโหมดที่เลือก
                double currentTemp = isTjMode ? values[1] : values[0]; // 1=TJ, 0=TR

               // Debug.WriteLine($"[Temperature Check] Step {waitStep}: {(isTjMode ? "TJ" : "TR")}={currentTemp:F2}, Target={targetTemp:F2}");

                // ตรวจสอบว่าถึงค่าเป้าหมายหรือไม่
                if (currentTemp >= targetTemp)
                {
                    this.BeginInvoke(new Action(() => {
                        if (waitingForThreshold_1)
                        {
                            waitingForThreshold_1 = false;
                           // Debug.WriteLine($"[Temperature Check] Step {waitStep}: threshold met, starting countdown");

                            if (!countdownTimer_1.Enabled)
                            {
                                countdownTimer_1.Start();
                               // Debug.WriteLine($"[Temperature Check] Step {waitStep}: countdown timer started");
                            }

                            // อัปเดต UI
                            UpdateLampStatus_1();
                        }
                    }));
                }
            }

            // ตรวจสอบว่ามีการเปิดใช้งาน External PT100 หรือไม่
            if (!string.IsNullOrEmpty(deviceSettings_1.PT100Sensor_1) &&
                deviceSettings_1.PT100Sensor_1 != "Disabled")
            {
                // ข้อมูล External Temp อยู่ที่ index 2 (เช่นเดียวกับ Set1)
                double actualExtTemp = values[2];
                if (actualExtTemp < deviceSettings_1.EmergencyMin_1 ||
                    actualExtTemp > deviceSettings_1.EmergencyMax_1)
                {
                    // เรียกบน UI thread เพื่อสั่งหยุดฉุกเฉิน
                    this.BeginInvoke(new Action(() => {
                        // โค้ดเดิม...
                    }));
                }
            }
        }



        private void SetupTextBoxArrays_1()
        {
            tempBoxes = new[] { text1_Temp1,  text1_Temp2,  text1_Temp3,  text1_Temp4,
                              text1_Temp5,  text1_Temp6,  text1_Temp7,  text1_Temp8 };
            rpmBoxes = new[] { text1_RPM1,   text1_RPM2,   text1_RPM3,   text1_RPM4,
                              text1_RPM5,   text1_RPM6,   text1_RPM7,   text1_RPM8 };
            dosingBoxes = new[] { text1_Dosing1,text1_Dosing2, text1_Dosing3, text1_Dosing4,
                              text1_Dosing5,text1_Dosing6, text1_Dosing7, text1_Dosing8 };
        }

        private bool ValidateAllStepCountdownInputs_1()
        {
            bool valid = true;

            // ตรวจสอบเวลาถอยหลังเดิม (ไม่เปลี่ยนแปลง)
            for (int step = 1; step <= 8; step++)
            {
                int secs = GetStepCountdownSeconds_1(step);
                if (secs <= 0) valid = false;
            }

            // ตรวจสอบ Setpoint (Temp, RPM, Dosing) ไม่เกิน Limit
            if (tempBoxes != null && rpmBoxes != null && dosingBoxes != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    // — Temp —
                    var tbTemp = tempBoxes[i];
                    if (tbTemp != null && float.TryParse(tbTemp.Text, out float temp))
                    {
                        if (temp < deviceSettings_1.TempMin_1 || temp > deviceSettings_1.TempMax_1)
                        {
                            errorProvider_1.SetError(
                                tbTemp,
                                $"อุณหภูมิต้องอยู่ระหว่าง {deviceSettings_1.TempMin_1} ถึง {deviceSettings_1.TempMax_1}"
                            );
                            valid = false;
                        }
                        else errorProvider_1.SetError(tbTemp, "");
                    }
                    // — RPM —
                    var tbRPM = rpmBoxes[i];
                    if (tbRPM != null && int.TryParse(tbRPM.Text, out int rpm))
                    {
                        if (rpm < deviceSettings_1.StirrerMin_1 || rpm > deviceSettings_1.StirrerMax_1)
                        {
                            errorProvider_1.SetError(
                                tbRPM,
                                $"RPM ต้องอยู่ระหว่าง {deviceSettings_1.StirrerMin_1} ถึง {deviceSettings_1.StirrerMax_1}"
                            );
                            valid = false;
                        }
                        else errorProvider_1.SetError(tbRPM, "");
                    }
                    // — Dosing —
                    var tbDosing = dosingBoxes[i];
                    if (tbDosing != null && float.TryParse(tbDosing.Text, out float dosing))
                    {
                        if (dosing < deviceSettings_1.DosingMin_1 || dosing > deviceSettings_1.DosingMax_1)
                        {
                            errorProvider_1.SetError(
                                tbDosing,
                                $"Dosing ต้องอยู่ระหว่าง {deviceSettings_1.DosingMin_1} ถึง {deviceSettings_1.DosingMax_1}"
                            );
                            valid = false;
                        }
                        else errorProvider_1.SetError(tbDosing, "");
                    }
                }
            }

            return valid;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateUIFromState_1();
            RestoreLatestSensorValues_1();   // ← เพิ่มบรรทัดนี้
        }

        private void ProgramState_StateChanged_1(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => UpdateUIFromState_1()));
            else
                UpdateUIFromState_1();
        }

        private void ProgramState_CountdownFinished_1(object sender, EventArgs e)
        {
            if (isStarted_1 && !ProgramState_1.IsPaused_1)
            {
              //  Debug.WriteLine("Countdown finished; moving to next step.");
                this.BeginInvoke(new Action(() => NextStep_1()));
            }
        }

        private void UpdateUIFromState_1()
        {
            settings_1.Toggle_1SwitchControl1 = toggle_1SwitchControl1.IsOn;
            settings_1.Toggle_1SwitchControl2 = toggle_1SwitchControl2.IsOn;
            settings_1.Toggle_1SwitchControl3 = toggle_1SwitchControl3.IsOn;
            settings_1.Toggle_1SwitchControl4 = toggle_1SwitchControl4.IsOn;
            settings_1.Toggle_1SwitchControl5 = toggle_1SwitchControl5.IsOn;
            settings_1.Toggle_1SwitchControl6 = toggle_1SwitchControl6.IsOn;
            settings_1.Toggle_1SwitchControl7 = toggle_1SwitchControl7.IsOn;
            settings_1.Toggle_1SwitchControl8 = toggle_1SwitchControl8.IsOn;

            switch_A_M1.IsOn = ProgramState_1.SwitchA_M1;

            if (!ProgramState_1.IsStarted_1 && !ProgramState_1.IsPaused_1)
            {
                switch_system1_1.IsOn = false;
                switch_system1_2.IsOn = true;
            }
            else if (ProgramState_1.IsStarted_1 && !ProgramState_1.IsPaused_1)
            {
                switch_system1_1.IsOn = true;
                switch_system1_2.IsOn = false;
            }
            else if (ProgramState_1.IsPaused_1)
            {
                switch_system1_1.IsOn = false;
                switch_system1_2.IsOn = true;
            }

            if (ProgramState_1.IsStarted_1 && !ProgramState_1.IsPaused_1)
            {
                TimeSpan gap = DateTime.Now - lastCountdownTick_1;
                int gapSeconds = (int)gap.TotalSeconds;
                if (gapSeconds > 0)
                {
                    ProgramState_1.RemainingSeconds_1 = Math.Max(ProgramState_1.RemainingSeconds_1 - gapSeconds, 0);
                    lastCountdownTick_1 = DateTime.Now;
                }
            }

            if (ProgramState_1.CurrentStep_1 > 0 && ProgramState_1.CurrentStep_1 != currentStep_1)
                currentStep_1 = ProgramState_1.CurrentStep_1;
            if (ProgramState_1.IsStarted_1)
                remainingSeconds_1 = ProgramState_1.RemainingSeconds_1;
            if (globalTotalStepSeconds_1 > 0)
                totalStepSeconds_1 = globalTotalStepSeconds_1;

            if (currentStep_1 > 0)
            {
                label_Process1Step.Text = currentStep_1.ToString();
                UpdatePicTarget_1(currentStep_1);
                UpdatePicModeTime_1(currentStep_1);
                UpdateSetpointLabels_1();
               // Debug.WriteLine("UI restored from ProgramState: Step " + currentStep + ", Remaining " + remainingSeconds + " sec.");
            }
            UpdateLampStatus_1();
        }

        /// <summary>
        /// ทำงานเมื่อสถานะการแสดงผลของ UserControl เปลี่ยนแปลง
        /// </summary>
        /// <summary>
        /// ทำงานเมื่อสถานะการแสดงผลของ UserControl เปลี่ยนแปลง
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                RegisterEvents_1();
                RestoreLatestSensorValues_1();
                // ลงทะเบียนรับข้อมูลเมื่อหน้านี้แสดง
                SerialPortManager.Instance.UI1DataReceivedEvent += OnUI1DataReceived_1;
                SerialPortManager.Instance.DataReceivedEvent += OnDataReceived_1;

                //  Debug.WriteLine("[UI1-Program] OnVisibleChanged: เข้าสู่หน้า UC_PROGRAM_CONTROL_SET_1");

                // โหลดค่าล่าสุดจาก static variables เมื่อเปิดหน้า
                if (hasReceivedData_1)
                {
                    // อัปเดตตัวแปรในคลาส
                    lastTR_1 = staticTR_1;
                    lastTJ_1 = staticTJ_1;
                    lastRPM_1 = staticRPM_1;
                    lastExt_1 = staticExt_1;

                    // อัปเดต UI ทันที
                    //  Debug.WriteLine($"[UI1-Program] OnVisibleChanged: ใช้ข้อมูลจากตัวแปร Static: TR1={lastTR1:F2}, TJ1={lastTJ1:F2}, RPM1={lastRPM1:F0}");
                    UpdateProgramUI_1();
                }
                else
                {
                    SerialPortManager.Instance.UI1DataReceivedEvent -= OnUI1DataReceived_1;
                    SerialPortManager.Instance.DataReceivedEvent -= OnDataReceived_1;
                    // ถ้ายังไม่เคยรับข้อมูล ให้ดึงจาก CurrentValues
                    var currentValues = SerialPortManager.Instance.CurrentValues;
                    if (currentValues != null && currentValues.Length >= 9)
                    {
                        lastTR_1 = currentValues[0];
                        lastTJ_1 = currentValues[1];
                        lastRPM_1 = currentValues[2];
                        lastExt_1 = currentValues[8];

                        // อัปเดตค่า static ด้วย
                        staticTR_1 = currentValues[0];
                        staticTJ_1 = currentValues[1];
                        staticRPM_1 = currentValues[2];
                        staticExt_1 = currentValues[8];
                        hasReceivedData_1 = true;

                        // อัปเดต UI
                        //  Debug.WriteLine($"[UI1-Program] OnVisibleChanged: ใช้ข้อมูลจาก CurrentValues: TR1={lastTR1:F2}, TJ1={lastTJ1:F2}, RPM1={lastRPM1:F0}");
                        UpdateProgramUI_1();
                    }
                    else
                    {
                        //  Debug.WriteLine("[UI1-Program] OnVisibleChanged: ไม่มีข้อมูลใน CurrentValues");
                    }
                }

                // โหลดการตั้งค่าต่างๆ
                LoadSavedValues_1();
                LoadLampState_1();
                UpdateUIFromState_1();

                // เริ่ม timers ต่างๆ
                if (ProgramState_1.IsStarted_1 && !ProgramState_1.IsPaused_1 && !countdownTimer_1.Enabled)
                    countdownTimer_1.Start();
                else if (ProgramState_1.IsStarted_1 && ProgramState_1.IsPaused_1)
                    lastCountdownTick_1 = DateTime.Now;   // baseline ใหม่ตอน pause

                if (updateTimer_1 != null && !updateTimer_1.Enabled)
                    updateTimer_1.Start();

                if (settingsUpdateTimer_1 != null && !settingsUpdateTimer_1.Enabled)
                    settingsUpdateTimer_1.Start();
            }
            else
            {
                // กรณีออกจากหน้านี้
              //  Debug.WriteLine("[UI1-Program] OnVisibleChanged: ออกจากหน้า UC_PROGRAM_CONTROL_SET_1");

                // ไม่ต้องยกเลิกการลงทะเบียนรับข้อมูล เพื่อให้ยังรับข้อมูลได้แม้ไม่แสดงหน้า
                // SerialPortManager.Instance.UI1DataReceivedEvent -= OnUI1DataReceived;
                // SerialPortManager.Instance.DataReceivedEvent -= OnDataReceived;

                // บันทึกค่าล่าสุดไว้ในตัวแปร static ก่อนออกจากหน้า
                staticTR_1 = lastTR_1;
                staticTJ_1 = lastTJ_1;
                staticRPM_1 = lastRPM_1;
                staticExt_1 = lastExt_1;
                hasReceivedData_1 = true;

                // หยุด timer เพื่อประหยัดทรัพยากร
                if (updateTimer_1 != null)
                    updateTimer_1.Stop();

                if (settingsUpdateTimer_1 != null)
                    settingsUpdateTimer_1.Stop();

                if (countdownTimer_1 != null && countdownTimer_1.Enabled)
                    countdownTimer_1.Stop();

                // บันทึกค่าสถานะปัจจุบัน
                SaveLampState_1();
                if (settings_1 != null)
                    settings_1.SaveToFile_1("Data_Set1.xml", "Combined");
            }
        }

        private void But_GoHome3_Click(object sender, EventArgs e)
        {
            try
            {
                // เปลี่ยนหน้าโดยใช้ NavigationHelper พร้อมส่ง cleanup action
                NavigationHelper.NavigateTo<UC_CONTROL_SET_1>(() => {
                    // หยุด timers
                    if (countdownTimer_1 != null) countdownTimer_1.Stop();
                    if (updateTimer_1 != null) updateTimer_1.Stop();
                    if (settingsUpdateTimer_1 != null) settingsUpdateTimer_1.Stop();

                    // ยกเลิกการลงทะเบียน event
                    if (SerialPortManager.Instance != null)
                    {
                        SerialPortManager.Instance.DataReceivedEvent -= OnDataReceived_1;
                        SerialPortManager.Instance.UI1DataReceivedEvent -= OnUI1DataReceived_1;
                    }

                    // บันทึกข้อมูล
                    SaveLampState_1();
                    if (settings_1 != null)
                        settings_1.SaveToFile_1("Data_Set1.xml", "Combined");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC1-Program][ERROR] But_GoHome3_Click: {ex.Message}");
                MessageBox.Show($"เกิดข้อผิดพลาดในการกลับหน้าหลัก: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CleanupResources_1()
        {
            Debug.WriteLine("[UC1-Program] CleanupResources started");

            try
            {
                // หยุด timers ทั้งหมด
                if (countdownTimer_1 != null) countdownTimer_1.Stop();
                if (updateTimer_1 != null) updateTimer_1.Stop();
                if (settingsUpdateTimer_1 != null) settingsUpdateTimer_1.Stop();

                // ยกเลิกการลงทะเบียน event
                if (SerialPortManager.Instance != null)
                {
                    SerialPortManager.Instance.DataReceivedEvent -= OnDataReceived_1;
                    SerialPortManager.Instance.UI1DataReceivedEvent -= OnUI1DataReceived_1;
                }

                // รีเซ็ตสถานะโปรแกรม (ถ้าจำเป็น)
                if (isStarted_1 && !isPaused_1)
                {
                    ProgramState_1.IsStarted_1 = false;
                    isStarted_1 = false;
                }

                // บันทึกข้อมูล
                SaveLampState_1();
                if (settings_1 != null)
                    settings_1.SaveToFile_1("Data_Set1.xml", "Combined");

                UnregisterEvents_1();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UC1-Program][ERROR] CleanupResources: {ex.Message}");
            }

            Debug.WriteLine("[UC1-Program] CleanupResources completed");
        }
        private void switch_A_M_ToggleChanged_1(object sender, EventArgs e)
        {
          //  Debug.WriteLine("switch_A_M1: " + (switch_A_M1.Mode == "Auto" ? "Auto mode activated" : "Manual mode activated"));
            ProgramState_1.SwitchA_M1 = switch_A_M1.IsOn;
        }



        // *** ฟังก์ชันส่งคำสั่งโดยเรียงลำดับและเพิ่ม Delay ***
        // UC_PROGRAM_CONTROL_SET_1.cs
        // -----------------------------------------------------------------------------
        // เฉพาะส่วน SendStepSetpointsToSerialAndDisplay ที่แก้ไขให้รองรับเฉพาะ UI1
        // -----------------------------------------------------------------------------
        private void SendStepSetpointsToSerialAndDisplay_1(int step)
        {
            // --- 1) อ่านค่าจาก UI1 (ชุด 1) ---
            float temp1 = 0f;
            int rpm1 = 0;
            bool thermo1IsTJ = false;

            switch (step)
            {
                case 1:
                    float.TryParse(text1_Temp1.Text, out temp1);
                    int.TryParse(text1_RPM1.Text, out rpm1);
                    thermo1IsTJ = switch_Target1_1.IsOn;
                    break;
                case 2:
                    float.TryParse(text1_Temp2.Text, out temp1);
                    int.TryParse(text1_RPM2.Text, out rpm1);
                    thermo1IsTJ = switch_Target1_2.IsOn;
                    break;
                case 3:
                    float.TryParse(text1_Temp3.Text, out temp1);
                    int.TryParse(text1_RPM3.Text, out rpm1);
                    thermo1IsTJ = switch_Target1_3.IsOn;
                    break;
                case 4:
                    float.TryParse(text1_Temp4.Text, out temp1);
                    int.TryParse(text1_RPM4.Text, out rpm1);
                    thermo1IsTJ = switch_Target1_4.IsOn;
                    break;
                case 5:
                    float.TryParse(text1_Temp5.Text, out temp1);
                    int.TryParse(text1_RPM5.Text, out rpm1);
                    thermo1IsTJ = switch_Target1_5.IsOn;
                    break;
                case 6:
                    float.TryParse(text1_Temp6.Text, out temp1);
                    int.TryParse(text1_RPM6.Text, out rpm1);
                    thermo1IsTJ = switch_Target1_6.IsOn;
                    break;
                case 7:
                    float.TryParse(text1_Temp7.Text, out temp1);
                    int.TryParse(text1_RPM7.Text, out rpm1);
                    thermo1IsTJ = switch_Target1_7.IsOn;
                    break;
                case 8:
                    float.TryParse(text1_Temp8.Text, out temp1);
                    int.TryParse(text1_RPM8.Text, out rpm1);
                    thermo1IsTJ = switch_Target1_8.IsOn;
                    break;
            }

            // --- 2) ส่งคำสั่ง TR/TJ เฉพาะ UI1 (ให้ UI2 เป็น false) ---
            var trtjFrame = CommandHelper.BuildDualThermostatTRTJ(thermo1IsTJ, false);
            SerialPortManager.Instance.Send(trtjFrame);
           // Debug.WriteLine($"[CommandHelper] [UI1] BuildDualThermostatTRTJ: " +
                  //  $"T1={(thermo1IsTJ ? "TJ" : "TR")}({(thermo1IsTJ ? "0x01" : "0x00")}), " +
                  //  $"T2=TR(0x00)");
            SerialPortManager.Instance.Send(trtjFrame);
          //  Debug.WriteLine($"[UI1] DualThermostatTRTJ Step{step} sent: {BitConverter.ToString(trtjFrame)}");
            Thread.Sleep(150);


            // --- 3) แปลงค่า temp1, rpm1 → high/low byte ---
            int t1 = (int)Math.Round(temp1);
            byte t1H = (byte)(t1 >> 8), t1L = (byte)(t1 & 0xFF);
            byte r1H = (byte)(rpm1 >> 8), r1L = (byte)(rpm1 & 0xFF);

            // --- 4) ส่ง SetTemp สำหรับ UI1 ตาม Step ---
            // --- 4) ส่ง SetTemp สำหรับ UI1 ตาม Step ---
         //  Debug.WriteLine($"[CommandHelper] [UI1] BuildSetTempStep{step}: Hi=0x{t1H:X2}, Lo=0x{t1L:X2} = {t1}");
            byte[] frame = Array.Empty<byte>();      // <<< Line ~975
            byte[] rpmFrame = Array.Empty<byte>();   // <<< Line ~976


            // --- 4) ส่ง SetTemp สำหรับ UI1 ตาม Step ---
            switch (step)
            {
                case 1:
                    frame = CommandHelper.BuildSet1_TempStep1(t1H, t1L);
                    SerialPortManager.Instance.Send(frame);
                  //  Debug.WriteLine($"[UI1] SetTemp Step1 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 2:
                    frame = CommandHelper.BuildSet1_TempStep2(t1H, t1L);
                    SerialPortManager.Instance.Send(frame);
                  //  Debug.WriteLine($"[UI1] SetTemp Step2 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 3:
                    frame = CommandHelper.BuildSet1_TempStep3(t1H, t1L);
                    SerialPortManager.Instance.Send(frame);
                   // Debug.WriteLine($"[UI1] SetTemp Step3 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 4:
                    frame = CommandHelper.BuildSet1_TempStep4(t1H, t1L);
                    SerialPortManager.Instance.Send(frame);
                  //  Debug.WriteLine($"[UI1] SetTemp Step4 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 5:
                    frame = CommandHelper.BuildSet1_TempStep5(t1H, t1L);
                    SerialPortManager.Instance.Send(frame);
                  //  Debug.WriteLine($"[UI1] SetTemp Step5 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 6:
                    frame = CommandHelper.BuildSet1_TempStep6(t1H, t1L);
                    SerialPortManager.Instance.Send(frame);
                  //  Debug.WriteLine($"[UI1] SetTemp Step6 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 7:
                    frame = CommandHelper.BuildSet1_TempStep7(t1H, t1L);
                    SerialPortManager.Instance.Send(frame);
                 //   Debug.WriteLine($"[UI1] SetTemp Step7 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
                case 8:
                    frame = CommandHelper.BuildSet1_TempStep8(t1H, t1L);
                    SerialPortManager.Instance.Send(frame);
                   // Debug.WriteLine($"[UI1] SetTemp Step8 sent: {BitConverter.ToString(frame)}");
                    Thread.Sleep(50);
                    break;
            }
            SerialPortManager.Instance.Send(frame);
           // Debug.WriteLine($"[UI1] SetTemp Step{step} sent: {BitConverter.ToString(frame)}");
            Thread.Sleep(50);
            // --- 5) ส่ง SetRPM สำหรับ UI1 ตาม Step ---
            switch (step)
            {
                case 1:
                    rpmFrame = CommandHelper.BuildSet1_RPMStep1(r1H, r1L);
                    SerialPortManager.Instance.Send(rpmFrame);
                  //  Debug.WriteLine($"[UI1] SetRPM  Step1 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 2:
                    rpmFrame = CommandHelper.BuildSet1_RPMStep2(r1H, r1L);
                    SerialPortManager.Instance.Send(rpmFrame);
                  //  Debug.WriteLine($"[UI1] SetRPM  Step2 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 3:
                    rpmFrame = CommandHelper.BuildSet1_RPMStep3(r1H, r1L);
                    SerialPortManager.Instance.Send(rpmFrame);
                   // Debug.WriteLine($"[UI1] SetRPM  Step3 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 4:
                    rpmFrame = CommandHelper.BuildSet1_RPMStep4(r1H, r1L);
                    SerialPortManager.Instance.Send(rpmFrame);
                  //  Debug.WriteLine($"[UI1] SetRPM  Step4 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 5:
                    rpmFrame = CommandHelper.BuildSet1_RPMStep5(r1H, r1L);
                    SerialPortManager.Instance.Send(rpmFrame);
                  //  Debug.WriteLine($"[UI1] SetRPM  Step5 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 6:
                    rpmFrame = CommandHelper.BuildSet1_RPMStep6(r1H, r1L);
                    SerialPortManager.Instance.Send(rpmFrame);
                  //  Debug.WriteLine($"[UI1] SetRPM  Step6 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 7:
                    rpmFrame = CommandHelper.BuildSet1_RPMStep7(r1H, r1L);
                    SerialPortManager.Instance.Send(rpmFrame);
                 //   Debug.WriteLine($"[UI1] SetRPM  Step7 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
                case 8:
                    rpmFrame = CommandHelper.BuildSet1_RPMStep8(r1H, r1L);
                    SerialPortManager.Instance.Send(rpmFrame);
                 //   Debug.WriteLine($"[UI1] SetRPM  Step8 sent: {BitConverter.ToString(rpmFrame)}");
                    Thread.Sleep(50);
                    break;
            }
            SerialPortManager.Instance.Send(rpmFrame);
           // Debug.WriteLine($"[UI1] SetRPM  Step{step} sent: {BitConverter.ToString(rpmFrame)}");
            Thread.Sleep(50);

            // --- 6) อัปเดต Label บน UI ---
            UpdateSetpointLabels_1();
        }




        // which step we’re waiting on
        private void StartStepCountdown_1(int step)
        {
            try
            {
                // อ่าน Hr/Min
                int hr = 0, min = 0;
                var tbHr = Controls.Find($"text1_Hr{step}", true).FirstOrDefault() as TextBox;
                var tbMin = Controls.Find($"text1_Min{step}", true).FirstOrDefault() as TextBox;

                if (tbHr != null && !int.TryParse(tbHr.Text, out hr))
                {
                    //Debug.WriteLine($"WARNING: Could not parse hour value from {tbHr.Text}");
                    hr = 0;
                }

                if (tbMin != null && !int.TryParse(tbMin.Text, out min))
                {
                   // Debug.WriteLine($"WARNING: Could not parse minute value from {tbMin.Text}");
                    min = 1; // Default to 1 minute
                }

                remainingSeconds_1 = hr * 3600 + min * 60;
                if (remainingSeconds_1 <= 0)
                {
                  //  Debug.WriteLine($"ERROR: Step {step}: invalid countdown ({remainingSeconds}s), abort.");
                    MessageBox.Show($"ไม่สามารถเริ่มนับเวลาสำหรับ Step {step} ได้ กรุณาตรวจสอบค่าเวลาที่ป้อน", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                totalStepSeconds_1 = remainingSeconds_1;

                // ตั้ง state และ UI พื้นฐาน
                currentStep_1 = step;
                ProgramState_1.CurrentStep_1 = step;
                ProgramState_1.RemainingSeconds_1 = remainingSeconds_1;
                label_Process1Step.Text = step.ToString();
                UpdatePicTarget_1(step);
                UpdatePicModeTime_1(step);

                // ส่งค่าไปยัง BoxControl
                SendStepSetpointsToSerialAndDisplay_1(step);

                // แสดงเวลาเริ่มต้นเสมอ
                int dispHr = remainingSeconds_1 / 3600;
                int dispMn = (remainingSeconds_1 % 3600) / 60;
                int dispSc = remainingSeconds_1 % 60;
                label_Process1Time_Seg.Text = $"{dispHr}:{dispMn:D2}:{dispSc:D2}";
                label_Process1Hr.Text = dispHr.ToString();
                label_Process1Min.Text = dispMn.ToString();
                label_Process1Sec.Text = dispSc.ToString();
                label_Process1Time_Left.Text = $"0:00:00";

                // เช็กโหมด RUN/WAIT
                bool isRunMode = IsStepTimeRun_1(step);
                bool isTJmode = IsStepTargetTJ_1(step);
               // Debug.WriteLine($"Step {step}: Mode={(isRunMode ? "RUN" : "WAIT")}, Channel={(isTJmode ? "TJ" : "TR")}.");

                if (isRunMode)
                {
                    // RUN → สตาร์ทนับถอยหลังทันที
                    waitingForThreshold_1 = false;
                  //  Debug.WriteLine($"Step {step}: RUN mode - starting countdown immediately");
                    if (!countdownTimer_1.Enabled)
                    {
                        countdownTimer_1.Start();
                      //  Debug.WriteLine($"Step {step}: countdown timer started");
                    }
                }
                else
                {
                    // WAIT → รอ threshold ก่อนนับ
                    waitingForThreshold_1 = true;
                    waitStep_1 = step;
                    double targetTemp = GetTargetTemp_1(step);
                   // Debug.WriteLine($"Step {step}: WAIT mode - waiting for temperature to reach {targetTemp}");

                    // ตรวจสอบทันทีว่าอุณหภูมิปัจจุบันถึงเป้าหมายหรือไม่
                    CheckTemperatureThreshold_1(step);
                }

                isStarted_1 = true;

                // อัปเดต UI ที่เกี่ยวข้อง
                UpdateLampStatus_1();
            }
            catch (Exception ex)
            {
               // Debug.WriteLine($"[ERROR] StartStepCountdown: {ex.Message}");
                MessageBox.Show($"เกิดข้อผิดพลาดในการเริ่ม Step {step}: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// ตรวจสอบทันทีว่าอุณหภูมิปัจจุบันถึงเป้าหมายหรือไม่ (สำหรับโหมด WAIT)
        /// </summary>
        private void CheckTemperatureThreshold_1(int step)
        {
            try
            {
                if (!waitingForThreshold_1 || step <= 0)
                    return;

                double target = GetTargetTemp_1(step);
                bool isTjMode = IsStepTargetTJ_1(step);
                double actual;

                // ใช้ค่า TR หรือ TJ ตามที่เลือกไว้
                if (latestValues_1 != null && latestValues_1.Length > 1)
                {
                    actual = isTjMode ? latestValues_1[1] : latestValues_1[0];
                }
                else
                {
                    actual = isTjMode ? lastTJ_1 : lastTR_1;
                }

               // Debug.WriteLine($"[Immediate Check] Step {step}: {(isTjMode ? "TJ" : "TR")}={actual:F2}, Target={target:F2}");

                // หากอุณหภูมิถึงเป้าหมายหรือสูงกว่าแล้ว ให้เริ่มนับเวลาทันที
                if (actual >= target)
                {
                    waitingForThreshold_1 = false;
                   // Debug.WriteLine($"[Immediate Check] Step {step}: temperature already above threshold, starting countdown immediately");

                    if (!countdownTimer_1.Enabled)
                    {
                        countdownTimer_1.Start();
                       // Debug.WriteLine($"[Immediate Check] Step {step}: countdown timer started");
                    }

                    // อัปเดต UI
                    UpdateLampStatus_1();
                }
            }
            catch (Exception ex)
            {
               // Debug.WriteLine($"[ERROR] CheckTemperatureThreshold: {ex.Message}");
            }
        }
        private void NextStep_1()
        {
            if (isNextStepProcessing_1)
                return;
            isNextStepProcessing_1 = true;

            if (!completedSteps_1.Contains(currentStep_1))
            {
                completedSteps_1.Add(currentStep_1);
               // Debug.WriteLine("Step " + currentStep + " is marked as completed.");
            }

            int nextEnabledStep = currentStep_1;
            bool found = false;
            for (int i = currentStep_1 + 1; i <= 8; i++)
            {
                if (!completedSteps_1.Contains(i) && IsStepToggleOn_1(i))
                {
                    nextEnabledStep = i;
                    found = true;
                    break;
                }
            }
            if (found)
            {
                currentStep_1 = nextEnabledStep;
                remainingSeconds_1 = GetStepCountdownSeconds_1(currentStep_1);
                if (remainingSeconds_1 <= 0)
                {
                    //  Debug.WriteLine("Step " + currentStep + " countdown is 0. Reloading data from Data_Set1.xml.");
                    settings_1 = Data_Set1.LoadFromFile_1("Data_Set1.xml");
                    remainingSeconds_1 = GetStepCountdownSeconds_1(currentStep_1);
                    if (remainingSeconds_1 <= 0)
                    {
                     //   Debug.WriteLine("After reloading, Step " + currentStep + " countdown is still 0. Cannot proceed.");
                        MessageBox.Show("Countdown for Step " + currentStep_1 + " is not set. Please enter a valid countdown time before proceeding.");
                        isNextStepProcessing_1 = false;
                        return;
                    }
                }
                ProgramState_1.CurrentStep_1 = currentStep_1;
                ProgramState_1.RemainingSeconds_1 = remainingSeconds_1;
               // Debug.WriteLine("Starting Step " + currentStep + " with countdown: " + remainingSeconds + " seconds.");
                StartStepCountdown_1(currentStep_1);
            }
            else
            {
                // Debug.WriteLine("All steps completed.");
                isStarted_1 = false;
                ProgramState_1.IsStarted_1 = false;
                ProgramState_1.RemainingSeconds_1 = 0;
                remainingSeconds_1 = 0;
                label_Process1Time_Seg.Text = "0:00:00";
                label_Process1Time_Left.Text = "0:00:00";
                label_Process1Hr.Text = "0";
                label_Process1Min.Text = "0";
                label_Process1Sec.Text = "0";

                switch_system1_1.IsOn = false;
                switch_system1_2.IsOn = true;
                UpdateSetpointLabels_1();
                UpdatePicTarget_1(currentStep_1);
                UpdatePicModeTime_1(currentStep_1);
                Debug.WriteLine("Final state reached; lamp statuses remain unchanged. Press Start to reset and begin a new run.");
            }

            isNextStepProcessing_1 = false;
        }

        private bool IsStepToggleOn_1(int step)
        {
            switch (step)
            {
                case 1: return toggle_1SwitchControl1.IsOn;
                case 2: return toggle_1SwitchControl2.IsOn;
                case 3: return toggle_1SwitchControl3.IsOn;
                case 4: return toggle_1SwitchControl4.IsOn;
                case 5: return toggle_1SwitchControl5.IsOn;
                case 6: return toggle_1SwitchControl6.IsOn;
                case 7: return toggle_1SwitchControl7.IsOn;
                case 8: return toggle_1SwitchControl8.IsOn;
                default: return false;
            }
        }

        private void DisplayStepsStatus_1()
        {
            for (int step = 1; step <= 8; step++)
            {
                if (IsStepToggleOn_1(step))
                {
                    int secs = (step == currentStep_1) ? ProgramState_1.RemainingSeconds_1 : GetStepCountdownSeconds_1(step);
                   // Debug.WriteLine("Status: Step " + step + " countdown = " + secs + " seconds.");
                }
            }
        }

        // *** Modified GetStepCountdownSeconds_1 function with ErrorProvider validation ***
        private int GetStepCountdownSeconds_1(int step)
        {
            int hr = 0, min = 0;
            switch (step)
            {
                case 1:
                    if (!int.TryParse(text1_Hr1.Text, out hr))
                    {
                        errorProvider_1.SetError(text1_Hr1, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                       // Debug.WriteLine("Step 1: Failed to parse Hr from text1_Hr1. Using saved value: " + settings.Text1_Hr1);
                        int.TryParse(settings_1.Text1_Hr1, out hr);
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Hr1, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_1.SetError(text1_Hr1, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text1_Min1.Text, out min))
                    {
                        errorProvider_1.SetError(text1_Min1, "กรุณากรอกตัวเลขสำหรับนาที");
                       // Debug.WriteLine("Step 1: Failed to parse Min from text1_Min1. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Min1, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_1.SetError(text1_Min1, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 2:
                    if (!int.TryParse(text1_Hr2.Text, out hr))
                    {
                        errorProvider_1.SetError(text1_Hr2, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                       // Debug.WriteLine("Step 2: Failed to parse Hr from text1_Hr2. Using saved value: " + settings.Text1_Hr2);
                        int.TryParse(settings_1.Text1_Hr2, out hr);
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Hr2, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_1.SetError(text1_Hr2, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text1_Min2.Text, out min))
                    {
                        errorProvider_1.SetError(text1_Min2, "กรุณากรอกตัวเลขสำหรับนาที");
                      //  Debug.WriteLine("Step 2: Failed to parse Min from text1_Min2. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Min2, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_1.SetError(text1_Min2, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 3:
                    if (!int.TryParse(text1_Hr3.Text, out hr))
                    {
                        errorProvider_1.SetError(text1_Hr3, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                     //   Debug.WriteLine("Step 3: Failed to parse Hr from text1_Hr3. Using saved value: " + settings.Text1_Hr3);
                        int.TryParse(settings_1.Text1_Hr3, out hr);
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Hr3, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_1.SetError(text1_Hr3, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text1_Min3.Text, out min))
                    {
                        errorProvider_1.SetError(text1_Min3, "กรุณากรอกตัวเลขสำหรับนาที");
                       // Debug.WriteLine("Step 3: Failed to parse Min from text1_Min3. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Min3, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_1.SetError(text1_Min3, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 4:
                    if (!int.TryParse(text1_Hr4.Text, out hr))
                    {
                        errorProvider_1.SetError(text1_Hr4, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                      //  Debug.WriteLine("Step 4: Failed to parse Hr from text1_Hr4. Using saved value: " + settings.Text1_Hr4);
                        int.TryParse(settings_1.Text1_Hr4, out hr);
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Hr4, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_1.SetError(text1_Hr4, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text1_Min4.Text, out min))
                    {
                        errorProvider_1.SetError(text1_Min4, "กรุณากรอกตัวเลขสำหรับนาที");
                     //   Debug.WriteLine("Step 4: Failed to parse Min from text1_Min4. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Min4, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_1.SetError(text1_Min4, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 5:
                    if (!int.TryParse(text1_Hr5.Text, out hr))
                    {
                        errorProvider_1.SetError(text1_Hr5, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                      //  Debug.WriteLine("Step 5: Failed to parse Hr from text1_Hr5. Using saved value: " + settings.Text1_Hr5);
                        int.TryParse(settings_1.Text1_Hr5, out hr);
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Hr5, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_1.SetError(text1_Hr5, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text1_Min5.Text, out min))
                    {
                        errorProvider_1.SetError(text1_Min5, "กรุณากรอกตัวเลขสำหรับนาที");
                      //  Debug.WriteLine("Step 5: Failed to parse Min from text1_Min5. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Min5, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_1.SetError(text1_Min5, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 6:
                    if (!int.TryParse(text1_Hr6.Text, out hr))
                    {
                        errorProvider_1.SetError(text1_Hr6, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                      //  Debug.WriteLine("Step 6: Failed to parse Hr from text1_Hr6. Using saved value: " + settings.Text1_Hr6);
                        int.TryParse(settings_1.Text1_Hr6, out hr);
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Hr6, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_1.SetError(text1_Hr6, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text1_Min6.Text, out min))
                    {
                        errorProvider_1.SetError(text1_Min6, "กรุณากรอกตัวเลขสำหรับนาที");
                     //   Debug.WriteLine("Step 6: Failed to parse Min from text1_Min6. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Min6, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_1.SetError(text1_Min6, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 7:
                    if (!int.TryParse(text1_Hr7.Text, out hr))
                    {
                        errorProvider_1.SetError(text1_Hr7, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                      //  Debug.WriteLine("Step 7: Failed to parse Hr from text1_Hr7. Using saved value: " + settings.Text1_Hr7);
                        int.TryParse(settings_1.Text1_Hr7, out hr);
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Hr7, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_1.SetError(text1_Hr7, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text1_Min7.Text, out min))
                    {
                        errorProvider_1.SetError(text1_Min7, "กรุณากรอกตัวเลขสำหรับนาที");
                      //  Debug.WriteLine("Step 7: Failed to parse Min from text1_Min7. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Min7, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_1.SetError(text1_Min7, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                case 8:
                    if (!int.TryParse(text1_Hr8.Text, out hr))
                    {
                        errorProvider_1.SetError(text1_Hr8, "กรุณากรอกตัวเลขสำหรับชั่วโมง");
                      //  Debug.WriteLine("Step 8: Failed to parse Hr from text1_Hr8. Using saved value: " + settings.Text1_Hr8);
                        int.TryParse(settings_1.Text1_Hr8, out hr);
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Hr8, "");
                        if (hr < 0 || hr > 99)
                        {
                            errorProvider_1.SetError(text1_Hr8, "ชั่วโมงต้องอยู่ในช่วง 0-99");
                            hr = 0;
                        }
                    }
                    if (!int.TryParse(text1_Min8.Text, out min))
                    {
                        errorProvider_1.SetError(text1_Min8, "กรุณากรอกตัวเลขสำหรับนาที");
                     //   Debug.WriteLine("Step 8: Failed to parse Min from text1_Min8. Defaulting Min to 1.");
                        min = 1;
                    }
                    else
                    {
                        errorProvider_1.SetError(text1_Min8, "");
                        if (min < 1 || min > 59)
                        {
                            errorProvider_1.SetError(text1_Min8, "นาทีต้องอยู่ในช่วง 1-59");
                            min = 1;
                        }
                    }
                    break;
                default:
                    //  Debug.WriteLine("GetStepCountdownSeconds_1: Invalid step number " + step);
                    break;
            }
            int totalSeconds = hr * 3600 + min * 60;
          //  Debug.WriteLine($"Step {step}: Parsed Hr = {hr}, Min = {min} --> Total seconds = {totalSeconds}");
            return totalSeconds;
        }
        // --- End Modified GetStepCountdownSeconds_1 ***

        private void UpdateSetpointLabels_1()
        {
            if ((DateTime.Now - lastSetpointUpdateTime_1).TotalMilliseconds < 1000)
                return;
            lastSetpointUpdateTime_1 = DateTime.Now;

            double temp = 0, rpm = 0, dosing = 0;
            switch (currentStep_1)
            {
                case 1:
                    temp = (settings_1.Target_Tj != 0 ? settings_1.Target_Tj : settings_1.Target_Tr);
                    double.TryParse(settings_1.Text1_RPM1, out rpm);
                    double.TryParse(settings_1.Text1_Dosing1, out dosing);
                    break;
                case 2:
                    temp = (settings_1.Target1_Tj2 != 0 ? settings_1.Target1_Tj2 : settings_1.Target1_Tr2);
                    double.TryParse(settings_1.Text1_RPM2, out rpm);
                    double.TryParse(settings_1.Text1_Dosing2, out dosing);
                    break;
                case 3:
                    temp = (settings_1.Target1_Tj3 != 0 ? settings_1.Target1_Tj3 : settings_1.Target1_Tr3);
                    double.TryParse(settings_1.Text1_RPM3, out rpm);
                    double.TryParse(settings_1.Text1_Dosing3, out dosing);
                    break;
                case 4:
                    temp = (settings_1.Target1_Tj4 != 0 ? settings_1.Target1_Tj4 : settings_1.Target1_Tr4);
                    double.TryParse(settings_1.Text1_RPM4, out rpm);
                    double.TryParse(settings_1.Text1_Dosing4, out dosing);
                    break;
                case 5:
                    temp = (settings_1.Target1_Tj5 != 0 ? settings_1.Target1_Tj5 : settings_1.Target1_Tr5);
                    double.TryParse(settings_1.Text1_RPM5, out rpm);
                    double.TryParse(settings_1.Text1_Dosing5, out dosing);
                    break;
                case 6:
                    temp = (settings_1.Target1_Tj6 != 0 ? settings_1.Target1_Tj6 : settings_1.Target1_Tr6);
                    double.TryParse(settings_1.Text1_RPM6, out rpm);
                    double.TryParse(settings_1.Text1_Dosing6, out dosing);
                    break;
                case 7:
                    temp = (settings_1.Target1_Tj7 != 0 ? settings_1.Target1_Tj7 : settings_1.Target1_Tr7);
                    double.TryParse(settings_1.Text1_RPM7, out rpm);
                    double.TryParse(settings_1.Text1_Dosing7, out dosing);
                    break;
                case 8:
                    temp = (settings_1.Target1_Tj8 != 0 ? settings_1.Target1_Tj8 : settings_1.Target1_Tr8);
                    double.TryParse(settings_1.Text1_RPM8, out rpm);
                    double.TryParse(settings_1.Text1_Dosing8, out dosing);
                    break;
            }
            label_Process1_TempStep.Text = temp.ToString("F2");
            label_Process1RPM_SP.Text = rpm.ToString("F2");
            label_Process1Dosing.Text = dosing.ToString("F2");
          //  Debug.WriteLine("UpdateSetpointLabels => currentStep=" + currentStep);
        }

        private void UpdatePicTarget_1(int step)
        {
            bool isTj = false;
            switch (step)
            {
                case 1: isTj = switch_Target1_1.IsOn; break;
                case 2: isTj = switch_Target1_2.IsOn; break;
                case 3: isTj = switch_Target1_3.IsOn; break;
                case 4: isTj = switch_Target1_4.IsOn; break;
                case 5: isTj = switch_Target1_5.IsOn; break;
                case 6: isTj = switch_Target1_6.IsOn; break;
                case 7: isTj = switch_Target1_7.IsOn; break;
                case 8: isTj = switch_Target1_8.IsOn; break;
            }
            pic_Target1.Mode = isTj ? "TJ" : "TR";
        }

        private void UpdatePicModeTime_1(int step)
        {
            bool timeOn = false;
            switch (step)
            {
                case 1: timeOn = switch_Time1_1.IsOn; break;
                case 2: timeOn = switch_Time1_2.IsOn; break;
                case 3: timeOn = switch_Time1_3.IsOn; break;
                case 4: timeOn = switch_Time1_4.IsOn; break;
                case 5: timeOn = switch_Time1_5.IsOn; break;
                case 6: timeOn = switch_Time1_6.IsOn; break;
                case 7: timeOn = switch_Time1_7.IsOn; break;
                case 8: timeOn = switch_Time1_8.IsOn; break;
            }
            pic_ModeTime1.Mode = timeOn ? "Run" : "Wait";
        }

        // --- Update Functions สำหรับแต่ละ Step ---
        private void UpdateStep1_1()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text1_Temp1.Text, out tempVal);
            int.TryParse(text1_RPM1.Text, out rpmVal);
            float.TryParse(text1_Dosing1.Text, out dosingVal);
            int.TryParse(text1_Hr1.Text, out hrVal);
            int.TryParse(text1_Min1.Text, out minVal);
            settings_1.Text1_Temp1 = tempVal.ToString();
            settings_1.Text1_RPM1 = rpmVal.ToString();
            settings_1.Text1_Dosing1 = dosingVal.ToString();
            settings_1.Text1_Hr1 = hrVal.ToString();
            settings_1.Text1_Min1 = minVal.ToString();
          //  Debug.WriteLine("Step 1: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            Target_Tr_1 = (switch_Target1_1.IsOn) ? 0 : tempVal;
            Target_Tj_1 = (switch_Target1_1.IsOn) ? tempVal : 0;
        }

        private void UpdateStep2_1()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text1_Temp2.Text, out tempVal);
            int.TryParse(text1_RPM2.Text, out rpmVal);
            float.TryParse(text1_Dosing2.Text, out dosingVal);
            int.TryParse(text1_Hr2.Text, out hrVal);
            int.TryParse(text1_Min2.Text, out minVal);
            settings_1.Text1_Temp2 = tempVal.ToString();
            settings_1.Text1_RPM2 = rpmVal.ToString();
            settings_1.Text1_Dosing2 = dosingVal.ToString();
            settings_1.Text1_Hr2 = hrVal.ToString();
            settings_1.Text1_Min2 = minVal.ToString();
          //  Debug.WriteLine("Step 2: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target1_2.IsOn)
            {
                Target1_Tj2 = tempVal;
                Target1_Tr2 = 0;
            }
            else
            {
                Target1_Tr2 = tempVal;
                Target1_Tj2 = 0;
            }
        }

        private void UpdateStep3_1()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text1_Temp3.Text, out tempVal);
            int.TryParse(text1_RPM3.Text, out rpmVal);
            float.TryParse(text1_Dosing3.Text, out dosingVal);
            int.TryParse(text1_Hr3.Text, out hrVal);
            int.TryParse(text1_Min3.Text, out minVal);
            settings_1.Text1_Temp3 = tempVal.ToString();
            settings_1.Text1_RPM3 = rpmVal.ToString();
            settings_1.Text1_Dosing3 = dosingVal.ToString();
            settings_1.Text1_Hr3 = hrVal.ToString();
            settings_1.Text1_Min3 = minVal.ToString();
          //  Debug.WriteLine("Step 3: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target1_3.IsOn)
            {
                Target1_Tj3 = tempVal;
                Target1_Tr3 = 0;
            }
            else
            {
                Target1_Tr3 = tempVal;
                Target1_Tj3 = 0;
            }
        }

        private void UpdateStep4_1()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text1_Temp4.Text, out tempVal);
            int.TryParse(text1_RPM4.Text, out rpmVal);
            float.TryParse(text1_Dosing4.Text, out dosingVal);
            int.TryParse(text1_Hr4.Text, out hrVal);
            int.TryParse(text1_Min4.Text, out minVal);
            settings_1.Text1_Temp4 = tempVal.ToString();
            settings_1.Text1_RPM4 = rpmVal.ToString();
            settings_1.Text1_Dosing4 = dosingVal.ToString();
            settings_1.Text1_Hr4 = hrVal.ToString();
            settings_1.Text1_Min4 = minVal.ToString();
         //   Debug.WriteLine("Step 4: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target1_4.IsOn)
            {
                Target1_Tj4 = tempVal;
                Target1_Tr4 = 0;
            }
            else
            {
                Target1_Tr4 = tempVal;
                Target1_Tj4 = 0;
            }
        }

        private void UpdateStep5_1()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text1_Temp5.Text, out tempVal);
            int.TryParse(text1_RPM5.Text, out rpmVal);
            float.TryParse(text1_Dosing5.Text, out dosingVal);
            int.TryParse(text1_Hr5.Text, out hrVal);
            int.TryParse(text1_Min5.Text, out minVal);
            settings_1.Text1_Temp5 = tempVal.ToString();
            settings_1.Text1_RPM5 = rpmVal.ToString();
            settings_1.Text1_Dosing5 = dosingVal.ToString();
            settings_1.Text1_Hr5 = hrVal.ToString();
            settings_1.Text1_Min5 = minVal.ToString();
          //  Debug.WriteLine("Step 5: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target1_5.IsOn)
            {
                Target1_Tj5 = tempVal;
                Target1_Tr5 = 0;
            }
            else
            {
                Target1_Tr5 = tempVal;
                Target1_Tj5 = 0;
            }
        }

        private void UpdateStep6_1()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text1_Temp6.Text, out tempVal);
            int.TryParse(text1_RPM6.Text, out rpmVal);
            float.TryParse(text1_Dosing6.Text, out dosingVal);
            int.TryParse(text1_Hr6.Text, out hrVal);
            int.TryParse(text1_Min6.Text, out minVal);
            settings_1.Text1_Temp6 = tempVal.ToString();
            settings_1.Text1_RPM6 = rpmVal.ToString();
            settings_1.Text1_Dosing6 = dosingVal.ToString();
            settings_1.Text1_Hr6 = hrVal.ToString();
            settings_1.Text1_Min6 = minVal.ToString();
         //   Debug.WriteLine("Step 6: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target1_6.IsOn)
            {
                Target1_Tj6 = tempVal;
                Target1_Tr6 = 0;
            }
            else
            {
                Target1_Tr6 = tempVal;
                Target1_Tj6 = 0;
            }
        }

        private void UpdateStep7_1()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text1_Temp7.Text, out tempVal);
            int.TryParse(text1_RPM7.Text, out rpmVal);
            float.TryParse(text1_Dosing7.Text, out dosingVal);
            int.TryParse(text1_Hr7.Text, out hrVal);
            int.TryParse(text1_Min7.Text, out minVal);
            settings_1.Text1_Temp7 = tempVal.ToString();
            settings_1.Text1_RPM7 = rpmVal.ToString();
            settings_1.Text1_Dosing7 = dosingVal.ToString();
            settings_1.Text1_Hr7 = hrVal.ToString();
            settings_1.Text1_Min7 = minVal.ToString();
          //  Debug.WriteLine("Step 7: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target1_7.IsOn)
            {
                Target1_Tj7 = tempVal;
                Target1_Tr7 = 0;
            }
            else
            {
                Target1_Tr7 = tempVal;
                Target1_Tj7 = 0;
            }
        }

        private void UpdateStep8_1()
        {
            float tempVal = 0;
            int rpmVal = 0;
            float dosingVal = 0;
            int hrVal = 0, minVal = 0;
            float.TryParse(text1_Temp8.Text, out tempVal);
            int.TryParse(text1_RPM8.Text, out rpmVal);
            float.TryParse(text1_Dosing8.Text, out dosingVal);
            int.TryParse(text1_Hr8.Text, out hrVal);
            int.TryParse(text1_Min8.Text, out minVal);
            settings_1.Text1_Temp8 = tempVal.ToString();
            settings_1.Text1_RPM8 = rpmVal.ToString();
            settings_1.Text1_Dosing8 = dosingVal.ToString();
            settings_1.Text1_Hr8 = hrVal.ToString();
            settings_1.Text1_Min8 = minVal.ToString();
          //  Debug.WriteLine("Step 8: Temp = " + tempVal + ", RPM = " + rpmVal + ", Dosing = " + dosingVal + ", Hr = " + hrVal + ", Min = " + minVal);
            if (switch_Target1_8.IsOn)
            {
                Target1_Tj8 = tempVal;
                Target1_Tr8 = 0;
            }
            else
            {
                Target1_Tr8 = tempVal;
                Target1_Tj8 = 0;
            }
        }
        // --- End Update Functions ---

        // --- Toggle event handlers สำหรับแต่ละ Step ---
        private void toggle_1SwitchControl1_Click(object sender, EventArgs e)
        {
            settings_1.Toggle_1SwitchControl1 = toggle_1SwitchControl1.IsOn;
            settings_1.Switch_Target1_1 = switch_Target1_1.IsOn;
            settings_1.Switch_Time1_1 = switch_Time1_1.IsOn;
            if (toggle_1SwitchControl1.IsOn)
            {
                UpdateStep1_1();
                settings_1.Target_Tr = Target_Tr_1;
                settings_1.Target_Tj = Target_Tj_1;
              //  Debug.WriteLine("Step 1 updated because toggle is ON.");
            }
            else
            {
              //  Debug.WriteLine("Toggle_1SwitchControl1 is OFF.");
            }
            settings_1.SaveToFile_1("Data_Set1.xml", "Step1");
        }

        private void toggle_1SwitchControl2_Click(object sender, EventArgs e)
        {
            settings_1.Toggle_1SwitchControl2 = toggle_1SwitchControl2.IsOn;
            settings_1.Switch_Target1_2 = switch_Target1_2.IsOn;
            settings_1.Switch_Time1_2 = switch_Time1_2.IsOn;
            if (toggle_1SwitchControl2.IsOn)
            {
                UpdateStep2_1();
                settings_1.Target1_Tr2 = Target1_Tr2;
                settings_1.Target1_Tj2 = Target1_Tj2;
              //  Debug.WriteLine("Step 2 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl2 is OFF.");
            }
            settings_1.SaveToFile_1("Data_Set1.xml", "Step2");
        }

        private void toggle_1SwitchControl3_Click(object sender, EventArgs e)
        {
            settings_1.Toggle_1SwitchControl3 = toggle_1SwitchControl3.IsOn;
            settings_1.Switch_Target1_3 = switch_Target1_3.IsOn;
            settings_1.Switch_Time1_3 = switch_Time1_3.IsOn;
            if (toggle_1SwitchControl3.IsOn)
            {
                UpdateStep3_1();
                settings_1.Target1_Tr3 = Target1_Tr3;
                settings_1.Target1_Tj3 = Target1_Tj3;
              //  Debug.WriteLine("Step 3 updated because toggle is ON.");
            }
            else
            {
              //  Debug.WriteLine("Toggle_1SwitchControl3 is OFF.");
            }
            settings_1.SaveToFile_1("Data_Set1.xml", "Step3");
        }

        private void toggle_1SwitchControl4_Click(object sender, EventArgs e)
        {
            settings_1.Toggle_1SwitchControl4 = toggle_1SwitchControl4.IsOn;
            settings_1.Switch_Target1_4 = switch_Target1_4.IsOn;
            settings_1.Switch_Time1_4 = switch_Time1_4.IsOn;
            if (toggle_1SwitchControl4.IsOn)
            {
                UpdateStep4_1();
                settings_1.Target1_Tr4 = Target1_Tr4;
                settings_1.Target1_Tj4 = Target1_Tj4;
              //  Debug.WriteLine("Step 4 updated because toggle is ON.");
            }
            else
            {
              //  Debug.WriteLine("Toggle_1SwitchControl4 is OFF.");
            }
            settings_1.SaveToFile_1("Data_Set1.xml", "Step4");
        }

        private void toggle_1SwitchControl5_Click(object sender, EventArgs e)
        {
            settings_1.Toggle_1SwitchControl5 = toggle_1SwitchControl5.IsOn;
            settings_1.Switch_Target1_5 = switch_Target1_5.IsOn;
            settings_1.Switch_Time1_5 = switch_Time1_5.IsOn;
            if (toggle_1SwitchControl5.IsOn)
            {
                UpdateStep5_1();
                settings_1.Target1_Tr5 = Target1_Tr5;
                settings_1.Target1_Tj5 = Target1_Tj5;
               // Debug.WriteLine("Step 5 updated because toggle is ON.");
            }
            else
            {
              //  Debug.WriteLine("Toggle_1SwitchControl5 is OFF.");
            }
            settings_1.SaveToFile_1("Data_Set1.xml", "Step5");
        }

        private void toggle_1SwitchControl6_Click(object sender, EventArgs e)
        {
            settings_1.Toggle_1SwitchControl6 = toggle_1SwitchControl6.IsOn;
            settings_1.Switch_Target1_6 = switch_Target1_6.IsOn;
            settings_1.Switch_Time1_6 = switch_Time1_6.IsOn;
            if (toggle_1SwitchControl6.IsOn)
            {
                UpdateStep6_1();
                settings_1.Target1_Tr6 = Target1_Tr6;
                settings_1.Target1_Tj6 = Target1_Tj6;
               // Debug.WriteLine("Step 6 updated because toggle is ON.");
            }
            else
            {
               // Debug.WriteLine("Toggle_1SwitchControl6 is OFF.");
            }
            settings_1.SaveToFile_1("Data_Set1.xml", "Step6");
        }

        private void toggle_1SwitchControl7_Click(object sender, EventArgs e)
        {
            settings_1.Toggle_1SwitchControl7 = toggle_1SwitchControl7.IsOn;
            settings_1.Switch_Target1_7 = switch_Target1_7.IsOn;
            settings_1.Switch_Time1_7 = switch_Time1_7.IsOn;
            if (toggle_1SwitchControl7.IsOn)
            {
                UpdateStep7_1();
                settings_1.Target1_Tr7 = Target1_Tr7;
                settings_1.Target1_Tj7 = Target1_Tj7;
              //  Debug.WriteLine("Step 7 updated because toggle is ON.");
            }
            else
            {
                Debug.WriteLine("Toggle_1SwitchControl7 is OFF.");
            }
            settings_1.SaveToFile_1("Data_Set1.xml", "Step7");
        }

        private void toggle_1SwitchControl8_Click(object sender, EventArgs e)
        {
            settings_1.Toggle_1SwitchControl8 = toggle_1SwitchControl8.IsOn;
            settings_1.Switch_Target1_8 = switch_Target1_8.IsOn;
            settings_1.Switch_Time1_8 = switch_Time1_8.IsOn;
            if (toggle_1SwitchControl8.IsOn)
            {
                UpdateStep8_1();
                settings_1.Target1_Tr8 = Target1_Tr8;
                settings_1.Target1_Tj8 = Target1_Tj8;
              //  Debug.WriteLine("Step 8 updated because toggle is ON.");
            }
            else
            {
             //   Debug.WriteLine("Toggle_1SwitchControl8 is OFF.");
            }
            settings_1.SaveToFile_1("Data_Set1.xml", "Step8");
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (this.ParentForm != null)
                this.ParentForm.FormClosing += ParentForm_FormClosing_1;
        }

        private void ParentForm_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            // บันทึก Lamp State และ settings ทั้งหมดก่อนโปรแกรมปิด
            SaveLampState_1();
            settings_1.SaveToFile_1("Data_Set1.xml", "Combined");
          //  Debug.WriteLine("Settings saved on closing.");
        }

        private void UpdateTimer_Tick_1(object sender, EventArgs e)
        {
            try
            {
                // ถ้าไม่เคยได้รับข้อมูลมาก่อน ให้ลองอัปเดตจาก CurrentValues
                if (!hasReceivedData_1)
                {
                    var currentValues = SerialPortManager.Instance.CurrentValues;
                    if (currentValues != null && currentValues.Length >= 9)
                    {
                        lastTR_1 = currentValues[0];
                        lastTJ_1 = currentValues[1];
                        lastRPM_1 = currentValues[2];
                        lastExt_1 = currentValues[8];

                        staticTR_1 = currentValues[0];
                        staticTJ_1 = currentValues[1];
                        staticRPM_1 = currentValues[2];
                        staticExt_1 = currentValues[8];
                        hasReceivedData_1 = true;

                        // อัปเดต UI
                        UpdateProgramUI_1();
                      //  Debug.WriteLine($"[UI1-Program] UpdateTimer_Tick: First update from CurrentValues: TR1={lastTR1:F2}, TJ1={lastTJ1:F2}, RPM1={lastRPM1:F0}");
                    }
                }
                else
                {
                    // อัปเดต UI จากค่าท้องถิ่น
                    UpdateProgramUI_1();
                }

                // (b) WAIT mode: รอ threshold
                if (isStarted_1 && waitingForThreshold_1)
                {
                    // ใช้ค่าอุณหภูมิที่ได้รับล่าสุด (latestValues หรือค่าท้องถิ่น)
                    double actual = 0;
                    double target = GetTargetTemp_1(waitStep_1);

                    // ตรวจสอบว่าควรใช้ค่า TR หรือ TJ
                    bool isTjMode = IsStepTargetTJ_1(waitStep_1);

                    // ใช้ค่าจาก latestValues หากมี หรือใช้ค่าท้องถิ่นแทน
                    if (latestValues_1 != null && latestValues_1.Length > 1)
                    {
                        actual = isTjMode ? latestValues_1[1] : latestValues_1[0]; // 1=TJ, 0=TR
                    }
                    else
                    {
                        actual = isTjMode ? lastTJ_1 : lastTR_1;
                    }

                  //  Debug.WriteLine($"Step {waitStep} WAIT: actual={actual:F2}, target={target:F2}, mode={(isTjMode ? "TJ" : "TR")}");

                    // เมื่อค่าอุณหภูมิถึงหรือมากกว่าค่าเป้าหมาย ให้เริ่มนับเวลาถอยหลัง
                    if (actual >= target)
                    {
                        waitingForThreshold_1 = false;
                      //  Debug.WriteLine($"Step {waitStep}: threshold met (actual={actual:F2} >= target={target:F2}), starting countdown.");

                        // เริ่มนับถอยหลังทันที
                        if (!countdownTimer_1.Enabled)
                        {
                            countdownTimer_1.Start();
                          //  Debug.WriteLine($"Step {waitStep}: countdown timer started");
                        }

                        // อัปเดต UI ให้แสดงว่าเริ่มนับเวลาแล้ว
                        UpdateLampStatus_1();
                    }
                }

                // (c) RUN mode หรือ หลัง threshold ผ่าน → นับถอยหลัง
                if (isStarted_1 && !waitingForThreshold_1)
                {
                    remainingSeconds_1 = Math.Max(remainingSeconds_1 - 1, 0);

                    if (remainingSeconds_1 == 0)
                    {
                     //   Debug.WriteLine($"Step {currentStep}: reached 00:00, completing step.");
                        OnStepCompleted_1(currentStep_1);
                        return;
                    }

                    // อัปเดต UI เกี่ยวกับเวลา
                    int hr = remainingSeconds_1 / 3600;
                    int mn = (remainingSeconds_1 % 3600) / 60;
                    int sc = remainingSeconds_1 % 60;
                    label_Process1Time_Seg.Text = $"{hr}:{mn:D2}:{sc:D2}";
                    label_Process1Hr.Text = hr.ToString();
                    label_Process1Min.Text = mn.ToString();
                    label_Process1Sec.Text = sc.ToString();

                    int elapsed = totalStepSeconds_1 - remainingSeconds_1;
                    int eHr = elapsed / 3600;
                    int eMn = (elapsed % 3600) / 60;
                    int eSc = elapsed % 60;
                    label_Process1Time_Left.Text = $"{eHr}:{eMn:D2}:{eSc:D2}";
                }
            }
            catch (Exception ex)
            {
               // Debug.WriteLine($"[UI1-Program][Error] UpdateTimer_Tick: {ex}");
            }
        }

        private void OnStepCompleted_1(int step)
        {
            countdownTimer_1?.Stop();
            //  Debug.WriteLine($"Step {step} completed; moving to next.");
            NextStep_1();
        }
        private double GetTargetTemp_1(int step)
        {
            try
            {
                var tb = Controls.Find($"text1_Temp{step}", true).FirstOrDefault() as TextBox;
                if (tb == null)
                {
                  //  Debug.WriteLine($"[ERROR] GetTargetTemp: Cannot find text1_Temp{step} control");
                    return double.NaN;
                }

                if (!double.TryParse(tb.Text, out double val))
                {
                  //  Debug.WriteLine($"[ERROR] GetTargetTemp: Invalid value in text1_Temp{step}: '{tb.Text}'");
                    return double.NaN;
                }

              //  Debug.WriteLine($"GetTargetTemp: Step {step} target temperature = {val:F2}");
                return val;
            }
            catch (Exception ex)
            {
              //  Debug.WriteLine($"[ERROR] GetTargetTemp: {ex.Message}");
                return double.NaN;
            }
        }

        private void RegisterTimeTextBox_1(TextBox tb)
        {
            if (tb == null) return;
            tb.TextChanged += (s, e) =>
            {
                UpdateDataSet_1(tb.Name, tb.Text);
                settings_1.SaveToFile_1("Data_Set1.xml", "Combined");
               // Debug.WriteLine(tb.Name + " changed to: " + tb.Text);
            };
            tb.MaxLength = 3;
        }

        private void RegisterTimeTextBoxes_1()
        {
            RegisterTimeTextBox_1(text1_Hr1);
            RegisterTimeTextBox_1(text1_Hr2);
            RegisterTimeTextBox_1(text1_Hr3);
            RegisterTimeTextBox_1(text1_Hr4);
            RegisterTimeTextBox_1(text1_Hr5);
            RegisterTimeTextBox_1(text1_Hr6);
            RegisterTimeTextBox_1(text1_Hr7);
            RegisterTimeTextBox_1(text1_Hr8);

            RegisterTimeTextBox_1(text1_Min1);
            RegisterTimeTextBox_1(text1_Min2);
            RegisterTimeTextBox_1(text1_Min3);
            RegisterTimeTextBox_1(text1_Min4);
            RegisterTimeTextBox_1(text1_Min5);
            RegisterTimeTextBox_1(text1_Min6);
            RegisterTimeTextBox_1(text1_Min7);
            RegisterTimeTextBox_1(text1_Min8);
        }

        private void RegisterTextBox_1(TextBox tb)
        {
            if (tb == null) return;
            tb.TextChanged += (s, e) =>
            {
                ProgramControlSet1Data.Values[tb.Name] = tb.Text;
            };
            tb.MaxLength = 3;
        }

        private void UpdateDataSet_1(string key, string value)
        {
            switch (key)
            {
                case "text1_Temp1": settings_1.Text1_Temp1 = value; break;
                case "text1_Temp2": settings_1.Text1_Temp2 = value; break;
                case "text1_Temp3": settings_1.Text1_Temp3 = value; break;
                case "text1_Temp4": settings_1.Text1_Temp4 = value; break;
                case "text1_Temp5": settings_1.Text1_Temp5 = value; break;
                case "text1_Temp6": settings_1.Text1_Temp6 = value; break;
                case "text1_Temp7": settings_1.Text1_Temp7 = value; break;
                case "text1_Temp8": settings_1.Text1_Temp8 = value; break;

                case "text1_RPM1": settings_1.Text1_RPM1 = value; break;
                case "text1_RPM2": settings_1.Text1_RPM2 = value; break;
                case "text1_RPM3": settings_1.Text1_RPM3 = value; break;
                case "text1_RPM4": settings_1.Text1_RPM4 = value; break;
                case "text1_RPM5": settings_1.Text1_RPM5 = value; break;
                case "text1_RPM6": settings_1.Text1_RPM6 = value; break;
                case "text1_RPM7": settings_1.Text1_RPM7 = value; break;
                case "text1_RPM8": settings_1.Text1_RPM8 = value; break;

                case "text1_Dosing1": settings_1.Text1_Dosing1 = value; break;
                case "text1_Dosing2": settings_1.Text1_Dosing2 = value; break;
                case "text1_Dosing3": settings_1.Text1_Dosing3 = value; break;
                case "text1_Dosing4": settings_1.Text1_Dosing4 = value; break;
                case "text1_Dosing5": settings_1.Text1_Dosing5 = value; break;
                case "text1_Dosing6": settings_1.Text1_Dosing6 = value; break;
                case "text1_Dosing7": settings_1.Text1_Dosing7 = value; break;
                case "text1_Dosing8": settings_1.Text1_Dosing8 = value; break;

                case "text1_Hr1": settings_1.Text1_Hr1 = value; break;
                case "text1_Hr2": settings_1.Text1_Hr2 = value; break;
                case "text1_Hr3": settings_1.Text1_Hr3 = value; break;
                case "text1_Hr4": settings_1.Text1_Hr4 = value; break;
                case "text1_Hr5": settings_1.Text1_Hr5 = value; break;
                case "text1_Hr6": settings_1.Text1_Hr6 = value; break;
                case "text1_Hr7": settings_1.Text1_Hr7 = value; break;
                case "text1_Hr8": settings_1.Text1_Hr8 = value; break;

                case "text1_Min1": settings_1.Text1_Min1 = value; break;
                case "text1_Min2": settings_1.Text1_Min2 = value; break;
                case "text1_Min3": settings_1.Text1_Min3 = value; break;
                case "text1_Min4": settings_1.Text1_Min4 = value; break;
                case "text1_Min5": settings_1.Text1_Min5 = value; break;
                case "text1_Min6": settings_1.Text1_Min6 = value; break;
                case "text1_Min7": settings_1.Text1_Min7 = value; break;
                case "text1_Min8": settings_1.Text1_Min8 = value; break;
            }
        }

        private void LoadSavedValues_1()
        {
            // โหลดค่า settings ใหม่ที่ถูกบันทึกไว้
            text1_Temp1.Text = settings_1.Text1_Temp1;
            text1_Temp2.Text = settings_1.Text1_Temp2;
            text1_Temp3.Text = settings_1.Text1_Temp3;
            text1_Temp4.Text = settings_1.Text1_Temp4;
            text1_Temp5.Text = settings_1.Text1_Temp5;
            text1_Temp6.Text = settings_1.Text1_Temp6;
            text1_Temp7.Text = settings_1.Text1_Temp7;
            text1_Temp8.Text = settings_1.Text1_Temp8;

            text1_RPM1.Text = settings_1.Text1_RPM1;
            text1_RPM2.Text = settings_1.Text1_RPM2;
            text1_RPM3.Text = settings_1.Text1_RPM3;
            text1_RPM4.Text = settings_1.Text1_RPM4;
            text1_RPM5.Text = settings_1.Text1_RPM5;
            text1_RPM6.Text = settings_1.Text1_RPM6;
            text1_RPM7.Text = settings_1.Text1_RPM7;
            text1_RPM8.Text = settings_1.Text1_RPM8;

            text1_Dosing1.Text = settings_1.Text1_Dosing1;
            text1_Dosing2.Text = settings_1.Text1_Dosing2;
            text1_Dosing3.Text = settings_1.Text1_Dosing3;
            text1_Dosing4.Text = settings_1.Text1_Dosing4;
            text1_Dosing5.Text = settings_1.Text1_Dosing5;
            text1_Dosing6.Text = settings_1.Text1_Dosing6;
            text1_Dosing7.Text = settings_1.Text1_Dosing7;
            text1_Dosing8.Text = settings_1.Text1_Dosing8;

            text1_Hr1.Text = settings_1.Text1_Hr1;
            text1_Hr2.Text = settings_1.Text1_Hr2;
            text1_Hr3.Text = settings_1.Text1_Hr3;
            text1_Hr4.Text = settings_1.Text1_Hr4;
            text1_Hr5.Text = settings_1.Text1_Hr5;
            text1_Hr6.Text = settings_1.Text1_Hr6;
            text1_Hr7.Text = settings_1.Text1_Hr7;
            text1_Hr8.Text = settings_1.Text1_Hr8;

            text1_Min1.Text = settings_1.Text1_Min1;
            text1_Min2.Text = settings_1.Text1_Min2;
            text1_Min3.Text = settings_1.Text1_Min3;
            text1_Min4.Text = settings_1.Text1_Min4;
            text1_Min5.Text = settings_1.Text1_Min5;
            text1_Min6.Text = settings_1.Text1_Min6;
            text1_Min7.Text = settings_1.Text1_Min7;
            text1_Min8.Text = settings_1.Text1_Min8;

            // โหลดสถานะ Lamp จากไฟล์ XML (ถ้ามี)
            LoadLampState_1();
        }
        private void RegisterDataTextBoxes_1()
        {
            // ทำซ้ำจาก 1 ถึง 8
            for (int i = 1; i <= 8; i++)
            {
                // Temp
                var tbTemp = this.Controls.Find($"text1_Temp{i}", true).FirstOrDefault() as TextBox;
                if (tbTemp != null) RegisterDataTextBox_1(tbTemp);

                // RPM
                var tbRpm = this.Controls.Find($"text1_RPM{i}", true).FirstOrDefault() as TextBox;
                if (tbRpm != null) RegisterDataTextBox_1(tbRpm);

                // Dosing
                var tbDos = this.Controls.Find($"text1_Dosing{i}", true).FirstOrDefault() as TextBox;
                if (tbDos != null) RegisterDataTextBox_1(tbDos);
            }
        }

        /// <summary>
        /// ผูก TextChanged ให้ TextBox ใดๆ ที่เรียกผ่านนี้ จะอัปเดต settings.Text1_* ตามชื่อ key
        /// แล้วบันทึกไฟล์ Data_Set1.xml
        /// </summary>
        private void RegisterDataTextBox_1(TextBox tb)
        {
            tb.TextChanged += (s, e) =>
            {
                // ใช้ฟังก์ชันเดิมในการแมป key->property
                UpdateDataSet_1(tb.Name, tb.Text);
                settings_1.SaveToFile_1("Data_Set1.xml", "Combined");
              //  Debug.WriteLine($"{tb.Name} changed to {tb.Text} -> saved to Data_Set1.xml");
            };
            tb.MaxLength = 10; // หรือกำหนดตามต้องการ
        }

    }
}
public static class ProgramControlSet1Data
{
    public static Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
}
