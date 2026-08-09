using TD_Project.Application;
using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

public class AislamientoExperimentosTests
{
    // spec: RNF-07 — ausencia absoluta de estado o memoria compartida entre simulaciones
    [Fact]
    public void DosEjecucionesConLaMismaConfiguracionNoComparténEstado()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 500m),
            new Candle(2, 50m, 50m, 50m, 50m, 500m)
        });

        var resultado1 = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());
        var resultado2 = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        Assert.Equal(resultado1.CashFinal, resultado2.CashFinal);
        Assert.Equal(resultado1.Fills.Count, resultado2.Fills.Count);
    }
}
