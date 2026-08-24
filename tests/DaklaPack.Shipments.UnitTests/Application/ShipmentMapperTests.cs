using DaklaPack.Shipments.Application.Shipments.GetShipments;
using DaklaPack.Shipments.Domain;
using DaklaPack.Shipments.UnitTests.TestSupport;

using Shouldly;

namespace DaklaPack.Shipments.UnitTests.Application;

public sealed class ShipmentMapperTests
{
    private readonly ShipmentMapper _mapper = new();

    [Fact]
    public void Flattens_single_value_value_objects()
    {
        var response = _mapper.ToResponse(
            ShipmentBuilder.A().WithTrackingId("DP-2026-000456").WithWeight(7.25m).Build(),
            new DateOnly(2026, 8, 1));

        response.TrackingId.ShouldBe("DP-2026-000456");
        response.WeightKg.ShouldBe(7.25m);
    }

    [Fact]
    public void Keeps_the_compound_value_object_nested()
    {
        var response = _mapper.ToResponse(
            ShipmentBuilder.A().WithDestination("Utrecht", "nl", "3511LX").Build(),
            new DateOnly(2026, 8, 1));

        response.Destination.City.ShouldBe("Utrecht");
        response.Destination.CountryCode.ShouldBe("NL");
        response.Destination.PostalCode.ShouldBe("3511LX");
    }

    [Fact]
    public void Derives_is_overdue_from_the_supplied_date()
    {
        var shipment = ShipmentBuilder.A().DueOn(2026, 8, 5).WithStatus(ShipmentStatus.InTransit).Build();

        _mapper.ToResponse(shipment, new DateOnly(2026, 8, 4)).IsOverdue.ShouldBeFalse();
        _mapper.ToResponse(shipment, new DateOnly(2026, 8, 6)).IsOverdue.ShouldBeTrue();
    }

    [Fact]
    public void Carries_every_remaining_field_across()
    {
        var dispatched = new DateTimeOffset(2026, 8, 2, 14, 30, 0, TimeSpan.FromHours(2));
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var response = _mapper.ToResponse(
            ShipmentBuilder.A()
                .WithId(id)
                .WithStatus(ShipmentStatus.OutForDelivery)
                .WithCarrier("DHL")
                .DispatchedAt(dispatched)
                .DueOn(2026, 8, 9)
                .Build(),
            new DateOnly(2026, 8, 1));

        response.Id.ShouldBe(id);
        response.Status.ShouldBe(ShipmentStatus.OutForDelivery);
        response.Carrier.ShouldBe("DHL");
        response.DispatchedAt.ShouldBe(dispatched);
        response.EstimatedDeliveryOn.ShouldBe(new DateOnly(2026, 8, 9));
    }

    [Fact]
    public void Maps_an_empty_page_without_complaint() =>
        _mapper.ToResponses([], new DateOnly(2026, 8, 1)).ShouldBeEmpty();
}
