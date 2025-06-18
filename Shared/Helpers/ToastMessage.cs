using Shared.Enums;

namespace Shared.Helpers;

public record ToastMessage(string Message, string? Title, ToastType Type)
{
    public bool IsVisible { get; set; } = true;
}
