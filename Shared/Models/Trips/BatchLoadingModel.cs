using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Helpers;
using Shared.Models.Checkpoints;
using Shared.Models.Drivers;
using Shared.Models.Incidents;
using Shared.Models.Services;
using Shared.Models.Stations;
using Shared.Models.TripCheckpoints;
using Shared.Models.Trucks;
using Shared.Models.Users;


namespace Shared.Models.Trips;

public class BatchLoadingModel
{
    public DateTime? LoadingDate { get; set; }
    public DispatchType? DispatchType { get; set; }
    public string? Destination { get; set; }
}