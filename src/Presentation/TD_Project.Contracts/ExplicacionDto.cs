namespace TD_Project.Contracts;

// spec: RNF-16 — descripciones interpretativas en espanol para un usuario no experto, y aviso
// obligatorio de que los resultados provienen exclusivamente de simulacion historica.
public sealed record ExplicacionDto(
    string Resumen,
    string? RegimenOptimoDescripcion,
    string? GestorRecomendadoDescripcion,
    string AvisoSimulacionHistorica);
