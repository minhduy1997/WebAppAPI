namespace WebAppApi.Models;

public enum TransmissionType
{
    MT = 0,
    AT = 1,
}

public enum FuelType
{
    RONE10 = 0,
    DIESEL = 1,
    ELECTRIC = 2,
}

public enum Drivetrain
{
    FWD = 0,
    RWD = 1,
    AWD = 2,
    FourWD = 3,
}

/// <summary>Operational status of a physical vehicle unit.</summary>
public enum VehicleStatus
{
    AVAILABLE = 0,
    RESERVED = 1,
    RENTED = 2,
    MAINTENANCE = 3,
    REPAIRING = 4,
    UNAVAILABLE = 5,
    INACTIVE = 6,
}
