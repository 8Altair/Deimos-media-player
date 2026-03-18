using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;


namespace Deimos.UI;

public sealed class WindowChrome
{
    public static IntPtr HandleWindowProc(Window window, IntPtr hwnd, int msg, IntPtr lParam, ref bool handled)
    {
        const int wmGetMinMaxInfo = 0x0024;

        if (msg != wmGetMinMaxInfo)
            return IntPtr.Zero;

        UpdateMinMaxInfo(window, hwnd, lParam);
        handled = true;

        return IntPtr.Zero;
    }

    private static void UpdateMinMaxInfo(Window window, IntPtr hwnd, IntPtr lParam)
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

            mmi.ptMinTrackSize.x = (int)Math.Ceiling(window.MinWidth * transformToDevice.M11);
            mmi.ptMinTrackSize.y = (int)Math.Ceiling(window.MinHeight * transformToDevice.M22);
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
}
