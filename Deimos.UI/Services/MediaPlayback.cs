using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using Deimos.UI.Models;


namespace Deimos.UI.Services;

public sealed class MediaPlayback
{
    private static readonly string[] AudioExtensions = [".mp3", ".flac", ".wav", ".wma", ".m4a"]; // Audio formats
    private static readonly string[] VideoExtensions = [".mp4", ".avi", ".wmv"]; // Video formats
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif"]; // Image formats

    private const string MediaFolder = @"C:\Users\dinoa\OneDrive\Radna površina\Fakulteto\Treća godina\Drugi semestar\Interakcija čovjek-računar\Vježbe\Media player\Media";
    private const string FallbackAudioImage = "pack://application:,,,/Assets/Default_cover/Default.png"; // Default audio art
    private const string FallbackVideoImage = "pack://application:,,,/Assets/Default_cover/Default.png"; // Default video art

    private readonly ObservableCollection<MediaFile> _playList; // Shared playlist collection
    private readonly MediaElement _player; // Playback surface
    private readonly Action<string> _updateNowPlaying; // Callback to update UI text
    private MediaFile? _currentlyPlaying; // Track that is marked as playing
    private bool _isPlaying; // Tracks playback state

    public bool IsPlaying => _isPlaying; // Exposes playback state for UI logic

    /// <summary>
    /// Initializes playback service with playlist data and target UI controls.
    /// </summary>
    public MediaPlayback(ObservableCollection<MediaFile> playList, MediaElement player, 
        Action<string> updateNowPlaying)
    {
        _playList = playList;
        _player = player;
        _updateNowPlaying = updateNowPlaying;
        Debug.WriteLine("MediaPlayback initialized");
    }

    /// <summary>
    /// Scans the default media folder and fills the playlist with metadata.
    /// </summary>
    public void LoadDefaultMediaFiles()
    {
        Debug.WriteLine("LoadDefaultMediaFiles started");
        if (!Directory.Exists(MediaFolder))
        {
            Debug.WriteLine($"Media folder not found: {MediaFolder}");
            return;
        }

        // Cache folder for extracted cover art
        var artworkCacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArtworkCache"); // Cache location
        Directory.CreateDirectory(artworkCacheFolder);
        Debug.WriteLine($"Loading default media from: {MediaFolder}");

        foreach (var filePath in Directory.GetFiles(MediaFolder))
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant(); // Normalized extension

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

                string imagePath; // Artwork path

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
        Debug.WriteLine($"PlaySelected requested: {selectedMedia?.FilePath ?? "(null)"}");
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

        if (!Uri.TryCreate(selectedMedia.FilePath, UriKind.RelativeOrAbsolute, out var sourceUri))
        {
            Debug.WriteLine($"Selected media path is not a valid URI: {selectedMedia.FilePath}");
            return;
        }

        if (!sourceUri.IsAbsoluteUri)
        {
            sourceUri = new Uri(Path.GetFullPath(selectedMedia.FilePath)); // Normalize to absolute path
        }

        if (sourceUri.IsFile && !File.Exists(sourceUri.LocalPath))
        {
            Debug.WriteLine($"Selected file does not exist: {sourceUri.LocalPath}");
            return;
        }

        Debug.WriteLine($"Resolved media source: {sourceUri}"); // Final source URI
        // Determine media type by file extension
        var extension = Path.GetExtension(sourceUri.IsFile ? sourceUri.LocalPath
            : selectedMedia.FilePath).ToLowerInvariant(); // Normalized extension

        // Images are shown in the right-side viewer
        if (ImageExtensions.Contains(extension))
        {
            _player.Visibility = Visibility.Collapsed; // Hide player surface

            try
            {
                Debug.WriteLine($"Showing image: {selectedMedia.FilePath}");
                _player.Stop(); // Stop any previous playback
                _player.Source = null; // Clear player source
                _isPlaying = false; // Image preview is not playback
                _updateNowPlaying($"Viewing: {selectedMedia.Title ?? 
                                               Path.GetFileNameWithoutExtension(selectedMedia.FilePath)}");
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
                _player.Visibility = Visibility.Visible;
            }
            else
            {
                _player.Visibility = Visibility.Visible;
            }

            _player.Stop(); // Reset playback before switching
            _player.Source = sourceUri; // Set media source
            _player.Play(); // Start playback
            _isPlaying = true; // Track active playback
            Debug.WriteLine("Playback started successfully");
            _updateNowPlaying($"Now playing: {selectedMedia.Title ?? 
                                               Path.GetFileNameWithoutExtension(selectedMedia.FilePath)}");
            MarkAsPlaying(selectedMedia);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start playback for {selectedMedia.FilePath}. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts playback for a selection or resumes the current source if already loaded.
    /// </summary>
    public void PlayOrResume(MediaFile? selectedMedia)
    {
        if (selectedMedia is null)
        {
            Debug.WriteLine("PlayOrResume skipped: no selection");
            return;
        }

        if (ReferenceEquals(_currentlyPlaying, selectedMedia) && _player.Source is not null)
        {
            Debug.WriteLine("Resuming current playback");
            _player.Play();
            _isPlaying = true;
            _updateNowPlaying($"Now playing: {selectedMedia.Title ?? 
                                           Path.GetFileNameWithoutExtension(selectedMedia.FilePath)}");
            return;
        }

        PlaySelected(selectedMedia);
    }

    /// <summary>
    /// Pauses the current playback if possible.
    /// </summary>
    public void Pause()
    {
        if (_player.Source is null)
        {
            Debug.WriteLine("Pause skipped: no active media source");
            return;
        }

        Debug.WriteLine("Pausing playback");
        _player.Pause();
        _isPlaying = false;
        if (_currentlyPlaying is not null)
        {
            _updateNowPlaying($"Paused: {_currentlyPlaying.Title ?? 
                                         Path.GetFileNameWithoutExtension(_currentlyPlaying.FilePath)}");
        }
    }

    /// <summary>
    /// Stops the current playback if possible.
    /// </summary>
    public void Stop()
    {
        if (_player.Source is null)
        {
            Debug.WriteLine("Stop skipped: no active media source");
            return;
        }

        Debug.WriteLine("Stopping playback");
        _player.Stop();
        _isPlaying = false;
        if (_currentlyPlaying is not null)
        {
            _updateNowPlaying($"Stopped: {_currentlyPlaying.Title ?? 
                                          Path.GetFileNameWithoutExtension(_currentlyPlaying.FilePath)}");
        }
    }

    /// <summary>
    /// Updates the playback volume on the media element.
    /// </summary>
    public void SetVolume(double volume)
    {
        var clamped = Math.Max(0, Math.Min(1, volume));
        _player.Volume = clamped;
        Debug.WriteLine($"Volume set to {clamped:0.00}");
    }

    /// <summary>
    /// Marks playback as ended without changing UI text.
    /// </summary>
    public void MarkPlaybackEnded()
    {
        if (_isPlaying)
        {
            _isPlaying = false;
            Debug.WriteLine("Playback ended.");
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
            var picture = tagFile.Tag.Pictures[0]; // First embedded image
            var imageData = picture.Data.Data; // Raw image bytes

            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath) + "_cover.jpg"; // Cache file name
            var outputPath = Path.Combine(cacheFolder, fileName); // Full cache path

            File.WriteAllBytes(outputPath, imageData); // Write bytes to disk
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
            Debug.WriteLine($"Now marked as playing: {selectedMedia.FilePath}");
        }
    }
}
