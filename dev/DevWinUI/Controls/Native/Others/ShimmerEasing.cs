namespace DevWinUI;

/// <summary>
/// Defines how the shimmer sweep accelerates as it crosses the control.
/// </summary>
public enum ShimmerEasing
{
    /// <summary>Constant velocity (no easing).</summary>
    Linear,

    /// <summary>Starts slow and accelerates.</summary>
    EaseIn,

    /// <summary>Starts fast and decelerates.</summary>
    EaseOut,

    /// <summary>Accelerates then decelerates.</summary>
    EaseInOut,
}
