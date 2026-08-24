using DaklaPack.Shipments.Domain.Exceptions;
using DaklaPack.Shipments.Domain.ValueObjects;

using Shouldly;

namespace DaklaPack.Shipments.UnitTests.Domain;

public sealed class WeightTests
{
    [Theory]
    [InlineData(0.001)]
    [InlineData(12.5)]
    [InlineData(1000)]     // exactly the maximum
    public void Accepts_a_positive_weight_up_to_the_maximum(decimal kilograms) =>
        new Weight(kilograms).Kilograms.ShouldBe(kilograms);

    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    [InlineData(-50)]
    public void Rejects_zero_and_negative(decimal kilograms) =>
        Should.Throw<DomainException>(() => new Weight(kilograms));

    [Fact]
    public void Rejects_above_the_maximum() =>
        Should.Throw<DomainException>(() => new Weight(Weight.MaxKilograms + 0.001m));
}
