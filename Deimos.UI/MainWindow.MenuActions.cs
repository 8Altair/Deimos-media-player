using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Deimos.UI.Windowing;


namespace Deimos.UI;

public partial class MainWindow
{
    private EditMediaWindow? _editWindow;

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

    /// <summary>
    /// Opens the edit media window as a non-modal dialog.
    /// </summary>
    private void EditMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
            Debug.WriteLine($"Detected: {menuItem.Header}");

        if (_viewModel.SelectedMedia is null)
            return;

        if (_editWindow is not null)
        {
            if (_editWindow.IsVisible)
            {
                _editWindow.UpdateMedia(_viewModel.SelectedMedia);
                if (_editWindow.WindowState == WindowState.Minimized)
                    _editWindow.WindowState = WindowState.Normal;
                _editWindow.Topmost = true;
                _editWindow.Activate();
                _editWindow.Focus();
                return;
            }

            _editWindow = null;
        }

        if (_editWindow is null)
        {
            _editWindow = new EditMediaWindow(_viewModel.SelectedMedia)
            {
                Owner = null,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true,
                Topmost = true
            };

            _editWindow.Closed += (_, _) => _editWindow = null;
            _editWindow.Show();
        }
    }

    private void ViewModel_OnPropertyChangedForEditWindow(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.MainViewModel.SelectedMedia))
            UpdateEditWindowSelection();
    }

    private void UpdateEditWindowSelection()
    {
        if (_editWindow is null || !_editWindow.IsVisible)
            return;

        _editWindow.UpdateMedia(_viewModel.SelectedMedia);
    }
}
