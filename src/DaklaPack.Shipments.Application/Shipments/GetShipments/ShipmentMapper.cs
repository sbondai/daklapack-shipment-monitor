using DaklaPack.Shipments.Domain;
using DaklaPack.Shipments.Domain.ValueObjects;

using Riok.Mapperly.Abstractions;

namespace DaklaPack.Shipments.Application.Shipments.GetShipments;

/// <summary>
/// Maps the shipment aggregate onto its wire representation.
/// </summary>
/// <remarks>
/// Mapperly generates the body at compile time, so a renamed property is a build error rather than
/// a runtime surprise. The value-object conversions are declared explicitly: the wire contract
/// should be something the code states, not something a naming convention happens to produce.
/// </remarks>
[Mapper]
public sealed partial class ShipmentMapper
{
    public ShipmentResponse ToResponse(Shipment shipment, DateOnly asOf) =>
        Map(shipment) with { IsOverdue = shipment.IsOverdue(asOf) };

    public IReadOnlyList<ShipmentResponse> ToResponses(IEnumerable<Shipment> shipments, DateOnly asOf) =>
        [.. shipments.Select(shipment => ToResponse(shipment, asOf))];

    // IsOverdue depends on a date the entity does not carry, so ToResponse sets it.
    [MapperIgnoreTarget(nameof(ShipmentResponse.IsOverdue))]
    [MapProperty(nameof(Shipment.Weight), nameof(ShipmentResponse.WeightKg))]
    private partial ShipmentResponse Map(Shipment shipment);

    private static string MapTrackingId(TrackingId trackingId) => trackingId.Value;

    private static decimal MapWeight(Weight weight) => weight.Kilograms;

    private partial DestinationResponse MapDestination(Destination destination);
}
