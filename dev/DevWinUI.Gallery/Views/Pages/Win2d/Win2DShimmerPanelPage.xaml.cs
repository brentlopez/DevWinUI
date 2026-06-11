namespace DevWinUIGallery.Views;

public sealed partial class Win2DShimmerPanelPage : Page
{
    public Win2DShimmerPanelPage()
    {
        InitializeComponent();
    }

    public TimeSpan GetDuration(double milliseconds)
    {
        return TimeSpan.FromMilliseconds(milliseconds <= 0 ? 1 : milliseconds);
    }

    public Windows.UI.Color GetHighlightColor(bool useCustom, Windows.UI.Color custom)
    {
        // A zero-alpha color tells the shimmer controls to fall back to the theme-default highlight.
        return useCustom ? custom : Microsoft.UI.Colors.Transparent;
    }

    // Typed enum sources for the combo boxes (ObservableCollection<T>, not a raw Array, which would
    // crash the XAML binding pipeline). The example bindings read the selected value directly.
    public System.Collections.ObjectModel.ObservableCollection<DevWinUI.ShimmerEasing> EasingValues { get; } =
        new(Enum.GetValues<DevWinUI.ShimmerEasing>());

    public System.Collections.ObjectModel.ObservableCollection<DevWinUI.ShimmerGradientShape> GradientShapeValues { get; } =
        new(Enum.GetValues<DevWinUI.ShimmerGradientShape>());

    public DevWinUI.ShimmerEasing ToEasing(object selected)
    {
        return selected is DevWinUI.ShimmerEasing easing ? easing : DevWinUI.ShimmerEasing.Linear;
    }

    public DevWinUI.ShimmerGradientShape ToShape(object selected)
    {
        return selected is DevWinUI.ShimmerGradientShape shape ? shape : DevWinUI.ShimmerGradientShape.Plateau;
    }
}
