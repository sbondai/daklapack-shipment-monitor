using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DaklaPack.Shipments.Application.Abstractions;
using DaklaPack.Shipments.Application.Common;
using DaklaPack.Shipments.Domain;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace DaklaPack.Shipments.ContractTests;

/// <summary>Pins the failure contract: what a client receives when something is wrong.</summary>
public sealed class ErrorContractTests(ShipmentsApiFactory factory) : IClassFixture<ShipmentsApiFactory>
{
    private const string Endpoint = "/api/v1/shipments";

    private async Task<(HttpStatusCode Status, string? ContentType, JsonElement Body)> GetAsync(string url)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return (response.StatusCode, response.Content.Headers.ContentType?.MediaType, body);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=-5")]
    public async Task Malformed_paging_is_rejected_as_a_validation_problem(string query)
    {
        var (status, contentType, body) = await GetAsync($"{Endpoint}?{query}");

        status.ShouldBe(HttpStatusCode.BadRequest);
        contentType.ShouldBe("application/problem+json");
        body.TryGetProperty("errors", out var errors).ShouldBeTrue();
        errors.EnumerateObject().ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("status=NotAStatus")]
    [InlineData("sortBy=NotAField")]
    [InlineData("sortOrder=Sideways")]
    public async Task Values_outside_the_allowlist_are_rejected(string query)
    {
        // sortBy never reaches a query builder as free text; an unknown value fails at binding.
        var (status, contentType, _) = await GetAsync($"{Endpoint}?{query}");

        status.ShouldBe(HttpStatusCode.BadRequest);
        contentType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Validation_problems_name_the_offending_field()
    {
        var (_, _, body) = await GetAsync($"{Endpoint}?page=0");

        body.GetProperty("errors").EnumerateObject()
            .Select(p => p.Name)
            .ShouldContain(name => name.Contains("age", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validation_problems_carry_a_trace_identifier()
    {
        // Without this a support engineer cannot tie a client's reported 400 back to a request in
        // the logs. It is the reason the framework's own problem factory is left in place rather
        // than replaced with a hand-built ValidationProblemDetails.
        var (_, _, body) = await GetAsync($"{Endpoint}?page=0");

        body.TryGetProperty("traceId", out var traceId).ShouldBeTrue("a 400 must be correlatable");
        traceId.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Server_faults_carry_a_trace_identifier()
    {
        using var hostile = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IShipmentRepository>();
                services.AddSingleton<IShipmentRepository, ThrowingShipmentRepository>();
            }));

        using var client = hostile.CreateClient();
        var response = await client.GetAsync(new Uri(Endpoint, UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        body.TryGetProperty("traceId", out var traceId).ShouldBeTrue("a 500 must be correlatable");
        traceId.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_unexpected_failure_returns_a_problem_that_leaks_nothing()
    {
        // The repository is replaced with one that throws. What matters is not that this is a 500 -
        // it is that the response carries no stack trace, no type name and no internal message,
        // because those are exactly what an attacker reads first.
        using var hostile = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IShipmentRepository>();
                services.AddSingleton<IShipmentRepository, ThrowingShipmentRepository>();
            }));

        using var client = hostile.CreateClient();
        var response = await client.GetAsync(new Uri(Endpoint, UriKind.Relative), TestContext.Current.CancellationToken);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        raw.ShouldNotContain("ThrowingShipmentRepository");
        raw.ShouldNotContain("connection string is a secret");
        raw.ShouldNotContain("at DaklaPack.");
        raw.ShouldNotContain("StackTrace", Case.Insensitive);
    }

    private sealed class ThrowingShipmentRepository : IShipmentRepository
    {
        public Task<(IReadOnlyList<Shipment> Items, int TotalCount)> SearchAsync(
            ShipmentSearchCriteria criteria, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("connection string is a secret");
    }
}
