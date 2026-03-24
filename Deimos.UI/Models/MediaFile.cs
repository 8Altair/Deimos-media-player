using System.ComponentModel;


namespace Deimos.UI.Models;

public class MediaFile: INotifyPropertyChanged
{
    private string? _title;
    private string? _filePath;
    private string? _imagePath;
    private TimeSpan _duration;
    private string? _artist;
    private string? _album;
    private bool _isPlaying;

    public string? Title
    {
        get => _title;
        set
        {
            if (value != _title)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
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
                OnPropertyChanged(nameof(FilePath));
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
                OnPropertyChanged(nameof(ImagePath));
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
                OnPropertyChanged(nameof(Duration));
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
                OnPropertyChanged(nameof(Artist));
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
                OnPropertyChanged(nameof(Album));
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
                OnPropertyChanged(nameof(IsPlaying));
            }
        }
    }
    
    public MediaFile()
    {
        
    }

    public event PropertyChangedEventHandler? PropertyChanged;  // Event required by the INotifyPropertyChanged

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));  // Notify the UI that property propertyName has changed on this object, if not null
    }

    public override string ToString()
    {
        return Title ?? "(Untitled)";
    }
}
