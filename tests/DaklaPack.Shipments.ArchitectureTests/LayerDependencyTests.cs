using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;

using Shouldly;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DaklaPack.Shipments.ArchitectureTests;

/// <summary>
/// Enforces the layering the solution claims.
/// </summary>
/// <remarks>
/// A README that says the dependency arrow points inward is a promise. These tests are the fact.
/// They fail the build if a <c>using</c> crosses a boundary, which is the only reason the claim is
/// worth making.
/// </remarks>
public sealed class LayerDependencyTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Domain.Shipment).Assembly,
            typeof(Application.Abstractions.IShipmentRepository).Assembly,
            typeof(Infrastructure.InfrastructureServiceCollectionExtensions).Assembly,
            typeof(Api.Controllers.ShipmentsController).Assembly)
        .Build();

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly("DaklaPack.Shipments.Domain").As("Domain");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly("DaklaPack.Shipments.Application").As("Application");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly("DaklaPack.Shipments.Infrastructure").As("Infrastructure");

    private static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly("DaklaPack.Shipments.Api").As("Api");

    [Fact]
    public void Every_layer_actually_contains_types()
    {
        // The rules below are all negative - "should not depend on" - and a negative rule over an
        // empty set passes for the wrong reason. ArchUnitNET guards against that by refusing to
        // evaluate rules with no positive results, which is why those rules opt out explicitly.
        // This test is what makes the opt-out safe: if a selector ever stops matching, this fails
        // rather than the whole suite turning green while checking nothing.
        Architecture.Types.Count(t => t.Assembly.Name.StartsWith("DaklaPack.Shipments.Domain", StringComparison.Ordinal))
            .ShouldBeGreaterThan(0, "the Domain selector matched no types");
        Architecture.Types.Count(t => t.Assembly.Name.StartsWith("DaklaPack.Shipments.Application", StringComparison.Ordinal))
            .ShouldBeGreaterThan(0, "the Application selector matched no types");
        Architecture.Types.Count(t => t.Assembly.Name.StartsWith("DaklaPack.Shipments.Infrastructure", StringComparison.Ordinal))
            .ShouldBeGreaterThan(0, "the Infrastructure selector matched no types");
        Architecture.Types.Count(t => t.Assembly.Name.StartsWith("DaklaPack.Shipments.Api", StringComparison.Ordinal))
            .ShouldBeGreaterThan(0, "the Api selector matched no types");
    }

    [Fact]
    public void Domain_depends_on_nothing_in_the_solution()
    {
        // The innermost layer. If this ever fails, the model has started to know about the machinery
        // that stores or serves it, and the whole arrangement stops paying for itself.
        Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .AndShould().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(ApiLayer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_the_api() =>
        // This is the rule the repository port exists to make true: the use case is written and
        // compiled with no persistence package in sight.
        Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(ApiLayer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Infrastructure_does_not_depend_on_the_api() =>
        Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(ApiLayer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

}
