using System.Text;
using System.Diagnostics;   // Debug.WriteLine for debug output
using System.Windows;   // Core WPF types like Window, Application, MessageBox
using System.Windows.Interop;
using System.Windows.Controls;  // WPF controls like MenuItem and ListViewItem
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input; // Input-related types like MouseButtonEventArgs
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Deimos.UI;    // Defines the namespace this class belongs to

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window    // Connects partial logic from xaml file and inherits Window class
{
/*
    private readonly WindowChrome _windowChromeLogic = new();
*/
    private readonly SeekBar _seekBar;

    public MainWindow() // Constructor
    {
        InitializeComponent();  // Builds and connects the XAML UI components to this class
        _seekBar = new SeekBar();
        SourceInitialized += MainWindow_SourceInitialized;
        
        UpdateSeekBarVisual();  // Calling a method for seek bar (visual) update
        
        // Temporary test items
        LvPlayList.Items.Add("Song1.mp3");
        LvPlayList.Items.Add("Song2.mp3");
        LvPlayList.Items.Add("Song3.mp3");
    }
    
    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr _, IntPtr lParam, ref bool handled)
    {
        return WindowChrome.HandleWindowProc(this, hwnd, msg, lParam, ref handled);
    }
    
    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        DragMove(); // Drag the custom title bar to move the window
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;    // Minimize the window
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();    // Toggle between normal and maximized states
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();    // Close the current window
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;    // Switch maximize/restore
    }

    private void SeekBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _seekBar.BeginDrag();
        SeekBar.CaptureMouse();
        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);
    }

    private void SeekBar_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_seekBar.IsDragging || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);
    }

    private void SeekBar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_seekBar.IsDragging)
            return;

        _seekBar.EndDrag();
        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);
        SeekBar.ReleaseMouseCapture();
    }

    private void SeekBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSeekBarVisual();
    }

    private void UpdateSeekBarFromMouse(double mouseX)
    {
        if (!_seekBar.UpdateFromMouse(mouseX, SeekBar.ActualWidth))
            return;

        UpdateSeekBarVisual();
    }

    private void UpdateSeekBarVisual()
    {
        var width = SeekBar.ActualWidth;

        if (width <= 0)
            return;

        var ratio = _seekBar.GetRatio();
        var progressWidth = width * ratio;
        SeekBarProgress.Width = progressWidth;

        var thumbLeft = progressWidth - SeekBarThumb.Width / 2;
        thumbLeft = Math.Max(0, Math.Min(width - SeekBarThumb.Width, thumbLeft));

        Canvas.SetLeft(SeekBarThumb, thumbLeft);
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
    
    // private void PlayList_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    // {
    //     Debug.WriteLine("Click detected for: " + e.ChangedButton);  // Writes which mouse button changed
    //     MessageBox.Show("File name: " + ((ListViewItem)sender).Content);    // Shows a message box with the clicked item's content
    // }
}
