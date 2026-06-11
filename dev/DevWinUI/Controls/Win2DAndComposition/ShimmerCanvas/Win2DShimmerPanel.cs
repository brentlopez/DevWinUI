using System.Diagnostics;

namespace DevWinUI;

/// <summary>
/// A Win2D-rendered shimmer overlay with the same parameters as <see cref="ShimmerPanel"/>, drawn
/// every frame by a <c>CanvasControl</c> instead of an off-thread Composition animation.
/// </summary>
/// <remarks>
/// Use this when you are already rendering with Win2D, want per-frame custom drawing, or need the
/// shimmer to be part of a Win2D scene. For plain skeleton placeholders the dependency-free
/// <see cref="ShimmerPanel"/> (Composition) is lighter and is the recommended default. Both share the
/// same <see cref="ShimmerSweep"/> math, so the appearance and the full property surface match.
/// </remarks>
[TemplatePart(Name = PART_Canvas, Type = typeof(CanvasControl))]
public partial class Win2DShimmerPanel : ContentControl
{
    /// <summary>Identifies the <see cref="Duration"/> dependency property.</summary>
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration), typeof(TimeSpan), typeof(Win2DShimmerPanel),
        new PropertyMetadata(ShimmerSweep.DefaultDuration, OnParameterChanged));

    /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(Win2DShimmerPanel),
        new PropertyMetadata(true, OnIsActiveChanged));

    /// <summary>Identifies the <see cref="Angle"/> dependency property.</summary>
    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
        nameof(Angle), typeof(double), typeof(Win2DShimmerPanel),
        new PropertyMetadata(ShimmerSweep.DefaultAngle, OnParameterChanged));

    /// <summary>Identifies the <see cref="BandWidth"/> dependency property.</summary>
    public static readonly DependencyProperty BandWidthProperty = DependencyProperty.Register(
        nameof(BandWidth), typeof(double), typeof(Win2DShimmerPanel),
        new PropertyMetadata(ShimmerSweep.DefaultBandWidth, OnParameterChanged));

    /// <summary>Identifies the <see cref="HighlightOpacity"/> dependency property.</summary>
    public static readonly DependencyProperty HighlightOpacityProperty = DependencyProperty.Register(
        nameof(HighlightOpacity), typeof(double), typeof(Win2DShimmerPanel),
        new PropertyMetadata(ShimmerSweep.DefaultHighlightOpacity, OnParameterChanged));

    /// <summary>Identifies the <see cref="HighlightColor"/> dependency property.</summary>
    public static readonly DependencyProperty HighlightColorProperty = DependencyProperty.Register(
        nameof(HighlightColor), typeof(Color), typeof(Win2DShimmerPanel),
        new PropertyMetadata(ShimmerSweep.ThemeDefaultColor, OnParameterChanged));

    /// <summary>Identifies the <see cref="Easing"/> dependency property.</summary>
    public static readonly DependencyProperty EasingProperty = DependencyProperty.Register(
        nameof(Easing), typeof(ShimmerEasing), typeof(Win2DShimmerPanel),
        new PropertyMetadata(ShimmerEasing.Linear, OnParameterChanged));

    /// <summary>Identifies the <see cref="GradientShape"/> dependency property.</summary>
    public static readonly DependencyProperty GradientShapeProperty = DependencyProperty.Register(
        nameof(GradientShape), typeof(ShimmerGradientShape), typeof(Win2DShimmerPanel),
        new PropertyMetadata(ShimmerGradientShape.Plateau, OnParameterChanged));

    /// <summary>Identifies the <see cref="ReverseGradient"/> dependency property.</summary>
    public static readonly DependencyProperty ReverseGradientProperty = DependencyProperty.Register(
        nameof(ReverseGradient), typeof(bool), typeof(Win2DShimmerPanel),
        new PropertyMetadata(false, OnParameterChanged));

    /// <summary>Gets or sets the time to travel a fixed reference distance (controls sweep speed).</summary>
    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>Gets or sets whether the sweep is playing. When false, the overlay is blank.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>Gets or sets the sweep angle in degrees (0 horizontal, 45 diagonal, 90 vertical).</summary>
    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    /// <summary>Gets or sets the width (in pixels) of the moving highlight band.</summary>
    public double BandWidth
    {
        get => (double)GetValue(BandWidthProperty);
        set => SetValue(BandWidthProperty, value);
    }

    /// <summary>Gets or sets the peak opacity (0-1) of the highlight band.</summary>
    public double HighlightOpacity
    {
        get => (double)GetValue(HighlightOpacityProperty);
        set => SetValue(HighlightOpacityProperty, value);
    }

    /// <summary>
    /// Gets or sets the highlight color. Leave at the default (transparent) to use the theme color
    /// (white in dark, black in light).
    /// </summary>
    public Color HighlightColor
    {
        get => (Color)GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
    }

    /// <summary>Gets or sets the easing applied to the sweep motion as it crosses the control.</summary>
    public ShimmerEasing Easing
    {
        get => (ShimmerEasing)GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    /// <summary>Gets or sets the opacity profile of the highlight across the band width.</summary>
    public ShimmerGradientShape GradientShape
    {
        get => (ShimmerGradientShape)GetValue(GradientShapeProperty);
        set => SetValue(GradientShapeProperty, value);
    }

    /// <summary>Gets or sets whether the <see cref="GradientShape"/> profile is mirrored.</summary>
    public bool ReverseGradient
    {
        get => (bool)GetValue(ReverseGradientProperty);
        set => SetValue(ReverseGradientProperty, value);
    }

    private const string PART_Canvas = "PART_Canvas";

    private readonly ShimmerSweepRenderer _renderer = new();
    private readonly Stopwatch _clock = new();
    private CanvasControl? _canvas;
    private bool _loaded;
    private bool _rendering;

    // CanvasControl raises Draw on the UI thread, so these values could be read directly; they are kept
    // as a single snapshot (taken in SyncParameters) so the per-frame Draw does no dependency-property
    // work and the appearance inputs are read consistently.
    private readonly object _sync = new();
    private double _angle = ShimmerSweep.DefaultAngle;
    private double _bandWidth = ShimmerSweep.DefaultBandWidth;
    private TimeSpan _duration = ShimmerSweep.DefaultDuration;
    private ShimmerEasing _easing = ShimmerEasing.Linear;
    private ShimmerGradientShape _shape = ShimmerGradientShape.Plateau;
    private bool _reverse;
    private double _opacity = ShimmerSweep.DefaultHighlightOpacity;
    private Color _color = ShimmerSweep.ThemeDefaultColor;
    private ElementTheme _theme = ElementTheme.Default;
    private bool _active = true;
    private bool _shouldAnimate = true;

    public Win2DShimmerPanel()
    {
        DefaultStyleKey = typeof(Win2DShimmerPanel);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ActualThemeChanged += OnActualThemeChanged;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // A new template produces a new canvas; drop any old handlers and wire the new one.
        DetachCanvas();
        _canvas = GetTemplateChild(PART_Canvas) as CanvasControl;
        WireCanvas();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;

        // The control can be unloaded and reloaded (e.g. reparented in a Flyout, TabView or a cached
        // page) without OnApplyTemplate running again, so re-acquire the canvas if it isn't wired yet.
        if (_canvas == null)
        {
            _canvas = GetTemplateChild(PART_Canvas) as CanvasControl;
            WireCanvas();
        }

        // Refresh the snapshot (reduced-motion / high-contrast may have changed) once we're live.
        SyncParameters();
        UpdateAnimationState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loaded = false;

        // Stop the per-frame loop and release the cached GPU brush, but keep the canvas wired so the
        // shimmer resumes if the control is reloaded; the Win2D brush rebuilds lazily on the next Draw.
        UpdateAnimationState();
        _renderer.Dispose();
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        // Theme changes the default highlight color and can flip the reduced-motion / high-contrast
        // gate, so refresh the snapshot and re-evaluate whether the sweep should be running.
        SyncParameters();
        UpdateAnimationState();
    }

    private void WireCanvas()
    {
        if (_canvas != null)
        {
            _canvas.ClearColor = Colors.Transparent;
            _canvas.CreateResources += OnCreateResources;
            _canvas.Draw += OnDraw;
            SyncParameters();
            UpdateAnimationState();
        }
    }

    private void DetachCanvas()
    {
        if (_canvas != null)
        {
            _canvas.CreateResources -= OnCreateResources;
            _canvas.Draw -= OnDraw;
        }
    }

    // Copies the dependency-property values into the render-thread snapshot. UI thread only.
    private void SyncParameters()
    {
        lock (_sync)
        {
            _angle = Angle;
            _bandWidth = BandWidth;
            _duration = Duration;
            _easing = Easing;
            _shape = GradientShape;
            _reverse = ReverseGradient;
            _opacity = HighlightOpacity;
            _color = HighlightColor;
            _theme = ActualTheme;
            _active = IsActive;
            _shouldAnimate = ShimmerSweep.ShouldAnimate();
        }
    }

    private void OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // Device (re)created — drop any brush cached against the old device.
        _renderer.ResetDeviceResources();
    }

    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        double angle, bandWidth, opacity;
        TimeSpan duration;
        ShimmerEasing easing;
        ShimmerGradientShape shape;
        bool reverse;
        Color color;
        ElementTheme theme;

        lock (_sync)
        {
            if (!_active || !_shouldAnimate)
            {
                return;
            }

            angle = _angle;
            bandWidth = _bandWidth;
            duration = _duration;
            easing = _easing;
            shape = _shape;
            reverse = _reverse;
            opacity = _opacity;
            color = _color;
            theme = _theme;
        }

        _renderer.Draw(args.DrawingSession, sender, new Size(sender.ActualWidth, sender.ActualHeight), _clock.Elapsed,
            angle, bandWidth, duration, easing, shape, reverse, theme, opacity, color);
    }

    private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (Win2DShimmerPanel)d;
        self.SyncParameters();
        self.UpdateAnimationState();
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (Win2DShimmerPanel)d;
        self.SyncParameters();
        self.UpdateAnimationState();
    }

    // Starts or stops the per-frame redraw loop for the current state. A CanvasControl only repaints
    // when invalidated, so an active sweep is driven by invalidating it once per frame from
    // CompositionTarget.Rendering; when inactive the loop is detached and the clock is paused.
    private void UpdateAnimationState()
    {
        var run = _canvas != null && _loaded && _active && _shouldAnimate;
        if (run == _rendering)
        {
            // No start/stop transition; a parameter likely changed, so request a single redraw.
            _canvas?.Invalidate();
            return;
        }

        _rendering = run;
        if (run)
        {
            _clock.Start();
            CompositionTarget.Rendering += OnRendering;
        }
        else
        {
            CompositionTarget.Rendering -= OnRendering;
            _clock.Stop();
            _canvas?.Invalidate();
        }
    }

    private void OnRendering(object? sender, object e) => _canvas?.Invalidate();
}
