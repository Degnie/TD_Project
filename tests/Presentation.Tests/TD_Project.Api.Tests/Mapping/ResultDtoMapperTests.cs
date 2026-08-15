using TD_Project.Api.Mapping;
using TD_Project.Application;
using TD_Project.Domain.Broker;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Regimen;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Api.Tests.Mapping;

public class ResultDtoMapperTests
{
    private static ConfiguracionExperimento CrearConfigDeEjemplo() => new(
        CapitalInicial: 1000m,
        Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        });

    private static ResultadoBacktest CrearResultadoDeEjemplo() => new(
        Estado: EstadoBacktest.Success,
        Fills: new[] { new Fill(SecuenciaCausal: 1, Side: Side.Buy, Cantidad: 10m, PrecioFill: 100m, CostoFriccionReal: 0.5m, Timestamp: 2, TipoOrdenOriginal: OrderType.Market) },
        CashFinal: 900m,
        Trades: new[] { new Trade(CantidadInicial: 10m, PrecioApertura: 100m, PrecioCierre: 110m, RealizedPnL: 100m) },
        OrdenesFinales: Array.Empty<Order>(),
        EquityCurve: new[]
        {
            new EquityPoint(Timestamp: 2, Cash: 900m, Margin: 100m, UnrealizedPnL: 50m, Equity: 1050m),
            new EquityPoint(Timestamp: 3, Cash: 900m, Margin: 100m, UnrealizedPnL: 60m, Equity: 1060m)
        },
        PortfolioSnapshots: new[] { new PortfolioSnapshot(Timestamp: 2, Cash: 900m, Margin: 100m, LotesVivos: new[] { new Lote(Cantidad: 10m, PrecioEntrada: 100m, Margin: 100m) }) },
        BranchResolutions: new[] { new BranchResolutionInfo(
            Timestamp: 2,
            TrayectoriaOficial: TrayectoriaResolucion.A,
            EquityA: 1050m,
            EquityB: 1040m,
            FillsA: new[] { new Fill(1, Side.Buy, 10m, 100m, 0.5m, 2, OrderType.Market) },
            FillsB: new[] { new Fill(1, Side.Sell, 5m, 99m, 0.5m, 2, OrderType.Market) }) });

    // spec: RNF-08 — un resultado completo se mapea campo a campo, sin perder listas
    [Fact]
    public void MapeaUnResultadoCompletoConTodasLasListasPobladas()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());

        Assert.Single(dto.Trades);
        Assert.Equal(10m, dto.Trades[0].CantidadInicial);
        Assert.Equal(100m, dto.Trades[0].PrecioApertura);
        Assert.Equal(110m, dto.Trades[0].PrecioCierre);
        Assert.Equal(100m, dto.Trades[0].RealizedPnL);
        Assert.Single(dto.FillLog);
        Assert.Equal(2, dto.EquityCurve.Count);
        Assert.Single(dto.PortfolioSnapshots);
        Assert.Single(dto.BranchResolutions);
        Assert.Equal(3, dto.ExperimentInfo.TotalVelas);
        Assert.Equal(1, dto.ExperimentInfo.FechaInicioTimestamp);
        Assert.Equal(3, dto.ExperimentInfo.FechaFinTimestamp);
    }

    // spec: RNF-05 — el mapeo no redondea ni pierde precision decimal
    [Fact]
    public void NoPierdePrecisionDecimalAlMapear()
    {
        var resultado = CrearResultadoDeEjemplo() with
        {
            EquityCurve = new[] { new EquityPoint(2, 900.123456789m, 100m, 50m, 1050.123456789m) }
        };

        var dto = ResultDtoMapper.Mapear(resultado, CrearConfigDeEjemplo());

        Assert.Equal(1050.123456789m, dto.EquityCurve[0].Equity);
    }

    // spec: RNF-08 — Side, OrderType y TrayectoriaResolucion se convierten a texto legible
    [Fact]
    public void ConvierteSideYTipoOrdenYTrayectoriaAString()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());

        Assert.Equal("Buy", dto.FillLog[0].Side);
        Assert.Equal("Market", dto.FillLog[0].TipoOrdenOriginal);
        Assert.Equal("A", dto.BranchResolutions[0].TrayectoriaOficial);
    }

    // spec: RNF-08 — "Fill Log Minimo" exige Costo Friccion Real como dato obligatorio por Fill,
    // para que la simulacion sea deterministicamente reconstruible. FillLogEntryDto lo omitia:
    // Domain.Shared.Fill ya lo calcula (CostoFriccionReal), pero el mapeo lo descartaba en
    // silencio, dejando al consumidor del JSON sin forma de conocer el costo de friccion real
    // de cada Fill (ni de distinguirlo de PrecioFill, que no lo incluye).
    [Fact]
    public void PropagaElCostoDeFriccionRealDeCadaFillAlLog()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());

        Assert.Equal(0.5m, dto.FillLog[0].CostoFriccionReal);
    }

    // spec: RNF-09 — un resultado no-Success (listas vacias) se mapea sin lanzar excepcion, y
    // el Estado permite distinguirlo de un experimento valido con cero actividad.
    [Fact]
    public void UnResultadoConListasVaciasProduceUnResultDtoConListasVacias()
    {
        var resultadoVacio = new ResultadoBacktest(
            EstadoBacktest.NotEvaluable, Array.Empty<Fill>(), 0m, Array.Empty<Trade>(), Array.Empty<Order>(),
            Array.Empty<EquityPoint>(), Array.Empty<PortfolioSnapshot>(), Array.Empty<BranchResolutionInfo>());

        var dto = ResultDtoMapper.Mapear(resultadoVacio, CrearConfigDeEjemplo());

        Assert.Empty(dto.Trades);
        Assert.Empty(dto.FillLog);
        Assert.Empty(dto.EquityCurve);
        Assert.Equal(0m, dto.Metrics.EquityFinal);
        Assert.Equal(0, dto.Metrics.TotalTrades);
    }

    // spec: RN-11 — el mapeo conserva ambas ramas de una BranchResolution, sin mezclar A y B
    [Fact]
    public void MapeaAmbasRamasDeUnaBranchResolutionSinMezclarlas()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());
        var branch = dto.BranchResolutions[0];

        Assert.Equal(1050m, branch.EquityA);
        Assert.Equal(1040m, branch.EquityB);
        Assert.Single(branch.FillsA);
        Assert.Single(branch.FillsB);
        Assert.Equal("Buy", branch.FillsA[0].Side);
        Assert.Equal("Sell", branch.FillsB[0].Side);
    }

    // spec: RNF-08 — MetricsDto es un agregado de reporte sobre las listas ya mapeadas, sin
    // recalcular Equity ni reconstruir Trades
    [Fact]
    public void CalculaMetricsComoAgregadosDeLasListasYaMapeadas()
    {
        var resultado = CrearResultadoDeEjemplo() with
        {
            Trades = new[]
            {
                new Trade(10m, 100m, 110m, 100m),
                new Trade(5m, 50m, 45m, -25m)
            }
        };

        var dto = ResultDtoMapper.Mapear(resultado, CrearConfigDeEjemplo());

        Assert.Equal(75m, dto.Metrics.PnLTotal);
        Assert.Equal(2, dto.Metrics.TotalTrades);
        Assert.Equal(1060m, dto.Metrics.EquityFinal);
    }

    // spec: RNF-09 — el Estado del resultado se propaga al DTO, permitiendo distinguir un fallo
    // (InternalCrash) de un experimento valido con cero actividad
    [Fact]
    public void MapeaEstadoBacktestCorrectamente()
    {
        var resultadoCrash = new ResultadoBacktest(
            EstadoBacktest.InternalCrash, Array.Empty<Fill>(), 0m, Array.Empty<Trade>(), Array.Empty<Order>(),
            Array.Empty<EquityPoint>(), Array.Empty<PortfolioSnapshot>(), Array.Empty<BranchResolutionInfo>());

        var dto = ResultDtoMapper.Mapear(resultadoCrash, CrearConfigDeEjemplo());

        Assert.Equal("InternalCrash", dto.Estado);
        Assert.NotEqual("Success", dto.Estado);
    }

    // spec: RNF-09 — un Success con cero actividad real y un resultado no-Success producen
    // exactamente la misma forma de datos (Trades/EquityCurve vacios, Metrics en cero): la forma
    // estructural del payload NO alcanza para interpretar el resultado. Estado es el unico
    // discriminante valido; un consumidor que ignore Estado no puede distinguir "no gano nada"
    // de "no llego a evaluarse".
    [Fact]
    public void UnResultadoNoSuccessMantieneEstadoDistintoAunqueLaFormaDeDatosSeaIgual()
    {
        var configSinActividad = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });
        var resultadoSuccessSinActividad = new ResultadoBacktest(
            EstadoBacktest.Success, Array.Empty<Fill>(), 1000m, Array.Empty<Trade>(), Array.Empty<Order>(),
            Array.Empty<EquityPoint>(), Array.Empty<PortfolioSnapshot>(), Array.Empty<BranchResolutionInfo>());
        var resultadoNotEvaluable = new ResultadoBacktest(
            EstadoBacktest.NotEvaluable, Array.Empty<Fill>(), 0m, Array.Empty<Trade>(), Array.Empty<Order>(),
            Array.Empty<EquityPoint>(), Array.Empty<PortfolioSnapshot>(), Array.Empty<BranchResolutionInfo>());

        var dtoSuccess = ResultDtoMapper.Mapear(resultadoSuccessSinActividad, configSinActividad);
        var dtoNotEvaluable = ResultDtoMapper.Mapear(resultadoNotEvaluable, configSinActividad);

        Assert.Equal(dtoSuccess.Trades, dtoNotEvaluable.Trades);
        Assert.Equal(dtoSuccess.EquityCurve, dtoNotEvaluable.EquityCurve);
        Assert.Equal(dtoSuccess.Metrics.TotalTrades, dtoNotEvaluable.Metrics.TotalTrades);
        Assert.Equal(dtoSuccess.Metrics.PnLTotal, dtoNotEvaluable.Metrics.PnLTotal);
        Assert.Equal(dtoSuccess.Metrics.EquityFinal, dtoNotEvaluable.Metrics.EquityFinal);
        Assert.NotEqual(dtoSuccess.Estado, dtoNotEvaluable.Estado);
        Assert.Equal("Success", dtoSuccess.Estado);
        Assert.Equal("NotEvaluable", dtoNotEvaluable.Estado);
    }

    // spec: RNF-05 — "redondeo exclusivo al final, Half-to-Even a 2 decimales; Equity_rep es la
    // suma estricta de sus componentes ya redondeados". RedondeoReporte.EquityReportado ya existe
    // y esta probado en aislamiento (tests/Domain.Tests/Precision/RedondeoDecimalTests.cs), pero
    // ResultDtoMapper.Mapear nunca lo invocaba: MetricsDto.EquityFinal tomaba el ultimo
    // EquityPoint.Equity crudo, sin aplicar el redondeo de reporte exigido por el SPEC.
    [Fact]
    public void MetricsEquityFinalAplicaElRedondeoDeReporteExigidoPorRnf05()
    {
        var resultado = CrearResultadoDeEjemplo() with
        {
            EquityCurve = new[] { new EquityPoint(Timestamp: 2, Cash: 100.005m, Margin: 50.005m, UnrealizedPnL: 0.005m, Equity: 150.015m) }
        };

        var dto = ResultDtoMapper.Mapear(resultado, CrearConfigDeEjemplo());

        var esperado = RedondeoReporte.EquityReportado(100.005m, 50.005m, 0.005m);
        Assert.Equal(esperado, dto.Metrics.EquityFinal);
        Assert.NotEqual(150.015m, dto.Metrics.EquityFinal);
    }

    // spec: RN-12, RNF-16 — Incapacidades se mapea desde ResultadoBacktest.IncapacidadesEfectivas,
    // lista vacia si no hubo ninguna (nunca null, mismo criterio que el resto de listas de ResultDto).
    [Fact]
    public void MapeaIncapacidadesDesdeElResultado()
    {
        var resultado = CrearResultadoDeEjemplo() with
        {
            Incapacidades = new[] { new RegistroIncapacidad(3, new OrderRequest(Side.Buy, OrderType.Market, 50m), 500m, 100m, Bloqueada: true) }
        };

        var dto = ResultDtoMapper.Mapear(resultado, CrearConfigDeEjemplo());

        Assert.Single(dto.Incapacidades);
        Assert.Equal(3, dto.Incapacidades[0].Timestamp);
        Assert.True(dto.Incapacidades[0].Bloqueada);
    }

    // spec: RNF-16 — un resultado sin incapacidades mapea una lista vacia, nunca null.
    [Fact]
    public void MapeaIncapacidadesVaciaCuandoNoHuboNinguna()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());

        Assert.Empty(dto.Incapacidades);
    }

    // spec: RNF-16 — ExposicionFinalDto distingue PnL realizado (Trades cerrados) de resultado
    // incluyendo posiciones vivas (Equity final, que ya incorpora UnrealizedPnL).
    [Fact]
    public void CalculaExposicionFinalConPosicionesVivas()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());

        Assert.Equal(10m, dto.Exposicion.CantidadNetaViva);
        Assert.Equal(100m, dto.Exposicion.MarginRetenido);
        Assert.Equal(60m, dto.Exposicion.UnrealizedPnL);
        Assert.Equal(100m, dto.Exposicion.PnLRealizado);
        Assert.Equal(160m, dto.Exposicion.ResultadoConPosicionesAbiertas);
    }

    // spec: RNF-16 — sin posiciones vivas ni PortfolioSnapshots, la exposicion final es cero y el
    // resultado con posiciones abiertas coincide exactamente con el PnL realizado.
    [Fact]
    public void CalculaExposicionFinalCeroCuandoNoHayPosicionesVivas()
    {
        var resultado = CrearResultadoDeEjemplo() with
        {
            PortfolioSnapshots = Array.Empty<PortfolioSnapshot>(),
            EquityCurve = Array.Empty<EquityPoint>()
        };

        var dto = ResultDtoMapper.Mapear(resultado, CrearConfigDeEjemplo());

        Assert.Equal(0m, dto.Exposicion.CantidadNetaViva);
        Assert.Equal(0m, dto.Exposicion.ResultadoConPosicionesAbiertas - dto.Exposicion.PnLRealizado);
    }

    // spec: RNF-16 — cuando hay posiciones vivas al cierre, Explicacion incluye la advertencia
    // aprobada en la decision de arquitectura (caso14, DECISIONES_ARQUITECTURA_VALIDACION_
    // RESULTADOS_BACKTEST_V1.md S4.3).
    [Fact]
    public void ExplicacionIncluyeAdvertenciaDePosicionesAbiertasCuandoHayExposicionViva()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());

        Assert.Equal(
            "El resultado incluye posiciones abiertas al finalizar la simulacion. La ganancia/perdida final puede variar si esas posiciones fueran cerradas.",
            dto.Explicacion!.AdvertenciaPosicionesAbiertas);
    }

    // spec: RNF-16 — sin posiciones vivas al cierre, no se puebla la advertencia (queda null).
    [Fact]
    public void ExplicacionNoIncluyeAdvertenciaDePosicionesAbiertasSinExposicionViva()
    {
        var resultado = CrearResultadoDeEjemplo() with
        {
            PortfolioSnapshots = Array.Empty<PortfolioSnapshot>(),
            EquityCurve = Array.Empty<EquityPoint>()
        };

        var dto = ResultDtoMapper.Mapear(resultado, CrearConfigDeEjemplo());

        Assert.Null(dto.Explicacion!.AdvertenciaPosicionesAbiertas);
    }

    // spec: RNF-16 — cuando Incapacidades no esta vacio, Explicacion incluye la advertencia de
    // incapacidad de capital.
    [Fact]
    public void ExplicacionIncluyeAdvertenciaDeIncapacidadCuandoHuboAlMenosUna()
    {
        var resultado = CrearResultadoDeEjemplo() with
        {
            Incapacidades = new[] { new RegistroIncapacidad(3, new OrderRequest(Side.Buy, OrderType.Market, 50m), 500m, 100m, Bloqueada: true) }
        };

        var dto = ResultDtoMapper.Mapear(resultado, CrearConfigDeEjemplo());

        Assert.NotNull(dto.Explicacion!.AdvertenciaIncapacidadCapital);
    }

    // spec: RNF-16 — sin ninguna incapacidad, no se puebla la advertencia (queda null).
    [Fact]
    public void ExplicacionNoIncluyeAdvertenciaDeIncapacidadSinNinguna()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());

        Assert.Null(dto.Explicacion!.AdvertenciaIncapacidadCapital);
    }

    // spec: RN-19, RNF-16 — con reporteRegimen no nulo, ResultDto.ReporteRegimen transporta las 3
    // fases con sus metricas (Regimen/TotalTrades/PnLTotal/WinRate) sin alterarlas, y RegimenOptimo
    // como texto legible (mismo criterio que DescribirRegimen ya usa para RegimenOptimoDescripcion).
    [Fact]
    public void MapeaReporteRegimenConLasTresFasesYRegimenOptimoComoTexto()
    {
        var reporte = new ReporteRegimenResultado(
            Fases: new[]
            {
                new FilaFaseRegimen(Regimen.Alcista, TotalTrades: 2, PnLTotal: 85m, WinRate: 0.5m),
                new FilaFaseRegimen(Regimen.Bajista, TotalTrades: 0, PnLTotal: 0m, WinRate: 0m),
                new FilaFaseRegimen(Regimen.Horizontal, TotalTrades: 1, PnLTotal: -10m, WinRate: 0m)
            },
            RegimenOptimo: Regimen.Alcista);

        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo(), reporteRegimen: reporte);

        Assert.NotNull(dto.ReporteRegimen);
        Assert.Equal(3, dto.ReporteRegimen!.Fases.Count);
        var filaAlcista = dto.ReporteRegimen.Fases.Single(f => f.Regimen == "Alcista");
        Assert.Equal(2, filaAlcista.TotalTrades);
        Assert.Equal(85m, filaAlcista.PnLTotal);
        Assert.Equal(0.5m, filaAlcista.WinRate);
        Assert.Equal("Alcista", dto.ReporteRegimen.RegimenOptimo);
    }

    // spec: RNF-16 — sin reporteRegimen (default null, ej. endpoints que no ejecutan este
    // analisis), ResultDto.ReporteRegimen permanece null: no se fuerza un objeto vacio.
    [Fact]
    public void NoMapeaReporteRegimenCuandoNoSePasaNinguno()
    {
        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo());

        Assert.Null(dto.ReporteRegimen);
    }

    // spec: RN-19 — sin Trades, el reporte sigue siendo un objeto valido con las 3 fases en cero,
    // no un ReporteRegimenDto completo en null; unicamente RegimenOptimo es null.
    [Fact]
    public void MapeaReporteRegimenSinTradesConFasesEnCeroYSinRegimenOptimo()
    {
        var reporte = new ReporteRegimenResultado(
            Fases: new[]
            {
                new FilaFaseRegimen(Regimen.Alcista, TotalTrades: 0, PnLTotal: 0m, WinRate: 0m),
                new FilaFaseRegimen(Regimen.Bajista, TotalTrades: 0, PnLTotal: 0m, WinRate: 0m),
                new FilaFaseRegimen(Regimen.Horizontal, TotalTrades: 0, PnLTotal: 0m, WinRate: 0m)
            },
            RegimenOptimo: null);

        var dto = ResultDtoMapper.Mapear(CrearResultadoDeEjemplo(), CrearConfigDeEjemplo(), reporteRegimen: reporte);

        Assert.NotNull(dto.ReporteRegimen);
        Assert.Equal(3, dto.ReporteRegimen!.Fases.Count);
        Assert.All(dto.ReporteRegimen.Fases, f => Assert.Equal(0, f.TotalTrades));
        Assert.Null(dto.ReporteRegimen.RegimenOptimo);
    }

    // spec: CU-24 — integracion de punta a punta: dataset con fases diferenciadas + Trades reales
    // en mas de una fase, verificando que el ReporteRegimenDto resultante del mapeo distingue
    // correctamente el desempeno por fase (no solo el mapeo aislado de un fixture manual).
    [Fact]
    public void ElReporteRegimenDePuntaAPuntaDistingueElDesempenoPorFase()
    {
        var velas = Enumerable.Range(0, 20)
            .Select(i => new Candle(i, 100m + 5m * i, 100m + 5m * i, 100m + 5m * i, 100m + 5m * i, 500m))
            .ToArray();
        var trades = new[]
        {
            new Trade(CantidadInicial: 1m, PrecioApertura: 100m, PrecioCierre: 195m, RealizedPnL: 95m, TimestampApertura: 19, TimestampCierre: 19),
            new Trade(CantidadInicial: 1m, PrecioApertura: 100m, PrecioCierre: 90m, RealizedPnL: -10m, TimestampApertura: 19, TimestampCierre: 19)
        };
        var reporte = ReporteRegimen.Calcular(trades, velas);
        var resultado = CrearResultadoDeEjemplo() with { Trades = trades };

        var dto = ResultDtoMapper.Mapear(resultado, CrearConfigDeEjemplo(), reporteRegimen: reporte);

        var filaAlcista = dto.ReporteRegimen!.Fases.Single(f => f.Regimen == "Alcista");
        Assert.Equal(2, filaAlcista.TotalTrades);
        Assert.Equal(85m, filaAlcista.PnLTotal);
        Assert.Equal("Alcista", dto.ReporteRegimen.RegimenOptimo);
    }
}
