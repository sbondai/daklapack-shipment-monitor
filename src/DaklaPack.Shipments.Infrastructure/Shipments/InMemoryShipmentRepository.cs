using DaklaPack.Shipments.Application.Abstractions;
using DaklaPack.Shipments.Application.Shipments;
using DaklaPack.Shipments.Domain;

namespace DaklaPack.Shipments.Infrastructure.Shipments;

/// <summary>
/// Serves shipments from a fixed in-memory sample set.
/// </summary>
/// <remarks>
/// Not a test double: this is the adapter the running application uses, so the solution starts with
/// no database and no connection string. Replacing it with a database-backed one leaves the
/// application layer untouched, which is the evidence for the port.
/// </remarks>
internal sealed class InMemoryShipmentRepository(TimeProvider timeProvider) : IShipmentRepository
{
    private readonly Lazy<IReadOnlyList<Shipment>> _shipments = new(() =>
        ShipmentSeedData.Create(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)));

    public Task<(IReadOnlyList<Shipment> Items, int TotalCount)> SearchAsync(
        ShipmentSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        cancellationToken.ThrowIfCancellationRequested();

        var matching = criteria.Status is { } status
            ? _shipments.Value.Where(shipment => shipment.Status == status).ToList()
            : _shipments.Value;

        var page = Sort(matching, criteria)
            .Skip(criteria.Skip)
            .Take(criteria.Take)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Shipment>)page, matching.Count));
    }

    // The id tiebreaker is not decoration: every sortable field here is non-unique, and paging over
    // a non-total ordering lets rows repeat or disappear between pages.
    private static IEnumerable<Shipment> Sort(IEnumerable<Shipment> shipments, ShipmentSearchCriteria criteria)
    {
        var descending = criteria.SortOrder == SortDirection.Desc;

        IOrderedEnumerable<Shipment> ordered = criteria.SortBy switch
        {
            ShipmentSortField.DispatchedAt => By(shipments, s => s.DispatchedAt, descending),
            ShipmentSortField.EstimatedDeliveryOn => By(shipments, s => s.EstimatedDeliveryOn, descending),
            ShipmentSortField.Status => By(shipments, s => s.Status, descending),
            ShipmentSortField.Carrier => By(shipments, s => s.Carrier, descending),
            ShipmentSortField.TrackingId => By(shipments, s => s.TrackingId.Value, descending),

            _ => throw new ArgumentOutOfRangeException(
                nameof(criteria), criteria.SortBy, "Unhandled sort field.")
        };

        return ordered.ThenByDescending(shipment => shipment.Id);
    }

    private static IOrderedEnumerable<Shipment> By<TKey>(
        IEnumerable<Shipment> shipments, Func<Shipment, TKey> key, bool descending) =>
        descending ? shipments.OrderByDescending(key) : shipments.OrderBy(key);
}
