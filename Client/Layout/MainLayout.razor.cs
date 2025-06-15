using Client.Services.Flowbites;
using Microsoft.AspNetCore.Components;

namespace Client.Layout;


public partial class MainLayout
{
    [Inject]
    protected IFlowbiteService FlowbiteService { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await FlowbiteService.InitializeFlowbiteAsync();
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}