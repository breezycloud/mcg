using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public class SidebarService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;

    public bool IsCollapsed { get; private set; } = false;

    public event Action? OnChange;

    public SidebarService(ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        // Restore saved state
        var saved = await _localStorage.GetItemAsync<bool?>("sidebar.collapsed");
        IsCollapsed = saved ?? false;

        // On mobile, force collapse
        if (await IsMobileAsync())
        {
            IsCollapsed = true;
        }

        NotifyStateChanged();
    }

    public async Task ToggleAsync()
    {
        IsCollapsed = !IsCollapsed;
        await SaveStateAsync();
        await UpdateBasedOnScreenSizeAsync();
        NotifyStateChanged();
    }

    public async Task UpdateBasedOnScreenSizeAsync()
    {
        var isMobile = await IsMobileAsync();
        if (isMobile && !IsCollapsed)
        {
            IsCollapsed = true;
            await SaveStateAsync();
            NotifyStateChanged();
        }
        else if (!isMobile && IsCollapsed == false)
        {
            // Keep user preference unless mobile
            await SaveStateAsync();
            NotifyStateChanged();
        }
    }

    private async Task<bool> IsMobileAsync()
    {
        return await _jsRuntime.InvokeAsync<bool>("window.matchMedia('(max-width: 768px)').matches");
    }

    private async Task SaveStateAsync()
    {
        await _localStorage.SetItemAsync("sidebar.collapsed", IsCollapsed);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}