namespace TD_Project.AnalisisEscenariosMercado;

// Fase 1.4-A (EVALUACION_CLASIFICADORES_REGIMEN_V1.md): categorias comunes a los 3 candidatos
// experimentales. "Ambiguo" es una categoria propia, nunca se fuerza una ventana sin tendencia
// clara a "Lateral" (ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §5).
public enum Escenario { Alcista, Bajista, Lateral, Ambiguo }

// Una clasificacion por ventana de N velas consecutivas del dataset. InicioUtcMs/FinUtcMs delimitan
// la ventana para poder medir estabilidad temporal y cobertura sin volver a tocar el dataset crudo.
public readonly record struct VentanaClasificada(long InicioUtcMs, long FinUtcMsExclusivo, Escenario Escenario);
