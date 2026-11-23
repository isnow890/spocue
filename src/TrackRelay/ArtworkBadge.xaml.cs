using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TrackRelay;

internal sealed partial class ArtworkBadge : UserControl
{
    public static readonly DependencyProperty ArtworkProperty = DependencyProperty.Register(
        nameof(Artwork), typeof(ImageSource), typeof(ArtworkBadge));

    public ArtworkBadge() => InitializeComponent();

    public ImageSource? Artwork
    {
        get => (ImageSource?)GetValue(ArtworkProperty);
        set => SetValue(ArtworkProperty, value);
    }
}
