using System.Reflection;

using DaklaPack.Shipments.Application.Abstractions;
using DaklaPack.Shipments.Domain;

using Shouldly;

namespace DaklaPack.Shipments.ArchitectureTests;

/// <summary>
/// Asserts which frameworks each inner layer is allowed to have compiled against.
/// </summary>
/// <remarks>
/// Deliberately checks <see cref="Assembly.GetReferencedAssemblies"/> rather than expressing this
/// through the fluent architecture DSL. A namespace-matching rule silently passes when its pattern
/// fails to match, and a rule that cannot fail is worse than no rule - it reports success while
/// checking nothing. The reference list is what the compiler actually emitted, so a leak cannot
/// hide from it.
/// </remarks>
public sealed class FrameworkIsolationTests
{
    private static string[] ReferencesOf(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)];

    [Theory]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("System.Text.Json")]
    public void Domain_is_compiled_against_no_web_serialisation_or_persistence_framework(string forbidden)
    {
        // The domain describes shipments. If it starts referencing the machinery that stores or
        // serves them, the layering has stopped paying for itself.
        ReferencesOf(typeof(Shipment).Assembly)
            .ShouldNotContain(
                reference => reference.StartsWith(forbidden, StringComparison.Ordinal),
                $"Domain must not reference {forbidden}");
    }

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore")]
    public void Application_is_compiled_against_no_persistence_or_web_framework(string forbidden)
    {
        // This is the rule IShipmentRepository exists to make true: the use case compiles with no
        // persistence package in sight, which is what lets the adapter be swapped without touching it.
        ReferencesOf(typeof(IShipmentRepository).Assembly)
            .ShouldNotContain(
                reference => reference.StartsWith(forbidden, StringComparison.Ordinal),
                $"Application must not reference {forbidden}");
    }

    [Fact]
    public void Domain_references_nothing_from_this_solution() =>
        ReferencesOf(typeof(Shipment).Assembly)
            .ShouldNotContain(reference => reference.StartsWith("DaklaPack.", StringComparison.Ordinal));

    [Fact]
    public void Application_references_only_the_domain_from_this_solution() =>
        ReferencesOf(typeof(IShipmentRepository).Assembly)
            .Where(reference => reference.StartsWith("DaklaPack.", StringComparison.Ordinal))
            .ShouldBe(["DaklaPack.Shipments.Domain"]);
}
