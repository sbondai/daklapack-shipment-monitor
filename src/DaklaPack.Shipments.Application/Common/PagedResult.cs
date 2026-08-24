namespace DaklaPack.Shipments.Application.Common;

/// <summary>
/// One page of results, with the metadata a client needs to render a paginator.
/// </summary>
/// <remarks>
/// <see cref="PageSize"/> is the size actually applied after clamping, which may be smaller than
/// the one requested — a client binding a paginator needs the number that was used, not the one it
/// asked for.
/// </remarks>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
