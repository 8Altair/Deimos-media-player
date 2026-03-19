using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Deimos.UI.Controls;


namespace Deimos.UI;

public partial class MainWindow
{
    private readonly SeekBar _seekBar = new();

    private void InitializeSeekBarLogic()
    {
        UpdateSeekBarVisual();
    }

    private void SeekBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _seekBar.BeginDrag();
        SeekBar.CaptureMouse();
        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);
    }

    private void SeekBar_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_seekBar.IsDragging || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);
    }

    private void SeekBar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_seekBar.IsDragging)
            return;

        _seekBar.EndDrag();
        UpdateSeekBarFromMouse(e.GetPosition(SeekBar).X);
        SeekBar.ReleaseMouseCapture();
    }

    private void SeekBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSeekBarVisual();
    }

    private void UpdateSeekBarFromMouse(double mouseX)
    {
        if (!_seekBar.UpdateFromMouse(mouseX, SeekBar.ActualWidth))
            return;

        UpdateSeekBarVisual();
    }

    private void UpdateSeekBarVisual()
    {
        var width = SeekBar.ActualWidth;

        if (width <= 0)
            return;

        var ratio = _seekBar.GetRatio();
        var progressWidth = width * ratio;
        SeekBarProgress.Width = progressWidth;

        var thumbLeft = progressWidth - SeekBarThumb.Width / 2;
        thumbLeft = Math.Max(0, Math.Min(width - SeekBarThumb.Width, thumbLeft));

        Canvas.SetLeft(SeekBarThumb, thumbLeft);
    }
}
