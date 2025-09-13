using Client.Services.Flowbites;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Client.Layout;


public partial class MainLayout
{
    [Inject]
    protected IFlowbiteService FlowbiteService { get; set; } = default!;
    [CascadingParameter] public Task<AuthenticationState>? AuthenticationState { get; set; } = default;
    AuthenticationState Authentication { get; set; }
    private bool IsAuthenticated = false;
    protected override async Task OnInitializedAsync()
    {
        Authentication = await AuthenticationState!;
        if (Authentication.User.Identity.IsAuthenticated)
            await _js.InvokeVoidAsync("triggerPwaInstall", null);
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