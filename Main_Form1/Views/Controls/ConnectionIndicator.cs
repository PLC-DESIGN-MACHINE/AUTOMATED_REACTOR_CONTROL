using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Main_Form1.AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Architecture.ModernDesignSystem;

namespace AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Views.Controls
{
    /// <summary>
    /// 📡 ConnectionIndicator - Real-time Connection Status Widget
    /// Shows connection status with animated indicators and detailed tooltips
    /// Features: Multi-connection support, pulse animations, status history
    /// </summary>
    public class ConnectionIndicator : TemplatedControl
    {
        #region Styled Properties

        public static readonly StyledProperty<ConnectionStatus> StatusProperty =
            AvaloniaProperty.Register<ConnectionIndicator, ConnectionStatus>(nameof(Status), ConnectionStatus.Disconnected);

        public static readonly StyledProperty<string> ConnectionNameProperty =
            AvaloniaProperty.Register<ConnectionIndicator, string>(nameof(ConnectionName), "Connection");

        public static readonly StyledProperty<bool> ShowLabelProperty =
            AvaloniaProperty.Register<ConnectionIndicator, bool>(nameof(ShowLabel), true);

        public static readonly StyledProperty<bool> ShowDetailedInfoProperty =
            AvaloniaProperty.Register<ConnectionIndicator, bool>(nameof(ShowDetailedInfo), false);

        public static readonly StyledProperty<bool> EnableAnimationProperty =
            AvaloniaProperty.Register<ConnectionIndicator, bool>(nameof(EnableAnimation), true);

        public static readonly StyledProperty<ConnectionIndicatorSize> IndicatorSizeProperty =
            AvaloniaProperty.Register<ConnectionIndicator, ConnectionIndicatorSize>(nameof(IndicatorSize), ConnectionIndicatorSize.Medium);

        public static readonly StyledProperty<DateTime> LastUpdateTimeProperty =
            AvaloniaProperty.Register<ConnectionIndicator, DateTime>(nameof(LastUpdateTime), DateTime.Now);

        public static readonly StyledProperty<string> StatusMessageProperty =
            AvaloniaProperty.Register<ConnectionIndicator, string>(nameof(StatusMessage), "");

        public static readonly StyledProperty<double> SignalStrengthProperty =
            AvaloniaProperty.Register<ConnectionIndicator, double>(nameof(SignalStrength), 100.0);

        #endregion

        #region Properties

        /// <summary>
        /// Current connection status
        /// </summary>
        public ConnectionStatus Status
        {
            get => GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>
        /// Name of the connection
        /// </summary>
        public string ConnectionName
        {
            get => GetValue(ConnectionNameProperty);
            set => SetValue(ConnectionNameProperty, value);
        }

        /// <summary>
        /// Whether to show connection label
        /// </summary>
        public bool ShowLabel
        {
            get => GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        /// <summary>
        /// Whether to show detailed connection information
        /// </summary>
        public bool ShowDetailedInfo
        {
            get => GetValue(ShowDetailedInfoProperty);
            set => SetValue(ShowDetailedInfoProperty, value);
        }

        /// <summary>
        /// Enable status animations
        /// </summary>
        public bool EnableAnimation
        {
            get => GetValue(EnableAnimationProperty);
            set => SetValue(EnableAnimationProperty, value);
        }

        /// <summary>
        /// Size of the connection indicator
        /// </summary>
        public ConnectionIndicatorSize IndicatorSize
        {
            get => GetValue(IndicatorSizeProperty);
            set => SetValue(IndicatorSizeProperty, value);
        }

        /// <summary>
        /// Last time the connection was updated
        /// </summary>
        public DateTime LastUpdateTime
        {
            get => GetValue(LastUpdateTimeProperty);
            set => SetValue(LastUpdateTimeProperty, value);
        }

        /// <summary>
        /// Status message for detailed info
        /// </summary>
        public string StatusMessage
        {
            get => GetValue(StatusMessageProperty);
            set => SetValue(StatusMessageProperty, value);
        }

        /// <summary>
        /// Signal strength percentage (0-100)
        /// </summary>
        public double SignalStrength
        {
            get => GetValue(SignalStrengthProperty);
            set => SetValue(SignalStrengthProperty, value);
        }

        #endregion

        #region Events

        public event EventHandler<ConnectionStatusChangedEventArgs>? StatusChanged;
        public event EventHandler? IndicatorClicked;

        #endregion

        #region Private Fields

        private Ellipse? _mainIndicator;
        private Ellipse? _pulseIndicator;
        private TextBlock? _labelText;
        private TextBlock? _statusText;
        private StackPanel? _detailPanel;
        private Grid? _signalBars;

        private Animation? _pulseAnimation;
        private Animation? _fadeAnimation;
        private ConnectionStatus _previousStatus;
        private DispatcherTimer? _updateTimer;

        // Status history for trends
        private readonly Queue<ConnectionStatusHistory> _statusHistory = new();
        private const int MaxHistoryEntries = 50;

        #endregion

        #region Constructor

        static ConnectionIndicator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectionIndicator),
                new FuncStyleKey<ConnectionIndicator>(GetDefaultStyle));

            // Property change handlers
            StatusProperty.Changed.AddClassHandler<ConnectionIndicator>((x, e) => x.OnStatusChanged(e));
            IndicatorSizeProperty.Changed.AddClassHandler<ConnectionIndicator>((x, e) => x.OnSizeChanged());
            EnableAnimationProperty.Changed.AddClassHandler<ConnectionIndicator>((x, e) => x.OnAnimationToggled());
        }

        public ConnectionIndicator()
        {
            // Set up click handling
            this.PointerPressed += OnIndicatorClicked;

            // Initialize update timer for real-time info
            InitializeUpdateTimer();

            Logger.Log("[ConnectionIndicator] 📡 Connection status widget initialized", LogLevel.Debug);
        }

        #endregion

        #region Template Application

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            try
            {
                // Get template elements
                _mainIndicator = e.NameScope.Find("PART_MainIndicator") as Ellipse;
                _pulseIndicator = e.NameScope.Find("PART_PulseIndicator") as Ellipse;
                _labelText = e.NameScope.Find("PART_LabelText") as TextBlock;
                _statusText = e.NameScope.Find("PART_StatusText") as TextBlock;
                _detailPanel = e.NameScope.Find("PART_DetailPanel") as StackPanel;
                _signalBars = e.NameScope.Find("PART_SignalBars") as Grid;

                // Initialize animations
                InitializeAnimations();

                // Update initial state
                UpdateIndicatorAppearance();
                UpdateTooltip();

                Logger.Log("[ConnectionIndicator] 🎨 Template applied successfully", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] OnApplyTemplate: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Initialization

        private void InitializeUpdateTimer()
        {
            try
            {
                _updateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _updateTimer.Tick += OnUpdateTimerTick;
                _updateTimer.Start();
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] InitializeUpdateTimer: {ex.Message}", LogLevel.Error);
            }
        }

        private void InitializeAnimations()
        {
            try
            {
                if (!EnableAnimation) return;

                // Pulse animation for active connections
                if (_pulseIndicator != null)
                {
                    _pulseAnimation = new Animation
                    {
                        Duration = TimeSpan.FromSeconds(2),
                        IterationCount = IterationCount.Infinite,
                        Children =
                        {
                            new KeyFrame
                            {
                                Setters =
                                {
                                    new Setter(OpacityProperty, 0.0),
                                    new Setter(Ellipse.WidthProperty, GetIndicatorSize()),
                                    new Setter(Ellipse.HeightProperty, GetIndicatorSize())
                                },
                                Cue = new Cue(0.0)
                            },
                            new KeyFrame
                            {
                                Setters =
                                {
                                    new Setter(OpacityProperty, 0.6),
                                    new Setter(Ellipse.WidthProperty, GetIndicatorSize() * 1.5),
                                    new Setter(Ellipse.HeightProperty, GetIndicatorSize() * 1.5)
                                },
                                Cue = new Cue(0.5)
                            },
                            new KeyFrame
                            {
                                Setters =
                                {
                                    new Setter(OpacityProperty, 0.0),
                                    new Setter(Ellipse.WidthProperty, GetIndicatorSize() * 2.0),
                                    new Setter(Ellipse.HeightProperty, GetIndicatorSize() * 2.0)
                                },
                                Cue = new Cue(1.0)
                            }
                        }
                    };
                }

                // Fade animation for status changes
                _fadeAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(300),
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters = { new Setter(OpacityProperty, 0.3) },
                            Cue = new Cue(0.5)
                        },
                        new KeyFrame
                        {
                            Setters = { new Setter(OpacityProperty, 1.0) },
                            Cue = new Cue(1.0)
                        }
                    }
                };

                Logger.Log("[ConnectionIndicator] 🎬 Animations initialized", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] InitializeAnimations: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Event Handlers

        private void OnStatusChanged(AvaloniaPropertyChangedEventArgs e)
        {
            try
            {
                var newStatus = (ConnectionStatus)e.NewValue;
                var oldStatus = (ConnectionStatus)e.OldValue;

                // Record status change in history
                RecordStatusChange(oldStatus, newStatus);

                // Update appearance
                UpdateIndicatorAppearance();
                UpdateTooltip();

                // Play animation if status changed
                if (EnableAnimation && newStatus != oldStatus)
                {
                    PlayStatusChangeAnimation();
                }

                // Raise event
                StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(oldStatus, newStatus));

                Logger.Log($"[ConnectionIndicator] 📊 Status changed: {oldStatus} → {newStatus}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] OnStatusChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnSizeChanged()
        {
            try
            {
                UpdateIndicatorSize();
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] OnSizeChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnAnimationToggled()
        {
            try
            {
                if (EnableAnimation)
                {
                    InitializeAnimations();
                    if (Status == ConnectionStatus.Connected)
                    {
                        StartPulseAnimation();
                    }
                }
                else
                {
                    StopAllAnimations();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] OnAnimationToggled: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnUpdateTimerTick(object? sender, EventArgs e)
        {
            try
            {
                // Update last update time display
                if (ShowDetailedInfo)
                {
                    UpdateDetailedInfo();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] OnUpdateTimerTick: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnIndicatorClicked(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            try
            {
                IndicatorClicked?.Invoke(this, EventArgs.Empty);
                Logger.Log($"[ConnectionIndicator] 🖱️ Connection indicator clicked: {ConnectionName}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] OnIndicatorClicked: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Visual Updates

        private void UpdateIndicatorAppearance()
        {
            try
            {
                if (_mainIndicator == null) return;

                // Update main indicator color and animation
                var brush = GetStatusBrush(Status);
                _mainIndicator.Fill = brush;

                // Update pulse indicator
                if (_pulseIndicator != null)
                {
                    _pulseIndicator.Fill = brush;
                }

                // Update label text
                if (_labelText != null && ShowLabel)
                {
                    _labelText.Text = ConnectionName;
                }

                // Update status text
                if (_statusText != null)
                {
                    _statusText.Text = GetStatusText(Status);
                    _statusText.Foreground = brush;
                }

                // Control animations based on status
                if (EnableAnimation)
                {
                    if (Status == ConnectionStatus.Connected)
                    {
                        StartPulseAnimation();
                    }
                    else
                    {
                        StopPulseAnimation();
                    }
                }

                // Update signal strength bars
                UpdateSignalStrengthBars();
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] UpdateIndicatorAppearance: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateIndicatorSize()
        {
            try
            {
                var size = GetIndicatorSize();

                if (_mainIndicator != null)
                {
                    _mainIndicator.Width = size;
                    _mainIndicator.Height = size;
                }

                if (_pulseIndicator != null)
                {
                    _pulseIndicator.Width = size;
                    _pulseIndicator.Height = size;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] UpdateIndicatorSize: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateSignalStrengthBars()
        {
            try
            {
                if (_signalBars == null || !ShowDetailedInfo) return;

                _signalBars.Children.Clear();

                // Create signal strength bars
                var barCount = 4;
                var activeBarCount = (int)Math.Ceiling(SignalStrength / 100.0 * barCount);

                for (int i = 0; i < barCount; i++)
                {
                    var bar = new Rectangle
                    {
                        Width = 3,
                        Height = 4 + (i * 2),
                        Fill = i < activeBarCount ? GetStatusBrush(Status) : Brushes.Gray,
                        Margin = new Thickness(1, 0),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom
                    };

                    Grid.SetColumn(bar, i);
                    _signalBars.Children.Add(bar);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] UpdateSignalStrengthBars: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateDetailedInfo()
        {
            try
            {
                if (_detailPanel == null || !ShowDetailedInfo) return;

                // Update detailed information display
                var timeSpan = DateTime.Now - LastUpdateTime;
                var timeText = timeSpan.TotalMinutes < 1
                    ? $"{timeSpan.Seconds}s ago"
                    : $"{timeSpan.Minutes}m ago";

                if (_detailPanel.Children.Count > 0 && _detailPanel.Children[^1] is TextBlock timeBlock)
                {
                    timeBlock.Text = $"Updated: {timeText}";
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] UpdateDetailedInfo: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateTooltip()
        {
            try
            {
                var statusText = GetStatusText(Status);
                var timeAgo = DateTime.Now - LastUpdateTime;
                var tooltip = $"{ConnectionName}: {statusText}";

                if (!string.IsNullOrEmpty(StatusMessage))
                {
                    tooltip += $"\n{StatusMessage}";
                }

                tooltip += $"\nLast update: {timeAgo.TotalSeconds:F0}s ago";

                if (ShowDetailedInfo)
                {
                    tooltip += $"\nSignal strength: {SignalStrength:F0}%";
                }

                ToolTip.SetTip(this, tooltip);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] UpdateTooltip: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Animation Control

        private void PlayStatusChangeAnimation()
        {
            try
            {
                if (_fadeAnimation != null && _mainIndicator != null)
                {
                    _fadeAnimation.RunAsync(_mainIndicator);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] PlayStatusChangeAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        private void StartPulseAnimation()
        {
            try
            {
                if (_pulseAnimation != null && _pulseIndicator != null)
                {
                    _pulseAnimation.RunAsync(_pulseIndicator);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] StartPulseAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        private void StopPulseAnimation()
        {
            try
            {
                // Animation will stop automatically when control is updated
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] StopPulseAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        private void StopAllAnimations()
        {
            try
            {
                // Animations will stop when control is updated
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] StopAllAnimations: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Helper Methods

        private IBrush GetStatusBrush(ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Connected => Brushes.LimeGreen,
                ConnectionStatus.Connecting => Brushes.Orange,
                ConnectionStatus.Disconnected => Brushes.Red,
                ConnectionStatus.Warning => Brushes.Yellow,
                ConnectionStatus.Error => Brushes.DarkRed,
                _ => Brushes.Gray
            };
        }

        private string GetStatusText(ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Connected => "Connected",
                ConnectionStatus.Connecting => "Connecting",
                ConnectionStatus.Disconnected => "Disconnected",
                ConnectionStatus.Warning => "Warning",
                ConnectionStatus.Error => "Error",
                _ => "Unknown"
            };
        }

        private double GetIndicatorSize()
        {
            return IndicatorSize switch
            {
                ConnectionIndicatorSize.Small => 8,
                ConnectionIndicatorSize.Medium => 12,
                ConnectionIndicatorSize.Large => 16,
                _ => 12
            };
        }

        private void RecordStatusChange(ConnectionStatus oldStatus, ConnectionStatus newStatus)
        {
            try
            {
                var historyEntry = new ConnectionStatusHistory
                {
                    Timestamp = DateTime.Now,
                    Status = newStatus,
                    PreviousStatus = oldStatus
                };

                _statusHistory.Enqueue(historyEntry);

                // Keep only recent history
                while (_statusHistory.Count > MaxHistoryEntries)
                {
                    _statusHistory.Dequeue();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] RecordStatusChange: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Update connection status with optional message
        /// </summary>
        public void UpdateStatus(ConnectionStatus status, string? message = null)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Status = status;
                    LastUpdateTime = DateTime.Now;

                    if (message != null)
                    {
                        StatusMessage = message;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] UpdateStatus: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Get connection status history
        /// </summary>
        public IReadOnlyCollection<ConnectionStatusHistory> GetStatusHistory()
        {
            return _statusHistory.ToArray();
        }

        /// <summary>
        /// Force update of all visual elements
        /// </summary>
        public void RefreshIndicator()
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateIndicatorAppearance();
                    UpdateTooltip();
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] RefreshIndicator: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Default Style

        private static IStyle GetDefaultStyle(ConnectionIndicator control)
        {
            return new Style(x => x.OfType<ConnectionIndicator>())
            {
                Setters =
                {
                    new Setter(TemplateProperty, CreateDefaultTemplate())
                }
            };
        }

        private static IControlTemplate CreateDefaultTemplate()
        {
            return new FuncControlTemplate<ConnectionIndicator>((control, scope) =>
            {
                var mainGrid = new Grid();
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                // Indicator container
                var indicatorGrid = new Grid
                {
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                // Pulse indicator (background)
                var pulseIndicator = new Ellipse
                {
                    Name = "PART_PulseIndicator",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                pulseIndicator.RegisterInNameScope(scope, "PART_PulseIndicator");

                // Main indicator
                var mainIndicator = new Ellipse
                {
                    Name = "PART_MainIndicator",
                    [!Ellipse.WidthProperty] = control[!IndicatorSizeProperty, size => GetSizeValue(size)],
                    [!Ellipse.HeightProperty] = control[!IndicatorSizeProperty, size => GetSizeValue(size)],
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                mainIndicator.RegisterInNameScope(scope, "PART_MainIndicator");

                indicatorGrid.Children.Add(pulseIndicator);
                indicatorGrid.Children.Add(mainIndicator);

                Grid.SetColumn(indicatorGrid, 0);
                mainGrid.Children.Add(indicatorGrid);

                // Text panel
                var textPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Vertical,
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    [!StackPanel.IsVisibleProperty] = control[!ShowLabelProperty]
                };

                var labelText = new TextBlock
                {
                    Name = "PART_LabelText",
                    [!TextBlock.TextProperty] = control[!ConnectionNameProperty],
                    FontSize = 11,
                    FontWeight = FontWeight.Medium
                };
                labelText.RegisterInNameScope(scope, "PART_LabelText");

                var statusText = new TextBlock
                {
                    Name = "PART_StatusText",
                    FontSize = 9,
                    Opacity = 0.8
                };
                statusText.RegisterInNameScope(scope, "PART_StatusText");

                textPanel.Children.Add(labelText);
                textPanel.Children.Add(statusText);

                Grid.SetColumn(textPanel, 1);
                mainGrid.Children.Add(textPanel);

                // Detailed info panel (when enabled)
                var detailPanel = new StackPanel
                {
                    Name = "PART_DetailPanel",
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    [!StackPanel.IsVisibleProperty] = control[!ShowDetailedInfoProperty]
                };
                detailPanel.RegisterInNameScope(scope, "PART_DetailPanel");

                // Signal bars
                var signalBars = new Grid
                {
                    Name = "PART_SignalBars",
                    Margin = new Thickness(4, 0, 0, 0)
                };
                signalBars.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                signalBars.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                signalBars.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                signalBars.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                signalBars.RegisterInNameScope(scope, "PART_SignalBars");

                detailPanel.Children.Add(signalBars);

                Grid.SetColumn(detailPanel, 2);
                mainGrid.Children.Add(detailPanel);

                return mainGrid;
            });
        }

        private static double GetSizeValue(ConnectionIndicatorSize size)
        {
            return size switch
            {
                ConnectionIndicatorSize.Small => 8,
                ConnectionIndicatorSize.Medium => 12,
                ConnectionIndicatorSize.Large => 16,
                _ => 12
            };
        }

        #endregion

        #region Cleanup

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            try
            {
                _updateTimer?.Stop();
                StopAllAnimations();

                Logger.Log("[ConnectionIndicator] 🧹 Connection indicator detached and cleaned up", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConnectionIndicator][ERROR] OnDetachedFromVisualTree: {ex.Message}", LogLevel.Error);
            }

            base.OnDetachedFromVisualTree(e);
        }

        #endregion
    }

    #region Support Types

    /// <summary>
    /// Connection status enumeration
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Warning,
        Error
    }

    /// <summary>
    /// Connection indicator sizes
    /// </summary>
    public enum ConnectionIndicatorSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Connection status history entry
    /// </summary>
    public class ConnectionStatusHistory
    {
        public DateTime Timestamp { get; set; }
        public ConnectionStatus Status { get; set; }
        public ConnectionStatus PreviousStatus { get; set; }
    }

    /// <summary>
    /// Event args for connection status changes
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public ConnectionStatus OldStatus { get; }
        public ConnectionStatus NewStatus { get; }
        public DateTime Timestamp { get; }

        public ConnectionStatusChangedEventArgs(ConnectionStatus oldStatus, ConnectionStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}