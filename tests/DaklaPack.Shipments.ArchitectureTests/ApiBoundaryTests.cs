using System.Reflection;

using DaklaPack.Shipments.Api.Controllers;
using DaklaPack.Shipments.Domain;

using Shouldly;

namespace DaklaPack.Shipments.ArchitectureTests;

/// <summary>
/// Asserts what the API is allowed to expose and depend on.
/// </summary>
/// <remarks>
/// Reflection over the real controller types rather than a namespace rule, for the same reason as
/// <see cref="FrameworkIsolationTests"/>: this cannot pass by matching nothing. The guard test
/// below fails if the controller set is ever empty.
/// </remarks>
public sealed class ApiBoundaryTests
{
    private static readonly Type[] Controllers =
        [.. typeof(ShipmentsController).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal) && !t.IsAbstract)];

    private static readonly Assembly DomainAssembly = typeof(Shipment).Assembly;

    private static bool IsDomainType(Type type)
    {
        // Unwrap Task<T>, ActionResult<T>, IEnumerable<T> and so on: a domain entity nested inside
        // a generic return type still crosses the boundary.
        if (type.IsGenericType)
        {
            return type.GetGenericArguments().Any(IsDomainType);
        }

        return type.Assembly == DomainAssembly && !type.IsEnum;
    }

    [Fact]
    public void There_is_at_least_one_controller_to_check() =>
        // Without this, every rule below would pass by checking an empty set.
        Controllers.ShouldNotBeEmpty();

    [Fact]
    public void No_action_returns_a_domain_entity()
    {
        // DTOs at the boundary. An entity on the wire couples the client to the model and leaks
        // whatever the model gains later - including fields nobody meant to publish.
        var leaks = Controllers
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => IsDomainType(m.ReturnType))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}");

        leaks.ShouldBeEmpty();
    }

    [Fact]
    public void No_action_accepts_a_domain_entity()
    {
        var leaks = Controllers
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .SelectMany(m => m.GetParameters().Select(p => (Method: m, Parameter: p)))
            .Where(x => IsDomainType(x.Parameter.ParameterType))
            .Select(x => $"{x.Method.DeclaringType!.Name}.{x.Method.Name}({x.Parameter.Name})");

        leaks.ShouldBeEmpty();
    }

    [Fact]
    public void Controllers_depend_on_no_concrete_infrastructure_adapter()
    {
        // Api references Infrastructure so the composition root can register adapters. That is the
        // only place allowed to know them; a controller reaching for one bypasses the port and the
        // layering stops meaning anything.
        var offenders = Controllers
            .SelectMany(c => c.GetConstructors())
            .SelectMany(ctor => ctor.GetParameters().Select(p => (Controller: ctor.DeclaringType!, p.ParameterType)))
            .Where(x => x.ParameterType.Assembly.GetName().Name == "DaklaPack.Shipments.Infrastructure")
            .Select(x => $"{x.Controller.Name} <- {x.ParameterType.Name}");

        offenders.ShouldBeEmpty();
    }
}
