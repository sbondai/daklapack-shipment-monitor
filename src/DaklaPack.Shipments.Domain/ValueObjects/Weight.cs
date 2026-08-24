using DaklaPack.Shipments.Domain.Exceptions;

namespace DaklaPack.Shipments.Domain.ValueObjects;

/// <summary>A shipment's gross weight in kilograms.</summary>
/// <remarks>
/// The unit is part of the type, not a naming convention on a loose <see cref="decimal"/>. That is
/// the whole point: a weight cannot be silently mixed with a count, a price, or a pound.
/// </remarks>
public sealed record Weight
{
    /// <summary>The largest weight a single shipment may carry, in kilograms.</summary>
    public const decimal MaxKilograms = 1_000m;

    /// <summary>The weight in kilograms. Always greater than zero.</summary>
    public decimal Kilograms { get; }

    /// <summary>Creates a weight, rejecting non-positive and implausible values.</summary>
    /// <exception cref="DomainException">The value is zero, negative, or above <see cref="MaxKilograms"/>.</exception>
    public Weight(decimal kilograms)
    {
        if (kilograms <= 0m)
        {
            throw new DomainException($"Weight must be greater than zero, but was {kilograms} kg.");
        }

        if (kilograms > MaxKilograms)
        {
            throw new DomainException($"Weight must not exceed {MaxKilograms} kg, but was {kilograms} kg.");
        }

        Kilograms = kilograms;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Kilograms:0.###} kg";
}
