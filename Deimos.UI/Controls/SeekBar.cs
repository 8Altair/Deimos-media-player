using System.Diagnostics;

namespace Deimos.UI.Controls;

/// <summary>
/// Encapsulates non-visual seek bar state and calculations.
/// </summary>
/// <param name="minimum">Minimum logical seek value.</param>
/// <param name="maximum">Maximum logical seek value.</param>
public sealed class SeekBar(double minimum = 0, double maximum = 100)
{
    public bool IsDragging { get; private set; }    // Tracks whether a drag operation is active
    private double Minimum { get; set; } = minimum;  // Stores the configured minimum value
    private double Maximum { get; set; } = maximum;  // Stores the configured maximum value
    private double Value { get; set; } = minimum;  // Stores the current logical value

    /// <summary>
    /// Marks the beginning of a drag operation.
    /// </summary>
    public void BeginDrag()
    {
        IsDragging = true;  // Enable drag state so the UI can update on mouse move
        Debug.WriteLine("SeekBar: Drag started.");  // Trace drag start within logic
    }

    /// <summary>
    /// Marks the end of a drag operation.
    /// </summary>
    public void EndDrag()
    {
        IsDragging = false; // Disable drag state to prevent further updates
        Debug.WriteLine("SeekBar: Drag ended.");    // Trace drag end within logic
    }

    /// <summary>
    /// Updates the logical value based on the mouse position and control width.
    /// </summary>
    /// <param name="mouseX">Horizontal mouse coordinate within the seek bar.</param>
    /// <param name="width">Actual width of the seek bar in pixels.</param>
    /// <returns>True when the value is updated; false when input width is invalid.</returns>
    public bool UpdateFromMouse(double mouseX, double width)
    {
        if (width <= 0) // Guard against invalid width before dividing
        {
            Debug.WriteLine("SeekBar: Update skipped due to invalid width.");   // Trace invalid width input
            return false;   // Skip update to avoid divide-by-zero
        }

        var ratio = Clamp(mouseX / width);  // Normalize mouse position to a 0-1 range
        Value = Minimum + (Maximum - Minimum) * ratio;  // Convert ratio to a logical value
        return true;    // Signal that the update succeeded
    }

    /// <summary>
    /// Updates the configured minimum and maximum range.
    /// </summary>
    public void SetRange(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
        Value = Math.Max(Minimum, Math.Min(Maximum, Value));
        Debug.WriteLine($"SeekBar: Range set to {Minimum} - {Maximum}."); // Trace range updates
    }

    /// <summary>
    /// Updates the current logical value.
    /// </summary>
    public void SetValue(double value)
    {
        Value = Math.Max(Minimum, Math.Min(Maximum, value));
    }

    /// <summary>
    /// Returns the current logical value.
    /// </summary>
    public double GetValue()
    {
        return Value;
    }

    /// <summary>
    /// Returns the current value as a 0-1 ratio.
    /// </summary>
    /// <returns>Normalized ratio of the current value.</returns>
    public double GetRatio()
    {
        var range = Maximum - Minimum;  // Compute the value range

        if (!(range <= 0)) return Clamp((Value - Minimum) / range); // Normalize the current value
        Debug.WriteLine("SeekBar: Invalid range detected.");    // Trace invalid range configuration
        return 0;   // Fall back to the minimum position

    }

    /// <summary>
    /// Clamps a ratio to the 0-1 range.
    /// </summary>
    /// <param name="value">Value to clamp.</param>
    /// <returns>Value clamped to 0-1.</returns>
    private static double Clamp(double value)
    {
        return Math.Max(0, Math.Min(1, value)); // Constrain ratio within valid bounds
    }
}
