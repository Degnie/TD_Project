using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: Caso 2 D-063/D-064/D-065 — pruebas obligatorias de cierre de implementacion
// (ESPECIFICACION_MODELO_COSTES_V1.md): P1 regresion sin costes, P2 comision, P3 slippage solo
// Market, P4 Cross-Zero por tramo, P5 determinismo.
public class ModeloCostesTests
{
    private static ConfiguracionExperimento ConfigConVelas(decimal capitalInicial, ConfiguracionCostes? costes = null) =>
        new(CapitalInicial: capitalInicial, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        }, Costes: costes);

    // P1 — regresion sin costes: sin ConfiguracionCostes explicita, el resultado economico debe
    // ser identico al que producia el motor antes de Caso 2.2 (CostoFriccionReal = 0).
    [Fact]
    public void SinCostesExplicitosElResultadoEconomicoEsIdenticoAlHistorico()
    {
        var configSinCostes = ConfigConVelas(1000m);
        var configConDefaultExplicito = ConfigConVelas(1000m, costes: ConfiguracionCostes.Default);

        var r1 = BacktestRunner.Ejecutar(configSinCostes, new EstrategiaMarketSiempre());
        var r2 = BacktestRunner.Ejecutar(configConDefaultExplicito, new EstrategiaMarketSiempre());

        Assert.Equal(r1.CashFinal, r2.CashFinal);
        Assert.All(r1.Fills, f => Assert.Equal(0m, f.CostoFriccionReal));
        Assert.Equal(0m, ConfiguracionCostes.Default.TasaComision);
        Assert.Equal(0m, ConfiguracionCostes.Default.TasaSlippage);
    }

    // P2 — comision: Cash disminuye respecto al escenario sin coste; Equity refleja PnL neto.
    [Fact]
    public void ComisionMayorQueCeroReduceElCashRespectoAlEscenarioSinCoste()
    {
        var configSinCoste = ConfigConVelas(1000m);
        var configConComision = ConfigConVelas(1000m, costes: new ConfiguracionCostes(TasaComision: 0.01m, TasaSlippage: 0m));

        var rSinCoste = BacktestRunner.Ejecutar(configSinCoste, new EstrategiaMarketSiempre());
        var rConComision = BacktestRunner.Ejecutar(configConComision, new EstrategiaMarketSiempre());

        Assert.True(rConComision.CashFinal < rSinCoste.CashFinal);
        Assert.All(rConComision.Fills, f => Assert.True(f.CostoFriccionReal > 0m));
        Assert.Equal(rConComision.EquityCurve[^1].Cash, rConComision.CashFinal);
    }

    // P3 — slippage solo afecta ordenes Market; Limit/Stop no cambian (RN-03: precio pactado =
    // precio ejecucion, sin divergencia que modelar).
    [Fact]
    public void SlippageSoloAfectaOrdenesMarketNoLimitNiStop()
    {
        var configSinSlippage = ConfigConVelas(1000m);
        var configConSlippage = ConfigConVelas(1000m, costes: new ConfiguracionCostes(TasaComision: 0m, TasaSlippage: 0.5m));

        var rMarketSinSlippage = BacktestRunner.Ejecutar(configSinSlippage, new EstrategiaMarketSiempre());
        var rMarketConSlippage = BacktestRunner.Ejecutar(configConSlippage, new EstrategiaMarketSiempre());
        Assert.True(rMarketConSlippage.CashFinal < rMarketSinSlippage.CashFinal);

        var rLimitSinSlippage = BacktestRunner.Ejecutar(configSinSlippage, new EstrategiaLimitSiempreAlPrecioDeEntrada());
        var rLimitConSlippage = BacktestRunner.Ejecutar(configConSlippage, new EstrategiaLimitSiempreAlPrecioDeEntrada());
        Assert.Equal(rLimitSinSlippage.CashFinal, rLimitConSlippage.CashFinal);
    }

    // P4 — Cross-Zero: el coste se prorratea por tramo (cierre + apertura), sin doble aplicacion
    // ni omision — la suma de lo prorrateado debe ser exactamente el coste total del Fill.
    [Fact]
    public void CrossZeroAplicaElCostePorTramoSinDobleAplicacionNiOmision()
    {
        // N=0: Buy 10 (abre Long 10). N=1: Sell 15 (cierra 10, cruza cero, abre Short 5).
        var estrategia = new EstrategiaCrossZeroControlada();
        var config = new ConfiguracionExperimento(CapitalInicial: 100000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        }, Costes: new ConfiguracionCostes(TasaComision: 0.01m, TasaSlippage: 0m));

        var resultado = BacktestRunner.Ejecutar(config, estrategia);

        var fillCrossZero = Assert.Single(resultado.Fills, f => f.Cantidad == 15m);
        Assert.True(fillCrossZero.CostoFriccionReal > 0m);

        // Sin costes, para aislar el efecto exclusivo del prorrateo sobre Cash.
        var configSinCostes = new ConfiguracionExperimento(CapitalInicial: 100000m, Velas: config.Velas);
        var resultadoSinCostes = BacktestRunner.Ejecutar(configSinCostes, new EstrategiaCrossZeroControlada());

        var costeTotalAplicado = resultadoSinCostes.CashFinal - resultado.CashFinal;
        var costeTotalDeFills = resultado.Fills.Sum(f => f.CostoFriccionReal);
        Assert.Equal(costeTotalDeFills, costeTotalAplicado);
    }

    // P5 — determinismo: misma entrada con costes produce el mismo resultado en dos ejecuciones.
    [Fact]
    public void MismaEntradaConCostesProduceElMismoResultadoEnDosEjecuciones()
    {
        var config = ConfigConVelas(1000m, costes: new ConfiguracionCostes(TasaComision: 0.01m, TasaSlippage: 0.1m));

        var r1 = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());
        var r2 = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        Assert.Equal(r1.CashFinal, r2.CashFinal);
        Assert.Equal(r1.Fills.Count, r2.Fills.Count);
        Assert.Equal(r1.Fills.Select(f => f.CostoFriccionReal), r2.Fills.Select(f => f.CostoFriccionReal));
        Assert.Equal(r1.EquityCurve.Select(e => e.Equity), r2.EquityCurve.Select(e => e.Equity));
    }
}
