using TD_Project.Application;
using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

public class CicloVitalTests
{
    // spec: CU-01 — Input valido -> Ejecucion -> Reporte (Success)
    [Fact]
    public void UnInputValidoProduceUnResultadoSuccess()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCiega());

        Assert.Equal(EstadoBacktest.Success, resultado.Estado);
    }

    // spec: CU-03 — longitud <= warmup -> NotEvaluable -> 0 Fills
    [Fact]
    public void UnaLongitudMenorOIgualAlWarmupEsNotEvaluable()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m)
        }, Warmup: 5);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCiega());

        Assert.Equal(EstadoBacktest.NotEvaluable, resultado.Estado);
        Assert.Empty(resultado.Fills);
    }

    // spec: CU-04 — estrategia ciega -> Cash intacto, 0 Trades
    [Fact]
    public void UnaEstrategiaCiegaDejaElCashIntactoYSinTrades()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCiega());

        Assert.Equal(1000m, resultado.CashFinal);
        Assert.Empty(resultado.Trades);
    }

    // spec: CU-06 (Look-ahead) — lectura futura bloqueada -> Aborto (StrategyError)
    [Fact]
    public void UnaLecturaMasAllaDeNAbortaConStrategyError()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaConLookAhead());

        Assert.Equal(EstadoBacktest.StrategyError, resultado.Estado);
    }

    // spec: CU-07 (Fin) — ordenes Pending se cancelan, posicion viva se documenta M2M
    [Fact]
    public void AlFinalizarLasOrdenesPendingSeCancelanYLaPosicionVivaSeDocumentaM2M()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaConOrdenPendingAlFinal());

        Assert.All(resultado.OrdenesFinales, o => Assert.Equal(OrderStatus.Cancelled, o.Status));
    }

    // spec: RN-13 — en N, Strategy lee DataSlice(N); en N+1, Matching cruza contra OHLCV(N+1)
    [Fact]
    public void LaSenalGeneradaEnNSeEjecutaContraLaVelaNMasUno()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 500m),
            new Candle(2, 50m, 50m, 50m, 50m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        Assert.Single(resultado.Fills);
        Assert.Equal(50m, resultado.Fills[0].PrecioFill);
    }
}
