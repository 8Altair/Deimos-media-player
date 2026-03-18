using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Deimos.UI;

public partial class MainWindow
{
    private bool _isDraggingSeekBar;
    private const double SeekMinimum = 0;
    private const double SeekMaximum = 100;
    private double _seekValue;

    private void SeekBar_OnMouseLeftButtonDown(object? _, MouseButtonEventArgs e)
    {
        _isDraggingSeekBar = true;
        SeekBar.CaptureMouse();
        SetSeekBarValueFromMouse(e.GetPosition(SeekBar).X);
    }

    private void SeekBar_OnMouseMove(object? _, MouseEventArgs e)
    {
        if (!_isDraggingSeekBar || e.LeftButton != MouseButtonState.Pressed)
            return;

        SetSeekBarValueFromMouse(e.GetPosition(SeekBar).X);
    }

    private void SeekBar_OnMouseLeftButtonUp(object? _, MouseButtonEventArgs e)
    {
        if (!_isDraggingSeekBar)
            return;

        _isDraggingSeekBar = false;
        SetSeekBarValueFromMouse(e.GetPosition(SeekBar).X);
        SeekBar.ReleaseMouseCapture();
    }

    private void SeekBar_OnSizeChanged(object? _, SizeChangedEventArgs e)
    {
        UpdateSeekBarVisual();
    }

    private void SetSeekBarValueFromMouse(double mouseX)
    {
        var width = SeekBar.ActualWidth;

        if (width <= 0)
            return;

        var ratio = mouseX / width;
        ratio = Math.Max(0, Math.Min(1, ratio));

        _seekValue = SeekMinimum + (SeekMaximum - SeekMinimum) * ratio;
        UpdateSeekBarVisual();
    }

    private void UpdateSeekBarVisual()
    {
        var width = SeekBar.ActualWidth;

        if (width <= 0)
            return;

        const double range = SeekMaximum - SeekMinimum;

        var ratio = (_seekValue - SeekMinimum) / range;
        ratio = Math.Max(0, Math.Min(1, ratio));

        var progressWidth = width * ratio;
        SeekBarProgress.Width = progressWidth;

        var thumbLeft = progressWidth - SeekBarThumb.Width / 2;
        thumbLeft = Math.Max(0, Math.Min(width - SeekBarThumb.Width, thumbLeft));

        Canvas.SetLeft(SeekBarThumb, thumbLeft);
    }
}
