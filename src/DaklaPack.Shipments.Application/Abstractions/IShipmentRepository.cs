using DaklaPack.Shipments.Domain;

namespace DaklaPack.Shipments.Application.Abstractions;

/// <summary>Reads shipments from wherever they are stored.</summary>
/// <remarks>
/// This port exists to invert the dependency, so the application layer compiles with no persistence
/// package in sight. It does not exist so the database can be mocked — that produces tests which
/// pass against a fake and fail against the real store.
/// </remarks>
public interface IShipmentRepository
{
    /// <summary>
    /// Returns one page plus the total match count. Implementations must apply a total ordering:
    /// the sort field is not unique, so the shipment id is appended as a tiebreaker, without which
    /// paging can repeat or drop rows between pages.
    /// </summary>
    Task<(IReadOnlyList<Shipment> Items, int TotalCount)> SearchAsync(
        ShipmentSearchCriteria criteria,
        CancellationToken cancellationToken);
}
