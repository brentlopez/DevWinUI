namespace DevWinUI;

/// <summary>
/// A device-independent placeholder primitive (an ellipse or a rounded rectangle, positioned in the
/// control's local pixel coordinates) used to build the mask of a <see cref="Win2DShimmerMaskView"/>.
/// The union of all of a view's shapes both draws the resting skeleton and clips the moving highlight.
/// </summary>
public sealed partial class Win2DShimmerMaskShape
{
    /// <summary>Gets or sets the kind of primitive. Defaults to <see cref="Win2DShimmerMaskShapeKind.RoundedRectangle"/>.</summary>
    public Win2DShimmerMaskShapeKind Kind { get; set; } = Win2DShimmerMaskShapeKind.RoundedRectangle;

    /// <summary>Gets or sets the left edge of the shape, in the control's local coordinates.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the top edge of the shape, in the control's local coordinates.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the width of the shape.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the height of the shape.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the corner radius applied when <see cref="Kind"/> is a rounded rectangle.</summary>
    public double CornerRadius { get; set; }
}
