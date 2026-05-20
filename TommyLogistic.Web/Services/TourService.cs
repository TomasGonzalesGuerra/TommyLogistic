using TommyLogistic.Shared.Models;

namespace TommyLogistic.Web.Services;

public class TourService
{
    private bool _isVisible;
    public bool IsVisible { get; private set; }
    public int CurrentStep { get; private set; }
    public List<TourStep> Steps { get; private set; } = [];

    public event Action? OnChange;

    public bool IsActive { get; private set; }
    public TourStep? Current => Steps.ElementAtOrDefault(CurrentStep);
    public bool IsLastStep => CurrentStep >= Steps.Count - 1;
    public bool IsFirstStep => CurrentStep == 0;
    public int TotalSteps => Steps.Count;


    public void TryStartTour(string role, bool alreadyDone)
    {
        if (alreadyDone) return;
        Steps = TourSteps.GetStepsForRole(role);
        if (!Steps.Any()) return;
        CurrentStep = 0;
        IsActive = true;
        NotifyChange();
    }

    public void StartTourManual(string role)
    {
        Steps = TourSteps.GetStepsForRole(role);
        if (!Steps.Any()) return;
        CurrentStep = 0;
        IsActive = true;
        NotifyChange();
    }

    public void Next()
    {
        if (IsLastStep) return;
        CurrentStep++;
        NotifyChange();
    }

    public void Previous()
    {
        if (IsFirstStep) return;
        CurrentStep--;
        NotifyChange();
    }

    public void Finish()
    {
        IsActive = false;
        NotifyChange();
    }

    public void Skip() => Finish();

    private void NotifyChange() => OnChange?.Invoke();
}