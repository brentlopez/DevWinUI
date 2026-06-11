namespace DevWinUI;

/// <summary>
/// Owns the animated-brush lifecycle shared by <see cref="ShimmerPanel"/> and
/// <see cref="ShimmerMaskView"/>: it rebuilds the gradient stops, parks the band off-screen, and
/// starts/stops the StartPoint/EndPoint key-frame animations (plus the easing function) on a single
/// <see cref="CompositionLinearGradientBrush"/>.
/// </summary>
/// <remarks>
/// The two controls cannot share a control base (<see cref="ContentControl"/> vs
/// <see cref="RedirectVisualView"/>), so the sweep is shared by composition rather than inheritance.
/// Each control creates one controller over its brush and forwards Start/Stop/Rebuild calls; the
/// host-specific wiring (the overlay visual, the size expression, the mask brush) stays in the
/// control.
/// </remarks>
internal sealed partial class ShimmerSweepController : IDisposable
{
    private readonly Compositor _compositor;
    private readonly CompositionLinearGradientBrush _brush;

    private Vector2KeyFrameAnimation? _startPointAnimation;
    private Vector2KeyFrameAnimation? _endPointAnimation;
    private CompositionEasingFunction? _easingFunction;
    private bool _running;

    public ShimmerSweepController(CompositionLinearGradientBrush brush)
    {
        _brush = brush;
        _compositor = brush.Compositor;
    }

    public bool IsRunning => _running;

    /// <summary>Rebuilds the gradient stops for the current appearance (shape, reverse, theme, opacity, color).</summary>
    public void RebuildStops(ShimmerGradientShape shape, bool reverse, ElementTheme theme, double opacity, Color highlightColor)
    {
        ShimmerSweep.BuildGradientStops(_compositor, _brush, shape, reverse, theme, opacity, highlightColor);
    }

    /// <summary>Positions the band just off the top-left corner so nothing is drawn until the sweep runs.</summary>
    public void ParkOffscreen(double bandWidth)
    {
        _brush.StartPoint = new Vector2(-(float)bandWidth, -(float)bandWidth);
        _brush.EndPoint = new Vector2(0.0f, 0.0f);
    }

    /// <summary>
    /// Starts the looping sweep for the given pixel size and parameters. No-op (and parks the band
    /// off-screen) when the size is not yet known or when the user prefers reduced motion / high
    /// contrast (<see cref="ShimmerSweep.ShouldAnimate"/>).
    /// </summary>
    public void Start(float width, float height, double angle, double bandWidth, TimeSpan duration, ShimmerEasing easing)
    {
        if (_running)
        {
            return;
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (!ShimmerSweep.ShouldAnimate())
        {
            // Reduced motion / high contrast: show a static, un-animated state.
            ParkOffscreen(bandWidth);
            return;
        }

        ShimmerSweep.ComputeKeyframes(width, height, angle, (float)bandWidth, duration,
            out var startFrom, out var startTo, out var endFrom, out var endTo, out var sweepDuration);

        _easingFunction = ShimmerSweep.CreateEasingFunction(_compositor, easing);

        _startPointAnimation = _compositor.CreateVector2KeyFrameAnimation();
        _startPointAnimation.Duration = sweepDuration;
        _startPointAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
        _startPointAnimation.InsertKeyFrame(0.0f, startFrom);
        _startPointAnimation.InsertKeyFrame(1.0f, startTo, _easingFunction);
        _brush.StartAnimation(nameof(CompositionLinearGradientBrush.StartPoint), _startPointAnimation);

        _endPointAnimation = _compositor.CreateVector2KeyFrameAnimation();
        _endPointAnimation.Duration = sweepDuration;
        _endPointAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
        _endPointAnimation.InsertKeyFrame(0.0f, endFrom);
        _endPointAnimation.InsertKeyFrame(1.0f, endTo, _easingFunction);
        _brush.StartAnimation(nameof(CompositionLinearGradientBrush.EndPoint), _endPointAnimation);

        _running = true;
    }

    /// <summary>Stops the sweep and disposes the transient animation objects (the brush is left intact).</summary>
    public void Stop()
    {
        if (_running is false)
        {
            return;
        }

        _brush.StopAnimation(nameof(CompositionLinearGradientBrush.StartPoint));
        _brush.StopAnimation(nameof(CompositionLinearGradientBrush.EndPoint));

        _startPointAnimation?.Dispose();
        _startPointAnimation = null;
        _endPointAnimation?.Dispose();
        _endPointAnimation = null;
        _easingFunction?.Dispose();
        _easingFunction = null;
        _running = false;
    }

    public void Dispose() => Stop();
}
