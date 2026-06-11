namespace DevWinUIGallery.Views;

public sealed partial class ShimmerPanelPage : Page
{
    public ShimmerPanelPage()
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

    // Typed enum sources for the combo boxes — adding an enum value automatically appears in the UI
    // and the example bindings read the selected value directly (no positional index coupling).
    // Uses ObservableCollection<T> (not a non-generic Array) to match the gallery's working pattern;
    // binding ItemsSource to a raw System.Array of boxed enums crashes the XAML pipeline (E_INVALIDARG).
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
