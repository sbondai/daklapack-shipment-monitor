using DaklaPack.Shipments.Domain;

namespace DaklaPack.Shipments.Application.Shipments.GetShipments;

/// <summary>
/// A shipment as it appears on the wire.
/// </summary>
/// <remarks>
/// Value objects are shaped differently on purpose. <see cref="TrackingId"/> and
/// <see cref="WeightKg"/> wrap a single scalar, so they flatten, with the unit in the property name.
/// <see cref="Destination"/> is genuinely compound and stays nested.
///
/// Init-only rather than positional so <see cref="IsOverdue"/> can be excluded from the generated
/// mapping: it is derived from the current date, which the entity does not carry.
/// </remarks>
public sealed record ShipmentResponse
{
    public required Guid Id { get; init; }

    public required string TrackingId { get; init; }

    public required ShipmentStatus Status { get; init; }

    public required decimal WeightKg { get; init; }

    public required DestinationResponse Destination { get; init; }

    public required string Carrier { get; init; }

    /// <summary>An instant. ISO 8601 with a UTC offset.</summary>
    public required DateTimeOffset DispatchedAt { get; init; }

    /// <summary>A calendar date. ISO 8601, no time and no zone.</summary>
    public required DateOnly EstimatedDeliveryOn { get; init; }

    /// <summary>Past its delivery date and not finished. Derived at read time, stored nowhere.</summary>
    public bool IsOverdue { get; init; }
}

/// <summary>A delivery address as it appears on the wire.</summary>
public sealed record DestinationResponse(string City, string CountryCode, string PostalCode);
