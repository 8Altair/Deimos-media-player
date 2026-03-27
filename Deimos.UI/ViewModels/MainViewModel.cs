using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using Deimos.UI.Models;
using Deimos.UI.Services;


namespace Deimos.UI.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private MediaFile? _selectedMedia; // Currently selected playlist item
    private string _nowPlayingText = "Now playing:"; // Text shown in the UI label
    
    /// <summary>
    /// Initializes the view model, playlist, and playback wiring.
    /// </summary>
    public MainViewModel(MediaElement player, Image imageViewer)
    {
        Debug.WriteLine("MainViewModel initialized.");
        PlayList = new ObservableCollection<MediaFile>();
        // Playback service handles media scanning and playback logic.
        var mediaPlayback = new MediaPlayback(PlayList, player, imageViewer, UpdateNowPlayingText);
        // Command routes the UI action to playback logic.
        PlaySelectedCommand = new RelayCommand(_ => mediaPlayback.PlaySelected(SelectedMedia), 
            _ => SelectedMedia is not null);
        mediaPlayback.LoadDefaultMediaFiles();
    }
    
    public ObservableCollection<MediaFile> PlayList { get; } // Items shown in the playlist
    public ICommand PlaySelectedCommand { get; } // Command used by the UI to start playback

    public MediaFile? SelectedMedia
    {
        get => _selectedMedia;
        set
        {
            if (!ReferenceEquals(_selectedMedia, value))
            {
                _selectedMedia = value;
                Debug.WriteLine($"Selected media changed: {_selectedMedia}");
                OnPropertyChanged(nameof(SelectedMedia));
                // Refresh command availability when selection changes.
                ((RelayCommand)PlaySelectedCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string NowPlayingText
    {
        get => _nowPlayingText;
        private set
        {
            if (_nowPlayingText != value)
            {
                _nowPlayingText = value;
                Debug.WriteLine($"Now playing text updated: {_nowPlayingText}");
                OnPropertyChanged(nameof(NowPlayingText));
            }
        }
    }
    
    /// <summary>
    /// Receives playback updates and pushes them into the bound text property.
    /// </summary>
    private void UpdateNowPlayingText(string text)
    {
        NowPlayingText = text;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises a change notification for a single property.
    /// </summary>
    private void OnPropertyChanged(string propertyName)
    {
        Debug.WriteLine($"Property changed: {propertyName}");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
