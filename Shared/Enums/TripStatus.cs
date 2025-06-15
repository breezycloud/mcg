using System.ComponentModel;

namespace Shared.Enums;



public enum TripStatus
{
    [Description("In Progress")]
    InProgress,
    [Description("Completed")]
    Completed
}