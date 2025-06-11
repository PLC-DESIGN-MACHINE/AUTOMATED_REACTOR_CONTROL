using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Animation;
using System;
using System.Collections.Generic;

namespace AUTOMATED_REACTOR_CONTROL_Ver6_AVALONIA.Views.Controls
{
    /// <summary>
    /// 🎨 ModernIcon - Scalable Vector Icon System
    /// Hardware-accelerated vector icons with smooth animations
    /// Features: Auto-scaling, color themes, hover effects, accessibility
    /// </summary>
    public class ModernIcon : Control
    {
        #region Styled Properties

        public static readonly StyledProperty<IconType> IconTypeProperty =
            AvaloniaProperty.Register<ModernIcon, IconType>(nameof(IconType), IconType.None);

        public static readonly StyledProperty<double> SizeProperty =
            AvaloniaProperty.Register<ModernIcon, double>(nameof(Size), 24.0);

        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<ModernIcon, IBrush>(nameof(Foreground), Brushes.Black);

        public static readonly StyledProperty<IBrush> HoverForegroundProperty =
            AvaloniaProperty.Register<ModernIcon, IBrush>(nameof(HoverForeground), Brushes.DodgerBlue);

        public static readonly StyledProperty<bool> EnableHoverEffectProperty =
            AvaloniaProperty.Register<ModernIcon, bool>(nameof(EnableHoverEffect), true);

        public static readonly StyledProperty<bool> EnableAnimationProperty =
            AvaloniaProperty.Register<ModernIcon, bool>(nameof(EnableAnimation), true);

        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<ModernIcon, double>(nameof(StrokeThickness), 2.0);

        public static readonly StyledProperty<IconTheme> ThemeProperty =
            AvaloniaProperty.Register<ModernIcon, IconTheme>(nameof(Theme), IconTheme.Outline);

        #endregion

        #region Properties

        /// <summary>
        /// Type of icon to display
        /// </summary>
        public IconType IconType
        {
            get => GetValue(IconTypeProperty);
            set => SetValue(IconTypeProperty, value);
        }

        /// <summary>
        /// Size of the icon in pixels
        /// </summary>
        public double Size
        {
            get => GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        /// <summary>
        /// Foreground brush for the icon
        /// </summary>
        public new IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// Foreground brush when hovering
        /// </summary>
        public IBrush HoverForeground
        {
            get => GetValue(HoverForegroundProperty);
            set => SetValue(HoverForegroundProperty, value);
        }

        /// <summary>
        /// Enable hover color effect
        /// </summary>
        public bool EnableHoverEffect
        {
            get => GetValue(EnableHoverEffectProperty);
            set => SetValue(EnableHoverEffectProperty, value);
        }

        /// <summary>
        /// Enable animations
        /// </summary>
        public bool EnableAnimation
        {
            get => GetValue(EnableAnimationProperty);
            set => SetValue(EnableAnimationProperty, value);
        }

        /// <summary>
        /// Stroke thickness for outline icons
        /// </summary>
        public double StrokeThickness
        {
            get => GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Visual theme of the icon
        /// </summary>
        public IconTheme Theme
        {
            get => GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        #endregion

        #region Private Fields

        private Geometry? _iconGeometry;
        private bool _isHovered;
        private Animation? _hoverAnimation;
        private CompositeTransform? _transform;

        // Icon geometry cache for performance
        private static readonly Dictionary<IconType, Geometry> _geometryCache = new();

        #endregion

        #region Constructor

        static ModernIcon()
        {
            // Property change handlers
            IconTypeProperty.Changed.AddClassHandler<ModernIcon>((x, e) => x.InvalidateVisual());
            SizeProperty.Changed.AddClassHandler<ModernIcon>((x, e) => x.OnSizeChanged());
            ForegroundProperty.Changed.AddClassHandler<ModernIcon>((x, e) => x.InvalidateVisual());
            ThemeProperty.Changed.AddClassHandler<ModernIcon>((x, e) => x.InvalidateVisual());
            StrokeThicknessProperty.Changed.AddClassHandler<ModernIcon>((x, e) => x.InvalidateVisual());

            AffectsRender<ModernIcon>(IconTypeProperty, SizeProperty, ForegroundProperty, ThemeProperty, StrokeThicknessProperty);
        }

        public ModernIcon()
        {
            // Set up transform for animations
            _transform = new CompositeTransform();
            RenderTransform = _transform;

            // Initialize hover animation
            InitializeHoverAnimation();

            Logger.Log("[ModernIcon] 🎨 Scalable vector icon initialized", LogLevel.Debug);
        }

        #endregion

        #region Size Management

        private void OnSizeChanged()
        {
            try
            {
                Width = Size;
                Height = Size;
                InvalidateVisual();

                Logger.Log($"[ModernIcon] 📐 Icon size changed to: {Size}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] OnSizeChanged: {ex.Message}", LogLevel.Error);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(Size, Size);
        }

        #endregion

        #region Hover Animation

        private void InitializeHoverAnimation()
        {
            try
            {
                if (!EnableAnimation) return;

                _hoverAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(200),
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter(CompositeTransform.ScaleXProperty, 1.1),
                                new Setter(CompositeTransform.ScaleYProperty, 1.1)
                            },
                            Cue = new Cue(1.0)
                        }
                    }
                };

                Logger.Log("[ModernIcon] 🎬 Hover animation initialized", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] InitializeHoverAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        protected override void OnPointerEntered(Avalonia.Input.PointerEventArgs e)
        {
            base.OnPointerEntered(e);

            try
            {
                if (!EnableHoverEffect) return;

                _isHovered = true;

                if (EnableAnimation && _hoverAnimation != null && _transform != null)
                {
                    _hoverAnimation.RunAsync(_transform);
                }

                InvalidateVisual();
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] OnPointerEntered: {ex.Message}", LogLevel.Error);
            }
        }

        protected override void OnPointerExited(Avalonia.Input.PointerEventArgs e)
        {
            base.OnPointerExited(e);

            try
            {
                if (!EnableHoverEffect) return;

                _isHovered = false;

                if (EnableAnimation && _transform != null)
                {
                    // Animate back to normal size
                    var exitAnimation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(150),
                        Children =
                        {
                            new KeyFrame
                            {
                                Setters =
                                {
                                    new Setter(CompositeTransform.ScaleXProperty, 1.0),
                                    new Setter(CompositeTransform.ScaleYProperty, 1.0)
                                },
                                Cue = new Cue(1.0)
                            }
                        }
                    };
                    exitAnimation.RunAsync(_transform);
                }

                InvalidateVisual();
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] OnPointerExited: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Icon Rendering

        public override void Render(DrawingContext context)
        {
            try
            {
                if (IconType == IconType.None) return;

                // Get or create icon geometry
                _iconGeometry = GetIconGeometry(IconType);
                if (_iconGeometry == null) return;

                // Determine brush to use
                var brush = (_isHovered && EnableHoverEffect) ? HoverForeground : Foreground;

                // Scale geometry to fit size
                var transform = Matrix.CreateScale(Size / 24.0, Size / 24.0);
                var scaledGeometry = _iconGeometry.Clone();
                scaledGeometry.Transform = new MatrixTransform(transform);

                // Render based on theme
                switch (Theme)
                {
                    case IconTheme.Filled:
                        context.DrawGeometry(brush, null, scaledGeometry);
                        break;

                    case IconTheme.Outline:
                        var pen = new Pen(brush, StrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
                        context.DrawGeometry(null, pen, scaledGeometry);
                        break;

                    case IconTheme.Dual:
                        // Draw filled with reduced opacity, then outline
                        var fillBrush = brush.Clone();
                        if (fillBrush is SolidColorBrush solidBrush)
                        {
                            var color = solidBrush.Color;
                            fillBrush = new SolidColorBrush(Color.FromArgb((byte)(color.A * 0.3), color.R, color.G, color.B));
                        }
                        context.DrawGeometry(fillBrush, new Pen(brush, StrokeThickness), scaledGeometry);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] Render: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Icon Geometry

        private static Geometry GetIconGeometry(IconType iconType)
        {
            try
            {
                // Check cache first
                if (_geometryCache.TryGetValue(iconType, out var cachedGeometry))
                {
                    return cachedGeometry;
                }

                // Create geometry based on icon type
                var geometry = CreateIconGeometry(iconType);
                if (geometry != null)
                {
                    _geometryCache[iconType] = geometry;
                }

                return geometry;
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] GetIconGeometry: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        private static Geometry? CreateIconGeometry(IconType iconType)
        {
            try
            {
                var pathData = iconType switch
                {
                    IconType.Reactor => "M12 2L1 7L12 12L23 7L12 2ZM12 17L7 14.5V18.5L12 21L17 18.5V14.5L12 17Z",
                    IconType.ControlPanel => "M3 3H21V21H3V3ZM7 7V17H17V7H7ZM9 9H15V11H9V9ZM9 13H15V15H9V13Z",
                    IconType.Program => "M8 3V5H16V3H18V5H20V7H18V19H16V21H8V19H6V7H4V5H6V3H8ZM8 7V19H16V7H8ZM10 9H14V11H10V9ZM10 13H14V15H10V13Z",
                    IconType.Graph => "M2 2V22H22V20H4V2H2ZM7 17L11.5 12.5L15.5 16.5L22 10V12.5L15.5 19L11.5 15L7 19.5V17Z",
                    IconType.Settings => "M12 15.5A3.5 3.5 0 0 1 8.5 12A3.5 3.5 0 0 1 12 8.5A3.5 3.5 0 0 1 15.5 12A3.5 3.5 0 0 1 12 15.5M19.43 12.98C19.47 12.66 19.5 12.34 19.5 12C19.5 11.66 19.47 11.34 19.43 11.02L21.54 9.37C21.73 9.22 21.78 8.95 21.66 8.73L19.66 5.27C19.54 5.05 19.27 4.96 19.05 5.05L16.56 6.05C16.04 5.65 15.48 5.32 14.87 5.07L14.5 2.42C14.46 2.18 14.25 2 14 2H10C9.75 2 9.54 2.18 9.5 2.42L9.13 5.07C8.52 5.32 7.96 5.66 7.44 6.05L4.95 5.05C4.73 4.96 4.46 5.05 4.34 5.27L2.34 8.73C2.21 8.95 2.27 9.22 2.46 9.37L4.57 11.02C4.53 11.34 4.5 11.67 4.5 12C4.5 12.33 4.53 12.66 4.57 12.98L2.46 14.63C2.27 14.78 2.21 15.05 2.34 15.27L4.34 18.73C4.46 18.95 4.73 19.03 4.95 18.95L7.44 17.94C7.96 18.34 8.52 18.68 9.13 18.93L9.5 21.58C9.54 21.82 9.75 22 10 22H14C14.25 22 14.46 21.82 14.5 21.58L14.87 18.93C15.48 18.68 16.04 18.34 16.56 17.94L19.05 18.95C19.27 19.03 19.54 18.95 19.66 18.73L21.66 15.27C21.78 15.05 21.73 14.78 21.54 14.63L19.43 12.98Z",
                    IconType.Save => "M15 9H5V5H15M12 19A3 3 0 0 1 9 16A3 3 0 0 1 12 13A3 3 0 0 1 15 16A3 3 0 0 1 12 19M17 3H5C3.89 3 3 3.9 3 5V19A2 2 0 0 0 5 21H19A2 2 0 0 0 21 19V7L17 3Z",
                    IconType.Stop => "M18 18H6V6H18V18Z",
                    IconType.Warning => "M12 2L1 21H23M12 6L19.53 19H4.47M11 10V14H13V10M11 16V18H13V16",
                    IconType.Connection => "M17 7H22V9H19V12H17V9H14V7H17V4H19V7ZM2 12H7V14H4V17H2V14ZM7 4H2V6H5V9H7V6H10V4H7V1H5V4Z",
                    IconType.Performance => "M16 6L18.29 8.29L13.41 13.17L9.41 9.17L2 16.59L3.41 18L9.41 12L13.41 16L19.71 9.71L22 12V6H16Z",
                    IconType.Menu => "M3 6H21V8H3V6ZM3 11H21V13H3V11ZM3 16H21V18H3V16Z",
                    IconType.Close => "M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12L19 6.41Z",
                    IconType.ChevronLeft => "M15.41 7.41L14 6L8 12L14 18L15.41 16.59L10.83 12L15.41 7.41Z",
                    IconType.ChevronRight => "M8.59 16.59L10 18L16 12L10 6L8.59 7.41L13.17 12L8.59 16.59Z",
                    IconType.ChevronUp => "M7.41 15.41L12 10.83L16.59 15.41L18 14L12 8L6 14L7.41 15.41Z",
                    IconType.ChevronDown => "M7.41 8.59L12 13.17L16.59 8.59L18 10L12 16L6 10L7.41 8.59Z",
                    IconType.Check => "M9 16.17L4.83 12L3.41 13.41L9 19L21 7L19.59 5.59L9 16.17Z",
                    IconType.Error => "M12 2C6.48 2 2 6.48 2 12S6.48 22 12 22S22 17.52 22 12S17.52 2 12 2ZM13 17H11V15H13V17ZM13 13H11V7H13V13Z",
                    IconType.Info => "M12 2C6.48 2 2 6.48 2 12S6.48 22 12 22S22 17.52 22 12S17.52 2 12 2ZM13 17H11V11H13V17ZM13 9H11V7H13V9Z",
                    IconType.Add => "M19 13H13V19H11V13H5V11H11V5H13V11H19V13Z",
                    IconType.Remove => "M19 13H5V11H19V13Z",
                    IconType.Edit => "M20.71 7.04C21.1 6.65 21.1 6 20.71 5.63L18.37 3.29C18 2.9 17.35 2.9 16.96 3.29L15.12 5.12L18.87 8.87M3 17.25V21H6.75L17.81 9.93L14.06 6.18L3 17.25Z",
                    IconType.Search => "M9.5 3A6.5 6.5 0 0 1 16 9.5C16 11.11 15.41 12.59 14.44 13.73L14.71 14H15.5L20.5 19L19 20.5L14 15.5V14.71L13.73 14.44C12.59 15.41 11.11 16 9.5 16A6.5 6.5 0 0 1 3 9.5A6.5 6.5 0 0 1 9.5 3M9.5 5C7 5 5 7 5 9.5S7 14 9.5 14S14 12 14 9.5S12 5 9.5 5Z",
                    IconType.Refresh => "M17.65 6.35C16.2 4.9 14.21 4 12 4C7.58 4 4 7.58 4 12S7.58 20 12 20C15.73 20 18.84 17.45 19.73 14H17.65C16.83 16.33 14.61 18 12 18C8.69 18 6 15.31 6 12S8.69 6 12 6C13.66 6 15.14 6.69 16.22 7.78L13 11H20V4L17.65 6.35Z",
                    _ => ""
                };

                if (string.IsNullOrEmpty(pathData))
                {
                    Logger.Log($"[ModernIcon] No geometry defined for icon type: {iconType}", LogLevel.Warn);
                    return null;
                }

                return Geometry.Parse(pathData);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] CreateIconGeometry for {iconType}: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Trigger a bounce animation
        /// </summary>
        public async void PlayBounceAnimation()
        {
            try
            {
                if (!EnableAnimation || _transform == null) return;

                var bounceAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(600),
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter(CompositeTransform.ScaleXProperty, 1.3),
                                new Setter(CompositeTransform.ScaleYProperty, 1.3)
                            },
                            Cue = new Cue(0.3)
                        },
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter(CompositeTransform.ScaleXProperty, 0.9),
                                new Setter(CompositeTransform.ScaleYProperty, 0.9)
                            },
                            Cue = new Cue(0.6)
                        },
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter(CompositeTransform.ScaleXProperty, 1.0),
                                new Setter(CompositeTransform.ScaleYProperty, 1.0)
                            },
                            Cue = new Cue(1.0)
                        }
                    }
                };

                await bounceAnimation.RunAsync(_transform);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] PlayBounceAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Trigger a pulse animation
        /// </summary>
        public async void PlayPulseAnimation()
        {
            try
            {
                if (!EnableAnimation) return;

                var pulseAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(800),
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters = { new Setter(OpacityProperty, 0.5) },
                            Cue = new Cue(0.5)
                        },
                        new KeyFrame
                        {
                            Setters = { new Setter(OpacityProperty, 1.0) },
                            Cue = new Cue(1.0)
                        }
                    }
                };

                await pulseAnimation.RunAsync(this);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] PlayPulseAnimation: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Clear the geometry cache
        /// </summary>
        public static void ClearGeometryCache()
        {
            try
            {
                _geometryCache.Clear();
                Logger.Log("[ModernIcon] 🧹 Geometry cache cleared", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModernIcon][ERROR] ClearGeometryCache: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// Available icon types in the system
    /// </summary>
    public enum IconType
    {
        None,
        Reactor,
        ControlPanel,
        Program,
        Graph,
        Settings,
        Save,
        Stop,
        Warning,
        Connection,
        Performance,
        Menu,
        Close,
        ChevronLeft,
        ChevronRight,
        ChevronUp,
        ChevronDown,
        Check,
        Error,
        Info,
        Add,
        Remove,
        Edit,
        Search,
        Refresh
    }

    /// <summary>
    /// Visual themes for icons
    /// </summary>
    public enum IconTheme
    {
        Filled,
        Outline,
        Dual
    }

    #endregion
}