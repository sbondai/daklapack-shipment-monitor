using DaklaPack.Shipments.Domain.Exceptions;
using DaklaPack.Shipments.Domain.ValueObjects;

using Shouldly;

namespace DaklaPack.Shipments.UnitTests.Domain;

public sealed class TrackingIdTests
{
    [Theory]
    [InlineData("DP-2026-000123")]
    [InlineData("DP-1999-999999")]
    public void Accepts_a_well_formed_reference(string value) =>
        new TrackingId(value).Value.ShouldBe(value);

    [Fact]
    public void Trims_surrounding_whitespace() =>
        new TrackingId("  DP-2026-000123  ").Value.ShouldBe("DP-2026-000123");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("DP-2026-12345")]      // five digits
    [InlineData("DP-2026-1234567")]    // seven digits
    [InlineData("XX-2026-000123")]     // wrong prefix
    [InlineData("DP-26-000123")]       // two-digit year
    [InlineData("dp-2026-000123")]     // lower case
    [InlineData("DP-2026-00012A")]     // non-digit
    public void Rejects_anything_malformed(string value) =>
        Should.Throw<DomainException>(() => new TrackingId(value));

    [Fact]
    public void Compares_by_value_not_reference() =>
        new TrackingId("DP-2026-000123").ShouldBe(new TrackingId("DP-2026-000123"));
}
