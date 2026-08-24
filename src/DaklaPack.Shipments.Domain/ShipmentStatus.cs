namespace DaklaPack.Shipments.Domain;

/// <summary>The lifecycle state of a shipment.</summary>
public enum ShipmentStatus
{
    Created,
    InTransit,
    OutForDelivery,
    Delivered,
    Delayed,
    Cancelled,
}
