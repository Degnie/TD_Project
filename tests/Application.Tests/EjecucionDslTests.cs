using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy.Dsl;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: CU-22 — carga de estrategia valida en JSON DSL -> BacktestRunner ejecuta la simulacion
// determinista -> emite resultados completos. InterpreteDsl produce un IStrategy (RN-16); el
// BacktestRunner ya congelado (RN-13) lo consume sin ningun cambio de contrato.
public class EjecucionDslTests
{
    private const string JsonSmaBuy = """
    {
      "condicion": { "indicador": "SMA", "periodo": 1, "operador": ">", "campo": "Close" },
      "accion": { "side": "Buy", "type": "Market" }
    }
    """;

    // spec: CU-22 — una estrategia DSL cargada se ejecuta contra el BacktestRunner existente y
    // produce un ResultadoBacktest en estado Success
    [Fact]
    public void UnaEstrategiaDslValidaSeEjecutaContraElBacktestRunner()
    {
        var estrategia = InterpreteDsl.CargarDesdeJson(JsonSmaBuy);
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 0m),
            new Candle(2, 200m, 200m, 200m, 200m, 0m),
            new Candle(3, 300m, 300m, 300m, 300m, 0m)
        });

        var resultado = BacktestRunner.Ejecutar(config, estrategia);

        Assert.Equal(EstadoBacktest.Success, resultado.Estado);
        Assert.NotEmpty(resultado.Fills);
    }
}
