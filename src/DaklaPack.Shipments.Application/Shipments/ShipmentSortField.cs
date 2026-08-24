namespace DaklaPack.Shipments.Application.Shipments;

/// <summary>
/// The fields a shipment list may be sorted by. An enum rather than a free string, so an unknown
/// value is rejected at model binding and no caller text reaches the query builder.
/// </summary>
public enum ShipmentSortField
{
    DispatchedAt,
    EstimatedDeliveryOn,
    Status,
    Carrier,
    TrackingId,
}
