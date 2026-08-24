using DaklaPack.Shipments.Application.Abstractions;
using DaklaPack.Shipments.Application.Shipments;
using DaklaPack.Shipments.Domain;
using DaklaPack.Shipments.Infrastructure.Shipments;
using DaklaPack.Shipments.UnitTests.TestSupport;
using Shouldly;
using ShouldlyOrder = Shouldly.SortDirection;
// Both Shouldly and the application define a SortDirection; name them apart rather than qualifying
// every use site.
using SortDirection = DaklaPack.Shipments.Application.Shipments.SortDirection;

namespace DaklaPack.Shipments.UnitTests.Infrastructure;

public sealed class InMemoryShipmentRepositoryTests
{
    private static readonly DateOnly Today = new(2026, 8, 22);

    // Constructed directly: the adapter is internal on purpose, and InternalsVisibleTo lets the
    // tests reach it without widening the production surface.
    //
    // Deliberately typed as the port rather than the concrete class. These assertions describe the
    // contract every implementation must honour, so they are written against the interface and can
    // be pointed at a database-backed adapter unchanged. CA1859's performance advice does not apply
    // to a test helper, and following it would defeat the point.
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1859:Use concrete types when possible",
        Justification = "These are port contract tests; the abstraction is the subject under test.")]
    private static IShipmentRepository Repository() =>
        new InMemoryShipmentRepository(FixedTimeProvider.On(2026, 8, 22));

    private static ShipmentSearchCriteria Criteria(
        ShipmentStatus? status = null,
        ShipmentSortField sortBy = ShipmentSortField.DispatchedAt,
        SortDirection order = SortDirection.Desc,
        int skip = 0,
        int take = 100) => new(status, sortBy, order, skip, take);

    [Fact]
    public async Task Returns_the_whole_sample_set_by_default()
    {
        var (items, total) = await Repository().SearchAsync(Criteria(), TestContext.Current.CancellationToken);

        total.ShouldBe(40);
        items.Count.ShouldBe(40);
    }

    [Fact]
    public async Task Seed_data_is_deterministic_across_instances()
    {
        var first = await Repository().SearchAsync(Criteria(), TestContext.Current.CancellationToken);
        var second = await Repository().SearchAsync(Criteria(), TestContext.Current.CancellationToken);

        first.Items.Select(s => s.Id).ShouldBe(second.Items.Select(s => s.Id));
        first.Items.Select(s => s.TrackingId.Value).ShouldBe(second.Items.Select(s => s.TrackingId.Value));
    }

    [Fact]
    public async Task Contains_a_realistic_mix_of_states()
    {
        var (items, _) = await Repository().SearchAsync(Criteria(), TestContext.Current.CancellationToken);

        // A monitoring view is uninteresting if everything is in one state.
        items.Select(s => s.Status).Distinct().Count().ShouldBeGreaterThanOrEqualTo(5);
        items.ShouldContain(s => s.IsOverdue(Today));
        items.ShouldContain(s => !s.IsOverdue(Today));
    }

    public sealed class Filtering
    {
        [Theory]
        [InlineData(ShipmentStatus.Delivered)]
        [InlineData(ShipmentStatus.InTransit)]
        [InlineData(ShipmentStatus.Delayed)]
        [InlineData(ShipmentStatus.Created)]
        public async Task Returns_only_the_requested_status(ShipmentStatus status)
        {
            var (items, total) = await Repository()
                .SearchAsync(Criteria(status: status), TestContext.Current.CancellationToken);

            items.ShouldNotBeEmpty();
            items.ShouldAllBe(s => s.Status == status);
            total.ShouldBe(items.Count);
        }

        [Fact]
        public async Task Total_count_reflects_the_filter_not_the_whole_set()
        {
            var (_, filtered) = await Repository()
                .SearchAsync(Criteria(status: ShipmentStatus.Cancelled), TestContext.Current.CancellationToken);
            var (_, everything) = await Repository()
                .SearchAsync(Criteria(), TestContext.Current.CancellationToken);

            filtered.ShouldBeLessThan(everything);
        }
    }

    public sealed class Ordering
    {
        [Fact]
        public async Task Newest_first_when_descending_by_dispatch()
        {
            var (items, _) = await Repository().SearchAsync(
                Criteria(sortBy: ShipmentSortField.DispatchedAt, order: SortDirection.Desc),
                TestContext.Current.CancellationToken);

            items.Select(s => s.DispatchedAt).ShouldBeInOrder(ShouldlyOrder.Descending);
        }

        [Fact]
        public async Task Oldest_first_when_ascending_by_dispatch()
        {
            var (items, _) = await Repository().SearchAsync(
                Criteria(sortBy: ShipmentSortField.DispatchedAt, order: SortDirection.Asc),
                TestContext.Current.CancellationToken);

            items.Select(s => s.DispatchedAt).ShouldBeInOrder(ShouldlyOrder.Ascending);
        }

        [Fact]
        public async Task Carrier_sort_is_total_despite_many_ties()
        {
            // Carrier repeats heavily in the sample, so without the id tiebreaker this ordering
            // would not be deterministic. Two identical queries must return identical sequences.
            var criteria = Criteria(sortBy: ShipmentSortField.Carrier, order: SortDirection.Asc);

            var first = await Repository().SearchAsync(criteria, TestContext.Current.CancellationToken);
            var second = await Repository().SearchAsync(criteria, TestContext.Current.CancellationToken);

            first.Items.Select(s => s.Id).ShouldBe(second.Items.Select(s => s.Id));
        }

        [Fact]
        public async Task Ties_are_broken_by_id_descending()
        {
            var (items, _) = await Repository().SearchAsync(
                Criteria(sortBy: ShipmentSortField.Status, order: SortDirection.Asc),
                TestContext.Current.CancellationToken);

            foreach (var group in items.GroupBy(s => s.Status))
            {
                group.Select(s => s.Id).ShouldBeInOrder(ShouldlyOrder.Descending);
            }
        }
    }

    public sealed class Paging
    {
        [Fact]
        public async Task Pages_do_not_overlap_and_cover_the_set()
        {
            var repository = Repository();
            var seen = new List<Guid>();

            for (var skip = 0; skip < 40; skip += 10)
            {
                var (page, _) = await repository.SearchAsync(
                    Criteria(skip: skip, take: 10), TestContext.Current.CancellationToken);
                seen.AddRange(page.Select(s => s.Id));
            }

            seen.Count.ShouldBe(40);
            seen.Distinct().Count().ShouldBe(40);
        }

        [Fact]
        public async Task A_page_beyond_the_end_is_empty_but_still_reports_the_total()
        {
            var (items, total) = await Repository()
                .SearchAsync(Criteria(skip: 500, take: 25), TestContext.Current.CancellationToken);

            items.ShouldBeEmpty();
            total.ShouldBe(40);
        }

        [Fact]
        public async Task A_partial_final_page_returns_what_remains()
        {
            var (items, _) = await Repository()
                .SearchAsync(Criteria(skip: 35, take: 25), TestContext.Current.CancellationToken);

            items.Count.ShouldBe(5);
        }
    }

    [Fact]
    public async Task Honours_cancellation()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            Repository().SearchAsync(Criteria(), cancelled.Token));
    }
}
