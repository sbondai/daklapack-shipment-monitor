namespace DaklaPack.Shipments.Domain.Exceptions;

/// <summary>
/// A domain invariant was violated. Signals bad data or a defect, not a bad request: domain objects
/// are built from trusted sources, so the API surfaces this as a 500 rather than a 400.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
