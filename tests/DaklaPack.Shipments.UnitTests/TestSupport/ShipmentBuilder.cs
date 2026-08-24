using DaklaPack.Shipments.Domain;
using DaklaPack.Shipments.Domain.ValueObjects;

namespace DaklaPack.Shipments.UnitTests.TestSupport;

/// <summary>
/// Builds shipments for tests, so each test states only the field it actually cares about.
/// </summary>
internal sealed class ShipmentBuilder
{
    private Guid _id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private TrackingId _trackingId = new("DP-2026-000001");
    private ShipmentStatus _status = ShipmentStatus.InTransit;
    private Weight _weight = new(10m);
    private Destination _destination = new("Amsterdam", "NL", "1011AB");
    private string _carrier = "PostNL";
    private DateTimeOffset _dispatchedAt = new(2026, 8, 1, 9, 0, 0, TimeSpan.FromHours(2));
    private DateOnly _estimatedDeliveryOn = new(2026, 8, 5);

    public static ShipmentBuilder A() => new();

    public ShipmentBuilder WithId(Guid id) { _id = id; return this; }

    public ShipmentBuilder WithTrackingId(string value) { _trackingId = new TrackingId(value); return this; }

    public ShipmentBuilder WithStatus(ShipmentStatus status) { _status = status; return this; }

    public ShipmentBuilder WithWeight(decimal kilograms) { _weight = new Weight(kilograms); return this; }

    public ShipmentBuilder WithDestination(string city, string country, string postalCode)
    {
        _destination = new Destination(city, country, postalCode);
        return this;
    }

    public ShipmentBuilder WithCarrier(string carrier) { _carrier = carrier; return this; }

    public ShipmentBuilder DispatchedAt(DateTimeOffset at) { _dispatchedAt = at; return this; }

    public ShipmentBuilder DueOn(int year, int month, int day)
    {
        _estimatedDeliveryOn = new DateOnly(year, month, day);
        return this;
    }

    public Shipment Build() => new(
        _id, _trackingId, _status, _weight, _destination, _carrier, _dispatchedAt, _estimatedDeliveryOn);
}
