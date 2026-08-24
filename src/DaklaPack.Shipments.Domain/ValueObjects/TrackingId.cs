using System.Text.RegularExpressions;

using DaklaPack.Shipments.Domain.Exceptions;

namespace DaklaPack.Shipments.Domain.ValueObjects;

/// <summary>
/// A DaklaPack tracking reference, in the form <c>DP-{year}-{sequence}</c>, e.g. DP-2026-000123.
/// </summary>
/// <remarks>
/// Modelled as a value object rather than a <see cref="string"/> so that a malformed reference
/// cannot be constructed at all. Validating at the boundary and then passing a bare string around
/// leaves every later consumer wondering whether it was checked.
/// </remarks>
public sealed partial record TrackingId
{
    private const string Format = "DP-{year}-{6 digits}";

    /// <summary>The canonical string form.</summary>
    public string Value { get; }

    /// <summary>Creates a tracking id, rejecting anything that does not match the required format.</summary>
    /// <exception cref="DomainException">The value is blank or malformed.</exception>
    public TrackingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Tracking id must not be blank.");
        }

        var trimmed = value.Trim();

        if (!Pattern().IsMatch(trimmed))
        {
            throw new DomainException($"Tracking id '{trimmed}' is not of the form {Format}.");
        }

        Value = trimmed;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex(@"^DP-\d{4}-\d{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
