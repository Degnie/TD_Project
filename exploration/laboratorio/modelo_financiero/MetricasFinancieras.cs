namespace TD_Project.ModeloFinanciero;

// spec: Caso 2 D-072/D-073/D-075/D-077/D-078 — todo campo se deriva exclusivamente de
// ResultadoBacktest y CapitalInicial (fuente oficial). DrawdownMaximoPct es decimal? porque puede
// no existir fuente valida (EquityCurve vacia) — null nunca se confunde con 0m (D-078).
public sealed record MetricasFinancieras(
    decimal CapitalInicial,
    decimal CashFinal,
    decimal EquityFinal,
    decimal PnLTotal,
    decimal? DrawdownMaximoPct,
    decimal ExposicionMaxima);
