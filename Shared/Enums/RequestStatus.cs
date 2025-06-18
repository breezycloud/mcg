using System.ComponentModel;

namespace Shared.Enums;


public enum RequestStatus
{
    [Description("Pending")]
    Pending,
    [Description("In Progress")]
    InProgress,
    [Description("Escalated")]
    Escalated,
    [Description("Closed")]
    Closed,
    [Description("Treated")]
    Treated    
}