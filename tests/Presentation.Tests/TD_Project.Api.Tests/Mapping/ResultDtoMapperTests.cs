using TD_Project.Api.Mapping;
using TD_Project.Application;
using TD_Project.Domain.Portfolio;
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
}
