using AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Architecture;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Views.Controls
{
    /// <summary>
    /// 📈 PerformanceDisplay - Real-time Performance Metrics Widget
    /// Shows FPS, Memory, CPU usage in modern compact format
    /// Hardware-accelerated rendering with smooth updates
    /// </summary>
    public class PerformanceDisplay : TemplatedControl
    {
        #region Styled Properties

        public static readonly StyledProperty<double> FpsProperty =
            AvaloniaProperty.Register<PerformanceDisplay, double>(nameof(Fps), 60.0);

        public static readonly StyledProperty<double> MemoryUsageProperty =
            AvaloniaProperty.Register<PerformanceDisplay, double>(nameof(MemoryUsage), 0.0);

        public static readonly StyledProperty<double> CpuUsageProperty =
            AvaloniaProperty.Register<PerformanceDisplay, double>(nameof(CpuUsage), 0.0);

        public static readonly StyledProperty<bool> IsMonitoringProperty =
            AvaloniaProperty.Register<PerformanceDisplay, bool>(nameof(IsMonitoring), true);

        public static readonly StyledProperty<bool> ShowDetailedInfoProperty =
            AvaloniaProperty.Register<PerformanceDisplay, bool>(nameof(ShowDetailedInfo), false);

        public static readonly StyledProperty<PerformanceTheme> ThemeProperty =
            AvaloniaProperty.Register<PerformanceDisplay, PerformanceTheme>(nameof(Theme), PerformanceTheme.Dark);

        #endregion

        #region Properties

        /// <summary>
        /// Current frames per second
        /// </summary>
        public double Fps
        {
            get => GetValue(FpsProperty);
            set => SetValue(FpsProperty, value);
        }

        /// <summary>
        /// Memory usage in MB
        /// </summary>
        public double MemoryUsage
        {
            get => GetValue(MemoryUsageProperty);
            set => SetValue(MemoryUsageProperty, value);
        }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsage
        {
            get => GetValue(CpuUsageProperty);
            set => SetValue(CpuUsageProperty, value);
        }

        /// <summary>
        /// Whether performance monitoring is active
        /// </summary>
        public bool IsMonitoring
        {
            get => GetValue(IsMonitoringProperty);
            set => SetValue(IsMonitoringProperty, value);
        }

        /// <summary>
        /// Show detailed performance information
        /// </summary>
        public bool ShowDetailedInfo
        {
            get => GetValue(ShowDetailedInfoProperty);
            set => SetValue(ShowDetailedInfoProperty, value);
        }

        /// <summary>
        /// Visual theme for the performance display
        /// </summary>
        public PerformanceTheme Theme
        {
            get => GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        #endregion

        #region Private Fields

        private DispatcherTimer? _updateTimer;
        private IPerformanceMonitor? _performanceMonitor;
        private Canvas? _chartCanvas;
        private TextBlock? _fpsText;
        private TextBlock? _memoryText;
        private TextBlock? _cpuText;
        private ProgressBar? _fpsBar;
        private ProgressBar? _memoryBar;
        private ProgressBar? _cpuBar;

        // Performance history for mini-charts
        private readonly double[] _fpsHistory = new double[60];
        private readonly double[] _memoryHistory = new double[60];
        private readonly double[] _cpuHistory = new double[60];
        private int _historyIndex = 0;

        private DateTime _lastUpdate = DateTime.Now;
        private int _frameCount = 0;

        #endregion

        #region Constructor

        static PerformanceDisplay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PerformanceDisplay),
                new FuncStyleKey<PerformanceDisplay>(GetDefaultStyle));

            // Property change handlers
            FpsProperty.Changed.AddClassHandler<PerformanceDisplay>((x, e) => x.OnPerformanceChanged());
            MemoryUsageProperty.Changed.AddClassHandler<PerformanceDisplay>((x, e) => x.OnPerformanceChanged());
            CpuUsageProperty.Changed.AddClassHandler<PerformanceDisplay>((x, e) => x.OnPerformanceChanged());
            IsMonitoringProperty.Changed.AddClassHandler<PerformanceDisplay>((x, e) => x.OnMonitoringChanged());
        }

        public PerformanceDisplay()
        {
            Width = 200;
            Height = 28;

            InitializePerformanceMonitor();
            StartPerformanceTracking();

            Logger.Log("[PerformanceDisplay] 📈 Real-time performance widget initialized", LogLevel.Info);
        }

        #endregion

        #region Initialization

        private void InitializePerformanceMonitor()
        {
            try
            {
                // Get performance monitor from DI container
                var container = App.Current?.Services as IDependencyContainer;
                _performanceMonitor = container?.Resolve<IPerformanceMonitor>();

                if (_performanceMonitor != null)
                {
                    _performanceMonitor.PerformanceUpdated += OnPerformanceMonitorUpdated;
                    Logger.Log("[PerformanceDisplay] ✅ Connected to performance monitor", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] InitializePerformanceMonitor: {ex.Message}", LogLevel.Error);
            }
        }

        private void StartPerformanceTracking()
        {
            try
            {
                _updateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100) // 10 FPS update rate
                };

                _updateTimer.Tick += OnUpdateTick;
                _updateTimer.Start();

                Logger.Log("[PerformanceDisplay] ⏱️ Performance tracking started", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] StartPerformanceTracking: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Template Application

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            try
            {
                // Get template elements
                _chartCanvas = e.NameScope.Find("PART_ChartCanvas") as Canvas;
                _fpsText = e.NameScope.Find("PART_FpsText") as TextBlock;
                _memoryText = e.NameScope.Find("PART_MemoryText") as TextBlock;
                _cpuText = e.NameScope.Find("PART_CpuText") as TextBlock;
                _fpsBar = e.NameScope.Find("PART_FpsBar") as ProgressBar;
                _memoryBar = e.NameScope.Find("PART_MemoryBar") as ProgressBar;
                _cpuBar = e.NameScope.Find("PART_CpuBar") as ProgressBar;

                // Initial update
                UpdateDisplay();

                Logger.Log("[PerformanceDisplay] 🎨 Template applied successfully", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] OnApplyTemplate: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Event Handlers

        private void OnPerformanceMonitorUpdated(object? sender, PerformanceMetrics metrics)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Fps = metrics.FramesPerSecond;
                    MemoryUsage = metrics.MemoryUsageMB;
                    CpuUsage = metrics.CpuUsagePercent;
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] OnPerformanceMonitorUpdated: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnUpdateTick(object? sender, EventArgs e)
        {
            try
            {
                if (!IsMonitoring) return;

                // Update frame counting
                _frameCount++;

                var now = DateTime.Now;
                var elapsed = now - _lastUpdate;

                if (elapsed.TotalSeconds >= 1.0)
                {
                    // Calculate FPS from frame count
                    var measuredFps = _frameCount / elapsed.TotalSeconds;

                    // Update only if we're not getting external FPS data
                    if (_performanceMonitor == null)
                    {
                        Fps = measuredFps;
                    }

                    _frameCount = 0;
                    _lastUpdate = now;
                }

                // Update history for mini-charts
                UpdatePerformanceHistory();

                // Update visual display
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] OnUpdateTick: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnPerformanceChanged()
        {
            try
            {
                UpdateDisplay();
                UpdatePerformanceHistory();
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] OnPerformanceChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnMonitoringChanged()
        {
            try
            {
                if (_updateTimer != null)
                {
                    if (IsMonitoring)
                    {
                        _updateTimer.Start();
                        Logger.Log("[PerformanceDisplay] ▶️ Performance monitoring resumed", LogLevel.Debug);
                    }
                    else
                    {
                        _updateTimer.Stop();
                        Logger.Log("[PerformanceDisplay] ⏸️ Performance monitoring paused", LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] OnMonitoringChanged: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Display Updates

        private void UpdateDisplay()
        {
            try
            {
                if (!IsMonitoring) return;

                UpdateTextDisplays();
                UpdateProgressBars();

                if (ShowDetailedInfo)
                {
                    UpdateMiniCharts();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] UpdateDisplay: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateTextDisplays()
        {
            try
            {
                if (_fpsText != null)
                {
                    _fpsText.Text = $"{Fps:F0} FPS";
                    _fpsText.Foreground = GetFpsColor(Fps);
                }

                if (_memoryText != null)
                {
                    _memoryText.Text = $"{MemoryUsage:F0} MB";
                    _memoryText.Foreground = GetMemoryColor(MemoryUsage);
                }

                if (_cpuText != null)
                {
                    _cpuText.Text = $"{CpuUsage:F0}%";
                    _cpuText.Foreground = GetCpuColor(CpuUsage);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] UpdateTextDisplays: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateProgressBars()
        {
            try
            {
                if (_fpsBar != null)
                {
                    _fpsBar.Value = Math.Min(Fps / 60.0 * 100, 100);
                    _fpsBar.Foreground = GetFpsColor(Fps);
                }

                if (_memoryBar != null)
                {
                    // Assume 1GB as max for percentage calculation
                    _memoryBar.Value = Math.Min(MemoryUsage / 1024.0 * 100, 100);
                    _memoryBar.Foreground = GetMemoryColor(MemoryUsage);
                }

                if (_cpuBar != null)
                {
                    _cpuBar.Value = Math.Min(CpuUsage, 100);
                    _cpuBar.Foreground = GetCpuColor(CpuUsage);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] UpdateProgressBars: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateMiniCharts()
        {
            try
            {
                if (_chartCanvas == null) return;

                _chartCanvas.Children.Clear();

                // Draw mini performance charts
                DrawMiniChart(_chartCanvas, _fpsHistory, Brushes.LimeGreen, 0);
                DrawMiniChart(_chartCanvas, _memoryHistory, Brushes.Orange, 20);
                DrawMiniChart(_chartCanvas, _cpuHistory, Brushes.DeepSkyBlue, 40);
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] UpdateMiniCharts: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Performance History

        private void UpdatePerformanceHistory()
        {
            try
            {
                _fpsHistory[_historyIndex] = Fps;
                _memoryHistory[_historyIndex] = MemoryUsage;
                _cpuHistory[_historyIndex] = CpuUsage;

                _historyIndex = (_historyIndex + 1) % _fpsHistory.Length;
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] UpdatePerformanceHistory: {ex.Message}", LogLevel.Error);
            }
        }

        private void DrawMiniChart(Canvas canvas, double[] history, IBrush brush, double yOffset)
        {
            try
            {
                if (canvas.Bounds.Width <= 0 || canvas.Bounds.Height <= 0) return;

                var width = canvas.Bounds.Width;
                var height = 15; // Chart height
                var points = new List<Point>();

                // Find max value for scaling
                double maxValue = 1;
                for (int i = 0; i < history.Length; i++)
                {
                    maxValue = Math.Max(maxValue, history[i]);
                }

                // Create points for the chart
                for (int i = 0; i < history.Length; i++)
                {
                    var x = (i / (double)(history.Length - 1)) * width;
                    var y = yOffset + height - (history[i] / maxValue * height);
                    points.Add(new Point(x, y));
                }

                // Draw the chart line
                if (points.Count > 1)
                {
                    var geometry = new StreamGeometry();
                    using (var context = geometry.Open())
                    {
                        context.BeginFigure(points[0], false);
                        for (int i = 1; i < points.Count; i++)
                        {
                            context.LineTo(points[i]);
                        }
                    }

                    var path = new Avalonia.Controls.Shapes.Path
                    {
                        Data = geometry,
                        Stroke = brush,
                        StrokeThickness = 1.5
                    };

                    canvas.Children.Add(path);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] DrawMiniChart: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Color Helpers

        private IBrush GetFpsColor(double fps)
        {
            return fps switch
            {
                >= 55 => Brushes.LimeGreen,     // Excellent
                >= 45 => Brushes.Yellow,        // Good
                >= 30 => Brushes.Orange,        // Fair
                _ => Brushes.Red                // Poor
            };
        }

        private IBrush GetMemoryColor(double memory)
        {
            return memory switch
            {
                < 200 => Brushes.LimeGreen,     // Low usage
                < 500 => Brushes.Yellow,        // Moderate usage
                < 800 => Brushes.Orange,        // High usage
                _ => Brushes.Red                // Very high usage
            };
        }

        private IBrush GetCpuColor(double cpu)
        {
            return cpu switch
            {
                < 30 => Brushes.LimeGreen,      // Low usage
                < 60 => Brushes.Yellow,         // Moderate usage
                < 80 => Brushes.Orange,         // High usage
                _ => Brushes.Red                // Very high usage
            };
        }

        #endregion

        #region Default Style

        private static IStyle GetDefaultStyle(PerformanceDisplay control)
        {
            return new Style(x => x.OfType<PerformanceDisplay>())
            {
                Setters =
                {
                    new Setter(TemplateProperty, CreateDefaultTemplate())
                }
            };
        }

        private static IControlTemplate CreateDefaultTemplate()
        {
            return new FuncControlTemplate<PerformanceDisplay>((control, scope) =>
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(230, 45, 45, 48)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4)
                };

                var stackPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 12
                };

                // FPS Display
                var fpsPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical, Spacing = 2 };
                var fpsText = new TextBlock
                {
                    [!TextBlock.TextProperty] = control[!FpsProperty, value => $"{value:F0} FPS"],
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                fpsText.RegisterInNameScope(scope, "PART_FpsText");

                var fpsBar = new ProgressBar
                {
                    Width = 40,
                    Height = 3,
                    Maximum = 100,
                    [!ProgressBar.ValueProperty] = control[!FpsProperty, fps => Math.Min(fps / 60.0 * 100, 100)]
                };
                fpsBar.RegisterInNameScope(scope, "PART_FpsBar");

                fpsPanel.Children.Add(fpsText);
                fpsPanel.Children.Add(fpsBar);

                // Memory Display
                var memoryPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical, Spacing = 2 };
                var memoryText = new TextBlock
                {
                    [!TextBlock.TextProperty] = control[!MemoryUsageProperty, value => $"{value:F0} MB"],
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                memoryText.RegisterInNameScope(scope, "PART_MemoryText");

                var memoryBar = new ProgressBar
                {
                    Width = 40,
                    Height = 3,
                    Maximum = 100,
                    [!ProgressBar.ValueProperty] = control[!MemoryUsageProperty, mem => Math.Min(mem / 1024.0 * 100, 100)]
                };
                memoryBar.RegisterInNameScope(scope, "PART_MemoryBar");

                memoryPanel.Children.Add(memoryText);
                memoryPanel.Children.Add(memoryBar);

                // CPU Display
                var cpuPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical, Spacing = 2 };
                var cpuText = new TextBlock
                {
                    [!TextBlock.TextProperty] = control[!CpuUsageProperty, value => $"{value:F0}%"],
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                cpuText.RegisterInNameScope(scope, "PART_CpuText");

                var cpuBar = new ProgressBar
                {
                    Width = 40,
                    Height = 3,
                    Maximum = 100,
                    [!ProgressBar.ValueProperty] = control[!CpuUsageProperty]
                };
                cpuBar.RegisterInNameScope(scope, "PART_CpuBar");

                cpuPanel.Children.Add(cpuText);
                cpuPanel.Children.Add(cpuBar);

                // Chart Canvas (for detailed view)
                var chartCanvas = new Canvas
                {
                    Width = 120,
                    Height = 60,
                    [!Canvas.IsVisibleProperty] = control[!ShowDetailedInfoProperty]
                };
                chartCanvas.RegisterInNameScope(scope, "PART_ChartCanvas");

                stackPanel.Children.Add(fpsPanel);
                stackPanel.Children.Add(memoryPanel);
                stackPanel.Children.Add(cpuPanel);
                stackPanel.Children.Add(chartCanvas);

                border.Child = stackPanel;
                return border;
            });
        }

        #endregion

        #region Cleanup

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            try
            {
                _updateTimer?.Stop();

                if (_performanceMonitor != null)
                {
                    _performanceMonitor.PerformanceUpdated -= OnPerformanceMonitorUpdated;
                }

                Logger.Log("[PerformanceDisplay] 🧹 Performance display detached and cleaned up", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[PerformanceDisplay][ERROR] OnDetachedFromVisualTree: {ex.Message}", LogLevel.Error);
            }

            base.OnDetachedFromVisualTree(e);
        }

        #endregion
    }

    #region Support Types

    /// <summary>
    /// Visual themes for performance display
    /// </summary>
    public enum PerformanceTheme
    {
        Dark,
        Light,
        HighContrast
    }

    /// <summary>
    /// Performance metrics data structure
    /// </summary>
    public class PerformanceMetrics
    {
        public double FramesPerSecond { get; set; }
        public double MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    #endregion
}