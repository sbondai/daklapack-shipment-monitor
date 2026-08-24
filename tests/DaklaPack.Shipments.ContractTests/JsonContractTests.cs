using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

namespace DaklaPack.Shipments.ContractTests;

/// <summary>
/// Pins the JSON shape the browser client is written against.
/// </summary>
/// <remarks>
/// Serialization is the layer with no compiler behind it. Casing, enum form and date format are all
/// decided by configuration that can be changed by accident, and the failure is silent: the API
/// keeps returning 200 while the client quietly renders nothing.
/// </remarks>
public sealed partial class JsonContractTests(ShipmentsApiFactory factory) : IClassFixture<ShipmentsApiFactory>
{
    private async Task<JsonElement> FirstShipmentAsync()
    {
        using var client = factory.CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>(
            new Uri("/api/v1/shipments?pageSize=100", UriKind.Relative), TestContext.Current.CancellationToken);
        return body.GetProperty("items")[0];
    }

    [Theory]
    [InlineData("id")]
    [InlineData("trackingId")]
    [InlineData("status")]
    [InlineData("weightKg")]
    [InlineData("destination")]
    [InlineData("carrier")]
    [InlineData("dispatchedAt")]
    [InlineData("estimatedDeliveryOn")]
    [InlineData("isOverdue")]
    public async Task Every_expected_property_is_present_in_camel_case(string property) =>
        (await FirstShipmentAsync()).TryGetProperty(property, out _)
            .ShouldBeTrue($"the client is written against '{property}'");

    [Fact]
    public async Task Status_is_a_string_not_a_number()
    {
        // "InTransit" is legible in a log and in the UI; 1 is not, and its meaning shifts the
        // moment somebody inserts a member into the enum.
        var status = (await FirstShipmentAsync()).GetProperty("status");

        status.ValueKind.ShouldBe(JsonValueKind.String);
        status.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Weight_is_a_number_with_its_unit_in_the_property_name() =>
        (await FirstShipmentAsync()).GetProperty("weightKg").ValueKind.ShouldBe(JsonValueKind.Number);

    [Fact]
    public async Task Destination_stays_nested_rather_than_flattened()
    {
        var destination = (await FirstShipmentAsync()).GetProperty("destination");

        destination.ValueKind.ShouldBe(JsonValueKind.Object);
        destination.GetProperty("city").GetString().ShouldNotBeNullOrWhiteSpace();
        destination.GetProperty("countryCode").GetString()!.Length.ShouldBe(2);
        destination.GetProperty("postalCode").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Estimated_delivery_is_a_bare_calendar_date()
    {
        // No time, no zone. The client renders it verbatim; anything else here would let a time
        // zone shift a delivery date onto the wrong day.
        var value = (await FirstShipmentAsync()).GetProperty("estimatedDeliveryOn").GetString();

        CalendarDate().IsMatch(value!).ShouldBeTrue($"'{value}' is not yyyy-MM-dd");
    }

    [Fact]
    public async Task Dispatched_at_is_an_instant_that_keeps_its_offset()
    {
        var value = (await FirstShipmentAsync()).GetProperty("dispatchedAt").GetString();

        Instant().IsMatch(value!).ShouldBeTrue($"'{value}' is not ISO 8601 with an offset");
    }

    [Fact]
    public async Task Overdue_is_a_boolean() =>
        (await FirstShipmentAsync()).GetProperty("isOverdue").ValueKind
            .ShouldBeOneOf(JsonValueKind.True, JsonValueKind.False);

    [Fact]
    public async Task Overdue_is_derived_against_the_frozen_clock()
    {
        // The clock is pinned by the factory, so this is a fact about the contract rather than
        // about the day the suite happens to run.
        using var client = factory.CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>(
            new Uri("/api/v1/shipments?pageSize=100", UriKind.Relative), TestContext.Current.CancellationToken);

        var overdue = body.GetProperty("items").EnumerateArray()
            .Where(i => i.GetProperty("isOverdue").GetBoolean())
            .ToList();

        overdue.ShouldNotBeEmpty("the sample is meant to contain overdue work");
        foreach (var shipment in overdue)
        {
            var due = DateOnly.Parse(shipment.GetProperty("estimatedDeliveryOn").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);
            due.ShouldBeLessThan(DateOnly.FromDateTime(ShipmentsApiFactory.FrozenNow.UtcDateTime).AddDays(1));
            shipment.GetProperty("status").GetString().ShouldNotBeOneOf("Delivered", "Cancelled");
        }
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    private static partial Regex CalendarDate();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}([+-]\d{2}:\d{2}|Z)$")]
    private static partial Regex Instant();
}
