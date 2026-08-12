namespace TD_Project.ModeloFinanciero;

// spec: Caso 2 D-072/D-073/D-075/D-077/D-078 — todo campo se deriva exclusivamente de
// ResultadoBacktest y CapitalInicial (fuente oficial). DrawdownMaximoPct es decimal? porque puede
// no existir fuente valida (EquityCurve vacia) — null nunca se confunde con 0m (D-078).
// spec: Caso 5A D-111 (DECISIONES_CASO5_V1.md) — ProfitFactor/CapitalLibreMinimo nuevos, alcance
// reducido de implementacion (RachaPositivaMaxima/DuracionDrawdown/RiesgoDeRuina diferidos,
// documentados como pendientes explicitos, no deuda bloqueante). MargenMaximoUtilizado NO se
// agrega como campo nuevo: es el mismo calculo que ExposicionMaxima ya tiene
// (PortfolioSnapshots.Max(s => s.Margin)) — equivalencia documentada, sin duplicar fuente (D-072).
public sealed record MetricasFinancieras(
    decimal CapitalInicial,
    decimal CashFinal,
    decimal EquityFinal,
    decimal PnLTotal,
    decimal? DrawdownMaximoPct,
    decimal ExposicionMaxima,
    decimal? ProfitFactor,
    decimal CapitalLibreMinimo);
