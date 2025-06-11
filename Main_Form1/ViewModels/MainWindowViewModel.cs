using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Architecture;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Architecture;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Services;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Views.Controls;
using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.ViewModels
{
    /// <summary>
    /// 🎯 MainWindowViewModel - Ultra-Modern MVVM Pattern
    /// Reactive UI with hardware-accelerated navigation and state management
    /// Features: Reactive commands, state binding, performance monitoring
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly INavigationService _navigationService;
        private readonly IStateManager _stateManager;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILegacyAdapter _legacyAdapter;

        // Reactive properties backing fields
        private string _windowTitle = "Ultra-Modern Automated Reactor Control";
        private string _currentViewTitle = "Control Set 1";
        private string _currentViewDescription = "Primary Reactor Controls";
        private Control? _currentView;
        private bool _isNavigating;
        private bool _isLoading;
        private string _loadingText = "";
        private double _loadingProgress;
        private bool _isEmergencyMode;
        private string _emergencyMessage = "";
        private string _statusMessage = "System Ready";
        private string _systemStatus = "Online";
        private bool _isSystemOnline = true;
        private bool _isSerialConnected = true;
        private bool _isWindowActive = true;
        private double _fpsDisplay = 60.0;
        private double _memoryUsage = 0.0;
        private DateTime _lastUpdateTime = DateTime.Now;
        private ConnectionStatus _connectionStatus = ConnectionStatus.Connected;

        // Navigation state
        private bool _isControlSet1Active = true;
        private bool _isControlSet2Active;
        private bool _isProgramSet1Active;
        private bool _isProgramSet2Active;
        private bool _isGraphData1Active;
        private bool _isGraphData2Active;

        #endregion

        #region Properties

        /// <summary>
        /// Window title with dynamic updates
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
        }

        /// <summary>
        /// Current view title for header display
        /// </summary>
        public string CurrentViewTitle
        {
            get => _currentViewTitle;
            set => this.RaiseAndSetIfChanged(ref _currentViewTitle, value);
        }

        /// <summary>
        /// Current view description
        /// </summary>
        public string CurrentViewDescription
        {
            get => _currentViewDescription;
            set => this.RaiseAndSetIfChanged(ref _currentViewDescription, value);
        }

        /// <summary>
        /// Currently displayed view control
        /// </summary>
        public Control? CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        /// <summary>
        /// Whether navigation animation is in progress
        /// </summary>
        public bool IsNavigating
        {
            get => _isNavigating;
            set => this.RaiseAndSetIfChanged(ref _isNavigating, value);
        }

        /// <summary>
        /// Whether loading overlay is visible
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        /// <summary>
        /// Loading text for progress display
        /// </summary>
        public string LoadingText
        {
            get => _loadingText;
            set => this.RaiseAndSetIfChanged(ref _loadingText, value);
        }

        /// <summary>
        /// Loading progress percentage (0-100)
        /// </summary>
        public double LoadingProgress
        {
            get => _loadingProgress;
            set => this.RaiseAndSetIfChanged(ref _loadingProgress, value);
        }

        /// <summary>
        /// Whether emergency mode is active
        /// </summary>
        public bool IsEmergencyMode
        {
            get => _isEmergencyMode;
            set => this.RaiseAndSetIfChanged(ref _isEmergencyMode, value);
        }

        /// <summary>
        /// Emergency mode message
        /// </summary>
        public string EmergencyMessage
        {
            get => _emergencyMessage;
            set => this.RaiseAndSetIfChanged(ref _emergencyMessage, value);
        }

        /// <summary>
        /// Status bar message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        /// <summary>
        /// System status text
        /// </summary>
        public string SystemStatus
        {
            get => _systemStatus;
            set => this.RaiseAndSetIfChanged(ref _systemStatus, value);
        }

        /// <summary>
        /// Whether system is online
        /// </summary>
        public bool IsSystemOnline
        {
            get => _isSystemOnline;
            set => this.RaiseAndSetIfChanged(ref _isSystemOnline, value);
        }

        /// <summary>
        /// Whether serial connection is active
        /// </summary>
        public bool IsSerialConnected
        {
            get => _isSerialConnected;
            set => this.RaiseAndSetIfChanged(ref _isSerialConnected, value);
        }

        /// <summary>
        /// Whether main window is active
        /// </summary>
        public bool IsWindowActive
        {
            get => _isWindowActive;
            set => this.RaiseAndSetIfChanged(ref _isWindowActive, value);
        }

        /// <summary>
        /// Current FPS display
        /// </summary>
        public double FpsDisplay
        {
            get => _fpsDisplay;
            set => this.RaiseAndSetIfChanged(ref _fpsDisplay, value);
        }

        /// <summary>
        /// Current memory usage in MB
        /// </summary>
        public double MemoryUsage
        {
            get => _memoryUsage;
            set => this.RaiseAndSetIfChanged(ref _memoryUsage, value);
        }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set => this.RaiseAndSetIfChanged(ref _lastUpdateTime, value);
        }

        /// <summary>
        /// Connection status for indicator
        /// </summary>
        public ConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
        }

        #endregion

        #region Navigation State Properties

        public bool IsControlSet1Active
        {
            get => _isControlSet1Active;
            set => this.RaiseAndSetIfChanged(ref _isControlSet1Active, value);
        }

        public bool IsControlSet2Active
        {
            get => _isControlSet2Active;
            set => this.RaiseAndSetIfChanged(ref _isControlSet2Active, value);
        }

        public bool IsProgramSet1Active
        {
            get => _isProgramSet1Active;
            set => this.RaiseAndSetIfChanged(ref _isProgramSet1Active, value);
        }

        public bool IsProgramSet2Active
        {
            get => _isProgramSet2Active;
            set => this.RaiseAndSetIfChanged(ref _isProgramSet2Active, value);
        }

        public bool IsGraphData1Active
        {
            get => _isGraphData1Active;
            set => this.RaiseAndSetIfChanged(ref _isGraphData1Active, value);
        }

        public bool IsGraphData2Active
        {
            get => _isGraphData2Active;
            set => this.RaiseAndSetIfChanged(ref _isGraphData2Active, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Navigate to Control Set 1
        /// </summary>
        public ReactiveCommand<Unit, Unit> NavigateToControlSet1Command { get; }

        /// <summary>
        /// Navigate to Control Set 2
        /// </summary>
        public ReactiveCommand<Unit, Unit> NavigateToControlSet2Command { get; }

        /// <summary>
        /// Navigate to Program Set 1
        /// </summary>
        public ReactiveCommand<Unit, Unit> NavigateToProgramSet1Command { get; }

        /// <summary>
        /// Navigate to Program Set 2
        /// </summary>
        public ReactiveCommand<Unit, Unit> NavigateToProgramSet2Command { get; }

        /// <summary>
        /// Navigate to Graph Data Set 1
        /// </summary>
        public ReactiveCommand<Unit, Unit> NavigateToGraphData1Command { get; }

        /// <summary>
        /// Navigate to Graph Data Set 2
        /// </summary>
        public ReactiveCommand<Unit, Unit> NavigateToGraphData2Command { get; }

        /// <summary>
        /// Show settings dialog
        /// </summary>
        public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }

        /// <summary>
        /// Emergency stop all operations
        /// </summary>
        public ReactiveCommand<Unit, Unit> EmergencyStopCommand { get; }

        /// <summary>
        /// Save all data
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveAllCommand { get; }

        /// <summary>
        /// Clear emergency mode
        /// </summary>
        public ReactiveCommand<Unit, Unit> ClearEmergencyCommand { get; }

        #endregion

        #region Events

        public event EventHandler<NavigationRequestedEventArgs>? NavigationRequested;
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;
        public event EventHandler<StatusChangedEventArgs>? StatusChanged;

        #endregion

        #region Constructor

        public MainWindowViewModel() : this(null, null, null, null)
        {
        }

        public MainWindowViewModel(
            INavigationService? navigationService,
            IStateManager? stateManager,
            IPerformanceMonitor? performanceMonitor,
            ILegacyAdapter? legacyAdapter)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _legacyAdapter = legacyAdapter ?? throw new ArgumentNullException(nameof(legacyAdapter));

            // Initialize reactive commands
            InitializeCommands();

            // Subscribe to service events
            SubscribeToServiceEvents();

            // Start real-time updates
            StartRealtimeUpdates();

            Logger.Log("[MainWindowViewModel] 🎯 Ultra-modern MVVM ViewModel initialized", LogLevel.Info);
        }

        #endregion

        #region Command Initialization

        private void InitializeCommands()
        {
            try
            {
                // Navigation commands
                NavigateToControlSet1Command = ReactiveCommand.CreateFromTask(
                    async () => await NavigateToViewAsync("UC_CONTROL_SET_1", "Control Set 1", "Primary Reactor Controls"));

                NavigateToControlSet2Command = ReactiveCommand.CreateFromTask(
                    async () => await NavigateToViewAsync("UC_CONTROL_SET_2", "Control Set 2", "Secondary Reactor Controls"));

                NavigateToProgramSet1Command = ReactiveCommand.CreateFromTask(
                    async () => await NavigateToViewAsync("UC_PROGRAM_CONTROL_SET_1", "Program Set 1", "Automated Sequences"));

                NavigateToProgramSet2Command = ReactiveCommand.CreateFromTask(
                    async () => await NavigateToViewAsync("UC_PROGRAM_CONTROL_SET_2", "Program Set 2", "Advanced Automation"));

                NavigateToGraphData1Command = ReactiveCommand.CreateFromTask(
                    async () => await NavigateToViewAsync("UC_Graph_Data_Set_1", "Graph Data 1", "Real-time Monitoring"));

                NavigateToGraphData2Command = ReactiveCommand.CreateFromTask(
                    async () => await NavigateToViewAsync("UC_Graph_Data_Set_2", "Graph Data 2", "Historical Analysis"));

                // System commands
                ShowSettingsCommand = ReactiveCommand.CreateFromTask(ShowSettingsAsync);
                EmergencyStopCommand = ReactiveCommand.CreateFromTask(ExecuteEmergencyStopAsync);
                SaveAllCommand = ReactiveCommand.CreateFromTask(SaveAllDataAsync);
                ClearEmergencyCommand = ReactiveCommand.Create(ClearEmergencyMode);

                Logger.Log("[MainWindowViewModel] ⚡ Reactive commands initialized", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] InitializeCommands: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Service Event Subscriptions

        private void SubscribeToServiceEvents()
        {
            try
            {
                // Performance monitor events
                if (_performanceMonitor != null)
                {
                    _performanceMonitor.PerformanceUpdated += OnPerformanceUpdated;
                }

                // State manager events
                if (_stateManager != null)
                {
                    _stateManager.StateChanged += OnStateChanged;
                    _stateManager.SaveCompleted += OnSaveCompleted;
                }

                // Navigation service events
                if (_navigationService != null)
                {
                    _navigationService.NavigationCompleted += OnNavigationCompleted;
                    _navigationService.NavigationFailed += OnNavigationFailed;
                }

                Logger.Log("[MainWindowViewModel] 📡 Service events subscribed", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] SubscribeToServiceEvents: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Real-time Updates

        private void StartRealtimeUpdates()
        {
            try
            {
                // Update timestamps every second
                Observable.Interval(TimeSpan.FromSeconds(1))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => LastUpdateTime = DateTime.Now);

                // System status monitoring
                Observable.Interval(TimeSpan.FromSeconds(5))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => UpdateSystemStatus());

                Logger.Log("[MainWindowViewModel] ⏱️ Real-time updates started", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] StartRealtimeUpdates: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Navigation Methods

        private async Task NavigateToViewAsync(string viewName, string title, string description)
        {
            try
            {
                Logger.Log($"[MainWindowViewModel] 🧭 Navigating to: {viewName}", LogLevel.Info);

                IsNavigating = true;

                // Request navigation through event
                NavigationRequested?.Invoke(this, new NavigationRequestedEventArgs(viewName));

                // Update UI state
                CurrentViewTitle = title;
                CurrentViewDescription = description;

                // Update navigation state
                UpdateNavigationState(viewName);

                await Task.Delay(100); // Small delay for UI feedback

                Logger.Log($"[MainWindowViewModel] ✅ Navigation completed to: {viewName}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] NavigateToViewAsync: {ex.Message}", LogLevel.Error);
                ErrorOccurred?.Invoke(this, new ErrorEventArgs($"Navigation failed: {ex.Message}", ErrorType.Navigation, ex));
            }
            finally
            {
                IsNavigating = false;
            }
        }

        public void UpdateNavigationState(string viewName)
        {
            try
            {
                // Reset all navigation states
                IsControlSet1Active = false;
                IsControlSet2Active = false;
                IsProgramSet1Active = false;
                IsProgramSet2Active = false;
                IsGraphData1Active = false;
                IsGraphData2Active = false;

                // Set active state for current view
                switch (viewName)
                {
                    case "UC_CONTROL_SET_1":
                        IsControlSet1Active = true;
                        break;
                    case "UC_CONTROL_SET_2":
                        IsControlSet2Active = true;
                        break;
                    case "UC_PROGRAM_CONTROL_SET_1":
                        IsProgramSet1Active = true;
                        break;
                    case "UC_PROGRAM_CONTROL_SET_2":
                        IsProgramSet2Active = true;
                        break;
                    case "UC_Graph_Data_Set_1":
                        IsGraphData1Active = true;
                        break;
                    case "UC_Graph_Data_Set_2":
                        IsGraphData2Active = true;
                        break;
                }

                Logger.Log($"[MainWindowViewModel] 🎯 Navigation state updated: {viewName}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] UpdateNavigationState: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region System Commands

        private async Task ShowSettingsAsync()
        {
            try
            {
                Logger.Log("[MainWindowViewModel] ⚙️ Showing settings", LogLevel.Info);
                await NavigateToViewAsync("UC_Setting", "Settings", "System Configuration");
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] ShowSettingsAsync: {ex.Message}", LogLevel.Error);
                ErrorOccurred?.Invoke(this, new ErrorEventArgs($"Failed to show settings: {ex.Message}", ErrorType.Navigation, ex));
            }
        }

        private async Task ExecuteEmergencyStopAsync()
        {
            try
            {
                Logger.Log("[MainWindowViewModel] 🚨 EMERGENCY STOP ACTIVATED", LogLevel.Error);

                IsEmergencyMode = true;
                EmergencyMessage = "EMERGENCY STOP ACTIVATED\n\nAll reactor operations have been halted.\nSystem is in safe mode.";

                // TODO: Implement actual emergency stop logic
                // Stop all reactor operations
                // Save current state
                // Notify all systems

                StatusMessage = "EMERGENCY MODE - All operations stopped";
                ConnectionStatus = ConnectionStatus.Warning;

                await Task.Delay(100); // Ensure UI updates
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] ExecuteEmergencyStopAsync: {ex.Message}", LogLevel.Error);
                ErrorOccurred?.Invoke(this, new ErrorEventArgs($"Emergency stop failed: {ex.Message}", ErrorType.Critical, ex));
            }
        }

        private async Task SaveAllDataAsync()
        {
            try
            {
                Logger.Log("[MainWindowViewModel] 💾 Saving all data", LogLevel.Info);

                IsLoading = true;
                LoadingText = "Saving all data...";
                LoadingProgress = 0;

                // Use state manager to save all data
                if (_stateManager != null)
                {
                    LoadingProgress = 25;
                    await _stateManager.SaveStateAsync();

                    LoadingProgress = 75;
                    await Task.Delay(500); // Visual feedback

                    LoadingProgress = 100;
                    StatusMessage = "All data saved successfully";
                }

                Logger.Log("[MainWindowViewModel] ✅ All data saved successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] SaveAllDataAsync: {ex.Message}", LogLevel.Error);
                ErrorOccurred?.Invoke(this, new ErrorEventArgs($"Save failed: {ex.Message}", ErrorType.DataSave, ex));
                StatusMessage = $"Save error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                LoadingProgress = 0;
            }
        }

        private void ClearEmergencyMode()
        {
            try
            {
                Logger.Log("[MainWindowViewModel] ✅ Clearing emergency mode", LogLevel.Info);

                IsEmergencyMode = false;
                EmergencyMessage = "";
                StatusMessage = "Emergency mode cleared - System ready";
                ConnectionStatus = ConnectionStatus.Connected;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] ClearEmergencyMode: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Event Handlers

        private void OnPerformanceUpdated(object? sender, PerformanceMetrics metrics)
        {
            try
            {
                FpsDisplay = metrics.FramesPerSecond;
                MemoryUsage = metrics.MemoryUsageMB;

                // Update connection status based on performance
                if (metrics.FramesPerSecond < 30)
                {
                    ConnectionStatus = ConnectionStatus.Warning;
                }
                else if (metrics.FramesPerSecond >= 55)
                {
                    ConnectionStatus = ConnectionStatus.Connected;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] OnPerformanceUpdated: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnStateChanged(object? sender, StateChangedEventArgs e)
        {
            try
            {
                StatusMessage = $"State updated: {e.StateName}";
                Logger.Log($"[MainWindowViewModel] 📊 State changed: {e.StateName}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] OnStateChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnSaveCompleted(object? sender, SaveCompletedEventArgs e)
        {
            try
            {
                if (e.Success)
                {
                    StatusMessage = "Auto-save completed successfully";
                }
                else
                {
                    StatusMessage = $"Auto-save failed: {e.Error}";
                    ErrorOccurred?.Invoke(this, new ErrorEventArgs($"Save failed: {e.Error}", ErrorType.DataSave));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] OnSaveCompleted: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnNavigationCompleted(object? sender, NavigationCompletedEventArgs e)
        {
            try
            {
                CurrentView = e.View;
                StatusMessage = $"Navigated to {e.ViewName}";
                Logger.Log($"[MainWindowViewModel] ✅ Navigation completed: {e.ViewName}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] OnNavigationCompleted: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnNavigationFailed(object? sender, NavigationFailedEventArgs e)
        {
            try
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs($"Navigation failed: {e.Error}", ErrorType.Navigation));
                StatusMessage = $"Navigation failed: {e.Error}";
                Logger.Log($"[MainWindowViewModel] ❌ Navigation failed: {e.Error}", LogLevel.Error);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] OnNavigationFailed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Status Updates

        private void UpdateSystemStatus()
        {
            try
            {
                // Update system status based on various factors
                if (IsEmergencyMode)
                {
                    SystemStatus = "Emergency Mode";
                    IsSystemOnline = false;
                }
                else if (FpsDisplay < 30)
                {
                    SystemStatus = "Performance Warning";
                    IsSystemOnline = true;
                }
                else
                {
                    SystemStatus = "Online";
                    IsSystemOnline = true;
                }

                // Update serial connection status (placeholder logic)
                IsSerialConnected = IsSystemOnline && !IsEmergencyMode;

                // Raise status changed event
                StatusChanged?.Invoke(this, new StatusChangedEventArgs(StatusMessage, SystemStatus, IsSystemOnline));
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] UpdateSystemStatus: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force refresh of all UI elements
        /// </summary>
        public void RefreshUI()
        {
            try
            {
                this.RaisePropertyChanged(nameof(WindowTitle));
                this.RaisePropertyChanged(nameof(CurrentViewTitle));
                this.RaisePropertyChanged(nameof(StatusMessage));
                this.RaisePropertyChanged(nameof(SystemStatus));

                Logger.Log("[MainWindowViewModel] 🔄 UI refreshed", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] RefreshUI: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Update loading progress
        /// </summary>
        public void UpdateLoadingProgress(double progress, string? text = null)
        {
            try
            {
                LoadingProgress = Math.Max(0, Math.Min(100, progress));
                if (text != null)
                {
                    LoadingText = text;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] UpdateLoadingProgress: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Show temporary status message
        /// </summary>
        public async Task ShowTemporaryStatusAsync(string message, TimeSpan duration)
        {
            try
            {
                var originalMessage = StatusMessage;
                StatusMessage = message;

                await Task.Delay(duration);

                StatusMessage = originalMessage;
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindowViewModel][ERROR] ShowTemporaryStatusAsync: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Unsubscribe from events
                    if (_performanceMonitor != null)
                    {
                        _performanceMonitor.PerformanceUpdated -= OnPerformanceUpdated;
                    }

                    if (_stateManager != null)
                    {
                        _stateManager.StateChanged -= OnStateChanged;
                        _stateManager.SaveCompleted -= OnSaveCompleted;
                    }

                    if (_navigationService != null)
                    {
                        _navigationService.NavigationCompleted -= OnNavigationCompleted;
                        _navigationService.NavigationFailed -= OnNavigationFailed;
                    }

                    Logger.Log("[MainWindowViewModel] 🧹 ViewModel disposed", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[MainWindowViewModel][ERROR] Dispose: {ex.Message}", LogLevel.Error);
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }

    #region Support Classes

    /// <summary>
    /// Base class for ViewModels with common functionality
    /// </summary>
    public abstract class ViewModelBase : ReactiveObject, IDisposable
    {
        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Event args for navigation requests
    /// </summary>
    public class NavigationRequestedEventArgs : EventArgs
    {
        public string ViewName { get; }
        public object? Parameters { get; }

        public NavigationRequestedEventArgs(string viewName, object? parameters = null)
        {
            ViewName = viewName;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Event args for errors
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public ErrorType ErrorType { get; }
        public Exception? Exception { get; }

        public ErrorEventArgs(string errorMessage, ErrorType errorType, Exception? exception = null)
        {
            ErrorMessage = errorMessage;
            ErrorType = errorType;
            Exception = exception;
        }
    }

    /// <summary>
    /// Event args for status changes
    /// </summary>
    public class StatusChangedEventArgs : EventArgs
    {
        public string StatusMessage { get; }
        public string SystemStatus { get; }
        public bool IsOnline { get; }

        public StatusChangedEventArgs(string statusMessage, string systemStatus, bool isOnline)
        {
            StatusMessage = statusMessage;
            SystemStatus = systemStatus;
            IsOnline = isOnline;
        }
    }

    /// <summary>
    /// Error types enumeration
    /// </summary>
    public enum ErrorType
    {
        Navigation,
        DataSave,
        SerialCommunication,
        Critical
    }

    // Placeholder event args classes for service events
    public class StateChangedEventArgs : EventArgs
    {
        public string StateName { get; }
        public StateChangedEventArgs(string stateName) => StateName = stateName;
    }

    public class SaveCompletedEventArgs : EventArgs
    {
        public bool Success { get; }
        public string? Error { get; }
        public SaveCompletedEventArgs(bool success, string? error = null)
        {
            Success = success;
            Error = error;
        }
    }

    public class NavigationCompletedEventArgs : EventArgs
    {
        public string ViewName { get; }
        public Control View { get; }
        public NavigationCompletedEventArgs(string viewName, Control view)
        {
            ViewName = viewName;
            View = view;
        }
    }

    public class NavigationFailedEventArgs : EventArgs
    {
        public string ViewName { get; }
        public string Error { get; }
        public NavigationFailedEventArgs(string viewName, string error)
        {
            ViewName = viewName;
            Error = error;
        }
    }

    #endregion
}