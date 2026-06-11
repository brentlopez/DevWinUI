namespace DevWinUI;

/// <summary>
/// A skeleton-style shimmer whose single diagonal light sweep is masked by the alpha of its
/// <see cref="RedirectVisualView.Child"/> content, so the highlight is only ever visible over the
/// placeholder elements rather than across the whole surface.
/// </summary>
/// <remarks>
/// Unlike <see cref="ShimmerPanel"/> (which sweeps over its full rectangular bounds), a single
/// continuous sweep travels across the entire control but is clipped to the union of the child
/// placeholder shapes (an ellipse, rounded rectangles, text, etc.). The child shapes remain visible
/// as the resting skeleton color, and the gaps between them are transparent, so the card's own
/// background — supplied by a parent <c>Border</c>/<c>Grid</c> — shows through and is never
/// shimmered. Fill the placeholder shapes with an <b>opaque</b> brush: the sweep is masked by the
/// shapes' alpha, so semi-transparent fills (such as the Fluent <c>*AltFill*</c> overlay brushes)
/// make the highlight correspondingly faint. Reuse <see cref="Duration"/> and <see cref="IsActive"/>
/// to control the sweep.
/// </remarks>
public partial class ShimmerMaskView : RedirectVisualView
{
    /// <summary>
    /// Identifies the <see cref="Duration"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration),
        typeof(TimeSpan),
        typeof(ShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.DefaultDuration, OnShimmerPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="IsActive"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(ShimmerMaskView),
        new PropertyMetadata(true, OnShimmerPropertyChanged));

    /// <summary>
    /// Gets or sets a value that controls the shimmer sweep speed. It is the time taken to travel a
    /// fixed reference distance; the band always moves at a constant pixel velocity, so larger panels
    /// take proportionally longer to sweep. Lower values sweep faster.
    /// </summary>
    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the shimmer sweep is playing. When <see langword="false"/>,
    /// the placeholder shapes are shown without any moving highlight.
    /// </summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Angle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
        nameof(Angle),
        typeof(double),
        typeof(ShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.DefaultAngle, OnShimmerPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="BandWidth"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BandWidthProperty = DependencyProperty.Register(
        nameof(BandWidth),
        typeof(double),
        typeof(ShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.DefaultBandWidth, OnShimmerPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="HighlightOpacity"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HighlightOpacityProperty = DependencyProperty.Register(
        nameof(HighlightOpacity),
        typeof(double),
        typeof(ShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.DefaultHighlightOpacity, OnAppearanceChanged));

    /// <summary>
    /// Identifies the <see cref="HighlightColor"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HighlightColorProperty = DependencyProperty.Register(
        nameof(HighlightColor),
        typeof(Color),
        typeof(ShimmerMaskView),
        new PropertyMetadata(ShimmerSweep.ThemeDefaultColor, OnAppearanceChanged));

    /// <summary>
    /// Gets or sets the angle (in degrees) of the sweep. 0 is a horizontal left-to-right sweep;
    /// 45 (default) is a top-left to bottom-right diagonal; 90 is a vertical sweep.
    /// </summary>
    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    /// <summary>
    /// Gets or sets the width (in pixels) of the moving highlight band.
    /// </summary>
    public double BandWidth
    {
        get => (double)GetValue(BandWidthProperty);
        set => SetValue(BandWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the peak opacity (0-1) of the highlight band.
    /// </summary>
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

    /// <summary>
    /// Identifies the <see cref="Easing"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EasingProperty = DependencyProperty.Register(
        nameof(Easing),
        typeof(ShimmerEasing),
        typeof(ShimmerMaskView),
        new PropertyMetadata(ShimmerEasing.Linear, OnShimmerPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="GradientShape"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GradientShapeProperty = DependencyProperty.Register(
        nameof(GradientShape),
        typeof(ShimmerGradientShape),
        typeof(ShimmerMaskView),
        new PropertyMetadata(ShimmerGradientShape.Plateau, OnAppearanceChanged));

    /// <summary>
    /// Identifies the <see cref="ReverseGradient"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ReverseGradientProperty = DependencyProperty.Register(
        nameof(ReverseGradient),
        typeof(bool),
        typeof(ShimmerMaskView),
        new PropertyMetadata(false, OnAppearanceChanged));

    /// <summary>
    /// Gets or sets the easing applied to the sweep motion as it crosses the control.
    /// </summary>
    public ShimmerEasing Easing
    {
        get => (ShimmerEasing)GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    /// <summary>
    /// Gets or sets the opacity profile of the highlight across the band width.
    /// </summary>
    public ShimmerGradientShape GradientShape
    {
        get => (ShimmerGradientShape)GetValue(GradientShapeProperty);
        set => SetValue(GradientShapeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="GradientShape"/> profile is mirrored
    /// across the band width (e.g. a reversed linear ramp fades 1 → 0).
    /// </summary>
    public bool ReverseGradient
    {
        get => (bool)GetValue(ReverseGradientProperty);
        set => SetValue(ReverseGradientProperty, value);
    }

    // The sweep is rendered in ABSOLUTE (pixel) gradient space so its angle, band width and velocity
    // are independent of the panel's size and aspect ratio. See ShimmerPanel for the full rationale.
    private readonly Compositor _compositor;
    private readonly CompositionLinearGradientBrush _shimmerGradient;
    private readonly CompositionMaskBrush _maskBrush;
    private readonly ShimmerSweepController _controller;

    public ShimmerMaskView()
    {
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        _shimmerGradient = _compositor.CreateLinearGradientBrush();
        _shimmerGradient.MappingMode = CompositionMappingMode.Absolute;

        _controller = new ShimmerSweepController(_shimmerGradient);
        // Park the band off-screen and build the stops before the sweep runs (this is also the
        // resting appearance whenever IsActive is false).
        _controller.ParkOffscreen(BandWidth);
        _controller.RebuildStops(GradientShape, ReverseGradient, ActualTheme, HighlightOpacity, HighlightColor);

        // The single sweep (Source) is masked by the child placeholder shapes (Mask), so the
        // highlight is only visible where the shapes are.
        _maskBrush = _compositor.CreateMaskBrush();
        _maskBrush.Source = _shimmerGradient;
        _maskBrush.Mask = ChildVisualBrush;

        RootVisual.Brush = _maskBrush;
    }

    protected override void OnAttachVisuals()
    {
        base.OnAttachVisuals();

        // Keep the placeholder shapes visible as the resting skeleton; the masked sweep layers on top.
        // NOTE: this deliberately re-shows ChildPresenterContainer, which the base AttachVisuals()
        // hides immediately before invoking this override (RedirectVisualView hides it, then calls
        // OnAttachVisuals last). This relies on that documented call order; if the base ever stopped
        // calling OnAttachVisuals after hiding the container, the resting skeleton would disappear.
        if (ChildPresenterContainer != null)
        {
            ElementCompositionPreview.GetElementVisual(ChildPresenterContainer).IsVisible = true;
        }

        ActualThemeChanged += OnActualThemeChanged;
        UpdateActiveState();
    }

    protected override void OnDetachVisuals()
    {
        ActualThemeChanged -= OnActualThemeChanged;
        _controller.Stop();

        base.OnDetachVisuals();
    }

    protected override void OnUpdateSize()
    {
        base.OnUpdateSize();

        // Absolute-space keyframes depend on the pixel size of the child, so recompute on resize.
        if (RedirectVisualAttached && IsActive)
        {
            _controller.Stop();
            StartSweep();
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        RebuildGradientStops();
    }

    private void UpdateActiveState()
    {
        if (RedirectVisualAttached is false)
        {
            return;
        }

        if (IsActive)
        {
            RootVisual.IsVisible = true;
            _controller.Stop();
            StartSweep();
        }
        else
        {
            _controller.Stop();
            RootVisual.IsVisible = false;
        }
    }

    private void StartSweep()
    {
        if (ChildPresenter is null)
        {
            return;
        }

        _controller.Start((float)ChildPresenter.ActualWidth, (float)ChildPresenter.ActualHeight,
            Angle, BandWidth, Duration, Easing);
    }

    private void RebuildGradientStops()
    {
        _controller.RebuildStops(GradientShape, ReverseGradient, ActualTheme, HighlightOpacity, HighlightColor);
    }

    private static void OnShimmerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ShimmerMaskView)d).UpdateActiveState();
    }

    private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ShimmerMaskView)d).RebuildGradientStops();
    }
}
