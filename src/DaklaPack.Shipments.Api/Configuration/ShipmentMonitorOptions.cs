using System.ComponentModel.DataAnnotations;

namespace DaklaPack.Shipments.Api.Configuration;

/// <summary>Settings for the shipment monitoring API.</summary>
public sealed class ShipmentMonitorOptions
{
    public const string SectionName = "ShipmentMonitor";

    /// <summary>
    /// The IANA time zone whose calendar decides whether a shipment is overdue.
    /// </summary>
    /// <remarks>
    /// Explicit rather than inherited from the host, so the answer does not change with the machine
    /// the service lands on. Defaulted, so an absent section starts correctly; a present but invalid
    /// id fails at startup. Resolving an IANA id needs tzdata in the image — the chiseled .NET base
    /// images ship without it, which is why deployment calls for the "-extra" variant.
    /// </remarks>
    [Required(AllowEmptyStrings = false)]
    public string BusinessTimeZone { get; init; } = "Europe/Amsterdam";

    /// <summary>Origins the browser client is served from. Never a wildcard.</summary>
    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>
    /// How long in-flight requests are given to finish after a shutdown signal.
    /// </summary>
    /// <remarks>
    /// A deployment decision rather than an application one: it depends on the slowest request the
    /// service serves and on how long the orchestrator waits before killing the container. Exposed
    /// so the two can be set to agree.
    /// </remarks>
    [Range(1, 120)]
    public int ShutdownDrainSeconds { get; init; } = 15;
}
