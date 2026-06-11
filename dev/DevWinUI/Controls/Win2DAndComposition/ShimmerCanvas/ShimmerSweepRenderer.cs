namespace DevWinUI;

/// <summary>
/// Draws the shimmer sweep into a Win2D <see cref="CanvasDrawingSession"/>. This is the Win2D
/// counterpart to <see cref="ShimmerSweepController"/>: where the controller animates a Composition
/// brush off-thread, this renderer paints the band immediately for the current frame, so it can be
/// composed as one layer inside an existing Win2D scene (over a Win2D-drawn image, geometry or text)
/// as well as driving the standalone <see cref="Win2DShimmerPanel"/>.
/// </summary>
/// <remarks>
/// All of the appearance and motion math is shared with the Composition controls via
/// <see cref="ShimmerSweep"/>, so the two backends stay visually identical and expose the same
/// parameters. The cached <see cref="CanvasLinearGradientBrush"/> is rebuilt only when the appearance
/// inputs change or the device is lost (call <see cref="ResetDeviceResources"/> from the host's
/// <c>CreateResources</c> handler).
/// </remarks>
internal sealed partial class ShimmerSweepRenderer : IDisposable
{
    private CanvasLinearGradientBrush? _brush;
    private bool _hasBrush;
    private ShimmerGradientShape _shape;
    private bool _reverse;
    private ElementTheme _theme;
    private double _opacity;
    private Color _color;

    /// <summary>
    /// Paints one frame of the sweep over the whole <paramref name="size"/>. No-op when the size is
    /// not yet known. Callers should skip drawing (and pause their loop) when the user prefers reduced
    /// motion / high contrast — check <see cref="ShimmerSweep.ShouldAnimate"/>. The method only touches
    /// the supplied Win2D objects, so it is safe to call from either the UI thread (a
    /// <c>CanvasControl</c>) or a Win2D render thread (a <c>CanvasAnimatedControl</c>).
    /// </summary>
    public void Draw(CanvasDrawingSession session, ICanvasResourceCreator resourceCreator, Size size, TimeSpan totalTime,
        double angle, double bandWidth, TimeSpan duration, ShimmerEasing easing,
        ShimmerGradientShape shape, bool reverse, ElementTheme theme, double opacity, Color highlightColor)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        EnsureBrush(resourceCreator, shape, reverse, theme, opacity, highlightColor);

        ShimmerSweep.ComputeKeyframes((float)size.Width, (float)size.Height, angle, (float)bandWidth, duration,
            out var startFrom, out var startTo, out var endFrom, out var endTo, out var period);

        var raw = period.Ticks <= 0 ? 0.0 : (totalTime.Ticks % period.Ticks) / (double)period.Ticks;
        var t = (float)ShimmerSweep.Ease(easing, raw);

        _brush!.StartPoint = Vector2.Lerp(startFrom, startTo, t);
        _brush.EndPoint = Vector2.Lerp(endFrom, endTo, t);

        session.FillRectangle(new Rect(0, 0, size.Width, size.Height), _brush);
    }

    /// <summary>Drops the cached device resources; call when the Win2D device is lost/recreated.</summary>
    public void ResetDeviceResources()
    {
        _brush?.Dispose();
        _brush = null;
        _hasBrush = false;
    }

    public void Dispose() => ResetDeviceResources();

    private void EnsureBrush(ICanvasResourceCreator resourceCreator,
        ShimmerGradientShape shape, bool reverse, ElementTheme theme, double opacity, Color highlightColor)
    {
        if (_hasBrush && _shape == shape && _reverse == reverse && _theme == theme
            && _opacity == opacity && _color.Equals(highlightColor))
        {
            return;
        }

        _brush?.Dispose();
        _brush = new CanvasLinearGradientBrush(resourceCreator,
            ShimmerSweep.BuildCanvasGradientStops(shape, reverse, theme, opacity, highlightColor),
            CanvasEdgeBehavior.Clamp, CanvasAlphaMode.Straight);

        _shape = shape;
        _reverse = reverse;
        _theme = theme;
        _opacity = opacity;
        _color = highlightColor;
        _hasBrush = true;
    }
}
