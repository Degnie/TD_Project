namespace TD_Project.DatosReales;

// Vela tal como llega del CSV crudo, ANTES de pasar el validador de integridad. No es un
// TD_Project.Domain.Shared.Candle: esa conversion solo ocurre para datos ya aceptados como
// dataset valido (ver PLAN_FASE2A.md, seccion 1 — separacion raw/ vs datasets/reales/).
public sealed record VelaCruda(long TimestampUtcMs, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
