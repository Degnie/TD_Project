namespace TD_Project.Application;

// spec: RN-18 — resultado de evaluar un Gestor de Capital aislado: identidad declarada
// (nunca el nombre de una estrategia — invariante de exclusion de RN-18), PnL total, MaxDrawdown
// absoluto y CR = PnLTotal / (MaxDrawdown + 1).
public sealed record ResultadoGestorEvaluado(string IdentidadGestor, decimal PnLTotal, decimal MaxDrawdown, decimal Cr, bool CuentaLiquidada);

// spec: RN-18 — GestorRecomendado es null si todos los gestores evaluados liquidaron la cuenta
// (Equity <= 0 en algun punto de la simulacion): "recomendacion de inadaptabilidad".
public sealed record RecomendacionGestorResultado(IReadOnlyList<ResultadoGestorEvaluado> Resultados, string? GestorRecomendado);
