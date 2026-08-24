using DaklaPack.Shipments.Domain.Exceptions;

namespace DaklaPack.Shipments.Domain.ValueObjects;

/// <summary>Where a shipment is going.</summary>
/// <remarks>
/// Kept as one value object rather than three loose strings on the entity, because the three parts
/// are only meaningful together: a postal code without a country does not identify anywhere.
/// </remarks>
public sealed record Destination
{
    /// <summary>The destination city or town.</summary>
    public string City { get; }

    /// <summary>ISO 3166-1 alpha-2 country code, upper case.</summary>
    public string CountryCode { get; }

    /// <summary>The destination postal code, as written locally.</summary>
    public string PostalCode { get; }

    /// <summary>Creates a destination, rejecting blank parts and malformed country codes.</summary>
    /// <exception cref="DomainException">Any part is blank, or the country code is not two letters.</exception>
    public Destination(string city, string countryCode, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("Destination city must not be blank.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainException("Destination postal code must not be blank.");
        }

        var country = (countryCode ?? string.Empty).Trim().ToUpperInvariant();

        if (country.Length != 2 || !country.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException(
                $"Country code '{countryCode}' must be an ISO 3166-1 alpha-2 code, e.g. NL.");
        }

        City = city.Trim();
        CountryCode = country;
        PostalCode = postalCode.Trim();
    }

    /// <inheritdoc />
    public override string ToString() => $"{PostalCode} {City}, {CountryCode}";
}
