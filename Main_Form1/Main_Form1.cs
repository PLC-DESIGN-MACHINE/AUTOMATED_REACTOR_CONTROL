using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// หน้าจอหลักของโปรแกรม Automated Reactor Control
    /// แก้ไขปัญหา XML Management, UserControl Lifecycle และ Architecture
    /// พร้อม Multiple Panel System (ไม่กระพริบ)
    /// </summary>
    public partial class Main_Form1 : Form
    {
        #region Private Fields

        // UserControls - ใช้ Lazy Loading แทนการสร้างทั้งหมดตั้งแต่เริ่มต้น
        private Lazy<UC_CONTROL_SET_1> controlSet1;
        private Lazy<UC_CONTROL_SET_2> controlSet2;
        private Lazy<UC_PROGRAM_CONTROL_SET_1> programSet1;
        private Lazy<UC_PROGRAM_CONTROL_SET_2> programSet2;
        private Lazy<UC_Setting> setting;
        private Lazy<UC_Graph_Data_Set_1> graphDataSet1;
        private Lazy<UC_Graph_Data_Set_2> graphDataSet2;

        // Configuration และ File Management
        private ConfigurationManager configManager;
        private UserControlManager userControlManager;
        private ErrorHandler errorHandler;

        // UI และ State Management
        private UserControl currentControl;
        private readonly object saveLock = new object();
        private volatile bool isClosing = false;
        private CancellationTokenSource cancellationTokenSource;

        // Event Handlers
        public event EventHandler<UserControlChangedEventArgs> UserControlChanged;
        public event EventHandler<SaveOperationEventArgs> SaveOperationCompleted;

        #endregion

        #region ✅ Multiple Panel Management System

        // Dictionary เก็บ Panel ที่ active อยู่
        private Dictionary<string, Panel> activePanels = new Dictionary<string, Panel>();
        private Dictionary<string, UserControl> cachedControls = new Dictionary<string, UserControl>();
        private HashSet<string> keepAliveControls = new HashSet<string> { "UC_CONTROL_SET_1", "UC_CONTROL_SET_2" };
        private HashSet<string> cacheableControls = new HashSet<string> { "UC_PROGRAM_CONTROL_SET_1", "UC_PROGRAM_CONTROL_SET_2" };
        private readonly object panelLock = new object();

        // ✅ Slide Animation Fields (แทนที่ Fade Animation)
        private System.Windows.Forms.Timer slideTimer;
        private Panel currentSlidingPanel;          // Panel ที่กำลัง slide out
        private Panel targetSlidingPanel;           // Panel ที่กำลัง slide in
        private int slideStep = 0;                  // ขั้นตอนปัจจุบัน
        private int totalSlideSteps = 8;            // จำนวนขั้นตอนทั้งหมด
        private int slideDistance = 0;              // ระยะทางที่ต้อง slide
        private bool isSliding = false;             // สถานะ animation
        private const int SLIDE_INTERVAL = 25;      // 40 FPS = smooth แต่ไม่กระตุก

        #endregion
        #region ✅ Slide Animation System (แทนที่ Fade Animation)

        /// <summary>
        /// เริ่ม Slide Animation (ไม่กระตุก)
        /// </summary>
        private void StartSlideAnimation(Panel fromPanel, Panel toPanel, Panel container)
        {
            try
            {
                // หยุด animation เก่า
                StopSlideAnimation();

                Logger.Log("[Animation] เริ่ม Slide Animation", LogLevel.Debug);

                // ตั้งค่า animation
                currentSlidingPanel = fromPanel;
                targetSlidingPanel = toPanel;
                slideStep = 0;
                slideDistance = container.Width;
                isSliding = true;

                // เตรียม target panel
                if (targetSlidingPanel != null)
                {
                    targetSlidingPanel.Dock = DockStyle.None; // ปิด Dock เพื่อใช้ Location
                    targetSlidingPanel.Size = container.Size;
                    targetSlidingPanel.Location = new Point(slideDistance, 0); // เริ่มนอกจอขวา
                    targetSlidingPanel.Visible = true;
                    targetSlidingPanel.BringToFront();
                }

                // เตรียม current panel
                if (currentSlidingPanel != null)
                {
                    currentSlidingPanel.Dock = DockStyle.None; // ปิด Dock เพื่อใช้ Location
                    currentSlidingPanel.Size = container.Size;
                    currentSlidingPanel.Location = new Point(0, 0); // เริ่มตำแหน่งปกติ
                }

                // เริ่ม Timer
                slideTimer = new System.Windows.Forms.Timer();
                slideTimer.Interval = SLIDE_INTERVAL;
                slideTimer.Tick += SlideTimer_Tick;
                slideTimer.Start();

                Logger.Log($"[Animation] Slide Timer เริ่มทำงาน - Steps: {totalSlideSteps}, Distance: {slideDistance}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Animation][ERROR] StartSlideAnimation: {ex.Message}", LogLevel.Error);
                FinishSlideImmediate();
            }
        }

        /// <summary>
        /// Timer สำหรับ Slide Animation
        /// </summary>
        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!isSliding)
                {
                    StopSlideAnimation();
                    return;
                }

                slideStep++;
                double progress = (double)slideStep / totalSlideSteps;

                if (progress >= 1.0)
                {
                    // Animation เสร็จสิ้น
                    FinishSlideAnimation();
                    return;
                }

                // ใช้ Easing function สำหรับ smooth movement
                double easedProgress = EaseOutCubic(progress);
                int currentOffset = (int)(slideDistance * easedProgress);

                // เลื่อน panels
                if (currentSlidingPanel != null && !currentSlidingPanel.IsDisposed)
                {
                    currentSlidingPanel.Location = new Point(-currentOffset, 0); // เลื่อนออกซ้าย
                }

                if (targetSlidingPanel != null && !targetSlidingPanel.IsDisposed)
                {
                    targetSlidingPanel.Location = new Point(slideDistance - currentOffset, 0); // เลื่อนเข้าจากขวา
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Animation][ERROR] SlideTimer_Tick: {ex.Message}", LogLevel.Error);
                FinishSlideImmediate();
            }
        }

        /// <summary>
        /// เสร็จสิ้น Slide Animation
        /// </summary>
        private void FinishSlideAnimation()
        {
            try
            {
                Logger.Log("[Animation] เสร็จสิ้น Slide Animation", LogLevel.Debug);

                // ตั้งตำแหน่งและ Dock สุดท้าย
                if (targetSlidingPanel != null && !targetSlidingPanel.IsDisposed)
                {
                    targetSlidingPanel.Location = new Point(0, 0);
                    targetSlidingPanel.Dock = DockStyle.Fill; // คืน Dock
                    targetSlidingPanel.Visible = true;
                }

                // ซ่อน current panel
                if (currentSlidingPanel != null && !currentSlidingPanel.IsDisposed)
                {
                    currentSlidingPanel.Visible = false;
                    currentSlidingPanel.Dock = DockStyle.Fill; // คืน Dock
                }

                // หยุด animation
                StopSlideAnimation();

                Logger.Log("[Animation] Slide Animation เสร็จสิ้นสมบูรณ์", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Animation][ERROR] FinishSlideAnimation: {ex.Message}", LogLevel.Error);
                FinishSlideImmediate();
            }
        }

        /// <summary>
        /// จบ Animation ทันที (กรณี error)
        /// </summary>
        private void FinishSlideImmediate()
        {
            try
            {
                StopSlideAnimation();

                // แสดง target panel ปกติ
                if (targetSlidingPanel != null && !targetSlidingPanel.IsDisposed)
                {
                    targetSlidingPanel.Location = new Point(0, 0);
                    targetSlidingPanel.Dock = DockStyle.Fill;
                    targetSlidingPanel.Visible = true;
                }

                // ซ่อน current panel
                if (currentSlidingPanel != null && !currentSlidingPanel.IsDisposed)
                {
                    currentSlidingPanel.Visible = false;
                    currentSlidingPanel.Dock = DockStyle.Fill;
                }

                Logger.Log("[Animation] Animation จบแบบทันที", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Animation][ERROR] FinishSlideImmediate: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// หยุด Slide Animation
        /// </summary>
        private void StopSlideAnimation()
        {
            try
            {
                if (slideTimer != null)
                {
                    slideTimer.Stop();
                    slideTimer.Tick -= SlideTimer_Tick;
                    slideTimer.Dispose();
                    slideTimer = null;
                }

                isSliding = false;
                currentSlidingPanel = null;
                targetSlidingPanel = null;
                slideStep = 0;
                slideDistance = 0;
            }
            catch (Exception ex)
            {
                Logger.Log($"[Animation][ERROR] StopSlideAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Easing function สำหรับ smooth animation
        /// </summary>
        private double EaseOutCubic(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        #endregion
        // เพิ่มเมธอดเหล่านี้ใน Main_Form1.cs เพื่อแก้ปัญหา Navigation State

        #region 🚀 Navigation State Reset Fix (เพิ่มใหม่)

        /// <summary>
        /// 🚀 แก้ไข: Reset Navigation State เมื่อแสดง Cached Panel
        /// </summary>
        private void ResetCachedControlNavigationState(string controlType, Panel targetPanel)
        {
            try
            {
                Logger.Log($"[NavFix] 🔄 Resetting navigation state for: {controlType}", LogLevel.Info);

                // หา UserControl ใน Panel
                UserControl userControl = null;
                foreach (Control control in targetPanel.Controls)
                {
                    if (control is UserControl uc && uc.GetType().Name == controlType)
                    {
                        userControl = uc;
                        break;
                    }
                }

                if (userControl == null)
                {
                    Logger.Log($"[NavFix] ⚠️ UserControl not found in panel: {controlType}", LogLevel.Warn);
                    return;
                }

                // 🎯 Reset Navigation State สำหรับ UC_CONTROL_SET_1
                if (controlType == "UC_CONTROL_SET_1" && userControl is UC_CONTROL_SET_1 ucControl1)
                {
                    ResetUC1NavigationState(ucControl1);
                }
                // 🎯 Reset Navigation State สำหรับ UC_CONTROL_SET_2  
                else if (controlType == "UC_CONTROL_SET_2" && userControl is UC_CONTROL_SET_2 ucControl2)
                {
                    ResetUC2NavigationState(ucControl2);
                }
                // 🎯 Reset สำหรับ UserControls อื่นๆ
                else
                {
                    ResetGenericNavigationState(userControl, controlType);
                }

                // 🔄 Trigger VisibleChanged event เพื่อให้ UserControl รู้ว่ากลับมาแสดงแล้ว
                TriggerUserControlReactivation(userControl, controlType);

                Logger.Log($"[NavFix] ✅ Navigation state reset completed: {controlType}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] 🚨 Error resetting navigation state: {ex.Message}", LogLevel.Error);
            }
        }


        /// <summary>
        /// 🎯 Reset Navigation State สำหรับ UC_CONTROL_SET_1
        /// </summary>
        private void ResetUC1NavigationState(UC_CONTROL_SET_1 ucControl1)
        {
            try
            {
                Logger.Log("[NavFix] 🎯 Resetting UC_CONTROL_SET_1 navigation state", LogLevel.Info);

                // 🔧 เรียก ForceResetAllStates ถ้ามี method นี้
                try
                {
                    var forceResetMethod = ucControl1.GetType().GetMethod("ForceResetAllStates",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (forceResetMethod != null)
                    {
                        forceResetMethod.Invoke(ucControl1, null);
                        Logger.Log("[NavFix] ✅ ForceResetAllStates called successfully", LogLevel.Info);
                    }
                    else
                    {
                        Logger.Log("[NavFix] ⚠️ ForceResetAllStates method not found", LogLevel.Warn);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[NavFix] ⚠️ Error calling ForceResetAllStates: {ex.Message}", LogLevel.Warn);
                }

                // 🔘 Reset Button States
                ResetUC1ButtonStates(ucControl1);

                // 🔄 Trigger NavigationHelper reset
                NavigationHelper.ForceResetNavigationState();

                Logger.Log("[NavFix] ✅ UC_CONTROL_SET_1 navigation state reset completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] 🚨 Error resetting UC1 navigation state: {ex.Message}", LogLevel.Error);
            }
        }


        /// <summary>
        /// 🔘 Reset Button States ใน UC_CONTROL_SET_1
        /// </summary>
        private void ResetUC1ButtonStates(UC_CONTROL_SET_1 ucControl1)
        {
            try
            {
                var buttonNames = new[] {
            "But_CONTROL1_SET_2",
            "But_Graph_Data1",
            "But_Program_Sequence1",
            "but_Setting1"
        };

                foreach (string buttonName in buttonNames)
                {
                    var button = FindControlRecursive(ucControl1, buttonName) as Button;
                    if (button != null)
                    {
                        button.Enabled = true;
                        Logger.Log($"[NavFix] ✅ Button enabled: {buttonName}", LogLevel.Debug);
                    }
                    else
                    {
                        Logger.Log($"[NavFix] ⚠️ Button not found: {buttonName}", LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error resetting button states: {ex.Message}", LogLevel.Error);
            }
        }
        /// <summary>
        /// 🎯 Reset Navigation State สำหรับ UC_CONTROL_SET_2
        /// </summary>
        private void ResetUC2NavigationState(UC_CONTROL_SET_2 ucControl2)
        {
            try
            {
                Logger.Log("[NavFix] 🎯 Resetting UC_CONTROL_SET_2 navigation state", LogLevel.Info);

                // Reset methods สำหรับ UC_CONTROL_SET_2 (ถ้ามี)
                try
                {
                    var forceResetMethod = ucControl2.GetType().GetMethod("ForceResetAllStates",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    forceResetMethod?.Invoke(ucControl2, null);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[NavFix] Error calling UC2 ForceResetAllStates: {ex.Message}", LogLevel.Warn);
                }

                Logger.Log("[NavFix] ✅ UC_CONTROL_SET_2 navigation state reset completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error resetting UC2 navigation state: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🔄 Reset Navigation State สำหรับ UserControls อื่นๆ
        /// </summary>
        private void ResetGenericNavigationState(UserControl userControl, string controlType)
        {
            try
            {
                Logger.Log($"[NavFix] 🔄 Resetting generic navigation state: {controlType}", LogLevel.Debug);

                // Enable buttons ทั่วไป
                EnableAllButtonsInControl(userControl);

                // Refresh UserControl
                userControl.Refresh();
                userControl.Invalidate();

                Logger.Log($"[NavFix] ✅ Generic navigation state reset: {controlType}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error resetting generic navigation state: {ex.Message}", LogLevel.Error);
            }
        }


        /// <summary>
        /// 🔘 Enable All Buttons ใน UserControl
        /// </summary>
        private void EnableAllButtonsInControl(UserControl userControl)
        {
            try
            {
                var buttons = GetAllControlsRecursive(userControl).OfType<Button>();
                foreach (var button in buttons)
                {
                    button.Enabled = true;
                }

                Logger.Log($"[NavFix] ✅ All buttons enabled in: {userControl.GetType().Name}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error enabling buttons: {ex.Message}", LogLevel.Error);
            }
        }


        /// <summary>
        /// 🔄 Trigger UserControl Reactivation Events
        /// </summary>
        private void TriggerUserControlReactivation(UserControl userControl, string controlType)
        {
            try
            {
                Logger.Log($"[NavFix] 🔄 Triggering reactivation events: {controlType}", LogLevel.Debug);

                // แน่ใจว่า UserControl visible
                userControl.Visible = true;

                // Trigger Load event (ถ้ายังไม่ได้ trigger)
                if (!userControl.IsHandleCreated)
                {
                    userControl.CreateControl();
                }

                // ✅ แก้ไข: ใช้ reflection เพื่อเรียก OnVisibleChanged
                try
                {
                    var onVisibleChangedMethod = typeof(Control).GetMethod("OnVisibleChanged",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (onVisibleChangedMethod != null)
                    {
                        onVisibleChangedMethod.Invoke(userControl, new object[] { EventArgs.Empty });
                        Logger.Log($"[NavFix] ✅ OnVisibleChanged triggered via reflection", LogLevel.Debug);
                    }
                    else
                    {
                        // Fallback: Toggle Visible property เพื่อ trigger event
                        bool wasVisible = userControl.Visible;
                        userControl.Visible = false;
                        Application.DoEvents(); // ให้ event process
                        userControl.Visible = wasVisible;
                        Logger.Log($"[NavFix] ✅ VisibleChanged triggered via property toggle", LogLevel.Debug);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[NavFix] ⚠️ Could not trigger OnVisibleChanged: {ex.Message}", LogLevel.Warn);

                    // Fallback: ใช้ Refresh และ Focus แทน
                    userControl.Refresh();
                    userControl.Invalidate();
                    Logger.Log($"[NavFix] ✅ Used Refresh fallback", LogLevel.Debug);
                }

                // Focus เพื่อให้ UserControl รู้ว่าถูกใช้งาน
                if (userControl.CanFocus)
                {
                    userControl.Focus();
                }

                Logger.Log($"[NavFix] ✅ Reactivation events triggered: {controlType}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error triggering reactivation: {ex.Message}", LogLevel.Error);
            }
        }
        /// <summary>
        /// 🔍 Find Control Recursively
        /// </summary>
        private Control FindControlRecursive(Control parent, string name)
        {
            try
            {
                if (parent.Name == name)
                    return parent;

                foreach (Control child in parent.Controls)
                {
                    var found = FindControlRecursive(child, name);
                    if (found != null)
                        return found;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 🔍 Get All Controls Recursively
        /// </summary>
        private IEnumerable<Control> GetAllControlsRecursive(Control parent)
        {
            var controls = new List<Control>();

            try
            {
                foreach (Control control in parent.Controls)
                {
                    controls.Add(control);
                    controls.AddRange(GetAllControlsRecursive(control));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error in GetAllControlsRecursive: {ex.Message}", LogLevel.Error);
            }

            return controls;
        }

        /// <summary>
        /// 🎬 Enhanced Animation Completion Check
        /// </summary>
        private void EnsureAnimationCompleted(Action onCompleted)
        {
            try
            {
                if (!isSliding)
                {
                    // Animation ไม่ได้ทำงาน - execute ทันที
                    onCompleted?.Invoke();
                    return;
                }

                // รอให้ animation เสร็จ (timeout 3 วินาที)
                var timeout = DateTime.Now.AddSeconds(3);
                var checkTimer = new System.Windows.Forms.Timer();
                checkTimer.Interval = 50; // Check every 50ms

                checkTimer.Tick += (sender, e) =>
                {
                    try
                    {
                        if (!isSliding || DateTime.Now > timeout)
                        {
                            checkTimer.Stop();
                            checkTimer.Dispose();

                            if (isSliding && DateTime.Now > timeout)
                            {
                                Logger.Log("[NavFix] ⏱️ Animation timeout - forcing completion", LogLevel.Warn);
                                ForceCompleteAnimation();
                            }

                            onCompleted?.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[NavFix] Error in animation check: {ex.Message}", LogLevel.Error);
                        checkTimer.Stop();
                        checkTimer.Dispose();
                        onCompleted?.Invoke();
                    }
                };

                checkTimer.Start();
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error ensuring animation completion: {ex.Message}", LogLevel.Error);
                onCompleted?.Invoke();
            }
        }


        /// <summary>
        /// 🎬 Force Complete Animation
        /// </summary>
        private void ForceCompleteAnimation()
        {
            try
            {
                Logger.Log("[NavFix] 🎬 Force completing animation", LogLevel.Info);

                StopSlideAnimation();

                if (targetSlidingPanel != null && !targetSlidingPanel.IsDisposed)
                {
                    targetSlidingPanel.Location = new Point(0, 0);
                    targetSlidingPanel.Dock = DockStyle.Fill;
                    targetSlidingPanel.Visible = true;
                }

                if (currentSlidingPanel != null && !currentSlidingPanel.IsDisposed)
                {
                    currentSlidingPanel.Visible = false;
                    currentSlidingPanel.Dock = DockStyle.Fill;
                }

                Logger.Log("[NavFix] ✅ Animation force completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error force completing animation: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🛠️ Enhanced Panel System Methods (แก้ไขเดิม)


        /// <summary>
        /// 🚀 เพิ่มใหม่: Post-Animation State Reset
        /// </summary>
        private void PostAnimationStateReset(string controlType, Panel targetPanel)
        {
            try
            {
                Logger.Log($"[NavFix] 🔄 Post-animation state reset: {controlType}", LogLevel.Debug);

                // Delay เล็กน้อยเพื่อให้ UI settle
                var resetTimer = new System.Windows.Forms.Timer();
                resetTimer.Interval = 100; // 100ms delay
                resetTimer.Tick += (sender, e) =>
                {
                    try
                    {
                        resetTimer.Stop();
                        resetTimer.Dispose();

                        // Final state reset
                        ResetCachedControlNavigationState(controlType, targetPanel);

                        // Reset NavigationHelper global state
                        NavigationHelper.ForceResetNavigationState();

                        Logger.Log($"[NavFix] ✅ Post-animation state reset completed: {controlType}", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[NavFix] Error in post-animation reset: {ex.Message}", LogLevel.Error);
                    }
                };
                resetTimer.Start();
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error in PostAnimationStateReset: {ex.Message}", LogLevel.Error);
            }
        }


        /// <summary>
        /// 🚀 เพิ่มใหม่: Initialize New UserControl State
        /// </summary>
        private void InitializeNewUserControlState(UserControl uc, string controlType)
        {
            try
            {
                Logger.Log($"[NavFix] 🎯 Initializing new UserControl state: {controlType}", LogLevel.Debug);

                // สำหรับ UC_CONTROL_SET_1 - ให้แน่ใจว่า navigation state ถูกต้อง
                if (controlType == "UC_CONTROL_SET_1" && uc is UC_CONTROL_SET_1 ucControl1)
                {
                    // Reset NavigationHelper state
                    NavigationHelper.ForceResetNavigationState();

                    // อาจจะเรียก initialization methods อื่นๆ ถ้าจำเป็น
                    Logger.Log("[NavFix] ✅ UC_CONTROL_SET_1 initial state set", LogLevel.Debug);
                }

                Logger.Log($"[NavFix] ✅ New UserControl state initialized: {controlType}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error initializing new UserControl state: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🚀 เพิ่มใหม่: Post-Animation New Panel Setup
        /// </summary>
        private void PostAnimationNewPanelSetup(UserControl uc, string controlType, Panel newPanel)
        {
            try
            {
                Logger.Log($"[NavFix] 🔧 Post-animation new panel setup: {controlType}", LogLevel.Debug);

                // Final state verification
                InitializeNewUserControlState(uc, controlType);

                // Trigger reactivation events
                TriggerUserControlReactivation(uc, controlType);

                Logger.Log($"[NavFix] ✅ Post-animation new panel setup completed: {controlType}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] Error in post-animation new panel setup: {ex.Message}", LogLevel.Error);
            }
        }


        #endregion
        #region Safe Invoke Methods

        /// <summary>
        /// เรียกใช้ Action บน UI Thread อย่างปลอดภัย
        /// </summary>
        private bool SafeInvoke(Action action)
        {
            try
            {
                if (this.IsDisposed || this.Disposing || !this.IsHandleCreated || isClosing)
                {
                    Logger.Log("[Main_Form1] SafeInvoke: Form ไม่พร้อมใช้งาน", LogLevel.Debug);
                    return false;
                }

                if (this.InvokeRequired)
                {
                    if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
                        return false;

                    this.Invoke(action);
                }
                else
                {
                    action();
                }
                return true;
            }
            catch (ObjectDisposedException)
            {
                Logger.Log("[Main_Form1] SafeInvoke: Form ถูก dispose แล้ว", LogLevel.Debug);
                return false;
            }
            catch (InvalidOperationException)
            {
                Logger.Log("[Main_Form1] SafeInvoke: Form handle ไม่พร้อม", LogLevel.Debug);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SafeInvoke: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// เรียกใช้ Func บน UI Thread อย่างปลอดภัย
        /// </summary>
        private T SafeInvoke<T>(Func<T> func, T defaultValue = default(T))
        {
            try
            {
                if (this.IsDisposed || this.Disposing || !this.IsHandleCreated || isClosing)
                    return defaultValue;

                if (this.InvokeRequired)
                {
                    if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
                        return defaultValue;

                    return (T)this.Invoke(func);
                }
                else
                {
                    return func();
                }
            }
            catch (ObjectDisposedException)
            {
                Logger.Log("[Main_Form1] SafeInvoke<T>: Form ถูก dispose แล้ว", LogLevel.Debug);
                return defaultValue;
            }
            catch (InvalidOperationException)
            {
                Logger.Log("[Main_Form1] SafeInvoke<T>: Form handle ไม่พร้อม", LogLevel.Debug);
                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SafeInvoke<T>: {ex.Message}", LogLevel.Error);
                return defaultValue;
            }
        }

        #endregion

        #region Constructor และ Initialization

        public Main_Form1()
        {
            try
            {
                XmlRepairUtility.RepairAllXmlFiles();

                // 1. เริ่มต้น UI และ Logger
                InitializeComponent();

                // 2. สร้าง panelMain ถ้าไม่มี และเปิด Double Buffering
                EnsureMainPanelExists();
                EnableDoubleBuffering();

                InitializeLogger();

                // 3. เริ่มต้น Management Classes
                InitializeManagers();

                // 4. เริ่มต้น Lazy UserControls
                InitializeLazyUserControls();

                // 5. ตรวจสอบและสร้างไฟล์ที่จำเป็น
                Task.Run(async () => await configManager.EnsureAllRequiredFilesAsync());

                // 6. เริ่มต้น SerialPortManager
                InitializeSerialPortManager();

                // 7. เริ่มต้น ProgramStates
                InitializeProgramStates();

                // 8. โหลดหน้าเริ่มต้น - ปรับให้ทำงานหลัง Form โหลดเสร็จ
                this.Load += Main_Form1_Load;

                // 9. ลงทะเบียน Events
                RegisterEvents();

                Logger.Log("[Main_Form1] เริ่มต้นโปรแกรมสำเร็จ (Multiple Panel System)", LogLevel.Info);
            }
            catch (Exception ex)
            {
                HandleCriticalError(ex, "เกิดข้อผิดพลาดในการเริ่มต้นโปรแกรม");
                MessageBox.Show($"Form initialization error: {ex.Message}");
            }
        }

        /// <summary>
        /// ตรวจสอบและสร้าง panelMain ถ้าไม่มี
        /// </summary>
        private void EnsureMainPanelExists()
        {
            try
            {
                var foundPanel = this.Controls.Find("panelMain", true).FirstOrDefault() as Panel;

                if (foundPanel == null)
                {
                    Logger.Log("[Main_Form1] ไม่พบ panelMain - สร้างใหม่", LogLevel.Warn);

                    var newPanel = new Panel
                    {
                        Name = "panelMain",
                        Dock = DockStyle.Fill,
                        BackColor = Color.White,
                        BorderStyle = BorderStyle.None
                    };

                    this.Controls.Add(newPanel);
                    newPanel.BringToFront();

                    Logger.Log("[Main_Form1] สร้าง panelMain ใหม่สำเร็จ", LogLevel.Info);
                }
                else
                {
                    Logger.Log("[Main_Form1] พบ panelMain อยู่แล้ว", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] EnsureMainPanelExists: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// เปิด Double Buffering สำหรับ Form และ Panel
        /// </summary>
        private void EnableDoubleBuffering()
        {
            try
            {
                // เปิด Double Buffering สำหรับ Form
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

                Logger.Log("[Main_Form1] เปิด Double Buffering สำเร็จ", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] EnableDoubleBuffering: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Event เมื่อ Form โหลดเสร็จ
        /// </summary>
        private void Main_Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Logger.Log("[Main_Form1] Form โหลดเสร็จ - เริ่มโหลด UserControl", LogLevel.Info);
                LoadDefaultUserControl();
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] Main_Form1_Load: {ex.Message}", LogLevel.Error);
                CreateFallbackUI();
            }
        }

        /// <summary>
        /// เริ่มต้น Logger อย่างปลอดภัย
        /// </summary>
        private void InitializeLogger()
        {
            try
            {
                Logger.CurrentLogLevel = LogLevel.Debug;
                Logger.Log("[Main_Form1] เริ่มต้นการทำงาน", LogLevel.Info);
            }
            catch (Exception logEx)
            {
                Debug.WriteLine($"[Main_Form1] Logger ไม่พร้อมใช้งาน: {logEx.Message}");
            }
        }

        /// <summary>
        /// เริ่มต้น Management Classes
        /// </summary>
        private void InitializeManagers()
        {
            try
            {
                configManager = new ConfigurationManager();
                userControlManager = new UserControlManager(this);
                errorHandler = new ErrorHandler();
                cancellationTokenSource = new CancellationTokenSource();

                Logger.Log("[Main_Form1] เริ่มต้น Managers สำเร็จ", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] InitializeManagers: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// เริ่มต้น Lazy UserControls - สร้างเมื่อจำเป็นเท่านั้น
        /// </summary>
        private void InitializeLazyUserControls()
        {
            try
            {
                controlSet1 = new Lazy<UC_CONTROL_SET_1>(() =>
                {
                    Logger.Log("[Main_Form1] สร้าง UC_CONTROL_SET_1", LogLevel.Info);
                    var control = new UC_CONTROL_SET_1();
                    userControlManager.RegisterControl("ControlSet1", control);
                    return control;
                });

                programSet1 = new Lazy<UC_PROGRAM_CONTROL_SET_1>(() =>
                {
                    Logger.Log("[Main_Form1] สร้าง UC_PROGRAM_CONTROL_SET_1", LogLevel.Info);
                    var control = new UC_PROGRAM_CONTROL_SET_1();
                    userControlManager.RegisterControl("ProgramSet1", control);
                    return control;
                });

                graphDataSet1 = new Lazy<UC_Graph_Data_Set_1>(() =>
                {
                    Logger.Log("[Main_Form1] สร้าง UC_Graph_Data_Set_1", LogLevel.Info);
                    var control = new UC_Graph_Data_Set_1();
                    userControlManager.RegisterControl("GraphDataSet1", control);
                    return control;
                });

                controlSet2 = new Lazy<UC_CONTROL_SET_2>(() =>
                {
                    Logger.Log("[Main_Form1] สร้าง UC_CONTROL_SET_2", LogLevel.Info);
                    var control = new UC_CONTROL_SET_2();
                    userControlManager.RegisterControl("ControlSet2", control);
                    return control;
                });

                programSet2 = new Lazy<UC_PROGRAM_CONTROL_SET_2>(() =>
                {
                    Logger.Log("[Main_Form1] สร้าง UC_PROGRAM_CONTROL_SET_2", LogLevel.Info);
                    var control = new UC_PROGRAM_CONTROL_SET_2();
                    userControlManager.RegisterControl("ProgramSet2", control);
                    return control;
                });

                graphDataSet2 = new Lazy<UC_Graph_Data_Set_2>(() =>
                {
                    Logger.Log("[Main_Form1] สร้าง UC_Graph_Data_Set_2", LogLevel.Info);
                    var control = new UC_Graph_Data_Set_2();
                    userControlManager.RegisterControl("GraphDataSet2", control);
                    return control;
                });

                setting = new Lazy<UC_Setting>(() =>
                {
                    Logger.Log("[Main_Form1] สร้าง UC_Setting", LogLevel.Info);
                    var control = new UC_Setting();
                    userControlManager.RegisterControl("Setting", control);
                    return control;
                });

                Logger.Log("[Main_Form1] เริ่มต้น Lazy UserControls สำเร็จ", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] InitializeLazyUserControls: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// เริ่มต้น SerialPortManager อย่างปลอดภัย
        /// </summary>
        private void InitializeSerialPortManager()
        {
            try
            {
                if (SerialPortManager.Instance == null)
                {
                    Logger.Log("[Main_Form1] SerialPortManager.Instance เป็น null", LogLevel.Warn);
                    return;
                }

                var settings1 = configManager.LoadDeviceSettings1();
                var settings2 = configManager.LoadDeviceSettings2();

                if (settings1 != null && settings2 != null)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            byte t1 = settings1.GetThermostatCode_1(1);
                            byte s1 = settings1.GetStirrerCode_1(1);
                            byte t2 = settings2.GetThermostatCode_2(2);
                            byte s2 = settings2.GetStirrerCode_2(2);

                            Logger.Log("[Main_Form1] SerialPortManager เริ่มต้นสำเร็จ", LogLevel.Info);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"[Main_Form1][ERROR] ส่งคำสั่งเริ่มต้น SerialPortManager ไม่สำเร็จ: {ex.Message}", LogLevel.Error);
                        }
                    });
                }
                else
                {
                    Logger.Log("[Main_Form1][WARNING] ไม่สามารถโหลดการตั้งค่าอุปกรณ์ได้", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                errorHandler?.HandleError(ex, "InitializeSerialPortManager", ErrorSeverity.Medium);
            }
        }

        /// <summary>
        /// เริ่มต้น ProgramStates แยกตาม Set
        /// </summary>
        private void InitializeProgramStates()
        {
            try
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        ProgramState_1.Initialize_Set_1();
                        Logger.Log("[Main_Form1] ProgramState_1 เริ่มต้นสำเร็จ", LogLevel.Info);
                    }
                    catch (Exception ex1)
                    {
                        errorHandler?.HandleError(ex1, "ProgramState_1.Initialize_Set_1", ErrorSeverity.High);
                    }
                });

                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        ProgramState_2.Initialize_Set_2();
                        Logger.Log("[Main_Form1] ProgramState_2 เริ่มต้นสำเร็จ", LogLevel.Info);
                    }
                    catch (Exception ex2)
                    {
                        errorHandler?.HandleError(ex2, "ProgramState_2.Initialize_Set_2", ErrorSeverity.High);
                    }
                });
            }
            catch (Exception ex)
            {
                errorHandler?.HandleError(ex, "InitializeProgramStates", ErrorSeverity.Critical);
            }
        }

        /// <summary>
        /// ลงทะเบียน Events ต่างๆ
        /// </summary>
        private void RegisterEvents()
        {
            try
            {
                this.FormClosing += Main_Form1_FormClosing;
                configManager.FileOperationCompleted += OnFileOperationCompleted;
                configManager.FileOperationFailed += OnFileOperationFailed;
                userControlManager.UserControlLoaded += OnUserControlLoaded;
                userControlManager.UserControlUnloaded += OnUserControlUnloaded;

                Logger.Log("[Main_Form1] ลงทะเบียน Events สำเร็จ", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] RegisterEvents: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region UserControl Management

        /// <summary>
        /// โหลด UserControl เริ่มต้น
        /// </summary>
        private void LoadDefaultUserControl()
        {
            try
            {
                Logger.Log("[Main_Form1] เริ่มโหลด UserControl เริ่มต้น", LogLevel.Info);

                if (TryLoadUserControlWithDebug(() => controlSet1.Value, "ControlSet1"))
                {
                    Logger.Log("[Main_Form1] โหลด UC_CONTROL_SET_1 เป็นหน้าเริ่มต้น", LogLevel.Info);
                }
                else if (TryLoadUserControlWithDebug(() => controlSet2.Value, "ControlSet2"))
                {
                    Logger.Log("[Main_Form1] โหลด UC_CONTROL_SET_2 เป็นหน้าเริ่มต้นสำรอง", LogLevel.Info);
                }
                else if (TryLoadUserControlWithDebug(() => setting.Value, "Setting"))
                {
                    Logger.Log("[Main_Form1] โหลด UC_Setting เป็นหน้าเริ่มต้นสำรอง", LogLevel.Info);
                }
                else
                {
                    Logger.Log("[Main_Form1] ไม่สามารถโหลด UserControl ใดได้ - สร้าง Fallback UI", LogLevel.Warn);
                    CreateFallbackUI();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] LoadDefaultUserControl: {ex.Message}", LogLevel.Error);
                errorHandler.HandleError(ex, "LoadDefaultUserControl", ErrorSeverity.High);
                CreateFallbackUI();
            }
        }

        /// <summary>
        /// ลองโหลด UserControl พร้อม debug info
        /// </summary>
        private bool TryLoadUserControlWithDebug(Func<UserControl> controlFactory, string controlName)
        {
            try
            {
                Logger.Log($"[Main_Form1] กำลังลองสร้าง {controlName}", LogLevel.Info);

                var control = controlFactory();

                if (control != null)
                {
                    Logger.Log($"[Main_Form1] สร้าง {controlName} สำเร็จ - กำลังโหลด", LogLevel.Info);
                    LoadUserControl(control);
                    return true;
                }
                else
                {
                    Logger.Log($"[Main_Form1] {controlName} คืนค่า null", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] TryLoadUserControlWithDebug {controlName}: {ex.Message}", LogLevel.Error);
                Logger.Log($"[Main_Form1][STACK] {ex.StackTrace}", LogLevel.Error);
            }
            return false;
        }

        public void LoadUserControl(UserControl uc)
        {
            if (uc == null || isClosing || this.IsDisposed)
            {
                Logger.Log("[Main_Form1][ERROR] ไม่สามารถโหลด UserControl ได้", LogLevel.Error);
                return;
            }

            try
            {
                string controlType = uc.GetType().Name;
                Panel mainContainer = FindMainPanel();

                if (mainContainer == null)
                {
                    Logger.Log("[Main_Form1] ไม่มี Panel - โหลดตรงใน Form", LogLevel.Warn);
                    LoadUserControlDirectToForm(uc);
                    return;
                }

                Logger.Log($"[Main_Form1] กำลังโหลด {controlType} ด้วย Multiple Panel System", LogLevel.Info);

                // ✅ เพิ่ม Debug logging
                Logger.Log($"[DEBUG] Panel created for: {controlType}", LogLevel.Debug);

                // ✅ บังคับใช้ Multiple Panel System
                LoadUserControlWithMultiplePanel(uc, controlType, mainContainer);

                // อัปเดตข้อมูล
                currentControl = uc;
                this.Text = $"Automated Reactor Control - {GetUserControlDisplayName(controlType)}";

                // ✅ บังคับ Refresh UserControl
                uc.Visible = true;
                uc.Refresh();

                UserControlChanged?.Invoke(this, new UserControlChangedEventArgs(controlType, uc));

                Logger.Log($"[Main_Form1] โหลด {controlType} สำเร็จ (ไม่กระพริบ)", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] LoadUserControl: {ex.Message}", LogLevel.Error);

                // ✅ Fallback แบบปลอดภัย
                try
                {
                    Logger.Log("[Main_Form1] ใช้ Fallback Method", LogLevel.Warn);
                    LoadUserControlFallback(uc);
                }
                catch (Exception ex2)
                {
                    Logger.Log($"[Main_Form1][ERROR] LoadUserControlFallback: {ex2.Message}", LogLevel.Error);
                    CreateErrorDisplay(ex.Message);
                }
            }
        }
        /// <summary>
        /// ✅ Fallback Method - แบบ SuspendLayout
        /// </summary>
        private void LoadUserControlFallback(UserControl uc)
        {
            try
            {
                string controlName = uc.GetType().Name;
                Logger.Log($"[Main_Form1] Fallback โหลด {controlName}", LogLevel.Info);

                Panel container = FindMainPanel();
                if (container == null)
                {
                    LoadUserControlDirectToForm(uc);
                    return;
                }

                // ✅ SuspendLayout Pattern
                container.SuspendLayout();
                this.SuspendLayout();

                // เตรียม UserControl
                uc.Tag = this;
                uc.Dock = DockStyle.Fill;
                uc.BackColor = Color.White;
                uc.Visible = true;  // ✅ บังคับให้แสดง

                // เปลี่ยน Controls แบบไม่กระพริบ
                container.Controls.Clear();
                container.Controls.Add(uc);
                uc.BringToFront();

                container.ResumeLayout(true);
                this.ResumeLayout(true);

                // อัปเดตตัวแปร
                currentControl = uc;
                this.Text = $"Automated Reactor Control - {GetUserControlDisplayName(controlName)}";

                // ✅ Force Refresh
                container.Refresh();
                uc.Refresh();

                Logger.Log($"[Main_Form1] Fallback โหลด {controlName} สำเร็จ", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] LoadUserControlFallback: {ex.Message}", LogLevel.Error);
                throw;
            }
        }
        private Panel FindMainPanel()
        {
            try
            {
                var panel = this.Controls.Find("panelMain", true).FirstOrDefault() as Panel;

                if (panel != null)
                {
                    Logger.Log("[Main_Form1] พบ panelMain ใน existing controls", LogLevel.Debug);
                    return panel;
                }

                panel = this.Controls.OfType<Panel>().FirstOrDefault();

                if (panel != null)
                {
                    Logger.Log($"[Main_Form1] พบ Panel: {panel.Name}", LogLevel.Debug);
                    return panel;
                }

                Logger.Log("[Main_Form1] ไม่พบ Panel ใดๆ - สร้างใหม่", LogLevel.Warn);
                return CreateNewMainPanel();
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] FindMainPanel: {ex.Message}", LogLevel.Error);
                return CreateNewMainPanel();
            }
        }

        private Panel CreateNewMainPanel()
        {
            try
            {
                var newPanel = new Panel
                {
                    Name = "panelMain",
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None
                };

                this.Controls.Add(newPanel);
                newPanel.BringToFront();

                Logger.Log("[Main_Form1] สร้าง Main Panel ใหม่สำเร็จ", LogLevel.Info);

                return newPanel;
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] CreateNewMainPanel: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        private void LoadUserControlDirectToForm(UserControl uc)
        {
            try
            {
                string controlName = uc.GetType().Name;
                Logger.Log($"[Main_Form1] โหลด {controlName} ตรงใน Form", LogLevel.Info);

                var controlsToRemove = this.Controls.OfType<UserControl>().ToList();
                foreach (var ctrl in controlsToRemove)
                {
                    this.Controls.Remove(ctrl);
                    ctrl.Dispose();
                }

                uc.Tag = this;
                uc.Dock = DockStyle.Fill;
                uc.BackColor = Color.White;

                this.Controls.Add(uc);
                uc.BringToFront();

                currentControl = uc;
                this.Text = $"Automated Reactor Control - {GetUserControlDisplayName(controlName)}";

                this.Refresh();
                Logger.Log($"[Main_Form1] โหลด {controlName} ใน Form สำเร็จ", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] LoadUserControlDirectToForm: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private void CreateErrorDisplay(string errorMessage)
        {
            try
            {
                var errorLabel = new Label
                {
                    Text = $"❌ เกิดข้อผิดพลาด\n\n{errorMessage}\n\nกรุณาตรวจสอบ logs สำหรับรายละเอียดเพิ่มเติม",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.LightYellow,
                    ForeColor = Color.Red,
                    Font = new Font("Microsoft Sans Serif", 12, FontStyle.Bold),
                    BorderStyle = BorderStyle.FixedSingle
                };

                this.Controls.Clear();
                this.Controls.Add(errorLabel);
                this.Text = "Automated Reactor Control - Error";
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] CreateErrorDisplay: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region ✅ Multiple Panel System Implementation

        /// <summary>
        /// โหลด UserControl ด้วย Hybrid Panel System
        /// </summary>
        private void LoadUserControlWithMultiplePanel(UserControl uc, string controlType, Panel mainContainer)
        {
            lock (panelLock)
            {
                try
                {
                    // ตรวจสอบว่ามี Panel อยู่แล้วหรือไม่
                    if (activePanels.ContainsKey(controlType))
                    {
                        // ✅ มีอยู่แล้ว - แค่ Switch
                        SwitchToExistingPanel(controlType, mainContainer);
                    }
                    else
                    {
                        // ✅ ไม่มี - สร้างใหม่
                        CreateAndSwitchToNewPanel(uc, controlType, mainContainer);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[Panel][ERROR] LoadUserControlWithMultiplePanel: {ex.Message}", LogLevel.Error);
                    throw;
                }
            }
        }

        /// <summary>
        /// ✅ แก้ไข: Switch ไป Panel ที่มีอยู่แล้ว พร้อม Navigation State Reset
        /// </summary>
        private void SwitchToExistingPanel(string controlType, Panel mainContainer)
        {
            try
            {
                var targetPanel = activePanels[controlType];

                // ค้นหา panel ปัจจุบันที่แสดงอยู่
                Panel currentVisiblePanel = null;
                foreach (var kvp in activePanels)
                {
                    if (kvp.Value.Visible && kvp.Value != targetPanel)
                    {
                        currentVisiblePanel = kvp.Value;
                        break;
                    }
                }

                // ให้ target panel อยู่ด้านบน
                targetPanel.BringToFront();

                // 🚀 เพิ่มใหม่: Reset Navigation State ก่อน Animation
                ResetCachedControlNavigationState(controlType, targetPanel);

                // ✅ เริ่ม Slide Animation
                if (currentVisiblePanel != null && !isSliding)
                {
                    Logger.Log($"[NavFix] 🎬 Starting slide animation with state reset: {controlType}", LogLevel.Info);

                    StartSlideAnimation(currentVisiblePanel, targetPanel, mainContainer);

                    // 🚀 เพิ่มใหม่: ทำงานหลัง animation เสร็จ
                    EnsureAnimationCompleted(() => {
                        PostAnimationStateReset(controlType, targetPanel);
                    });
                }
                else
                {
                    // ไม่มี animation - แสดงทันทีพร้อม state reset
                    targetPanel.Visible = true;
                    targetPanel.Dock = DockStyle.Fill;

                    // ซ่อน panel อื่นๆ
                    foreach (var kvp in activePanels)
                    {
                        if (kvp.Value != targetPanel)
                        {
                            kvp.Value.Visible = false;
                        }
                    }

                    // 🚀 เพิ่มใหม่: Post-display state reset
                    PostAnimationStateReset(controlType, targetPanel);
                }

                Logger.Log($"[NavFix] ✅ Switch to existing panel with state reset: {controlType}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] 🚨 Error switching to existing panel: {ex.Message}", LogLevel.Error);
                throw;
            }
        }
        /// <summary>
        /// ✅ แก้ไข: สร้าง Panel ใหม่และ Switch พร้อม Enhanced State Management
        /// </summary>
        private void CreateAndSwitchToNewPanel(UserControl uc, string controlType, Panel mainContainer)
        {
            try
            {
                Logger.Log($"[NavFix] 🏗️ Creating new panel with enhanced state management: {controlType}", LogLevel.Info);

                // ค้นหา panel ปัจจุบันที่แสดงอยู่
                Panel currentVisiblePanel = null;
                foreach (var kvp in activePanels)
                {
                    if (kvp.Value.Visible)
                    {
                        currentVisiblePanel = kvp.Value;
                        break;
                    }
                }

                // สร้าง Panel ใหม่
                Panel newPanel = new Panel
                {
                    Name = $"panel_{controlType}_{DateTime.Now.Ticks}",
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Visible = false  // ซ่อนไว้ก่อน animation
                };

                // เตรียม UserControl
                uc.Tag = this;
                uc.Dock = DockStyle.Fill;
                uc.BackColor = Color.White;
                uc.Visible = true;

                // เพิ่ม UserControl ใน Panel
                newPanel.Controls.Add(uc);
                uc.BringToFront();

                // เพิ่ม Panel ใน Container
                mainContainer.SuspendLayout();
                mainContainer.Controls.Add(newPanel);
                newPanel.BringToFront();
                mainContainer.ResumeLayout(false);

                // เพิ่มลง Cache ก่อน animation
                activePanels[controlType] = newPanel;
                cachedControls[controlType] = uc;

                // 🚀 เพิ่มใหม่: Initial state setup สำหรับ UserControl ใหม่
                InitializeNewUserControlState(uc, controlType);

                // ✅ เริ่ม Slide Animation
                if (currentVisiblePanel != null && currentVisiblePanel != newPanel && !isSliding)
                {
                    Logger.Log("[NavFix] 🎬 Starting slide animation for new panel", LogLevel.Info);

                    StartSlideAnimation(currentVisiblePanel, newPanel, mainContainer);

                    // 🚀 เพิ่มใหม่: Post-animation setup
                    EnsureAnimationCompleted(() => {
                        PostAnimationNewPanelSetup(uc, controlType, newPanel);
                    });
                }
                else
                {
                    // ไม่มี animation - แสดงทันที
                    newPanel.Visible = true;
                    newPanel.Dock = DockStyle.Fill;

                    // ซ่อน panel อื่นๆ
                    if (currentVisiblePanel != null && currentVisiblePanel != newPanel)
                    {
                        currentVisiblePanel.Visible = false;
                    }

                    // Setup ทันที
                    PostAnimationNewPanelSetup(uc, controlType, newPanel);
                }

                // จัดการ Memory
                ManageMemory(controlType);

                Logger.Log($"[NavFix] ✅ New panel created with enhanced state management: {controlType}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[NavFix] 🚨 Error creating new panel: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// ✅ เพิ่มใหม่ - บังคับแสดง Controls ของ UC_CONTROL_SET_1
        /// </summary>
        private void ForceShowUC1Controls(UserControl uc)
        {
            try
            {
                Logger.Log("[ForceShow] เริ่มบังคับแสดง UC1 Controls", LogLevel.Debug);

                // ค้นหา sidebarPanel
                var sidebarPanel = uc.Controls.Find("sidebarPanel", true).FirstOrDefault();
                if (sidebarPanel != null)
                {
                    sidebarPanel.Visible = true;
                    sidebarPanel.BringToFront();
                    Logger.Log("[ForceShow] sidebarPanel แสดงแล้ว", LogLevel.Debug);

                    // แสดง controls ภายใน sidebarPanel
                    foreach (Control ctrl in sidebarPanel.Controls)
                    {
                        ctrl.Visible = true;
                        Logger.Log($"[ForceShow] แสดง {ctrl.Name}", LogLevel.Debug);
                    }
                }

                // ค้นหา mainContainer และปรับขนาด
                var mainContainer = uc.Controls.Find("mainContainer", true).FirstOrDefault();
                if (mainContainer != null)
                {
                    mainContainer.Width = 400; // ขยายออก
                    Logger.Log("[ForceShow] mainContainer ขยายเป็น 400", LogLevel.Debug);
                }

                // ค้นหา toggleSidebarButton และให้แสดงว่าขยายแล้ว
                var toggleButton = uc.Controls.Find("toggleSidebarButton", true).FirstOrDefault();
                if (toggleButton != null)
                {
                    // ใช้ reflection เพื่อตั้งค่า IsOn = true (ถ้าเป็น Switch)
                    var isOnProperty = toggleButton.GetType().GetProperty("IsOn");
                    if (isOnProperty != null)
                    {
                        isOnProperty.SetValue(toggleButton, true, null);
                        Logger.Log("[ForceShow] toggleButton set to expanded", LogLevel.Debug);
                    }
                }

                uc.Refresh();
                Logger.Log("[ForceShow] UC1 Controls บังคับแสดงเสร็จ", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ForceShow][ERROR] ForceShowUC1Controls: {ex.Message}", LogLevel.Error);
            }
        }
        /// <summary>
        /// จัดการ Memory ตาม Hybrid Strategy
        /// </summary>
        private void ManageMemory(string currentControlType)
        {
            try
            {
                var panelsToRemove = new List<string>();

                foreach (var kvp in activePanels)
                {
                    string controlType = kvp.Key;
                    Panel panel = kvp.Value;

                    // ข้าม panel ปัจจุบัน
                    if (controlType == currentControlType)
                        continue;

                    // ✅ Keep Alive - เก็บไว้เสมอ (UC_CONTROL_SET_1, UC_CONTROL_SET_2)
                    if (keepAliveControls.Contains(controlType))
                    {
                        Logger.Log($"[Memory] Keep Alive: {controlType}", LogLevel.Debug);
                        continue;
                    }

                    // ⏱️ Cacheable - เก็บไว้ถ้ายังไม่เยอะ (UC_PROGRAM_CONTROL_SET_1, UC_PROGRAM_CONTROL_SET_2)
                    if (cacheableControls.Contains(controlType))
                    {
                        if (activePanels.Count <= 4) // เก็บไว้ถ้าไม่เกิน 4 panels
                        {
                            Logger.Log($"[Memory] Cache: {controlType}", LogLevel.Debug);
                            continue;
                        }
                    }

                    // 🗑️ On Demand - ลบทิ้ง (UC_Setting, UC_Graph_Data_Set_1, UC_Graph_Data_Set_2)
                    Logger.Log($"[Memory] Remove: {controlType}", LogLevel.Debug);
                    panelsToRemove.Add(controlType);
                }

                // ลบ Panels ที่ไม่ต้องการ
                foreach (string controlType in panelsToRemove)
                {
                    RemovePanel(controlType);
                }

                Logger.Log($"[Memory] จัดการ Memory เสร็จ - เหลือ {activePanels.Count} panels", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Memory][ERROR] ManageMemory: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// ลบ Panel และ UserControl
        /// </summary>
        private void RemovePanel(string controlType)
        {
            try
            {
                if (activePanels.ContainsKey(controlType))
                {
                    var panel = activePanels[controlType];
                    var mainContainer = FindMainPanel();

                    if (mainContainer != null && mainContainer.Controls.Contains(panel))
                    {
                        mainContainer.Controls.Remove(panel);
                    }

                    panel.Dispose();
                    activePanels.Remove(controlType);
                }

                if (cachedControls.ContainsKey(controlType))
                {
                    cachedControls[controlType].Dispose();
                    cachedControls.Remove(controlType);
                }

                Logger.Log($"[Panel] ลบ panel: {controlType}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Panel][ERROR] RemovePanel: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// ✅ เพิ่มข้อมูล Slide Animation
        /// </summary>
        private string GetPanelSystemStatus()
        {
            try
            {
                var status = new StringBuilder();
                status.AppendLine("=== Panel System Status ===");
                status.AppendLine($"Active Panels: {activePanels.Count}");
                status.AppendLine($"Cached Controls: {cachedControls.Count}");

                // ✅ เพิ่มข้อมูล Animation
                status.AppendLine();
                status.AppendLine("=== Slide Animation Status ===");
                status.AppendLine($"Is Sliding: {isSliding}");
                status.AppendLine($"Slide Timer: {(slideTimer != null ? "Running" : "Stopped")}");
                status.AppendLine($"Slide Step: {slideStep}/{totalSlideSteps}");
                status.AppendLine($"Current Panel: {currentSlidingPanel?.Name ?? "NULL"}");
                status.AppendLine($"Target Panel: {targetSlidingPanel?.Name ?? "NULL"}");

                status.AppendLine();

                foreach (var kvp in activePanels)
                {
                    string controlType = kvp.Key;
                    Panel panel = kvp.Value;

                    string category = "On-Demand";
                    if (keepAliveControls.Contains(controlType))
                        category = "Keep-Alive";
                    else if (cacheableControls.Contains(controlType))
                        category = "Cacheable";

                    status.AppendLine($"• {controlType}: {category} - Visible: {panel.Visible}");
                }

                return status.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting panel status: {ex.Message}";
            }
        }

        #endregion

        #region Save/Load Operations

        private async Task SaveCurrentUserControlDataSafeAsync()
        {
            if (currentControl == null || isClosing || this.IsDisposed)
                return;

            try
            {
                string controlType = currentControl.GetType().Name;
                Logger.Log($"[Main_Form1] บันทึกข้อมูลจาก {controlType} แบบ Safe", LogLevel.Info);

                await Task.Run(() =>
                {
                    try
                    {
                        if (cancellationTokenSource?.Token.IsCancellationRequested == true)
                            return;

                        if (controlType.Contains("_1"))
                        {
                            SaveSet1DataSync(currentControl, controlType);
                        }
                        else if (controlType.Contains("_2"))
                        {
                            SaveSet2DataSync(currentControl, controlType);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        Logger.Log("[Main_Form1] Form disposed ขณะบันทึกข้อมูล", LogLevel.Debug);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[Main_Form1][ERROR] SaveCurrentUserControlDataSafeAsync: {ex.Message}", LogLevel.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SaveCurrentUserControlDataSafeAsync outer: {ex.Message}", LogLevel.Error);
            }
        }

        private void SaveSet1DataSync(UserControl control, string controlType)
        {
            try
            {
                if (control is UC_CONTROL_SET_1 cs1 && controlType == "UC_CONTROL_SET_1")
                {
                    SafeInvoke(() => cs1.SaveAllSetting_1());
                }
                else if (control is UC_PROGRAM_CONTROL_SET_1 ps1 && controlType == "UC_PROGRAM_CONTROL_SET_1")
                {
                    SaveProgramControlStateSync(ps1, "SaveState_1");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SaveSet1DataSync: {ex.Message}", LogLevel.Error);
            }
        }

        private void SaveSet2DataSync(UserControl control, string controlType)
        {
            try
            {
                if (control is UC_CONTROL_SET_2 cs2 && controlType == "UC_CONTROL_SET_2")
                {
                    SafeInvoke(() => cs2.SaveAllSetting_2());
                }
                else if (control is UC_PROGRAM_CONTROL_SET_2 ps2 && controlType == "UC_PROGRAM_CONTROL_SET_2")
                {
                    SaveProgramControlStateSync(ps2, "SaveState_2");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SaveSet2DataSync: {ex.Message}", LogLevel.Error);
            }
        }

        private void SaveProgramControlStateSync(UserControl control, string methodName)
        {
            try
            {
                var saveMethod = control.GetType().GetMethod(methodName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (saveMethod != null)
                {
                    SafeInvoke(() => saveMethod.Invoke(control, null));
                    Logger.Log($"[Main_Form1] เรียกใช้ {methodName} แบบ Sync สำเร็จ", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SaveProgramControlStateSync - {methodName}: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Form Closing

        private async void Main_Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isClosing) return;

            isClosing = true;

            try
            {
                Logger.Log("[Main_Form1] เริ่มการบันทึกข้อมูลก่อนปิดโปรแกรม", LogLevel.Info);

                cancellationTokenSource?.Cancel();
                await Task.Delay(500);

                Form progressForm = null;
                try
                {
                    progressForm = ShowSaveProgressDialog();
                }
                catch (Exception ex)
                {
                    Logger.Log($"[Main_Form1] ไม่สามารถแสดง Progress Dialog: {ex.Message}", LogLevel.Warn);
                }

                try
                {
                    SaveAllDataSync();
                    await SaveProgramStateAsync();

                    Logger.Log("[Main_Form1] บันทึกข้อมูลและปิดโปรแกรมสำเร็จ", LogLevel.Info);
                }
                finally
                {
                    progressForm?.Close();
                    progressForm?.Dispose();
                }

                CleanupResources();
            }
            catch (Exception ex)
            {
                errorHandler.HandleError(ex, "Main_Form1_FormClosing", ErrorSeverity.Critical);

                var result = MessageBox.Show(
                    $"เกิดข้อผิดพลาดขณะบันทึกข้อมูล:\n{ex.Message}\n\nต้องการปิดโปรแกรมต่อไปหรือไม่?",
                    "ข้อผิดพลาด", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    isClosing = false;
                }
            }
        }

        private void SaveAllDataSync()
        {
            try
            {
                Logger.Log("[Main_Form1] เริ่มบันทึกข้อมูลแบบ Sync", LogLevel.Info);

                if (currentControl != null && !this.IsDisposed)
                {
                    string controlType = currentControl.GetType().Name;
                    Logger.Log($"[Main_Form1] บันทึกข้อมูลจาก {controlType}", LogLevel.Info);

                    if (controlType.Contains("_1"))
                    {
                        SaveSet1DataSync(currentControl, controlType);
                    }
                    else if (controlType.Contains("_2"))
                    {
                        SaveSet2DataSync(currentControl, controlType);
                    }
                }

                if (!this.IsDisposed && controlSet1?.IsValueCreated == true)
                {
                    SaveControlSet1Sync();
                }

                if (!this.IsDisposed && controlSet2?.IsValueCreated == true)
                {
                    SaveControlSet2Sync();
                }

                Logger.Log("[Main_Form1] บันทึกข้อมูลแบบ Sync เสร็จสิ้น", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SaveAllDataSync: {ex.Message}", LogLevel.Error);
            }
        }

        private void SaveControlSet1Sync()
        {
            try
            {
                SafeInvoke(() => controlSet1.Value?.SaveBeforeFormClosing_1());

                if (Data_Set1.CurrentData_1 != null)
                {
                    configManager.SaveDataSet1Sync("Main_Form1_FormClosing");
                }

                Logger.Log("[Main_Form1] บันทึกข้อมูล Set1 Sync เสร็จสิ้น", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SaveControlSet1Sync: {ex.Message}", LogLevel.Error);
            }
        }

        private void SaveControlSet2Sync()
        {
            try
            {
                SafeInvoke(() => controlSet2.Value?.SaveBeforeFormClosing_2());

                if (Data_Set2.CurrentData_2 != null)
                {
                    configManager.SaveDataSet2Sync("Main_Form1_FormClosing");
                }

                Logger.Log("[Main_Form1] บันทึกข้อมูล Set2 Sync เสร็จสิ้น", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SaveControlSet2Sync: {ex.Message}", LogLevel.Error);
            }
        }

        private Form ShowSaveProgressDialog()
        {
            try
            {
                var progressForm = new Form
                {
                    Text = "กำลังบันทึกข้อมูล...",
                    Size = new Size(400, 120),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ShowInTaskbar = false
                };

                var progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    MarqueeAnimationSpeed = 30,
                    Dock = DockStyle.Top,
                    Height = 30
                };

                var label = new Label
                {
                    Text = "กรุณารอสักครู่...",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };

                progressForm.Controls.Add(label);
                progressForm.Controls.Add(progressBar);
                progressForm.Show(this);
                Application.DoEvents();

                return progressForm;
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] ShowSaveProgressDialog: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        private async Task SaveProgramStateAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    string programStateInfo = GenerateProgramStateInfo();
                    File.WriteAllText("ProgramStateAtExit.txt", programStateInfo);
                });

                Logger.Log("[Main_Form1] บันทึกสถานะโปรแกรมสำเร็จ", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] SaveProgramStateAsync: {ex.Message}", LogLevel.Error);
            }
        }

        private string GenerateProgramStateInfo()
        {
            try
            {
                return $"ปิดโปรแกรมเมื่อ: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                       $"------------- สถานะ Set1 -------------\n" +
                       $"SwitchA_M1: {ProgramState_1.SwitchA_M1}\n" +
                       $"IsTjMode1: {ProgramState_1.IsTjMode_1}\n" +
                       $"CurrentThermoSetpoint1: {ProgramState_1.CurrentThermoSetpoint_1}\n" +
                       $"StirrerSetpoint1: {ProgramState_1.StirrerSetpoint_1}\n\n" +
                       $"------------- สถานะ Set2 -------------\n" +
                       $"SwitchA_M2: {ProgramState_2.SwitchA_M2}\n" +
                       $"IsTjMode2: {ProgramState_2.IsTjMode_2}\n" +
                       $"CurrentThermoSetpoint2: {ProgramState_2.CurrentThermoSetpoint_2}\n" +
                       $"StirrerSetpoint2: {ProgramState_2.StirrerSetpoint_2}\n\n" +
                       $"------------- การตั้งค่าระบบ -------------\n" +
                       $"UserControls สร้างแล้ว: {GetCreatedControlsCount()}\n" +
                       $"หน้าปัจจุบัน: {currentControl?.GetType().Name ?? "ไม่มี"}\n" +
                       $"Active Panels: {activePanels.Count}";
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] GenerateProgramStateInfo: {ex.Message}", LogLevel.Error);
                return $"เกิดข้อผิดพลาดในการสร้างข้อมูลสถานะ: {ex.Message}";
            }
        }

        #endregion

        #region Event Handlers

        private void OnFileOperationCompleted(object sender, FileOperationEventArgs e)
        {
            try
            {
                Logger.Log($"[Main_Form1] การทำงานกับไฟล์เสร็จสิ้น: {e.FileName} - {e.Operation}", LogLevel.Info);
                SaveOperationCompleted?.Invoke(this, new SaveOperationEventArgs(e.FileName, true, null));
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] OnFileOperationCompleted: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnFileOperationFailed(object sender, FileOperationEventArgs e)
        {
            try
            {
                Logger.Log($"[Main_Form1][ERROR] การทำงานกับไฟล์ล้มเหลว: {e.FileName} - {e.Operation} - {e.Error}", LogLevel.Error);
                SaveOperationCompleted?.Invoke(this, new SaveOperationEventArgs(e.FileName, false, e.Error));

                MessageBox.Show($"เกิดข้อผิดพลาดในการทำงานกับไฟล์:\n{e.FileName}\n\nรายละเอียด: {e.Error}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] OnFileOperationFailed: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnUserControlLoaded(object sender, UserControlEventArgs e)
        {
            try
            {
                Logger.Log($"[Main_Form1] UserControl {e.ControlName} ถูกโหลดแล้ว", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] OnUserControlLoaded: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnUserControlUnloaded(object sender, UserControlEventArgs e)
        {
            try
            {
                Logger.Log($"[Main_Form1] UserControl {e.ControlName} ถูกยกเลิกการโหลดแล้ว", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] OnUserControlUnloaded: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Utility Methods

        private void CreateFallbackUI()
        {
            try
            {
                Logger.Log("[Main_Form1] สร้าง Fallback UI", LogLevel.Info);

                Panel mainPanel = FindMainPanel();
                Control container = mainPanel ?? (Control)this;

                container.Controls.Clear();

                var errorPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.LightGray,
                    Padding = new Padding(20)
                };

                var diagnosticInfo = GenerateDiagnosticInfo();

                var errorLabel = new Label
                {
                    Text = $"⚠️ AUTOMATED REACTOR CONTROL - Diagnostic Mode\n\n" +
                           $"สถานะ UserControls:\n{diagnosticInfo}\n\n" +
                           $"สถานะ Panel System:\n{GetPanelSystemStatus()}\n\n" +
                           $"กรุณาเลือกการดำเนินการ:",
                    AutoSize = false,
                    Size = new Size(500, 350),
                    TextAlign = ContentAlignment.TopLeft,
                    Font = new Font("Microsoft Sans Serif", 9, FontStyle.Regular),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };

                var buttonPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    AutoSize = true,
                    Location = new Point(20, errorLabel.Bottom + 20)
                };

                var testButtons = new[]
                {
                    new { Text = "🔥 Force Load CS1", Action = new Action(() => ForceLoadControlSet1()) },
                    new { Text = "Debug Info", Action = new Action(() => ShowDebugInfo()) },
                    new { Text = "Test ControlSet1", Action = new Action(() => TestUserControl("ControlSet1")) },
                    new { Text = "Test ControlSet2", Action = new Action(() => TestUserControl("ControlSet2")) },
                    new { Text = "Test ProgramSet1", Action = new Action(() => TestUserControl("ProgramSet1")) },
                    new { Text = "Test ProgramSet2", Action = new Action(() => TestUserControl("ProgramSet2")) },
                    new { Text = "Test Setting", Action = new Action(() => TestUserControl("Setting")) },
                    new { Text = "Force Refresh", Action = new Action(() => ForceRefreshUI()) },
                    new { Text = "Panel Status", Action = new Action(() => ShowPanelStatus()) }
                };

                foreach (var btn in testButtons)
                {
                    var button = new Button
                    {
                        Text = btn.Text,
                        Size = new Size(120, 30),
                        Margin = new Padding(5),
                        Font = new Font("Microsoft Sans Serif", 8, FontStyle.Regular)
                    };
                    button.Click += (s, e) => btn.Action();
                    buttonPanel.Controls.Add(button);
                }

                errorLabel.Location = new Point(20, 20);

                errorPanel.Resize += (s, e) =>
                {
                    errorLabel.Location = new Point(20, 20);
                    buttonPanel.Location = new Point(20, errorLabel.Bottom + 20);
                };

                errorPanel.Controls.Add(errorLabel);
                errorPanel.Controls.Add(buttonPanel);
                container.Controls.Add(errorPanel);

                this.Text = "Automated Reactor Control - Diagnostic Mode";
                Logger.Log("[Main_Form1] สร้าง Fallback UI สำเร็จ", LogLevel.Info);
            }
            catch (Exception ex)
            {
                errorHandler?.HandleError(ex, "CreateFallbackUI", ErrorSeverity.Critical);
                this.Text = "Automated Reactor Control - Critical Error";
                this.BackColor = Color.Red;

                try
                {
                    var label = new Label
                    {
                        Text = $"Critical Error:\n{ex.Message}",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.Red,
                        ForeColor = Color.White,
                        Font = new Font("Microsoft Sans Serif", 12, FontStyle.Bold)
                    };
                    this.Controls.Clear();
                    this.Controls.Add(label);
                }
                catch { }
            }
        }

        private void ShowDebugInfo()
        {
            try
            {
                var debugInfo = new StringBuilder();
                debugInfo.AppendLine("=== DEBUG INFO ===");
                debugInfo.AppendLine($"Form Handle Created: {this.IsHandleCreated}");
                debugInfo.AppendLine($"Form Disposed: {this.IsDisposed}");
                debugInfo.AppendLine($"Is Closing: {isClosing}");
                debugInfo.AppendLine($"Controls Count: {this.Controls.Count}");

                var foundPanel = FindMainPanel();
                if (foundPanel != null)
                {
                    debugInfo.AppendLine($"PanelMain Name: {foundPanel.Name}");
                    debugInfo.AppendLine($"PanelMain Size: {foundPanel.Size}");
                    debugInfo.AppendLine($"PanelMain Controls: {foundPanel.Controls.Count}");
                }
                else
                {
                    debugInfo.AppendLine("PanelMain: NULL");
                }

                debugInfo.AppendLine($"Current Control: {currentControl?.GetType().Name ?? "NULL"}");

                // ✅ เพิ่มข้อมูล Panel System
                debugInfo.AppendLine();
                debugInfo.AppendLine(GetPanelSystemStatus());

                MessageBox.Show(debugInfo.ToString(), "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Logger.Log($"[Main_Form1] {debugInfo.ToString()}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] ShowDebugInfo: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// ✅ แสดงสถานะ Panel System
        /// </summary>
        private void ShowPanelStatus()
        {
            try
            {
                var status = GetPanelSystemStatus();
                MessageBox.Show(status, "Panel System Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Panel Status Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ForceLoadControlSet1()
        {
            try
            {
                Logger.Log("[Main_Form1] บังคับโหลด ControlSet1", LogLevel.Info);

                var control = new UC_CONTROL_SET_1();
                LoadUserControl(control);

                Logger.Log("[Main_Form1] บังคับโหลด ControlSet1 สำเร็จ", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[Main_Form1][ERROR] ForceLoadControlSet1: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"ไม่สามารถบังคับโหลด ControlSet1 ได้: {ex.Message}",
                    "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerateDiagnosticInfo()
        {
            try
            {
                var info = new StringBuilder();

                info.AppendLine($"• Main Panel: {(FindMainPanel() != null ? "มีอยู่" : "ไม่มี")}");
                info.AppendLine($"• ControlSet1: {(controlSet1?.IsValueCreated == true ? "สร้างแล้ว" : "ยังไม่สร้าง")}");
                info.AppendLine($"• ControlSet2: {(controlSet2?.IsValueCreated == true ? "สร้างแล้ว" : "ยังไม่สร้าง")}");
                info.AppendLine($"• ProgramSet1: {(programSet1?.IsValueCreated == true ? "สร้างแล้ว" : "ยังไม่สร้าง")}");
                info.AppendLine($"• ProgramSet2: {(programSet2?.IsValueCreated == true ? "สร้างแล้ว" : "ยังไม่สร้าง")}");
                info.AppendLine($"• Setting: {(setting?.IsValueCreated == true ? "สร้างแล้ว" : "ยังไม่สร้าง")}");
                info.AppendLine($"• ConfigManager: {(configManager != null ? "มีอยู่" : "ไม่มี")}");
                info.AppendLine($"• Form Handle: {(this.IsHandleCreated ? "สร้างแล้ว" : "ยังไม่สร้าง")}");
                info.AppendLine($"• Panels Count: {this.Controls.OfType<Panel>().Count()}");

                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Error generating diagnostic: {ex.Message}";
            }
        }

        private void TestUserControl(string controlName)
        {
            try
            {
                Logger.Log($"[Main_Form1] ทดสอบ {controlName}", LogLevel.Info);

                UserControl control = null;

                switch (controlName)
                {
                    case "ControlSet1":
                        control = controlSet1?.Value;
                        break;
                    case "ControlSet2":
                        control = controlSet2?.Value;
                        break;
                    case "ProgramSet1":
                        control = programSet1?.Value;
                        break;
                    case "ProgramSet2":
                        control = programSet2?.Value;
                        break;
                    case "Setting":
                        control = setting?.Value;
                        break;
                }

                if (control != null)
                {
                    LoadUserControl(control);  // ✅ ใช้ Multiple Panel System
                    MessageBox.Show($"โหลด {controlName} สำเร็จ!", "ทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"ไม่สามารถสร้าง {controlName} ได้", "ทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing {controlName}: {ex.Message}", "ทดสอบ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ForceRefreshUI()
        {
            try
            {
                Logger.Log("[Main_Form1] Force Refresh UI", LogLevel.Info);

                // ✅ ทำความสะอาด Panel System
                lock (panelLock)
                {
                    foreach (var panel in activePanels.Values)
                    {
                        panel?.Dispose();
                    }
                    activePanels.Clear();

                    foreach (var control in cachedControls.Values)
                    {
                        control?.Dispose();
                    }
                    cachedControls.Clear();
                }

                Panel mainPanel = FindMainPanel();
                if (mainPanel != null)
                {
                    mainPanel.Controls.Clear();
                }
                else
                {
                    this.Controls.Clear();
                }

                InitializeLazyUserControls();
                LoadDefaultUserControl();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing UI: {ex.Message}", "Refresh", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetUserControlDisplayName(string controlName)
        {
            switch (controlName)
            {
                case "UC_CONTROL_SET_1":
                    return "Control Set 1";
                case "UC_CONTROL_SET_2":
                    return "Control Set 2";
                case "UC_PROGRAM_CONTROL_SET_1":
                    return "Program Control Set 1";
                case "UC_PROGRAM_CONTROL_SET_2":
                    return "Program Control Set 2";
                case "UC_Setting":
                    return "Settings";
                case "UC_Graph_Data_Set_1":
                    return "Graph Data Set 1";
                case "UC_Graph_Data_Set_2":
                    return "Graph Data Set 2";
                default:
                    return controlName.Replace("UC_", "").Replace("_", " ");
            }
        }

        private int GetCreatedControlsCount()
        {
            int count = 0;
            if (controlSet1?.IsValueCreated == true) count++;
            if (controlSet2?.IsValueCreated == true) count++;
            if (programSet1?.IsValueCreated == true) count++;
            if (programSet2?.IsValueCreated == true) count++;
            if (setting?.IsValueCreated == true) count++;
            if (graphDataSet1?.IsValueCreated == true) count++;
            if (graphDataSet2?.IsValueCreated == true) count++;
            return count;
        }

        private void HandleCriticalError(Exception ex, string context)
        {
            string errorMsg = $"{context}: {ex.Message}";
            Debug.WriteLine($"[Main_Form1][CRITICAL ERROR] {errorMsg}");

            try
            {
                Logger.Log($"[Main_Form1][CRITICAL ERROR] {errorMsg}", LogLevel.Error);
                Logger.Log($"[Main_Form1][STACK TRACE] {ex.StackTrace}", LogLevel.Error);
            }
            catch { }

            MessageBox.Show($"{errorMsg}\n\nรายละเอียด: {ex.StackTrace}",
                "ข้อผิดพลาดร้ายแรง", MessageBoxButtons.OK, MessageBoxIcon.Error);

            CreateFallbackUI();
        }

        #endregion

        #region Public Navigation Methods

        public void NavigateToControlSet1()
        {
            try
            {
                if (controlSet1 != null)
                {
                    LoadUserControl(controlSet1.Value);
                }
            }
            catch (Exception ex)
            {
                errorHandler.HandleError(ex, "NavigateToControlSet1", ErrorSeverity.Medium);
            }
        }

        public void NavigateToControlSet2()
        {
            try
            {
                if (controlSet2 != null)
                {
                    LoadUserControl(controlSet2.Value);
                }
            }
            catch (Exception ex)
            {
                errorHandler.HandleError(ex, "NavigateToControlSet2", ErrorSeverity.Medium);
            }
        }

        public void NavigateToProgramSet1()
        {
            try
            {
                if (programSet1 != null)
                {
                    LoadUserControl(programSet1.Value);
                }
            }
            catch (Exception ex)
            {
                errorHandler.HandleError(ex, "NavigateToProgramSet1", ErrorSeverity.Medium);
            }
        }

        public void NavigateToProgramSet2()
        {
            try
            {
                if (programSet2 != null)
                {
                    LoadUserControl(programSet2.Value);
                }
            }
            catch (Exception ex)
            {
                errorHandler.HandleError(ex, "NavigateToProgramSet2", ErrorSeverity.Medium);
            }
        }

        public void NavigateToSetting()
        {
            try
            {
                if (setting != null)
                {
                    LoadUserControl(setting.Value);
                }
            }
            catch (Exception ex)
            {
                errorHandler.HandleError(ex, "NavigateToSetting", ErrorSeverity.Medium);
            }
        }

        public void NavigateToGraphDataSet1()
        {
            try
            {
                if (graphDataSet1 != null)
                {
                    LoadUserControl(graphDataSet1.Value);
                }
            }
            catch (Exception ex)
            {
                errorHandler.HandleError(ex, "NavigateToGraphDataSet1", ErrorSeverity.Medium);
            }
        }

        public void NavigateToGraphDataSet2()
        {
            try
            {
                if (graphDataSet2 != null)
                {
                    LoadUserControl(graphDataSet2.Value);
                }
            }
            catch (Exception ex)
            {
                errorHandler.HandleError(ex, "NavigateToGraphDataSet2", ErrorSeverity.Medium);
            }
        }

        #endregion

        #region Public Accessors

        public UC_CONTROL_SET_1 GetControlSet1() => controlSet1?.IsValueCreated == true ? controlSet1.Value : null;
        public UC_CONTROL_SET_2 GetControlSet2() => controlSet2?.IsValueCreated == true ? controlSet2.Value : null;
        public UC_PROGRAM_CONTROL_SET_1 GetProgramSet1() => programSet1?.IsValueCreated == true ? programSet1.Value : null;
        public UC_PROGRAM_CONTROL_SET_2 GetProgramSet2() => programSet2?.IsValueCreated == true ? programSet2.Value : null;
        public UC_Setting GetSetting() => setting?.IsValueCreated == true ? setting.Value : null;
        public UC_Graph_Data_Set_1 GetGraphDataSet1() => graphDataSet1?.IsValueCreated == true ? graphDataSet1.Value : null;
        public UC_Graph_Data_Set_2 GetGraphDataSet2() => graphDataSet2?.IsValueCreated == true ? graphDataSet2.Value : null;

        public ConfigurationManager ConfigManager => configManager;
        public UserControlManager UserControlManager => userControlManager;

        #endregion

        #region Cleanup Methods

        private void CleanupResources()
        {
            try
            {
                // ✅ หยุด Slide Animation (แทน Fade Animation)
                StopSlideAnimation();

                // ทำความสะอาด Multiple Panel System
                lock (panelLock)
                {
                    foreach (var panel in activePanels.Values)
                    {
                        panel?.Dispose();
                    }
                    activePanels.Clear();

                    foreach (var control in cachedControls.Values)
                    {
                        control?.Dispose();
                    }
                    cachedControls.Clear();
                }

                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();

                configManager?.Dispose();
                userControlManager?.Dispose();
                errorHandler?.Dispose();

                if (controlSet1?.IsValueCreated == true) controlSet1.Value?.Dispose();
                if (controlSet2?.IsValueCreated == true) controlSet2.Value?.Dispose();
                if (programSet1?.IsValueCreated == true) programSet1.Value?.Dispose();
                if (programSet2?.IsValueCreated == true) programSet2.Value?.Dispose();
                if (setting?.IsValueCreated == true) setting.Value?.Dispose();
                if (graphDataSet1?.IsValueCreated == true) graphDataSet1.Value?.Dispose();
                if (graphDataSet2?.IsValueCreated == true) graphDataSet2.Value?.Dispose();

                Logger.Log("[Main_Form1] ทำความสะอาดทรัพยากรเสร็จสิ้น (รวม Slide Animation)", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Main_Form1][ERROR] CleanupResources: {ex.Message}");
            }
        }

        #endregion
    }
}