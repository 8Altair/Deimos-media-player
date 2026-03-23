using System.Windows.Media;


namespace Deimos.UI.Models;

public class MediaFile
{
    public string? Title { get; set; }
    public string? FilePath { get; set; }
    public string? ImagePath { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public bool IsPlaying { get; set; }

    public MediaFile()
    {
        
    }
}