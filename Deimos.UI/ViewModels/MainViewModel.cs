using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;

using Deimos.UI.Models;
using Deimos.UI.Services;


namespace Deimos.UI.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const string StaticImageUri = "pack://application:,,,/Assets/Default_cover/Default.png";    // Default image resource
    private const double ShuffleImageDurationSeconds = 5; // Shuffle image display length
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif"]; // Image formats for shuffle preview
    private readonly MediaPlayback _mediaPlayback;  // Playback service instance
    private readonly Random _random = new();    // Shuffle source
    private readonly DispatcherTimer _shuffleImageTimer = new() { Interval = TimeSpan.FromSeconds(ShuffleImageDurationSeconds) }; // Auto-advance timer for images
    private readonly List<int> _shuffleOrder = new(); // Stores the shuffled playlist order
    private int _shufflePosition = -1; // Current index within the shuffle order
    private MediaFile? _selectedMedia;  // Currently selected playlist item
    private string _nowPlayingText = "Now playing:";    // Text shown in the UI label
    private bool _isPlaying; // Tracks current playback state
    private bool _isShuffleEnabled; // Shuffle toggle state
    private bool _isRepeatEnabled;  // Repeat toggle state
    
    /// <summary>
    /// Initializes the view model, playlist, and playback wiring.
    /// </summary>
    public MainViewModel(MediaElement player, Image imageViewer)
    {
        Debug.WriteLine("MainViewModel initialized.");
        PlayList = new ObservableCollection<MediaFile>();
        // Playback service handles media scanning and playback logic
        _mediaPlayback = new MediaPlayback(PlayList, player, imageViewer, UpdateNowPlayingText);
        // Command routes the UI action to playback logic
        PlaySelectedCommand = new RelayCommand(_ => PlaySelectedFromUi(), 
            _ => SelectedMedia is not null);
        PlayPauseCommand = new RelayCommand(_ => TogglePlayPause(), _ => SelectedMedia is not null);
        StopCommand = new RelayCommand(_ => StopPlayback(), _ => SelectedMedia is not null);
        NextCommand = new RelayCommand(_ => PlayNext(), _ => PlayList.Count > 0);
        PreviousCommand = new RelayCommand(_ => PlayPrevious(), _ => PlayList.Count > 0);
        ShuffleCommand = new RelayCommand(_ => ToggleShuffle());
        RepeatCommand = new RelayCommand(_ => ToggleRepeat());
        _ = PlayPauseCommand;
        _ = StopCommand;
        _ = NextCommand;
        _ = PreviousCommand;
        _ = ShuffleCommand;
        _ = RepeatCommand;
        AddStaticCommand = new RelayCommand(_ => AddStaticItem());  // Adds a predefined item
        _ = AddStaticCommand;   // Touch getter for analyzers that don't see XAML bindings
        RemoveSelectedCommand = new RelayCommand(_ => RemoveSelectedItem(), _ => SelectedMedia is not null);  // Remove a selected item
        _ = RemoveSelectedCommand;
        EditStaticCommand = new RelayCommand(_ => EditStaticItem(), _ => SelectedMedia is not null);  // Edit a selected item
        _ = EditStaticCommand;
        PlayList.CollectionChanged += (_, _) =>
        {
            NextCommand.RaiseCanExecuteChanged();
            PreviousCommand.RaiseCanExecuteChanged();
            if (IsShuffleEnabled)
                BuildShuffleOrder(CurrentIndex);
        };
        _shuffleImageTimer.Tick += ShuffleImageTimer_OnTick;
        _mediaPlayback.LoadDefaultMediaFiles();
    }
    
    public ObservableCollection<MediaFile> PlayList { get; }    // Items shown in the playlist
    public RelayCommand PlaySelectedCommand { get; }    // Command used by the UI to start playback
    public RelayCommand PlayPauseCommand { get; }    // Command used by the UI to play or pause
    public RelayCommand StopCommand { get; }    // Command used by the UI to stop playback
    public RelayCommand NextCommand { get; }    // Command used by the UI to play the next item
    public RelayCommand PreviousCommand { get; }    // Command used by the UI to play the previous item
    public RelayCommand ShuffleCommand { get; }    // Command used by the UI to toggle shuffle
    public RelayCommand RepeatCommand { get; }    // Command used by the UI to toggle repeat
    public RelayCommand AddStaticCommand { get; }    // Command used to add a static item
    public RelayCommand RemoveSelectedCommand { get; }    // Command used to remove a selected item
    public RelayCommand EditStaticCommand { get; }    // Command used to edit a selected item

    /// <summary>
    /// Gets or sets the currently selected media item.
    /// </summary>
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
                OnPropertyChanged(nameof(IsShuffleImageActive));
                // Refresh command availability when selection changes
                PlaySelectedCommand.RaiseCanExecuteChanged();
                PlayPauseCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                NextCommand.RaiseCanExecuteChanged();
                PreviousCommand.RaiseCanExecuteChanged();
                RemoveSelectedCommand.RaiseCanExecuteChanged();
                EditStaticCommand.RaiseCanExecuteChanged();
                if (IsShuffleEnabled)
                    SyncShufflePosition(CurrentIndex);
                ScheduleImageAdvanceIfNeeded();
            }
        }
    }

    /// <summary>
    /// Gets the text shown in the now playing label.
    /// </summary>
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
    /// Gets whether shuffle mode is enabled.
    /// </summary>
    public bool IsShuffleEnabled
    {
        get => _isShuffleEnabled;
        private set
        {
            if (_isShuffleEnabled != value)
            {
                _isShuffleEnabled = value;
                OnPropertyChanged(nameof(IsShuffleEnabled));
                OnPropertyChanged(nameof(IsShuffleImageActive));
            }
        }
    }

    /// <summary>
    /// Gets whether repeat mode is enabled.
    /// </summary>
    public bool IsRepeatEnabled
    {
        get => _isRepeatEnabled;
        private set
        {
            if (_isRepeatEnabled != value)
            {
                _isRepeatEnabled = value;
                OnPropertyChanged(nameof(IsRepeatEnabled));
            }
        }
    }

    public bool IsShuffleImageActive => IsShuffleEnabled && SelectedMedia is not null && IsImageMedia(SelectedMedia);

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(PlayPauseIconPath));
            }
        }
    }

    public string PlayPauseIconPath => IsPlaying ? "Assets/Icons/Controls/pause.svg" : "Assets/Icons/Controls/play.svg";
    
    /// <summary>
    /// Receives playback updates and pushes them into the bound text property.
    /// </summary>
    private void UpdateNowPlayingText(string text)
    {
        NowPlayingText = text;
    }

    private void PlaySelectedFromUi()
    {
        _mediaPlayback.PlaySelected(SelectedMedia);
        SyncPlaybackState();
        ScheduleImageAdvanceIfNeeded();
    }

    /// <summary>
    /// Toggles playback for the current selection.
    /// </summary>
    private void TogglePlayPause()
    {
        if (SelectedMedia is null)
        {
            Debug.WriteLine("TogglePlayPause skipped: no selection");
            return;
        }

        if (IsPlaying)
        {
            _mediaPlayback.Pause();
            SyncPlaybackState();
            return;
        }

        _mediaPlayback.PlayOrResume(SelectedMedia);
        SyncPlaybackState();
        ScheduleImageAdvanceIfNeeded();
    }

    /// <summary>
    /// Stops the current playback.
    /// </summary>
    private void StopPlayback()
    {
        _mediaPlayback.Stop();
        SyncPlaybackState();
        StopImageAdvanceTimer();
    }

    /// <summary>
    /// Plays the next item based on shuffle and repeat rules.
    /// </summary>
    private void PlayNext()
    {
        if (PlayList.Count == 0)
        {
            Debug.WriteLine("PlayNext skipped: playlist is empty");
            return;
        }

        if (IsShuffleEnabled)
        {
            PlayShuffleNext();
            return;
        }

        var currentIndex = SelectedMedia is null ? -1 : PlayList.IndexOf(SelectedMedia);
        var nextIndex = currentIndex + 1;
        if (nextIndex >= PlayList.Count)
        {
            if (!IsRepeatEnabled)
            {
                Debug.WriteLine("PlayNext skipped: end of playlist and repeat disabled");
                return;
            }

            nextIndex = 0;
        }

        SelectedMedia = PlayList[nextIndex];
        StartPlaybackForSelection();
    }

    /// <summary>
    /// Plays the previous item based on shuffle and repeat rules.
    /// </summary>
    private void PlayPrevious()
    {
        if (PlayList.Count == 0)
        {
            Debug.WriteLine("PlayPrevious skipped: playlist is empty");
            return;
        }

        if (IsShuffleEnabled)
        {
            PlayShufflePrevious();
            return;
        }

        var currentIndex = SelectedMedia is null ? PlayList.Count : PlayList.IndexOf(SelectedMedia);
        var previousIndex = currentIndex - 1;
        if (previousIndex < 0)
        {
            if (!IsRepeatEnabled)
            {
                Debug.WriteLine("PlayPrevious skipped: start of playlist and repeat disabled");
                return;
            }

            previousIndex = PlayList.Count - 1;
        }

        SelectedMedia = PlayList[previousIndex];

        StartPlaybackForSelection();
    }

    /// <summary>
    /// Toggles shuffle mode.
    /// </summary>
    private void ToggleShuffle()
    {
        IsShuffleEnabled = !IsShuffleEnabled;
        Debug.WriteLine($"Shuffle toggled: {IsShuffleEnabled}");
        if (IsShuffleEnabled)
        {
            BuildShuffleOrder(CurrentIndex);
        }
        else
        {
            _shuffleOrder.Clear();
            _shufflePosition = -1;
        }
        ScheduleImageAdvanceIfNeeded();
    }

    /// <summary>
    /// Toggles repeat mode.
    /// </summary>
    private void ToggleRepeat()
    {
        IsRepeatEnabled = !IsRepeatEnabled;
        Debug.WriteLine($"Repeat toggled: {IsRepeatEnabled}");
    }

    /// <summary>
    /// Handles media ending to honor repeat and shuffle modes.
    /// </summary>
    public void HandleMediaEnded()
    {
        if (SelectedMedia is null)
        {
            Debug.WriteLine("HandleMediaEnded skipped: no selection");
            return;
        }

        if (IsRepeatEnabled)
        {
            Debug.WriteLine("HandleMediaEnded: repeating current track");
            StartPlaybackForSelection();
            return;
        }

        if (IsShuffleEnabled)
        {
            Debug.WriteLine("HandleMediaEnded: shuffle enabled");
            PlayShuffleNext();
            return;
        }

        Debug.WriteLine("HandleMediaEnded: advancing to next track");
        PlayNext();
    }

    public void NotifyPlaybackEnded()
    {
        _mediaPlayback.MarkPlaybackEnded();
        SyncPlaybackState();
    }

    private int CurrentIndex => SelectedMedia is null ? -1 : PlayList.IndexOf(SelectedMedia);

    private void BuildShuffleOrder(int currentIndex)
    {
        _shuffleOrder.Clear();
        if (PlayList.Count == 0)
        {
            _shufflePosition = -1;
            return;
        }

        var indices = new List<int>(PlayList.Count);
        for (var i = 0; i < PlayList.Count; i++)
        {
            if (i != currentIndex)
                indices.Add(i);
        }

        for (var i = indices.Count - 1; i > 0; i--)
        {
            var swapIndex = _random.Next(i + 1);
            (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);
        }

        if (currentIndex >= 0)
            _shuffleOrder.Add(currentIndex);

        _shuffleOrder.AddRange(indices);
        _shufflePosition = currentIndex >= 0 && _shuffleOrder.Count > 0 ? 0 : -1;
    }

    private void SyncShufflePosition(int currentIndex)
    {
        if (_shuffleOrder.Count == 0)
        {
            BuildShuffleOrder(currentIndex);
            return;
        }

        var position = _shuffleOrder.IndexOf(currentIndex);
        if (position < 0)
        {
            BuildShuffleOrder(currentIndex);
            return;
        }

        _shufflePosition = position;
    }

    private void PlayShuffleNext()
    {
        if (_shuffleOrder.Count == 0)
            BuildShuffleOrder(CurrentIndex);

        if (_shuffleOrder.Count == 0)
            return;

        var nextPosition = _shufflePosition < 0 ? 0 : _shufflePosition + 1;
        if (nextPosition >= _shuffleOrder.Count)
        {
            if (!IsRepeatEnabled)
            {
                Debug.WriteLine("PlayShuffleNext skipped: end of shuffle order and repeat disabled");
                return;
            }

            BuildShuffleOrder(CurrentIndex);
            nextPosition = 0;
        }

        _shufflePosition = nextPosition;
        var nextIndex = _shuffleOrder[_shufflePosition];
        SelectedMedia = PlayList[nextIndex];
        StartPlaybackForSelection();
    }

    private void PlayShufflePrevious()
    {
        if (_shuffleOrder.Count == 0)
            BuildShuffleOrder(CurrentIndex);

        if (_shuffleOrder.Count == 0)
            return;

        var previousPosition = _shufflePosition < 0 ? _shuffleOrder.Count - 1 : _shufflePosition - 1;
        if (previousPosition < 0)
        {
            if (!IsRepeatEnabled)
            {
                Debug.WriteLine("PlayShufflePrevious skipped: start of shuffle order and repeat disabled");
                return;
            }

            previousPosition = _shuffleOrder.Count - 1;
        }

        _shufflePosition = previousPosition;
        var previousIndex = _shuffleOrder[_shufflePosition];
        SelectedMedia = PlayList[previousIndex];
        StartPlaybackForSelection();
    }

    private void StartPlaybackForSelection()
    {
        _mediaPlayback.PlaySelected(SelectedMedia);
        SyncPlaybackState();
        ScheduleImageAdvanceIfNeeded();
    }

    private void ScheduleImageAdvanceIfNeeded()
    {
        if (!IsShuffleEnabled || SelectedMedia is null || !IsImageMedia(SelectedMedia))
        {
            StopImageAdvanceTimer();
            return;
        }

        _shuffleImageTimer.Stop();
        _shuffleImageTimer.Interval = TimeSpan.FromSeconds(ShuffleImageDurationSeconds);
        _shuffleImageTimer.Start();
        Debug.WriteLine("Shuffle image timer started.");
    }

    private void StopImageAdvanceTimer()
    {
        if (_shuffleImageTimer.IsEnabled)
        {
            _shuffleImageTimer.Stop();
            Debug.WriteLine("Shuffle image timer stopped.");
        }
    }

    private void ShuffleImageTimer_OnTick(object? sender, EventArgs e)
    {
        _shuffleImageTimer.Stop();
        if (!IsShuffleEnabled || SelectedMedia is null || !IsImageMedia(SelectedMedia))
            return;

        Debug.WriteLine("Shuffle image timeout reached, advancing.");
        PlayShuffleNext();
    }

    public void RescheduleShuffleImageAdvance(double elapsedSeconds)
    {
        if (!IsShuffleImageActive)
            return;

        var remainingSeconds = Math.Max(0, ShuffleImageDurationSeconds - elapsedSeconds);
        _shuffleImageTimer.Stop();
        _shuffleImageTimer.Interval = TimeSpan.FromSeconds(Math.Max(remainingSeconds, 0.01));
        _shuffleImageTimer.Start();
        Debug.WriteLine($"Shuffle image timer rescheduled: {remainingSeconds:0.00}s remaining.");
    }

    private static bool IsImageMedia(MediaFile mediaFile)
    {
        var extension = GetExtension(mediaFile.FilePath ?? mediaFile.ImagePath);
        return ImageExtensions.Contains(extension);
    }

    private static string GetExtension(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
        {
            var uriPath = uri.IsFile ? uri.LocalPath : uri.AbsolutePath;
            return Path.GetExtension(uriPath).ToLowerInvariant();
        }

        return Path.GetExtension(path).ToLowerInvariant();
    }

    /// <summary>
    /// Adds a predefined static media item to the playlist.
    /// </summary>
    private void AddStaticItem()
    {
        var staticItem = new MediaFile
        {
            Title = "Default image",
            FilePath = StaticImageUri,
            ImagePath = StaticImageUri,
            Duration = TimeSpan.Zero,
            Artist = "Image file",
            Album = "Images",
            IsPlaying = false
        };

        PlayList.Add(staticItem);
        Debug.WriteLine($"Default image added: {staticItem.Title}");
    }
    
    /// <summary>
    /// Removes the currently selected media item from the playlist.
    /// </summary>
    private void RemoveSelectedItem()
    {
        if (SelectedMedia is null)
        {
            Debug.WriteLine("RemoveSelectedItem skipped: no selection");
            return;
        }

        var removedTitle = SelectedMedia.Title ?? "(Untitled)";
        PlayList.Remove(SelectedMedia);
        Debug.WriteLine($"Removed selected item: {removedTitle}");
        SelectedMedia = null;
    }

    /// <summary>
    /// Updates the selected media item title to a predefined static value.
    /// </summary>
    private void EditStaticItem()
    {
        if (SelectedMedia is null)
        {
            Debug.WriteLine("EditStaticItem skipped: no selection");
            return;
        }

        SelectedMedia.Title = "Edited static title";
        Debug.WriteLine($"Static title applied: {SelectedMedia.Title}");
    }

    private void SyncPlaybackState()
    {
        IsPlaying = _mediaPlayback.IsPlaying;
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
