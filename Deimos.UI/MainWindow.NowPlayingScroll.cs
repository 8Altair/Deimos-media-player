using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;


namespace Deimos.UI;

public partial class MainWindow
{
    private Storyboard? _nowPlayingScrollStoryboard;

    private void InitializeNowPlayingScroll()
    {
        var textDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        textDescriptor.AddValueChanged(NowPlaying, (_, _) => UpdateNowPlayingScroll());
        NowPlaying.Loaded += (_, _) => UpdateNowPlayingScroll();
        NowPlayingViewport.SizeChanged += (_, _) => UpdateNowPlayingScroll();
    }

    private void UpdateNowPlayingScroll()
    {
        if (!NowPlaying.IsLoaded || NowPlayingViewport.ActualWidth <= 0)
            return;

        _nowPlayingScrollStoryboard?.Stop();
        _nowPlayingScrollStoryboard = null;
        NowPlayingTransform.X = 0;

        var text = NowPlaying.Text ?? string.Empty;
        if (text.Length == 0)
            return;

        var typeface = new Typeface(NowPlaying.FontFamily, NowPlaying.FontStyle, NowPlaying.FontWeight, 
            NowPlaying.FontStretch);
        var dpi = VisualTreeHelper.GetDpi(NowPlaying).PixelsPerDip;
        var formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 
            NowPlaying.FontSize, Brushes.Black, dpi);
        var overflow = formattedText.WidthIncludingTrailingWhitespace - NowPlayingViewport.ActualWidth;

        if (overflow <= 0)
            return;

        var seconds = overflow / 30.0;
        if (seconds < 4)
            seconds = 4;
        if (seconds > 12)
            seconds = 12;

        var animation = new DoubleAnimation
        {
            From = 0,
            To = -overflow,
            BeginTime = TimeSpan.FromSeconds(2),
            Duration = TimeSpan.FromSeconds(seconds),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        _nowPlayingScrollStoryboard = new Storyboard();
        Storyboard.SetTarget(animation, NowPlayingTransform);
        Storyboard.SetTargetProperty(animation, new PropertyPath(TranslateTransform.XProperty));
        _nowPlayingScrollStoryboard.Children.Add(animation);
        _nowPlayingScrollStoryboard.Begin();
    }
}
