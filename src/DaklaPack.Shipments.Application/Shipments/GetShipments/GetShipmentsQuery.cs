using System.ComponentModel.DataAnnotations;

using DaklaPack.Shipments.Domain;

namespace DaklaPack.Shipments.Application.Shipments.GetShipments;

/// <summary>
/// The request for a page of shipments, bound from the query string.
/// </summary>
/// <remarks>
/// Annotations reject what is <em>malformed</em> (a page below one); the handler <em>clamps</em>
/// what is merely excessive (an oversized page size), because a monitoring UI asking for too many
/// rows should get the maximum rather than an error.
///
/// The attributes sit on the constructor parameters, not the generated properties: MVC throws at
/// request time if a positional record carries validation metadata on its properties, so a
/// <c>[property:]</c> target compiles cleanly and then fails on every call.
/// </remarks>
public sealed record GetShipmentsQuery(
    ShipmentStatus? Status = null,
    ShipmentSortField SortBy = ShipmentSortField.DispatchedAt,
    SortDirection SortOrder = SortDirection.Desc,
    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    int Page = 1,
    [Range(1, int.MaxValue, ErrorMessage = "Page size must be 1 or greater.")]
    int PageSize = GetShipmentsQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 25;

    public const int MaxPageSize = 100;
}
