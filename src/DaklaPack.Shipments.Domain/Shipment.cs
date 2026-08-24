using DaklaPack.Shipments.Domain.Exceptions;
using DaklaPack.Shipments.Domain.ValueObjects;

namespace DaklaPack.Shipments.Domain;

/// <summary>
/// A parcel consignment moving through the Daklapack network. Identity is <see cref="Id"/>.
/// </summary>
/// <remarks>
/// Every argument is checked, so an instance cannot hold a malformed reference, a negative weight,
/// an unnamed carrier or an undefined status. That is the point of the type: callers downstream
/// never have to re-check.
/// </remarks>
public sealed class Shipment
{
    /// <exception cref="DomainException">An argument would leave the shipment invalid.</exception>
    public Shipment(
        Guid id,
        TrackingId trackingId,
        ShipmentStatus status,
        Weight weight,
        Destination destination,
        string carrier,
        DateTimeOffset dispatchedAt,
        DateOnly estimatedDeliveryOn)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Shipment id must not be empty.");
        }

        // Guarded explicitly rather than relying on nullable reference types: this constructor is
        // reached from persistence and deserialisation, where the compiler's view is not binding.
        ArgumentNullException.ThrowIfNull(trackingId);
        ArgumentNullException.ThrowIfNull(weight);
        ArgumentNullException.ThrowIfNull(destination);

        if (!Enum.IsDefined(status))
        {
            throw new DomainException($"Shipment status '{status}' is not a defined status.");
        }

        if (string.IsNullOrWhiteSpace(carrier))
        {
            throw new DomainException("Carrier must not be blank.");
        }

        Id = id;
        TrackingId = trackingId;
        Status = status;
        Weight = weight;
        Destination = destination;
        Carrier = carrier.Trim();
        DispatchedAt = dispatchedAt;
        EstimatedDeliveryOn = estimatedDeliveryOn;
    }

    public Guid Id { get; }

    public TrackingId TrackingId { get; }

    public ShipmentStatus Status { get; }

    public Weight Weight { get; }

    public Destination Destination { get; }

    public string Carrier { get; }

    /// <summary>When the shipment left the depot. An instant, so it carries a UTC offset.</summary>
    public DateTimeOffset DispatchedAt { get; }

    /// <summary>
    /// The date delivery is expected. A calendar date, not an instant: it is a day a human agreed
    /// to, and must not shift when read in another time zone.
    /// </summary>
    public DateOnly EstimatedDeliveryOn { get; }

    /// <summary>
    /// Past its estimated delivery date and not in a terminal state.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="ShipmentStatus.Delayed"/>: that is what the carrier reported, this
    /// is what the calendar says. A shipment can be overdue with the carrier having said nothing,
    /// which is exactly the case an operations user needs to see.
    ///
    /// A pure function of the supplied date, so the caller decides which calendar "today" means.
    /// </remarks>
    public bool IsOverdue(DateOnly asOf) =>
        asOf > EstimatedDeliveryOn
        && Status is not (ShipmentStatus.Delivered or ShipmentStatus.Cancelled);
}
