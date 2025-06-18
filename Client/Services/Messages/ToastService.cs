using System.Timers;
using Shared.Enums;
using Shared.Helpers;

namespace Client.Services.Messages;

public class ToastService : IDisposable
{
    public event Action<ToastMessage>? OnShow;
    private System.Timers.Timer? _countdown;

    public void ShowSuccess(string message, string? title = "Success", int duration = 5000)
        => Show(message, title, ToastType.Success, duration);

    public void ShowError(string message, string? title = "Error", int duration = 5000)
        => Show(message, title, ToastType.Error, duration);

    public void ShowWarning(string message, string? title = "Warning", int duration = 5000)
        => Show(message, title, ToastType.Warning, duration);

    public void ShowInfo(string message, string? title = "Info", int duration = 5000)
        => Show(message, title, ToastType.Info, duration);

    private void Show(string message, string? title, ToastType type, int duration)
    {
        OnShow?.Invoke(new ToastMessage(message, title, type));
        StartCountdown(duration);
    }

    private void StartCountdown(int duration)
    {
        _countdown?.Dispose();
        _countdown = new System.Timers.Timer(duration);
        _countdown.Elapsed += HideToast;
        _countdown.AutoReset = false;
        _countdown.Start();
    }

    private void HideToast(object? source, ElapsedEventArgs args)
        => OnShow?.Invoke(new ToastMessage("", "", ToastType.Info) { IsVisible = false });

    public void Dispose() => _countdown?.Dispose();
}

