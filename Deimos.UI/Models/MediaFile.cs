using System.ComponentModel;
using System.Diagnostics;


namespace Deimos.UI.Models;

public class MediaFile: INotifyPropertyChanged
{
    private string? _title; // Track title text
    private string? _filePath;  // Absolute path to media file
    private string? _imagePath; // Cover art or image path
    private TimeSpan _duration; // Media duration
    private string? _artist;    // Artist metadata
    private string? _album; // Album metadata
    private bool _isPlaying;    // Playback state flag

    public string? Title
    {
        get => _title;
        set
        {
            if (value != _title)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));   // Notify UI that the title changed
            }
        }
    }
    
    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (value != _filePath)
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));    // Notify UI that the file path changed
            }
        }
    }
    
    public string? ImagePath
    {
        get => _imagePath;
        set
        {
            if (value != _imagePath)
            {
                _imagePath = value;
                OnPropertyChanged(nameof(ImagePath));   // Notify UI that the image path changed
            }
        }
    }
    
    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (value != _duration)
            {
                _duration = value;
                OnPropertyChanged(nameof(Duration));    // Notify UI that the duration changed
            }
        }
    }
    
    public string? Artist
    {
        get => _artist;
        set
        {
            if (_artist != value)
            {
                _artist = value;
                OnPropertyChanged(nameof(Artist));  // Notify UI that the artist changed
            }
        }
    }
    
    public string? Album
    {
        get => _album;
        set
        {
            if (value != _album)
            {
                _album = value;
                OnPropertyChanged(nameof(Album));   // Notify UI that the album changed
            }
        }
    }
    
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged(nameof(IsPlaying));   // Notify UI that the playback state changed
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;   // Event required by the INotifyPropertyChanged

    /// <summary>
    /// Raises a change notification for a single property.
    /// </summary>
    protected void OnPropertyChanged(string propertyName)
    {
        Debug.WriteLine($"Property changed: {propertyName}");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));  // Notify the UI that property propertyName has changed on this object, if not null
    }
}
