using Microsoft.AspNetCore.Components;

public partial class Search : ComponentBase
{
    public string SearchTerm { get; set; }
    public string Text { get; set;  }
    [Parameter]
    public EventCallback<string> OnSearchChanged { get; set; }
}