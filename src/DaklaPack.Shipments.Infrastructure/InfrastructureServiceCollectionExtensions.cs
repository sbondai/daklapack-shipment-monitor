using DaklaPack.Shipments.Application.Abstractions;
using DaklaPack.Shipments.Infrastructure.Shipments;

using Microsoft.Extensions.DependencyInjection;

namespace DaklaPack.Shipments.Infrastructure;

/// <summary>
/// Registers the infrastructure adapters. The composition root is the only place that knows which
/// implementation of a port is in use.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();

        return services;
    }
}
