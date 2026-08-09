using Xunit;

namespace TD_Project.Contracts.Tests;

public class FronteraDeDependenciasTests
{
    // spec: RNF-12 — Contracts no debe referenciar Domain ni Application; es un contrato de
    // lectura externo, no un concepto del motor (ver diseno Fase 6).
    [Fact]
    public void ContractsNoReferenciaDomainNiApplication()
    {
        var referencias = typeof(ResultDto).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencias, r => r.Name == "TD_Project.Domain");
        Assert.DoesNotContain(referencias, r => r.Name == "TD_Project.Application");
    }
}
