using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Drivers;
using Shared.Models.Incidents;
using Shared.Models.Services;
using Shared.Models.Stations;
using Shared.Models.TripCheckpoints;
using Shared.Models.Trucks;
using Shared.Models.Users;

namespace Shared.Models.Trips;

public class Trip
{
    [Key]
    public Guid Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public Guid? DriverId { get; set; }
    public Guid TruckId { get; set; }
    public string? DispatchId { get; set; }
    public Guid? LoadingDepotId { get; set; }
    public Guid? ReceivingDepotId { get; set; }
    [Column(TypeName = "jsonb")]
    public LoadingInfo LoadingInfo { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public ArrivalInfo ArrivalInfo { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public List<Metrics>? Metrics { get; set; } = [];
    [Column(TypeName = "jsonb")]
    public CloseInfo CloseInfo { get; set; } = new();
    public TripStatus Status { get; set; }
    public Guid? ClosedById { get; set; }
    public Guid? CompletedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }
    [ForeignKey(nameof(TruckId))]
    public virtual Truck? Truck { get; set; }
    public virtual ICollection<TripCheckpoint>? Checkpoints { get; set; } = [];
    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = [];
    public virtual ICollection<Incident> Incidents { get; set; } = [];
    public virtual ICollection<Discharge> Discharges { get; set; } = [];
    [ForeignKey(nameof(LoadingDepotId))]
    public Station? LoadingDepot { get; set; }

    [ForeignKey(nameof(ReceivingDepotId))]
    public Station? ReceivingDepot { get; set; }

    [ForeignKey(nameof(ClosedById))]
    public User? ClosedBy { get; set; }
    [ForeignKey(nameof(CompletedById))]
    public User? CompletedBy { get; set; }

    [NotMapped]
    public bool IsSelected { get; set; }

    public int CalculateTripDuration(DateTimeOffset createdDate, DateTimeOffset? returnDate)
    {
        var endDate = (returnDate ?? DateTimeOffset.Now).Date;
        var startDate = createdDate.Date;
        return (endDate - startDate).Days;
    }


    public decimal? CalculateShortageOverageAmount(decimal? Quantity, decimal? SumQuantity)
    {
        return Quantity - SumQuantity;
    }

    public string CalculateShortageOverage(decimal? Quantity)
    {
        if (Quantity < 0)
            return "Shortage";
        else if (Quantity > 0)
            return "Overage";
        else
            return "Nil";
    }

    public string CalculateShortageOverage(decimal? Quantity, decimal? SumQuantity)
    {
        if (SumQuantity - Quantity != 0)
            return "Overage";
        else if (SumQuantity - Quantity != 0)
            return "Shortage";
        else
            return "Nil";
    }

    public string GetUnit()
    {
        return Truck?.Product.ToString() switch
        {
            "CNG" => "SCM",
            "LPG" => "KG",
            _ => "LTR"
        };
    }

    public void CalculateQuatity()
    {
        if (Truck is not null && (LoadingInfo.Metrics is not null || LoadingInfo.Metrics.Any()))
        {
            if (Truck.Product == Product.PMS || Truck.Product == Product.AGO || Truck.Product == Product.ATK)
            {
                LoadingInfo.Metrics.ForEach((item) =>
                {
                    // item.Overall += item.Ullage?.Quantity ?? 0;
                    // item.Overall += item.LiquidHeight?.Quantity ?? 0;
                });
            }
            else if (Truck.Product == Product.LPG)
            {
                LoadingInfo.Metrics.ForEach((index) =>
                {
                    LoadingInfo.Quantity = index.GrossWeight - index.TareWeight;
                });
            }
            else
            {
                LoadingInfo.Quantity = LoadingInfo.Quantity;
            }
        }
    }
    public void SetInitialStatus()
    {
        Status = LoadingInfo == null || string.IsNullOrEmpty(LoadingInfo.WaybillNo) ? TripStatus.Dispatched : TripStatus.Active;
    }

    public void UpdateStatusWithLoadingInfo()
    {
        if (Status == TripStatus.Dispatched && LoadingInfo != null && !string.IsNullOrEmpty(LoadingInfo.WaybillNo))
        {
            Status = TripStatus.Active;
        }
    }
}

public class ArrivalInfo
{    
    public bool ArrivedDepot { get; set; }
    public bool ArrivedAtStation { get; set; }
    public bool InvoiceIssued { get; set; }
    public DateTime? DepotArrivalDateTime { get; set; }
    public string? Destination { get; set; }
    public DateTime? InvoiceToStationDateTime { get; set; }
    public DateTime? StationArrivalDateTime { get; set; }
    public string? Remark { get; set; }
}

public class CloseInfo
{
    public DateTime? ReturnDateTime { get; set; }
    public string? TripRemark { get; set; }
    public int Rating { get; set; } = 0; // 1-5
    public string? SupervisorSignaturePath { get; set; }
    public DateTime CloseDateTime { get; set; } = DateTime.UtcNow;
}

public class TripReconciliation
{
    public decimal LoadingQuantity { get; set; }
    public List<Discharge> Discharges { get; set; } = new();
    public decimal TolerancePercentage { get; set; } = 0.29m;

    // Calculated properties
    public decimal TotalDischarged => Discharges.Sum(d => d.QuantityDischarged);
    public decimal Variance => LoadingQuantity - TotalDischarged;
    public decimal VariancePercentage => (Math.Abs(Variance) / LoadingQuantity) * 100m;
    public decimal AllowedVariance => LoadingQuantity * (TolerancePercentage / 100m);

    public bool IsWithinTolerance => Math.Abs(Variance) <= AllowedVariance;
}
