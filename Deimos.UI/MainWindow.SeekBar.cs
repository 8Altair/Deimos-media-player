using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Deimos.UI.Controls;
using Deimos.UI.ViewModels;


namespace Deimos.UI;

/// <summary>
/// Provides the UI glue between the WPF seek bar elements and the SeekBar logic class.
/// </summary>
public partial class MainWindow
{
    private readonly SeekBar _seekBar = new();  // Stores seek bar state and calculations
    private readonly DispatcherTimer _seekBarTimer = new()  // Periodically syncs the seek bar with playback
    {
        Interval = TimeSpan.FromMilliseconds(250)
    };
    
    private const double ImageShuffleDurationSeconds = 5; // Duration for shuffled image display
    private bool _isImageShuffleActive; // Tracks when shuffle image mode is active
    private DateTime _imageShuffleStart; // Start time for image shuffle progress

    /// <summary>
    /// Initializes seek bar behavior after the XAML elements are created.
    /// </summary>
    private void InitializeSeekBarLogic()
    {
        Debug.WriteLine("MainWindow: Initializing seek bar logic.");    // Trace seek bar setup entry
        Player.MediaOpened += Player_OnMediaOpened;
        Player.MediaEnded += Player_OnMediaEnded;
        _seekBarTimer.Tick += SeekBarTimer_OnTick;
        _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        UpdateTimeLabels(TimeSpan.Zero, TimeSpan.Zero);
        UpdateSeekBarMode();
        UpdateSeekBarVisual();  // Ensure the visual state matches the logic immediately
    }

    /// <summary>
    /// Starts a drag when the user presses the left mouse button on the seek bar.
    /// </summary>
    /// <param name="sender">The seek bar element that raised the event.</param>
    /// <param name="e">Mouse data used to compute the initial position.</param>
    private void SeekBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Debug.WriteLine("MainWindow: Seek bar drag started.");  // Trace drag start
        _seekBar.BeginDrag();   // Mark the drag state for subsequent move events
        SeekBar.CaptureMouse(); // Keep receiving mouse events even if the pointer leaves the control
        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);   // Update value from the initial click position
    }

    /// <summary>
    /// Updates the seek bar while the user drags the thumb.
    /// </summary>
    /// <param name="sender">The seek bar element that raised the event.</param>
    /// <param name="e">Mouse data used to compute the current position.</param>
    private void SeekBar_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_seekBar.IsDragging || e.LeftButton != MouseButtonState.Pressed)   // Ignore moves when not dragging
            return; // Avoid updates outside a valid drag operation.

        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);   // Recalculate value based on the current mouse position
    }

    /// <summary>
    /// Ends a drag when the user releases the left mouse button.
    /// </summary>
    /// <param name="sender">The seek bar element that raised the event.</param>
    /// <param name="e">Mouse data used to compute the final position.</param>
    private void SeekBar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_seekBar.IsDragging)   // Do nothing if a drag was not active
            return; // Prevents accidental updates on unrelated mouse up events

        _seekBar.EndDrag(); // Finalize the drag state
        Debug.WriteLine("MainWindow: Seek bar drag ended.");    // Trace drag end
        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);   // Apply the last mouse position
        UpdatePlayerFromSeekBar(); // Apply the seek position to the media element
        SeekBar.ReleaseMouseCapture();  // Restore normal mouse routing to other controls
    }

    /// <summary>
    /// Recalculates the visuals when the seek bar size changes.
    /// </summary>
    /// <param name="sender">The seek bar element that raised the event.</param>
    /// <param name="e">Size change data used to trigger a redraw.</param>
    private void SeekBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Debug.WriteLine("MainWindow: Seek bar size changed.");  // Trace layout changes.
        UpdateSeekBarVisual();  // Ensure the thumb and progress bar match the new size
    }

    /// <summary>
    /// Updates the logic state from a mouse position and refreshes the visuals.
    /// </summary>
    /// <param name="mouseX">Horizontal mouse coordinate within the seek bar.</param>
    private void UpdateSeekBarFromMouse(double mouseX)
    {
        if (!_seekBar.UpdateFromMouse(mouseX, SeekBar.ActualWidth)) // Skip updates when width is invalid
            return; // Avoid divide-by-zero and invalid math

        UpdateSeekBarVisual();  // Redraw the progress and thumb after a valid update
    }

    /// <summary>
    /// Updates the seek bar value from the media element position.
    /// </summary>
    private void UpdateSeekBarFromPlayer()
    {
        if (_isImageShuffleActive)
            return;

        if (_seekBar.IsDragging) // Avoid snapping while the user is dragging
            return;

        var naturalDuration = Player.NaturalDuration;
        if (Player.Source is null || !naturalDuration.HasTimeSpan)
        {
            _seekBar.SetValue(0);
            UpdateTimeLabels(TimeSpan.Zero, TimeSpan.Zero);
            UpdateSeekBarVisual();
            _seekBarTimer.Stop();
            return;
        }

        var effectiveDuration = GetEffectivePlaybackDuration(naturalDuration.TimeSpan);
        _seekBar.SetRange(0, Math.Max(1, effectiveDuration.TotalSeconds));
        if (Player.Position >= effectiveDuration)
        {
            Player.Position = effectiveDuration;
            _seekBar.SetValue(effectiveDuration.TotalSeconds);
            UpdateTimeLabels(effectiveDuration, effectiveDuration);
            UpdateSeekBarVisual();
            _seekBarTimer.Stop();
            _viewModel.NotifyPlaybackEnded();
            _viewModel.HandleMediaEnded();
            return;
        }

        _seekBar.SetValue(Player.Position.TotalSeconds);
        UpdateTimeLabels(Player.Position, effectiveDuration);
        UpdateSeekBarVisual();
    }

    /// <summary>
    /// Applies the seek bar value to the media element.
    /// </summary>
    private void UpdatePlayerFromSeekBar()
    {
        if (_isImageShuffleActive)
        {
            var imageSeconds = _seekBar.GetValue();
            _imageShuffleStart = DateTime.UtcNow - TimeSpan.FromSeconds(imageSeconds);
            _viewModel.RescheduleShuffleImageAdvance(imageSeconds);
            UpdateTimeLabels(TimeSpan.FromSeconds(imageSeconds), TimeSpan.FromSeconds(ImageShuffleDurationSeconds));
            return;
        }

        var naturalDuration = Player.NaturalDuration;
        if (Player.Source is null || !naturalDuration.HasTimeSpan)
            return;

        var effectiveDuration = GetEffectivePlaybackDuration(naturalDuration.TimeSpan);
        var mediaSeconds = Math.Min(_seekBar.GetValue(), effectiveDuration.TotalSeconds);
        Player.Position = TimeSpan.FromSeconds(mediaSeconds);
        Debug.WriteLine($"MainWindow: Player position updated to {Player.Position}.");
        UpdateTimeLabels(Player.Position, effectiveDuration);
    }

    private void Player_OnMediaOpened(object? sender, RoutedEventArgs e)
    {
        if (!Player.NaturalDuration.HasTimeSpan)
        {
            _seekBar.SetRange(0, 1);
            _seekBar.SetValue(0);
            UpdateTimeLabels(TimeSpan.Zero, TimeSpan.Zero);
            UpdateSeekBarVisual();
            _seekBarTimer.Stop();
            UpdateSeekBarMode();
            return;
        }

        var effectiveDuration = GetEffectivePlaybackDuration(Player.NaturalDuration.TimeSpan);
        var durationSeconds = Math.Max(1, effectiveDuration.TotalSeconds);
        _seekBar.SetRange(0, durationSeconds);
        _seekBar.SetValue(0);
        UpdateTimeLabels(TimeSpan.Zero, effectiveDuration);
        UpdateSeekBarVisual();
        _seekBarTimer.Start();
        UpdateSeekBarMode();
        Debug.WriteLine($"MainWindow: Seek bar range set to {durationSeconds} seconds.");
    }

    private void Player_OnMediaEnded(object? sender, RoutedEventArgs e)
    {
        _seekBar.SetValue(0);
        UpdateTimeLabels(TimeSpan.Zero, Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero);
        UpdateSeekBarVisual();
        UpdateSeekBarMode();
        _viewModel.NotifyPlaybackEnded();
        _viewModel.HandleMediaEnded();
        Debug.WriteLine("MainWindow: Media ended, seek bar reset.");
    }

    private void SeekBarTimer_OnTick(object? sender, EventArgs e)
    {
        if (_isImageShuffleActive)
        {
            UpdateSeekBarForImageShuffle();
            return;
        }

        UpdateSeekBarFromPlayer();
    }

    private void ViewModel_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SelectedMedia) or nameof(MainViewModel.IsShuffleEnabled)
            or nameof(MainViewModel.IsShuffleImageActive))
        {
            UpdateSeekBarMode();
        }
    }

    private void UpdateSeekBarMode()
    {
        var shuffleImageActive = _viewModel.IsShuffleImageActive;
        if (shuffleImageActive)
        {
            _isImageShuffleActive = true;
            _imageShuffleStart = DateTime.UtcNow;
            _seekBar.SetRange(0, ImageShuffleDurationSeconds);
            _seekBar.SetValue(0);
            UpdateTimeLabels(TimeSpan.Zero, TimeSpan.FromSeconds(ImageShuffleDurationSeconds));
            UpdateSeekBarVisual();
            _seekBarTimer.Start();
        }
        else
        {
            _isImageShuffleActive = false;
            if (Player.Source is null || !Player.NaturalDuration.HasTimeSpan)
            {
                _seekBar.SetRange(0, 1);
                _seekBar.SetValue(0);
                UpdateTimeLabels(TimeSpan.Zero, TimeSpan.Zero);
                UpdateSeekBarVisual();
            }
        }

        SeekBar.IsHitTestVisible = _isImageShuffleActive || (Player.Source is not null && Player.NaturalDuration.HasTimeSpan);
    }

    private void UpdateSeekBarForImageShuffle()
    {
        var elapsedSeconds = (DateTime.UtcNow - _imageShuffleStart).TotalSeconds;
        elapsedSeconds = Math.Max(0, Math.Min(ImageShuffleDurationSeconds, elapsedSeconds));
        _seekBar.SetValue(elapsedSeconds);
        UpdateTimeLabels(TimeSpan.FromSeconds(elapsedSeconds), TimeSpan.FromSeconds(ImageShuffleDurationSeconds));
        UpdateSeekBarVisual();
    }

    private void UpdateTimeLabels(TimeSpan current, TimeSpan total)
    {
        CurrentTimeLabel.Content = current.ToString(@"hh\:mm\:ss");
        TotalTimeLabel.Content = total.ToString(@"hh\:mm\:ss");
    }

    private TimeSpan GetEffectivePlaybackDuration(TimeSpan naturalDuration)
    {
        var configuredDuration = _viewModel.CurrentPlayingMedia?.Duration ?? TimeSpan.Zero;
        if (configuredDuration <= TimeSpan.Zero || configuredDuration > naturalDuration)
            return naturalDuration;

        return configuredDuration;
    }

    /// <summary>
    /// Renders the seek bar progress fill and thumb position.
    /// </summary>
    private void UpdateSeekBarVisual()
    {
        var width = SeekBar.ActualWidth;    // Read the current pixel width of the bar

        if (width <= 0) // Guard against layout not ready yet
            return; // Avoid invalid calculations when width is not available

        var ratio = _seekBar.GetRatio();    // Normalized value from 0 to 1
        var progressWidth = width * ratio;  // Convert ratio to pixels
        SeekBarProgress.Width = progressWidth;  // Stretch the progress fill to the computed width

        var thumbLeft = progressWidth - SeekBarThumb.Width / 2; // Center the thumb on the progress edge
        thumbLeft = Math.Max(0, Math.Min(width - SeekBarThumb.Width, thumbLeft));   // Clamp within control bounds

        Canvas.SetLeft(SeekBarThumb, thumbLeft);    // Position the thumb on the canvas
    }
}
