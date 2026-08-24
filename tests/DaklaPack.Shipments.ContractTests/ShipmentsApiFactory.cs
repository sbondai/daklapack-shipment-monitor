using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DaklaPack.Shipments.ContractTests;

/// <summary>
/// Hosts the real application in process for contract tests.
/// </summary>
/// <remarks>
/// Requests go through the actual middleware, routing, model binding, validation, mapping and
/// serialization pipeline. That is the point: these tests assert the contract a browser would
/// receive, not the shape a handler returns before ASP.NET has had its say.
///
/// The clock is pinned so date-dependent fields are deterministic; a suite whose assertions change
/// at midnight is a suite people learn to ignore.
/// </remarks>
public sealed class ShipmentsApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The instant every test runs at.</summary>
    public static readonly DateTimeOffset FrozenNow = new(2026, 8, 22, 10, 0, 0, TimeSpan.Zero);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Production");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FrozenTimeProvider(FrozenNow));
        });
    }

    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
