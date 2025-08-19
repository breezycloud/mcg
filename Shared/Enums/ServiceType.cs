using System.ComponentModel;

namespace Shared.Enums;


public enum ServiceType
{
    Routine,
    Emergency,
    Unscheduled,
    Incident
}

public enum ServiceItem
{
    Body,
    Elock,
    TPMS,
    Camera,
    Driver
}
