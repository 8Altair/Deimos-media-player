using System.ComponentModel;
using System.Diagnostics;   // Debug.WriteLine for debug output
using System.Globalization;
using System.Windows;   // Core WPF types like Window, Application, MessageBox
using System.Windows.Controls;  // WPF controls like MenuItem and ListViewItem
using System.Windows.Input; // Mouse button events
using System.Windows.Media;
using System.Windows.Media.Animation;

using System.Collections.ObjectModel;

using Deimos.UI.Models;
using Deimos.UI.Services;


namespace Deimos.UI;    // Defines the namespace this class belongs to

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow    // Connects partial logic from xaml file and inherits Window class
{
    public ObservableCollection<MediaFile> PlayList { get; } = []; // Notifies UI when items are added/removed
    private readonly MediaPlayback _mediaPlayback;
    private Storyboard? _nowPlayingScrollStoryboard;
    
    public MainWindow() // Constructor
    {
        InitializeComponent();  // Builds and connects the XAML UI components to this class
        InitializeWindowChrome();
        InitializeSeekBarLogic();

        _mediaPlayback = new MediaPlayback(PlayList, Player, ImageViewer, NowPlaying);
        _mediaPlayback.LoadDefaultMediaFiles();

        InitializeNowPlayingScroll();
        
        DataContext = this;
    }
    
    private void PlayList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Debug.WriteLine("Playlist double-click detected.");
        _mediaPlayback.PlaySelected(LvPlayList.SelectedItem as MediaFile);
    }

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

        var typeface = new Typeface(NowPlaying.FontFamily, NowPlaying.FontStyle, NowPlaying.FontWeight, NowPlaying.FontStretch);
        var dpi = VisualTreeHelper.GetDpi(NowPlaying).PixelsPerDip;
        var formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, NowPlaying.FontSize, Brushes.Black, dpi);
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
    
    /// <summary>
    /// Event handler for clicking Exit
    /// sender = control that triggered the event, e = event data for this routed event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ExitClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Detected: " + ((MenuItem)sender).Name);    // Writes the clicked MenuItem name to debug output
        Debug.WriteLine("Shutting down application.");   // Writes a debug message before shutting down
        Application.Current.Shutdown(); // Closes the entire application
    }
}
