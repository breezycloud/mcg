using System.Timers;
using Shared.Enums;
using Shared.Helpers;

namespace Client.Services.Messages;

public class ToastService
{
    public event Action<string, string, int>? OnShow;
    public event Action? OnHide;

    public void ShowToast(string message, string type = "info", int duration = 3)
        => OnShow?.Invoke(message, type, duration);

    public void HideToast() => OnHide?.Invoke();

    public void ShowSuccess(string message, int duration = 3) => ShowToast(message, "success", duration);
    public void ShowError(string message, int duration = 3) => ShowToast(message, "error", duration);
    public void ShowWarning(string message, int duration = 3) => ShowToast(message, "warning", duration);
    public void ShowInfo(string message, int duration = 3) => ShowToast(message, "info", duration);
}