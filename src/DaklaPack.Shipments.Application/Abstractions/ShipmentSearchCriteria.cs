using DaklaPack.Shipments.Application.Shipments;
using DaklaPack.Shipments.Domain;

namespace DaklaPack.Shipments.Application.Abstractions;

/// <summary>
/// What a repository needs to fetch one page.
/// </summary>
/// <remarks>
/// Distinct from the API request on purpose: that speaks in pages, a store speaks in offsets. The
/// handler owns the translation, so page arithmetic lives in one place instead of every adapter.
/// </remarks>
public sealed record ShipmentSearchCriteria(
    ShipmentStatus? Status,
    ShipmentSortField SortBy,
    SortDirection SortOrder,
    int Skip,
    int Take);
