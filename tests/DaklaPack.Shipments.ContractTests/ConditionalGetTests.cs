using System.Net;
using Microsoft.Net.Http.Headers;

using Shouldly;

namespace DaklaPack.Shipments.ContractTests;

/// <summary>
/// Pins the conditional-request behaviour the polling client depends on.
/// </summary>
/// <remarks>
/// The optimisation is only worth having if it is exactly right: a 304 for a page the client does
/// not actually hold would show stale shipments, which is worse than never optimising at all.
/// </remarks>
public sealed class ConditionalGetTests(ShipmentsApiFactory factory) : IClassFixture<ShipmentsApiFactory>
{
    private const string Endpoint = "/api/v1/shipments";

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string? ifNoneMatch = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Relative));
        if (ifNoneMatch is not null)
        {
            request.Headers.TryAddWithoutValidation(HeaderNames.IfNoneMatch, ifNoneMatch);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_successful_read_carries_an_entity_tag()
    {
        using var client = factory.CreateClient();

        using var response = await GetAsync(client, Endpoint);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
    }

    [Fact]
    public async Task The_same_request_yields_the_same_tag()
    {
        // The tag has to be stable across requests or every poll is a full download and the
        // optimisation silently does nothing.
        using var client = factory.CreateClient();

        using var first = await GetAsync(client, Endpoint);
        using var second = await GetAsync(client, Endpoint);

        second.Headers.ETag!.Tag.ShouldBe(first.Headers.ETag!.Tag);
    }

    [Fact]
    public async Task Re_requesting_with_the_tag_returns_not_modified_and_no_body()
    {
        using var client = factory.CreateClient();
        using var first = await GetAsync(client, Endpoint);

        using var second = await GetAsync(client, Endpoint, first.Headers.ETag!.Tag);

        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("?page=2")]
    [InlineData("?pageSize=10")]
    [InlineData("?status=Delayed")]
    [InlineData("?sortBy=Carrier")]
    [InlineData("?sortOrder=Asc")]
    public async Task A_different_query_is_a_different_page_and_must_not_be_short_circuited(string query)
    {
        // The failure that would matter: serving 304 against a tag from another query would show
        // the operator a page they never asked for. Every parameter that changes the response has
        // to change the tag.
        using var client = factory.CreateClient();
        using var first = await GetAsync(client, Endpoint);

        using var other = await GetAsync(client, $"{Endpoint}{query}", first.Headers.ETag!.Tag);

        other.StatusCode.ShouldBe(HttpStatusCode.OK);
        other.Headers.ETag!.Tag.ShouldNotBe(first.Headers.ETag!.Tag);
    }

    [Fact]
    public async Task A_list_of_tags_matches_if_any_entry_does()
    {
        // If-None-Match is a comma-separated list (RFC 9110 s13.1.2). Comparing the raw header as
        // one string fails open: the client re-downloads every poll and the optimisation silently
        // does nothing.
        using var client = factory.CreateClient();
        using var first = await GetAsync(client, Endpoint);

        using var second = await GetAsync(
            client, Endpoint, $"\"aaaaaaaa\", {first.Headers.ETag!.Tag}, \"bbbbbbbb\"");

        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task A_wildcard_matches_any_existing_representation()
    {
        using var client = factory.CreateClient();

        using var response = await GetAsync(client, Endpoint, "*");

        response.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Matching_is_weak_so_the_strength_prefix_does_not_defeat_it()
    {
        // If-None-Match uses the weak comparison function, so W/"x" and "x" are a match. Comparing
        // strongly would silently disable caching for any intermediary that re-tags weakly.
        using var client = factory.CreateClient();
        using var first = await GetAsync(client, Endpoint);

        var withoutWeakPrefix = first.Headers.ETag!.Tag;
        using var second = await GetAsync(client, Endpoint, withoutWeakPrefix);
        using var third = await GetAsync(client, Endpoint, $"W/{withoutWeakPrefix}");

        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        third.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task A_malformed_header_is_ignored_rather_than_matched()
    {
        using var client = factory.CreateClient();

        using var response = await GetAsync(client, Endpoint, "not a valid tag at all");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/health")]
    public async Task Endpoints_outside_the_shipment_routes_are_not_buffered_or_tagged(string path)
    {
        // The middleware buffers a response to hash it. Applying that to everything would tax the
        // OpenAPI document and the docs UI for no benefit, and would break anything streamed.
        using var client = factory.CreateClient();

        using var response = await GetAsync(client, path);

        response.Headers.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task An_unknown_tag_is_ignored_rather_than_trusted()
    {
        using var client = factory.CreateClient();

        using var response = await GetAsync(client, Endpoint, "\"not-a-tag-we-issued\"");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_validation_failure_is_never_answered_with_not_modified()
    {
        // 304 is only ever correct for a cacheable success. A conditional header on a bad request
        // must not suppress the problem document that explains what is wrong.
        using var client = factory.CreateClient();
        using var ok = await GetAsync(client, Endpoint);

        using var bad = await GetAsync(client, $"{Endpoint}?page=0", ok.Headers.ETag!.Tag);

        bad.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        bad.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
