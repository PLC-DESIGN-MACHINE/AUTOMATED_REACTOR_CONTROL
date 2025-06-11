using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Views.Controls
{
    /// <summary>
    /// 🔄 LoadingOverlay - Modern Loading Animation with Progress
    /// Hardware-accelerated spinner with glassmorphism effect
    /// Features: Smooth animations, progress tracking, cancellation support
    /// </summary>
    public class LoadingOverlay : TemplatedControl
    {
        #region Styled Properties

        public static readonly StyledProperty<bool> IsVisibleProperty =
            AvaloniaProperty.Register<LoadingOverlay, bool>(nameof(IsVisible), false);

        public static readonly StyledProperty<string> LoadingTextProperty =
            AvaloniaProperty.Register<LoadingOverlay, string>(nameof(LoadingText), "Loading...");

        public static readonly StyledProperty<double> ProgressProperty =
            AvaloniaProperty.Register<LoadingOverlay, double>(nameof(Progress), -1);

        public static readonly StyledProperty<bool> ShowProgressProperty =
            AvaloniaProperty.Register<LoadingOverlay, bool>(nameof(ShowProgress), false);

        public static readonly StyledProperty<bool> ShowCancelButtonProperty =
            AvaloniaProperty.Register<LoadingOverlay, bool>(nameof(ShowCancelButton), false);

        public static readonly StyledProperty<LoadingStyle> StyleProperty =
            AvaloniaProperty.Register<LoadingOverlay, LoadingStyle>(nameof(Style), LoadingStyle.Modern);

        public static readonly StyledProperty<IBrush> AccentColorProperty =
            AvaloniaProperty.Register<LoadingOverlay, IBrush>(nameof(AccentColor), Brushes.DodgerBlue);

        #endregion

        #region Properties

        /// <summary>
        /// Controls visibility of the loading overlay
        /// </summary>
        public new bool IsVisible
        {
            get => GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        /// <summary>
        /// Text to display during loading
        /// </summary>
        public string LoadingText
        {
            get => GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }

        /// <summary>
        /// Progress value (0-100, -1 for indeterminate)
        /// </summary>
        public double Progress
        {
            get => GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        /// <summary>
        /// Whether to show progress bar
        /// </summary>
        public bool ShowProgress
        {
            get => GetValue(ShowProgressProperty);
            set => SetValue(ShowProgressProperty, value);
        }

        /// <summary>
        /// Whether to show cancel button
        /// </summary>
        public bool ShowCancelButton
        {
            get => GetValue(ShowCancelButtonProperty);
            set => SetValue(ShowCancelButtonProperty, value);
        }

        /// <summary>
        /// Visual style of the loading overlay
        /// </summary>
        public new LoadingStyle Style
        {
            get => GetValue(StyleProperty);
            set => SetValue(StyleProperty, value);
        }

        /// <summary>
        /// Accent color for the loading animation
        /// </summary>
        public IBrush AccentColor
        {
            get => GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        #endregion

        #region Events

        public event EventHandler? CancelRequested;
        public event EventHandler<LoadingStateChangedEventArgs>? LoadingStateChanged;

        #endregion

        #region Private Fields

        private Border? _backgroundBorder;
        private Canvas? _spinnerCanvas;
        private TextBlock? _loadingTextBlock;
        private ProgressBar? _progressBar;
        private Button? _cancelButton;
        private Grid? _contentGrid;

        private Animation? _spinnerAnimation;
        private Animation? _pulseAnimation;
        private bool _isAnimating;
        private DateTime _loadingStartTime;

        // Hardware acceleration elements
        private CompositeTransform? _spinnerTransform;

        #endregion

        #region Constructor

        static LoadingOverlay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LoadingOverlay),
                new FuncStyleKey<LoadingOverlay>(GetDefaultStyle));

            // Property change handlers
            IsVisibleProperty.Changed.AddClassHandler<LoadingOverlay>((x, e) => x.OnVisibilityChanged(e));
            ProgressProperty.Changed.AddClassHandler<LoadingOverlay>((x, e) => x.OnProgressChanged());
            LoadingTextProperty.Changed.AddClassHandler<LoadingOverlay>((x, e) => x.OnLoadingTextChanged());
        }

        public LoadingOverlay()
        {
            // Set default properties
            this.IsHitTestVisible = false; // Allow clicks through when not visible
            this.ZIndex = 1000; // Always on top

            Logger.Log("[LoadingOverlay] 🔄 Modern loading overlay initialized", LogLevel.Debug);
        }

        #endregion

        #region Template Application

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            try
            {
                // Get template elements
                _backgroundBorder = e.NameScope.Find("PART_BackgroundBorder") as Border;
                _spinnerCanvas = e.NameScope.Find("PART_SpinnerCanvas") as Canvas;
                _loadingTextBlock = e.NameScope.Find("PART_LoadingText") as TextBlock;
                _progressBar = e.NameScope.Find("PART_ProgressBar") as ProgressBar;
                _cancelButton = e.NameScope.Find("PART_CancelButton") as Button;
                _contentGrid = e.NameScope.Find("PART_ContentGrid") as Grid;

                // Setup event handlers
                if (_cancelButton != null)
                {
                    _cancelButton.Click += OnCancelButtonClick;
                }

                // Initialize animations
                InitializeAnimations();

                // Update initial state
                UpdateVisibility();
                UpdateProgress();
                UpdateLoadingText();

                Logger.Log("[LoadingOverlay] 🎨 Template applied successfully", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] OnApplyTemplate: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Animation Setup

        private void InitializeAnimations()
        {
            try
            {
                if (_spinnerCanvas != null)
                {
                    // Setup spinner transform for hardware acceleration
                    _spinnerTransform = new CompositeTransform
                    {
                        CenterX = 24, // Half of spinner size
                        CenterY = 24
                    };
                    _spinnerCanvas.RenderTransform = _spinnerTransform;

                    // Create continuous spinner animation
                    CreateSpinnerAnimation();

                    // Create pulse animation for modern style
                    CreatePulseAnimation();
                }

                Logger.Log("[LoadingOverlay] 🎬 Animations initialized", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] InitializeAnimations: {ex.Message}", LogLevel.Error);
            }
        }

        private void CreateSpinnerAnimation()
        {
            try
            {
                if (_spinnerTransform == null) return;

                _spinnerAnimation = new Animation
                {
                    Duration = TimeSpan.FromSeconds(1),
                    IterationCount = IterationCount.Infinite,
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters = { new Setter(CompositeTransform.RotationProperty, 0.0) },
                            Cue = new Cue(0.0)
                        },
                        new KeyFrame
                        {
                            Setters = { new Setter(CompositeTransform.RotationProperty, 360.0) },
                            Cue = new Cue(1.0)
                        }
                    }
                };

                Logger.Log("[LoadingOverlay] 🌀 Spinner animation created", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] CreateSpinnerAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        private void CreatePulseAnimation()
        {
            try
            {
                if (_contentGrid == null) return;

                _pulseAnimation = new Animation
                {
                    Duration = TimeSpan.FromSeconds(2),
                    IterationCount = IterationCount.Infinite,
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters = { new Setter(OpacityProperty, 0.8) },
                            Cue = new Cue(0.0)
                        },
                        new KeyFrame
                        {
                            Setters = { new Setter(OpacityProperty, 1.0) },
                            Cue = new Cue(0.5)
                        },
                        new KeyFrame
                        {
                            Setters = { new Setter(OpacityProperty, 0.8) },
                            Cue = new Cue(1.0)
                        }
                    }
                };

                Logger.Log("[LoadingOverlay] 💫 Pulse animation created", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] CreatePulseAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Event Handlers

        private async void OnVisibilityChanged(AvaloniaPropertyChangedEventArgs e)
        {
            try
            {
                var isVisible = (bool)e.NewValue;

                if (isVisible)
                {
                    await ShowAsync();
                }
                else
                {
                    await HideAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] OnVisibilityChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnProgressChanged()
        {
            try
            {
                UpdateProgress();
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] OnProgressChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnLoadingTextChanged()
        {
            try
            {
                UpdateLoadingText();
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] OnLoadingTextChanged: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnCancelButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                Logger.Log("[LoadingOverlay] 🚫 Cancel button clicked", LogLevel.Info);
                CancelRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] OnCancelButtonClick: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show loading overlay with animation
        /// </summary>
        public async Task ShowAsync()
        {
            try
            {
                if (_isAnimating) return;

                _loadingStartTime = DateTime.Now;
                _isAnimating = true;
                this.IsHitTestVisible = true;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    base.IsVisible = true;
                    UpdateVisibility();
                    StartAnimations();
                });

                LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(true));
                Logger.Log("[LoadingOverlay] ✅ Loading overlay shown", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] ShowAsync: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Hide loading overlay with animation
        /// </summary>
        public async Task HideAsync()
        {
            try
            {
                if (!_isAnimating) return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StopAnimations();
                    base.IsVisible = false;
                    this.IsHitTestVisible = false;
                    _isAnimating = false;
                });

                var loadingDuration = DateTime.Now - _loadingStartTime;
                LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(false, loadingDuration));

                Logger.Log($"[LoadingOverlay] ✅ Loading overlay hidden (Duration: {loadingDuration.TotalSeconds:F1}s)", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] HideAsync: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Update progress with optional text
        /// </summary>
        public void UpdateProgress(double progress, string? text = null)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Progress = progress;
                    if (text != null)
                    {
                        LoadingText = text;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] UpdateProgress: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Set indeterminate progress
        /// </summary>
        public void SetIndeterminate(string? text = null)
        {
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Progress = -1;
                    if (text != null)
                    {
                        LoadingText = text;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] SetIndeterminate: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Animation Control

        private void StartAnimations()
        {
            try
            {
                // Start spinner animation
                if (_spinnerAnimation != null && _spinnerTransform != null)
                {
                    _spinnerAnimation.RunAsync(_spinnerTransform);
                }

                // Start pulse animation for modern style
                if (Style == LoadingStyle.Modern && _pulseAnimation != null && _contentGrid != null)
                {
                    _pulseAnimation.RunAsync(_contentGrid);
                }

                Logger.Log("[LoadingOverlay] 🎬 Animations started", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] StartAnimations: {ex.Message}", LogLevel.Error);
            }
        }

        private void StopAnimations()
        {
            try
            {
                // Animations will stop automatically when the control is hidden
                Logger.Log("[LoadingOverlay] ⏹️ Animations stopped", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] StopAnimations: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region UI Updates

        private void UpdateVisibility()
        {
            try
            {
                if (_backgroundBorder != null)
                {
                    _backgroundBorder.IsVisible = IsVisible;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] UpdateVisibility: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateProgress()
        {
            try
            {
                if (_progressBar != null)
                {
                    _progressBar.IsVisible = ShowProgress && Progress >= 0;
                    _progressBar.IsIndeterminate = Progress < 0;
                    _progressBar.Value = Math.Max(0, Math.Min(100, Progress));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] UpdateProgress: {ex.Message}", LogLevel.Error);
            }
        }

        private void UpdateLoadingText()
        {
            try
            {
                if (_loadingTextBlock != null)
                {
                    _loadingTextBlock.Text = LoadingText;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] UpdateLoadingText: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Default Style

        private static IStyle GetDefaultStyle(LoadingOverlay control)
        {
            return new Style(x => x.OfType<LoadingOverlay>())
            {
                Setters =
                {
                    new Setter(TemplateProperty, CreateDefaultTemplate())
                }
            };
        }

        private static IControlTemplate CreateDefaultTemplate()
        {
            return new FuncControlTemplate<LoadingOverlay>((control, scope) =>
            {
                // Background with glassmorphism effect
                var backgroundBorder = new Border
                {
                    Name = "PART_BackgroundBorder",
                    Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                    [!Border.IsVisibleProperty] = control[!IsVisibleProperty]
                };
                backgroundBorder.RegisterInNameScope(scope, "PART_BackgroundBorder");

                // Content grid
                var contentGrid = new Grid
                {
                    Name = "PART_ContentGrid",
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                contentGrid.RegisterInNameScope(scope, "PART_ContentGrid");

                // Main content panel with glassmorphism
                var contentPanel = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(32, 24),
                    BoxShadow = new BoxShadows(new BoxShadow { Blur = 20, Color = Colors.Black, OffsetY = 8 })
                };

                var contentStack = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Vertical,
                    Spacing = 16,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };

                // Spinner
                var spinnerCanvas = new Canvas
                {
                    Name = "PART_SpinnerCanvas",
                    Width = 48,
                    Height = 48,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                spinnerCanvas.RegisterInNameScope(scope, "PART_SpinnerCanvas");

                // Create modern spinner
                CreateModernSpinner(spinnerCanvas, control);

                // Loading text
                var loadingText = new TextBlock
                {
                    Name = "PART_LoadingText",
                    [!TextBlock.TextProperty] = control[!LoadingTextProperty],
                    FontSize = 14,
                    FontWeight = FontWeight.Medium,
                    Foreground = Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                loadingText.RegisterInNameScope(scope, "PART_LoadingText");

                // Progress bar
                var progressBar = new ProgressBar
                {
                    Name = "PART_ProgressBar",
                    Width = 200,
                    Height = 4,
                    [!ProgressBar.ValueProperty] = control[!ProgressProperty],
                    [!ProgressBar.IsVisibleProperty] = control[!ShowProgressProperty],
                    [!ProgressBar.IsIndeterminateProperty] = control[!ProgressProperty, p => p < 0],
                    Foreground = control[!AccentColorProperty]
                };
                progressBar.RegisterInNameScope(scope, "PART_ProgressBar");

                // Cancel button
                var cancelButton = new Button
                {
                    Name = "PART_CancelButton",
                    Content = "Cancel",
                    [!Button.IsVisibleProperty] = control[!ShowCancelButtonProperty],
                    Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(16, 8),
                    Margin = new Thickness(0, 8, 0, 0)
                };
                cancelButton.RegisterInNameScope(scope, "PART_CancelButton");

                // Assemble content
                contentStack.Children.Add(spinnerCanvas);
                contentStack.Children.Add(loadingText);
                contentStack.Children.Add(progressBar);
                contentStack.Children.Add(cancelButton);

                contentPanel.Child = contentStack;
                contentGrid.Children.Add(contentPanel);
                backgroundBorder.Child = contentGrid;

                return backgroundBorder;
            });
        }

        private static void CreateModernSpinner(Canvas canvas, LoadingOverlay control)
        {
            // Create multiple arcs for modern spinner effect
            for (int i = 0; i < 8; i++)
            {
                var arc = new Border
                {
                    Width = 4,
                    Height = 16,
                    Background = control[!AccentColorProperty],
                    CornerRadius = new CornerRadius(2),
                    Opacity = 1.0 - (i * 0.1),
                    RenderTransform = new RotateTransform(i * 45),
                    RenderTransformOrigin = new RelativePoint(0.5, 3, RelativeUnit.Relative)
                };

                Canvas.SetLeft(arc, 22);
                Canvas.SetTop(arc, 4);
                canvas.Children.Add(arc);
            }
        }

        #endregion

        #region Cleanup

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            try
            {
                StopAnimations();

                if (_cancelButton != null)
                {
                    _cancelButton.Click -= OnCancelButtonClick;
                }

                Logger.Log("[LoadingOverlay] 🧹 Loading overlay detached and cleaned up", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[LoadingOverlay][ERROR] OnDetachedFromVisualTree: {ex.Message}", LogLevel.Error);
            }

            base.OnDetachedFromVisualTree(e);
        }

        #endregion
    }

    #region Support Types

    /// <summary>
    /// Loading overlay visual styles
    /// </summary>
    public enum LoadingStyle
    {
        Modern,
        Classic,
        Minimal
    }

    /// <summary>
    /// Event args for loading state changes
    /// </summary>
    public class LoadingStateChangedEventArgs : EventArgs
    {
        public bool IsLoading { get; }
        public TimeSpan? Duration { get; }

        public LoadingStateChangedEventArgs(bool isLoading, TimeSpan? duration = null)
        {
            IsLoading = isLoading;
            Duration = duration;
        }
    }

    #endregion
}