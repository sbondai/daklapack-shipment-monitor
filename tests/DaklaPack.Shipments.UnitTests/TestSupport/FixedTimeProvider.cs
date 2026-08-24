namespace DaklaPack.Shipments.UnitTests.TestSupport;

/// <summary>A <see cref="TimeProvider"/> pinned to one instant, so date-dependent behaviour is testable.</summary>
/// <remarks>
/// Five lines is cheaper than another package reference, and it makes the point that
/// <see cref="TimeProvider"/> is already the abstraction — no bespoke clock interface required.
/// </remarks>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static FixedTimeProvider On(int year, int month, int day) =>
        new(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => now;
}
