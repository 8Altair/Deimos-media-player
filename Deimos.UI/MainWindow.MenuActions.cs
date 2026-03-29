using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

using Deimos.UI.Windowing;


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

    /// <summary>
    /// Opens the add media window as a modal dialog.
    /// </summary>
    private void AddMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
            Debug.WriteLine($"Detected: {menuItem.Header}");

        var addWindow = new AddMediaWindow(_viewModel.PlayList)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        addWindow.ShowDialog();
    }
}
