using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Win32;

using Deimos.UI.Models;


namespace Deimos.UI.Windowing;

public partial class AddMediaWindow
{
    private const string DefaultCoverPath = "pack://application:,,,/Assets/Default_cover/Default.png";
    private readonly ObservableCollection<MediaFile> _playList;
    public MediaFile? AddedMedia { get; private set; }
    public bool StartPlaybackAfterSave { get; private set; }

    /// <summary>
    /// Initializes the add-media dialog with the shared playlist reference.
    /// </summary>
    public AddMediaWindow(ObservableCollection<MediaFile> playList)
    {
        _playList = playList;   // Reuse the live playlist collection
        InitializeComponent();
        UpdateDurationInputState();
        Debug.WriteLine("AddMediaWindow initialized.");
    }

    /// <summary>
    /// Opens the file picker and fills the file path field.
    /// </summary>
    private void BrowseFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = BuildMediaFilter(),    // Limit selections to supported media types
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)    // Only proceed when the dialog completes successfully
        {
            FilePathTextBox.Text = dialog.FileName; // Populate the chosen file path
            if (TryResolveExtension(dialog.FileName, out var extension) &&
                MediaExtensions.ImageExtensions.Contains(extension))
                ImagePathTextBox.Text = dialog.FileName;
        }
    }

    private void FilePathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateDurationInputState();
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

    /// <summary>
    /// Builds the OpenFileDialog filter from supported media extensions.
    /// </summary>
    private static string BuildMediaFilter()
    {
        var audio = string.Join(";", MediaExtensions.AudioExtensions.Select(ext => $"*{ext}"));
        var video = string.Join(";", MediaExtensions.VideoExtensions.Select(ext => $"*{ext}"));
        var images = string.Join(";", MediaExtensions.ImageExtensions.Select(ext => $"*{ext}"));
        var all = string.Join(";", audio, video, images);   // Combined filter entry

        return $"All supported media|{all}|Audio files|{audio}|Video files|{video}|Image files|{images}|All files|*.*";
    }

    private static string BuildImageFilter()
    {
        var images = string.Join(";", MediaExtensions.ImageExtensions.Select(ext => $"*{ext}"));
        return $"Image files|{images}|All files|*.*";
    }

    /// <summary>
    /// Enables drag-to-move for the custom title bar.
    /// </summary>
    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    /// <summary>
    /// Closes the add-media dialog without saving.
    /// </summary>
    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Validates the input, adds the media item, and closes the dialog.
    /// </summary>
    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryBuildMediaFile(out var mediaFile, out var errorMessage))
        {
            Debug.WriteLine($"AddMediaWindow validation failed: {errorMessage}");
            MessageBox.Show(this, errorMessage, "Invalid input", MessageBoxButton.OK, 
                MessageBoxImage.Warning);
            return;
        }

        _playList.Add(mediaFile);   // Add the new item directly to the live playlist
        AddedMedia = mediaFile;
        StartPlaybackAfterSave = IsPlayingCheckBox.IsChecked == true;
        Debug.WriteLine($"AddMediaWindow added: {mediaFile.Title}");
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Builds a MediaFile from the form, returning false with an error message on failure.
    /// </summary>
    private bool TryBuildMediaFile(out MediaFile mediaFile, out string errorMessage)
    {
        var title = TitleTextBox.Text.Trim();
        var filePath = FilePathTextBox.Text.Trim();
        var imagePath = ImagePathTextBox.Text.Trim();
        var durationText = DurationTextBox.Text.Trim();
        var artist = ArtistTextBox.Text.Trim();
        var album = AlbumTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            errorMessage = "Title is required.";
            mediaFile = null!;
            return false;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "File path is required.";
            mediaFile = null!;
            return false;
        }

        if (!TryResolveExtension(filePath, out var extension))
        {
            errorMessage = "File path is not a valid URI or file path.";
            mediaFile = null!;
            return false;
        }

        if (!IsSupportedMediaExtension(extension))
        {
            errorMessage = "Unsupported media format.";
            mediaFile = null!;
            return false;
        }

        if (Uri.TryCreate(filePath, UriKind.RelativeOrAbsolute, out var fileUri) &&
            fileUri.IsAbsoluteUri && fileUri.IsFile)
        {
            if (!File.Exists(fileUri.LocalPath))
            {
                errorMessage = "The selected file does not exist.";
                mediaFile = null!;
                return false;
            }
        }
        else if (!Path.IsPathRooted(filePath))
        {
            errorMessage = "File path must be a valid absolute path.";
            mediaFile = null!;
            return false;
        }

        var duration = TimeSpan.Zero;   // Images have no duration
        if (!MediaExtensions.ImageExtensions.Contains(extension))
        {
            if (!TryGetActualDuration(filePath, out var actualDuration, out var durationError))
            {
                errorMessage = durationError;
                mediaFile = null!;
                return false;
            }

            if (string.IsNullOrWhiteSpace(durationText))
            {
                duration = actualDuration;
            }
            else if (!TimeSpan.TryParse(durationText, out duration))
            {
                errorMessage = "Duration must be in hh:mm:ss format.";
                mediaFile = null!;
                return false;
            }

            if (duration <= TimeSpan.Zero)
            {
                errorMessage = "Duration must be greater than 00:00:00.";
                mediaFile = null!;
                return false;
            }

            if (duration > actualDuration)
            {
                errorMessage = "Duration cannot be longer than the media file duration.";
                mediaFile = null!;
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            imagePath = MediaExtensions.ImageExtensions.Contains(extension)
                ? filePath
                : DefaultCoverPath; // Use the default cover when no image is supplied
        }
        else if (!TryResolveExtension(imagePath, out var imageExtension) ||
                 !MediaExtensions.ImageExtensions.Contains(imageExtension))
        {
            errorMessage = "Image path must point to a supported image file.";
            mediaFile = null!;
            return false;
        }
        else if (!ImagePathExists(imagePath))
        {
            errorMessage = "The selected image does not exist.";
            mediaFile = null!;
            return false;
        }

        mediaFile = new MediaFile
        {
            Title = title,
            FilePath = filePath,
            ImagePath = imagePath,
            Duration = duration,
            Artist = string.IsNullOrWhiteSpace(artist) ? null : artist,
            Album = string.IsNullOrWhiteSpace(album) ? null : album,
            IsPlaying = false
        };

        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Resolves a file extension from a path or URI.
    /// </summary>
    private static bool TryResolveExtension(string path, out string extension)
    {
        extension = string.Empty;
        if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
        {
            var uriPath = uri.IsAbsoluteUri ? uri.IsFile ? uri.LocalPath : uri.AbsolutePath : uri.OriginalString;
            extension = Path.GetExtension(uriPath).ToLowerInvariant();
            return !string.IsNullOrWhiteSpace(extension);
        }

        extension = Path.GetExtension(path).ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(extension);
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

    private static bool ImagePathExists(string imagePath)
    {
        if (imagePath.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
            return true;

        if (Uri.TryCreate(imagePath, UriKind.RelativeOrAbsolute, out var imageUri) &&
            imageUri is { IsAbsoluteUri: true, IsFile: true })
        {
            return File.Exists(imageUri.LocalPath);
        }

        return Path.IsPathRooted(imagePath) && File.Exists(imagePath);
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
}
