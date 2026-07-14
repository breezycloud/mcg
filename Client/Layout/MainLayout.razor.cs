using Client.Services.Flowbites;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Client.Layout;


public partial class MainLayout
{
    [Inject]
    protected IFlowbiteService FlowbiteService { get; set; } = default!;
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    protected AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [CascadingParameter] public Task<AuthenticationState>? AuthenticationState { get; set; } = default;
    AuthenticationState Authentication { get; set; }
    protected override async Task OnInitializedAsync()
    {
        Authentication = await AuthenticationState!;
        if (Authentication.User.Identity.IsAuthenticated)
            await _js.InvokeVoidAsync("triggerPwaInstall", null);

        NavigationManager.RegisterLocationChangingHandler(OnLocationChanging);
    }

    // Route guard: a user flagged MustChangePassword can only reach /change-password until
    // they set a new one. Re-fetches auth state fresh each navigation (rather than relying on
    // the Authentication field captured at startup) so it reflects a token issued after this
    // layout first loaded — e.g. right after logging in.
    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var mustChangeClaim = authState.User?.FindFirst("must_change_password")?.Value;
        if (bool.TryParse(mustChangeClaim, out var mustChange) && mustChange
            && !context.TargetLocation.Contains("change-password", StringComparison.OrdinalIgnoreCase))
        {
            context.PreventNavigation();
            NavigationManager.NavigateTo("/change-password");
        }
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