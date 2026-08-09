using NetArchTest.Rules;
using Xunit;

namespace TD_Project.Application.Tests;

public class ArchitectureTests
{
    // spec: RNF-12 — Domain no depende de Infrastructure ni de Application (ADR-001)
    [Fact]
    public void DomainNoDependeDeInfrastructureNiDeApplication()
    {
        var domainAssembly = typeof(Domain.Shared.Candle).Assembly;

        var resultado = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOnAny("TD_Project.Infrastructure", "TD_Project.Application")
            .GetResult();

        Assert.True(resultado.IsSuccessful,
            "Domain no debe depender de Infrastructure ni de Application: " +
            string.Join(", ", resultado.FailingTypeNames ?? Array.Empty<string>()));
    }
}
