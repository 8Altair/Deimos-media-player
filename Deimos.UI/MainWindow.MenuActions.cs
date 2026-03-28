using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Deimos.UI;

public partial class MainWindow
{
    /// <summary>
    /// Event handler for clicking Exit.
    /// sender = control that triggered the event, e = event data for this routed event.
    /// </summary>
    private void ExitClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Detected: " + ((MenuItem)sender).Name);    // Writes the clicked MenuItem name to debug output
        Debug.WriteLine("Shutting down application.");   // Writes a debug message before shutting down
        Application.Current.Shutdown(); // Closes the entire application
    }
}
