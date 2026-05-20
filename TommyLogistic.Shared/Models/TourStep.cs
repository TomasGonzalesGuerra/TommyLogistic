namespace TommyLogistic.Shared.Models;

public class TourStep
{
    public string TargetSelector { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MascotMood Mood { get; set; } = MascotMood.Happy;
    public BubblePosition BubblePos { get; set; } = BubblePosition.Right;
}
