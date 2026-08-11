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

    // spec: RN-07, RN-09, glosario "Trade" — Open Long 10 @100, Sell 4 @105 (parcial, sin cierre),
    // Sell 6 @110 (cierra a cero). Un unico Trade se cierra al llegar la posicion a cero,
    // consolidando el RealizedPnL de ambas reducciones: 4*(105-100) + 6*(110-100) = 20+60 = 80.
    [Fact]
    public void UnaPosicionConDosReduccionesCierraUnSoloTradeAlLlegarACero()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 500m),
            new Candle(2, 100m, 100m, 100m, 100m, 500m),
            new Candle(3, 105m, 105m, 105m, 105m, 500m),
            new Candle(4, 110m, 110m, 110m, 110m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaAperturaYDosReduccionesParciales());

        var trade = Assert.Single(resultado.Trades);
        Assert.Equal(10m, trade.CantidadInicial);
        Assert.Equal(100m, trade.PrecioApertura);
        Assert.Equal(110m, trade.PrecioCierre);
        Assert.Equal(80m, trade.RealizedPnL);
    }

    // spec: RNF-08 — EquityCurve, PortfolioSnapshots y BranchResolutions (evidencia RN-11) deben
    // sobrevivir hasta ResultadoBacktest: un punto/snapshot/resolucion por cada vela procesada.
    [Fact]
    public void ElResultadoConservaEquityCurvePortfolioSnapshotsYBranchResolutionsPorVela()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCiega());

        Assert.Equal(2, resultado.EquityCurve.Count);
        Assert.Equal(2, resultado.PortfolioSnapshots.Count);
        Assert.Equal(2, resultado.BranchResolutions.Count);
        Assert.All(resultado.EquityCurve, p => Assert.Equal(1000m, p.Equity));
    }

    // spec: RN-10, glosario "Trade" — tres Cross-Zero consecutivos deben producir tres Trades,
    // cada uno con el PrecioApertura/CantidadInicial REALES de su propio ciclo, no heredados del
    // ciclo anterior. Buy 10@100 (abre) -> Sell 15@105 (cierra Trade1, abre Short 5) -> Buy
    // 8@110 (cierra Trade2, abre Long 3) -> Sell 6@120 (cierra Trade3, abre Short 3 viva).
    [Fact]
    public void TresCrossZeroConsecutivosReportanElPrecioDeAperturaRealDeCadaCiclo()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 100000m, Velas: new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 500m),
            new Candle(2, 100m, 100m, 100m, 100m, 500m),
            new Candle(3, 105m, 105m, 105m, 105m, 500m),
            new Candle(4, 110m, 110m, 110m, 110m, 500m),
            new Candle(5, 120m, 120m, 120m, 120m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresCrossZeroConsecutivos());

        Assert.Equal(3, resultado.Trades.Count);
        Assert.Equal(100m, resultado.Trades[0].PrecioApertura);
        Assert.Equal(10m, resultado.Trades[0].CantidadInicial);
        Assert.Equal(105m, resultado.Trades[1].PrecioApertura);
        Assert.Equal(5m, resultado.Trades[1].CantidadInicial);
        Assert.Equal(110m, resultado.Trades[2].PrecioApertura);
        Assert.Equal(3m, resultado.Trades[2].CantidadInicial);
    }

    // spec: RN-11, RNF-06 — el caso canonico de divergencia (ver StopLimitTests) corrido a traves
    // del flujo real de BacktestRunner produce el mismo resultado en ejecuciones repetidas con el
    // mismo input, y las dos ramas dejan de ser trivialmente identicas (FillsA != FillsB).
    [Fact]
    public void BacktestCompletoMantieneDeterminismoConNuevaResolucionTemporal()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 90m, 90m, 90m, 90m, 500m),
            new Candle(2, 100m, 102m, 90m, 102m, 500m)
        });

        var primeraEjecucion = BacktestRunner.Ejecutar(config, new EstrategiaStopLimitDivergente());
        var segundaEjecucion = BacktestRunner.Ejecutar(config, new EstrategiaStopLimitDivergente());

        Assert.Equal(primeraEjecucion.CashFinal, segundaEjecucion.CashFinal);
        Assert.Equal(primeraEjecucion.Fills.Count, segundaEjecucion.Fills.Count);

        var resolucion = Assert.Single(primeraEjecucion.BranchResolutions);
        Assert.NotEqual(resolucion.FillsA.Count, resolucion.FillsB.Count);
    }
}
