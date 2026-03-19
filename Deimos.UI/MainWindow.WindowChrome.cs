using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Deimos.UI.Windowing;


namespace Deimos.UI;

public partial class MainWindow
{
    private readonly WindowChrome _windowChrome = new();

    private void InitializeWindowChrome()
    {
        SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs eventArgs)
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(windowHandle)?.AddHook(HandleWindowProcedureHook);
    }

    private IntPtr HandleWindowProcedureHook(IntPtr windowHandle, int messageIdentifier, 
        IntPtr unusedWindowParameterPointer, IntPtr longParameterPointer, ref bool isHandled)
    {
        _ = unusedWindowParameterPointer;
        return _windowChrome.HandleWindowProcedure(this, windowHandle, messageIdentifier, longParameterPointer, ref isHandled);
    }
    
    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        if (mouseButtonEventArgs.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        DragMove(); // Drag the custom title bar to move the window
    }

    private void Minimize_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        WindowState = WindowState.Minimized;    // Minimize the window
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        ToggleWindowState();    // Toggle between normal and maximized states
    }

    private void Close_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        Close();    // Close the current window
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;    // Switch maximize/restore
    }
}
