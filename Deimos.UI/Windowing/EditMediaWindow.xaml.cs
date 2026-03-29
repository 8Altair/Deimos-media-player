using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

using Deimos.UI.Models;


namespace Deimos.UI.Windowing;

public partial class EditMediaWindow : Window
{
    /// <summary>
    /// Updates the dialog bindings to the currently selected media item.
    /// </summary>
    public void UpdateMedia(MediaFile? mediaFile)
    {
        DataContext = mediaFile;
        Debug.WriteLine($"EditMediaWindow updated to: {mediaFile?.Title ?? "(none)"}");
    }

    /// <summary>
    /// Initializes the edit dialog and binds to the selected media item.
    /// </summary>
    public EditMediaWindow(MediaFile mediaFile)
    {
        InitializeComponent();
        UpdateMedia(mediaFile);
        Debug.WriteLine($"EditMediaWindow opened for: {mediaFile.Title}");
    }

    /// <summary>
    /// Enables drag-to-move for the custom title bar.
    /// </summary>
    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            Debug.WriteLine("EditMediaWindow title bar drag started.");
            DragMove();
        }
    }

    /// <summary>
    /// Closes the edit dialog.
    /// </summary>
    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("EditMediaWindow closed by user.");
        Close();
    }
}
