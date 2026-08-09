using TD_Project.Contracts;
using Xunit;

namespace TD_Project.Contracts.Tests;

public class ResultDtoTests
{
    private static ResultDto CrearResultDtoDeEjemplo() => new(
        ExperimentInfo: new ExperimentInfoDto(FechaInicioTimestamp: 1, FechaFinTimestamp: 10, TotalVelas: 10),
        EquityCurve: new[] { new EquityPointDto(Timestamp: 2, Cash: 900m, Margin: 100m, UnrealizedPnL: 50m, Equity: 1050m) },
        Trades: new[] { new TradeDto(CantidadInicial: 10m, PrecioApertura: 100m, PrecioCierre: 110m, RealizedPnL: 100m) },
        FillLog: new[] { new FillLogEntryDto(SecuenciaCausal: 1, Side: "Buy", Cantidad: 10m, PrecioFill: 100m, VelaTimestamp: 2, TipoOrdenOriginal: "Market") },
        PortfolioSnapshots: new[] { new PortfolioSnapshotDto(Timestamp: 2, Cash: 900m, Margin: 100m, LotesVivos: new[] { new LoteDto(Cantidad: 10m, PrecioEntrada: 100m, Margin: 100m) }) },
        Metrics: new MetricsDto(EquityFinal: 1050m, PnLTotal: 100m, TotalTrades: 1),
        BranchResolutions: new[] { new BranchResolutionDto(Timestamp: 2, TrayectoriaOficial: "A", EquityA: 1050m, EquityB: 1040m, FillsA: Array.Empty<FillLogEntryDto>(), FillsB: Array.Empty<FillLogEntryDto>()) });

    // spec: RNF-08 — el contrato de salida de Presentation transporta la trazabilidad completa
    // del resultado (EquityCurve, Trades, FillLog, PortfolioSnapshots, BranchResolutions)
    [Fact]
    public void ResultDtoSeConstruyeConTodosLosCamposDeEjemplo()
    {
        var resultado = CrearResultDtoDeEjemplo();

        Assert.Equal(10, resultado.ExperimentInfo.TotalVelas);
        Assert.Single(resultado.EquityCurve);
        Assert.Single(resultado.Trades);
        Assert.Single(resultado.FillLog);
        Assert.Single(resultado.PortfolioSnapshots);
        Assert.Single(resultado.BranchResolutions);
    }

    // spec: RNF-13 — Deserializar(Serializar(x)) == x, aplicado al contrato externo de Presentation
    [Fact]
    public void ResultDtoSobreviveUnRoundTripDeSerializacionJson()
    {
        var resultado = CrearResultDtoDeEjemplo();

        var json = System.Text.Json.JsonSerializer.Serialize(resultado);
        var deserializado = System.Text.Json.JsonSerializer.Deserialize<ResultDto>(json);
        var jsonDelDeserializado = System.Text.Json.JsonSerializer.Serialize(deserializado);

        Assert.Equal(json, jsonDelDeserializado);
    }

    // spec: RNF-05 (precision) — el DTO transporta el decimal completo, sin redondear; el
    // redondeo a 2 decimales es responsabilidad del mapper/reporte, no de Contracts.
    [Fact]
    public void LaSerializacionNoPierdePrecisionDecimal()
    {
        var punto = new EquityPointDto(Timestamp: 1, Cash: 0m, Margin: 0m, UnrealizedPnL: 0m, Equity: 1234.56789012m);

        var json = System.Text.Json.JsonSerializer.Serialize(punto);
        var deserializado = System.Text.Json.JsonSerializer.Deserialize<EquityPointDto>(json);

        Assert.Equal(1234.56789012m, deserializado!.Equity);
    }
}
