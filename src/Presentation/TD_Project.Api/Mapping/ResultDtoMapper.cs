using TD_Project.Application;
using TD_Project.Contracts;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

namespace TD_Project.Api.Mapping;

// spec: RNF-08, RNF-13 — convierte el resultado interno de Application al contrato externo de
// Presentation. Solo conversion de tipos y agregados de reporte (suma/conteo/ultimo elemento);
// no recalcula Equity, no reconstruye Trades, no decide TrayectoriaOficial.
public static class ResultDtoMapper
{
    public static ResultDto Mapear(ResultadoBacktest resultado, ConfiguracionExperimento config)
    {
        var equityCurve = resultado.EquityCurve.Select(MapearEquityPoint).ToList();
        var trades = resultado.Trades.Select(MapearTrade).ToList();
        var fillLog = resultado.Fills.Select(MapearFill).ToList();
        var portfolioSnapshots = resultado.PortfolioSnapshots.Select(MapearPortfolioSnapshot).ToList();
        var branchResolutions = resultado.BranchResolutions.Select(MapearBranchResolution).ToList();

        return new ResultDto(
            Estado: resultado.Estado.ToString(),
            ExperimentInfo: new ExperimentInfoDto(
                FechaInicioTimestamp: config.Velas.Count > 0 ? config.Velas[0].Timestamp : 0,
                FechaFinTimestamp: config.Velas.Count > 0 ? config.Velas[^1].Timestamp : 0,
                TotalVelas: config.Velas.Count),
            EquityCurve: equityCurve,
            Trades: trades,
            FillLog: fillLog,
            PortfolioSnapshots: portfolioSnapshots,
            Metrics: new MetricsDto(
                EquityFinal: resultado.EquityCurve.Count > 0 ? EquityFinalReportado(resultado.EquityCurve[^1]) : 0m,
                PnLTotal: trades.Sum(t => t.RealizedPnL),
                TotalTrades: trades.Count),
            BranchResolutions: branchResolutions);
    }

    private static EquityPointDto MapearEquityPoint(EquityPoint p) =>
        new(p.Timestamp, p.Cash, p.Margin, p.UnrealizedPnL, p.Equity);

    // spec: RNF-05 — Equity_rep = Cash_rep + Margin_rep + UnrealizedPnL_rep, redondeo Half-to-Even
    // a 2 decimales exclusivo al final. EquityCurve conserva precision completa (RNF-05 tambien
    // exige 8 decimales intermedios); solo el agregado de reporte en Metrics se redondea.
    private static decimal EquityFinalReportado(EquityPoint ultimo) =>
        RedondeoReporte.EquityReportado(ultimo.Cash, ultimo.Margin, ultimo.UnrealizedPnL);

    private static TradeDto MapearTrade(Trade t) =>
        new(t.CantidadInicial, t.PrecioApertura, t.PrecioCierre, t.RealizedPnL);

    private static FillLogEntryDto MapearFill(Fill f) =>
        new(f.SecuenciaCausal, f.Side.ToString(), f.Cantidad, f.PrecioFill, f.CostoFriccionReal, f.Timestamp, f.TipoOrdenOriginal.ToString());

    private static LoteDto MapearLote(Lote l) =>
        new(l.Cantidad, l.PrecioEntrada, l.Margin);

    private static PortfolioSnapshotDto MapearPortfolioSnapshot(PortfolioSnapshot s) =>
        new(s.Timestamp, s.Cash, s.Margin, s.LotesVivos.Select(MapearLote).ToList());

    private static BranchResolutionDto MapearBranchResolution(BranchResolutionInfo b) =>
        new(b.Timestamp, b.TrayectoriaOficial.ToString(), b.EquityA, b.EquityB,
            b.FillsA.Select(MapearFill).ToList(), b.FillsB.Select(MapearFill).ToList());
}
