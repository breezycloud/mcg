using System.ComponentModel;

namespace Shared.Enums;


public enum UserRole
{
    [Description("Master")]
    Master, // Dashboard and all
    [Description("Admin")]
    Admin, // Dashboard and all
    [Description("Supervisor")]
    Supervisor, // Driver data, Truck Data
    [Description("Maintenance")]
    Maintenance, // Service Request
    [Description("Monitoring")]
    Monitoring, // Trip Data, Service Request
    [Description("Management")]
    Management, // Dashboard only
    [Description("Nrl")]
    Nrl,
    [Description("Equipment Manager")]
    EquipmentManager,
    [Description("Driver Supervisor")]
    DriverSupervisor
}


