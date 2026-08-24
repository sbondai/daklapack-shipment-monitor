using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Shouldly;

namespace DaklaPack.Shipments.ContractTests;

/// <summary>
/// Pins the HTTP and JSON contract of <c>GET /api/v1/shipments</c>.
/// </summary>
/// <remarks>
/// The C# response types and the TypeScript interfaces in the Angular client are the same contract
/// written twice, on either side of a network boundary. Nothing in either compiler checks that they
/// still agree. These tests are what does.
/// </remarks>
public abstract class ShipmentsEndpointTests(ShipmentsApiFactory factory) : IClassFixture<ShipmentsApiFactory>
{
    private const string Endpoint = "/api/v1/shipments";

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    public sealed class PageEnvelope(ShipmentsApiFactory factory) : ShipmentsEndpointTests(factory)
    {
        [Fact]
        public async Task Default_request_returns_the_first_page_with_metadata()
        {
            var body = await GetJsonAsync(Endpoint);

            body.GetProperty("page").GetInt32().ShouldBe(1);
            body.GetProperty("pageSize").GetInt32().ShouldBe(25);
            body.GetProperty("totalCount").GetInt32().ShouldBe(40);
            body.GetProperty("totalPages").GetInt32().ShouldBe(2);
            body.GetProperty("items").GetArrayLength().ShouldBe(25);
        }

        [Fact]
        public async Task Oversized_page_size_is_clamped_and_the_effective_size_is_reported()
        {
            var body = await GetJsonAsync($"{Endpoint}?pageSize=5000");

            body.GetProperty("pageSize").GetInt32().ShouldBe(100);
            body.GetProperty("items").GetArrayLength().ShouldBe(40);
        }

        [Fact]
        public async Task A_page_beyond_the_end_is_empty_but_still_reports_the_total()
        {
            var body = await GetJsonAsync($"{Endpoint}?page=99");

            body.GetProperty("items").GetArrayLength().ShouldBe(0);
            body.GetProperty("totalCount").GetInt32().ShouldBe(40);
        }

        [Theory]
        [InlineData(2147483647, 100)]
        [InlineData(1000000000, 25)]
        public async Task An_extreme_page_does_not_wrap_round_to_the_first_page(int page, int pageSize)
        {
            // The regression this exists for: the offset overflowed int, went negative, and the
            // store treated it as zero - so page two billion served page one with 200 OK.
            var body = await GetJsonAsync($"{Endpoint}?page={page}&pageSize={pageSize}");

            body.GetProperty("items").GetArrayLength().ShouldBe(0);
            body.GetProperty("totalCount").GetInt32().ShouldBe(40);
        }

        [Fact]
        public async Task Pages_do_not_overlap()
        {
            var first = await GetJsonAsync($"{Endpoint}?page=1&pageSize=10");
            var second = await GetJsonAsync($"{Endpoint}?page=2&pageSize=10");

            var firstIds = first.GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("trackingId").GetString()).ToList();
            var secondIds = second.GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("trackingId").GetString()).ToList();

            firstIds.Intersect(secondIds).ShouldBeEmpty();
        }
    }

    public sealed class FilteringAndSorting(ShipmentsApiFactory factory) : ShipmentsEndpointTests(factory)
    {
        [Theory]
        [InlineData("Delayed")]
        [InlineData("delayed")]      // binding is case-insensitive
        [InlineData("DELAYED")]
        public async Task Filters_by_status_regardless_of_casing(string status)
        {
            var body = await GetJsonAsync($"{Endpoint}?status={status}&pageSize=100");

            body.GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("status").GetString())
                .ShouldAllBe(s => s == "Delayed");
        }

        [Theory]
        [InlineData("DispatchedAt")]
        [InlineData("EstimatedDeliveryOn")]
        [InlineData("Status")]
        [InlineData("Carrier")]
        [InlineData("TrackingId")]
        public async Task Accepts_every_allowlisted_sort_field(string sortBy) =>
            (await GetJsonAsync($"{Endpoint}?sortBy={sortBy}")).GetProperty("items").GetArrayLength().ShouldBe(25);

        [Fact]
        public async Task Sorting_descending_reverses_ascending()
        {
            var ascending = await GetJsonAsync($"{Endpoint}?sortBy=TrackingId&sortOrder=Asc&pageSize=100");
            var descending = await GetJsonAsync($"{Endpoint}?sortBy=TrackingId&sortOrder=Desc&pageSize=100");

            var up = ascending.GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("trackingId").GetString()).ToList();
            var down = descending.GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("trackingId").GetString()).ToList();

            up.ShouldBe(down.AsEnumerable().Reverse().ToList());
        }

        [Fact]
        public async Task Ordering_is_stable_across_identical_requests()
        {
            // Carrier repeats heavily in the sample. Without the id tiebreaker the order would be
            // whatever the store felt like that time, and paging would silently drop rows.
            var url = $"{Endpoint}?sortBy=Carrier&sortOrder=Asc&pageSize=100";

            var first = await GetJsonAsync(url);
            var second = await GetJsonAsync(url);

            first.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("trackingId").GetString())
                .ShouldBe(second.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("trackingId").GetString()));
        }
    }
}
