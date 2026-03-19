using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;


namespace Deimos.UI.Windowing;

public sealed partial class WindowChrome
{
    public IntPtr HandleWindowProcedure(Window window, IntPtr windowHandle, int messageIdentifier, 
        IntPtr longParameterPointer, ref bool isHandled)
    {
        const int getMinimumMaximumInformationMessageIdentifier = 0x0024;

        if (messageIdentifier != getMinimumMaximumInformationMessageIdentifier)
            return IntPtr.Zero;

        UpdateMinimumMaximumInformation(window, windowHandle, longParameterPointer);
        isHandled = true;

        return IntPtr.Zero;
    }

    private void UpdateMinimumMaximumInformation(Window window, IntPtr windowHandle, IntPtr longParameterPointer)
    {
        var minimumMaximumInformation =
            Marshal.PtrToStructure<MinimumMaximumInformation>(longParameterPointer);
        var monitorHandle = GetMonitorHandleFromWindow(windowHandle, MonitorDefaultToNearestFlag);

        if (monitorHandle != IntPtr.Zero)
        {
            var monitorInformation = new MonitorInformation
            {
                StructureSize = Marshal.SizeOf<MonitorInformation>()
            };

            if (!GetMonitorInformation(monitorHandle, ref monitorInformation))
                return;

            var workAreaRectangle = monitorInformation.WorkArea;
            var monitorAreaRectangle = monitorInformation.MonitorArea;

            minimumMaximumInformation.MaximumPositionPoint.Horizontal =
                Math.Abs(workAreaRectangle.Left - monitorAreaRectangle.Left);
            minimumMaximumInformation.MaximumPositionPoint.Vertical =
                Math.Abs(workAreaRectangle.Top - monitorAreaRectangle.Top);
            minimumMaximumInformation.MaximumSizePoint.Horizontal =
                Math.Abs(workAreaRectangle.Right - workAreaRectangle.Left);
            minimumMaximumInformation.MaximumSizePoint.Vertical =
                Math.Abs(workAreaRectangle.Bottom - workAreaRectangle.Top);
        }

        var windowSource = HwndSource.FromHwnd(windowHandle);

        if (windowSource?.CompositionTarget != null)
        {
            var transformToDevice = windowSource.CompositionTarget.TransformToDevice;

            minimumMaximumInformation.MinimumTrackSizePoint.Horizontal =
                (int)Math.Ceiling(window.MinWidth * transformToDevice.M11);
            minimumMaximumInformation.MinimumTrackSizePoint.Vertical =
                (int)Math.Ceiling(window.MinHeight * transformToDevice.M22);
        }

        Marshal.StructureToPtr(minimumMaximumInformation, longParameterPointer, true);
    }

    private const int MonitorDefaultToNearestFlag = 0x00000002;

    [LibraryImport("user32.dll", EntryPoint = "MonitorFromWindow")]
    private static partial IntPtr GetMonitorHandleFromWindow(IntPtr windowHandle, int defaultMonitorSelectionFlag);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInformation(IntPtr monitorHandle, ref MonitorInformation monitorInformation);

    [StructLayout(LayoutKind.Sequential)]
    private struct PointCoordinates
    {
        public int Horizontal;
        public int Vertical;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinimumMaximumInformation
    {
        public PointCoordinates ReservedPoint;
        public PointCoordinates MaximumSizePoint;
        public PointCoordinates MaximumPositionPoint;
        public PointCoordinates MinimumTrackSizePoint;
        public PointCoordinates MaximumTrackSizePoint;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RectangleBounds
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInformation
    {
        public int StructureSize;
        public RectangleBounds MonitorArea;
        public RectangleBounds WorkArea;
        public int Flags;
    }
}
