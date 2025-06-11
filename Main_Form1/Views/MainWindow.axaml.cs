using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.ViewModels;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Services;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Architecture;
using System;
using System.Threading.Tasks;

namespace AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Views
{
    /// <summary>
    /// 🚀 Ultra-Modern MainWindow - Hardware Accelerated UI
    /// Replaces legacy Main_Form1.cs with modern Avalonia architecture
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields

        private readonly MainWindowViewModel _viewModel;
        private readonly INavigationService _navigationService;
        private readonly IAnimationEngine _animationEngine;
        private readonly IStateManager _stateManager;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly IDependencyContainer _container;

        private bool _isInitialized = false;
        private bool _isClosing = false;

        #endregion

        #region Constructor & Initialization

        public MainWindow()
        {
            InitializeComponent();

            // Get services from DI container
            _container = App.Current?.Services as IDependencyContainer;
            _navigationService = _container?.Resolve<INavigationService>();
            _animationEngine = _container?.Resolve<IAnimationEngine>();
            _stateManager = _container?.Resolve<IStateManager>();
            _performanceMonitor = _container?.Resolve<IPerformanceMonitor>();

            // Initialize ViewModel
            _viewModel = _container?.Resolve<MainWindowViewModel>() ?? new MainWindowViewModel();
            DataContext = _viewModel;

            // Setup window properties
            SetupWindowProperties();

            // Register event handlers
            RegisterEventHandlers();
        }

        public MainWindow(IDependencyContainer container) : this()
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #endregion

        #region Window Setup

        private void SetupWindowProperties()
        {
            try
            {
                // Enable hardware acceleration
                if (_animationEngine != null)
                {
                    _animationEngine.SetHardwareAcceleration(true);
                }

                // Set window icon if available
                if (App.Current?.Resources.TryGetResource("AppIcon", null, out var iconResource) == true)
                {
                    Icon = iconResource as Avalonia.Controls.WindowIcon;
                }

                // Configure for optimal performance
                RenderOptions.SetBitmapInterpolationMode(this, Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality);

                Logger.Log("[MainWindow] Window properties configured successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] SetupWindowProperties: {ex.Message}", LogLevel.Error);
            }
        }

        private void RegisterEventHandlers()
        {
            try
            {
                // Window lifecycle events
                this.Loaded += MainWindow_Loaded;
                this.Closing += MainWindow_Closing;
                this.Activated += MainWindow_Activated;
                this.Deactivated += MainWindow_Deactivated;

                // Size and state changes
                this.PropertyChanged += MainWindow_PropertyChanged;

                // ViewModel events
                if (_viewModel != null)
                {
                    _viewModel.NavigationRequested += OnNavigationRequested;
                    _viewModel.ErrorOccurred += OnErrorOccurred;
                    _viewModel.StatusChanged += OnStatusChanged;
                }

                // Performance monitoring
                if (_performanceMonitor != null)
                {
                    _performanceMonitor.PerformanceUpdated += OnPerformanceUpdated;
                }

                Logger.Log("[MainWindow] Event handlers registered successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] RegisterEventHandlers: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Window Events

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_isInitialized) return;

                Logger.Log("[MainWindow] 🚀 Starting ultra-modern initialization", LogLevel.Info);

                // Show loading
                if (_viewModel != null)
                {
                    _viewModel.IsLoading = true;
                    _viewModel.LoadingText = "Initializing Ultra-Modern Architecture...";
                    _viewModel.LoadingProgress = 0;
                }

                // Initialize services with progress updates
                await InitializeServicesAsync();

                // Start performance monitoring
                await StartPerformanceMonitoringAsync();

                // Load default view
                await LoadDefaultViewAsync();

                // Complete initialization
                if (_viewModel != null)
                {
                    _viewModel.IsLoading = false;
                    _viewModel.StatusMessage = "Ultra-Modern Reactor Control Ready";
                }

                _isInitialized = true;
                Logger.Log("[MainWindow] ✅ Ultra-modern initialization completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] MainWindow_Loaded: {ex.Message}", LogLevel.Error);
                await HandleInitializationErrorAsync(ex);
            }
        }

        private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (_isClosing) return;

            try
            {
                _isClosing = true;

                Logger.Log("[MainWindow] 💾 Starting graceful shutdown with auto-save", LogLevel.Info);

                // Cancel close temporarily to save data
                e.Cancel = true;

                // Show saving progress
                if (_viewModel != null)
                {
                    _viewModel.IsLoading = true;
                    _viewModel.LoadingText = "Saving all data...";
                    _viewModel.StatusMessage = "Performing auto-save before exit";
                }

                // Save all data
                await SaveAllDataAsync();

                // Stop services
                await StopServicesAsync();

                // Final cleanup
                CleanupResources();

                Logger.Log("[MainWindow] ✅ Graceful shutdown completed", LogLevel.Info);

                // Now actually close
                _isClosing = false;
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] MainWindow_Closing: {ex.Message}", LogLevel.Error);

                // Force close on error
                e.Cancel = false;
            }
        }

        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            try
            {
                // Resume performance monitoring when window is activated
                _performanceMonitor?.ResumeMonitoring();

                if (_viewModel != null)
                {
                    _viewModel.IsWindowActive = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] MainWindow_Activated: {ex.Message}", LogLevel.Error);
            }
        }

        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            try
            {
                // Pause performance monitoring when window is deactivated
                _performanceMonitor?.PauseMonitoring();

                if (_viewModel != null)
                {
                    _viewModel.IsWindowActive = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] MainWindow_Deactivated: {ex.Message}", LogLevel.Error);
            }
        }

        private void MainWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            try
            {
                // Handle window state changes for performance optimization
                if (e.Property == Window.WindowStateProperty)
                {
                    HandleWindowStateChange((WindowState)e.NewValue);
                }
                else if (e.Property == Window.ClientSizeProperty)
                {
                    HandleWindowSizeChange((Size)e.NewValue);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] MainWindow_PropertyChanged: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Service Initialization

        private async Task InitializeServicesAsync()
        {
            try
            {
                var totalSteps = 5;
                var currentStep = 0;

                // Step 1: Initialize Navigation Service
                currentStep++;
                UpdateLoadingProgress(currentStep, totalSteps, "Initializing Navigation Service...");

                if (_navigationService != null)
                {
                    await _navigationService.InitializeAsync();
                }

                await Task.Delay(200); // Visual feedback

                // Step 2: Initialize Animation Engine
                currentStep++;
                UpdateLoadingProgress(currentStep, totalSteps, "Starting Hardware-Accelerated Animations...");

                if (_animationEngine != null)
                {
                    await _animationEngine.InitializeAsync();
                    _animationEngine.SetHardwareAcceleration(true);
                }

                await Task.Delay(200);

                // Step 3: Initialize State Manager
                currentStep++;
                UpdateLoadingProgress(currentStep, totalSteps, "Loading Application State...");

                if (_stateManager != null)
                {
                    await _stateManager.InitializeAsync();
                    await _stateManager.LoadStateAsync();
                }

                await Task.Delay(200);

                // Step 4: Initialize Performance Monitor
                currentStep++;
                UpdateLoadingProgress(currentStep, totalSteps, "Starting Real-time Performance Monitoring...");

                if (_performanceMonitor != null)
                {
                    await _performanceMonitor.StartMonitoringAsync();
                }

                await Task.Delay(200);

                // Step 5: Complete
                currentStep++;
                UpdateLoadingProgress(currentStep, totalSteps, "Ultra-Modern Architecture Ready!");

                await Task.Delay(300);

                Logger.Log("[MainWindow] ✅ All services initialized successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] InitializeServicesAsync: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private async Task StartPerformanceMonitoringAsync()
        {
            try
            {
                if (_performanceMonitor != null)
                {
                    await _performanceMonitor.StartMonitoringAsync();
                    Logger.Log("[MainWindow] 📈 Performance monitoring started", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] StartPerformanceMonitoringAsync: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task LoadDefaultViewAsync()
        {
            try
            {
                if (_navigationService != null && _viewModel != null)
                {
                    // Navigate to default view (UC_CONTROL_SET_1)
                    await _navigationService.NavigateToAsync("UC_CONTROL_SET_1");

                    _viewModel.CurrentViewTitle = "Control Set 1";
                    _viewModel.CurrentViewDescription = "Primary Reactor Controls";

                    Logger.Log("[MainWindow] 🎯 Default view loaded successfully", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] LoadDefaultViewAsync: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #endregion

        #region Event Handlers

        private async void OnNavigationRequested(object? sender, NavigationRequestedEventArgs e)
        {
            try
            {
                if (_navigationService == null) return;

                Logger.Log($"[MainWindow] 🧭 Navigation requested to: {e.ViewName}", LogLevel.Info);

                // Show navigation animation
                if (_viewModel != null)
                {
                    _viewModel.IsNavigating = true;
                }

                // Perform navigation with hardware acceleration
                await _navigationService.NavigateToAsync(e.ViewName);

                // Update UI state
                Dispatcher.UIThread.Post(() =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.UpdateNavigationState(e.ViewName);
                        _viewModel.IsNavigating = false;
                    }
                });

                Logger.Log($"[MainWindow] ✅ Navigation completed to: {e.ViewName}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] OnNavigationRequested: {ex.Message}", LogLevel.Error);

                if (_viewModel != null)
                {
                    _viewModel.IsNavigating = false;
                    _viewModel.StatusMessage = $"Navigation error: {ex.Message}";
                }
            }
        }

        private void OnErrorOccurred(object? sender, ErrorEventArgs e)
        {
            try
            {
                Logger.Log($"[MainWindow][ERROR] {e.ErrorMessage}", LogLevel.Error);

                // Handle different error types
                switch (e.ErrorType)
                {
                    case ErrorType.Navigation:
                        HandleNavigationError(e);
                        break;
                    case ErrorType.DataSave:
                        HandleDataSaveError(e);
                        break;
                    case ErrorType.SerialCommunication:
                        HandleSerialError(e);
                        break;
                    case ErrorType.Critical:
                        HandleCriticalError(e);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] OnErrorOccurred: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnStatusChanged(object? sender, StatusChangedEventArgs e)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.StatusMessage = e.StatusMessage;
                        _viewModel.SystemStatus = e.SystemStatus;
                        _viewModel.IsSystemOnline = e.IsOnline;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] OnStatusChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnPerformanceUpdated(object? sender, PerformanceMetrics metrics)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.FpsDisplay = metrics.FramesPerSecond;
                        _viewModel.MemoryUsage = metrics.MemoryUsageMB;
                        _viewModel.LastUpdateTime = DateTime.Now;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] OnPerformanceUpdated: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Error Handling

        private async Task HandleInitializationErrorAsync(Exception ex)
        {
            try
            {
                Logger.Log($"[MainWindow][CRITICAL] Initialization failed: {ex.Message}", LogLevel.Error);

                if (_viewModel != null)
                {
                    _viewModel.IsLoading = false;
                    _viewModel.IsEmergencyMode = true;
                    _viewModel.EmergencyMessage = $"Initialization Failed:\n{ex.Message}\n\nPlease restart the application.";
                }

                // Try to show error dialog
                await ShowErrorDialogAsync("Initialization Error",
                    $"Failed to initialize the application:\n\n{ex.Message}\n\nPlease check logs and restart.");
            }
            catch (Exception dialogEx)
            {
                Logger.Log($"[MainWindow][ERROR] HandleInitializationErrorAsync: {dialogEx.Message}", LogLevel.Error);
            }
        }

        private void HandleNavigationError(ErrorEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.StatusMessage = $"Navigation Error: {e.ErrorMessage}";
                _viewModel.IsNavigating = false;
            }
        }

        private void HandleDataSaveError(ErrorEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.StatusMessage = $"Save Error: {e.ErrorMessage}";
            }
        }

        private void HandleSerialError(ErrorEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsSerialConnected = false;
                _viewModel.StatusMessage = $"Serial Error: {e.ErrorMessage}";
            }
        }

        private void HandleCriticalError(ErrorEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsEmergencyMode = true;
                _viewModel.EmergencyMessage = $"Critical Error:\n{e.ErrorMessage}";
            }
        }

        #endregion

        #region Performance Optimization

        private void HandleWindowStateChange(WindowState newState)
        {
            try
            {
                switch (newState)
                {
                    case WindowState.Minimized:
                        // Reduce performance monitoring frequency
                        _performanceMonitor?.SetUpdateFrequency(TimeSpan.FromSeconds(2));
                        break;

                    case WindowState.Normal:
                    case WindowState.Maximized:
                        // Resume normal performance monitoring
                        _performanceMonitor?.SetUpdateFrequency(TimeSpan.FromMilliseconds(100));
                        break;
                }

                Logger.Log($"[MainWindow] Window state changed to: {newState}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] HandleWindowStateChange: {ex.Message}", LogLevel.Error);
            }
        }

        private void HandleWindowSizeChange(Size newSize)
        {
            try
            {
                // Optimize rendering based on window size
                if (newSize.Width < 800 || newSize.Height < 600)
                {
                    // Reduce animation quality for small windows
                    _animationEngine?.SetQualityLevel(AnimationQuality.Medium);
                }
                else
                {
                    // Use high quality for larger windows
                    _animationEngine?.SetQualityLevel(AnimationQuality.High);
                }

                Logger.Log($"[MainWindow] Window size changed to: {newSize}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] HandleWindowSizeChange: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Utility Methods

        private void UpdateLoadingProgress(int currentStep, int totalSteps, string message)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.LoadingProgress = (double)currentStep / totalSteps * 100;
                        _viewModel.LoadingText = message;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] UpdateLoadingProgress: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var messageBox = MessageBoxManager.GetMessageBoxStandard(title, message);
                    await messageBox.ShowAsync();
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] ShowErrorDialogAsync: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task SaveAllDataAsync()
        {
            try
            {
                if (_stateManager != null)
                {
                    await _stateManager.SaveStateAsync();
                    Logger.Log("[MainWindow] 💾 All data saved successfully", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] SaveAllDataAsync: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task StopServicesAsync()
        {
            try
            {
                if (_performanceMonitor != null)
                {
                    await _performanceMonitor.StopMonitoringAsync();
                }

                if (_animationEngine != null)
                {
                    await _animationEngine.StopAsync();
                }

                Logger.Log("[MainWindow] 🛑 All services stopped", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] StopServicesAsync: {ex.Message}", LogLevel.Error);
            }
        }

        private void CleanupResources()
        {
            try
            {
                // Unregister event handlers
                if (_viewModel != null)
                {
                    _viewModel.NavigationRequested -= OnNavigationRequested;
                    _viewModel.ErrorOccurred -= OnErrorOccurred;
                    _viewModel.StatusChanged -= OnStatusChanged;
                }

                if (_performanceMonitor != null)
                {
                    _performanceMonitor.PerformanceUpdated -= OnPerformanceUpdated;
                }

                Logger.Log("[MainWindow] 🧹 Resources cleaned up", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] CleanupResources: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Navigate to specific view programmatically
        /// </summary>
        public async Task NavigateToViewAsync(string viewName)
        {
            try
            {
                if (_navigationService != null)
                {
                    await _navigationService.NavigateToAsync(viewName);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] NavigateToViewAsync: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Force emergency mode
        /// </summary>
        public void ActivateEmergencyMode(string message)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.IsEmergencyMode = true;
                        _viewModel.EmergencyMessage = message;
                    }
                });

                Logger.Log($"[MainWindow] 🚨 Emergency mode activated: {message}", LogLevel.Error);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] ActivateEmergencyMode: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        public PerformanceMetrics? GetPerformanceMetrics()
        {
            try
            {
                return _performanceMonitor?.GetCurrentMetrics();
            }
            catch (Exception ex)
            {
                Logger.Log($"[MainWindow][ERROR] GetPerformanceMetrics: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        #endregion
    }

    #region Event Args Classes

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

    public enum ErrorType
    {
        Navigation,
        DataSave,
        SerialCommunication,
        Critical
    }

    #endregion
}