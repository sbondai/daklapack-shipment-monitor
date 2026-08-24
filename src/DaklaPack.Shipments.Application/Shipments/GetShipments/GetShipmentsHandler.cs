using DaklaPack.Shipments.Application.Abstractions;
using DaklaPack.Shipments.Application.Common;

namespace DaklaPack.Shipments.Application.Shipments.GetShipments;

/// <summary>
/// Serves one page of shipments for the operations monitoring view.
/// </summary>
/// <remarks>
/// A plain class, not a mediator request: for one use case that indirection makes call sites harder
/// to navigate in exchange for a pipeline that is not needed. Validation is handled by the framework
/// and exceptions by a global handler, which are the two concerns a pipeline would have carried.
/// </remarks>
public sealed class GetShipmentsHandler(
    IShipmentRepository repository,
    ShipmentMapper mapper,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone)
{
    public async Task<PagedResult<ShipmentResponse>> HandleAsync(
        GetShipmentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageSize = Math.Min(query.PageSize, GetShipmentsQuery.MaxPageSize);

        // Widened to long deliberately. Page is only bounded below, so (page - 1) * pageSize
        // overflows int for large pages, wraps negative, and Skip then treats it as zero — which
        // silently served page one's rows for page two billion. Clamping keeps the existing
        // contract: a page past the end returns empty with the true total.
        var offset = (long)(query.Page - 1) * pageSize;
        var skip = (int)Math.Min(offset, int.MaxValue);

        var criteria = new ShipmentSearchCriteria(
            query.Status,
            query.SortBy,
            query.SortOrder,
            Skip: skip,
            Take: pageSize);

        var (shipments, totalCount) = await repository.SearchAsync(criteria, cancellationToken);

        return new PagedResult<ShipmentResponse>(
            mapper.ToResponses(shipments, BusinessToday()),
            query.Page,
            pageSize,
            totalCount);
    }

    // Not UTC. UTC changes date at 02:00 Amsterdam in summer, so for the first two hours of the
    // local day a shipment that became overdue at local midnight would still read as on time.
    private DateOnly BusinessToday() =>
        DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), businessTimeZone).DateTime);
}
