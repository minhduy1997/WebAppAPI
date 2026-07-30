namespace WebAppApi.Models;

public enum CustomerGender
{
    MALE = 0,
    FEMALE = 1,
    OTHER = 2,
}

public enum CustomerVerificationStatus
{
    NOT_SUBMITTED = 0,
    PENDING = 1,
    VERIFIED = 2,
    REJECTED = 3,
    EXPIRED = 4,
}

public enum CustomerDocumentType
{
    IDENTITY_CARD = 0,
    DRIVER_LICENSE = 1,
    PASSPORT = 2,
    OTHER = 3,
}

public enum BookingStatus
{
    DRAFT = 0,
    PENDING_DEPOSIT = 1,
    CONFIRMED = 2,
    VEHICLE_ASSIGNED = 3,
    READY_FOR_PICKUP = 4,
    IN_PROGRESS = 5,
    COMPLETED = 6,
    CANCELLED = 7,
    EXPIRED = 8,
}
