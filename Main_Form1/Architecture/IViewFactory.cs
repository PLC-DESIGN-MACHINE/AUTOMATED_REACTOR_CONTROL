// =====================================================
//  ViewFactory.cs - MODERN VIEW CREATION SYSTEM
//  AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA
//  Complete View Factory with Legacy Integration
//  Auto View Discovery + Smart Caching + Performance Optimization
// =====================================================

using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Architecture;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Architecture;
using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Views;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Utils.Logger;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Services
{
    /// <summary>
    /// 🏭 Modern View Factory - Complete View Creation & Management System
    /// Features: Auto Discovery, Smart Caching, Legacy Integration, Performance Optimization
    /// </summary>
    public class AvaloniaViewFactory : IViewFactory, IDisposable
    {
        #region 🏗️ Factory Infrastructure

        private readonly IDependencyContainer _container;
        private readonly ILegacyAdapter _legacyAdapter;
        private readonly IPerformanceMonitor _performanceMonitor;

        // View Registration & Caching
        private readonly ConcurrentDictionary<string, Type> _viewTypeRegistry;
        private readonly ConcurrentDictionary<string, Type> _viewModelTypeRegistry;
        private readonly ConcurrentDictionary<string, object> _viewCache;
        private readonly ConcurrentDictionary<string, ViewCreationInfo> _creationStats;

        // Legacy Integration
        private readonly HashSet<string> _legacyControlNames;
        private readonly Assembly _currentAssembly;

        // Performance & State
        private volatile bool _isDisposed = false;
        private readonly object _registrationLock = new object();

        #endregion

        #region 🚀 Constructor

        public AvaloniaViewFactory(
            IDependencyContainer container,
            ILegacyAdapter legacyAdapter = null,
            IPerformanceMonitor performanceMonitor = null)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _legacyAdapter = legacyAdapter;
            _performanceMonitor = performanceMonitor;

            _viewTypeRegistry = new ConcurrentDictionary<string, Type>();
            _viewModelTypeRegistry = new ConcurrentDictionary<string, Type>();
            _viewCache = new ConcurrentDictionary<string, object>();
            _creationStats = new ConcurrentDictionary<string, ViewCreationInfo>();
            _legacyControlNames = new HashSet<string>();
            _currentAssembly = Assembly.GetExecutingAssembly();

            // Initialize view discovery
            InitializeViewDiscovery();

            Logger.Log("🏭 [ViewFactory] Modern view factory initialized with auto-discovery", LogLevel.Info);
        }

        #endregion

        #region 🔍 Auto View Discovery - Replaces Manual Registration

        /// <summary>
        /// 🔍 Initialize View Discovery - Automatic View & ViewModel Detection
        /// </summary>
        private void InitializeViewDiscovery()
        {
            try
            {
                Logger.Log("🔍 [ViewFactory] Starting auto view discovery", LogLevel.Info);

                // Discover modern Avalonia views
                DiscoverModernViews();

                // Discover ViewModels
                DiscoverViewModels();

                // Register legacy UserControls
                RegisterLegacyUserControls();

                Logger.Log($"✅ [ViewFactory] Discovery complete - Views: {_viewTypeRegistry.Count}, ViewModels: {_viewModelTypeRegistry.Count}, Legacy: {_legacyControlNames.Count}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] View discovery failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// 🎨 Discover Modern Avalonia Views
        /// </summary>
        private void DiscoverModernViews()
        {
            try
            {
                var viewTypes = _currentAssembly.GetTypes()
                    .Where(t => t.Namespace?.Contains("Views") == true)
                    .Where(t => t.IsSubclassOf(typeof(UserControl)) || t.IsSubclassOf(typeof(Window)))
                    .Where(t => !t.IsAbstract)
                    .ToList();

                foreach (var viewType in viewTypes)
                {
                    var viewName = GetViewName(viewType);
                    _viewTypeRegistry[viewName] = viewType;

                    Logger.Log($"🎨 [ViewFactory] Registered modern view: {viewName} -> {viewType.Name}", LogLevel.Debug);
                }

                Logger.Log($"🎨 [ViewFactory] Discovered {viewTypes.Count} modern views", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] Modern view discovery failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🧠 Discover ViewModels
        /// </summary>
        private void DiscoverViewModels()
        {
            try
            {
                var viewModelTypes = _currentAssembly.GetTypes()
                    .Where(t => t.Namespace?.Contains("ViewModels") == true)
                    .Where(t => t.Name.EndsWith("ViewModel"))
                    .Where(t => !t.IsAbstract)
                    .ToList();

                foreach (var vmType in viewModelTypes)
                {
                    var vmName = vmType.Name.Replace("ViewModel", "");
                    _viewModelTypeRegistry[vmName] = vmType;

                    Logger.Log($"🧠 [ViewFactory] Registered ViewModel: {vmName} -> {vmType.Name}", LogLevel.Debug);
                }

                Logger.Log($"🧠 [ViewFactory] Discovered {viewModelTypes.Count} ViewModels", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] ViewModel discovery failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🔄 Register Legacy UserControls
        /// </summary>
        private void RegisterLegacyUserControls()
        {
            try
            {
                var legacyControls = new[]
                {
                    "UC_CONTROL_SET_1",
                    "UC_CONTROL_SET_2",
                    "UC_PROGRAM_CONTROL_SET_1",
                    "UC_PROGRAM_CONTROL_SET_2",
                    "UC_Setting",
                    "UC_Graph_Data_Set_1",
                    "UC_Graph_Data_Set_2"
                };

                foreach (var controlName in legacyControls)
                {
                    _legacyControlNames.Add(controlName);
                    Logger.Log($"🔄 [ViewFactory] Registered legacy control: {controlName}", LogLevel.Debug);
                }

                Logger.Log($"🔄 [ViewFactory] Registered {legacyControls.Length} legacy controls", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] Legacy control registration failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🏭 Core View Creation Methods

        /// <summary>
        /// 🏭 Create View by Name - Main Factory Method
        /// </summary>
        public async Task<object> CreateViewAsync(string viewName)
        {
            if (string.IsNullOrEmpty(viewName) || _isDisposed)
                return null;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Logger.Log($"🏭 [ViewFactory] Creating view: {viewName}", LogLevel.Info);

                // Check cache first
                if (ShouldUseCache(viewName) && _viewCache.TryGetValue(viewName, out var cachedView))
                {
                    Logger.Log($"📦 [ViewFactory] Using cached view: {viewName}", LogLevel.Debug);
                    RecordCreationStats(viewName, stopwatch.Elapsed, true);
                    return cachedView;
                }

                object view = null;

                // Try to create modern view first
                if (_viewTypeRegistry.TryGetValue(viewName, out var viewType))
                {
                    view = await CreateModernViewAsync(viewType, viewName);
                }
                // Fallback to legacy control
                else if (_legacyControlNames.Contains(viewName))
                {
                    view = await CreateLegacyViewAsync(viewName);
                }
                // Create fallback view
                else
                {
                    view = CreateFallbackView(viewName);
                }

                // Cache if appropriate
                if (view != null && ShouldCache(viewName))
                {
                    _viewCache[viewName] = view;
                    Logger.Log($"💾 [ViewFactory] Cached view: {viewName}", LogLevel.Debug);
                }

                stopwatch.Stop();
                RecordCreationStats(viewName, stopwatch.Elapsed, false);
                _performanceMonitor?.RecordViewCreation(viewName, stopwatch.Elapsed);

                Logger.Log($"✅ [ViewFactory] Created view: {viewName} in {stopwatch.ElapsedMilliseconds}ms", LogLevel.Info);
                return view;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] View creation failed: {viewName} - {ex.Message}", LogLevel.Error);
                return CreateErrorView(viewName, ex);
            }
        }

        /// <summary>
        /// 🏭 Create View by Type - Generic Factory Method
        /// </summary>
        public async Task<TView> CreateViewAsync<TView>() where TView : class
        {
            var viewName = typeof(TView).Name;
            var view = await CreateViewAsync(viewName);
            return view as TView;
        }

        #endregion

        #region 🎨 Modern View Creation

        /// <summary>
        /// 🎨 Create Modern Avalonia View
        /// </summary>
        private async Task<object> CreateModernViewAsync(Type viewType, string viewName)
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // Create view instance
                    var view = _container.Resolve(viewType);

                    // Try to create and assign ViewModel
                    if (view is Control control)
                    {
                        var viewModel = CreateViewModelForView(viewName);
                        if (viewModel != null)
                        {
                            control.DataContext = viewModel;
                            Logger.Log($"🧠 [ViewFactory] Assigned ViewModel to: {viewName}", LogLevel.Debug);
                        }
                    }

                    Logger.Log($"🎨 [ViewFactory] Created modern view: {viewName}", LogLevel.Debug);
                    return view;
                }
                catch (Exception ex)
                {
                    Logger.Log($"❌ [ViewFactory] Modern view creation failed: {ex.Message}", LogLevel.Error);
                    throw;
                }
            });
        }

        /// <summary>
        /// 🧠 Create ViewModel for View
        /// </summary>
        private object CreateViewModelForView(string viewName)
        {
            try
            {
                // Try exact match first
                if (_viewModelTypeRegistry.TryGetValue(viewName, out var vmType))
                {
                    return _container.Resolve(vmType);
                }

                // Try pattern matching
                var possibleVmNames = new[]
                {
                    $"{viewName}ViewModel",
                    viewName.Replace("View", "ViewModel"),
                    viewName.Replace("Control", "ViewModel")
                };

                foreach (var vmName in possibleVmNames)
                {
                    if (_viewModelTypeRegistry.TryGetValue(vmName, out var vmType2))
                    {
                        return _container.Resolve(vmType2);
                    }
                }

                Logger.Log($"⚠️ [ViewFactory] No ViewModel found for: {viewName}", LogLevel.Debug);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] ViewModel creation failed: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        #endregion

        #region 🔄 Legacy View Creation

        /// <summary>
        /// 🔄 Create Legacy UserControl View
        /// </summary>
        private async Task<object> CreateLegacyViewAsync(string viewName)
        {
            try
            {
                if (_legacyAdapter == null)
                {
                    Logger.Log($"⚠️ [ViewFactory] Legacy adapter not available for: {viewName}", LogLevel.Warn);
                    return CreateFallbackView(viewName);
                }

                // Get legacy control type
                var legacyType = GetLegacyControlType(viewName);
                if (legacyType == null)
                {
                    Logger.Log($"❌ [ViewFactory] Legacy type not found: {viewName}", LogLevel.Error);
                    return CreateFallbackView(viewName);
                }

                // Wrap legacy control
                var wrappedView = await _legacyAdapter.WrapLegacyUserControl(legacyType);

                Logger.Log($"🔄 [ViewFactory] Created legacy view: {viewName}", LogLevel.Debug);
                return wrappedView;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] Legacy view creation failed: {ex.Message}", LogLevel.Error);
                return CreateFallbackView(viewName);
            }
        }

        /// <summary>
        /// 🔍 Get Legacy Control Type
        /// </summary>
        private Type GetLegacyControlType(string viewName)
        {
            try
            {
                // Try to find type by name in all loaded assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var type = assembly.GetType($"AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.{viewName}") ??
                                  assembly.GetType(viewName);

                        if (type != null && type.IsSubclassOf(typeof(System.Windows.Forms.UserControl)))
                        {
                            return type;
                        }
                    }
                    catch
                    {
                        // Continue searching in other assemblies
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] Legacy type lookup failed: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        #endregion

        #region 🛡️ Fallback & Error Views

        /// <summary>
        /// 🛡️ Create Fallback View
        /// </summary>
        private object CreateFallbackView(string viewName)
        {
            try
            {
                return new ContentControl
                {
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "🔄 View Loading",
                                FontSize = 18,
                                FontWeight = Avalonia.Media.FontWeight.Bold,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Avalonia.Thickness(0, 0, 0, 10)
                            },
                            new TextBlock
                            {
                                Text = $"Loading view: {viewName}",
                                FontSize = 14,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Gray)
                            },
                            new Border
                            {
                                Width = 200,
                                Height = 4,
                                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.LightBlue),
                                CornerRadius = new Avalonia.CornerRadius(2),
                                Margin = new Avalonia.Thickness(0, 20, 0, 0)
                            }
                        }
                    },
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] Fallback view creation failed: {ex.Message}", LogLevel.Error);
                return new TextBlock { Text = $"Error: {viewName}" };
            }
        }

        /// <summary>
        /// 🚨 Create Error View
        /// </summary>
        private object CreateErrorView(string viewName, Exception error)
        {
            try
            {
                return new ContentControl
                {
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "❌ View Error",
                                FontSize = 18,
                                FontWeight = Avalonia.Media.FontWeight.Bold,
                                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red),
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Avalonia.Thickness(0, 0, 0, 10)
                            },
                            new TextBlock
                            {
                                Text = $"Failed to load: {viewName}",
                                FontSize = 14,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Avalonia.Thickness(0, 0, 0, 5)
                            },
                            new TextBlock
                            {
                                Text = error.Message,
                                FontSize = 12,
                                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Gray),
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                MaxWidth = 400
                            }
                        }
                    },
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
            }
            catch
            {
                return new TextBlock { Text = $"Critical Error: {viewName}" };
            }
        }

        #endregion

        #region 📝 Registration Methods

        /// <summary>
        /// 📝 Register View Manually
        /// </summary>
        public void RegisterView<TView>(string name) where TView : class
        {
            lock (_registrationLock)
            {
                _viewTypeRegistry[name] = typeof(TView);
                Logger.Log($"📝 [ViewFactory] Manually registered view: {name} -> {typeof(TView).Name}", LogLevel.Debug);
            }
        }

        /// <summary>
        /// 📝 Register ViewModel Manually
        /// </summary>
        public void RegisterViewModel<TViewModel>(string name) where TViewModel : class
        {
            lock (_registrationLock)
            {
                _viewModelTypeRegistry[name] = typeof(TViewModel);
                Logger.Log($"📝 [ViewFactory] Manually registered ViewModel: {name} -> {typeof(TViewModel).Name}", LogLevel.Debug);
            }
        }

        /// <summary>
        /// ❓ Is View Registered
        /// </summary>
        public bool IsViewRegistered(string name)
        {
            return _viewTypeRegistry.ContainsKey(name) || _legacyControlNames.Contains(name);
        }

        #endregion

        #region 🧠 Smart Caching Strategy

        /// <summary>
        /// 🧠 Should Use Cache
        /// </summary>
        private bool ShouldUseCache(string viewName)
        {
            // Use cache for frequently accessed views
            var cacheableViews = new[] { "UC_CONTROL_SET_1", "UC_CONTROL_SET_2", "MainWindow" };
            return cacheableViews.Contains(viewName);
        }

        /// <summary>
        /// 💾 Should Cache View
        /// </summary>
        private bool ShouldCache(string viewName)
        {
            // Cache high-priority views
            var highPriorityViews = new[] { "UC_CONTROL_SET_1", "UC_CONTROL_SET_2" };
            return highPriorityViews.Contains(viewName);
        }

        /// <summary>
        /// 🧹 Clear View Cache
        /// </summary>
        public void ClearCache()
        {
            try
            {
                foreach (var view in _viewCache.Values.OfType<IDisposable>())
                {
                    view.Dispose();
                }
                _viewCache.Clear();

                GC.Collect();
                Logger.Log("🧹 [ViewFactory] View cache cleared", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] Cache clearing error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 📊 Performance Monitoring

        /// <summary>
        /// 📊 Record Creation Statistics
        /// </summary>
        private void RecordCreationStats(string viewName, TimeSpan duration, bool fromCache)
        {
            try
            {
                var stats = _creationStats.GetOrAdd(viewName, _ => new ViewCreationInfo());
                stats.TotalCreations++;
                if (fromCache) stats.CacheHits++;
                stats.LastCreationTime = duration;
                stats.AverageCreationTime = TimeSpan.FromTicks((stats.AverageCreationTime.Ticks + duration.Ticks) / 2);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] Stats recording error: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 📈 Get Performance Statistics
        /// </summary>
        public Dictionary<string, ViewCreationInfo> GetPerformanceStats()
        {
            return new Dictionary<string, ViewCreationInfo>(_creationStats);
        }

        #endregion

        #region 🛠️ Utility Methods

        /// <summary>
        /// 🏷️ Get View Name from Type
        /// </summary>
        private string GetViewName(Type viewType)
        {
            // Remove common suffixes
            var name = viewType.Name;
            var suffixes = new[] { "View", "Control", "Window" };

            foreach (var suffix in suffixes)
            {
                if (name.EndsWith(suffix) && name.Length > suffix.Length)
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    break;
                }
            }

            return name;
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                _isDisposed = true;
                ClearCache();

                Logger.Log("🗑️ [ViewFactory] Disposed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ViewFactory] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 📊 Supporting Classes

        /// <summary>
        /// 📊 View Creation Information
        /// </summary>
        public class ViewCreationInfo
        {
            public int TotalCreations { get; set; }
            public int CacheHits { get; set; }
            public TimeSpan LastCreationTime { get; set; }
            public TimeSpan AverageCreationTime { get; set; }
            public double CacheHitRatio => TotalCreations > 0 ? (double)CacheHits / TotalCreations : 0;
        }

        #endregion
    }
}