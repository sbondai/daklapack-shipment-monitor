using DaklaPack.Shipments.Application.Abstractions;
using DaklaPack.Shipments.Domain;

namespace DaklaPack.Shipments.UnitTests.TestSupport;

/// <summary>
/// A hand-written stub that records the criteria it was asked for, so tests can assert on the
/// translation the handler performs rather than on a mocking framework's call log.
/// </summary>
internal sealed class RecordingShipmentRepository(
    IReadOnlyList<Shipment>? items = null,
    int? totalCount = null) : IShipmentRepository
{
    private readonly IReadOnlyList<Shipment> _items = items ?? [];

    public ShipmentSearchCriteria? LastCriteria { get; private set; }

    public Task<(IReadOnlyList<Shipment> Items, int TotalCount)> SearchAsync(
        ShipmentSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastCriteria = criteria;
        return Task.FromResult((_items, totalCount ?? _items.Count));
    }
}
