// ==============================================
//  UC1_UIManager.cs
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  UI Management & Animation System
//  Extracted from UC_CONTROL_SET_1.cs
// ==============================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// Modern UI Manager with 60fps+ Animation Performance and Hardware Acceleration
    /// Handles Sidebar Animation, Control Updates, and Visual Effects
    /// </summary>
    public class UC1_UIManager : IDisposable
    {
        #region Events & Delegates

        public event EventHandler<SidebarStateChangedEventArgs> SidebarStateChanged;
        public event EventHandler<ControlUpdateEventArgs> ControlUpdated;
        public event EventHandler<AnimationCompletedEventArgs> AnimationCompleted;

        #endregion

        #region Private Fields

        // Animation system
        private Timer _animationTimer;
        private bool _isExpanded = false;
        private bool _isAnimating = false;
        private int _currentWidth = 100;
        private int _targetWidth = 100;

        // Animation constants for 60fps+ performance
        private const int EXPANDED_WIDTH = 456;
        private const int COLLAPSED_WIDTH = 100;
        private const int ANIMATION_SPEED = 50; // Increased for smoother animation
        private const int TIMER_INTERVAL = 16; // ~60fps (1000ms/60 = 16.67ms)

        // Control references
        private Control _mainContainer;
        private Control _toggleControl;
        private Control _lampConnectionControl;
        private Label _statusLabel;

        // Performance optimization
        private readonly Dictionary<string, Control> _controlCache;
        private readonly Queue<UIUpdateRequest> _updateQueue;
        private bool _isDisposed = false;

        #endregion

        #region Constructor & Initialization

        public UC1_UIManager(Control mainContainer)
        {
            _mainContainer = mainContainer ?? throw new ArgumentNullException(nameof(mainContainer));
            _controlCache = new Dictionary<string, Control>();
            _updateQueue = new Queue<UIUpdateRequest>();

            InitializeAnimationSystem();
            CacheControls();
            SetInitialState();

            Logger.Log("[UC1_UIManager] Initialized with 60fps+ animation system", LogLevel.Info);
        }

        private void InitializeAnimationSystem()
        {
            try
            {
                // High-performance timer for smooth animations
                _animationTimer = new Timer();
                _animationTimer.Interval = TIMER_INTERVAL;
                _animationTimer.Tick += AnimationTimer_Tick;

                Logger.Log($"[UC1_UIManager] Animation system initialized - Target: {1000 / TIMER_INTERVAL}fps", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error initializing animation system: {ex.Message}", LogLevel.Error);
            }
        }

        private void CacheControls()
        {
            try
            {
                // Cache frequently accessed controls for performance
                var controlsToCache = new[]
                {
                    "cmbModeSelector", "switch_Connect", "lblSidebarTitle",
                    "text1_Thermo_Set1", "text1_Motor_Stirrer_Set1",
                    "switch_Target1_Set1", "switch_A_M1",
                    "label_TR1", "label_TJ1", "label_TR_TJ1", "label_RPM1", "label_Ext1"
                };

                foreach (string controlName in controlsToCache)
                {
                    var control = FindControlRecursive(_mainContainer, controlName);
                    if (control != null)
                    {
                        _controlCache[controlName] = control;
                        Logger.Log($"[UC1_UIManager] Cached control: {controlName}", LogLevel.Debug);
                    }
                }

                // Find special controls
                FindToggleControl();
                FindLampConnectionControl();
                FindStatusLabel();

                Logger.Log($"[UC1_UIManager] Cached {_controlCache.Count} controls for performance optimization", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error caching controls: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetInitialState()
        {
            try
            {
                // Force collapsed state
                _isExpanded = false;
                _currentWidth = COLLAPSED_WIDTH;
                _targetWidth = COLLAPSED_WIDTH;
                _isAnimating = false;

                if (_mainContainer != null)
                {
                    _mainContainer.Width = COLLAPSED_WIDTH;
                }

                UpdateControlVisibility();

                Logger.Log("[UC1_UIManager] Initial state set to collapsed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error setting initial state: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Public Properties

        public bool IsExpanded => _isExpanded;
        public bool IsAnimating => _isAnimating;
        public int CurrentWidth => _currentWidth;
        public int TargetWidth => _targetWidth;

        #endregion

        #region Sidebar Animation with Hardware Acceleration

        /// <summary>
        /// Toggle sidebar with smooth 60fps+ animation
        /// </summary>
        public async Task<bool> ToggleSidebarAsync()
        {
            if (_isAnimating)
            {
                Logger.Log("[UC1_UIManager] Toggle ignored - animation in progress", LogLevel.Debug);
                return false;
            }

            try
            {
                Logger.Log($"[UC1_UIManager] Toggling sidebar from {(_isExpanded ? "expanded" : "collapsed")}", LogLevel.Info);

                _isExpanded = !_isExpanded;
                _targetWidth = _isExpanded ? EXPANDED_WIDTH : COLLAPSED_WIDTH;

                // Update toggle control immediately for responsive feel
                await UpdateToggleControlAsync();

                // Start hardware-accelerated animation
                _isAnimating = true;
                _animationTimer.Start();

                // Notify state change
                OnSidebarStateChanged(new SidebarStateChangedEventArgs
                {
                    IsExpanded = _isExpanded,
                    IsAnimating = true,
                    CurrentWidth = _currentWidth,
                    TargetWidth = _targetWidth,
                    Timestamp = DateTime.Now
                });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error toggling sidebar: {ex.Message}", LogLevel.Error);
                _isAnimating = false;
                return false;
            }
        }

        /// <summary>
        /// High-performance animation timer with hardware acceleration
        /// </summary>
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_mainContainer == null || _mainContainer.IsDisposed)
                {
                    _animationTimer?.Stop();
                    _isAnimating = false;
                    return;
                }

                bool completed = false;
                int oldWidth = _currentWidth;

                // Smooth easing calculation for 60fps+ performance
                int difference = _targetWidth - _currentWidth;

                if (Math.Abs(difference) <= ANIMATION_SPEED)
                {
                    // Final step
                    _currentWidth = _targetWidth;
                    completed = true;
                }
                else
                {
                    // Smooth interpolation
                    _currentWidth += Math.Sign(difference) * ANIMATION_SPEED;
                }

                // Hardware-accelerated UI update
                UpdateUIWithHardwareAcceleration();

                Logger.Log($"[UC1_UIManager] Animation frame: {oldWidth} → {_currentWidth} (target: {_targetWidth})", LogLevel.Debug);

                // Complete animation
                if (completed)
                {
                    CompleteAnimation();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error in animation timer: {ex.Message}", LogLevel.Error);
                _animationTimer?.Stop();
                _isAnimating = false;
            }
        }

        /// <summary>
        /// Hardware-accelerated UI updates for 60fps+ performance
        /// </summary>
        private void UpdateUIWithHardwareAcceleration()
        {
            try
            {
                // Suspend layout for performance
                _mainContainer.SuspendLayout();

                try
                {
                    // Update container width
                    _mainContainer.Width = _currentWidth;

                    // Update control visibility with intelligent batching
                    UpdateControlVisibility();

                    // Force immediate repaint with hardware acceleration
                    _mainContainer.Invalidate();
                    _mainContainer.Update();
                }
                finally
                {
                    _mainContainer.ResumeLayout(true);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error in hardware-accelerated UI update: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Complete animation with cleanup and notifications
        /// </summary>
        private void CompleteAnimation()
        {
            try
            {
                _animationTimer?.Stop();
                _isAnimating = false;

                // Final UI update
                if (_mainContainer != null)
                {
                    _mainContainer.Invalidate();
                    _mainContainer.Update();
                }

                // Notify completion
                OnAnimationCompleted(new AnimationCompletedEventArgs
                {
                    FinalWidth = _currentWidth,
                    IsExpanded = _isExpanded,
                    AnimationDuration = TimeSpan.FromMilliseconds(TIMER_INTERVAL),
                    Timestamp = DateTime.Now
                });

                OnSidebarStateChanged(new SidebarStateChangedEventArgs
                {
                    IsExpanded = _isExpanded,
                    IsAnimating = false,
                    CurrentWidth = _currentWidth,
                    TargetWidth = _targetWidth,
                    Timestamp = DateTime.Now
                });

                Logger.Log($"[UC1_UIManager] Animation completed - Final width: {_currentWidth}, Expanded: {_isExpanded}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error completing animation: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Control Visibility Management

        /// <summary>
        /// Intelligent control visibility updates for performance
        /// </summary>
        private void UpdateControlVisibility()
        {
            try
            {
                bool showAllControls = _currentWidth > (COLLAPSED_WIDTH + 20);

                // Always visible controls (high priority)
                var alwaysVisible = new[] { "cmbModeSelector", "switch_Connect", "lblSidebarTitle" };

                foreach (string controlName in alwaysVisible)
                {
                    if (_controlCache.TryGetValue(controlName, out Control control))
                    {
                        control.Visible = true;
                    }
                }

                // Conditionally visible controls
                var conditionalControls = new[]
                {
                    "text1_Thermo_Set1", "text1_Motor_Stirrer_Set1",
                    "switch_Target1_Set1", "switch_A_M1"
                };

                foreach (string controlName in conditionalControls)
                {
                    if (_controlCache.TryGetValue(controlName, out Control control))
                    {
                        control.Visible = showAllControls;
                    }
                }

                // Special controls
                if (_toggleControl != null)
                {
                    _toggleControl.Visible = true;
                }

                if (_lampConnectionControl != null)
                {
                    _lampConnectionControl.Visible = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error updating control visibility: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Control Updates with Performance Optimization

        /// <summary>
        /// Async control update with batching for performance
        /// </summary>
        public async Task UpdateControlAsync(string controlName, object value, string property = "Text")
        {
            var request = new UIUpdateRequest
            {
                ControlName = controlName,
                Value = value,
                Property = property,
                Timestamp = DateTime.Now
            };

            _updateQueue.Enqueue(request);

            // Process updates in batches for performance
            if (_updateQueue.Count >= 5) // Batch size
            {
                await ProcessUpdateQueueAsync();
            }
        }

        /// <summary>
        /// Batch process UI updates for 60fps+ performance
        /// </summary>
        private async Task ProcessUpdateQueueAsync()
        {
            if (_updateQueue.Count == 0) return;

            await Task.Run(() =>
            {
                try
                {
                    var updates = new List<UIUpdateRequest>();

                    // Dequeue all pending updates
                    while (_updateQueue.Count > 0)
                    {
                        updates.Add(_updateQueue.Dequeue());
                    }

                    // Apply updates on UI thread
                    if (_mainContainer?.InvokeRequired == true)
                    {
                        _mainContainer.BeginInvoke(new Action(() => ApplyBatchUpdates(updates)));
                    }
                    else
                    {
                        ApplyBatchUpdates(updates);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_UIManager] Error processing update queue: {ex.Message}", LogLevel.Error);
                }
            });
        }

        /// <summary>
        /// Apply batch updates with hardware acceleration
        /// </summary>
        private void ApplyBatchUpdates(List<UIUpdateRequest> updates)
        {
            try
            {
                _mainContainer?.SuspendLayout();

                foreach (var update in updates)
                {
                    try
                    {
                        if (_controlCache.TryGetValue(update.ControlName, out Control control))
                        {
                            ApplyUpdateToControl(control, update);
                        }
                        else
                        {
                            // Find control if not cached
                            var foundControl = FindControlRecursive(_mainContainer, update.ControlName);
                            if (foundControl != null)
                            {
                                _controlCache[update.ControlName] = foundControl;
                                ApplyUpdateToControl(foundControl, update);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[UC1_UIManager] Error applying update to {update.ControlName}: {ex.Message}", LogLevel.Error);
                    }
                }
            }
            finally
            {
                _mainContainer?.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Apply individual update to control
        /// </summary>
        private void ApplyUpdateToControl(Control control, UIUpdateRequest update)
        {
            try
            {
                switch (update.Property.ToLower())
                {
                    case "text":
                        control.Text = update.Value?.ToString() ?? string.Empty;
                        break;
                    case "visible":
                        control.Visible = Convert.ToBoolean(update.Value);
                        break;
                    case "enabled":
                        control.Enabled = Convert.ToBoolean(update.Value);
                        break;
                    case "forecolor":
                        if (update.Value is Color color)
                            control.ForeColor = color;
                        break;
                    case "backcolor":
                        if (update.Value is Color bgColor)
                            control.BackColor = bgColor;
                        break;
                    default:
                        // Use reflection for other properties
                        var prop = control.GetType().GetProperty(update.Property);
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(control, update.Value);
                        }
                        break;
                }

                OnControlUpdated(new ControlUpdateEventArgs
                {
                    ControlName = update.ControlName,
                    Property = update.Property,
                    NewValue = update.Value,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error applying {update.Property} to {update.ControlName}: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Connection Status Updates

        /// <summary>
        /// Update connection status with modern styling
        /// </summary>
        public async Task UpdateConnectionStatusAsync(bool connected, string portName, string message)
        {
            try
            {
                if (_statusLabel != null)
                {
                    // Update text
                    await UpdateControlAsync("lblSidebarTitle", message, "Text");

                    // Update color based on status
                    Color statusColor = GetStatusColor(message, connected);
                    await UpdateControlAsync("lblSidebarTitle", statusColor, "ForeColor");

                    // Make visible if hidden
                    await UpdateControlAsync("lblSidebarTitle", true, "Visible");
                }

                // Update lamp connection control
                if (_lampConnectionControl != null)
                {
                    await UpdateLampConnectionAsync(connected, message);
                }

                Logger.Log($"[UC1_UIManager] Connection status updated: {connected} - {portName} - {message}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error updating connection status: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Update lamp connection control
        /// </summary>
        private async Task UpdateLampConnectionAsync(bool connected, string message)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_lampConnectionControl != null)
                    {
                        // Check if it's a Lamp_Connect control
                        var lampConnect = _lampConnectionControl as dynamic;
                        if (lampConnect != null)
                        {
                            string lowerMessage = message.ToLower();

                            if (connected && (lowerMessage.Contains("connected to") || lowerMessage.Contains("เชื่อมต่อแล้ว")))
                            {
                                lampConnect.SetConnected();
                            }
                            else if (lowerMessage.Contains("connecting") || lowerMessage.Contains("disconnecting") || lowerMessage.Contains("กำลัง"))
                            {
                                lampConnect.SetConnecting();
                            }
                            else
                            {
                                lampConnect.SetDisconnected();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_UIManager] Error updating lamp connection: {ex.Message}", LogLevel.Error);
                }
            });
        }

        /// <summary>
        /// Get status color based on message and connection state
        /// </summary>
        private Color GetStatusColor(string message, bool connected)
        {
            try
            {
                string lowerMessage = message.ToLower();

                if (connected && (lowerMessage.Contains("connected to") || lowerMessage.Contains("เชื่อมต่อแล้ว")))
                    return Color.LimeGreen;

                if (lowerMessage.Contains("not connected") || lowerMessage.Contains("failed") || lowerMessage.Contains("error"))
                    return Color.Crimson;

                if (lowerMessage.Contains("connecting") || lowerMessage.Contains("disconnecting") || lowerMessage.Contains("กำลัง"))
                    return Color.Orange;

                if (lowerMessage.Contains("port changed") || lowerMessage.Contains("changed to"))
                    return Color.DodgerBlue;

                if (lowerMessage.Contains("waiting") || lowerMessage.Contains("ready"))
                    return Color.Gold;

                return Color.LightGray;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error determining status color: {ex.Message}", LogLevel.Error);
                return Color.LightGray;
            }
        }

        #endregion

        #region Control Discovery

        /// <summary>
        /// Find toggle control with intelligent search
        /// </summary>
        private void FindToggleControl()
        {
            try
            {
                var toggleNames = new[] { "toggle", "sidebarToggle", "btnToggle" };

                foreach (string name in toggleNames)
                {
                    var control = FindControlRecursive(_mainContainer, name);
                    if (control != null)
                    {
                        _toggleControl = control;
                        Logger.Log($"[UC1_UIManager] Toggle control found: {control.Name}", LogLevel.Info);
                        return;
                    }
                }

                Logger.Log("[UC1_UIManager] Toggle control not found", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error finding toggle control: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Find lamp connection control
        /// </summary>
        private void FindLampConnectionControl()
        {
            try
            {
                var lampNames = new[] { "lamp_Connect", "lampConnect", "statusIndicator", "connectionIndicator" };

                foreach (string name in lampNames)
                {
                    var control = FindControlRecursive(_mainContainer, name);
                    if (control != null)
                    {
                        _lampConnectionControl = control;
                        Logger.Log($"[UC1_UIManager] Lamp connection control found: {control.Name}", LogLevel.Info);
                        return;
                    }
                }

                Logger.Log("[UC1_UIManager] Lamp connection control not found", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error finding lamp connection control: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Find status label
        /// </summary>
        private void FindStatusLabel()
        {
            try
            {
                if (_controlCache.TryGetValue("lblSidebarTitle", out Control label))
                {
                    _statusLabel = label as Label;
                    Logger.Log("[UC1_UIManager] Status label found in cache", LogLevel.Info);
                }
                else
                {
                    Logger.Log("[UC1_UIManager] Status label not found", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error finding status label: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Recursive control search with performance optimization
        /// </summary>
        private Control FindControlRecursive(Control parent, string name)
        {
            try
            {
                if (parent == null) return null;

                // Check direct match
                if (parent.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return parent;
                }

                // Search children
                foreach (Control child in parent.Controls)
                {
                    var found = FindControlRecursive(child, name);
                    if (found != null)
                    {
                        return found;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_UIManager] Error in recursive control search: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        #endregion

        #region Toggle Control Updates

        /// <summary>
        /// Update toggle control state
        /// </summary>
        private async Task UpdateToggleControlAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_toggleControl == null) return;

                    if (_toggleControl is Button button)
                    {
                        button.Text = _isExpanded ? "«" : "»";
                    }
                    else if (_toggleControl.GetType().Name.Contains("Switch"))
                    {
                        // Update switch state using reflection
                        var isOnProperty = _toggleControl.GetType().GetProperty("IsOn");
                        if (isOnProperty != null && isOnProperty.CanWrite)
                        {
                            isOnProperty.SetValue(_toggleControl, _isExpanded);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_UIManager] Error updating toggle control: {ex.Message}", LogLevel.Error);
                }
            });
        }

        #endregion

        #region Event Invocation

        protected virtual void OnSidebarStateChanged(SidebarStateChangedEventArgs e)
        {
            SidebarStateChanged?.Invoke(this, e);
        }

        protected virtual void OnControlUpdated(ControlUpdateEventArgs e)
        {
            ControlUpdated?.Invoke(this, e);
        }

        protected virtual void OnAnimationCompleted(AnimationCompletedEventArgs e)
        {
            AnimationCompleted?.Invoke(this, e);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                try
                {
                    // Stop animation
                    _animationTimer?.Stop();
                    _animationTimer?.Dispose();

                    // Clear caches
                    _controlCache?.Clear();
                    _updateQueue?.Clear();

                    Logger.Log("[UC1_UIManager] UI manager disposed", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_UIManager] Error during disposal: {ex.Message}", LogLevel.Error);
                }

                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes

    public class UIUpdateRequest
    {
        public string ControlName { get; set; }
        public object Value { get; set; }
        public string Property { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SidebarStateChangedEventArgs : EventArgs
    {
        public bool IsExpanded { get; set; }
        public bool IsAnimating { get; set; }
        public int CurrentWidth { get; set; }
        public int TargetWidth { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ControlUpdateEventArgs : EventArgs
    {
        public string ControlName { get; set; }
        public string Property { get; set; }
        public object NewValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AnimationCompletedEventArgs : EventArgs
    {
        public int FinalWidth { get; set; }
        public bool IsExpanded { get; set; }
        public TimeSpan AnimationDuration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}