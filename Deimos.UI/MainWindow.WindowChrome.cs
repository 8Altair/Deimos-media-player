using System.Diagnostics;
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
        Debug.WriteLine("MainWindow: Initializing window chrome."); // Trace chrome setup entry
        SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs eventArgs)
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        Debug.WriteLine($"MainWindow: Window handle acquired: 0x{windowHandle.ToInt64():X}.");  // Trace handle value
        HwndSource.FromHwnd(windowHandle)?.AddHook(HandleWindowProcedureHook);
    }

    private IntPtr HandleWindowProcedureHook(IntPtr windowHandle, int messageIdentifier, 
        IntPtr unusedWindowParameterPointer, IntPtr longParameterPointer, ref bool isHandled)
    {
        _ = unusedWindowParameterPointer;
        Debug.WriteLine($"MainWindow: Window procedure hook received message {messageIdentifier}.");    // Trace message flow
        return WindowChrome.HandleWindowProcedure(this, windowHandle, messageIdentifier, longParameterPointer, ref isHandled);
    }
    
    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        if (mouseButtonEventArgs.ClickCount == 2)
        {
            Debug.WriteLine("MainWindow: Title bar double-click detected.");    // Trace maximize/restore intent
            ToggleWindowState();
            return;
        }

        Debug.WriteLine("MainWindow: Title bar drag initiated.");   // Trace window move intent
        DragMove(); // Drag the custom title bar to move the window
    }

    private void Minimize_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        Debug.WriteLine("MainWindow: Minimize clicked.");   // Trace minimize action
        WindowState = WindowState.Minimized;    // Minimize the window
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        Debug.WriteLine("MainWindow: Maximize/Restore clicked.");   // Trace maximize/restore action
        ToggleWindowState();    // Toggle between normal and maximized states
    }

    private void Close_Click(object sender, RoutedEventArgs routedEventArgs)
    {
        Debug.WriteLine("MainWindow: Close clicked.");  // Trace close action
        Close();    // Close the current window
    }

    private void ToggleWindowState()
    {
        Debug.WriteLine($"MainWindow: Toggling window state from {WindowState}.");  // Trace state transition.
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;    // Switch maximize/restore
    }
}
