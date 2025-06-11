using AutomatedReactorControl.Core.Caching;
using AutomatedReactorControl.Core.Memory;
using AutomatedReactorControl.Core.Visualization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Testing.Platform.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AutomatedReactorControl.Core.Dashboard
{
    /// <summary>
    /// Enterprise Custom Dashboard Builder & Management Engine
    /// Features: Real-time multi-chart dashboards, Custom layouts, Theme engine, Export capabilities
    /// Performance: 60fps+ updates, Lazy loading, Virtualized rendering, Auto-scaling
    /// </summary>
    public sealed class UC1_DashboardEngine : BackgroundService, IDisposable
    {
        private readonly ILogger<UC1_DashboardEngine> _logger;
        private readonly DashboardConfiguration _config;
        private readonly UC1_VisualizationService _visualizationService;
        private readonly UC1_CacheManager _cacheManager;
        private readonly UC1_MemoryPool _memoryPool;

        // Dashboard Management
        private readonly ConcurrentDictionary<string, Dashboard> _dashboards;
        private readonly ConcurrentDictionary<string, DashboardLayout> _layouts;
        private readonly ConcurrentDictionary<string, DashboardTheme> _themes;

        // Real-time Updates
        private readonly Channel<DashboardCommand> _commandChannel;
        private readonly ChannelWriter<DashboardCommand> _commandWriter;
        private readonly ChannelReader<DashboardCommand> _commandReader;

        // Event Streams
        private readonly Subject<DashboardEvent> _dashboardEvents;
        private readonly Subject<LayoutChangeEvent> _layoutEvents;
        private readonly Subject<ThemeChangeEvent> _themeEvents;

        // Template & Widget System
        private readonly WidgetFactory _widgetFactory;
        private readonly TemplateEngine _templateEngine;
        private readonly ThemeEngine _themeEngine;

        // Performance & Optimization
        private readonly ViewportManager _viewportManager;
        private readonly LazyLoader _lazyLoader;
        private readonly RenderOptimizer _renderOptimizer;

        // Export & Persistence
        private readonly DashboardExporter _exporter;
        private readonly DashboardPersistence _persistence;

        // Auto-scaling & Responsive Design
        private readonly ResponsiveEngine _responsiveEngine;
        private readonly AutoScaler _autoScaler;

        // Real-time Data Integration
        private readonly DataBindingEngine _dataBindingEngine;
        private readonly RefreshScheduler _refreshScheduler;

        // Monitoring & Analytics
        private readonly DashboardMetrics _metrics;
        private readonly Timer _optimizationTimer;

        private volatile bool _disposed;

        public UC1_DashboardEngine(
            ILogger<UC1_DashboardEngine> logger,
            IOptions<DashboardConfiguration> config,
            UC1_VisualizationService visualizationService,
            UC1_CacheManager cacheManager,
            UC1_MemoryPool memoryPool)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _visualizationService = visualizationService ?? throw new ArgumentNullException(nameof(visualizationService));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _memoryPool = memoryPool ?? throw new ArgumentNullException(nameof(memoryPool));

            // Initialize Collections
            _dashboards = new ConcurrentDictionary<string, Dashboard>();
            _layouts = new ConcurrentDictionary<string, DashboardLayout>();
            _themes = new ConcurrentDictionary<string, DashboardTheme>();

            // Initialize Command Pipeline
            var channelOptions = new BoundedChannelOptions(_config.MaxCommandQueueSize)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            _commandChannel = Channel.CreateBounded<DashboardCommand>(channelOptions);
            _commandWriter = _commandChannel.Writer;
            _commandReader = _commandChannel.Reader;

            // Initialize Event Streams
            _dashboardEvents = new Subject<DashboardEvent>();
            _layoutEvents = new Subject<LayoutChangeEvent>();
            _themeEvents = new Subject<ThemeChangeEvent>();

            // Initialize Core Systems
            _widgetFactory = new WidgetFactory(_visualizationService, _config);
            _templateEngine = new TemplateEngine(_config.TemplateDirectory);
            _themeEngine = new ThemeEngine(_config.ThemeDirectory);

            // Initialize Performance Systems
            _viewportManager = new ViewportManager();
            _lazyLoader = new LazyLoader(_config.EnableLazyLoading);
            _renderOptimizer = new RenderOptimizer(_config.EnableRenderOptimization);

            // Initialize Export & Persistence
            _exporter = new DashboardExporter(_config.ExportDirectory, _memoryPool);
            _persistence = new DashboardPersistence(_config.PersistenceDirectory, _cacheManager);

            // Initialize Responsive & Auto-scaling
            _responsiveEngine = new ResponsiveEngine(_config.ResponsiveBreakpoints);
            _autoScaler = new AutoScaler(_config.EnableAutoScaling);

            // Initialize Data Integration
            _dataBindingEngine = new DataBindingEngine(_visualizationService, _logger);
            _refreshScheduler = new RefreshScheduler(_config.DefaultRefreshInterval);

            // Initialize Monitoring
            _metrics = new DashboardMetrics();
            _optimizationTimer = new Timer(PerformOptimization, null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            // Load default themes and templates
            _ = Task.Run(InitializeDefaultResourcesAsync);

            SetupEventStreams();

            _logger.LogInformation("UC1_DashboardEngine initialized - Max Dashboards: {Max}, Lazy Loading: {Lazy}",
                _config.MaxDashboards, _config.EnableLazyLoading);
        }

        #region Public API

        /// <summary>
        /// Create new dashboard with custom layout
        /// </summary>
        public async Task<string> CreateDashboardAsync(DashboardSpec spec, CancellationToken cancellationToken = default)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            var dashboardId = spec.Id ?? Guid.NewGuid().ToString();

            // Validate dashboard limits
            if (_dashboards.Count >= _config.MaxDashboards)
            {
                throw new InvalidOperationException($"Maximum number of dashboards reached: {_config.MaxDashboards}");
            }

            var dashboard = new Dashboard
            {
                Id = dashboardId,
                Name = spec.Name,
                Description = spec.Description,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Layout = spec.Layout ?? await CreateDefaultLayoutAsync(dashboardId),
                Theme = spec.ThemeId != null ? await GetThemeAsync(spec.ThemeId) : await GetDefaultThemeAsync(),
                Widgets = new Dictionary<string, DashboardWidget>(),
                Configuration = spec.Configuration ?? new DashboardConfig(),
                IsActive = true
            };

            // Create widgets from specification
            if (spec.Widgets != null)
            {
                foreach (var widgetSpec in spec.Widgets)
                {
                    var widget = await _widgetFactory.CreateWidgetAsync(widgetSpec, cancellationToken);
                    dashboard.Widgets[widget.Id] = widget;
                }
            }

            _dashboards.TryAdd(dashboardId, dashboard);

            // Setup data bindings
            await _dataBindingEngine.SetupBindingsAsync(dashboard, cancellationToken);

            // Schedule refresh if auto-refresh is enabled
            if (dashboard.Configuration.AutoRefresh)
            {
                _refreshScheduler.ScheduleDashboard(dashboardId, dashboard.Configuration.RefreshInterval);
            }

            // Cache dashboard configuration
            var cacheKey = $"dashboard:{dashboardId}";
            await _cacheManager.SetAsync(cacheKey, dashboard, TimeSpan.FromHours(1), cancellationToken: cancellationToken);

            // Emit dashboard created event
            _dashboardEvents.OnNext(new DashboardEvent
            {
                Type = DashboardEventType.Created,
                DashboardId = dashboardId,
                Timestamp = DateTime.UtcNow
            });

            _metrics.IncrementDashboardsCreated();
            _logger.LogInformation("Dashboard created: {DashboardId} - {Name}", dashboardId, spec.Name);

            return dashboardId;
        }

        /// <summary>
        /// Add widget to existing dashboard
        /// </summary>
        public async Task<string> AddWidgetAsync(string dashboardId, WidgetSpec widgetSpec, CancellationToken cancellationToken = default)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                throw new ArgumentException($"Dashboard not found: {dashboardId}");

            var widget = await _widgetFactory.CreateWidgetAsync(widgetSpec, cancellationToken);
            dashboard.Widgets[widget.Id] = widget;
            dashboard.LastModified = DateTime.UtcNow;

            // Update layout to include new widget
            await UpdateLayoutWithNewWidgetAsync(dashboard, widget, cancellationToken);

            // Setup data binding for new widget
            await _dataBindingEngine.SetupWidgetBindingAsync(dashboard, widget, cancellationToken);

            // Queue dashboard update command
            await QueueCommandAsync(new DashboardCommand
            {
                Type = DashboardCommandType.AddWidget,
                DashboardId = dashboardId,
                WidgetId = widget.Id,
                Data = widget
            }, cancellationToken);

            _metrics.IncrementWidgetsCreated();
            _logger.LogInformation("Widget added to dashboard {DashboardId}: {WidgetId}", dashboardId, widget.Id);

            return widget.Id;
        }

        /// <summary>
        /// Update dashboard layout with drag-and-drop support
        /// </summary>
        public async Task<bool> UpdateLayoutAsync(string dashboardId, DashboardLayout newLayout, CancellationToken cancellationToken = default)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                return false;

            var oldLayout = dashboard.Layout;
            dashboard.Layout = newLayout;
            dashboard.LastModified = DateTime.UtcNow;

            // Apply responsive adjustments
            var responsiveLayout = await _responsiveEngine.ApplyResponsiveAdjustmentsAsync(newLayout, cancellationToken);
            dashboard.Layout = responsiveLayout;

            // Queue layout update command
            await QueueCommandAsync(new DashboardCommand
            {
                Type = DashboardCommandType.UpdateLayout,
                DashboardId = dashboardId,
                Data = newLayout
            }, cancellationToken);

            // Emit layout change event
            _layoutEvents.OnNext(new LayoutChangeEvent
            {
                DashboardId = dashboardId,
                OldLayout = oldLayout,
                NewLayout = newLayout,
                Timestamp = DateTime.UtcNow
            });

            return true;
        }

        /// <summary>
        /// Apply theme to dashboard with smooth transitions
        /// </summary>
        public async Task<bool> ApplyThemeAsync(string dashboardId, string themeId, CancellationToken cancellationToken = default)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                return false;

            if (!_themes.TryGetValue(themeId, out var theme))
                return false;

            var oldTheme = dashboard.Theme;
            dashboard.Theme = theme;
            dashboard.LastModified = DateTime.UtcNow;

            // Apply theme to all widgets
            foreach (var widget in dashboard.Widgets.Values)
            {
                await ApplyThemeToWidgetAsync(widget, theme, cancellationToken);
            }

            // Queue theme update command
            await QueueCommandAsync(new DashboardCommand
            {
                Type = DashboardCommandType.ApplyTheme,
                DashboardId = dashboardId,
                Data = theme
            }, cancellationToken);

            // Emit theme change event
            _themeEvents.OnNext(new ThemeChangeEvent
            {
                DashboardId = dashboardId,
                OldTheme = oldTheme,
                NewTheme = theme,
                Timestamp = DateTime.UtcNow
            });

            return true;
        }

        /// <summary>
        /// Get dashboard with lazy loading optimization
        /// </summary>
        public async Task<Dashboard> GetDashboardAsync(string dashboardId, bool includeData = true, CancellationToken cancellationToken = default)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                return null;

            if (!includeData || !_config.EnableLazyLoading)
                return dashboard;

            // Apply lazy loading for large dashboards
            var optimizedDashboard = await _lazyLoader.LoadDashboardAsync(dashboard, cancellationToken);
            return optimizedDashboard;
        }

        /// <summary>
        /// Export dashboard to various formats
        /// </summary>
        public async Task<byte[]> ExportDashboardAsync(string dashboardId, ExportFormat format, ExportOptions options = null, CancellationToken cancellationToken = default)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                throw new ArgumentException($"Dashboard not found: {dashboardId}");

            options ??= new ExportOptions();
            return await _exporter.ExportDashboardAsync(dashboard, format, options, cancellationToken);
        }

        /// <summary>
        /// Save dashboard template for reuse
        /// </summary>
        public async Task<string> SaveTemplateAsync(string dashboardId, string templateName, CancellationToken cancellationToken = default)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                throw new ArgumentException($"Dashboard not found: {dashboardId}");

            var template = CreateTemplateFromDashboard(dashboard, templateName);
            var templateId = await _templateEngine.SaveTemplateAsync(template, cancellationToken);

            _logger.LogInformation("Dashboard template saved: {TemplateId} from {DashboardId}", templateId, dashboardId);
            return templateId;
        }

        /// <summary>
        /// Create dashboard from template
        /// </summary>
        public async Task<string> CreateFromTemplateAsync(string templateId, string dashboardName, CancellationToken cancellationToken = default)
        {
            var template = await _templateEngine.LoadTemplateAsync(templateId, cancellationToken);
            if (template == null)
                throw new ArgumentException($"Template not found: {templateId}");

            var spec = CreateSpecFromTemplate(template, dashboardName);
            return await CreateDashboardAsync(spec, cancellationToken);
        }

        /// <summary>
        /// Get real-time dashboard events stream
        /// </summary>
        public IObservable<DashboardEvent> GetDashboardEventsStream() => _dashboardEvents.AsObservable();

        /// <summary>
        /// Get layout change events stream
        /// </summary>
        public IObservable<LayoutChangeEvent> GetLayoutEventsStream() => _layoutEvents.AsObservable();

        /// <summary>
        /// Get theme change events stream
        /// </summary>
        public IObservable<ThemeChangeEvent> GetThemeEventsStream() => _themeEvents.AsObservable();

        /// <summary>
        /// Refresh dashboard data
        /// </summary>
        public async Task<bool> RefreshDashboardAsync(string dashboardId, CancellationToken cancellationToken = default)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                return false;

            await QueueCommandAsync(new DashboardCommand
            {
                Type = DashboardCommandType.Refresh,
                DashboardId = dashboardId,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }

        /// <summary>
        /// Get dashboard performance metrics
        /// </summary>
        public DashboardMetrics GetMetrics() => _metrics.Clone();

        /// <summary>
        /// Remove dashboard and cleanup resources
        /// </summary>
        public async Task<bool> RemoveDashboardAsync(string dashboardId, CancellationToken cancellationToken = default)
        {
            if (!_dashboards.TryRemove(dashboardId, out var dashboard))
                return false;

            // Cleanup widgets
            foreach (var widget in dashboard.Widgets.Values)
            {
                await _widgetFactory.DisposeWidgetAsync(widget, cancellationToken);
            }

            // Cleanup data bindings
            await _dataBindingEngine.CleanupBindingsAsync(dashboardId, cancellationToken);

            // Remove from refresh scheduler
            _refreshScheduler.UnscheduleDashboard(dashboardId);

            // Cleanup cache
            var cacheKeys = new[]
            {
                $"dashboard:{dashboardId}",
                $"dashboard_layout:{dashboardId}",
                $"dashboard_data:{dashboardId}"
            };

            foreach (var key in cacheKeys)
            {
                await _cacheManager.RemoveAsync(key, cancellationToken);
            }

            // Emit dashboard removed event
            _dashboardEvents.OnNext(new DashboardEvent
            {
                Type = DashboardEventType.Removed,
                DashboardId = dashboardId,
                Timestamp = DateTime.UtcNow
            });

            _metrics.IncrementDashboardsRemoved();
            _logger.LogInformation("Dashboard removed: {DashboardId}", dashboardId);

            return true;
        }

        #endregion

        #region Background Processing

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dashboard engine background processing started");

            // Start multiple command processors
            var processorCount = Math.Max(2, Environment.ProcessorCount / 2);
            var processorTasks = Enumerable.Range(0, processorCount)
                .Select(i => ProcessCommandsAsync($"Processor-{i}", stoppingToken))
                .ToArray();

            try
            {
                await Task.WhenAll(processorTasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Dashboard engine background processing cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard engine background processing failed");
                throw;
            }
        }

        private async Task ProcessCommandsAsync(string processorName, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Dashboard command processor {ProcessorName} started", processorName);

            await foreach (var command in _commandReader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var startTime = DateTime.UtcNow;
                    await ProcessCommandAsync(command, cancellationToken);
                    var processingTime = DateTime.UtcNow - startTime;

                    _metrics.RecordCommandProcessingTime(command.Type, processingTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing dashboard command: {CommandType}", command.Type);
                    _metrics.IncrementCommandErrors();
                }
            }

            _logger.LogDebug("Dashboard command processor {ProcessorName} stopped", processorName);
        }

        private async Task ProcessCommandAsync(DashboardCommand command, CancellationToken cancellationToken)
        {
            switch (command.Type)
            {
                case DashboardCommandType.AddWidget:
                    await ProcessAddWidgetCommandAsync(command, cancellationToken);
                    break;

                case DashboardCommandType.UpdateLayout:
                    await ProcessUpdateLayoutCommandAsync(command, cancellationToken);
                    break;

                case DashboardCommandType.ApplyTheme:
                    await ProcessApplyThemeCommandAsync(command, cancellationToken);
                    break;

                case DashboardCommandType.Refresh:
                    await ProcessRefreshCommandAsync(command, cancellationToken);
                    break;

                case DashboardCommandType.UpdateData:
                    await ProcessUpdateDataCommandAsync(command, cancellationToken);
                    break;
            }
        }

        private async Task ProcessAddWidgetCommandAsync(DashboardCommand command, CancellationToken cancellationToken)
        {
            // Process widget addition with render optimization
            await _renderOptimizer.OptimizeWidgetAdditionAsync(command.DashboardId, command.WidgetId, cancellationToken);
        }

        private async Task ProcessUpdateLayoutCommandAsync(DashboardCommand command, CancellationToken cancellationToken)
        {
            // Process layout update with smooth transitions
            var layout = command.Data as DashboardLayout;
            await _renderOptimizer.OptimizeLayoutChangeAsync(command.DashboardId, layout, cancellationToken);
        }

        private async Task ProcessApplyThemeCommandAsync(DashboardCommand command, CancellationToken cancellationToken)
        {
            // Process theme application with transition effects
            var theme = command.Data as DashboardTheme;
            await _renderOptimizer.OptimizeThemeChangeAsync(command.DashboardId, theme, cancellationToken);
        }

        private async Task ProcessRefreshCommandAsync(DashboardCommand command, CancellationToken cancellationToken)
        {
            if (_dashboards.TryGetValue(command.DashboardId, out var dashboard))
            {
                await _dataBindingEngine.RefreshDashboardDataAsync(dashboard, cancellationToken);
            }
        }

        private async Task ProcessUpdateDataCommandAsync(DashboardCommand command, CancellationToken cancellationToken)
        {
            // Process real-time data updates
            await _dataBindingEngine.UpdateWidgetDataAsync(command.DashboardId, command.WidgetId, command.Data, cancellationToken);
        }

        #endregion

        #region Helper Methods

        private async Task QueueCommandAsync(DashboardCommand command, CancellationToken cancellationToken)
        {
            try
            {
                await _commandWriter.WriteAsync(command, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Channel closed, ignore
            }
        }

        private async Task<DashboardLayout> CreateDefaultLayoutAsync(string dashboardId)
        {
            return new DashboardLayout
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default Layout",
                Type = LayoutType.Grid,
                Columns = 12,
                Rows = 8,
                Responsive = true,
                Items = new List<LayoutItem>()
            };
        }

        private async Task<DashboardTheme> GetDefaultThemeAsync()
        {
            const string defaultThemeId = "default";
            if (_themes.TryGetValue(defaultThemeId, out var theme))
                return theme;

            // Create default theme if not exists
            var defaultTheme = CreateDefaultTheme();
            _themes.TryAdd(defaultThemeId, defaultTheme);
            return defaultTheme;
        }

        private DashboardTheme CreateDefaultTheme()
        {
            return new DashboardTheme
            {
                Id = "default",
                Name = "Default Theme",
                Colors = new ThemeColors
                {
                    Primary = "#2196F3",
                    Secondary = "#FFC107",
                    Background = "#FFFFFF",
                    Surface = "#F5F5F5",
                    Text = "#212121"
                },
                Typography = new ThemeTypography
                {
                    FontFamily = "Roboto, sans-serif",
                    FontSizes = new Dictionary<string, int>
                    {
                        ["small"] = 12,
                        ["medium"] = 14,
                        ["large"] = 16,
                        ["xlarge"] = 20
                    }
                },
                Spacing = new ThemeSpacing
                {
                    Unit = 8,
                    Small = 4,
                    Medium = 8,
                    Large = 16,
                    XLarge = 24
                }
            };
        }

        private async Task<DashboardTheme> GetThemeAsync(string themeId)
        {
            if (_themes.TryGetValue(themeId, out var theme))
                return theme;

            // Try to load from theme engine
            theme = await _themeEngine.LoadThemeAsync(themeId);
            if (theme != null)
            {
                _themes.TryAdd(themeId, theme);
            }

            return theme;
        }

        private async Task UpdateLayoutWithNewWidgetAsync(Dashboard dashboard, DashboardWidget widget, CancellationToken cancellationToken)
        {
            // Find available space in layout
            var availablePosition = FindAvailablePosition(dashboard.Layout);

            var layoutItem = new LayoutItem
            {
                Id = widget.Id,
                X = availablePosition.X,
                Y = availablePosition.Y,
                Width = widget.Configuration.DefaultWidth,
                Height = widget.Configuration.DefaultHeight,
                MinWidth = widget.Configuration.MinWidth,
                MinHeight = widget.Configuration.MinHeight
            };

            dashboard.Layout.Items.Add(layoutItem);
        }

        private (int X, int Y) FindAvailablePosition(DashboardLayout layout)
        {
            // Simple algorithm to find next available position
            for (int y = 0; y < layout.Rows; y++)
            {
                for (int x = 0; x < layout.Columns; x++)
                {
                    if (!IsPositionOccupied(layout, x, y))
                    {
                        return (x, y);
                    }
                }
            }

            // If no space available, add new row
            return (0, layout.Rows);
        }

        private bool IsPositionOccupied(DashboardLayout layout, int x, int y)
        {
            return layout.Items.Any(item =>
                x >= item.X && x < item.X + item.Width &&
                y >= item.Y && y < item.Y + item.Height);
        }

        private async Task ApplyThemeToWidgetAsync(DashboardWidget widget, DashboardTheme theme, CancellationToken cancellationToken)
        {
            // Apply theme colors and styling to widget
            widget.Style.BackgroundColor = theme.Colors.Background;
            widget.Style.TextColor = theme.Colors.Text;
            widget.Style.BorderColor = theme.Colors.Primary;
            widget.Style.FontFamily = theme.Typography.FontFamily;
        }

        private DashboardTemplate CreateTemplateFromDashboard(Dashboard dashboard, string templateName)
        {
            return new DashboardTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = templateName,
                Description = $"Template created from dashboard '{dashboard.Name}'",
                Layout = dashboard.Layout,
                Theme = dashboard.Theme,
                WidgetSpecs = dashboard.Widgets.Values.Select(w => new WidgetSpec
                {
                    Type = w.Type,
                    Configuration = w.Configuration,
                    DataSource = w.DataSource
                }).ToList(),
                CreatedAt = DateTime.UtcNow
            };
        }

        private DashboardSpec CreateSpecFromTemplate(DashboardTemplate template, string dashboardName)
        {
            return new DashboardSpec
            {
                Name = dashboardName,
                Description = $"Dashboard created from template '{template.Name}'",
                Layout = template.Layout,
                ThemeId = template.Theme?.Id,
                Widgets = template.WidgetSpecs,
                Configuration = new DashboardConfig
                {
                    AutoRefresh = true,
                    RefreshInterval = TimeSpan.FromSeconds(30)
                }
            };
        }

        private void SetupEventStreams()
        {
            // Setup dashboard event aggregation
            _dashboardEvents
                .GroupBy(evt => evt.DashboardId)
                .Subscribe(group =>
                {
                    group.Buffer(TimeSpan.FromSeconds(1))
                         .Subscribe(async events =>
                         {
                             if (events.Any())
                             {
                                 await PersistDashboardEvents(group.Key, events);
                             }
                         });
                });

            // Setup auto-save for layout changes
            _layoutEvents
                .Throttle(TimeSpan.FromSeconds(5))
                .Subscribe(async evt =>
                {
                    await _persistence.SaveLayoutAsync(evt.DashboardId, evt.NewLayout);
                });

            // Setup theme change persistence
            _themeEvents
                .Subscribe(async evt =>
                {
                    await _persistence.SaveThemePreferenceAsync(evt.DashboardId, evt.NewTheme.Id);
                });
        }

        private async Task PersistDashboardEvents(string dashboardId, IList<DashboardEvent> events)
        {
            try
            {
                await _persistence.SaveEventsAsync(dashboardId, events);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist dashboard events for {DashboardId}", dashboardId);
            }
        }

        private async Task InitializeDefaultResourcesAsync()
        {
            try
            {
                // Load default themes
                await _themeEngine.LoadDefaultThemesAsync();

                // Load available templates
                await _templateEngine.LoadAvailableTemplatesAsync();

                _logger.LogInformation("Default dashboard resources initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize default dashboard resources");
            }
        }

        private void PerformOptimization(object state)
        {
            if (_disposed) return;

            try
            {
                // Optimize inactive dashboards
                var inactiveDashboards = _dashboards.Values
                    .Where(d => DateTime.UtcNow - d.LastAccessed > TimeSpan.FromMinutes(30))
                    .ToList();

                foreach (var dashboard in inactiveDashboards)
                {
                    _ = Task.Run(() => OptimizeDashboardAsync(dashboard));
                }

                // Update metrics
                _metrics.UpdateOptimizationMetrics();

                _logger.LogDebug("Dashboard optimization completed - Processed {Count} inactive dashboards",
                    inactiveDashboards.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during dashboard optimization");
            }
        }

        private async Task OptimizeDashboardAsync(Dashboard dashboard)
        {
            try
            {
                // Apply lazy loading optimization
                await _lazyLoader.OptimizeDashboardAsync(dashboard);

                // Optimize render performance
                await _renderOptimizer.OptimizeDashboardRenderingAsync(dashboard);

                // Update cache with optimized state
                var cacheKey = $"dashboard:{dashboard.Id}";
                await _cacheManager.SetAsync(cacheKey, dashboard, TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to optimize dashboard {DashboardId}", dashboard.Id);
            }
        }

        #endregion

        #region IDisposable

        public override void Dispose()
        {
            if (_disposed) return;

            _commandWriter.Complete();
            _optimizationTimer?.Dispose();

            // Dispose event streams
            _dashboardEvents?.Dispose();
            _layoutEvents?.Dispose();
            _themeEvents?.Dispose();

            // Dispose core systems
            _widgetFactory?.Dispose();
            _templateEngine?.Dispose();
            _themeEngine?.Dispose();
            _exporter?.Dispose();
            _persistence?.Dispose();
            _dataBindingEngine?.Dispose();
            _refreshScheduler?.Dispose();

            // Dispose all dashboards
            foreach (var dashboard in _dashboards.Values)
            {
                foreach (var widget in dashboard.Widgets.Values)
                {
                    widget.Dispose();
                }
            }

            base.Dispose();
            _disposed = true;

            _logger.LogInformation("UC1_DashboardEngine disposed");
        }

        #endregion
    }

    #region Data Models and Enums

    public enum DashboardEventType
    {
        Created, Updated, Removed, Refreshed, LayoutChanged, ThemeChanged
    }

    public enum DashboardCommandType
    {
        AddWidget, RemoveWidget, UpdateLayout, ApplyTheme, Refresh, UpdateData
    }

    public enum LayoutType
    {
        Grid, Flexbox, Absolute, Masonry
    }

    public enum WidgetType
    {
        Chart, Metric, Table, Text, Image, Custom
    }

    public class Dashboard
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
        public DashboardLayout Layout { get; set; }
        public DashboardTheme Theme { get; set; }
        public Dictionary<string, DashboardWidget> Widgets { get; set; }
        public DashboardConfig Configuration { get; set; }
        public bool IsActive { get; set; }
    }

    public class DashboardSpec
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DashboardLayout Layout { get; set; }
        public string ThemeId { get; set; }
        public List<WidgetSpec> Widgets { get; set; }
        public DashboardConfig Configuration { get; set; }
    }

    public class DashboardLayout
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public LayoutType Type { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
        public bool Responsive { get; set; }
        public List<LayoutItem> Items { get; set; }
    }

    public class LayoutItem
    {
        public string Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }
        public bool Resizable { get; set; } = true;
        public bool Draggable { get; set; } = true;
    }

    public class DashboardWidget : IDisposable
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public WidgetType Type { get; set; }
        public WidgetConfiguration Configuration { get; set; }
        public WidgetStyle Style { get; set; }
        public DataSource DataSource { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsVisible { get; set; } = true;

        public void Dispose()
        {
            // Cleanup widget resources
        }
    }

    public class WidgetSpec
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public WidgetType Type { get; set; }
        public WidgetConfiguration Configuration { get; set; }
        public DataSource DataSource { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class WidgetConfiguration
    {
        public int DefaultWidth { get; set; } = 4;
        public int DefaultHeight { get; set; } = 3;
        public int MinWidth { get; set; } = 2;
        public int MinHeight { get; set; } = 2;
        public bool AutoRefresh { get; set; } = true;
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class WidgetStyle
    {
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public string BorderColor { get; set; }
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public Dictionary<string, object> CustomStyles { get; set; } = new();
    }

    public class DataSource
    {
        public string Type { get; set; }
        public string ConnectionString { get; set; }
        public string Query { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    public class DashboardTheme
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ThemeColors Colors { get; set; }
        public ThemeTypography Typography { get; set; }
        public ThemeSpacing Spacing { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; } = new();
    }

    public class ThemeColors
    {
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Background { get; set; }
        public string Surface { get; set; }
        public string Text { get; set; }
        public string Error { get; set; }
        public string Warning { get; set; }
        public string Success { get; set; }
    }

    public class ThemeTypography
    {
        public string FontFamily { get; set; }
        public Dictionary<string, int> FontSizes { get; set; }
        public Dictionary<string, string> FontWeights { get; set; } = new();
    }

    public class ThemeSpacing
    {
        public int Unit { get; set; }
        public int Small { get; set; }
        public int Medium { get; set; }
        public int Large { get; set; }
        public int XLarge { get; set; }
    }

    public class DashboardConfig
    {
        public bool AutoRefresh { get; set; } = false;
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(1);
        public bool EnableAnimations { get; set; } = true;
        public bool EnableTooltips { get; set; } = true;
        public bool AllowEditing { get; set; } = true;
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public class DashboardTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DashboardLayout Layout { get; set; }
        public DashboardTheme Theme { get; set; }
        public List<WidgetSpec> WidgetSpecs { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DashboardCommand
    {
        public DashboardCommandType Type { get; set; }
        public string DashboardId { get; set; }
        public string WidgetId { get; set; }
        public object Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Event Classes
    public class DashboardEvent
    {
        public DashboardEventType Type { get; set; }
        public string DashboardId { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class LayoutChangeEvent
    {
        public string DashboardId { get; set; }
        public DashboardLayout OldLayout { get; set; }
        public DashboardLayout NewLayout { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ThemeChangeEvent
    {
        public string DashboardId { get; set; }
        public DashboardTheme OldTheme { get; set; }
        public DashboardTheme NewTheme { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DashboardMetrics
    {
        private long _dashboardsCreated, _dashboardsRemoved, _widgetsCreated, _commandErrors;
        private readonly ConcurrentDictionary<DashboardCommandType, List<TimeSpan>> _commandTimes = new();

        public long DashboardsCreated => _dashboardsCreated;
        public long DashboardsRemoved => _dashboardsRemoved;
        public long WidgetsCreated => _widgetsCreated;
        public long CommandErrors => _commandErrors;

        public void IncrementDashboardsCreated() => Interlocked.Increment(ref _dashboardsCreated);
        public void IncrementDashboardsRemoved() => Interlocked.Increment(ref _dashboardsRemoved);
        public void IncrementWidgetsCreated() => Interlocked.Increment(ref _widgetsCreated);
        public void IncrementCommandErrors() => Interlocked.Increment(ref _commandErrors);

        public void RecordCommandProcessingTime(DashboardCommandType commandType, TimeSpan processingTime)
        {
            _commandTimes.AddOrUpdate(commandType,
                new List<TimeSpan> { processingTime },
                (k, v) =>
                {
                    lock (v)
                    {
                        v.Add(processingTime);
                        if (v.Count > 100) v.RemoveAt(0);
                    }
                    return v;
                });
        }

        public void UpdateOptimizationMetrics()
        {
            // Update optimization-related metrics
        }

        public DashboardMetrics Clone() => (DashboardMetrics)MemberwiseClone();
    }

    public class DashboardConfiguration
    {
        public int MaxDashboards { get; set; } = 100;
        public int MaxCommandQueueSize { get; set; } = 10000;
        public bool EnableLazyLoading { get; set; } = true;
        public bool EnableRenderOptimization { get; set; } = true;
        public bool EnableAutoScaling { get; set; } = true;
        public string TemplateDirectory { get; set; } = "Templates";
        public string ThemeDirectory { get; set; } = "Themes";
        public string ExportDirectory { get; set; } = "Exports";
        public string PersistenceDirectory { get; set; } = "Persistence";
        public TimeSpan DefaultRefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
        public Dictionary<string, int> ResponsiveBreakpoints { get; set; } = new()
        {
            ["xs"] = 480,
            ["sm"] = 768,
            ["md"] = 1024,
            ["lg"] = 1200,
            ["xl"] = 1440
        };
    }

    #endregion

    #region Infrastructure Classes (Simplified Implementations)

    // These are placeholder implementations for the supporting infrastructure

    internal class WidgetFactory : IDisposable
    {
        public WidgetFactory(UC1_VisualizationService visualizationService, DashboardConfiguration config) { }
        public async Task<DashboardWidget> CreateWidgetAsync(WidgetSpec spec, CancellationToken cancellationToken)
        {
            return new DashboardWidget
            {
                Id = spec.Id ?? Guid.NewGuid().ToString(),
                Title = spec.Title,
                Type = spec.Type,
                Configuration = spec.Configuration ?? new WidgetConfiguration(),
                Style = new WidgetStyle(),
                DataSource = spec.DataSource,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
        }
        public async Task DisposeWidgetAsync(DashboardWidget widget, CancellationToken cancellationToken) { }
        public void Dispose() { }
    }

    internal class TemplateEngine : IDisposable
    {
        public TemplateEngine(string templateDirectory) { }
        public async Task<string> SaveTemplateAsync(DashboardTemplate template, CancellationToken cancellationToken) => template.Id;
        public async Task<DashboardTemplate> LoadTemplateAsync(string templateId, CancellationToken cancellationToken) => null;
        public async Task LoadAvailableTemplatesAsync() { }
        public void Dispose() { }
    }

    internal class ThemeEngine : IDisposable
    {
        public ThemeEngine(string themeDirectory) { }
        public async Task<DashboardTheme> LoadThemeAsync(string themeId) => null;
        public async Task LoadDefaultThemesAsync() { }
        public void Dispose() { }
    }

    internal class ViewportManager
    {
        // Viewport management implementation
    }

    internal class LazyLoader
    {
        public LazyLoader(bool enabled) { }
        public async Task<Dashboard> LoadDashboardAsync(Dashboard dashboard, CancellationToken cancellationToken) => dashboard;
        public async Task OptimizeDashboardAsync(Dashboard dashboard) { }
    }

    internal class RenderOptimizer
    {
        public RenderOptimizer(bool enabled) { }
        public async Task OptimizeWidgetAdditionAsync(string dashboardId, string widgetId, CancellationToken cancellationToken) { }
        public async Task OptimizeLayoutChangeAsync(string dashboardId, DashboardLayout layout, CancellationToken cancellationToken) { }
        public async Task OptimizeThemeChangeAsync(string dashboardId, DashboardTheme theme, CancellationToken cancellationToken) { }
        public async Task OptimizeDashboardRenderingAsync(Dashboard dashboard) { }
    }

    internal class DashboardExporter
    {
        public DashboardExporter(string exportDirectory, UC1_MemoryPool memoryPool) { }
        public async Task<byte[]> ExportDashboardAsync(Dashboard dashboard, ExportFormat format, ExportOptions options, CancellationToken cancellationToken)
        {
            return new byte[1024]; // Placeholder
        }
    }

    internal class DashboardPersistence : IDisposable
    {
        public DashboardPersistence(string persistenceDirectory, UC1_CacheManager cacheManager) { }
        public async Task SaveLayoutAsync(string dashboardId, DashboardLayout layout) { }
        public async Task SaveThemePreferenceAsync(string dashboardId, string themeId) { }
        public async Task SaveEventsAsync(string dashboardId, IList<DashboardEvent> events) { }
        public void Dispose() { }
    }

    internal class ResponsiveEngine
    {
        public ResponsiveEngine(Dictionary<string, int> breakpoints) { }
        public async Task<DashboardLayout> ApplyResponsiveAdjustmentsAsync(DashboardLayout layout, CancellationToken cancellationToken) => layout;
    }

    internal class AutoScaler
    {
        public AutoScaler(bool enabled) { }
    }

    internal class DataBindingEngine : IDisposable
    {
        public DataBindingEngine(UC1_VisualizationService visualizationService, ILogger logger) { }
        public async Task SetupBindingsAsync(Dashboard dashboard, CancellationToken cancellationToken) { }
        public async Task SetupWidgetBindingAsync(Dashboard dashboard, DashboardWidget widget, CancellationToken cancellationToken) { }
        public async Task CleanupBindingsAsync(string dashboardId, CancellationToken cancellationToken) { }
        public async Task RefreshDashboardDataAsync(Dashboard dashboard, CancellationToken cancellationToken) { }
        public async Task UpdateWidgetDataAsync(string dashboardId, string widgetId, object data, CancellationToken cancellationToken) { }
        public void Dispose() { }
    }

    internal class RefreshScheduler : IDisposable
    {
        public RefreshScheduler(TimeSpan defaultInterval) { }
        public void ScheduleDashboard(string dashboardId, TimeSpan interval) { }
        public void UnscheduleDashboard(string dashboardId) { }
        public void Dispose() { }
    }

    #endregion
}