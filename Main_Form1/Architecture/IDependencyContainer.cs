// =====================================================
//  DependencyContainer.cs & ViewModelFactory.cs
//  AUTOMATED_REACTOR_CONTROL_Ver5_MODERN
//  100 Lines - Modern IoC Container + Factory Pattern
//  Auto Service Resolution + Lifecycle Management
// =====================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Forms;
using static AUTOMATED_REACTOR_CONTROL_Ver5_MODERN.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver5_MODERN.Services
{
    /// <summary>
    /// 🏗️ Ultra-Modern Dependency Container with Auto Resolution
    /// Features: Singleton Management, Auto-wiring, Lifecycle Control
    /// </summary>
    public interface IDependencyContainer
    {
        void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface;
        void RegisterInstance<T>(T instance);
        T Resolve<T>();
        object Resolve(Type type);
        void Dispose();
    }

    public class DependencyContainer : IDependencyContainer, IDisposable
    {
        #region 🏗️ Container Infrastructure

        private readonly ConcurrentDictionary<Type, ServiceDescriptor> _services;
        private readonly ConcurrentDictionary<Type, object> _singletonInstances;
        private readonly object _lock = new object();
        private bool _disposed = false;

        #endregion

        #region 🚀 Constructor

        public DependencyContainer()
        {
            _services = new ConcurrentDictionary<Type, ServiceDescriptor>();
            _singletonInstances = new ConcurrentDictionary<Type, object>();

            // Register self
            RegisterInstance<IDependencyContainer>(this);

            Logger.Log("🏗️ [DependencyContainer] Ultra-Modern IoC container initialized", LogLevel.Info);
        }

        #endregion

        #region 📝 Registration Methods

        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _services[typeof(TInterface)] = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Singleton);
            Logger.Log($"📝 [DependencyContainer] Singleton registered: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}", LogLevel.Debug);
        }

        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _services[typeof(TInterface)] = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Transient);
            Logger.Log($"📝 [DependencyContainer] Transient registered: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}", LogLevel.Debug);
        }

        public void RegisterInstance<T>(T instance)
        {
            _singletonInstances[typeof(T)] = instance;
            Logger.Log($"📝 [DependencyContainer] Instance registered: {typeof(T).Name}", LogLevel.Debug);
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
                if (_singletonInstances.TryGetValue(type, out var instance))
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
                Logger.Log($"❌ [DependencyContainer] Resolution failed for {type.Name}: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #endregion

        #region 🏭 Instance Creation

        private object CreateInstance(ServiceDescriptor descriptor, Type serviceType)
        {
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                return _singletonInstances.GetOrAdd(serviceType, _ => CreateInstanceWithAutoWiring(descriptor.ImplementationType));
            }
            else
            {
                return CreateInstanceWithAutoWiring(descriptor.ImplementationType);
            }
        }

        private object CreateInstanceWithAutoWiring(Type type)
        {
            lock (_lock)
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
                                args[i] = Resolve(parameters[i].ParameterType);
                            }

                            var instance = Activator.CreateInstance(type, args);
                            Logger.Log($"🏭 [DependencyContainer] Created instance: {type.Name}", LogLevel.Debug);
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
                    Logger.Log($"❌ [DependencyContainer] Instance creation failed for {type.Name}: {ex.Message}", LogLevel.Error);
                    throw;
                }
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
                        Logger.Log($"⚠️ [DependencyContainer] Disposal warning: {ex.Message}", LogLevel.Warn);
                    }
                }

                _singletonInstances.Clear();
                _services.Clear();
                _disposed = true;

                Logger.Log("🗑️ [DependencyContainer] Disposed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DependencyContainer] Disposal error: {ex.Message}", LogLevel.Error);
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
    /// 🏭 Ultra-Modern ViewModelFactory with Auto Resolution
    /// Features: Dynamic View Creation, Legacy Integration, Smart Caching
    /// </summary>
    public interface IViewModelFactory
    {
        UserControl CreateView(string viewName);
        T CreateView<T>() where T : UserControl;
        TViewModel CreateViewModel<TViewModel>() where TViewModel : class;
    }

    public class ViewModelFactory : IViewModelFactory, IDisposable
    {
        #region 🏗️ Factory Infrastructure

        private readonly IDependencyContainer _container;
        private readonly ConcurrentDictionary<string, Type> _viewTypeCache;
        private readonly Assembly _currentAssembly;

        #endregion

        #region 🚀 Constructor

        public ViewModelFactory(IDependencyContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _viewTypeCache = new ConcurrentDictionary<string, Type>();
            _currentAssembly = Assembly.GetExecutingAssembly();

            // Pre-populate view type cache
            InitializeViewTypeCache();

            Logger.Log("🏭 [ViewModelFactory] Ultra-Modern factory initialized", LogLevel.Info);
        }

        #endregion

        #region 🎯 View Creation Methods

        /// <summary>🎯 Create View by Name with Smart Resolution</summary>
        public UserControl CreateView(string viewName)
        {
            try
            {
                // Get view type from cache
                if (!_viewTypeCache.TryGetValue(viewName, out var viewType))
                {
                    Logger.Log($"❌ [ViewModelFactory] View type not found: {viewName}", LogLevel.Error);
                    return CreateFallbackView(viewName);
                }

                // Create view instance with dependency injection
                var view = _container.Resolve(viewType) as UserControl;

                if (view == null)
                {
                    Logger.Log($"❌ [ViewModelFactory] Failed to create view: {viewName}", LogLevel.Error);
                    return CreateFallbackView(viewName);
                }

                Logger.Log($"🎯 [ViewModelFactory] Created view: {viewName}", LogLevel.Debug);
                return view;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewModelFactory] View creation failed for {viewName}: {ex.Message}", LogLevel.Error);
                return CreateFallbackView(viewName);
            }
        }

        /// <summary>🎯 Create View by Type</summary>
        public T CreateView<T>() where T : UserControl
        {
            return _container.Resolve<T>();
        }

        /// <summary>🎯 Create ViewModel with Dependency Injection</summary>
        public TViewModel CreateViewModel<TViewModel>() where TViewModel : class
        {
            return _container.Resolve<TViewModel>();
        }

        #endregion

        #region 🛠️ Utility Methods

        /// <summary>🔍 Initialize View Type Cache</summary>
        private void InitializeViewTypeCache()
        {
            try
            {
                var viewTypes = _currentAssembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(UserControl)) && !t.IsAbstract)
                    .ToList();

                foreach (var type in viewTypes)
                {
                    _viewTypeCache[type.Name] = type;

                    // Register as transient in container
                    var registerMethod = typeof(DependencyContainer)
                        .GetMethod("RegisterTransient")
                        ?.MakeGenericMethod(type, type);

                    registerMethod?.Invoke(_container, null);
                }

                Logger.Log($"🔍 [ViewModelFactory] Cached {viewTypes.Count} view types", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewModelFactory] Cache initialization failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>🛡️ Create Fallback View for Error Cases</summary>
        private UserControl CreateFallbackView(string viewName)
        {
            try
            {
                var fallbackView = new UserControl();
                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = $"⚠️ View '{viewName}' not available\nPlease check system configuration",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    FontSize = 16,
                    Foreground = System.Windows.Media.Brushes.Orange
                };

                fallbackView.Content = textBlock;
                return fallbackView;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewModelFactory] Fallback view creation failed: {ex.Message}", LogLevel.Error);
                return new UserControl(); // Empty fallback
            }
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            try
            {
                _viewTypeCache?.Clear();
                Logger.Log("🗑️ [ViewModelFactory] Disposed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewModelFactory] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }
}