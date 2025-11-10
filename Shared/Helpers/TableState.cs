namespace Shared.Helpers;

public class TableState<T>
{
    public GridDataRequest Request { get; set; } = new();
    public GridDataResponse<T>? Response { get; set; }
    public bool IsLoading { get; set; }
    public string? Error { get; set; }
}