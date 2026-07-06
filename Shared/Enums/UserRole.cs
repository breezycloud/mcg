namespace Shared.Enums;


public enum UserRole
{
    Master, // Dashboard and all
    Admin, // Dashboard and all
    Supervisor, // Driver data, Truck Data
    Maintenance, // Service Request
    Monitoring, // Trip Data, Service Request
    Management, // Dashboard only
    Nrl,
    DriverSupervisor,
    MaintenanceSupervisor, // Same access as Maintenance, plus Daily Reports, but not tied to a single site — sees all maintenance locations
    Manager // Same access as Master, minus User Management
}


