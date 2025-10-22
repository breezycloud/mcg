using Client.Services.Flowbites;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Client.Layout;


public partial class MainLayout
{
    [Inject]
    protected IFlowbiteService FlowbiteService { get; set; } = default!;
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;
    [CascadingParameter] public Task<AuthenticationState>? AuthenticationState { get; set; } = default;
    AuthenticationState Authentication { get; set; }
    private bool IsAuthenticated = false;
    protected override async Task OnInitializedAsync()
    {
        Authentication = await AuthenticationState!;
        if (Authentication.User.Identity.IsAuthenticated)
            await _js.InvokeVoidAsync("triggerPwaInstall", null);
    }
    
    string GetEnvironment()
    {
        var host = new Uri(NavigationManager.BaseUri).Host;
        Console.WriteLine("Host: {0}", host);
        if (host.Contains("staging"))
            return "Staging";
        else if (host.Contains("atlanticlogistics-atv.com.ng"))
            return "";
        else
            return "Development";
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await FlowbiteService.InitializeFlowbiteAsync();            
                
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}