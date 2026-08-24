using DaklaPack.Shipments.Domain.Exceptions;
using DaklaPack.Shipments.Domain.ValueObjects;

using Shouldly;

namespace DaklaPack.Shipments.UnitTests.Domain;

public sealed class DestinationTests
{
    [Fact]
    public void Normalises_the_country_code_to_upper_case() =>
        new Destination("Amsterdam", "nl", "1011AB").CountryCode.ShouldBe("NL");

    [Fact]
    public void Trims_each_part()
    {
        var destination = new Destination("  Amsterdam ", " NL ", " 1011AB ");

        destination.City.ShouldBe("Amsterdam");
        destination.CountryCode.ShouldBe("NL");
        destination.PostalCode.ShouldBe("1011AB");
    }

    [Theory]
    [InlineData("", "NL", "1011AB")]        // blank city
    [InlineData("Amsterdam", "NL", "")]     // blank postal code
    [InlineData("Amsterdam", "NLD", "1011AB")]  // three-letter country
    [InlineData("Amsterdam", "N", "1011AB")]    // one-letter country
    [InlineData("Amsterdam", "N1", "1011AB")]   // digit in country
    [InlineData("Amsterdam", "", "1011AB")]     // blank country
    public void Rejects_incomplete_or_malformed_input(string city, string country, string postalCode) =>
        Should.Throw<DomainException>(() => new Destination(city, country, postalCode));
}
