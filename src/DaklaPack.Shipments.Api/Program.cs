using System.Text.Json.Serialization;

using DaklaPack.Shipments.Api.Configuration;
using DaklaPack.Shipments.Application.Shipments.GetShipments;
using DaklaPack.Shipments.Infrastructure;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "web-client";

// Bound once and reused below. The section is optional: the defaults on ShipmentMonitorOptions are
// safe (Amsterdam, no CORS origins), so an absent section starts correctly rather than failing.
// What is not tolerated is a section that is present and wrong - an unparsable time zone id throws
// at startup, below, rather than on the first request that needs it.
builder.Services
    .AddOptions<ShipmentMonitorOptions>()
    .Bind(builder.Configuration.GetSection(ShipmentMonitorOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var options = new ShipmentMonitorOptions();
builder.Configuration.GetSection(ShipmentMonitorOptions.SectionName).Bind(options);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums travel as their names. "InTransit" is legible in an operations UI and in a log;
        // 1 is not, and it silently changes meaning if a member is ever inserted.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// RFC 7807 for both validation failures and unhandled faults. The framework's own factory is left
// in place deliberately: replacing it drops the traceId that ties a client's 400 back to a request
// in the logs, and support losing that correlation costs more than a hand-written Title is worth.
//
// The media type is a separate matter and was a real defect: [Produces("application/json")] on the
// controller constrained negotiation for *every* response, so problem documents were being served
// as application/json. It is gone, and a contract test holds that.
builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddCors(cors => cors.AddPolicy(
    CorsPolicy,
    policy => policy.WithOrigins(options.AllowedOrigins).AllowAnyHeader().WithMethods("GET")));

// TimeProvider.System is the framework's own clock abstraction; no bespoke IClock is needed.
builder.Services.AddSingleton(TimeProvider.System);

// The calendar "overdue" is judged against. Resolved once at startup so an invalid id is a boot
// failure rather than a per-request exception.
builder.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(options.BusinessTimeZone));
builder.Services.AddSingleton<ShipmentMapper>();
builder.Services.AddScoped<GetShipmentsHandler>();

// The composition root is the only place that knows which adapter backs the port.
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // The spec is generated from the code, so it cannot drift from what is served.
    // Exposed in development only, per Microsoft's guidance.
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("DaklaPack Shipment Monitor"));
}

app.UseCors(CorsPolicy);
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

/// <summary>Exposed so the contract tests can host the real pipeline in process.</summary>
public partial class Program;
