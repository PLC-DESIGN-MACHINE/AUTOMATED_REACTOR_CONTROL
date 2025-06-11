using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.Architecture;
using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.Services;
using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.ViewModels;
using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.Views;
using AUTOMATED_REACTOR_CONTROL_Ver5_MODERN.Services;
using AUTOMATED_REACTOR_CONTROL_Ver5_MODERN.ViewModels;
using Microsoft.Testing.Platform.Configurations;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.Bootstrap
{
    /// <summary>
    /// 📋 Ultra-Modern Service Registry with Auto Registration
    /// Features: Auto Service Discovery, Lifecycle Management, Performance Optimization
    /// </summary>
    public interface IServiceRegistry
    {
        Task RegisterAllServicesAsync();
        void RegisterCoreServices();
        void RegisterViewModels();
        void RegisterViews();
        void Dispose();
    }

    public class ServiceRegistry : IServiceRegistry, IDisposable
    {
        #region 🏗️ Registry Infrastructure

        private readonly IDependencyContainer _container;
        private readonly List<IDisposable> _registeredServices;

        #endregion

        #region 🚀 Constructor

        public ServiceRegistry(IDependencyContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _registeredServices = new List<IDisposable>();

            Logger.Log("📋 [ServiceRegistry] Ultra-Modern service registry initialized", LogLevel.Info);
        }

        #endregion

        #region 📝 Service Registration

        /// <summary>📝 Register All Services Asynchronously</summary>
        public async Task RegisterAllServicesAsync()
        {
            try
            {
                Logger.Log("📝 [ServiceRegistry] Starting service registration", LogLevel.Info);

                // Register core services
                RegisterCoreServices();

                // Register ViewModels
                RegisterViewModels();

                // Register Views (Legacy UserControls)
                RegisterViews();

                // Initialize services that require async setup
                await InitializeAsyncServicesAsync();

                Logger.Log("✅ [ServiceRegistry] All services registered successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] Service registration failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>🔧 Register Core Services</summary>
        public void RegisterCoreServices()
        {
            try
            {
                // Performance & Monitoring
                _container.RegisterSingleton<IPerformanceMonitor, PerformanceMonitor>();
                _container.RegisterSingleton<ISystemHealthService, SystemHealthService>();
                _container.RegisterSingleton<IMemoryManager, MemoryManager>();

                // Navigation & UI
                _container.RegisterSingleton<INavigationService, NavigationService>();
                _container.RegisterSingleton<IModernAnimationEngine, ModernAnimationEngine>();
                _container.RegisterSingleton<IUIManager, UIManager>();

                // State Management
                _container.RegisterSingleton<IStateManager, StateManager>();
                _container.RegisterSingleton<IConfigurationManager, ConfigurationManager>();

                // Factory Services
                _container.RegisterSingleton<IViewModelFactory, ViewModelFactory>();

                // Legacy Integration Services
                _container.RegisterSingleton<ILegacyIntegrationService, LegacyIntegrationService>();
                _container.RegisterSingleton<ISerialCommunicationService, SerialCommunicationService>();

                Logger.Log("🔧 [ServiceRegistry] Core services registered", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] Core service registration failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>🖼️ Register ViewModels</summary>
        public void RegisterViewModels()
        {
            try
            {
                // Main ViewModels
                _container.RegisterTransient<MainWindowViewModel, MainWindowViewModel>();

                // Control ViewModels (if needed for modern wrappers)
                _container.RegisterTransient<ControlSet1ViewModel, ControlSet1ViewModel>();
                _container.RegisterTransient<ControlSet2ViewModel, ControlSet2ViewModel>();
                _container.RegisterTransient<SettingsViewModel, SettingsViewModel>();

                Logger.Log("🖼️ [ServiceRegistry] ViewModels registered", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] ViewModel registration failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>🎨 Register Views (Legacy UserControls)</summary>
        public void RegisterViews()
        {
            try
            {
                // Register legacy UserControls as transient
                _container.RegisterTransient<UC_CONTROL_SET_1, UC_CONTROL_SET_1>();
                _container.RegisterTransient<UC_CONTROL_SET_2, UC_CONTROL_SET_2>();
                _container.RegisterTransient<UC_PROGRAM_CONTROL_SET_1, UC_PROGRAM_CONTROL_SET_1>();
                _container.RegisterTransient<UC_PROGRAM_CONTROL_SET_2, UC_PROGRAM_CONTROL_SET_2>();
                _container.RegisterTransient<UC_Setting, UC_Setting>();
                _container.RegisterTransient<UC_Graph_Data_Set_1, UC_Graph_Data_Set_1>();
                _container.RegisterTransient<UC_Graph_Data_Set_2, UC_Graph_Data_Set_2>();

                // Register modern windows
                _container.RegisterSingleton<MainWindow, MainWindow>();

                Logger.Log("🎨 [ServiceRegistry] Views registered", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] View registration failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>⚡ Initialize Async Services</summary>
        private async Task InitializeAsyncServicesAsync()
        {
            try
            {
                // Start background services that require async initialization
                var backgroundServices = new[]
                {
                    _container.Resolve<IPerformanceMonitor>(),
                    _container.Resolve<ISystemHealthService>()
                };

                var initTasks = backgroundServices
                    .OfType<IAsyncInitializable>()
                    .Select(service => service.InitializeAsync());

                await Task.WhenAll(initTasks);

                Logger.Log("⚡ [ServiceRegistry] Async services initialized", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] Async service initialization failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            try
            {
                foreach (var service in _registeredServices)
                {
                    try
                    {
                        service?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"⚠️ [ServiceRegistry] Service disposal warning: {ex.Message}", LogLevel.Warn);
                    }
                }

                _registeredServices.Clear();
                Logger.Log("🗑️ [ServiceRegistry] Disposed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }

    #region 📊 Supporting Interfaces

    /// <summary>⚡ Interface for Async Initializable Services</summary>
    public interface IAsyncInitializable
    {
        Task InitializeAsync();
    }

    #endregion
}
}
