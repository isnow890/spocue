using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TrackRelay;

internal static class ArtworkPalette
{
    public static Color? Sample(ImageSource? source)
    {
        if (source is not BitmapSource bitmap)
        {
            return null;
        }

        const int sampleSize = 24;
        var reduced = new TransformedBitmap(bitmap,
            new ScaleTransform((double)sampleSize / bitmap.PixelWidth, (double)sampleSize / bitmap.PixelHeight));
        var converted = new FormatConvertedBitmap(reduced, PixelFormats.Bgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);

        const int hueBins = 24;
        var colors = new (double Weight, double R, double G, double B)[hueBins + 1];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            var alpha = pixels[index + 3] / 255.0;
            if (alpha < 0.25)
            {
                continue;
            }

            var r = pixels[index + 2];
            var g = pixels[index + 1];
            var b = pixels[index];
            var maximum = Math.Max(r, Math.Max(g, b));
            var minimum = Math.Min(r, Math.Min(g, b));
            var chroma = maximum - minimum;
            var bin = hueBins;
            if (chroma >= 14)
            {
                var hue = maximum == r ? (double)(g - b) / chroma :
                          maximum == g ? 2 + (double)(b - r) / chroma :
                                         4 + (double)(r - g) / chroma;
                bin = (int)Math.Round((hue + 6) * 4) % hueBins;
            }

            // Prefer substantial areas of color to near-black and gray cover backgrounds.
            var weight = alpha * (0.12 + chroma / 96.0) * (0.4 + maximum / 255.0);
            ref var color = ref colors[bin];
            color.Weight += weight;
            color.R += r * weight;
            color.G += g * weight;
            color.B += b * weight;
        }

        var chosen = colors.MaxBy(color => color.Weight);
        return chosen.Weight > 0
            ? Color.FromRgb((byte)(chosen.R / chosen.Weight), (byte)(chosen.G / chosen.Weight), (byte)(chosen.B / chosen.Weight))
            : null;
    }

    public static void Apply(FrameworkElement panel, Color? artworkColor)
    {
        if (artworkColor is not { } color || SystemParameters.HighContrast)
        {
            panel.Resources.Remove("SurfaceBrush");
            panel.Resources.Remove("AccentBrush");
            panel.Resources.Remove("BorderBrush");
            panel.Resources.Remove("MutedBrush");
            return;
        }

        var surface = ((SolidColorBrush)Application.Current.FindResource("SurfaceBrush")).Color;
        panel.Resources["SurfaceBrush"] = new LinearGradientBrush(
            Blend(surface, color, 0.26), Blend(surface, color, 0.05), 0);
        panel.Resources["AccentBrush"] = new SolidColorBrush(Blend(color, Colors.White, 0.58));
        panel.Resources["BorderBrush"] = new SolidColorBrush(Blend(Blend(surface, Colors.White, 0.14), color, 0.22));
        panel.Resources["MutedBrush"] = new SolidColorBrush(Color.FromRgb(216, 224, 235));
    }

    private static Color Blend(Color from, Color to, double ratio) => Color.FromRgb(
        (byte)Math.Round(from.R + (to.R - from.R) * ratio),
        (byte)Math.Round(from.G + (to.G - from.G) * ratio),
        (byte)Math.Round(from.B + (to.B - from.B) * ratio));
}
