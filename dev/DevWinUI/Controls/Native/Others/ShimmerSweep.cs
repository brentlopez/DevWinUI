namespace DevWinUI;

/// <summary>
/// Shared math for the DevWinUI shimmer controls (<see cref="ShimmerPanel"/> and
/// <see cref="ShimmerMaskView"/>). The sweep is computed in absolute (pixel) space so its angle,
/// band width and velocity stay independent of the panel's size and aspect ratio.
/// </summary>
internal static class ShimmerSweep
{
    /// <summary>Pixels of travel that correspond to one unit of the control's Duration.</summary>
    public const float ReferenceTravel = 300.0f;

    public const double DefaultAngle = 45.0;
    public const double DefaultBandWidth = 180.0;
    public const double DefaultHighlightOpacity = 0.2;

    /// <summary>The default sweep <c>Duration</c> shared by every shimmer control.</summary>
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(1600);

    // Composition's KeyFrameAnimation.Duration accepts 1 ms .. 24 days; values outside that range throw.
    private static readonly long MinDurationTicks = TimeSpan.TicksPerMillisecond;
    private static readonly long MaxDurationTicks = TimeSpan.FromDays(24).Ticks;

    private const int StopCount = 32;

    private static Windows.UI.ViewManagement.UISettings? _uiSettings;
    private static Windows.UI.ViewManagement.AccessibilitySettings? _accessibilitySettings;

    /// <summary>
    /// A <see cref="Color"/> with zero alpha is treated as "unset", i.e. fall back to the
    /// theme-appropriate highlight color (white in dark, black in light).
    /// </summary>
    public static readonly Color ThemeDefaultColor = Color.FromArgb(0, 0, 0, 0);

    /// <summary>
    /// Returns <see langword="false"/> when the user has disabled animations (reduced motion) or is
    /// running a high-contrast theme; callers should then show a static, un-animated state. Read on
    /// the UI thread; reflects the current system state each call.
    /// </summary>
    public static bool ShouldAnimate()
    {
        try
        {
            _uiSettings ??= new Windows.UI.ViewManagement.UISettings();
            if (!_uiSettings.AnimationsEnabled)
            {
                return false;
            }
        }
        catch
        {
            // UISettings may be unavailable in some hosting contexts; assume animations are allowed.
        }

        try
        {
            _accessibilitySettings ??= new Windows.UI.ViewManagement.AccessibilitySettings();
            if (_accessibilitySettings.HighContrast)
            {
                return false;
            }
        }
        catch
        {
            // AccessibilitySettings may be unavailable; assume not high-contrast.
        }

        return true;
    }

    /// <summary>
    /// Rebuilds the brush's gradient stops for the given opacity profile, reverse flag, theme,
    /// opacity and (optional) explicit highlight color. The outermost stops are forced transparent
    /// so the band never bleeds past its width (a linear gradient clamps/extends its end stops).
    /// </summary>
    public static void BuildGradientStops(Compositor compositor, CompositionLinearGradientBrush brush,
        ShimmerGradientShape shape, bool reverse, ElementTheme theme, double opacity, Color highlightColor)
    {
        var rgb = ResolveRgb(theme, highlightColor);

        brush.ColorStops.Clear();
        for (var i = 0; i < StopCount; i++)
        {
            var offset = i / (float)(StopCount - 1);
            var alpha = StopAlpha(shape, reverse, i, StopCount, opacity);
            brush.ColorStops.Add(compositor.CreateColorGradientStop(offset, Color.FromArgb(alpha, rgb.R, rgb.G, rgb.B)));
        }
    }

    /// <summary>
    /// Builds the same feathered band as <see cref="BuildGradientStops"/>, but as Win2D
    /// <see cref="CanvasGradientStop"/> values for use with a <c>CanvasLinearGradientBrush</c>.
    /// Both backends share the identical opacity-profile math so the appearance matches.
    /// </summary>
    public static CanvasGradientStop[] BuildCanvasGradientStops(
        ShimmerGradientShape shape, bool reverse, ElementTheme theme, double opacity, Color highlightColor)
    {
        var rgb = ResolveRgb(theme, highlightColor);

        var stops = new CanvasGradientStop[StopCount];
        for (var i = 0; i < StopCount; i++)
        {
            var offset = i / (float)(StopCount - 1);
            var alpha = StopAlpha(shape, reverse, i, StopCount, opacity);
            stops[i] = new CanvasGradientStop { Position = offset, Color = Color.FromArgb(alpha, rgb.R, rgb.G, rgb.B) };
        }

        return stops;
    }

    /// <summary>Resolves the highlight RGB, falling back to the theme color when alpha is zero.</summary>
    private static Color ResolveRgb(ElementTheme theme, Color highlightColor) => highlightColor.A == 0
        ? (theme == ElementTheme.Light ? Colors.Black : Colors.White)
        : highlightColor;

    /// <summary>
    /// Resolves the resting skeleton fill color, falling back to the theme skeleton gray (a light gray
    /// in light, a dark gray in dark) when the supplied color is fully transparent.
    /// </summary>
    public static Color ResolveSkeletonColor(ElementTheme theme, Color skeletonColor) => skeletonColor.A == 0
        ? (theme == ElementTheme.Light ? Color.FromArgb(0xFF, 0xDA, 0xDA, 0xDA) : Color.FromArgb(0xFF, 0x42, 0x42, 0x42))
        : skeletonColor;

    /// <summary>
    /// The peak alpha for the stop at index <paramref name="i"/>: the outermost stops are forced
    /// transparent (so the band never bleeds past its width) and the interior follows the
    /// <see cref="ShimmerGradientShape"/> opacity profile scaled by <paramref name="opacity"/>.
    /// </summary>
    private static byte StopAlpha(ShimmerGradientShape shape, bool reverse, int i, int count, double opacity)
    {
        var offset = i / (double)(count - 1);
        var sample = reverse ? 1.0 - offset : offset;
        var profile = (i == 0 || i == count - 1) ? 0.0 : Math.Clamp(Evaluate(shape, sample), 0.0, 1.0);
        return (byte)Math.Clamp(opacity * profile * 255.0, 0.0, 255.0);
    }

    /// <summary>
    /// Creates the easing function applied to the sweep's key frames (Composition backend).
    /// </summary>
    public static CompositionEasingFunction CreateEasingFunction(Compositor compositor, ShimmerEasing easing) => easing switch
    {
        ShimmerEasing.EaseIn => compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0.0f), new Vector2(1.0f, 1.0f)),
        ShimmerEasing.EaseOut => compositor.CreateCubicBezierEasingFunction(new Vector2(0.0f, 0.0f), new Vector2(0.58f, 1.0f)),
        ShimmerEasing.EaseInOut => compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0.0f), new Vector2(0.58f, 1.0f)),
        _ => compositor.CreateLinearEasingFunction(),
    };

    /// <summary>
    /// Eases a normalized progress value (0..1) using the same cubic-bézier control points as
    /// <see cref="CreateEasingFunction"/>. Used by the Win2D backend, which owns its own clock and
    /// must ease progress in managed code rather than via a <see cref="CompositionEasingFunction"/>.
    /// </summary>
    public static double Ease(ShimmerEasing easing, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return easing switch
        {
            ShimmerEasing.EaseIn => CubicBezier(t, 0.42, 0.0, 1.0, 1.0),
            ShimmerEasing.EaseOut => CubicBezier(t, 0.0, 0.0, 0.58, 1.0),
            ShimmerEasing.EaseInOut => CubicBezier(t, 0.42, 0.0, 0.58, 1.0),
            _ => t,
        };
    }

    // Standard CSS-style cubic-bézier easing with end points (0,0) and (1,1). Solves x(u) == t for u
    // via a few Newton iterations, then returns y(u).
    private static double CubicBezier(double t, double x1, double y1, double x2, double y2)
    {
        var u = t;
        for (var i = 0; i < 8; i++)
        {
            var x = BezierAxis(u, x1, x2) - t;
            if (Math.Abs(x) < 1e-5)
            {
                break;
            }

            var d = BezierAxisDerivative(u, x1, x2);
            if (Math.Abs(d) < 1e-6)
            {
                break;
            }

            u = Math.Clamp(u - (x / d), 0.0, 1.0);
        }

        return BezierAxis(u, y1, y2);
    }

    private static double BezierAxis(double u, double c1, double c2)
    {
        var omu = 1.0 - u;
        return (3.0 * omu * omu * u * c1) + (3.0 * omu * u * u * c2) + (u * u * u);
    }

    private static double BezierAxisDerivative(double u, double c1, double c2)
    {
        var omu = 1.0 - u;
        return (3.0 * omu * omu * c1) + (6.0 * omu * u * (c2 - c1)) + (3.0 * u * u * (1.0 - c2));
    }

    private static double Evaluate(ShimmerGradientShape shape, double t) => shape switch
    {
        ShimmerGradientShape.Linear => t,
        ShimmerGradientShape.Flat => 1.0,
        ShimmerGradientShape.Sine => Math.Sin(Math.PI * t),
        ShimmerGradientShape.Gaussian => Math.Exp(-((t - 0.5) * (t - 0.5)) / (2.0 * 0.16 * 0.16)),
        ShimmerGradientShape.Triangle => 1.0 - Math.Abs((2.0 * t) - 1.0),
        _ => Plateau(t),
    };

    // Matches the original feathered trapezoid (stops at 0.35 / 0.48 / 0.52 / 0.65).
    private static double Plateau(double t)
    {
        if (t <= 0.35 || t >= 0.65)
        {
            return 0.0;
        }

        if (t < 0.48)
        {
            return (t - 0.35) / (0.48 - 0.35);
        }

        if (t > 0.52)
        {
            return (0.65 - t) / (0.65 - 0.52);
        }

        return 1.0;
    }

    /// <summary>
    /// Computes the StartPoint/EndPoint key frames for a fixed-width band that travels along the
    /// given <paramref name="angleDegrees"/> from just off one corner to just off the opposite
    /// corner, plus the size-compensated duration that keeps the pixel velocity constant.
    /// </summary>
    public static void ComputeKeyframes(float width, float height, double angleDegrees, float bandWidth, TimeSpan duration,
        out Vector2 startFrom, out Vector2 startTo, out Vector2 endFrom, out Vector2 endTo, out TimeSpan sweepDuration)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        var dx = (float)Math.Cos(radians);
        var dy = (float)Math.Sin(radians);

        // Project the four corners onto the sweep axis to find how far the band must travel.
        var p1 = width * dx;
        var p2 = height * dy;
        var p3 = p1 + p2;
        var minProjection = Math.Min(0f, Math.Min(p1, Math.Min(p2, p3)));
        var maxProjection = Math.Max(0f, Math.Max(p1, Math.Max(p2, p3)));

        var posStart = minProjection - bandWidth;
        var posEnd = maxProjection;
        var travel = posEnd - posStart;

        startFrom = new Vector2(posStart * dx, posStart * dy);
        startTo = new Vector2(posEnd * dx, posEnd * dy);
        endFrom = new Vector2((posStart + bandWidth) * dx, (posStart + bandWidth) * dy);
        endTo = new Vector2((posEnd + bandWidth) * dx, (posEnd + bandWidth) * dy);

        // Velocity is constant, so duration scales with travel. Compute in double to avoid the
        // long multiply overflowing for very large Duration values, then clamp to Composition's
        // documented [1 ms, 24 days] range (the earlier 1-tick floor was 10,000x too small and threw).
        var factor = travel <= 0 ? 1.0 : travel / ReferenceTravel;
        var scaledTicks = duration.Ticks * factor;
        var clampedTicks = (long)Math.Clamp(scaledTicks, MinDurationTicks, MaxDurationTicks);
        sweepDuration = TimeSpan.FromTicks(clampedTicks);
    }
}

