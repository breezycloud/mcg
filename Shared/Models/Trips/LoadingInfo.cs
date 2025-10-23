using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Helpers;

namespace Shared.Models.Trips;

public class LoadingInfo
{        
    public DateTime? LoadingDate { get; set; }
    public string? WaybillNo { get; set; }
    public decimal? Quantity { get; set; }
    [Column(TypeName = "jsonb")]
    public List<Metrics>? Metrics { get; set; } = [];
    [Column(TypeName = "jsonb")]
    public List<UploadResult> Files { get; set; } = [];    
    public DispatchType DispatchType { get; set; }
    public ElockStatus ElockStatus { get; set; }
    public string? Destination { get; set; }
    public string? Remark { get; set; }
}