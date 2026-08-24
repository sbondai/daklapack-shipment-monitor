using DaklaPack.Shipments.Domain;
using DaklaPack.Shipments.Domain.Exceptions;
using DaklaPack.Shipments.Domain.ValueObjects;
using DaklaPack.Shipments.UnitTests.TestSupport;

using Shouldly;

namespace DaklaPack.Shipments.UnitTests.Domain;

public sealed class ShipmentTests
{
    private static readonly DateOnly DueDate = new(2026, 8, 5);

    public sealed class IsOverdue
    {
        [Fact]
        public void Is_false_before_the_due_date() =>
            ShipmentBuilder.A().DueOn(2026, 8, 5).Build()
                .IsOverdue(new DateOnly(2026, 8, 4)).ShouldBeFalse();

        [Fact]
        public void Is_false_on_the_due_date_itself()
        {
            // The boundary that matters: a shipment due today is not yet late.
            ShipmentBuilder.A().DueOn(2026, 8, 5).Build()
                .IsOverdue(new DateOnly(2026, 8, 5)).ShouldBeFalse();
        }

        [Fact]
        public void Is_true_the_day_after_the_due_date() =>
            ShipmentBuilder.A().DueOn(2026, 8, 5).Build()
                .IsOverdue(new DateOnly(2026, 8, 6)).ShouldBeTrue();

        [Theory]
        [InlineData(ShipmentStatus.Delivered)]
        [InlineData(ShipmentStatus.Cancelled)]
        public void Is_false_in_a_terminal_state_however_late(ShipmentStatus terminal) =>
            ShipmentBuilder.A().DueOn(2026, 8, 5).WithStatus(terminal).Build()
                .IsOverdue(new DateOnly(2027, 1, 1)).ShouldBeFalse();

        [Theory]
        [InlineData(ShipmentStatus.Created)]
        [InlineData(ShipmentStatus.InTransit)]
        [InlineData(ShipmentStatus.OutForDelivery)]
        [InlineData(ShipmentStatus.Delayed)]
        public void Is_true_in_any_non_terminal_state_past_the_date(ShipmentStatus inFlight) =>
            ShipmentBuilder.A().DueOn(2026, 8, 5).WithStatus(inFlight).Build()
                .IsOverdue(new DateOnly(2026, 8, 6)).ShouldBeTrue();

        [Fact]
        public void Is_independent_of_the_carrier_reported_delay()
        {
            // "Delayed" is what the carrier told us; "overdue" is what the calendar says.
            // A shipment can be overdue while the carrier has reported nothing at all.
            var quietlyLate = ShipmentBuilder.A()
                .DueOn(2026, 8, 5)
                .WithStatus(ShipmentStatus.InTransit)
                .Build();

            quietlyLate.IsOverdue(new DateOnly(2026, 8, 10)).ShouldBeTrue();
            quietlyLate.Status.ShouldNotBe(ShipmentStatus.Delayed);
        }
    }

    public sealed class Invariants
    {
        private static Shipment Build(
            Guid? id = null,
            TrackingId? trackingId = null,
            ShipmentStatus status = ShipmentStatus.InTransit,
            Weight? weight = null,
            Destination? destination = null,
            string carrier = "PostNL") =>
            new(id ?? Guid.NewGuid(),
                trackingId ?? new TrackingId("DP-2026-000001"),
                status,
                weight ?? new Weight(1m),
                destination ?? new Destination("Amsterdam", "NL", "1011AB"),
                carrier,
                DateTimeOffset.UnixEpoch,
                new DateOnly(2026, 8, 5));

        [Fact]
        public void Rejects_an_empty_id() =>
            Should.Throw<DomainException>(() => Build(id: Guid.Empty));

        [Fact]
        public void Rejects_a_status_outside_the_defined_set() =>
            // Any int can be cast to an enum, so a bad value can reach here from persistence or
            // deserialisation without the compiler objecting.
            Should.Throw<DomainException>(() => Build(status: (ShipmentStatus)99));

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Rejects_a_blank_carrier(string carrier) =>
            Should.Throw<DomainException>(() => Build(carrier: carrier));

        [Fact]
        public void Trims_the_carrier_once() =>
            Build(carrier: "  DHL  ").Carrier.ShouldBe("DHL");

        // Constructed directly rather than through Build: its `?? default` fallbacks would
        // substitute a valid value and the test would silently assert nothing.
        private static Shipment WithParts(TrackingId trackingId, Weight weight, Destination destination) =>
            new(Guid.NewGuid(), trackingId, ShipmentStatus.InTransit, weight, destination,
                "PostNL", DateTimeOffset.UnixEpoch, new DateOnly(2026, 8, 5));

        [Fact]
        public void Rejects_a_null_tracking_id() =>
            Should.Throw<ArgumentNullException>(() =>
                WithParts(null!, new Weight(1m), new Destination("Amsterdam", "NL", "1011AB")));

        [Fact]
        public void Rejects_a_null_weight() =>
            Should.Throw<ArgumentNullException>(() =>
                WithParts(new TrackingId("DP-2026-000001"), null!, new Destination("Amsterdam", "NL", "1011AB")));

        [Fact]
        public void Rejects_a_null_destination() =>
            Should.Throw<ArgumentNullException>(() =>
                WithParts(new TrackingId("DP-2026-000001"), new Weight(1m), null!));
    }

    [Fact]
    public void Exposes_the_values_it_was_built_with()
    {
        var shipment = ShipmentBuilder.A()
            .WithTrackingId("DP-2026-000777")
            .WithWeight(42.5m)
            .WithDestination("Rotterdam", "NL", "3011AA")
            .WithCarrier("DHL")
            .DueOn(DueDate.Year, DueDate.Month, DueDate.Day)
            .Build();

        shipment.TrackingId.Value.ShouldBe("DP-2026-000777");
        shipment.Weight.Kilograms.ShouldBe(42.5m);
        shipment.Destination.City.ShouldBe("Rotterdam");
        shipment.Carrier.ShouldBe("DHL");
        shipment.EstimatedDeliveryOn.ShouldBe(DueDate);
    }
}
