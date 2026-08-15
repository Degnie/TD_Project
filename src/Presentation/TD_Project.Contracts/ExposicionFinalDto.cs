namespace TD_Project.Contracts;

// spec: RNF-16 — distingue PnL realizado (Trades cerrados) de resultado incluyendo posiciones
// vivas al cierre (Equity, que ya incorpora UnrealizedPnL).
public sealed record ExposicionFinalDto(
    decimal CantidadNetaViva,
    decimal MarginRetenido,
    decimal UnrealizedPnL,
    decimal PnLRealizado,
    decimal ResultadoConPosicionesAbiertas);
