using DaklaPack.Shipments.Application.Shipments;
using DaklaPack.Shipments.Application.Shipments.GetShipments;
using DaklaPack.Shipments.Domain;
using DaklaPack.Shipments.UnitTests.TestSupport;

using Shouldly;

// Shouldly also defines a SortDirection; ours is the one meant here.
using SortDirection = DaklaPack.Shipments.Application.Shipments.SortDirection;

namespace DaklaPack.Shipments.UnitTests.Application;

public sealed class GetShipmentsHandlerTests
{
    // Daklapack operates from the Netherlands, so the business calendar is Amsterdam's.
    private static readonly TimeZoneInfo Amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    private static GetShipmentsHandler HandlerFor(
        RecordingShipmentRepository repository,
        int year = 2026,
        int month = 8,
        int day = 22) =>
        new(repository, new ShipmentMapper(), FixedTimeProvider.On(year, month, day), Amsterdam);

    private static GetShipmentsHandler HandlerAt(
        RecordingShipmentRepository repository,
        DateTimeOffset utcNow) =>
        new(repository, new ShipmentMapper(), new FixedTimeProvider(utcNow), Amsterdam);

    public sealed class PageArithmetic
    {
        [Theory]
        [InlineData(1, 25, 0)]    // first page starts at the beginning
        [InlineData(2, 25, 25)]
        [InlineData(3, 10, 20)]
        [InlineData(7, 100, 600)]
        public async Task Converts_a_one_based_page_into_a_zero_based_offset(
            int page, int pageSize, int expectedSkip)
        {
            var repository = new RecordingShipmentRepository();

            await HandlerFor(repository).HandleAsync(
                new GetShipmentsQuery(Page: page, PageSize: pageSize), TestContext.Current.CancellationToken);

            repository.LastCriteria!.Skip.ShouldBe(expectedSkip);
            repository.LastCriteria.Take.ShouldBe(pageSize);
        }

        [Fact]
        public async Task Clamps_an_oversized_page_size_rather_than_rejecting_it()
        {
            var repository = new RecordingShipmentRepository();

            var result = await HandlerFor(repository).HandleAsync(
                new GetShipmentsQuery(PageSize: 5_000), TestContext.Current.CancellationToken);

            repository.LastCriteria!.Take.ShouldBe(GetShipmentsQuery.MaxPageSize);
            result.PageSize.ShouldBe(GetShipmentsQuery.MaxPageSize);
        }

        [Fact]
        public async Task Clamping_also_narrows_the_offset_so_paging_stays_consistent()
        {
            // Page 3 at a clamped size of 100 must skip 200, not 3 x the requested 5000.
            var repository = new RecordingShipmentRepository();

            await HandlerFor(repository).HandleAsync(
                new GetShipmentsQuery(Page: 3, PageSize: 5_000), TestContext.Current.CancellationToken);

            repository.LastCriteria!.Skip.ShouldBe(200);
        }

        [Fact]
        public async Task Reports_the_effective_page_size_not_the_requested_one()
        {
            var result = await HandlerFor(new RecordingShipmentRepository()).HandleAsync(
                new GetShipmentsQuery(PageSize: 250), TestContext.Current.CancellationToken);

            result.PageSize.ShouldNotBe(250);
            result.PageSize.ShouldBe(GetShipmentsQuery.MaxPageSize);
        }

        [Theory]
        [InlineData(2_147_483_647, 100)]   // int.MaxValue: (page-1)*size overflows int
        [InlineData(2_147_483_000, 100)]
        [InlineData(1_000_000_000, 25)]
        public async Task An_extreme_page_never_wraps_round_to_an_earlier_offset(int page, int pageSize)
        {
            // Before this was widened to long, (page - 1) * pageSize overflowed, went negative, and
            // LINQ's Skip treated it as zero - so page two billion quietly served page one's rows.
            var repository = new RecordingShipmentRepository();

            await HandlerFor(repository).HandleAsync(
                new GetShipmentsQuery(Page: page, PageSize: pageSize), TestContext.Current.CancellationToken);

            repository.LastCriteria!.Skip.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task An_extreme_page_returns_an_empty_page_rather_than_page_one()
        {
            var repository = new RecordingShipmentRepository(items: [], totalCount: 40);

            var result = await HandlerFor(repository).HandleAsync(
                new GetShipmentsQuery(Page: int.MaxValue, PageSize: 100),
                TestContext.Current.CancellationToken);

            result.Items.ShouldBeEmpty();
            result.TotalCount.ShouldBe(40);
        }

        [Theory]
        [InlineData(0, 25, 0)]     // no results at all
        [InlineData(1, 25, 1)]
        [InlineData(25, 25, 1)]    // exactly one full page
        [InlineData(26, 25, 2)]    // one over
        [InlineData(137, 25, 6)]
        public async Task Computes_total_pages_from_the_effective_size(
            int totalCount, int pageSize, int expectedPages)
        {
            var result = await HandlerFor(new RecordingShipmentRepository(totalCount: totalCount)).HandleAsync(
                new GetShipmentsQuery(PageSize: pageSize), TestContext.Current.CancellationToken);

            result.TotalPages.ShouldBe(expectedPages);
        }
    }

    public sealed class Defaults
    {
        [Fact]
        public async Task Newest_first_when_nothing_is_asked_for()
        {
            var repository = new RecordingShipmentRepository();

            await HandlerFor(repository).HandleAsync(new GetShipmentsQuery(), TestContext.Current.CancellationToken);

            repository.LastCriteria!.SortBy.ShouldBe(ShipmentSortField.DispatchedAt);
            repository.LastCriteria.SortOrder.ShouldBe(SortDirection.Desc);
            repository.LastCriteria.Skip.ShouldBe(0);
            repository.LastCriteria.Take.ShouldBe(GetShipmentsQuery.DefaultPageSize);
            repository.LastCriteria.Status.ShouldBeNull();
        }
    }

    public sealed class Behaviour
    {
        [Fact]
        public async Task Passes_the_status_filter_straight_through()
        {
            var repository = new RecordingShipmentRepository();

            await HandlerFor(repository).HandleAsync(
                new GetShipmentsQuery(Status: ShipmentStatus.Delayed), TestContext.Current.CancellationToken);

            repository.LastCriteria!.Status.ShouldBe(ShipmentStatus.Delayed);
        }

        [Fact]
        public async Task A_page_beyond_the_last_is_empty_rather_than_an_error()
        {
            var result = await HandlerFor(new RecordingShipmentRepository(items: [], totalCount: 12))
                .HandleAsync(new GetShipmentsQuery(Page: 99), TestContext.Current.CancellationToken);

            result.Items.ShouldBeEmpty();
            result.TotalCount.ShouldBe(12);
            result.Page.ShouldBe(99);
        }

        [Fact]
        public async Task Derives_overdue_against_the_injected_clock_not_the_wall_clock()
        {
            var overdueOnTheTwentySecond = ShipmentBuilder.A()
                .DueOn(2026, 8, 5)
                .WithStatus(ShipmentStatus.InTransit)
                .Build();
            var repository = new RecordingShipmentRepository([overdueOnTheTwentySecond]);

            var before = await HandlerFor(repository, day: 4).HandleAsync(
                new GetShipmentsQuery(), TestContext.Current.CancellationToken);
            var after = await HandlerFor(repository, day: 22).HandleAsync(
                new GetShipmentsQuery(), TestContext.Current.CancellationToken);

            before.Items[0].IsOverdue.ShouldBeFalse();
            after.Items[0].IsOverdue.ShouldBeTrue();
        }

        [Fact]
        public async Task Overdue_is_judged_in_the_business_time_zone_not_utc()
        {
            // 22:30 UTC on 5 August is already 00:30 on 6 August in Amsterdam (UTC+2 in summer).
            // A shipment due on the 5th is therefore overdue for the operators watching it, even
            // though a UTC clock still reads the 5th.
            var dueOnTheFifth = ShipmentBuilder.A()
                .DueOn(2026, 8, 5)
                .WithStatus(ShipmentStatus.InTransit)
                .Build();
            var repository = new RecordingShipmentRepository([dueOnTheFifth]);

            var result = await HandlerAt(repository, new DateTimeOffset(2026, 8, 5, 22, 30, 0, TimeSpan.Zero))
                .HandleAsync(new GetShipmentsQuery(), TestContext.Current.CancellationToken);

            result.Items[0].IsOverdue.ShouldBeTrue();
        }

        [Fact]
        public async Task Is_not_overdue_earlier_in_the_same_business_day()
        {
            var dueOnTheFifth = ShipmentBuilder.A()
                .DueOn(2026, 8, 5)
                .WithStatus(ShipmentStatus.InTransit)
                .Build();
            var repository = new RecordingShipmentRepository([dueOnTheFifth]);

            var result = await HandlerAt(repository, new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero))
                .HandleAsync(new GetShipmentsQuery(), TestContext.Current.CancellationToken);

            result.Items[0].IsOverdue.ShouldBeFalse();
        }

        [Fact]
        public async Task Honours_cancellation()
        {
            using var cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(() =>
                HandlerFor(new RecordingShipmentRepository())
                    .HandleAsync(new GetShipmentsQuery(), cancelled.Token));
        }

        [Fact]
        public async Task Rejects_a_null_query() =>
            await Should.ThrowAsync<ArgumentNullException>(() =>
                HandlerFor(new RecordingShipmentRepository())
                    .HandleAsync(null!, TestContext.Current.CancellationToken));
    }
}
