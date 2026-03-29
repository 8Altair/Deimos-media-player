using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

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

    private void BrowseFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = BuildMediaFilter(),
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        if (DataContext is not MediaFile mediaFile)
            return;

        mediaFile.FilePath = dialog.FileName;
        if (TryResolveExtension(dialog.FileName, out var extension) &&
            MediaExtensions.ImageExtensions.Contains(extension))
        {
            mediaFile.ImagePath = dialog.FileName;
        }
    }

    private static string BuildMediaFilter()
    {
        var audio = string.Join(";", MediaExtensions.AudioExtensions.Select(ext => $"*{ext}"));
        var video = string.Join(";", MediaExtensions.VideoExtensions.Select(ext => $"*{ext}"));
        var images = string.Join(";", MediaExtensions.ImageExtensions.Select(ext => $"*{ext}"));
        var all = string.Join(";", audio, video, images);

        return $"All supported media|{all}|Audio files|{audio}|Video files|{video}|Image files|{images}|All files|*.*";
    }

    private static bool TryResolveExtension(string path, out string extension)
    {
        extension = string.Empty;
        if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
        {
            var uriPath = uri.IsFile ? uri.LocalPath : uri.AbsolutePath;
            extension = Path.GetExtension(uriPath).ToLowerInvariant();
            return !string.IsNullOrWhiteSpace(extension);
        }

        extension = Path.GetExtension(path).ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(extension);
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
