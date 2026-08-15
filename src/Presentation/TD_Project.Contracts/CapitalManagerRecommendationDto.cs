namespace TD_Project.Contracts;

// spec: RN-18, CU-23 — contrato de entrada/salida del endpoint de recomendacion de gestores de
// capital. Reutiliza DatasetHash + EstrategiaDslJson (mismo par que CU-22): el sistema recomienda
// un gestor para una estrategia evaluada, nunca una estrategia (invariante de exclusion RN-18).
public sealed record RecomendarGestorRequestDto(string DatasetHash, decimal CapitalInicial, string EstrategiaDslJson);

public sealed record ResultadoGestorDto(string IdentidadGestor, decimal PnLTotal, decimal MaxDrawdown, decimal Cr, bool CuentaLiquidada);

public sealed record RecomendarGestorResponseDto(IReadOnlyList<ResultadoGestorDto> Resultados, string? GestorRecomendado);
