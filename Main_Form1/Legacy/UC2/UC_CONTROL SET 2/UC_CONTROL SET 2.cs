// ==============================================
//  UC_CONTROL_SET_2.cs (Enhanced with Min/Max Validation)
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  ------------------------------------------------------------
//  • แก้ไข: 01 มิถุนายน 2025
//  • เพิ่ม Min/Max Validation พร้อม MessageBox
//  • ตรวจสอบค่าจาก DeviceSettings_2.xml
//  • Enhanced Error Handling & User Experience
// ==============================================

using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;
using Timer = System.Windows.Forms.Timer;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    public partial class UC_CONTROL_SET_2 : UserControl
    {
        #region ตัวแปรหลัก

        private const string DisplayFileName_2 = "Data_Control_Set2.xml";

        // ตัวแปรสำหรับการสื่อสาร Serial
        private Timer updateTimer_2;
        private int expectedGroup_2 = -1;
        private readonly int[] allowedGroups_2 = { 3, 4, 5 };

        // ตัวแปรการตั้งค่า
        private DeviceSettings_2 deviceSettings_2;

        // ตัวแปรพิเศษสำหรับการตรวจสอบ Parent
        private Control _previousParent_2;

        // สถานะโหมด - ใช้จาก Manual_Set2
        private bool preventRecursion_2 = false;
        private bool _thermoChanged = false;
        private bool _stirrerChanged = false;
        private bool _modeChanged = false;
        private Timer _debouncedSaveTimer;
        private readonly object _saveLock = new object();

        // 🔥 เพิ่ม: ตัวแปรเก็บค่าล่าสุดที่ถูกต้องสำหรับ Validation
        private string _lastValidThermoValue_2 = "25.0";
        private string _lastValidStirrerValue_2 = "0";

        #endregion

        #region การสร้างและการตั้งค่าเริ่มต้น

        public UC_CONTROL_SET_2()
        {
            try
            {
                InitializeComponent();
                Logger.CurrentLogLevel = LogLevel.Debug;
                Logger.Log("[UI2] Starting UC_CONTROL_SET_2 constructor", LogLevel.Info);

                // โหลดการตั้งค่าอุปกรณ์เท่านั้น
                try
                {
                    deviceSettings_2 = DeviceSettings_2.Load_2("Settings_2.xml") ?? new DeviceSettings_2();
                    Logger.Log("[UI2] DeviceSettings_2 loaded successfully", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UI2] Failed to load DeviceSettings_2: {ex.Message}, using defaults", LogLevel.Warn);
                    deviceSettings_2 = new DeviceSettings_2();
                }

                SetupEventHandlers_2();
                SetupUpdateTimer_2();
                SetupButtons_2();
                SetupSerialCommunicationSafely_2();
                ApplyManualMode_2();

                Logger.Log("[UI2] UC_CONTROL_SET_2 constructor completed successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][CRITICAL ERROR] Constructor exception: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดหน้า Control Set 2: {ex.Message}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);

                if (deviceSettings_2 == null)
                    deviceSettings_2 = new DeviceSettings_2();
            }
        }

        /// <summary>
        /// ตั้งค่า Event Handlers ทั้งหมด
        /// </summary>
        private void SetupEventHandlers_2()
        {
            try
            {
                Logger.Log("[UI2] Setting up event handlers with enhanced validation...", LogLevel.Info);

                this.Load += UC_ControlSet2_Load;
                this.VisibleChanged += UC_Control_Set2_VisibleChanged;
                this.ParentChanged += UC_Control_Set2_ParentChanged;

                // **ปรับปรุงเหตุการณ์ของ text2_Thermo_Set2 - เพิ่ม Validation**
                text2_Thermo_Set2.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        ValidateAndSaveThermo_2();
                    }
                };

                text2_Thermo_Set2.Leave += (s, e) =>
                {
                    ValidateAndSaveThermo_2(); // เรียกตรงๆ เพื่อตรวจสอบทันที
                };

                text2_Thermo_Set2.TextChanged += (s, e) =>
                {
                    _thermoChanged = true;
                    Logger.Log($"[UI2] Thermo text changed to: {text2_Thermo_Set2.Text}", LogLevel.Debug);
                    TriggerDebouncedSave();
                };

                // **ปรับปรุงเหตุการณ์ของ text2_Motor_Stirrer_Set2 - เพิ่ม Validation**
                text2_Motor_Stirrer_Set2.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        ValidateAndSaveStirrer_2();
                    }
                };

                text2_Motor_Stirrer_Set2.Leave += (s, e) =>
                {
                    ValidateAndSaveStirrer_2(); // เรียกตรงๆ เพื่อตรวจสอบทันที
                };

                text2_Motor_Stirrer_Set2.TextChanged += (s, e) =>
                {
                    _stirrerChanged = true;
                    Logger.Log($"[UI2] Stirrer text changed to: {text2_Motor_Stirrer_Set2.Text}", LogLevel.Debug);
                    TriggerDebouncedSave();
                };

                // **ปรับปรุงเหตุการณ์ของ switch_Target1_Set2 (Tr/Tj)**
                switch_Target1_Set2.ToggleChanged += Target2_ToggleChanged;

                bool currentTjMode = Manual_Data_Set2.CurrentManual_2?.IsTjMode_2 ?? false;
                switch_Target1_Set2.IsOn = currentTjMode;

                // **ปรับปรุงเหตุการณ์ของ switch_A_M2 (Auto/Manual)**
                ProgramState_2.SwitchA_M2Changed += (s, e) => switch_A_M2.IsOn = ProgramState_2.SwitchA_M2;
                switch_A_M2.ToggleChanged += (s, e) =>
                {
                    if (preventRecursion_2) return;

                    try
                    {
                        preventRecursion_2 = true;

                        ProgramState_2.SwitchA_M2 = switch_A_M2.IsOn;
                        _modeChanged = true;

                        // **ใช้ consolidated logging แทนการสร้างไฟล์ซ้ำซ้อน**
                        CreateConsolidatedVerificationLog("AutoManualModeChanged", new Dictionary<string, object>
                        {
                            ["NewMode"] = switch_A_M2.IsOn ? "Auto" : "Manual",
                            ["ThermoMode"] = Manual_Data_Set2.CurrentManual_2?.IsTjMode_2 == true ? "Tj" : "Tr",
                            ["ThermoSetpoint"] = text2_Thermo_Set2.Text,
                            ["StirrerSetpoint"] = text2_Motor_Stirrer_Set2.Text
                        });

                        ApplyManualMode_2();

                        // **ปรับปรุง: บันทึกทันทีใน Manual mode, ใช้ debounced ใน Auto mode**
                        if (!switch_A_M2.IsOn) // Manual mode
                        {
                            Logger.Log("[UI2] Switching to Manual mode - Performing immediate consolidated save", LogLevel.Info);

                            ValidateAndSaveThermo_2();
                            ValidateAndSaveStirrer_2();

                            if (Manual_Data_Set2.CurrentManual_2 != null)
                            {
                                Manual_Data_Set2.CurrentManual_2.Switch_A_M2 = switch_A_M2.IsOn;
                                Manual_Data_Set2.SaveDirectToFile_2("ManualModeSwitch");
                            }

                            CreateConsolidatedVerificationLog("ManualModeSwitchSaved", new Dictionary<string, object>
                            {
                                ["FilePath"] = Path.GetFullPath(Manual_Data_Set2.DefaultFilePath_2),
                                ["SavedAt"] = DateTime.Now
                            });
                        }
                        else
                        {
                            TriggerDebouncedSave();
                        }
                    }
                    finally
                    {
                        preventRecursion_2 = false;
                    }
                };

                // **เพิ่ม: Save trigger เมื่อคลิกพื้นที่ว่าง**
                this.Click += (s, e) =>
                {
                    if (_thermoChanged || _stirrerChanged || _modeChanged)
                    {
                        Logger.Log("[UI2] Save triggered by user click", LogLevel.Debug);
                        TriggerDebouncedSave();
                    }
                };

                Logger.Log("[UI2] Event handlers setup completed with enhanced validation", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Error setting up event handlers: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private void SetupSerialCommunicationSafely_2()
        {
            try
            {
                if (SerialPortManager.Instance == null)
                {
                    Logger.Log("[UI2][ERROR] SerialPortManager.Instance is null", LogLevel.Error);
                    return;
                }

                SerialPortManager.Instance.DataGroupRequested -= OnDataGroupRequested_2;
                SerialPortManager.Instance.DataReceivedRawEvent -= OnRawDataReceived_2;

                SerialPortManager.Instance.DataGroupRequested += OnDataGroupRequested_2;
                SerialPortManager.Instance.ConfigureSequentialRequests(allowedGroups_2, "[UI2]");
                SerialPortManager.Instance.DataReceivedRawEvent += OnRawDataReceived_2;

                Logger.Log("[UI2] Serial communication setup completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][ERROR] SetupSerialCommunicationSafely2: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupUpdateTimer_2()
        {
            try
            {
                if (updateTimer_2 != null)
                {
                    updateTimer_2.Stop();
                    updateTimer_2.Tick -= UpdateTimer_Tick_2;
                }

                updateTimer_2 = new Timer { Interval = 500 };
                updateTimer_2.Tick += UpdateTimer_Tick_2;
                updateTimer_2.Start();

                Logger.Log("[UI2] Update timer initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][ERROR] SetupUpdateTimer2: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupButtons_2()
        {
            But_CONTROL2_SET_1.Click += But_CONTROL2_SET_1_Click;
            But_Graph_Data2.Click += But_Graph_Data2_Click;
            But_Program2_Sequence.Click += But_Program2_Sequence_Click;
            but_Setting2.Click += but_Setting2_Click;
        }

        #endregion

        #region 🔥 Enhanced Min/Max Validation System

        /// <summary>
        /// ตรวจสอบค่าอุณหภูมิให้อยู่ในช่วงที่กำหนด
        /// </summary>
        private bool ValidateTemperatureRange_2(float value, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (deviceSettings_2 == null)
                {
                    errorMessage = "ไม่สามารถโหลดการตั้งค่าอุปกรณ์ได้";
                    return false;
                }

                float minTemp = deviceSettings_2.TempMin_2;
                float maxTemp = deviceSettings_2.TempMax_2;

                if (value < minTemp)
                {
                    float difference = minTemp - value;
                    errorMessage = $"🔻 อุณหภูมิต่ำเกินไป!\n\n" +
                                  $"ค่าที่ใส่: {value:F2}°C\n" +
                                  $"ค่าต่ำสุดที่อนุญาต: {minTemp:F2}°C\n" +
                                  $"ต่ำกว่าที่กำหนด: {difference:F2}°C\n\n" +
                                  $"กรุณาใส่ค่าระหว่าง {minTemp:F2} - {maxTemp:F2}°C";
                    return false;
                }

                if (value > maxTemp)
                {
                    float difference = value - maxTemp;
                    errorMessage = $"🔺 อุณหภูมิสูงเกินไป!\n\n" +
                                  $"ค่าที่ใส่: {value:F2}°C\n" +
                                  $"ค่าสูงสุดที่อนุญาต: {maxTemp:F2}°C\n" +
                                  $"เกินกว่าที่กำหนด: {difference:F2}°C\n\n" +
                                  $"กรุณาใส่ค่าระหว่าง {minTemp:F2} - {maxTemp:F2}°C";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"เกิดข้อผิดพลาดในการตรวจสอบค่าอุณหภูมิ: {ex.Message}";
                Logger.Log($"[UI2] Error in ValidateTemperatureRange_2: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// ตรวจสอบค่า RPM/Stirrer ให้อยู่ในช่วงที่กำหนด
        /// </summary>
        private bool ValidateStirrerRange_2(int value, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (deviceSettings_2 == null)
                {
                    errorMessage = "ไม่สามารถโหลดการตั้งค่าอุปกรณ์ได้";
                    return false;
                }

                int minRpm = deviceSettings_2.StirrerMin_2;
                int maxRpm = deviceSettings_2.StirrerMax_2;

                if (value < minRpm)
                {
                    int difference = minRpm - value;
                    errorMessage = $"🔻 ความเร็วกวนต่ำเกินไป!\n\n" +
                                  $"ค่าที่ใส่: {value} RPM\n" +
                                  $"ค่าต่ำสุดที่อนุญาต: {minRpm} RPM\n" +
                                  $"ต่ำกว่าที่กำหนด: {difference} RPM\n\n" +
                                  $"กรุณาใส่ค่าระหว่าง {minRpm} - {maxRpm} RPM";
                    return false;
                }

                if (value > maxRpm)
                {
                    int difference = value - maxRpm;
                    errorMessage = $"🔺 ความเร็วกวนสูงเกินไป!\n\n" +
                                  $"ค่าที่ใส่: {value} RPM\n" +
                                  $"ค่าสูงสุดที่อนุญาต: {maxRpm} RPM\n" +
                                  $"เกินกว่าที่กำหนด: {difference} RPM\n\n" +
                                  $"กรุณาใส่ค่าระหว่าง {minRpm} - {maxRpm} RPM";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"เกิดข้อผิดพลาดในการตรวจสอบค่าความเร็วกวน: {ex.Message}";
                Logger.Log($"[UI2] Error in ValidateStirrerRange_2: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// แสดง MessageBox แจ้งข้อผิดพลาดและจัดการค่าที่ผิด
        /// </summary>
        private void ShowRangeErrorMessage_2(string errorMessage, TextBox targetTextBox, string lastValidValue)
        {
            try
            {
                // แสดง MessageBox พร้อมรายละเอียด
                MessageBox.Show(errorMessage,
                               "ค่าที่ใส่เกินขอบเขตที่กำหนด - Control Set 2",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);

                // คืนค่าล่าสุดที่ถูกต้อง
                if (targetTextBox != null && !string.IsNullOrEmpty(lastValidValue))
                {
                    targetTextBox.Text = lastValidValue;
                    Logger.Log($"[UI2] Restored last valid value: {lastValidValue} to {targetTextBox.Name}", LogLevel.Info);
                }

                // Focus กลับไปที่ TextBox เพื่อให้ผู้ใช้แก้ไข
                if (targetTextBox != null && targetTextBox.Enabled)
                {
                    targetTextBox.Focus();
                    targetTextBox.SelectAll();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Error in ShowRangeErrorMessage_2: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region เหตุการณ์ Load และ Visible

        // ใน UC_ControlSet2_Load
        private void UC_ControlSet2_Load(object sender, EventArgs e)
        {
            try
            {
                Logger.Log("[UI2] UC_ControlSet2_Load started", LogLevel.Info);

                // 🔥 ตรวจสอบ file integrity ก่อน
                string filePath = Manual_Data_Set2.DefaultFilePath_2;
                bool fileValid = Manual_Data_Set2.VerifyFileIntegrity(filePath);

                Logger.Log($"[UI2] File integrity check: {(fileValid ? "PASS" : "FAIL")} - {filePath}", LogLevel.Info);

                if (!fileValid)
                {
                    Logger.Log("[UI2] WARNING: Main file corrupted, attempting recovery...", LogLevel.Warn);
                }

                // โหลดข้อมูล
                LoadManualControlData_2();

                // 🔥 ดึง state จาก data
                bool currentTjMode = Manual_Data_Set2.CurrentManual_2?.IsTjMode_2 ?? false;

                // 🔥 Debug log เพื่อตรวจสอบ
                Logger.Log($"[UI2] DEBUG: Loaded state from file:", LogLevel.Info);
                Logger.Log($"  - IsTjMode_2: {currentTjMode}", LogLevel.Info);
                Logger.Log($"  - CurrentThermoSetpoint_2: {Manual_Data_Set2.CurrentManual_2?.CurrentThermoSetpoint_2}", LogLevel.Info);
                Logger.Log($"  - LastTrSetpoint_2: {Manual_Data_Set2.CurrentManual_2?.LastTrSetpoint_2}", LogLevel.Info);
                Logger.Log($"  - LastTjSetpoint_2: {Manual_Data_Set2.CurrentManual_2?.LastTjSetpoint_2}", LogLevel.Info);

                preventRecursion_2 = true;
                switch_Target1_Set2.IsOn = currentTjMode;
                switch_A_M2.IsOn = ProgramState_2.SwitchA_M2;
                preventRecursion_2 = false;

                ProgramState_2.IsTjMode_2 = currentTjMode;
                ApplyManualMode_2();

                // 🔥 เก็บค่าล่าสุดที่ถูกต้องสำหรับ Validation
                _lastValidThermoValue_2 = text2_Thermo_Set2.Text;
                _lastValidStirrerValue_2 = text2_Motor_Stirrer_Set2.Text;

                Logger.Log($"[UI2] UC_ControlSet2_Load completed - Switch state: {(currentTjMode ? "Tj (ON)" : "Tr (OFF)")}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Error in UC_Control_Set2_Load: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadManualControlData_2()
        {
            try
            {
                var manualData = Manual_Data_Set2.CurrentManual_2;
                if (manualData == null)
                {
                    Logger.Log("[UI2] Manual_Data_Set2.CurrentManual_2 is null", LogLevel.Warn);
                    return;
                }

                // ✅ แก้ไข: ใช้ค่าที่ถูกต้องตามโหมดปัจจุบัน
                string displayValue;
                if (manualData.IsTjMode_2)
                {
                    // โหมด Tj: ใช้ LastTjSetpoint_2 ถ้ามีค่า ไม่เช่นนั้นใช้ CurrentThermoSetpoint_2
                    displayValue = Math.Abs(manualData.LastTjSetpoint_2) > 0.01f
                        ? manualData.LastTjSetpoint_2.ToString("F2")
                        : manualData.CurrentThermoSetpoint_2;
                }
                else
                {
                    // โหมด Tr: ใช้ LastTrSetpoint_2 ถ้ามีค่า ไม่เช่นนั้นใช้ CurrentThermoSetpoint_2
                    displayValue = Math.Abs(manualData.LastTrSetpoint_2) > 0.01f
                        ? manualData.LastTrSetpoint_2.ToString("F2")
                        : manualData.CurrentThermoSetpoint_2;
                }

                text2_Thermo_Set2.Text = displayValue;
                text2_Motor_Stirrer_Set2.Text = manualData.Stirrer_Set2;

                Logger.Log($"[UI2] Manual control data loaded - Temp: {displayValue}, " +
                          $"Stirrer: {manualData.Stirrer_Set2}, Mode: {(manualData.IsTjMode_2 ? "Tj" : "Tr")}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Error in LoadManualControlData_2: {ex.Message}", LogLevel.Error);
            }
        }

        private void UC_Control_Set2_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible)
            {
                SaveAllValuesBeforeNavigation_2();
                return;
            }

            try
            {
                Logger.Log("[UI2] VisibleChanged เริ่มทำงาน - Enhanced data sync", LogLevel.Info);

                if (preventRecursion_2) return;
                preventRecursion_2 = true;

                try
                {
                    switch_A_M2.IsOn = ProgramState_2.SwitchA_M2;
                    ApplyManualMode_2();

                    LoadManualControlData_2();

                    bool currentTjMode = Manual_Data_Set2.CurrentManual_2?.IsTjMode_2 ?? false;
                    switch_Target1_Set2.IsOn = currentTjMode;
                    ProgramState_2.IsTjMode_2 = currentTjMode;

                    // 🔥 อัพเดตค่าล่าสุดที่ถูกต้อง
                    _lastValidThermoValue_2 = text2_Thermo_Set2.Text;
                    _lastValidStirrerValue_2 = text2_Motor_Stirrer_Set2.Text;

                    Logger.Log($"[UI2] VisibleChanged ทำงานเสร็จสิ้น - โหมด: {(currentTjMode ? "Tj" : "Tr")}", LogLevel.Info);
                }
                finally
                {
                    preventRecursion_2 = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][Error] UC_ControlSet2_VisibleChanged เกิดข้อผิดพลาด: {ex.Message}", LogLevel.Error);
                preventRecursion_2 = false;
            }
        }

        private void UC_Control_Set2_ParentChanged(object sender, EventArgs e)
        {
            try
            {
                if (_previousParent_2 != null)
                    _previousParent_2.VisibleChanged -= Parent_VisibleChanged_2;
                _previousParent_2 = Parent as Control;
                if (_previousParent_2 != null)
                    _previousParent_2.VisibleChanged += Parent_VisibleChanged_2;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][Error] UC_ControlSet2_ParentChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void Parent_VisibleChanged_2(object sender, EventArgs e)
        {
            try
            {
                if (sender is Control parent && parent.Visible)
                {
                    switch_A_M2.IsOn = ProgramState_2.SwitchA_M2;
                    ApplyManualMode_2();
                    LoadManualControlData_2();

                    // 🔥 อัพเดตค่าล่าสุดที่ถูกต้อง
                    _lastValidThermoValue_2 = text2_Thermo_Set2.Text;
                    _lastValidStirrerValue_2 = text2_Motor_Stirrer_Set2.Text;

                    Logger.Log("[UI2] Parent visible, values restored", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][Error] Parent_VisibleChanged: {ex}", LogLevel.Error);
            }
        }

        #endregion

        #region การจัดการโหมด Auto/Manual และ Tr/Tj

        private void ApplyManualMode_2()
        {
            bool manual = !switch_A_M2.IsOn;
            text2_Thermo_Set2.Enabled = manual;
            text2_Motor_Stirrer_Set2.Enabled = manual;

            Logger.Log($"[UI2] ApplyManualMode2: manual = {manual}", LogLevel.Debug);
        }

        private void Target2_ToggleChanged(object sender, EventArgs e)
        {
            if (preventRecursion_2) return;

            try
            {
                preventRecursion_2 = true;
                bool newMode = switch_Target1_Set2.IsOn; // true = Tj, false = Tr

                Logger.Log($"[UI2] Mode changing to {(newMode ? "Tj" : "Tr")}", LogLevel.Info);

                if (Manual_Data_Set2.CurrentManual_2 != null)
                {
                    // 🔥 ใช้ CurrentThermoSetpoint_2 เป็น fallback แทน default value
                    float currentValue = ParseFloat_2(Manual_Data_Set2.CurrentManual_2.CurrentThermoSetpoint_2, 25.0f);

                    // 🔥 ลองอ่านจาก text2_Thermo_Set2 ก่อน (หากมีค่า)
                    if (!string.IsNullOrWhiteSpace(text2_Thermo_Set2.Text))
                    {
                        if (float.TryParse(text2_Thermo_Set2.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
                        {
                            currentValue = parsedValue;
                            Logger.Log($"[UI2] Using value from text input: {currentValue:F2}", LogLevel.Info);
                        }
                        else
                        {
                            Logger.Log($"[UI2] Invalid text input '{text2_Thermo_Set2.Text}', using CurrentThermoSetpoint_2: {currentValue:F2}", LogLevel.Warn);
                        }
                    }
                    else
                    {
                        Logger.Log($"[UI2] Empty text input, using CurrentThermoSetpoint_2: {currentValue:F2}", LogLevel.Info);
                    }

                    // 🔥 แน่ใจว่า textbox มีค่าที่ถูกต้อง
                    text2_Thermo_Set2.Text = currentValue.ToString("F2", CultureInfo.InvariantCulture);

                    Logger.Log($"[UI2] Final temperature value for mode switch: {currentValue:F2}", LogLevel.Info);

                    // 🔥 ใช้ ToggleMode_2 ที่รับค่า currentValue
                    Manual_Data_Set2.CurrentManual_2.ToggleMode_2(newMode, currentValue);

                    // 🔥 อัปเดต ProgramState ด้วยค่าเดียวกัน
                    ProgramState_2.SetModeWithValue_2(newMode, currentValue);

                    // 🔥 Force save ทันที
                    try
                    {
                        Manual_Data_Set2.CurrentManual_2.SimpleSave_2(Manual_Data_Set2.DefaultFilePath_2, "ModeToggle_Immediate");

                        // 🔥 สร้าง verification backup
                        string verifyFile = $"ModeState_Verify_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
                        Manual_Data_Set2.CurrentManual_2.SimpleSave_2(verifyFile, "ModeToggle_Verification");

                        Logger.Log($"[UI2] Mode saved immediately: {(newMode ? "Tj" : "Tr")}", LogLevel.Info);
                        Logger.Log($"[UI2] Values after switch - Current: {Manual_Data_Set2.CurrentManual_2.CurrentThermoSetpoint_2}, LastTr: {Manual_Data_Set2.CurrentManual_2.LastTrSetpoint_2:F2}, LastTj: {Manual_Data_Set2.CurrentManual_2.LastTjSetpoint_2:F2}", LogLevel.Info);

                        // 🔥 Debug current state
                        Manual_Data_Set2.CurrentManual_2.DebugCurrentState_2();

                        // 🔥 อัพเดตค่าล่าสุดที่ถูกต้อง
                        _lastValidThermoValue_2 = text2_Thermo_Set2.Text;
                    }
                    catch (Exception saveEx)
                    {
                        Logger.Log($"[UI2] CRITICAL: Failed to save mode change: {saveEx.Message}", LogLevel.Error);

                        // 🔥 Emergency text save
                        string emergencyData = $"EMERGENCY_MODE_SAVE_{DateTime.Now:yyyyMMdd_HHmmss}\n" +
                                             $"IsTjMode_2={newMode}\n" +
                                             $"InputValue={currentValue:F2}\n" +
                                             $"text2_Thermo_Set2.Text={text2_Thermo_Set2.Text}\n" +
                                             $"CurrentThermoSetpoint_2={Manual_Data_Set2.CurrentManual_2.CurrentThermoSetpoint_2}\n" +
                                             $"LastTrSetpoint_2={Manual_Data_Set2.CurrentManual_2.LastTrSetpoint_2:F2}\n" +
                                             $"LastTjSetpoint_2={Manual_Data_Set2.CurrentManual_2.LastTjSetpoint_2:F2}";

                        File.WriteAllText($"EMERGENCY_ModeState_{DateTime.Now:yyyyMMdd_HHmmss}.txt", emergencyData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Error in Target2_ToggleChanged: {ex.Message}", LogLevel.Error);

                // 🔥 ย้อนกลับ switch state
                try
                {
                    if (Manual_Data_Set2.CurrentManual_2 != null)
                    {
                        switch_Target1_Set2.IsOn = Manual_Data_Set2.CurrentManual_2.IsTjMode_2;
                    }
                }
                catch { }
            }
            finally
            {
                preventRecursion_2 = false;
            }
        }

        #endregion

        #region การบันทึกและโหลดค่า - Enhanced with Min/Max Validation

        /// <summary>
        /// 🔥 ปรับปรุง: ตรวจสอบและบันทึกค่าอุณหภูมิพร้อม Min/Max Validation
        /// </summary>
        private void ValidateAndSaveThermo_2()
        {
            try
            {
                Logger.Log("[UI2] ValidateAndSaveThermo_2 started with Min/Max validation", LogLevel.Debug);

                // 1. ตรวจสอบการแปลงค่า
                if (!float.TryParse(text2_Thermo_Set2.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    string errorMsg = $"❌ รูปแบบอุณหภูมิไม่ถูกต้อง!\n\n" +
                                     $"ค่าที่ใส่: '{text2_Thermo_Set2.Text}'\n\n" +
                                     $"กรุณาใส่ตัวเลขเท่านั้น (เช่น 25.5)";

                    ShowRangeErrorMessage_2(errorMsg, text2_Thermo_Set2, _lastValidThermoValue_2);
                    return;
                }

                // 2. ตรวจสอบ Min/Max Range
                if (!ValidateTemperatureRange_2(value, out string rangeError))
                {
                    ShowRangeErrorMessage_2(rangeError, text2_Thermo_Set2, _lastValidThermoValue_2);
                    return;
                }

                // 3. ใช้ Clamp เพื่อความปลอดภัยเพิ่มเติม
                value = Clamp_2(value, deviceSettings_2.TempMin_2, deviceSettings_2.TempMax_2);

                // 4. อัปเดต UI
                string formattedValue = value.ToString("F2", CultureInfo.InvariantCulture);
                text2_Thermo_Set2.Text = formattedValue;

                // 5. บันทึกค่าที่ถูกต้องแล้ว
                if (Manual_Data_Set2.CurrentManual_2 != null)
                {
                    bool isTjMode = Manual_Data_Set2.CurrentManual_2.IsTjMode_2;

                    // อัปเดต CurrentThermoSetpoint_2 ให้เป็นค่าปัจจุบัน
                    Manual_Data_Set2.CurrentManual_2.CurrentThermoSetpoint_2 = formattedValue;
                    Manual_Data_Set2.CurrentManual_2.text2_Thermo_Set2 = formattedValue;

                    // อัปเดต setpoint ตามโหมด
                    Manual_Data_Set2.CurrentManual_2.UpdateSetpoint_2(value, isTjMode);

                    // ส่งคำสั่งไปอุปกรณ์ ถ้าอยู่ Manual mode
                    if (!switch_A_M2.IsOn)
                    {
                        int t = (int)Math.Round(value);
                        // 🔥 แก้ไข: ใช้ BuildSet1_TempStep1 สำหรับทั้ง Set 1 และ Set 2
                        // หรือตรวจสอบใน CommandHelper ว่ามี method ที่เหมาะสมสำหรับ Set 2
                        SerialPortManager.Instance.Send(
                            CommandHelper.BuildSet1_TempStep1((byte)(t >> 8), (byte)(t & 0xFF)));
                        Logger.Log($"[UI2] Sent temperature command: {t} to device (using Set1 command)", LogLevel.Info);
                    }

                    // อัปเดต ProgramState
                    ProgramState_2.CurrentThermoSetpoint_2 = formattedValue;

                    // 🔥 เก็บค่าที่ถูกต้องสำหรับครั้งต่อไป
                    _lastValidThermoValue_2 = formattedValue;

                    Logger.Log($"[UI2] Thermostat saved successfully: {value:F2}°C, Mode: {(isTjMode ? "Tj" : "Tr")}", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Critical error in ValidateAndSaveThermo_2: {ex.Message}", LogLevel.Error);

                string criticalError = $"⚠️ เกิดข้อผิดพลาดร้ายแรง!\n\n{ex.Message}\n\nระบบจะคืนค่าเดิม";
                ShowRangeErrorMessage_2(criticalError, text2_Thermo_Set2, _lastValidThermoValue_2);
            }
        }

        /// <summary>
        /// 🔥 ปรับปรุง: ตรวจสอบและบันทึกค่าความเร็วกวนพร้อม Min/Max Validation
        /// </summary>
        private void ValidateAndSaveStirrer_2()
        {
            try
            {
                Logger.Log("[UI2] ValidateAndSaveStirrer_2 started with Min/Max validation", LogLevel.Debug);

                // 1. ตรวจสอบการแปลงค่า
                if (!int.TryParse(text2_Motor_Stirrer_Set2.Text, out int rpm))
                {
                    string errorMsg = $"❌ รูปแบบความเร็วกวนไม่ถูกต้อง!\n\n" +
                                     $"ค่าที่ใส่: '{text2_Motor_Stirrer_Set2.Text}'\n\n" +
                                     $"กรุณาใส่ตัวเลขจำนวนเต็มเท่านั้น (เช่น 500)";

                    ShowRangeErrorMessage_2(errorMsg, text2_Motor_Stirrer_Set2, _lastValidStirrerValue_2);
                    return;
                }

                // 2. ตรวจสอบ Min/Max Range
                if (!ValidateStirrerRange_2(rpm, out string rangeError))
                {
                    ShowRangeErrorMessage_2(rangeError, text2_Motor_Stirrer_Set2, _lastValidStirrerValue_2);
                    return;
                }

                // 3. ใช้ Clamp เพื่อความปลอดภัยเพิ่มเติม
                rpm = (int)Clamp_2(rpm, deviceSettings_2.StirrerMin_2, deviceSettings_2.StirrerMax_2);

                // 4. อัปเดต UI
                text2_Motor_Stirrer_Set2.Text = rpm.ToString();

                // 5. บันทึกค่าที่ถูกต้องแล้ว
                if (Manual_Data_Set2.CurrentManual_2 != null)
                {
                    Manual_Data_Set2.CurrentManual_2.UpdateStirrer_2(rpm);
                    Manual_Data_Set2.CurrentManual_2.text2_Motor_Stirrer_Set2 = rpm.ToString();

                    // อัปเดต ProgramState
                    ProgramState_2.StirrerSetpoint_2 = rpm;

                    // 🔥 เก็บค่าที่ถูกต้องสำหรับครั้งต่อไป
                    _lastValidStirrerValue_2 = rpm.ToString();

                    Logger.Log($"[UI2] Stirrer saved successfully: {rpm} RPM", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Critical error in ValidateAndSaveStirrer_2: {ex.Message}", LogLevel.Error);

                string criticalError = $"⚠️ เกิดข้อผิดพลาดร้ายแรง!\n\n{ex.Message}\n\nระบบจะคืนค่าเดิม";
                ShowRangeErrorMessage_2(criticalError, text2_Motor_Stirrer_Set2, _lastValidStirrerValue_2);
            }
        }

        /// <summary>
        /// **ปรับปรุง: Enhanced Debounced Save เพื่อลด I/O operations**
        /// </summary>
        private void TriggerDebouncedSave()
        {
            try
            {
                if (_debouncedSaveTimer == null)
                {
                    _debouncedSaveTimer = new Timer();
                    _debouncedSaveTimer.Interval = 1500; // **เพิ่มเป็น 1.5 วินาที เพื่อลด I/O**
                    _debouncedSaveTimer.Tick += DebouncedSaveTimer_Tick;
                }

                _debouncedSaveTimer.Stop();
                _debouncedSaveTimer.Start();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Error in TriggerDebouncedSave: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// **ปรับปรุง: Enhanced Debounced Save Timer with Comprehensive Error Handling**
        /// </summary>
        private void DebouncedSaveTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _debouncedSaveTimer.Stop();

                if (_thermoChanged || _stirrerChanged || _modeChanged)
                {
                    Logger.Log("[UI2] Performing debounced save operation with enhanced error handling", LogLevel.Info);

                    try
                    {
                        if (Manual_Data_Set2.CurrentManual_2 != null)
                        {
                            Manual_Data_Set2.CurrentManual_2.Switch_A_M2 = switch_A_M2.IsOn;
                            Manual_Data_Set2.SaveDirectToFile_2("DebouncedSave");

                            _thermoChanged = _stirrerChanged = _modeChanged = false;

                            Logger.Log("[UI2] Debounced save completed successfully", LogLevel.Info);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[UI2] Error in debounced save operation: {ex.Message}", LogLevel.Error);

                        // **Enhanced error recovery**
                        try
                        {
                            Manual_Data_Set2.ForceSave_2();
                            Logger.Log("[UI2] Force save recovery completed", LogLevel.Info);
                            _thermoChanged = _stirrerChanged = _modeChanged = false;
                        }
                        catch (Exception forceEx)
                        {
                            Logger.Log($"[UI2] Force save recovery also failed: {forceEx.Message}", LogLevel.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Error in DebouncedSaveTimer_Tick: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// **ปรับปรุง: Enhanced Navigation Save with Comprehensive Error Recovery**
        /// </summary>
        private void SaveAllValuesBeforeNavigation_2()
        {
            lock (_saveLock)
            {
                try
                {
                    Logger.Log("[UI2] SaveAllValuesBeforeNavigation_2 started", LogLevel.Info);

                    if (_debouncedSaveTimer != null)
                    {
                        _debouncedSaveTimer.Stop();
                    }

                    ValidateAndSaveThermo_2();
                    ValidateAndSaveStirrer_2();

                    if (Manual_Data_Set2.CurrentManual_2 != null)
                    {
                        Manual_Data_Set2.CurrentManual_2.Switch_A_M2 = switch_A_M2.IsOn;
                        Manual_Data_Set2.SaveDirectToFile_2("NavigationSave");
                    }

                    Logger.Log("[UI2] SaveAllValuesBeforeNavigation_2 completed successfully", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UI2] Error in SaveAllValuesBeforeNavigation_2: {ex.Message}", LogLevel.Error);
                }
            }
        }

        private void CreateConsolidatedVerificationLog(string operation, Dictionary<string, object> data)
        {
            try
            {
                // ใช้ไฟล์ log เดียวต่อวัน แทนการสร้างไฟล์ใหม่ทุกครั้ง
                string logFile = $"UI2_Operations_{DateTime.Now:yyyyMMdd}.log";

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {operation}");

                foreach (var kvp in data)
                {
                    logEntry.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
                logEntry.AppendLine();

                // Append แทน Create ใหม่
                File.AppendAllText(logFile, logEntry.ToString());

                // Log เฉพาะเมื่อ debug level
                if (Logger.CurrentLogLevel <= LogLevel.Debug)
                {
                    Logger.Log($"[UI2] Operation logged: {operation}", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Could not create verification log: {ex.Message}", LogLevel.Warn);
            }
        }

        #endregion

        #region Serial Communication

        /// <summary>
        /// อีเวนต์เมื่อได้รับข้อมูล Raw จาก Serial
        /// </summary>
        private void OnRawDataReceived_2(string raw)
        {
            try
            {
                if (string.IsNullOrEmpty(raw)) return;

                byte[] b = raw.Select(c => (byte)c).ToArray();
                // ตรวจความยาวขั้นต่ำ
                if (b.Length < 7) return;

                int groupId = b[0];
                if (!allowedGroups_2.Contains(groupId)) return;  // กรองตามกลุ่มที่สนใจ
                if (groupId != expectedGroup_2)
                {
                    Logger.Log($"[UI2] Received DataGroup{groupId} but was expecting {expectedGroup_2}, ignoring", LogLevel.Warn);
                    return;
                }

                // Data Group 3: TR2, TJ2
                if (groupId == 3)
                {
                    ushort rawTr = (ushort)((b[2] << 8) | b[3]);
                    ushort rawTj = (ushort)((b[4] << 8) | b[5]);

                    float tr = HalfToFloat_2(rawTr);
                    float tj = HalfToFloat_2(rawTj);

                    var vals = SerialPortManager.Instance.CurrentValues;
                    if (vals.Length >= 6)
                    {
                        vals[4] = tr;
                        vals[5] = tj;
                    }

                    // ตรวจสอบว่า Control ถูก Dispose แล้วหรือไม่
                    if (!IsDisposed && IsHandleCreated)
                    {
                        this.BeginInvoke((Action)(() =>
                        {
                            label_TR2.Text = tr.ToString("F2");
                            label_TJ2.Text = tj.ToString("F2");
                            label_TR_TJ2.Text = (tr - tj).ToString("F2");
                        }));
                    }
                }
                // Data Group 4: RPM2
                else if (groupId == 4)
                {
                    ushort rawRpm = (ushort)((b[2] << 8) | b[3]);
                    double rpm = rawRpm;

                    var vals2 = SerialPortManager.Instance.CurrentValues;
                    if (vals2.Length >= 8)
                        vals2[7] = rpm;

                    // ตรวจสอบว่า Control ถูก Dispose แล้วหรือไม่
                    if (!IsDisposed && IsHandleCreated)
                    {
                        this.BeginInvoke((Action)(() =>
                        {
                            label_RPM2.Text = rpm.ToString("F0");
                        }));
                    }
                }
                // Data Group 5: Ext2
                else if (groupId == 5)
                {
                    ushort rawExt = (ushort)((b[2] << 8) | b[3]);
                    int ext2 = rawExt & 0x0FFF;  // ตัดไว้ในช่วง 0-4095

                    var vals5 = SerialPortManager.Instance.CurrentValues;
                    if (vals5.Length >= 10)
                        vals5[9] = ext2;

                    // ตรวจสอบว่า Control ถูก Dispose แล้วหรือไม่
                    if (!IsDisposed && IsHandleCreated)
                    {
                        this.BeginInvoke((Action)(() =>
                        {
                            label_Ext2.Text = ext2.ToString();
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][Error] OnRawDataReceived_2: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// อีเวนต์เมื่อ SerialPortManager ขอข้อมูลกลุ่มใด
        /// </summary>
        private void OnDataGroupRequested_2(byte group)
        {
            expectedGroup_2 = group;
            Logger.Log($"[UI2] Now expecting DataGroup{group}", LogLevel.Debug);
        }

        /// <summary>
        /// อีเวนต์เมื่อ Timer Tick
        /// </summary>
        private void UpdateTimer_Tick_2(object sender, EventArgs e)
        {
            try
            {
                // ตรวจสอบ SerialPortManager.Instance ก่อนเสมอ
                if (SerialPortManager.Instance == null)
                {
                    return;
                }

                // ใช้ UI2Values เพื่อแมป index ถูกต้อง
                var ui2 = SerialPortManager.Instance.UI2Values;
                if (ui2 != null && ui2.Length >= 4)
                {
                    double tr = ui2[0]; // TR2
                    double tj = ui2[1]; // TJ2
                    double rpm = ui2[2]; // RPM2
                    double ext2 = ui2[3]; // ExtTemp2
                    double diff = tr - tj;

                    // ตรวจสอบว่า Control ถูก Dispose แล้วหรือไม่
                    if (!IsDisposed && IsHandleCreated)
                    {
                        // อัพเดต UI
                        this.BeginInvoke(new Action(() => {
                            label_TR2.Text = $"{tr:F2}";
                            label_TR_TJ2.Text = $"{diff:F2}";
                            label_TJ2.Text = $"{tj:F2}";
                            label_RPM2.Text = $"{rpm:F0}";
                            label_Ext2.Text = $"{ext2:F0}";
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][ERROR] UpdateTimer_Tick2: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region ปุ่มและการเปลี่ยนหน้า

        private Main_Form1 FindMainForm_2()
        {
            try
            {
                if (this.ParentForm is Main_Form1 mainForm1)
                    return mainForm1;

                Control parent = this.Parent;
                while (parent != null)
                {
                    if (parent is Main_Form1 mainForm2)
                        return mainForm2;
                    parent = parent.Parent;
                }

                foreach (Form form in Application.OpenForms)
                {
                    if (form is Main_Form1 mainForm3)
                        return mainForm3;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UI2][ERROR] FindMainForm2: {ex.Message}");
                return null;
            }
        }

        private void But_CONTROL2_SET_1_Click(object sender, EventArgs e)
        {
            try
            {
                SaveAllValuesBeforeNavigation_2();

                NavigationHelper.NavigateTo<UC_CONTROL_SET_1>(() => {
                    CleanupResources_2();
                    Logger.Log($"[UI2] Navigation completed to UC_CONTROL_SET_1", LogLevel.Info);
                });

                Logger.Log($"[UI2] Navigation initiated to UC_CONTROL_SET_1", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][Error] But_CONTROL2_SET_1_Click: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"เกิดข้อผิดพลาดในการเปลี่ยนหน้า: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void But_Graph_Data2_Click(object sender, EventArgs e)
        {
            try
            {
                SaveAllValuesBeforeNavigation_2();
                NavigationHelper.NavigateTo<UC_Graph_Data_Set_2>(() => {
                    CleanupResources_2();
                    Logger.Log($"[UI2] Navigation completed to UC_Graph_Data_Set_2", LogLevel.Info);
                });
                Logger.Log($"[UI2] Navigation initiated to UC_Graph_Data_Set_2", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][Error] But_Graph_Data2_Click: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"เกิดข้อผิดพลาดในการเปลี่ยนหน้า: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void But_Program2_Sequence_Click(object sender, EventArgs e)
        {
            try
            {
                SaveAllValuesBeforeNavigation_2();
                NavigationHelper.NavigateTo<UC_PROGRAM_CONTROL_SET_2>(() => {
                    CleanupResources_2();
                    Logger.Log($"[UI2] Navigation completed to UC_PROGRAM_CONTROL_SET_2", LogLevel.Info);
                });
                Logger.Log($"[UI2] Navigation initiated to UC_PROGRAM_CONTROL_SET_2", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][Error] But_Program2_Sequence_Click: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"เกิดข้อผิดพลาดในการเปลี่ยนหน้า: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void but_Setting2_Click(object sender, EventArgs e)
        {
            try
            {
                SaveAllValuesBeforeNavigation_2();
                NavigationHelper.NavigateTo<UC_Setting>(() => {
                    CleanupResources_2();
                    Logger.Log($"[UI2] Navigation completed to UC_Setting", LogLevel.Info);
                });
                Logger.Log($"[UI2] Navigation initiated to UC_Setting", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2][Error] but_Setting2_Click: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"เกิดข้อผิดพลาดในการเปลี่ยนหน้า: {ex.Message}", "ข้อผิดพลาด",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// **ปรับปรุง: Enhanced Resource Cleanup with Comprehensive Error Handling**
        /// </summary>
        private void CleanupResources_2()
        {
            Logger.Log("[UI2] CleanupResources2 started with enhanced error handling", LogLevel.Info);

            try
            {
                // **1. Enhanced save with multiple fallbacks**
                try
                {
                    SaveAllValuesBeforeNavigation_2();
                    Logger.Log("[UI2] Settings saved in CleanupResources2", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UI2] Error saving settings in CleanupResources2: {ex.Message}", LogLevel.Error);

                    // **Emergency fallback save**
                    try
                    {
                        Manual_Data_Set2.ForceSave_2();
                        Logger.Log("[UI2] Emergency save completed in CleanupResources2", LogLevel.Info);
                    }
                    catch (Exception emergencyEx)
                    {
                        Logger.Log($"[UI2] Emergency save also failed: {emergencyEx.Message}", LogLevel.Error);
                    }
                }

                // **2. Enhanced timer cleanup**
                if (updateTimer_2 != null)
                {
                    try
                    {
                        updateTimer_2.Stop();
                        updateTimer_2.Tick -= UpdateTimer_Tick_2;
                        updateTimer_2.Dispose();
                        updateTimer_2 = null;
                        Logger.Log("[UI2] updateTimer stopped and disposed", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[UI2] Error disposing updateTimer: {ex.Message}", LogLevel.Error);
                    }
                }

                if (_debouncedSaveTimer != null)
                {
                    try
                    {
                        _debouncedSaveTimer.Stop();
                        _debouncedSaveTimer.Dispose();
                        _debouncedSaveTimer = null;
                        Logger.Log("[UI2] debouncedSaveTimer stopped and disposed", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[UI2] Error disposing debouncedSaveTimer: {ex.Message}", LogLevel.Error);
                    }
                }

                // **3. Enhanced Serial event cleanup**
                if (SerialPortManager.Instance != null)
                {
                    try
                    {
                        SerialPortManager.Instance.DataGroupRequested -= OnDataGroupRequested_2;
                        SerialPortManager.Instance.DataReceivedRawEvent -= OnRawDataReceived_2;
                        Logger.Log("[UI2] SerialPortManager events unregistered", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[UI2] Error unregistering serial events: {ex.Message}", LogLevel.Error);
                    }
                }

                // **4. Enhanced cleanup verification log**
                string logFile = $"CleanupResources2_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                try
                {
                    var manual = Manual_Data_Set2.CurrentManual_2;
                    string cleanupLog = $"เวลา: {DateTime.Now}\n" +
                                       $"Thermo: {text2_Thermo_Set2.Text}\n" +
                                       $"Stirrer: {text2_Motor_Stirrer_Set2.Text}\n" +
                                       $"Mode: {(manual?.IsTjMode_2 == true ? "Tj" : "Tr")}\n" +
                                       $"Manual_Set2 LastTr: {manual?.LastTrSetpoint_2}\n" +
                                       $"Manual_Set2 LastTj: {manual?.LastTjSetpoint_2}\n" +
                                       $"LastValidThermo: {_lastValidThermoValue_2}\n" +
                                       $"LastValidStirrer: {_lastValidStirrerValue_2}\n" +
                                       $"Cleanup Status: Success\n" +
                                       $"File Path: {Path.GetFullPath(Manual_Data_Set2.DefaultFilePath_2)}";

                    File.WriteAllText(logFile, cleanupLog);
                    Logger.Log($"[UI2] Created enhanced cleanup log at: {Path.GetFullPath(logFile)}", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UI2] Error creating cleanup log: {ex.Message}", LogLevel.Error);
                }

                // **5. Final status log**
                CreateConsolidatedVerificationLog("ResourceCleanup", new Dictionary<string, object>
                {
                    ["CleanupStatus"] = "Success",
                    ["TimersDisposed"] = "2",
                    ["EventsUnregistered"] = "SerialPort",
                    ["DataPreserved"] = "Manual_Set2",
                    ["ValidationPreserved"] = "LastValidValues"
                });

                // **6. Clean references**
                _previousParent_2 = null;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI2] Error in CleanupResources2: {ex.Message}", LogLevel.Error);

                // **Ultimate fallback log**
                CreateConsolidatedVerificationLog("ResourceCleanup", new Dictionary<string, object>
                {
                    ["CleanupStatus"] = "PartialFailure",
                    ["Error"] = ex.Message
                });
            }

            Logger.Log("[UI2] CleanupResources2 completed", LogLevel.Info);
        }

        #endregion

        #region เมธอดสำหรับบันทึกค่าก่อนปิดโปรแกรม

        /// <summary>
        /// **ปรับปรุง: Enhanced Form Closing Save with Multiple Recovery Strategies**
        /// </summary>
        public void SaveBeforeFormClosing_2()
        {
            lock (_saveLock)
            {
                try
                {
                    Logger.Log("[UI2] SaveBeforeFormClosing_2 started with comprehensive save system", LogLevel.Info);

                    // หยุด timers ทั้งหมด
                    if (_debouncedSaveTimer != null)
                    {
                        _debouncedSaveTimer.Stop();
                    }

                    // Enhanced batch save with validation
                    ValidateAndSaveThermo_2();
                    ValidateAndSaveStirrer_2();

                    // Enhanced Manual_Set2 save with multiple strategies
                    bool saveSuccess = false;
                    Exception lastException = null;

                    // Strategy 1: Normal save (ใช้ SimpleSave_2)
                    try
                    {
                        if (Manual_Data_Set2.CurrentManual_2 != null)
                        {
                            Manual_Data_Set2.CurrentManual_2.Switch_A_M2 = switch_A_M2.IsOn;
                            Manual_Data_Set2.SaveDirectToFile_2("FormClosing"); // ใช้ Static method
                            saveSuccess = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        Logger.Log($"[UI2] Strategy 1 (Normal save) failed: {ex.Message}", LogLevel.Error);
                    }

                    // Strategy 2: Force save
                    if (!saveSuccess)
                    {
                        try
                        {
                            Manual_Data_Set2.ForceSave_2(); // ใช้ Static method
                            saveSuccess = true;
                            Logger.Log("[UI2] Strategy 2 (Force save) succeeded", LogLevel.Info);
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            Logger.Log($"[UI2] Strategy 2 (Force save) failed: {ex.Message}", LogLevel.Error);
                        }
                    }

                    // Strategy 3: Emergency text save
                    if (!saveSuccess)
                    {
                        try
                        {
                            string emergencyData = $"Emergency form closing save at {DateTime.Now}\n" +
                                                  $"Thermo: {text2_Thermo_Set2.Text}\n" +
                                                  $"Stirrer: {text2_Motor_Stirrer_Set2.Text}\n" +
                                                  $"Mode: {(Manual_Data_Set2.CurrentManual_2?.IsTjMode_2 == true ? "Tj" : "Tr")}\n" +
                                                  $"AutoManual: {(switch_A_M2.IsOn ? "Auto" : "Manual")}\n" +
                                                  $"LastTr: {Manual_Data_Set2.CurrentManual_2?.LastTrSetpoint_2}\n" +
                                                  $"LastTj: {Manual_Data_Set2.CurrentManual_2?.LastTjSetpoint_2}\n" +
                                                  $"LastValidThermo: {_lastValidThermoValue_2}\n" +
                                                  $"LastValidStirrer: {_lastValidStirrerValue_2}";

                            File.WriteAllText($"EmergencyFormClose_Set2_{DateTime.Now:yyyyMMdd_HHmmss}.txt", emergencyData);
                            saveSuccess = true;
                            Logger.Log("[UI2] Strategy 3 (Emergency text save) completed", LogLevel.Info);
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            Logger.Log($"[UI2] Strategy 3 (Emergency text save) failed: {ex.Message}", LogLevel.Error);
                        }
                    }

                    // Enhanced final verification log
                    if (saveSuccess)
                    {
                        CreateConsolidatedVerificationLog("FormClosingSuccess", new Dictionary<string, object>
                        {
                            ["FinalState"] = new
                            {
                                Mode = Manual_Data_Set2.CurrentManual_2?.IsTjMode_2 == true ? "Tj" : "Tr",
                                AutoManual = switch_A_M2.IsOn ? "Auto" : "Manual",
                                Thermo = text2_Thermo_Set2.Text,
                                Stirrer = text2_Motor_Stirrer_Set2.Text,
                                LastTr = Manual_Data_Set2.CurrentManual_2?.LastTrSetpoint_2,
                                LastTj = Manual_Data_Set2.CurrentManual_2?.LastTjSetpoint_2,
                                LastValidThermo = _lastValidThermoValue_2,
                                LastValidStirrer = _lastValidStirrerValue_2
                            }.ToString(),
                            ["ManualFile"] = Path.GetFullPath(Manual_Data_Set2.DefaultFilePath_2),
                            ["SaveStrategy"] = "Success"
                        });

                        Logger.Log("[UI2] SaveBeforeFormClosing_2 completed successfully", LogLevel.Info);
                    }
                    else
                    {
                        CreateConsolidatedVerificationLog("FormClosingFailure", new Dictionary<string, object>
                        {
                            ["LastError"] = lastException?.Message ?? "Unknown",
                            ["AllStrategiesFailed"] = "True"
                        });

                        Logger.Log($"[UI2] All save strategies failed in SaveBeforeFormClosing_2. Last error: {lastException?.Message}", LogLevel.Error);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UI2] Critical error in SaveBeforeFormClosing_2: {ex.Message}", LogLevel.Error);

                    CreateConsolidatedVerificationLog("FormClosingCriticalError", new Dictionary<string, object>
                    {
                        ["CriticalError"] = ex.Message,
                        ["StackTrace"] = ex.StackTrace
                    });
                }
            }
        }

        /// <summary>
        /// **ปรับปรุง: Enhanced Manual Save All with Comprehensive Validation**
        /// </summary>
        public void SaveAllSetting_2()
        {
            lock (_saveLock)
            {
                try
                {
                    Logger.Log("[UI2] SaveAllSetting_2 started with enhanced comprehensive system", LogLevel.Info);

                    // Force บันทึกทุกอย่างโดยไม่สนใจ dirty flags
                    ValidateAndSaveThermo_2();
                    ValidateAndSaveStirrer_2();

                    // Enhanced Manual_Set2 save with validation
                    bool allSaveSuccess = true;
                    var saveResults = new List<string>();

                    try
                    {
                        if (Manual_Data_Set2.CurrentManual_2 != null)
                        {
                            Manual_Data_Set2.CurrentManual_2.Switch_A_M2 = switch_A_M2.IsOn;
                            Manual_Data_Set2.SaveDirectToFile_2("ManualSaveAll"); // ใช้ Static method
                            saveResults.Add("Manual_Set2: Success");
                        }
                        else
                        {
                            saveResults.Add("Manual_Set2: Failed - Null instance");
                            allSaveSuccess = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        saveResults.Add($"Manual_Set2: Failed - {ex.Message}");
                        allSaveSuccess = false;
                        Logger.Log($"[UI2] Manual_Set2 save failed: {ex.Message}", LogLevel.Error);
                    }

                    // DeviceSettings save
                    try
                    {
                        if (deviceSettings_2 != null)
                        {
                            deviceSettings_2.Save_Set2("Settings_2.xml");
                            saveResults.Add("DeviceSettings_2: Success");
                        }
                        else
                        {
                            saveResults.Add("DeviceSettings_2: Skipped - Null instance");
                        }
                    }
                    catch (Exception ex)
                    {
                        saveResults.Add($"DeviceSettings_2: Failed - {ex.Message}");
                        allSaveSuccess = false;
                        Logger.Log($"[UI2] DeviceSettings_2 save failed: {ex.Message}", LogLevel.Error);
                    }

                    // Enhanced comprehensive verification log
                    CreateConsolidatedVerificationLog("ManualSaveAllCompleted", new Dictionary<string, object>
                    {
                        ["TriggerType"] = "Manual",
                        ["OverallSuccess"] = allSaveSuccess,
                        ["Timestamp"] = DateTime.Now,
                        ["SaveResults"] = string.Join("; ", saveResults),
                        ["DataSnapshot"] = new
                        {
                            Thermo = text2_Thermo_Set2.Text,
                            Stirrer = text2_Motor_Stirrer_Set2.Text,
                            Mode = Manual_Data_Set2.CurrentManual_2?.IsTjMode_2 == true ? "Tj" : "Tr",
                            LastTr = Manual_Data_Set2.CurrentManual_2?.LastTrSetpoint_2,
                            LastTj = Manual_Data_Set2.CurrentManual_2?.LastTjSetpoint_2,
                            AutoManual = switch_A_M2.IsOn ? "Auto" : "Manual",
                            LastValidThermo = _lastValidThermoValue_2,
                            LastValidStirrer = _lastValidStirrerValue_2
                        }.ToString()
                    });

                    if (allSaveSuccess)
                    {
                        Logger.Log("[UI2] SaveAllSetting_2 completed successfully - all components saved", LogLevel.Info);
                        MessageBox.Show("บันทึกการตั้งค่าทั้งหมดเรียบร้อยแล้ว", "บันทึกสำเร็จ",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        Logger.Log("[UI2] SaveAllSetting_2 completed with some failures", LogLevel.Warn);
                        MessageBox.Show($"บันทึกการตั้งค่าเสร็จสิ้น แต่มีบางส่วนที่ไม่สำเร็จ:\n\n{string.Join("\n", saveResults)}\n\nโปรดตรวจสอบ log files สำหรับรายละเอียดเพิ่มเติม",
                            "บันทึกเสร็จสิ้น (มีข้อผิดพลาด)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UI2] Critical error in SaveAllSetting_2: {ex.Message}", LogLevel.Error);

                    CreateConsolidatedVerificationLog("ManualSaveAllCriticalError", new Dictionary<string, object>
                    {
                        ["CriticalError"] = ex.Message,
                        ["StackTrace"] = ex.StackTrace
                    });

                    MessageBox.Show(
                        $"เกิดข้อผิดพลาดร้ายแรงในการบันทึกการตั้งค่า:\n\n{ex.Message}\n\nโปรดตรวจสอบไฟล์ข้อมูลและลองใหม่อีกครั้ง\nหรือติดต่อผู้ดูแลระบบหากปัญหายังคงอยู่",
                        "ข้อผิดพลาดร้ายแรงการบันทึก",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        #endregion

        #region เครื่องมือและฟังก์ชันช่วยเหลือ

        private static float ParseFloat_2(string s, float defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s))
                return defaultValue;

            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
        }

        private static float Clamp_2(float v, float min, float max) => (v < min) ? min : (v > max) ? max : v;

        private static float HalfToFloat_2(ushort half)
        {
            int sign = (half >> 15) & 0x1;
            int exp = (half >> 10) & 0x1F;
            int mant = half & 0x3FF;
            int f;

            if (exp == 0)
            {
                if (mant == 0) f = sign << 31;
                else
                {
                    while ((mant & 0x400) == 0) { mant <<= 1; exp--; }
                    exp++; mant &= ~0x400;
                    exp += (127 - 15); mant <<= 13;
                    f = (sign << 31) | (exp << 23) | mant;
                }
            }
            else if (exp == 31)
            {
                f = (sign << 31) | unchecked((int)0x7F800000) | (mant << 13);
            }
            else
            {
                exp += (127 - 15); mant <<= 13;
                f = (sign << 31) | (exp << 23) | mant;
            }

            byte[] b32 = BitConverter.GetBytes(f);
            return BitConverter.ToSingle(b32, 0);
        }

        #endregion
    }
}