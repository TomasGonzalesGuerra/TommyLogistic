using Blazored.LocalStorage;
using TommyLogistic.Shared.Models;

namespace TommyLogistic.Web.Services;

public class TourService(ILocalStorageService localStorage)
{
    private readonly ILocalStorageService _localStorage = localStorage;

    public bool IsActive { get; private set; }
    public int CurrentStep { get; private set; }
    public List<TourStep> Steps { get; private set; } = [];

    public event Action? OnChange;

    public TourStep? Current => Steps.ElementAtOrDefault(CurrentStep);
    public bool IsLastStep => CurrentStep >= Steps.Count - 1;
    public bool IsFirstStep => CurrentStep == 0;
    public int TotalSteps => Steps.Count;

    public async Task TryStartTourAsync(string role)
    {
        var key = $"tour_done_{role.ToLower()}";
        var done = await _localStorage.GetItemAsync<bool>(key);
        if (done) return;

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

    public async Task FinishAsync(string role)
    {
        IsActive = false;
        var key = $"tour_done_{role.ToLower()}";
        await _localStorage.SetItemAsync(key, true);
        NotifyChange();
    }

    public async Task SkipAsync(string role) => await FinishAsync(role);

    public async Task ResetTourAsync(string role)
    {
        var key = $"tour_done_{role.ToLower()}";
        await _localStorage.RemoveItemAsync(key);
    }

    private void NotifyChange() => OnChange?.Invoke();
}