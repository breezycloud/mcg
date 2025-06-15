namespace Shared.Enums;


public enum UserRole
{
    Master,
    Admin,
    Supervisor, // Driver data, Truck Data
    Maintenance, // Service Request
    Monitoring, // Trip Data, Service Request
    Management // Dashboard only
}