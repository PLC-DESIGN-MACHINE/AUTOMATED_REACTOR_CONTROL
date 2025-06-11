using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.Bootstrap;
using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.Views;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1
{
    internal class AppBootstrapper
    {
        private IDependencyContainer _container;
        private IServiceRegistry _serviceRegistry;
        private bool _isInitialized = false;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            try
            {
                Logger.CurrentLogLevel = LogLevel.Info;
                Logger.Log("🚀 [App] Ultra-Modern Avalonia application startup initiated", LogLevel.Info);

                // Initialize modern architecture
                await InitializeModernArchitectureAsync();

                // Setup application lifetime
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // Create and configure main window
                    var mainWindow = await CreateMainWindowAsync();
                    desktop.MainWindow = mainWindow;

                    // Setup shutdown handling
                    desktop.ShutdownRequested += OnShutdownRequested;
                }
                else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                {
                    // Mobile/Browser support
                    var mainView = await CreateMainViewAsync();
                    singleView.MainView = mainView;
                }

                base.OnFrameworkInitializationCompleted();
                Logger.Log("🎉 [App] Ultra-Modern application startup completed successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [App] Critical startup error: {ex.Message}", LogLevel.Error);
                await HandleCriticalStartupErrorAsync(ex);
            }
        }

        /// <summary>🏗️ Initialize Modern Architecture</summary>
        private async Task InitializeModernArchitectureAsync()
        {
            try
            {
                Logger.Log("🏗️ [App] Initializing Ultra-Modern Architecture", LogLevel.Info);

                // Initialize IoC Container
                _container = new ModernDependencyContainer();

                // Initialize Service Registry
                _serviceRegistry = new ModernServiceRegistry(_container);

                // Register all services
                await _serviceRegistry.RegisterAllServicesAsync();

                // Initialize cross-platform services
                await InitializePlatformServicesAsync();

                _isInitialized = true;
                Logger.Log("✅ [App] Ultra-Modern Architecture initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [App] Architecture initialization failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>📱 Initialize Platform-Specific Services</summary>
        private async Task InitializePlatformServicesAsync()
        {
            try
            {
                var platformService = _container.Resolve<IPlatformService>();
                await platformService.InitializeAsync();

                var performanceMonitor = _container.Resolve<IPerformanceMonitor>();
                await performanceMonitor.StartMonitoringAsync();

                Logger.Log("📱 [App] Platform services initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ [App] Platform services warning: {ex.Message}", LogLevel.Warn);
            }
        }

        /// <summary>🪟 Create Modern Main Window</summary>
        private async Task<MainWindow> CreateMainWindowAsync()
        {
            try
            {
                var mainWindow = _container.Resolve<MainWindow>();
                var viewModel = _container.Resolve<MainWindowViewModel>();

                mainWindow.DataContext = viewModel;

                Logger.Log("🪟 [App] Modern main window created", LogLevel.Info);
                return mainWindow;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [App] Main window creation failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>📱 Create Main View for Mobile/Browser</summary>
        private async Task<object> CreateMainViewAsync()
        {
            try
            {
                var viewFactory = _container.Resolve<IViewFactory>();
                var mainView = await viewFactory.CreateViewAsync("MainView");

                Logger.Log("📱 [App] Main view created", LogLevel.Info);
                return mainView;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [App] Main view creation failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>🚨 Handle Critical Startup Errors</summary>
        private async Task HandleCriticalStartupErrorAsync(Exception ex)
        {
            var errorMessage = $"Critical Error during startup:\n\n{ex.Message}\n\nApplication will exit.";

            // Try to show error dialog
            try
            {
                // If possible, create a simple error window
                var errorWindow = new Window
                {
                    Title = "Critical Startup Error",
                    Width = 500,
                    Height = 300,
                    Content = new TextBlock
                    {
                        Text = errorMessage,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(20)
                    }
                };

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    await errorWindow.ShowDialog(desktop.MainWindow);
                }
            }
            catch
            {
                // Fallback to console output
                Console.WriteLine(errorMessage);
            }

            Environment.Exit(1);
        }

        /// <summary>🔚 Handle Application Shutdown</summary>
        private async void OnShutdownRequested(object sender, ShutdownRequestedEventArgs e)
        {
            try
            {
                Logger.Log("🔚 [App] Application shutdown initiated", LogLevel.Info);

                // Cancel shutdown to allow cleanup
                e.Cancel = true;

                // Perform cleanup
                await SaveApplicationStateAsync();

                // Dispose services
                _serviceRegistry?.Dispose();
                _container?.Dispose();

                Logger.Log("✅ [App] Application shutdown completed", LogLevel.Info);

                // Now allow shutdown
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown(0);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [App] Shutdown error: {ex.Message}", LogLevel.Error);
                Environment.Exit(1);
            }
        }

        /// <summary>💾 Save Application State</summary>
        private async Task SaveApplicationStateAsync()
        {
            try
            {
                if (_isInitialized && _container != null)
                {
                    var stateManager = _container.Resolve<IStateManager>();
                    await stateManager?.SaveAllAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ [App] State save warning: {ex.Message}", LogLevel.Warn);
            }
        }
    }
}
