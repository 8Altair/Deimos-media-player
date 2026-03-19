using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Deimos.UI.Controls;


namespace Deimos.UI;

/// <summary>
/// Provides the UI glue between the WPF seek bar elements and the SeekBar logic class.
/// </summary>
public partial class MainWindow
{
    private readonly SeekBar _seekBar = new();  // Stores seek bar state and calculations

    /// <summary>
    /// Initializes seek bar behavior after the XAML elements are created.
    /// </summary>
    private void InitializeSeekBarLogic()
    {
        UpdateSeekBarVisual();  // Ensure the visual state matches the logic immediately
    }

    /// <summary>
    /// Starts a drag when the user presses the left mouse button on the seek bar.
    /// </summary>
    /// <param name="sender">The seek bar element that raised the event.</param>
    /// <param name="e">Mouse data used to compute the initial position.</param>
    private void SeekBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
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
        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);   // Apply the last mouse position
        SeekBar.ReleaseMouseCapture();  // Restore normal mouse routing to other controls
    }

    /// <summary>
    /// Recalculates the visuals when the seek bar size changes.
    /// </summary>
    /// <param name="sender">The seek bar element that raised the event.</param>
    /// <param name="e">Size change data used to trigger a redraw.</param>
    private void SeekBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
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
