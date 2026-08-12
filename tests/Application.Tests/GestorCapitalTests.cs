using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: Caso 2 D-066/D-067/D-068/D-069/D-070/D-071 — pruebas obligatorias de cierre de
// implementacion (ESPECIFICACION_GESTOR_CAPITAL_PORCENTAJE_V1.md §6): P1 regresion sin sizing,
// P2 calculo correcto, P3 no modifica direccion, P4 bolsa completa (RN-14), P5 determinismo,
// P6 trazabilidad (D-069).
// spec: Caso 4 D-093/D-094 — P2/P4 actualizadas al contrato dimensional corregido
// (ESPECIFICACION_IMPLEMENTACION_SIZING_CORREGIDO_V1.md S4): Cantidad = MargenObjetivo /
// (CloseReferencia * TasaMargen), ya no CapitalDisponible * PorcentajeRiesgo directo. P1/P3/P5/P6
// sin cambio (propiedades estructurales independientes del valor exacto de la formula).
public class GestorCapitalTests
{
    private static ConfiguracionExperimento ConfigConVelas(decimal capitalInicial, ConfiguracionSizing? sizing = null) =>
        new(CapitalInicial: capitalInicial, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        }, Sizing: sizing);

    // spec: Caso 5A D-108 — GestorCapital.Ajustar ahora recibe DataSlice (ver GestorCapital.cs).
    // Las pruebas que invocan Ajustar directamente (sin pasar por BacktestRunner) construyen un
    // DataSlice minimo — ningun gestor usado en estas pruebas (GestorFixedFractional) lo consume.
    private static DataSlice DataSliceDePrueba() => new(new[] { new Candle(1, 100m, 100m, 100m, 100m, 0m) });

    // P1 — regresion sin sizing: Sizing=null produce resultado identico al historico.
    [Fact]
    public void SinSizingElResultadoEsIdenticoAlHistorico()
    {
        var configSinSizing = ConfigConVelas(1000m);

        var resultado = BacktestRunner.Ejecutar(configSinSizing, new EstrategiaMarketSiempre());

        Assert.All(resultado.Fills, f => Assert.Equal(1m, f.Cantidad));
    }

    // P2 — calculo correcto (D-093/D-094): Cantidad = MargenObjetivo / (CloseReferencia * TasaMargen),
    // con MargenObjetivo = (Cash - Margin) * PorcentajeRiesgo.
    [Fact]
    public void ConSizingActivoLaCantidadEsMargenObjetivoEntrePrecioPorTasaMargen()
    {
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
        var config = ConfigConVelas(1000m, sizing);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        var primerFill = resultado.Fills[0];
        // Primer ciclo: Cash=1000, Margin=0 -> CapitalDisponible=1000 -> MargenObjetivo=100.
        // CloseReferencia=Velas[1].Close=104, TasaMargen=Instrumento.Default=0.1.
        // Cantidad = 100 / (104 * 0.1) = 100 / 10.4.
        var cantidadEsperada = 100m / (104m * 0.1m);
        Assert.Equal(cantidadEsperada, primerFill.Cantidad);
    }

    // P3 — no modifica direccion: Side/Type/PrecioLimite/PrecioStop identicos, solo Cantidad cambia.
    [Fact]
    public void GestorCapitalNoModificaDireccionNiTipoDeOrden()
    {
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.05m));
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
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
        var config = ConfigConVelas(1000m, sizing);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaOcoDosOrdenes());

        Assert.Equal(2, resultado.OrdenesFinales.Count);
        Assert.Equal(resultado.OrdenesFinales[0].Cantidad, resultado.OrdenesFinales[1].Cantidad);
        // Mismo calculo que P2: MargenObjetivo=100, CloseReferencia=104, TasaMargen=0.1.
        var cantidadEsperada = 100m / (104m * 0.1m);
        Assert.Equal(cantidadEsperada, resultado.OrdenesFinales[0].Cantidad);
    }

    // P5 — determinismo: misma entrada con sizing activo produce el mismo resultado en dos ejecuciones.
    [Fact]
    public void MismaEntradaConSizingProduceElMismoResultadoEnDosEjecuciones()
    {
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
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
        var configConSizing = ConfigConVelas(1000m, new ConfiguracionSizing(new GestorFixedFractional(0.1m)));

        Assert.NotEqual(configSinSizing.Sizing, configConSizing.Sizing);

        var resultadoSinSizing = BacktestRunner.Ejecutar(configSinSizing, new EstrategiaMarketSiempre());
        var resultadoConSizing = BacktestRunner.Ejecutar(configConSizing, new EstrategiaMarketSiempre());
        Assert.NotEqual(resultadoSinSizing.Fills[0].Cantidad, resultadoConSizing.Fills[0].Cantidad);
    }

    // spec: Caso 4 D-093, ESPECIFICACION_INTEGRACION_GESTOR_CAPITAL_V1.md S7 — P1-P6 (arriba) son
    // las pruebas congeladas de Caso 2 (ESPECIFICACION_GESTOR_CAPITAL_PORCENTAJE_V1.md), todas
    // pasan sin modificacion tras la integracion de ClasificadorIntencionOrden (ningun escenario ahi
    // cruza cero). Las siguientes son las pruebas nuevas de 4.2.

    // P7 (4.2) — CierreTotal conserva la Cantidad original, no la de sizing: N=0 abre Long 1 (recibe
    // sizing), N=1 emite Sell 1 para cerrar exactamente esa posicion — debe ejecutarse con Cantidad=1,
    // no con la Cantidad calculada por sizing (causa raiz original de D-084).
    [Fact]
    public void CierreTotalConservaLaCantidadOriginalNoLaDeSizing()
    {
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
        var config = ConfigConVelas(1000m, sizing);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaAbreYCierraAlternado());

        // Fill[0] = apertura (sizing aplicado, Cantidad != 1). Fill[1] = cierre total (Cantidad = 1,
        // exactamente lo que abrio Fill[0], no un nuevo calculo de sizing).
        Assert.NotEqual(1m, resultado.Fills[0].Cantidad);
        Assert.Equal(1m, resultado.Fills[1].Cantidad);
    }

    // P8 (4.2, actualizada por D-095) — bajo sizing activo, un CrossZero detectado contra la
    // Cantidad nominal de la estrategia (15) es espurio: la posicion real que dejo la apertura con
    // sizing NO es 10 (la Cantidad que la estrategia penso que abria), sino la Cantidad calculada
    // por GestorCapital. D-095 normaliza ese CrossZero a CierreTotal usando la magnitud real de la
    // posicion proyectada — ya no conserva la Cantidad nominal de la orden (15), que era el
    // comportamiento correcto ANTES de D-095 pero dejo de serlo (ver ESPECIFICACION_NORMALIZACION_
    // CIERRES_SIZING_V1.md S1: interpretar el excedente nominal como CrossZero era el defecto).
    [Fact]
    public void CrossZeroEspurioSeNormalizaACierreTotalConLaPosicionRealBajoSizing()
    {
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
        var config = ConfigConVelas(1000m, sizing);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCrossZeroControlada());

        // Fill[0]: N=0 abre Buy 10 (nominal) -> sizing aplicado, posicion real != 10.
        // Fill[1]: N=1 Sell 15 (nominal) -> normalizado a CierreTotal con la magnitud de Fill[0].
        Assert.Equal(resultado.Fills[0].Cantidad, resultado.Fills[1].Cantidad);
        Assert.NotEqual(15m, resultado.Fills[1].Cantidad);
        Assert.NotEqual(10m, resultado.Fills[1].Cantidad);
    }

    // P9 (4.2, hallazgo critico S2/S6-P6-especificacion) — bolsa con 2 OrderRequest en el mismo
    // ciclo (cierre + apertura de signo contrario, patron real de EstrategiaNeutral/Z-Score): la
    // SEGUNDA orden debe clasificarse contra la posicion PROYECTADA (Apertura, recibe sizing), no
    // contra el PortfolioState real sin actualizar (que la clasificaria erroneamente como
    // CierreTotal por segunda vez). Probado directamente sobre GestorCapital.Ajustar con un
    // PortfolioState construido explicitamente (posicion Long=1 ya viva, sin pasar por sizing de
    // apertura) para aislar la clasificacion secuencial dentro de la bolsa del calculo de sizing.
    [Fact]
    public void SegundaOrdenDeUnaBolsaDeReversionSeClasificaContraLaPosicionProyectada()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 1m, 100m, 0m, 1, OrderType.Market));
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
        var requests = new[]
        {
            new OrderRequest(Side.Sell, OrderType.Market, 1m),
            new OrderRequest(Side.Sell, OrderType.Market, 1m)
        };

        var ajustadas = GestorCapital.Ajustar(requests, portfolio, sizing, DataSliceDePrueba(), 100m, 0.1m);

        // Primera Sell: cierra el Long=1 vivo -> CierreTotal -> conserva Cantidad=1 original.
        // Segunda Sell (misma bolsa): clasificada contra la posicion ya proyectada en 0 tras la
        // primera -> Apertura -> recibe sizing (Cantidad != 1, calculada sobre CapitalDisponible).
        Assert.Equal(1m, ajustadas[0].Cantidad);
        Assert.NotEqual(1m, ajustadas[1].Cantidad);
    }

    // P10 (4.2) — no mutacion de PortfolioState real por GestorCapital: Ajustar es una funcion pura
    // sobre requests, la proyeccion interna nunca se escribe de vuelta (D-071 sigue vigente).
    [Fact]
    public void AjustarNoMutaElPortfolioStateReal()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 1m, 100m, 0m, 1, OrderType.Market));
        var cashAntes = portfolio.Cash;
        var marginAntes = portfolio.Margin;
        var lotesAntes = portfolio.LotesVivos.Count;
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
        var requests = new[]
        {
            new OrderRequest(Side.Sell, OrderType.Market, 1m),
            new OrderRequest(Side.Sell, OrderType.Market, 1m)
        };

        GestorCapital.Ajustar(requests, portfolio, sizing, DataSliceDePrueba(), 100m, 0.1m);

        Assert.Equal(cashAntes, portfolio.Cash);
        Assert.Equal(marginAntes, portfolio.Margin);
        Assert.Equal(lotesAntes, portfolio.LotesVivos.Count);
    }

    // spec: Caso 4 D-093/D-094, ESPECIFICACION_IMPLEMENTACION_SIZING_CORREGIDO_V1.md S5 — pruebas
    // nuevas de 4.3 (P3/P7/P8/P9 de la especificacion; P1/P2/P4/P5/P6 son las de arriba, ya
    // actualizadas/verificadas).

    // P3 (4.3) — consistencia con CalculadoraLotes: el Margin retenido tras aplicar el Fill
    // coincide con el MargenObjetivo original — cierra el circulo dimensional completo, razon de
    // ser de D-093/D-094.
    [Fact]
    public void MargenRetenidoTrasElFillCoincideConElMargenObjetivo()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        var porcentajeRiesgo = 0.1m;
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(porcentajeRiesgo));
        var requests = new[] { new OrderRequest(Side.Buy, OrderType.Market, 1m) };
        var precioReferencia = 100m;
        var tasaMargen = 0.1m;

        var ajustadas = GestorCapital.Ajustar(requests, portfolio, sizing, DataSliceDePrueba(), precioReferencia, tasaMargen);
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, ajustadas[0].Cantidad, precioReferencia, 0m, 1, OrderType.Market), tasaMargen);

        var capitalDisponible = 1000m - 0m;
        var margenObjetivo = capitalDisponible * porcentajeRiesgo;
        Assert.Equal(margenObjetivo, portfolio.Margin);
    }

    // P7 (4.3) — no regresion de D-084: ReduccionParcial/CierreTotal/CrossZero siguen conservando
    // Cantidad original bajo la formula corregida (el cambio de formula solo afecta
    // Apertura/Aumento, la clasificacion de 4.1/4.2 no se toca).
    [Fact]
    public void CierreTotalSigueConservandoCantidadOriginalConFormulaCorregida()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 1m, 100m, 0m, 1, OrderType.Market));
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
        var requests = new[] { new OrderRequest(Side.Sell, OrderType.Market, 1m) };

        var ajustadas = GestorCapital.Ajustar(requests, portfolio, sizing, DataSliceDePrueba(), 100m, 0.1m);

        Assert.Equal(1m, ajustadas[0].Cantidad);
    }

    // spec: Caso 4 D-095, ESPECIFICACION_NORMALIZACION_CIERRES_SIZING_V1.md S6 — pruebas nuevas de
    // normalizacion de cierres bajo sizing. P1/P3/P4/P5/P6/P7 de la especificacion ya cubiertas
    // arriba (P9 de 4.2 = P3 de la spec; el nuevo P8 de 4.2 = P1 de la spec; P7-nueva de 4.3 = P6
    // de la spec; P1/P5 de la spec = baselines/Sizing=null, cubiertas por P1 congelada). Las
    // siguientes cubren lo que no tenia prueba dedicada todavia.

    // P2 (spec S6) — cierre parcial sin normalizacion necesaria: cantidad nominal ya menor a la
    // posicion real, CantidadEfectiva == Cantidad solicitada, sin recorte.
    [Fact]
    public void ReduccionParcialConCantidadNominalMenorNoSeNormaliza()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 1m, 100m, 0m, 1, OrderType.Market));
        var sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m));
        var requests = new[] { new OrderRequest(Side.Sell, OrderType.Market, 0.5m) };

        var ajustadas = GestorCapital.Ajustar(requests, portfolio, sizing, DataSliceDePrueba(), 100m, 0.1m);

        Assert.Equal(0.5m, ajustadas[0].Cantidad);
    }

    // P4 (spec S6) — Cross-Zero genuino bajo Sizing=null no se ve afectado por D-095: la
    // normalizacion solo se ejecuta cuando sizing esta activo (guarda "sizing is null" sin
    // cambios) — mismo resultado que antes de D-095/D-093/D-094.
    [Fact]
    public void CrossZeroGenuinoSinSizingConservaLaCantidadNominalSinNormalizar()
    {
        var config = ConfigConVelas(1000m); // Sizing=null

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCrossZeroControlada());

        Assert.Equal(10m, resultado.Fills[0].Cantidad);
        Assert.Equal(15m, resultado.Fills[1].Cantidad);
    }
}
