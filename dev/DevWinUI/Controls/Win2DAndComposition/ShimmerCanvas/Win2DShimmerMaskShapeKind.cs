namespace DevWinUI;

/// <summary>
/// The kind of placeholder primitive used to build a <see cref="Win2DShimmerMaskView"/> mask.
/// </summary>
public enum Win2DShimmerMaskShapeKind
{
    /// <summary>A rectangle, optionally rounded by <see cref="Win2DShimmerMaskShape.CornerRadius"/>.</summary>
    RoundedRectangle,

    /// <summary>An ellipse that fills the shape's bounds.</summary>
    Ellipse
}
