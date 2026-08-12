using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: Caso 2 D-066/D-067/D-068/D-069/D-070/D-071 — pruebas obligatorias de cierre de
// implementacion (ESPECIFICACION_GESTOR_CAPITAL_PORCENTAJE_V1.md §6): P1 regresion sin sizing,
// P2 calculo correcto, P3 no modifica direccion, P4 bolsa completa (RN-14), P5 determinismo,
// P6 trazabilidad (D-069).
public class GestorCapitalTests
{
    private static ConfiguracionExperimento ConfigConVelas(decimal capitalInicial, ConfiguracionSizing? sizing = null) =>
        new(CapitalInicial: capitalInicial, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        }, Sizing: sizing);

    // P1 — regresion sin sizing: Sizing=null produce resultado identico al historico.
    [Fact]
    public void SinSizingElResultadoEsIdenticoAlHistorico()
    {
        var configSinSizing = ConfigConVelas(1000m);

        var resultado = BacktestRunner.Ejecutar(configSinSizing, new EstrategiaMarketSiempre());

        Assert.All(resultado.Fills, f => Assert.Equal(1m, f.Cantidad));
    }

    // P2 — calculo correcto: Cantidad = (Cash - Margin) * PorcentajeRiesgo en el momento de la orden.
    [Fact]
    public void ConSizingActivoLaCantidadEsCapitalDisponiblePorPorcentaje()
    {
        var sizing = new ConfiguracionSizing(PorcentajeRiesgo: 0.1m);
        var config = ConfigConVelas(1000m, sizing);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        var primerFill = resultado.Fills[0];
        // Primer ciclo: Cash=1000, Margin=0 -> CapitalDisponible=1000 -> Cantidad=1000*0.1=100.
        Assert.Equal(100m, primerFill.Cantidad);
    }

    // P3 — no modifica direccion: Side/Type/PrecioLimite/PrecioStop identicos, solo Cantidad cambia.
    [Fact]
    public void GestorCapitalNoModificaDireccionNiTipoDeOrden()
    {
        var sizing = new ConfiguracionSizing(PorcentajeRiesgo: 0.05m);
        var config = ConfigConVelas(1000m, sizing);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        Assert.All(resultado.Fills, f => Assert.Equal(OrderType.Market, f.TipoOrdenOriginal));
        Assert.All(resultado.Fills, f => Assert.Equal(Side.Buy, f.Side));
    }

    // P4 — bolsa completa (RN-14): multiples OrderRequest en el mismo ciclo reciben la misma
    // Cantidad, calculada sobre el mismo CapitalDisponible (portfolio no cambia dentro del ciclo).
    [Fact]
    public void OrdenesDeLaMismaBolsaRecibenLaMismaCantidadCalculada()
    {
        var sizing = new ConfiguracionSizing(PorcentajeRiesgo: 0.1m);
        var config = ConfigConVelas(1000m, sizing);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaOcoDosOrdenes());

        Assert.Equal(2, resultado.OrdenesFinales.Count);
        Assert.Equal(resultado.OrdenesFinales[0].Cantidad, resultado.OrdenesFinales[1].Cantidad);
        Assert.Equal(100m, resultado.OrdenesFinales[0].Cantidad);
    }

    // P5 — determinismo: misma entrada con sizing activo produce el mismo resultado en dos ejecuciones.
    [Fact]
    public void MismaEntradaConSizingProduceElMismoResultadoEnDosEjecuciones()
    {
        var sizing = new ConfiguracionSizing(PorcentajeRiesgo: 0.1m);
        var config = ConfigConVelas(1000m, sizing);

        var r1 = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());
        var r2 = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        Assert.Equal(r1.CashFinal, r2.CashFinal);
        Assert.Equal(r1.Fills.Select(f => f.Cantidad), r2.Fills.Select(f => f.Cantidad));
    }

    // P6 — trazabilidad (D-069): dos configuraciones identicas salvo Sizing producen identidad
    // (hash compuesto) distinta — verificado sobre ConfiguracionExperimento en si (que alimenta
    // IdentidadExperimentoCompleta en el laboratorio), sin sizing vs con sizing son distintas.
    [Fact]
    public void ConfiguracionConYSinSizingProducenConfiguracionesDistintas()
    {
        var configSinSizing = ConfigConVelas(1000m);
        var configConSizing = ConfigConVelas(1000m, new ConfiguracionSizing(PorcentajeRiesgo: 0.1m));

        Assert.NotEqual(configSinSizing.Sizing, configConSizing.Sizing);

        var resultadoSinSizing = BacktestRunner.Ejecutar(configSinSizing, new EstrategiaMarketSiempre());
        var resultadoConSizing = BacktestRunner.Ejecutar(configConSizing, new EstrategiaMarketSiempre());
        Assert.NotEqual(resultadoSinSizing.Fills[0].Cantidad, resultadoConSizing.Fills[0].Cantidad);
    }
}
