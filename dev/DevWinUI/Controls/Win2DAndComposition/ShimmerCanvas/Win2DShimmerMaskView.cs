using System.Diagnostics;
using Microsoft.UI.Xaml.Markup;

namespace DevWinUI;

/// <summary>
/// A Win2D-rendered skeleton whose single shimmer sweep is clipped to the union of its placeholder
/// <see cref="Shapes"/>, so the highlight is only ever visible over the skeleton shapes. This is the
/// Win2D counterpart to <see cref="ShimmerMaskView"/> and shares the full property surface and the
/// <see cref="ShimmerSweep"/> motion/appearance math, so the two look identical.
/// </summary>
/// <remarks>
/// The shapes are filled with <see cref="SkeletonColor"/> as the resting skeleton, and the moving
/// highlight band is drawn on top, clipped to the same geometry with a Win2D layer. Unlike
/// <see cref="ShimmerMaskView"/> (which masks a Composition brush with captured XAML content), this
/// builds the mask from a device-independent <see cref="Win2DShimmerMaskShape"/> list, so it needs no
/// visual capture and composes naturally inside a Win2D scene. For plain placeholders the
/// dependency-free Composition <see cref="ShimmerMaskView"/> is lighter; use this when you are already
/// drawing with Win2D. Set <see cref="Shapes"/> before the control loads.
/// </remarks>
[ContentProperty(Name = nameof(Shapes))]
[TemplatePart(Name = PART_Canvas, Type = typeof(CanvasControl))]
public partial class Win2DShimmerMaskView : Control
{
    /// <summary>Identifies the <see cref="Duration"/> dependency property.</summary>
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration), typeof(TimeSpan), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.DefaultDuration, OnParameterChanged));

    /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(true, OnIsActiveChanged));

    /// <summary>Identifies the <see cref="Angle"/> dependency property.</summary>
    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
        nameof(Angle), typeof(double), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.DefaultAngle, OnParameterChanged));

    /// <summary>Identifies the <see cref="BandWidth"/> dependency property.</summary>
    public static readonly DependencyProperty BandWidthProperty = DependencyProperty.Register(
        nameof(BandWidth), typeof(double), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.DefaultBandWidth, OnParameterChanged));

    /// <summary>Identifies the <see cref="HighlightOpacity"/> dependency property.</summary>
    public static readonly DependencyProperty HighlightOpacityProperty = DependencyProperty.Register(
        nameof(HighlightOpacity), typeof(double), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.DefaultHighlightOpacity, OnParameterChanged));

    /// <summary>Identifies the <see cref="HighlightColor"/> dependency property.</summary>
    public static readonly DependencyProperty HighlightColorProperty = DependencyProperty.Register(
        nameof(HighlightColor), typeof(Color), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.ThemeDefaultColor, OnParameterChanged));

    /// <summary>Identifies the <see cref="Easing"/> dependency property.</summary>
    public static readonly DependencyProperty EasingProperty = DependencyProperty.Register(
        nameof(Easing), typeof(ShimmerEasing), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(ShimmerEasing.Linear, OnParameterChanged));

    /// <summary>Identifies the <see cref="GradientShape"/> dependency property.</summary>
    public static readonly DependencyProperty GradientShapeProperty = DependencyProperty.Register(
        nameof(GradientShape), typeof(ShimmerGradientShape), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(ShimmerGradientShape.Plateau, OnParameterChanged));

    /// <summary>Identifies the <see cref="ReverseGradient"/> dependency property.</summary>
    public static readonly DependencyProperty ReverseGradientProperty = DependencyProperty.Register(
        nameof(ReverseGradient), typeof(bool), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(false, OnParameterChanged));

    /// <summary>Identifies the <see cref="SkeletonColor"/> dependency property.</summary>
    public static readonly DependencyProperty SkeletonColorProperty = DependencyProperty.Register(
        nameof(SkeletonColor), typeof(Color), typeof(Win2DShimmerMaskView),
        new PropertyMetadata(default(Color), OnParameterChanged));

    /// <summary>Gets or sets the time to travel a fixed reference distance (controls sweep speed).</summary>
    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>Gets or sets whether the sweep is playing. When false, only the resting skeleton is shown.</summary>
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

    /// <summary>
    /// Gets or sets the resting fill color of the placeholder shapes. Leave at the default
    /// (transparent) to use the theme skeleton color (a dark gray in dark, a light gray in light).
    /// </summary>
    public Color SkeletonColor
    {
        get => (Color)GetValue(SkeletonColorProperty);
        set => SetValue(SkeletonColorProperty, value);
    }

    /// <summary>
    /// Gets the placeholder shapes whose union forms both the resting skeleton and the mask that the
    /// highlight sweep is clipped to. This is the control's XAML content property.
    /// </summary>
    public IList<Win2DShimmerMaskShape> Shapes { get; } = new List<Win2DShimmerMaskShape>();

    private const string PART_Canvas = "PART_Canvas";

    private readonly ShimmerSweepRenderer _renderer = new();
    private readonly Stopwatch _clock = new();
    private CanvasControl? _canvas;
    private CanvasGeometry? _maskGeometry;
    private bool _loaded;
    private bool _rendering;
    private bool _active = true;
    private bool _shouldAnimate = true;

    public Win2DShimmerMaskView()
    {
        DefaultStyleKey = typeof(Win2DShimmerMaskView);
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

        UpdateAnimationState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loaded = false;

        // Stop the per-frame loop and release the cached GPU resources, but keep the canvas wired so
        // the shimmer resumes if the control is reloaded; the brush and mask geometry rebuild lazily
        // on the next Draw.
        UpdateAnimationState();
        _renderer.Dispose();
        _maskGeometry?.Dispose();
        _maskGeometry = null;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        // Theme drives the resting skeleton and the default highlight color, and can flip the
        // reduced-motion / high-contrast gate, so re-evaluate whether the sweep should be running.
        UpdateAnimationState();
    }

    private void WireCanvas()
    {
        if (_canvas != null)
        {
            _canvas.ClearColor = Colors.Transparent;
            _canvas.CreateResources += OnCreateResources;
            _canvas.Draw += OnDraw;
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

    private void OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // Device (re)created — drop resources cached against the old device.
        _renderer.ResetDeviceResources();
        _maskGeometry?.Dispose();
        _maskGeometry = null;
    }

    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (Shapes.Count == 0)
        {
            return;
        }

        var geometry = EnsureGeometry(sender);
        if (geometry == null)
        {
            return;
        }

        var session = args.DrawingSession;

        // Resting skeleton: the placeholder shapes are always visible.
        session.FillGeometry(geometry, ShimmerSweep.ResolveSkeletonColor(ActualTheme, SkeletonColor));

        // Moving highlight: drawn over the whole surface but clipped to the placeholder shapes.
        if (_active && _shouldAnimate)
        {
            using (session.CreateLayer(1f, geometry))
            {
                _renderer.Draw(session, sender, new Size(sender.ActualWidth, sender.ActualHeight), _clock.Elapsed,
                    Angle, BandWidth, Duration, Easing, GradientShape, ReverseGradient, ActualTheme, HighlightOpacity, HighlightColor);
            }
        }
    }

    // Builds (and caches) the union of all placeholder shapes on the control's current device.
    private CanvasGeometry? EnsureGeometry(ICanvasResourceCreator creator)
    {
        if (_maskGeometry != null)
        {
            return _maskGeometry;
        }

        CanvasGeometry? combined = null;
        foreach (var shape in Shapes)
        {
            if (shape == null || shape.Width <= 0 || shape.Height <= 0)
            {
                continue;
            }

            var next = shape.Kind == Win2DShimmerMaskShapeKind.Ellipse
                ? CanvasGeometry.CreateEllipse(creator,
                    (float)(shape.X + (shape.Width / 2)), (float)(shape.Y + (shape.Height / 2)),
                    (float)(shape.Width / 2), (float)(shape.Height / 2))
                : CanvasGeometry.CreateRoundedRectangle(creator,
                    new Rect(shape.X, shape.Y, shape.Width, shape.Height),
                    (float)shape.CornerRadius, (float)shape.CornerRadius);

            if (combined == null)
            {
                combined = next;
            }
            else
            {
                var union = combined.CombineWith(next, Matrix3x2.Identity, CanvasGeometryCombine.Union);
                combined.Dispose();
                next.Dispose();
                combined = union;
            }
        }

        _maskGeometry = combined;
        return _maskGeometry;
    }

    private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Win2DShimmerMaskView)d).UpdateAnimationState();
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Win2DShimmerMaskView)d).UpdateAnimationState();
    }

    // Starts or stops the per-frame redraw loop for the current state. A CanvasControl only repaints
    // when invalidated, so an active sweep is driven by invalidating it once per frame from
    // CompositionTarget.Rendering; when inactive the loop is detached and only the skeleton is drawn.
    private void UpdateAnimationState()
    {
        _active = IsActive;
        _shouldAnimate = ShimmerSweep.ShouldAnimate();

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
