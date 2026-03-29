using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

using Microsoft.Win32;

using Deimos.UI.Models;


namespace Deimos.UI.Windowing;

public partial class AddMediaWindow
{
    private const string DefaultCoverPath = "pack://application:,,,/Assets/Default_cover/Default.png";
    private readonly ObservableCollection<MediaFile> _playList;

    public AddMediaWindow(ObservableCollection<MediaFile> playList)
    {
        _playList = playList;
        InitializeComponent();
    }

    private void BrowseFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = BuildMediaFilter(),
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
            FilePathTextBox.Text = dialog.FileName;
    }

    private static string BuildMediaFilter()
    {
        var audio = string.Join(";", MediaExtensions.AudioExtensions.Select(ext => $"*{ext}"));
        var video = string.Join(";", MediaExtensions.VideoExtensions.Select(ext => $"*{ext}"));
        var images = string.Join(";", MediaExtensions.ImageExtensions.Select(ext => $"*{ext}"));
        var all = string.Join(";", audio, video, images);

        return $"All supported media|{all}|Audio files|{audio}|Video files|{video}|Image files|{images}|All files|*.*";
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryBuildMediaFile(out var mediaFile, out var errorMessage))
        {
            MessageBox.Show(this, errorMessage, "Invalid input", MessageBoxButton.OK, 
                MessageBoxImage.Warning);
            return;
        }

        _playList.Add(mediaFile);
        DialogResult = true;
        Close();
    }

    private bool TryBuildMediaFile(out MediaFile mediaFile, out string errorMessage)
    {
        var title = TitleTextBox.Text.Trim();
        var filePath = FilePathTextBox.Text.Trim();
        var imagePath = ImagePathTextBox.Text.Trim();
        var durationText = DurationTextBox.Text.Trim();
        var artist = ArtistTextBox.Text.Trim();
        var album = AlbumTextBox.Text.Trim();
        var isPlaying = IsPlayingCheckBox.IsChecked == true;

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

        if (Uri.TryCreate(filePath, UriKind.RelativeOrAbsolute, out var fileUri) && fileUri.IsFile)
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

        var duration = TimeSpan.Zero;
        if (!MediaExtensions.ImageExtensions.Contains(extension))
        {
            if (!string.IsNullOrWhiteSpace(durationText) &&
                !TimeSpan.TryParse(durationText, out duration))
            {
                errorMessage = "Duration must be in hh:mm:ss format.";
                mediaFile = null!;
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
            IsPlaying = isPlaying
        };

        errorMessage = string.Empty;
        return true;
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

    private static bool IsSupportedMediaExtension(string extension)
    {
        return MediaExtensions.AudioExtensions.Contains(extension) ||
               MediaExtensions.VideoExtensions.Contains(extension) ||
               MediaExtensions.ImageExtensions.Contains(extension);
    }
}
