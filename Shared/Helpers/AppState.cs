namespace Shared.Helpers;


public class AppState
{
    public bool IsProcessing { get; set; } = false;
    public bool IsBusy { get; set; }

    public CancellationToken GetCancellationToken() =>
        new CancellationTokenSource().Token;

}