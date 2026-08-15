using TD_Project.Application;
using TD_Project.Contracts;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Regimen;
using TD_Project.Domain.Shared;

namespace TD_Project.Api.Mapping;

// spec: RNF-08, RNF-13 — convierte el resultado interno de Application al contrato externo de
// Presentation. Solo conversion de tipos y agregados de reporte (suma/conteo/ultimo elemento);
// no recalcula Equity, no reconstruye Trades, no decide TrayectoriaOficial.
public static class ResultDtoMapper
{
    // spec: RNF-16 — Explicacion es opcional (null si la campana no ejecuto recomendacion de
    // gestor ni reporte de regimen, ej. el endpoint demo historico ya auditado en caso11), para no
    // alterar el comportamiento de Mapear(resultado, config) ya existente.
    public static ResultDto Mapear(
        ResultadoBacktest resultado,
        ConfiguracionExperimento config,
        RecomendacionGestorResultado? recomendacion = null,
        ReporteRegimenResultado? reporteRegimen = null)
    {
        var equityCurve = resultado.EquityCurve.Select(MapearEquityPoint).ToList();
        var trades = resultado.Trades.Select(MapearTrade).ToList();
        var fillLog = resultado.Fills.Select(MapearFill).ToList();
        var portfolioSnapshots = resultado.PortfolioSnapshots.Select(MapearPortfolioSnapshot).ToList();
        var branchResolutions = resultado.BranchResolutions.Select(MapearBranchResolution).ToList();
        var pnlTotal = trades.Sum(t => t.RealizedPnL);

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
                PnLTotal: pnlTotal,
                TotalTrades: trades.Count),
            BranchResolutions: branchResolutions,
            Explicacion: MapearExplicacion(pnlTotal, recomendacion, reporteRegimen));
    }

    // spec: RNF-16 — descripciones interpretativas en espanol para un usuario no experto, con el
    // aviso obligatorio de simulacion historica en todo reporte (restriccion general del delta).
    private static ExplicacionDto MapearExplicacion(
        decimal pnlTotal, RecomendacionGestorResultado? recomendacion, ReporteRegimenResultado? reporteRegimen)
    {
        var resumen = pnlTotal >= 0m
            ? $"La estrategia obtuvo un resultado positivo de {pnlTotal:0.##} en la simulacion."
            : $"La estrategia obtuvo un resultado negativo de {pnlTotal:0.##} en la simulacion.";

        var regimenOptimoDescripcion = reporteRegimen?.RegimenOptimo is { } regimen
            ? $"Mejor desempeno observado en fase {DescribirRegimen(regimen)}."
            : null;

        // spec: RN-18 — invariante de exclusion: se describe el gestor recomendado, nunca una
        // estrategia recomendada.
        var gestorRecomendadoDescripcion = recomendacion?.GestorRecomendado is { } gestor
            ? $"Gestor de capital recomendado para esta estrategia: {gestor}."
            : recomendacion is not null
                ? "Ningun gestor de capital evaluado evito la liquidacion de la cuenta para esta estrategia."
                : null;

        return new ExplicacionDto(
            Resumen: resumen,
            RegimenOptimoDescripcion: regimenOptimoDescripcion,
            GestorRecomendadoDescripcion: gestorRecomendadoDescripcion,
            AvisoSimulacionHistorica: "Los resultados corresponden a simulacion historica y no garantizan resultados futuros.");
    }

    private static string DescribirRegimen(Regimen regimen) => regimen switch
    {
        Regimen.Alcista => "Alcista",
        Regimen.Bajista => "Bajista",
        Regimen.Horizontal => "Horizontal",
        _ => regimen.ToString()
    };

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
