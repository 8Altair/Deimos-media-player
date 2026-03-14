using System.Text;
using System.Diagnostics;   // Debug.WriteLine for debug output
using System.Runtime.InteropServices;
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
    private bool _isDraggingSeekBar;
    private const double SeekMinimum = 0;
    private const double SeekMaximum = 100;
    private double _seekValue;
    
    public MainWindow() // Constructor
    {
        InitializeComponent();  // Builds and connects the XAML UI components to this class
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

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int wmGetMinMaxInfo = 0x0024;

        if (msg != wmGetMinMaxInfo) return IntPtr.Zero;
        WmGetMinMaxInfo(hwnd, lParam);
        handled = true;

        return IntPtr.Zero;
    }

    private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        var mmi = Marshal.PtrToStructure<MinMaxInfo>(lParam);

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);

        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo
            {
                cbSize = Marshal.SizeOf<MonitorInfo>()
            };
            GetMonitorInfo(monitor, ref monitorInfo);

            var workArea = monitorInfo.rcWork;
            var monitorArea = monitorInfo.rcMonitor;

            mmi.ptMaxPosition.x = Math.Abs(workArea.left - monitorArea.left);
            mmi.ptMaxPosition.y = Math.Abs(workArea.top - monitorArea.top);
            mmi.ptMaxSize.x = Math.Abs(workArea.right - workArea.left);
            mmi.ptMaxSize.y = Math.Abs(workArea.bottom - workArea.top);
        }

        var source = HwndSource.FromHwnd(hwnd);

        if (source?.CompositionTarget != null)
        {
            var transformToDevice = source.CompositionTarget.TransformToDevice;

            mmi.ptMinTrackSize.x = (int)Math.Ceiling(MinWidth * transformToDevice.M11);
            mmi.ptMinTrackSize.y = (int)Math.Ceiling(MinHeight * transformToDevice.M22);
        }

        Marshal.StructureToPtr(mmi, lParam, true);
    }

    private const int MonitorDefaultToNearest = 0x00000002;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public int dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
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
    
    private void SeekBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSeekBar = true;
        SeekBar.CaptureMouse();
        SetSeekBarValueFromMouse(e.GetPosition(SeekBar).X);
    }

    private void SeekBar_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingSeekBar || e.LeftButton != MouseButtonState.Pressed)
            return;

        SetSeekBarValueFromMouse(e.GetPosition(SeekBar).X);
    }

    private void SeekBar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingSeekBar)
            return;

        _isDraggingSeekBar = false;
        SetSeekBarValueFromMouse(e.GetPosition(SeekBar).X);
        SeekBar.ReleaseMouseCapture();
    }

    private void SeekBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSeekBarVisual();
    }

    private void SetSeekBarValueFromMouse(double mouseX)
    {
        var width = SeekBar.ActualWidth;

        if (width <= 0)
            return;

        var ratio = mouseX / width;
        ratio = Math.Max(0, Math.Min(1, ratio));

        _seekValue = SeekMinimum + (SeekMaximum - SeekMinimum) * ratio;
        UpdateSeekBarVisual();
    }

    private void UpdateSeekBarVisual()
    {
        var width = SeekBar.ActualWidth;

        if (width <= 0)
            return;

        const double range = SeekMaximum - SeekMinimum;

        if (range <= 0)
            return;

        var ratio = (_seekValue - SeekMinimum) / range;
        ratio = Math.Max(0, Math.Min(1, ratio));

        var progressWidth = width * ratio;
        SeekBarProgress.Width = progressWidth;

        var thumbLeft = progressWidth - SeekBarThumb.Width / 2;
        thumbLeft = Math.Max(0, Math.Min(width - SeekBarThumb.Width, thumbLeft));

        Canvas.SetLeft(SeekBarThumb, thumbLeft);
    }
    
    // private void PlayList_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    // {
    //     Debug.WriteLine("Click detected for: " + e.ChangedButton);  // Writes which mouse button changed
    //     MessageBox.Show("File name: " + ((ListViewItem)sender).Content);    // Shows a message box with the clicked item's content
    // }
}
