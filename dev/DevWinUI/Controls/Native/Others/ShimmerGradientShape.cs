namespace DevWinUI;

/// <summary>
/// Defines the opacity profile of the shimmer highlight across the width of the band (its "gradient").
/// </summary>
public enum ShimmerGradientShape
{
    /// <summary>A soft, centered plateau with feathered edges (the default).</summary>
    Plateau,

    /// <summary>A linear ramp from transparent to full (0 → 1).</summary>
    Linear,

    /// <summary>A constant, near-solid band.</summary>
    Flat,

    /// <summary>A smooth half-sine bell (0 → 1 → 0).</summary>
    Sine,

    /// <summary>A sharp, centered bell.</summary>
    Gaussian,

    /// <summary>A linear rise then fall (0 → 1 → 0).</summary>
    Triangle,
}
