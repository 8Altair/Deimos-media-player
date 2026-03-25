using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Deimos.UI.Models;


namespace Deimos.UI.Services;

public sealed class MediaPlayback
{
    private static readonly string[] AudioExtensions = [".mp3", ".flac", ".wav", ".wma", ".m4a"];
    private static readonly string[] VideoExtensions = [".mp4", ".avi", ".wmv"];
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif"];

    private const string MediaFolder = @"C:\Users\dinoa\OneDrive\Radna površina\Fakulteto\Treća godina\Drugi semestar\Interakcija čovjek-računar\Vježbe\Media player\Media";
    private const string FallbackAudioImage = "pack://application:,,,/Assets/Default_cover/Default.png";
    private const string FallbackVideoImage = "pack://application:,,,/Assets/Default_cover/Default.png";

    private readonly ObservableCollection<MediaFile> _playList;
    private readonly MediaElement _player;
    private readonly Image _imageViewer;
    private readonly TextBlock _nowPlaying;
    private MediaFile? _currentlyPlaying;

    /// <summary>
    /// Initializes playback service with playlist data and target UI controls.
    /// </summary>
    public MediaPlayback(ObservableCollection<MediaFile> playList, MediaElement player, 
        Image imageViewer, TextBlock nowPlaying)
    {
        _playList = playList;
        _player = player;
        _imageViewer = imageViewer;
        _nowPlaying = nowPlaying;
    }

    /// <summary>
    /// Scans the default media folder and fills the playlist with metadata.
    /// </summary>
    public void LoadDefaultMediaFiles()
    {
        if (!Directory.Exists(MediaFolder))
        {
            Debug.WriteLine($"Media folder not found: {MediaFolder}");
            return;
        }

        // Cache folder for extracted cover art
        var artworkCacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArtworkCache");
        Directory.CreateDirectory(artworkCacheFolder);
        Debug.WriteLine($"Loading default media from: {MediaFolder}");

        foreach (var filePath in Directory.GetFiles(MediaFolder))
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Images are added as viewable items without playback
            if (ImageExtensions.Contains(extension))
            {
                Debug.WriteLine($"Detected image file: {filePath}");
                _playList.Add(new MediaFile
                {
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath,
                    ImagePath = filePath,
                    Duration = TimeSpan.Zero,
                    Artist = "Image file",
                    Album = "Images",
                    IsPlaying = false
                });

                continue;
            }

            // Skip files that are not audio or video
            if (!AudioExtensions.Contains(extension) && !VideoExtensions.Contains(extension))
            {
                Debug.WriteLine($"Skipping unsupported file: {filePath}");
                continue;
            }

            try
            {
                Debug.WriteLine($"Reading media tags: {filePath}");
                var tagFile = TagLib.File.Create(filePath);

                var title = !string.IsNullOrWhiteSpace(tagFile.Tag.Title) ? tagFile.Tag.Title
                    : Path.GetFileNameWithoutExtension(filePath);

                var artist = tagFile.Tag.FirstPerformer ?? "Unknown Artist";
                var album = tagFile.Tag.Album ?? "Unknown Album";
                var duration = tagFile.Properties.Duration;

                string imagePath;

                // Audio files can have embedded artwork
                if (AudioExtensions.Contains(extension))
                {
                    imagePath = ExtractEmbeddedArtwork(tagFile, artworkCacheFolder, filePath) ?? FallbackAudioImage;
                }
                else
                {
                    imagePath = FallbackVideoImage;
                }

                Debug.WriteLine($"Adding media to playlist: {title} ({extension})");
                _playList.Add(new MediaFile
                {
                    Title = title,
                    FilePath = filePath,
                    ImagePath = imagePath,
                    Duration = duration,
                    Artist = artist,
                    Album = album,
                    IsPlaying = false
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read media file: {filePath}. Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Displays an image or starts playback for the selected media file.
    /// </summary>
    public void PlaySelected(MediaFile? selectedMedia)
    {
        if (selectedMedia is null)
        {
            Debug.WriteLine("No playlist item selected for playback.");
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedMedia.FilePath))
        {
            Debug.WriteLine($"Selected item has no file path: {selectedMedia}");
            return;
        }

        if (!File.Exists(selectedMedia.FilePath))
        {
            Debug.WriteLine($"Selected file does not exist: {selectedMedia.FilePath}");
            return;
        }

        // Determine media type by file extension
        var extension = Path.GetExtension(selectedMedia.FilePath).ToLowerInvariant();

        // Images are shown in the right-side viewer
        if (ImageExtensions.Contains(extension))
        {
            _imageViewer.Visibility = Visibility.Visible;
            _player.Visibility = Visibility.Collapsed;

            try
            {
                Debug.WriteLine($"Showing image: {selectedMedia.FilePath}");
                _player.Stop();
                _player.Source = null;
                _imageViewer.Source = new BitmapImage(new Uri(selectedMedia.FilePath, UriKind.Absolute));
                _nowPlaying.Text = $"Viewing: {selectedMedia.Title ?? 
                                               Path.GetFileNameWithoutExtension(selectedMedia.FilePath)}";
                MarkAsPlaying(selectedMedia);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show image {selectedMedia.FilePath}. Error: {ex.Message}");
                return;
            }
        }

        if (!AudioExtensions.Contains(extension) && !VideoExtensions.Contains(extension))
        {
            Debug.WriteLine($"Unsupported media type selected: {selectedMedia.FilePath}");
            return;
        }

        try
        {
            Debug.WriteLine($"Starting playback: {selectedMedia.FilePath}");
            // Audio uses cover art; video uses the media element only
            if (AudioExtensions.Contains(extension))
            {
                _imageViewer.Visibility = Visibility.Visible;
                _player.Visibility = Visibility.Visible;
                _imageViewer.Source = LoadArtwork(selectedMedia);
            }
            else
            {
                _imageViewer.Visibility = Visibility.Collapsed;
                _player.Visibility = Visibility.Visible;
                _imageViewer.Source = null;
            }

            _player.Stop();
            _player.Source = new Uri(selectedMedia.FilePath, UriKind.Absolute);
            _player.Play();
            _nowPlaying.Text = $"Now playing: {selectedMedia.Title ?? 
                                               Path.GetFileNameWithoutExtension(selectedMedia.FilePath)}";
            MarkAsPlaying(selectedMedia);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start playback for {selectedMedia.FilePath}. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts embedded artwork from a tagged file and writes it to the cache.
    /// </summary>
    private static string? ExtractEmbeddedArtwork(TagLib.File tagFile, string cacheFolder, string sourceFilePath)
    {
        if (tagFile.Tag.Pictures == null || tagFile.Tag.Pictures.Length == 0)
        {
            Debug.WriteLine($"No embedded artwork found for: {sourceFilePath}");
            return null;
        }

        try
        {
            var picture = tagFile.Tag.Pictures[0];
            var imageData = picture.Data.Data;

            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath) + "_cover.jpg";
            var outputPath = Path.Combine(cacheFolder, fileName);

            File.WriteAllBytes(outputPath, imageData);
            Debug.WriteLine($"Extracted embedded artwork to: {outputPath}");

            return outputPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to extract embedded artwork for {sourceFilePath}. Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads artwork from disk into a BitmapImage for binding.
    /// </summary>
    private static BitmapImage? LoadArtwork(MediaFile selectedMedia)
    {
        if (string.IsNullOrWhiteSpace(selectedMedia.ImagePath))
        {
            Debug.WriteLine("No artwork path available for selected media.");
            return null;
        }

        try
        {
            Debug.WriteLine($"Loading artwork: {selectedMedia.ImagePath}");
            if (!Uri.TryCreate(selectedMedia.ImagePath, UriKind.RelativeOrAbsolute, out var uri))
            {
                Debug.WriteLine($"Artwork path is not a valid URI: {selectedMedia.ImagePath}");
                return null;
            }

            if (uri.IsFile && !File.Exists(uri.LocalPath))
            {
                Debug.WriteLine($"Artwork file not found: {uri.LocalPath}");
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();

            return bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load artwork {selectedMedia.ImagePath}. Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates the IsPlaying flag, keeping only the current item marked as playing.
    /// </summary>
    private void MarkAsPlaying(MediaFile selectedMedia)
    {
        if (!ReferenceEquals(_currentlyPlaying, selectedMedia))
        {
            // Clear the previous playing item, then set the new one
            if (_currentlyPlaying is not null)
            {
                _currentlyPlaying.IsPlaying = false;
            }

            selectedMedia.IsPlaying = true;
            _currentlyPlaying = selectedMedia;
        }
    }
}
