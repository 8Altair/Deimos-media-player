using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

using Deimos.UI.Models;


namespace Deimos.UI.Windowing;

public partial class EditMediaWindow
{
    private const string DefaultCoverPath = "pack://application:,,,/Assets/Default_cover/Default.png";
    private MediaFile? _mediaFile;

    /// <summary>
    /// Updates the dialog bindings to the currently selected media item.
    /// </summary>
    public void UpdateMedia(MediaFile? mediaFile)
    {
        _mediaFile = mediaFile;
        DataContext = mediaFile;
        ApplyMediaToForm(mediaFile);
        UpdateDurationInputState();
        HideSaveStatus();
        Debug.WriteLine($"EditMediaWindow updated to: {mediaFile?.Title ?? "(none)"}");
    }

    /// <summary>
    /// Initializes the edit dialog and binds to the selected media item.
    /// </summary>
    public EditMediaWindow(MediaFile mediaFile)
    {
        InitializeComponent();
        Closing += EditMediaWindow_OnClosing;
        RegisterStatusResetHandlers();
        UpdateDurationInputState();
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

        if (dialog.ShowDialog(this) == true)
        {
            FilePathTextBox.Text = dialog.FileName;
            if (TryResolveExtension(dialog.FileName, out var extension) &&
                MediaExtensions.ImageExtensions.Contains(extension))
                ImagePathTextBox.Text = dialog.FileName;
        }
    }

    private void ImagePreview_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = BuildImageFilter(),
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
            ImagePathTextBox.Text = dialog.FileName;
    }

    private void FilePathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateDurationInputState();
    }

    private static string BuildMediaFilter()
    {
        var audio = string.Join(";", MediaExtensions.AudioExtensions.Select(ext => $"*{ext}"));
        var video = string.Join(";", MediaExtensions.VideoExtensions.Select(ext => $"*{ext}"));
        var images = string.Join(";", MediaExtensions.ImageExtensions.Select(ext => $"*{ext}"));
        var all = string.Join(";", audio, video, images);

        return $"All supported media|{all}|Audio files|{audio}|Video files|{video}|Image files|{images}|All files|*.*";
    }

    private static string BuildImageFilter()
    {
        var images = string.Join(";", MediaExtensions.ImageExtensions.Select(ext => $"*{ext}"));
        return $"Image files|{images}|All files|*.*";
    }

    private static bool TryResolveExtension(string path, out string extension)
    {
        extension = string.Empty;
        if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
        {
            var uriPath = uri.IsAbsoluteUri
                ? (uri.IsFile ? uri.LocalPath : uri.AbsolutePath)
                : uri.OriginalString;
            extension = Path.GetExtension(uriPath).ToLowerInvariant();
            return !string.IsNullOrWhiteSpace(extension);
        }

        extension = Path.GetExtension(path).ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(extension);
    }

    /// <summary>
    /// Saves validated edits into the selected media item.
    /// </summary>
    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (_mediaFile is null)
        {
            Debug.WriteLine("EditMediaWindow save skipped: no selected media.");
            MessageBox.Show(this, "No media item is selected.", "Cannot save", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!TryReadMediaInputs(out var title, out var filePath, out var imagePath, out var duration,
                out var artist, out var album, out var isPlaying, out var errorMessage))
        {
            Debug.WriteLine($"EditMediaWindow validation failed: {errorMessage}");
            MessageBox.Show(this, errorMessage, "Invalid input", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _mediaFile.Title = title;
        _mediaFile.FilePath = filePath;
        _mediaFile.ImagePath = imagePath;
        _mediaFile.Duration = duration;
        _mediaFile.Artist = artist;
        _mediaFile.Album = album;
        _mediaFile.IsPlaying = isPlaying;

        Debug.WriteLine($"EditMediaWindow saved updates for: {_mediaFile.Title}");
        ApplyMediaToForm(_mediaFile);
        ShowSaveStatus("Changes saved");
    }

    /// <summary>
    /// Reads and validates the form fields, returning normalized values.
    /// </summary>
    private bool TryReadMediaInputs(out string title, out string filePath, out string imagePath,
        out TimeSpan duration, out string? artist, out string? album, out bool isPlaying, out string errorMessage)
    {
        title = TitleTextBox.Text.Trim();
        filePath = FilePathTextBox.Text.Trim();
        imagePath = ImagePathTextBox.Text.Trim();
        var durationText = DurationTextBox.Text.Trim();
        var artistText = ArtistTextBox.Text.Trim();
        var albumText = AlbumTextBox.Text.Trim();
        isPlaying = IsPlayingCheckBox.IsChecked == true;

        if (string.IsNullOrWhiteSpace(title))
        {
            errorMessage = "Title is required.";
            duration = TimeSpan.Zero;
            artist = null;
            album = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "File path is required.";
            duration = TimeSpan.Zero;
            artist = null;
            album = null;
            return false;
        }

        if (!TryResolveExtension(filePath, out var extension))
        {
            errorMessage = "File path is not a valid URI or file path.";
            duration = TimeSpan.Zero;
            artist = null;
            album = null;
            return false;
        }

        if (!IsSupportedMediaExtension(extension))
        {
            errorMessage = "Unsupported media format.";
            duration = TimeSpan.Zero;
            artist = null;
            album = null;
            return false;
        }

        if (Uri.TryCreate(filePath, UriKind.RelativeOrAbsolute, out var fileUri) &&
            fileUri.IsAbsoluteUri && fileUri.IsFile)
        {
            if (!File.Exists(fileUri.LocalPath))
            {
                errorMessage = "The selected file does not exist.";
                duration = TimeSpan.Zero;
                artist = null;
                album = null;
                return false;
            }
        }
        else if (!Path.IsPathRooted(filePath))
        {
            errorMessage = "File path must be a valid absolute path.";
            duration = TimeSpan.Zero;
            artist = null;
            album = null;
            return false;
        }

        duration = TimeSpan.Zero;
        if (!MediaExtensions.ImageExtensions.Contains(extension))
        {
            if (!TryGetActualDuration(filePath, out var actualDuration, out var durationError))
            {
                errorMessage = durationError;
                artist = null;
                album = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(durationText))
            {
                duration = actualDuration;
            }
            else if (!TimeSpan.TryParse(durationText, out duration))
            {
                errorMessage = "Duration must be in hh:mm:ss format.";
                artist = null;
                album = null;
                return false;
            }

            if (duration <= TimeSpan.Zero)
            {
                errorMessage = "Duration must be greater than 00:00:00.";
                artist = null;
                album = null;
                return false;
            }

            if (duration > actualDuration)
            {
                errorMessage = "Duration cannot be longer than the media file duration.";
                artist = null;
                album = null;
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            imagePath = MediaExtensions.ImageExtensions.Contains(extension)
                ? filePath
                : DefaultCoverPath;
        }
        else if (!TryResolveExtension(imagePath, out var imageExtension) ||
                 !MediaExtensions.ImageExtensions.Contains(imageExtension))
        {
            errorMessage = "Image path must point to a supported image file.";
            artist = null;
            album = null;
            return false;
        }

        artist = string.IsNullOrWhiteSpace(artistText) ? null : artistText;
        album = string.IsNullOrWhiteSpace(albumText) ? null : albumText;
        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Returns true when the extension matches supported media types.
    /// </summary>
    private static bool IsSupportedMediaExtension(string extension)
    {
        return MediaExtensions.AudioExtensions.Contains(extension) ||
               MediaExtensions.VideoExtensions.Contains(extension) ||
               MediaExtensions.ImageExtensions.Contains(extension);
    }

    /// <summary>
    /// Closes the window, warning if there are unsaved edits.
    /// </summary>
    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void EditMediaWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!HasUnsavedChanges())
            return;

        var result = MessageBox.Show(this, "You have unsaved changes. Close without saving?",
            "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            e.Cancel = true;
    }

    /// <summary>
    /// Populates the form fields with the media item values.
    /// </summary>
    private void ApplyMediaToForm(MediaFile? mediaFile)
    {
        TitleTextBox.Text = mediaFile?.Title ?? string.Empty;
        FilePathTextBox.Text = mediaFile?.FilePath ?? string.Empty;
        ImagePathTextBox.Text = mediaFile?.ImagePath ?? string.Empty;
        DurationTextBox.Text = mediaFile is null ? string.Empty : mediaFile.Duration.ToString(@"hh\:mm\:ss");
        ArtistTextBox.Text = mediaFile?.Artist ?? string.Empty;
        AlbumTextBox.Text = mediaFile?.Album ?? string.Empty;
        IsPlayingCheckBox.IsChecked = mediaFile?.IsPlaying ?? false;
    }

    private void RegisterStatusResetHandlers()
    {
        TitleTextBox.TextChanged += (_, _) => HideSaveStatus();
        FilePathTextBox.TextChanged += (_, _) => HideSaveStatus();
        ImagePathTextBox.TextChanged += (_, _) => HideSaveStatus();
        DurationTextBox.TextChanged += (_, _) => HideSaveStatus();
        ArtistTextBox.TextChanged += (_, _) => HideSaveStatus();
        AlbumTextBox.TextChanged += (_, _) => HideSaveStatus();
        IsPlayingCheckBox.Checked += (_, _) => HideSaveStatus();
        IsPlayingCheckBox.Unchecked += (_, _) => HideSaveStatus();
    }

    private void ShowSaveStatus(string message)
    {
        SaveStatusText.Text = message;
        SaveStatusText.Visibility = Visibility.Visible;
    }

    private void HideSaveStatus()
    {
        SaveStatusText.Visibility = Visibility.Collapsed;
    }

    private void UpdateDurationInputState()
    {
        var isImage = IsImagePath(FilePathTextBox.Text);
        DurationTextBox.IsEnabled = !isImage;
        if (isImage)
            DurationTextBox.Text = "00:00:00";
    }

    private static bool IsImagePath(string path)
    {
        if (!TryResolveExtension(path.Trim(), out var extension))
            return false;

        return MediaExtensions.ImageExtensions.Contains(extension);
    }

    private static bool TryGetActualDuration(string filePath, out TimeSpan duration, out string errorMessage)
    {
        duration = TimeSpan.Zero;
        errorMessage = string.Empty;
        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            duration = tagFile.Properties.Duration;
            if (duration > TimeSpan.Zero)
                return true;

            errorMessage = "Unable to read media duration from the selected file.";
            return false;
        }
        catch (Exception)
        {
            errorMessage = "Unable to read media duration from the selected file.";
            return false;
        }
    }

    private bool HasUnsavedChanges()
    {
        if (_mediaFile is null)
            return false;

        var title = TitleTextBox.Text.Trim();
        var filePath = FilePathTextBox.Text.Trim();
        var imagePath = ImagePathTextBox.Text.Trim();
        var durationText = DurationTextBox.Text.Trim();
        var artistText = ArtistTextBox.Text.Trim();
        var albumText = AlbumTextBox.Text.Trim();
        var isPlaying = IsPlayingCheckBox.IsChecked == true;

        var currentTitle = _mediaFile.Title ?? string.Empty;
        var currentFilePath = _mediaFile.FilePath ?? string.Empty;
        var currentImagePath = _mediaFile.ImagePath ?? string.Empty;
        var currentDuration = _mediaFile.Duration.ToString(@"hh\:mm\:ss");
        var currentArtist = _mediaFile.Artist ?? string.Empty;
        var currentAlbum = _mediaFile.Album ?? string.Empty;

        return !string.Equals(title, currentTitle, StringComparison.Ordinal) ||
               !string.Equals(filePath, currentFilePath, StringComparison.Ordinal) ||
               !string.Equals(imagePath, currentImagePath, StringComparison.Ordinal) ||
               !string.Equals(durationText, currentDuration, StringComparison.Ordinal) ||
               !string.Equals(artistText, currentArtist, StringComparison.Ordinal) ||
               !string.Equals(albumText, currentAlbum, StringComparison.Ordinal) ||
               isPlaying != _mediaFile.IsPlaying;
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
