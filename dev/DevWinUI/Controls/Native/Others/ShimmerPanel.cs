namespace DevWinUI;

/// <summary>
/// A content-agnostic shimmer control that plays an animated gradient "sheen" sweep over its
/// <see cref="ContentControl.Content"/>.
/// </summary>
/// <remarks>
/// <see cref="ShimmerPanel"/> generalizes the standalone <see cref="Shimmer"/> placeholder block:
/// it reuses the exact same Composition linear-gradient animation, but draws it as an overlay on
/// top of any hosted content — an <c>Image</c>, a <c>Rectangle</c>/shape, or an entire placeholder
/// layout — making it suitable for image- and rectangle-based shimmer in addition to text.
/// When <see cref="IsActive"/> is <see langword="false"/> the overlay is hidden and the content is
/// shown without any sheen.
/// </remarks>
[TemplatePart(Name = PART_Shape, Type = typeof(Border))]
public partial class ShimmerPanel : ContentControl
{
    /// <summary>
    /// Identifies the <see cref="Duration"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration),
        typeof(TimeSpan),
        typeof(ShimmerPanel),
        new PropertyMetadata(defaultValue: ShimmerSweep.DefaultDuration, PropertyChanged));

    /// <summary>
    /// Identifies the <see cref="IsActive"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(ShimmerPanel),
        new PropertyMetadata(defaultValue: true, PropertyChanged));

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
    /// the overlay is hidden and the content is shown without any sheen.
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
        typeof(ShimmerPanel),
        new PropertyMetadata(ShimmerSweep.DefaultAngle, PropertyChanged));

    /// <summary>
    /// Identifies the <see cref="BandWidth"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BandWidthProperty = DependencyProperty.Register(
        nameof(BandWidth),
        typeof(double),
        typeof(ShimmerPanel),
        new PropertyMetadata(ShimmerSweep.DefaultBandWidth, PropertyChanged));

    /// <summary>
    /// Identifies the <see cref="HighlightOpacity"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HighlightOpacityProperty = DependencyProperty.Register(
        nameof(HighlightOpacity),
        typeof(double),
        typeof(ShimmerPanel),
        new PropertyMetadata(ShimmerSweep.DefaultHighlightOpacity, OnAppearanceChanged));

    /// <summary>
    /// Identifies the <see cref="HighlightColor"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HighlightColorProperty = DependencyProperty.Register(
        nameof(HighlightColor),
        typeof(Color),
        typeof(ShimmerPanel),
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
        typeof(ShimmerPanel),
        new PropertyMetadata(ShimmerEasing.Linear, PropertyChanged));

    /// <summary>
    /// Identifies the <see cref="GradientShape"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GradientShapeProperty = DependencyProperty.Register(
        nameof(GradientShape),
        typeof(ShimmerGradientShape),
        typeof(ShimmerPanel),
        new PropertyMetadata(ShimmerGradientShape.Plateau, OnAppearanceChanged));

    /// <summary>
    /// Identifies the <see cref="ReverseGradient"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ReverseGradientProperty = DependencyProperty.Register(
        nameof(ReverseGradient),
        typeof(bool),
        typeof(ShimmerPanel),
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

    private const string PART_Shape = "Shape";

    private ShimmerSweepController? _controller;
    private ExpressionAnimation? _sizeExpression;
    private CompositionRoundedRectangleGeometry? _rectangleGeometry;
    private ShapeVisual? _shapeVisual;
    private CompositionLinearGradientBrush? _shimmerMaskGradient;
    private Border? _shape;

    private bool _initialized;

    public ShimmerPanel()
    {
        DefaultStyleKey = typeof(ShimmerPanel);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _shape = GetTemplateChild(PART_Shape) as Border;
        if (_initialized is false && TryInitializationResource())
        {
            UpdateActiveState();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized is false && TryInitializationResource())
        {
            UpdateActiveState();
        }

        ActualThemeChanged += OnActualThemeChanged;
        SizeChanged += OnSizeChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged -= OnActualThemeChanged;
        SizeChanged -= OnSizeChanged;
        _controller?.Dispose();

        if (_initialized && _shape != null)
        {
            ElementCompositionPreview.SetElementChildVisual(_shape, null);

            _sizeExpression?.Dispose();
            _rectangleGeometry!.Dispose();
            _shapeVisual!.Dispose();
            _shimmerMaskGradient!.Dispose();

            _controller = null;
            _sizeExpression = null;
            _initialized = false;
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (_initialized is false)
        {
            return;
        }

        RebuildGradientStops();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Absolute-space keyframes depend on the panel's pixel size, so recompute on resize.
        if (_initialized && IsActive)
        {
            _controller?.Stop();
            StartSweep();
        }
    }

    private bool TryInitializationResource()
    {
        if (_initialized)
        {
            return true;
        }

        if (_shape is null || IsLoaded is false)
        {
            return false;
        }

        var rootVisual = ElementCompositionPreview.GetElementVisual(_shape);
        var compositor = rootVisual.Compositor;

        _rectangleGeometry = compositor.CreateRoundedRectangleGeometry();
        _shapeVisual = compositor.CreateShapeVisual();
        _shimmerMaskGradient = compositor.CreateLinearGradientBrush();
        _shimmerMaskGradient.MappingMode = CompositionMappingMode.Absolute;

        _controller = new ShimmerSweepController(_shimmerMaskGradient);
        // Park the band off-screen and build the stops before the sweep runs (this is also the
        // resting appearance whenever IsActive is false).
        _controller.ParkOffscreen(BandWidth);
        _controller.RebuildStops(GradientShape, ReverseGradient, ActualTheme, HighlightOpacity, HighlightColor);

        _rectangleGeometry.CornerRadius = new Vector2((float)CornerRadius.TopLeft);
        var spriteShape = compositor.CreateSpriteShape(_rectangleGeometry);
        spriteShape.FillBrush = _shimmerMaskGradient;
        _shapeVisual.Shapes.Add(spriteShape);
        ElementCompositionPreview.SetElementChildVisual(_shape, _shapeVisual);

        // The overlay visual and geometry track the control size via a single expression animation
        // created once here and disposed on unload; it does not need restarting per property change.
        _sizeExpression = compositor.CreateExpressionAnimation("source.Size");
        _sizeExpression.SetReferenceParameter("source", rootVisual);
        _shapeVisual.StartAnimation(nameof(ShapeVisual.Size), _sizeExpression);
        _rectangleGeometry.StartAnimation(nameof(CompositionRoundedRectangleGeometry.Size), _sizeExpression);

        _initialized = true;
        return true;
    }

    private void RebuildGradientStops()
    {
        _controller?.RebuildStops(GradientShape, ReverseGradient, ActualTheme, HighlightOpacity, HighlightColor);
    }

    private void UpdateActiveState()
    {
        if (_initialized is false || _shapeVisual is null || _controller is null)
        {
            return;
        }

        if (IsActive)
        {
            _shapeVisual.IsVisible = true;
            _controller.Stop();
            StartSweep();
        }
        else
        {
            _controller.Stop();
            _shapeVisual.IsVisible = false;
        }
    }

    private void StartSweep()
    {
        if (_controller is null || _shape is null)
        {
            return;
        }

        _controller.Start((float)_shape.ActualWidth, (float)_shape.ActualHeight, Angle, BandWidth, Duration, Easing);
    }

    private static void PropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    {
        var self = (ShimmerPanel)s;
        self.UpdateActiveState();
    }

    private static void OnAppearanceChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    {
        var self = (ShimmerPanel)s;
        if (self._initialized)
        {
            self.RebuildGradientStops();
        }
    }
}
