namespace TD_Project.Contracts;

// spec: RNF-16 — descripciones interpretativas en espanol para un usuario no experto, y aviso
// obligatorio de que los resultados provienen exclusivamente de simulacion historica.
// spec: RNF-16, caso14 — advertencias explicativas cuando el backtest cierra con posiciones
// vivas o con ordenes bloqueadas por capacidad (texto aprobado en DECISIONES_ARQUITECTURA_
// VALIDACION_RESULTADOS_BACKTEST_V1.md S4.3).
public sealed record ExplicacionDto(
    string Resumen,
    string? RegimenOptimoDescripcion,
    string? GestorRecomendadoDescripcion,
    string AvisoSimulacionHistorica,
    string? AdvertenciaPosicionesAbiertas = null,
    string? AdvertenciaIncapacidadCapital = null);
