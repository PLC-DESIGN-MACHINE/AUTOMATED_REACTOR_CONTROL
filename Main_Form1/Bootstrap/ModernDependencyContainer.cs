using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.Services;
using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.ViewModels;
using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.Views;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.ViewModels;
using Microsoft.Testing.Platform.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1
{
    /// <summary>
    /// 🏗️ Ultra-Modern Dependency Container with Advanced Features
    /// Features: Singleton Management, Async Resolution, Lifecycle Control, Thread Safety
    /// </summary>
    public class ModernDependencyContainer : IDependencyContainer, IDisposable
    {
        #region 🏗️ Container Infrastructure

        private readonly ConcurrentDictionary<Type, ServiceDescriptor> _services;
        private readonly ConcurrentDictionary<Type, object> _singletonInstances;
        private readonly ConcurrentDictionary<Type, SemaphoreSlim> _creationLocks;
        private readonly object _registrationLock = new object();
        private bool _disposed = false;

        #endregion

        #region 🚀 Constructor

        public ModernDependencyContainer()
        {
            _services = new ConcurrentDictionary<Type, ServiceDescriptor>();
            _singletonInstances = new ConcurrentDictionary<Type, object>();
            _creationLocks = new ConcurrentDictionary<Type, SemaphoreSlim>();

            // Register self
            RegisterInstance<IDependencyContainer>(this);

            Logger.Log("🏗️ [ModernDependencyContainer] Ultra-Modern IoC container initialized", LogLevel.Info);
        }

        #endregion

        #region 📝 Registration Methods

        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            lock (_registrationLock)
            {
                _services[typeof(TInterface)] = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Singleton);
                Logger.Log($"📝 [Container] Singleton registered: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}", LogLevel.Debug);
            }
        }

        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            lock (_registrationLock)
            {
                _services[typeof(TInterface)] = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Transient);
                Logger.Log($"📝 [Container] Transient registered: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}", LogLevel.Debug);
            }
        }

        public void RegisterInstance<T>(T instance)
        {
            _singletonInstances[typeof(T)] = instance;
            Logger.Log($"📝 [Container] Instance registered: {typeof(T).Name}", LogLevel.Debug);
        }

        #endregion

        #region 🔧 Resolution Methods

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            try
            {
                // Check singleton instances first
                if _singletonInstances.TryGetValue(type, out var instance))
                {
                    return instance;
                }

                // Check registered services
                if (_services.TryGetValue(type, out var descriptor))
                {
                    return CreateInstance(descriptor, type);
                }

                // Try auto-registration for concrete types
                if (!type.IsInterface && !type.IsAbstract)
                {
                    return CreateInstanceWithAutoWiring(type);
                }

                throw new InvalidOperationException($"Service not registered: {type.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [Container] Resolution failed for {type.Name}: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        public async Task<T> ResolveAsync<T>()
        {
            return await Task.Run(() => Resolve<T>());
        }

        #endregion

        #region 🏭 Instance Creation

        private object CreateInstance(ServiceDescriptor descriptor, Type serviceType)
        {
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                return _singletonInstances.GetOrAdd(serviceType, _ =>
                {
                    var semaphore = _creationLocks.GetOrAdd(serviceType, _ => new SemaphoreSlim(1, 1));
                    semaphore.Wait();
                    try
                    {
                        // Double-check after acquiring lock
                        if (_singletonInstances.TryGetValue(serviceType, out var existingInstance))
                        {
                            return existingInstance;
                        }
                        return CreateInstanceWithAutoWiring(descriptor.ImplementationType);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }
            else
            {
                return CreateInstanceWithAutoWiring(descriptor.ImplementationType);
            }
        }

        private object CreateInstanceWithAutoWiring(Type type)
        {
            try
            {
                var constructors = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length);

                foreach (var constructor in constructors)
                {
                    try
                    {
                        var parameters = constructor.GetParameters();
                        var args = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var paramType = parameters[i].ParameterType;

                            // Handle optional parameters
                            if (parameters[i].HasDefaultValue && !_services.ContainsKey(paramType) && !_singletonInstances.ContainsKey(paramType))
                            {
                                args[i] = parameters[i].DefaultValue;
                            }
                            else
                            {
                                args[i] = Resolve(paramType);
                            }
                        }

                        var instance = Activator.CreateInstance(type, args);
                        Logger.Log($"🏭 [Container] Created instance: {type.Name}", LogLevel.Debug);
                        return instance;
                    }
                    catch (Exception)
                    {
                        continue; // Try next constructor
                    }
                }

                // Fallback to parameterless constructor
                return Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [Container] Instance creation failed for {type.Name}: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Dispose all disposable singletons
                foreach (var instance in _singletonInstances.Values.OfType<IDisposable>())
                {
                    try
                    {
                        instance.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"⚠️ [Container] Disposal warning: {ex.Message}", LogLevel.Warn);
                    }
                }

                // Dispose creation locks
                foreach (var semaphore in _creationLocks.Values)
                {
                    semaphore?.Dispose();
                }

                _singletonInstances.Clear();
                _services.Clear();
                _creationLocks.Clear();
                _disposed = true;

                Logger.Log("🗑️ [ModernDependencyContainer] Disposed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [Container] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 📊 Supporting Classes

        private class ServiceDescriptor
        {
            public Type ImplementationType { get; }
            public ServiceLifetime Lifetime { get; }

            public ServiceDescriptor(Type implementationType, ServiceLifetime lifetime)
            {
                ImplementationType = implementationType;
                Lifetime = lifetime;
            }
        }

        private enum ServiceLifetime
        {
            Singleton,
            Transient
        }

        #endregion
    }

    /// <summary>
    /// 📋 Ultra-Modern Service Registry with Auto Registration and Platform Detection
    /// </summary>
    public class ModernServiceRegistry : IServiceRegistry, IDisposable
    {
        private readonly IDependencyContainer _container;
        private readonly List<IDisposable> _registeredServices;
        private readonly Assembly _currentAssembly;

        public ModernServiceRegistry(IDependencyContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _registeredServices = new List<IDisposable>();
            _currentAssembly = Assembly.GetExecutingAssembly();

            Logger.Log("📋 [ModernServiceRegistry] Ultra-Modern service registry initialized", LogLevel.Info);
        }

        public async Task RegisterAllServicesAsync()
        {
            try
            {
                Logger.Log("📝 [ServiceRegistry] Starting comprehensive service registration", LogLevel.Info);

                // Core Infrastructure Services
                RegisterCoreServices();

                // Platform-Specific Services
                RegisterPlatformServices();

                // Business Logic Services
                RegisterBusinessServices();

                // ViewModels
                RegisterViewModels();

                // Views
                RegisterViews();

                // Legacy Integration Services
                RegisterLegacyServices();

                // Initialize async services
                await InitializeAsyncServicesAsync();

                Logger.Log("✅ [ServiceRegistry] All services registered successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] Service registration failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private void RegisterCoreServices()
        {
            try
            {
                // Core Infrastructure
                _container.RegisterSingleton<INavigationService, ModernNavigationService>();
                _container.RegisterSingleton<IAnimationEngine, AvaloniaAnimationEngine>();
                _container.RegisterSingleton<IStateManager, ReactiveStateManager>();
                _container.RegisterSingleton<IPerformanceMonitor, RealtimePerformanceMonitor>();

                // Factories
                _container.RegisterSingleton<IViewFactory, AvaloniaViewFactory>();

                // Platform Detection
                _container.RegisterSingleton<IPlatformService, CrossPlatformService>();

                Logger.Log("🔧 [ServiceRegistry] Core services registered", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] Core service registration failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private void RegisterPlatformServices()
        {
            try
            {
                // Platform-specific services based on runtime
                if (OperatingSystem.IsWindows())
                {
                    _container.RegisterSingleton<ISerialPortService, WindowsSerialPortService>();
                    _container.RegisterSingleton<IFileSystemService, WindowsFileSystemService>();
                }
                else if (OperatingSystem.IsLinux())
                {
                    _container.RegisterSingleton<ISerialPortService, LinuxSerialPortService>();
                    _container.RegisterSingleton<IFileSystemService, LinuxFileSystemService>();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    _container.RegisterSingleton<ISerialPortService, MacOSSerialPortService>();
                    _container.RegisterSingleton<IFileSystemService, MacOSFileSystemService>();
                }
                else
                {
                    // Browser/Generic fallback
                    _container.RegisterSingleton<ISerialPortService, GenericSerialPortService>();
                    _container.RegisterSingleton<IFileSystemService, GenericFileSystemService>();
                }

                Logger.Log("📱 [ServiceRegistry] Platform services registered", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] Platform service registration failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private void RegisterBusinessServices()
        {
            // Business Logic Services
            _container.RegisterSingleton<IConfigurationManager, ModernConfigurationManager>();
            _container.RegisterSingleton<IDataManager, ReactiveDataManager>();
            _container.RegisterSingleton<IEventBus, ModernEventBus>();
        }

        private void RegisterViewModels()
        {
            // Main ViewModels
            _container.RegisterTransient<MainWindowViewModel>();

            // Feature ViewModels
            _container.RegisterTransient<ControlSet1ViewModel>();
            _container.RegisterTransient<ControlSet2ViewModel>();
            _container.RegisterTransient<SettingsViewModel>();
            _container.RegisterTransient<DiagnosticsViewModel>();
        }

        private void RegisterViews()
        {
            // Modern Views
            _container.RegisterSingleton<MainWindow>();

            // Auto-register view types
            var viewTypes = _currentAssembly.GetTypes()
                .Where(t => t.Namespace?.Contains("Views") == true &&
                           t.IsSubclassOf(typeof(UserControl)) &&
                           !t.IsAbstract)
                .ToList();

            foreach (var viewType in viewTypes)
            {
                var registerMethod = typeof(IDependencyContainer)
                    .GetMethod("RegisterTransient")
                    ?.MakeGenericMethod(viewType, viewType);

                registerMethod?.Invoke(_container, null);
            }
        }

        private void RegisterLegacyServices()
        {
            // Legacy Integration
            _container.RegisterSingleton<ILegacyAdapter, ModernLegacyAdapter>();

            // Legacy UserControl Registration
            _container.RegisterTransient<UC_CONTROL_SET_1>();
            _container.RegisterTransient<UC_CONTROL_SET_2>();
            // Add other legacy controls...
        }

        private async Task InitializeAsyncServicesAsync()
        {
            try
            {
                var asyncServices = new List<IAsyncInitializable>();

                // Resolve services that need async initialization
                try { asyncServices.Add(_container.Resolve<IPerformanceMonitor>() as IAsyncInitializable); } catch { }
                try { asyncServices.Add(_container.Resolve<IPlatformService>() as IAsyncInitializable); } catch { }

                var initTasks = asyncServices
                    .Where(service => service != null)
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
                Logger.Log("🗑️ [ModernServiceRegistry] Disposed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ServiceRegistry] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }
    }

    #region 📊 Service Interfaces

    public interface IServiceRegistry
    {
        Task RegisterAllServicesAsync();
        void Dispose();
    }

    public interface IPlatformService : IAsyncInitializable
    {
        string PlatformName { get; }
        bool SupportsHardwareAcceleration { get; }
        bool SupportsNativeControls { get; }
    }

    public interface IAsyncInitializable
    {
        Task InitializeAsync();
    }

    #endregion
}