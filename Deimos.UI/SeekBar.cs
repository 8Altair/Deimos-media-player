namespace Deimos.UI;

public sealed class SeekBar(double minimum = 0, double maximum = 100)
{
    public bool IsDragging { get; private set; }
    private double Minimum { get; } = minimum;
    private double Maximum { get; } = maximum;
    private double Value { get; set; }

    public void BeginDrag()
    {
        IsDragging = true;
    }

    public void EndDrag()
    {
        IsDragging = false;
    }

    public bool UpdateFromMouse(double mouseX, double width)
    {
        if (width <= 0)
            return false;

        var ratio = Clamp(mouseX / width);
        Value = Minimum + (Maximum - Minimum) * ratio;
        return true;
    }

    public double GetRatio()
    {
        var range = Maximum - Minimum;

        if (range <= 0)
            return 0;

        return Clamp((Value - Minimum) / range);
    }

    private static double Clamp(double value)
    {
        return Math.Max(0, Math.Min(1, value));
    }
}
