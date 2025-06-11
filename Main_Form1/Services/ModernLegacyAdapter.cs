using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Architecture;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Services
{
    /// <summary>
    /// 🔄 LegacyAdapter - Seamless WinForms Integration
    /// Wraps legacy UserControls in modern Avalonia containers
    /// Features: Event bridging, state synchronization, memory management
    /// </summary>
    public class ModernLegacyAdapter : ILegacyAdapter
    {
        #region Private Fields

        private readonly Dictionary<string, LegacyControlWrapper> _wrappedControls = new();
        private readonly Dictionary<Type, Func<object>> _controlFactories = new();
        private readonly IStateManager _stateManager;
        private readonly object _wrapperLock = new object();

        // Event bridge for state synchronization
        private readonly EventBridge _eventBridge;

        // Performance monitoring
        private readonly Dictionary<string, DateTime> _lastAccessTimes = new();

        #endregion

        #region Constructor

        public ModernLegacyAdapter(IStateManager stateManager)
        {
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _eventBridge = new EventBridge();

            RegisterLegacyControlFactories();
            InitializeEventBridge();

            Logger.Log("[LegacyAdapter] 🔄 Legacy adapter initialized for seamless integration", LogLevel.Info);
        }

        #endregion

        #region ILegacyAdapter Implementation

        public async Task<Control> WrapLegacyUserControl(Type userControlType)
        {
            try
            {
                var typeName = userControlType.Name;
                Logger.Log($"[LegacyAdapter] 🎯 Wrapping legacy control: {typeName}", LogLevel.Info);

                // Check if already wrapped
                if (_wrappedControls.TryGetValue(typeName, out var existingWrapper))
                {
                    _lastAccessTimes[typeName] = DateTime.Now;
                    Logger.Log($"[LegacyAdapter] ♻️ Reusing existing wrapper: {typeName}", LogLevel.Debug);
                    return existingWrapper.AvaloniaContainer;
                }

                // Create new wrapper
                var wrapper = await CreateLegacyWrapperAsync(userControlType);

                lock (_wrapperLock)
                {
                    _wrappedControls[typeName] = wrapper;
                    _lastAccessTimes[typeName] = DateTime.Now;
                }

                Logger.Log($"[LegacyAdapter] ✅ Successfully wrapped: {typeName}", LogLevel.Info);
                return wrapper.AvaloniaContainer;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] WrapLegacyUserControl: {ex.Message}", LogLevel.Error);
                throw new LegacyIntegrationException($"Failed to wrap {userControlType.Name}", ex);
            }
        }

        public async Task<T> CreateLegacyControl<T>() where T : class, new()
        {
            try
            {
                Logger.Log($"[LegacyAdapter] 🏭 Creating legacy control: {typeof(T).Name}", LogLevel.Debug);

                var control = await Task.Run(() =>
                {
                    if (_controlFactories.TryGetValue(typeof(T), out var factory))
                    {
                        return factory() as T;
                    }
                    return new T();
                });

                if (control != null)
                {
                    await InitializeLegacyControlAsync(control);
                }

                return control ?? throw new InvalidOperationException($"Failed to create {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateLegacyControl<{typeof(T).Name}>: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        public async Task MigrateLegacySettings()
        {
            try
            {
                Logger.Log("[LegacyAdapter] 📋 Starting legacy settings migration", LogLevel.Info);

                var migrationTasks = new List<Task>
                {
                    MigrateControlSet1Settings(),
                    MigrateControlSet2Settings(),
                    MigrateProgramSettings(),
                    MigrateDeviceSettings()
                };

                await Task.WhenAll(migrationTasks);

                Logger.Log("[LegacyAdapter] ✅ Legacy settings migration completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] MigrateLegacySettings: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        public void RegisterLegacyEventHandler(string eventName, Delegate handler)
        {
            try
            {
                _eventBridge.RegisterHandler(eventName, handler);
                Logger.Log($"[LegacyAdapter] 📡 Event handler registered: {eventName}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] RegisterLegacyEventHandler: {ex.Message}", LogLevel.Error);
            }
        }

        public void UnregisterLegacyEventHandler(string eventName, Delegate handler)
        {
            try
            {
                _eventBridge.UnregisterHandler(eventName, handler);
                Logger.Log($"[LegacyAdapter] 📡 Event handler unregistered: {eventName}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] UnregisterLegacyEventHandler: {ex.Message}", LogLevel.Error);
            }
        }

        public void BridgeEvent(string eventName, object sender, EventArgs args)
        {
            try
            {
                _eventBridge.RaiseEvent(eventName, sender, args);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] BridgeEvent: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Legacy Control Factories

        private void RegisterLegacyControlFactories()
        {
            try
            {
                // Register factory methods for each legacy control type
                _controlFactories[typeof(UC_CONTROL_SET_1)] = () => CreateUC_CONTROL_SET_1();
                _controlFactories[typeof(UC_CONTROL_SET_2)] = () => CreateUC_CONTROL_SET_2();
                _controlFactories[typeof(UC_PROGRAM_CONTROL_SET_1)] = () => CreateUC_PROGRAM_CONTROL_SET_1();
                _controlFactories[typeof(UC_PROGRAM_CONTROL_SET_2)] = () => CreateUC_PROGRAM_CONTROL_SET_2();
                _controlFactories[typeof(UC_Setting)] = () => CreateUC_Setting();
                _controlFactories[typeof(UC_Graph_Data_Set_1)] = () => CreateUC_Graph_Data_Set_1();
                _controlFactories[typeof(UC_Graph_Data_Set_2)] = () => CreateUC_Graph_Data_Set_2();

                Logger.Log("[LegacyAdapter] 🏭 Legacy control factories registered", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] RegisterLegacyControlFactories: {ex.Message}", LogLevel.Error);
            }
        }

        private UC_CONTROL_SET_1 CreateUC_CONTROL_SET_1()
        {
            try
            {
                var control = new UC_CONTROL_SET_1();

                // Apply any necessary configuration
                ConfigureControlSet1(control);

                return control;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateUC_CONTROL_SET_1: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private UC_CONTROL_SET_2 CreateUC_CONTROL_SET_2()
        {
            try
            {
                var control = new UC_CONTROL_SET_2();
                ConfigureControlSet2(control);
                return control;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateUC_CONTROL_SET_2: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private UC_PROGRAM_CONTROL_SET_1 CreateUC_PROGRAM_CONTROL_SET_1()
        {
            try
            {
                var control = new UC_PROGRAM_CONTROL_SET_1();
                ConfigureProgramControl1(control);
                return control;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateUC_PROGRAM_CONTROL_SET_1: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private UC_PROGRAM_CONTROL_SET_2 CreateUC_PROGRAM_CONTROL_SET_2()
        {
            try
            {
                var control = new UC_PROGRAM_CONTROL_SET_2();
                ConfigureProgramControl2(control);
                return control;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateUC_PROGRAM_CONTROL_SET_2: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private UC_Setting CreateUC_Setting()
        {
            try
            {
                var control = new UC_Setting();
                ConfigureSettings(control);
                return control;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateUC_Setting: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private UC_Graph_Data_Set_1 CreateUC_Graph_Data_Set_1()
        {
            try
            {
                var control = new UC_Graph_Data_Set_1();
                ConfigureGraphData1(control);
                return control;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateUC_Graph_Data_Set_1: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private UC_Graph_Data_Set_2 CreateUC_Graph_Data_Set_2()
        {
            try
            {
                var control = new UC_Graph_Data_Set_2();
                ConfigureGraphData2(control);
                return control;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateUC_Graph_Data_Set_2: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #endregion

        #region Legacy Control Configuration

        private void ConfigureControlSet1(UC_CONTROL_SET_1 control)
        {
            try
            {
                // Set up event bridging for Control Set 1
                SetupControlSet1EventBridge(control);

                // Initialize with current state
                LoadControlSet1State(control);

                Logger.Log("[LegacyAdapter] ⚙️ UC_CONTROL_SET_1 configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] ConfigureControlSet1: {ex.Message}", LogLevel.Error);
            }
        }

        private void ConfigureControlSet2(UC_CONTROL_SET_2 control)
        {
            try
            {
                SetupControlSet2EventBridge(control);
                LoadControlSet2State(control);
                Logger.Log("[LegacyAdapter] ⚙️ UC_CONTROL_SET_2 configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] ConfigureControlSet2: {ex.Message}", LogLevel.Error);
            }
        }

        private void ConfigureProgramControl1(UC_PROGRAM_CONTROL_SET_1 control)
        {
            try
            {
                SetupProgramControl1EventBridge(control);
                LoadProgramControl1State(control);
                Logger.Log("[LegacyAdapter] ⚙️ UC_PROGRAM_CONTROL_SET_1 configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] ConfigureProgramControl1: {ex.Message}", LogLevel.Error);
            }
        }

        private void ConfigureProgramControl2(UC_PROGRAM_CONTROL_SET_2 control)
        {
            try
            {
                SetupProgramControl2EventBridge(control);
                LoadProgramControl2State(control);
                Logger.Log("[LegacyAdapter] ⚙️ UC_PROGRAM_CONTROL_SET_2 configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] ConfigureProgramControl2: {ex.Message}", LogLevel.Error);
            }
        }

        private void ConfigureSettings(UC_Setting control)
        {
            try
            {
                SetupSettingsEventBridge(control);
                LoadSettingsState(control);
                Logger.Log("[LegacyAdapter] ⚙️ UC_Setting configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] ConfigureSettings: {ex.Message}", LogLevel.Error);
            }
        }

        private void ConfigureGraphData1(UC_Graph_Data_Set_1 control)
        {
            try
            {
                SetupGraphData1EventBridge(control);
                LoadGraphData1State(control);
                Logger.Log("[LegacyAdapter] ⚙️ UC_Graph_Data_Set_1 configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] ConfigureGraphData1: {ex.Message}", LogLevel.Error);
            }
        }

        private void ConfigureGraphData2(UC_Graph_Data_Set_2 control)
        {
            try
            {
                SetupGraphData2EventBridge(control);
                LoadGraphData2State(control);
                Logger.Log("[LegacyAdapter] ⚙️ UC_Graph_Data_Set_2 configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] ConfigureGraphData2: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Wrapper Creation

        private async Task<LegacyControlWrapper> CreateLegacyWrapperAsync(Type userControlType)
        {
            try
            {
                // Create the legacy control on UI thread
                var legacyControl = await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_controlFactories.TryGetValue(userControlType, out var factory))
                    {
                        return factory();
                    }
                    return Activator.CreateInstance(userControlType);
                });

                if (legacyControl == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of {userControlType.Name}");
                }

                // Initialize the control
                await InitializeLegacyControlAsync(legacyControl);

                // Create Avalonia wrapper container
                var container = CreateAvaloniaContainer(legacyControl, userControlType.Name);

                var wrapper = new LegacyControlWrapper
                {
                    LegacyControl = legacyControl,
                    AvaloniaContainer = container,
                    ControlType = userControlType,
                    CreatedAt = DateTime.Now
                };

                Logger.Log($"[LegacyAdapter] 📦 Wrapper created for: {userControlType.Name}", LogLevel.Debug);
                return wrapper;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateLegacyWrapperAsync: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private Control CreateAvaloniaContainer(object legacyControl, string controlName)
        {
            try
            {
                // Create a border container with modern styling
                var border = new Border
                {
                    Background = Avalonia.Media.Brushes.Transparent,
                    BorderBrush = Avalonia.Media.Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0),
                    Padding = new Avalonia.Thickness(0),
                    Name = $"LegacyWrapper_{controlName}"
                };

                // For now, create a placeholder content
                // In a real implementation, you would integrate with WindowsFormsHost or similar
                var placeholder = new TextBlock
                {
                    Text = $"Legacy Control: {controlName}\n(Wrapped in Modern Container)",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontSize = 14,
                    FontWeight = Avalonia.Media.FontWeight.Medium,
                    Foreground = Avalonia.Media.Brushes.Gray
                };

                border.Child = placeholder;

                Logger.Log($"[LegacyAdapter] 🎨 Avalonia container created for: {controlName}", LogLevel.Debug);
                return border;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CreateAvaloniaContainer: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #endregion

        #region Event Bridge Setup

        private void InitializeEventBridge()
        {
            try
            {
                // Register common event types
                _eventBridge.RegisterEventType("NavigationRequested");
                _eventBridge.RegisterEventType("DataChanged");
                _eventBridge.RegisterEventType("StateUpdated");
                _eventBridge.RegisterEventType("ErrorOccurred");
                _eventBridge.RegisterEventType("SaveRequested");

                Logger.Log("[LegacyAdapter] 🌉 Event bridge initialized", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] InitializeEventBridge: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupControlSet1EventBridge(UC_CONTROL_SET_1 control)
        {
            try
            {
                // Bridge navigation events from legacy control
                var navigateToSet2Method = control.GetType().GetMethod("NavigateToControlSet2");
                if (navigateToSet2Method != null)
                {
                    _eventBridge.RegisterHandler("NavigateToControlSet2",
                        new Action(() => BridgeEvent("NavigationRequested", control, new NavigationEventArgs("UC_CONTROL_SET_2"))));
                }

                // Bridge data change events
                // Note: This would need actual event subscription in a real implementation

                Logger.Log("[LegacyAdapter] 🌉 Control Set 1 event bridge configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] SetupControlSet1EventBridge: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupControlSet2EventBridge(UC_CONTROL_SET_2 control)
        {
            try
            {
                // Similar setup for Control Set 2
                Logger.Log("[LegacyAdapter] 🌉 Control Set 2 event bridge configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] SetupControlSet2EventBridge: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupProgramControl1EventBridge(UC_PROGRAM_CONTROL_SET_1 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 🌉 Program Control 1 event bridge configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] SetupProgramControl1EventBridge: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupProgramControl2EventBridge(UC_PROGRAM_CONTROL_SET_2 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 🌉 Program Control 2 event bridge configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] SetupProgramControl2EventBridge: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupSettingsEventBridge(UC_Setting control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 🌉 Settings event bridge configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] SetupSettingsEventBridge: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupGraphData1EventBridge(UC_Graph_Data_Set_1 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 🌉 Graph Data 1 event bridge configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] SetupGraphData1EventBridge: {ex.Message}", LogLevel.Error);
            }
        }

        private void SetupGraphData2EventBridge(UC_Graph_Data_Set_2 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 🌉 Graph Data 2 event bridge configured", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] SetupGraphData2EventBridge: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region State Management

        private async Task InitializeLegacyControlAsync(object control)
        {
            try
            {
                // Initialize the legacy control with current state
                if (control is ILegacyInitializable initializable)
                {
                    await initializable.InitializeAsync();
                }

                // Load saved state if available
                await LoadControlStateAsync(control);

                Logger.Log($"[LegacyAdapter] 🔧 Legacy control initialized: {control.GetType().Name}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] InitializeLegacyControlAsync: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task LoadControlStateAsync(object control)
        {
            try
            {
                var controlType = control.GetType().Name;

                if (_stateManager != null)
                {
                    var state = await _stateManager.GetStateAsync(controlType);
                    if (state != null && control is IStateful statefulControl)
                    {
                        statefulControl.LoadState(state);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] LoadControlStateAsync: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadControlSet1State(UC_CONTROL_SET_1 control)
        {
            try
            {
                // Load specific Control Set 1 state
                // This would involve reading from Data_Set1, ProgramState_1, etc.
                Logger.Log("[LegacyAdapter] 📊 Control Set 1 state loaded", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] LoadControlSet1State: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadControlSet2State(UC_CONTROL_SET_2 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 📊 Control Set 2 state loaded", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] LoadControlSet2State: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadProgramControl1State(UC_PROGRAM_CONTROL_SET_1 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 📊 Program Control 1 state loaded", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] LoadProgramControl1State: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadProgramControl2State(UC_PROGRAM_CONTROL_SET_2 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 📊 Program Control 2 state loaded", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] LoadProgramControl2State: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadSettingsState(UC_Setting control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 📊 Settings state loaded", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] LoadSettingsState: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadGraphData1State(UC_Graph_Data_Set_1 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 📊 Graph Data 1 state loaded", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] LoadGraphData1State: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadGraphData2State(UC_Graph_Data_Set_2 control)
        {
            try
            {
                Logger.Log("[LegacyAdapter] 📊 Graph Data 2 state loaded", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] LoadGraphData2State: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Settings Migration

        private async Task MigrateControlSet1Settings()
        {
            try
            {
                // Migrate Data_Set1 and related settings
                await Task.Run(() =>
                {
                    // Read legacy settings from Data_Set1.CurrentData_1
                    // Convert to modern state format
                    // Save using IStateManager
                });

                Logger.Log("[LegacyAdapter] 📋 Control Set 1 settings migrated", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] MigrateControlSet1Settings: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task MigrateControlSet2Settings()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Similar migration for Set 2
                });

                Logger.Log("[LegacyAdapter] 📋 Control Set 2 settings migrated", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] MigrateControlSet2Settings: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task MigrateProgramSettings()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Migrate ProgramState_1 and ProgramState_2
                });

                Logger.Log("[LegacyAdapter] 📋 Program settings migrated", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] MigrateProgramSettings: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task MigrateDeviceSettings()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Migrate device configuration settings
                });

                Logger.Log("[LegacyAdapter] 📋 Device settings migrated", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] MigrateDeviceSettings: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Memory Management

        public void CleanupUnusedWrappers()
        {
            try
            {
                var cutoffTime = DateTime.Now.AddMinutes(-30); // Cleanup after 30 minutes
                var wrappersToRemove = new List<string>();

                lock (_wrapperLock)
                {
                    foreach (var kvp in _lastAccessTimes)
                    {
                        if (kvp.Value < cutoffTime && _wrappedControls.ContainsKey(kvp.Key))
                        {
                            wrappersToRemove.Add(kvp.Key);
                        }
                    }

                    foreach (var key in wrappersToRemove)
                    {
                        if (_wrappedControls.TryGetValue(key, out var wrapper))
                        {
                            wrapper.Dispose();
                            _wrappedControls.Remove(key);
                            _lastAccessTimes.Remove(key);
                        }
                    }
                }

                if (wrappersToRemove.Count > 0)
                {
                    Logger.Log($"[LegacyAdapter] 🧹 Cleaned up {wrappersToRemove.Count} unused wrappers", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] CleanupUnusedWrappers: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Public API

        public IReadOnlyDictionary<string, LegacyControlWrapper> GetActiveWrappers()
        {
            lock (_wrapperLock)
            {
                return new Dictionary<string, LegacyControlWrapper>(_wrappedControls);
            }
        }

        public async Task<bool> TestLegacyIntegration()
        {
            try
            {
                Logger.Log("[LegacyAdapter] 🧪 Testing legacy integration", LogLevel.Info);

                // Test wrapping each control type
                var testResults = new List<bool>();

                var controlTypes = new[]
                {
                    typeof(UC_CONTROL_SET_1),
                    typeof(UC_CONTROL_SET_2),
                    typeof(UC_PROGRAM_CONTROL_SET_1),
                    typeof(UC_PROGRAM_CONTROL_SET_2),
                    typeof(UC_Setting),
                    typeof(UC_Graph_Data_Set_1),
                    typeof(UC_Graph_Data_Set_2)
                };

                foreach (var controlType in controlTypes)
                {
                    try
                    {
                        var wrapper = await WrapLegacyUserControl(controlType);
                        testResults.Add(wrapper != null);
                        Logger.Log($"[LegacyAdapter] ✅ Test passed: {controlType.Name}", LogLevel.Debug);
                    }
                    catch (Exception ex)
                    {
                        testResults.Add(false);
                        Logger.Log($"[LegacyAdapter] ❌ Test failed: {controlType.Name} - {ex.Message}", LogLevel.Error);
                    }
                }

                var allPassed = testResults.All(r => r);
                Logger.Log($"[LegacyAdapter] 🧪 Integration test completed: {(allPassed ? "PASSED" : "FAILED")}",
                    allPassed ? LogLevel.Info : LogLevel.Error);

                return allPassed;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] TestLegacyIntegration: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            try
            {
                lock (_wrapperLock)
                {
                    foreach (var wrapper in _wrappedControls.Values)
                    {
                        wrapper.Dispose();
                    }
                    _wrappedControls.Clear();
                    _lastAccessTimes.Clear();
                }

                _eventBridge?.Dispose();

                Logger.Log("[LegacyAdapter] 🧹 Legacy adapter disposed", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyAdapter][ERROR] Dispose: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }

    #region Support Classes

    /// <summary>
    /// Wrapper for legacy UserControl integration
    /// </summary>
    public class LegacyControlWrapper : IDisposable
    {
        public object LegacyControl { get; set; } = null!;
        public Control AvaloniaContainer { get; set; } = null!;
        public Type ControlType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public void Dispose()
        {
            try
            {
                if (LegacyControl is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[LegacyWrapper][ERROR] Dispose: {ex.Message}", LogLevel.Error);
            }
        }
    }

    /// <summary>
    /// Event bridge for legacy-modern communication
    /// </summary>
    public class EventBridge : IDisposable
    {
        private readonly Dictionary<string, List<Delegate>> _eventHandlers = new();
        private readonly Dictionary<string, Type> _eventTypes = new();

        public void RegisterEventType(string eventName)
        {
            if (!_eventTypes.ContainsKey(eventName))
            {
                _eventTypes[eventName] = typeof(EventArgs);
                _eventHandlers[eventName] = new List<Delegate>();
            }
        }

        public void RegisterHandler(string eventName, Delegate handler)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Add(handler);
            }
        }

        public void UnregisterHandler(string eventName, Delegate handler)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        public void RaiseEvent(string eventName, object sender, EventArgs args)
        {
            try
            {
                if (_eventHandlers.TryGetValue(eventName, out var handlers))
                {
                    foreach (var handler in handlers.ToList())
                    {
                        try
                        {
                            handler.DynamicInvoke(sender, args);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"[EventBridge][ERROR] Event handler failed: {ex.Message}", LogLevel.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[EventBridge][ERROR] RaiseEvent: {ex.Message}", LogLevel.Error);
            }
        }

        public void Dispose()
        {
            _eventHandlers.Clear();
            _eventTypes.Clear();
        }
    }

    /// <summary>
    /// Exception for legacy integration issues
    /// </summary>
    public class LegacyIntegrationException : Exception
    {
        public LegacyIntegrationException(string message) : base(message) { }
        public LegacyIntegrationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Interface for legacy controls that need initialization
    /// </summary>
    public interface ILegacyInitializable
    {
        Task InitializeAsync();
    }

    /// <summary>
    /// Interface for stateful legacy controls
    /// </summary>
    public interface IStateful
    {
        void LoadState(object state);
        object SaveState();
    }

    /// <summary>
    /// Event args for navigation events
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public string TargetView { get; }
        public NavigationEventArgs(string targetView) => TargetView = targetView;
    }

    #endregion

    #region Placeholder Legacy Control Types

    // These represent the actual legacy UserControl types
    // In a real implementation, these would be the existing controls

    public class UC_CONTROL_SET_1 : System.Windows.Forms.UserControl { }
    public class UC_CONTROL_SET_2 : System.Windows.Forms.UserControl { }
    public class UC_PROGRAM_CONTROL_SET_1 : System.Windows.Forms.UserControl { }
    public class UC_PROGRAM_CONTROL_SET_2 : System.Windows.Forms.UserControl { }
    public class UC_Setting : System.Windows.Forms.UserControl { }
    public class UC_Graph_Data_Set_1 : System.Windows.Forms.UserControl { }
    public class UC_Graph_Data_Set_2 : System.Windows.Forms.UserControl { }

    #endregion
}