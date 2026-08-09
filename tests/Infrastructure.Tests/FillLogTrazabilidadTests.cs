using TD_Project.Application;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Infrastructure.Tests;

public class FillLogTrazabilidadTests
{
    // spec: RNF-08 — cada Fill del resultado debe rastrearse inequivocamente a su Order
    // mediante la Secuencia Causal, y exponer los campos obligatorios del Fill Log Minimo.
    [Fact]
    public void CadaFillDelResultadoSeRastreaASuOrdenPorSecuenciaCausal()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 500m),
            new Candle(2, 50m, 50m, 50m, 50m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempreParaFillLog());

        var fill = Assert.Single(resultado.Fills);
        var orden = Assert.Single(resultado.OrdenesFinales);
        Assert.Equal(orden.SecuenciaCausal, fill.SecuenciaCausal);
        Assert.Equal(orden.Type, fill.TipoOrdenOriginal);
    }
}
