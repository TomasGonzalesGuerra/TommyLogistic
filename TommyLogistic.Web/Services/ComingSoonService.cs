namespace TommyLogistic.Web.Services;

public class ComingSoonService
{
    private bool _isVisible;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnChange?.Invoke();
        }
    }

    public event Action? OnChange;

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
}