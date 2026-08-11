using TD_Project.AnalisisOperacional;
using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisMultiTimeframe;

// Fase 1.3 (ESPECIFICACION_ANALISIS_MULTITIMEFRAME_V1.md): agrupa ReporteOperacional (Fase 1.2)
// por estrategia y expone comparacion descriptiva entre timeframes. No recalcula ninguna metrica
// del motor ni del AnalizadorOperacional — solo agrupa y presenta. D-008/D-009 siguen vigentes:
// no modifica BacktestRunner/IStrategy/AnalizadorOperacional, no calcula ranking financiero.

// spec-lab: ESPECIFICACION_ANALISIS_MULTITIMEFRAME_V1.md §6, D-010 — toda fila de comparacion
// incluye obligatoriamente el tamaño de muestra junto a la metrica, nunca la metrica sola.
public sealed record FilaTimeframe(
    string Timeframe,
    int IntentosCompletados,
    decimal EficienciaOperacionalPct,
    decimal PctResueltasPorMartingala,
    int MayorRachaNegativa,
    decimal RetornoPct); // dato derivado no financiero, ver DatosDerivadosModeloActual — nunca se ordena por este campo

// spec-lab: ESPECIFICACION_ANALISIS_MULTITIMEFRAME_V1.md §4 — rango/minimo/maximo/amplitud,
// sin clasificacion cualitativa (mismo criterio que D-005).
public sealed record Consistencia(
    string TimeframeMinimo, decimal ValorMinimo,
    string TimeframeMaximo, decimal ValorMaximo,
    decimal AmplitudPuntosPorcentuales);

// spec-lab: ESPECIFICACION_ANALISIS_MULTITIMEFRAME_V1.md §7 — las tres preguntas se calculan por
// separado y nunca se combinan en un unico indicador o ranking.
public sealed record MejorResultadoObservado(string Timeframe, decimal EficienciaOperacionalPct, int IntentosCompletados);
public sealed record MayorEvidencia(string Timeframe, int IntentosCompletados);

public sealed record PerfilMultiTimeframe(
    string Estrategia,
    IReadOnlyList<FilaTimeframe> Filas,
    Consistencia ConsistenciaEficienciaOperacional,
    MejorResultadoObservado MejorResultadoObservado,
    MayorEvidencia MayorEvidencia);

public static class ComparadorMultiTimeframe
{
    // Orden de entrada = orden de presentacion (ej. 1m, 5m, 15m, 1h, 4h, 1D). El comparador nunca
    // reordena las filas por valor de metrica — evita ranking implicito (ESPECIFICACION §5).
    public static PerfilMultiTimeframe Comparar(string estrategia, IReadOnlyList<PerfilMultiTf> perfilesEnOrden)
    {
        if (perfilesEnOrden.Count == 0)
            throw new ArgumentException("Se requiere al menos un timeframe para comparar.", nameof(perfilesEnOrden));

        var filas = perfilesEnOrden
            .Select(p =>
            {
                var reporte = AnalizadorOperacional.Analizar(p);
                return new FilaTimeframe(
                    Timeframe: p.Identidad.Timeframe,
                    IntentosCompletados: reporte.ResultadoGeneral.IntentosCompletados,
                    EficienciaOperacionalPct: reporte.ResultadoGeneral.EficienciaOperacionalPct,
                    PctResueltasPorMartingala: reporte.ResolucionDeIntentos.PctResueltasPorMartingala,
                    MayorRachaNegativa: reporte.PeoresEscenarios.MayorRachaNegativa,
                    RetornoPct: reporte.DatosDerivadosModeloActual.RetornoPct);
            })
            .ToList();

        var minimo = filas.MinBy(f => f.EficienciaOperacionalPct)!;
        var maximo = filas.MaxBy(f => f.EficienciaOperacionalPct)!;
        var consistencia = new Consistencia(
            TimeframeMinimo: minimo.Timeframe, ValorMinimo: minimo.EficienciaOperacionalPct,
            TimeframeMaximo: maximo.Timeframe, ValorMaximo: maximo.EficienciaOperacionalPct,
            AmplitudPuntosPorcentuales: maximo.EficienciaOperacionalPct - minimo.EficienciaOperacionalPct);

        // "Mejor resultado observado" = mayor valor puntual de Eficiencia operacional (D-011: eje
        // fijo por ahora, pendiente si el reporte debe permitir elegir otro eje en el futuro).
        var mejorResultado = new MejorResultadoObservado(maximo.Timeframe, maximo.EficienciaOperacionalPct, maximo.IntentosCompletados);

        var filaMayorEvidencia = filas.MaxBy(f => f.IntentosCompletados)!;
        var mayorEvidencia = new MayorEvidencia(filaMayorEvidencia.Timeframe, filaMayorEvidencia.IntentosCompletados);

        return new PerfilMultiTimeframe(estrategia, filas, consistencia, mejorResultado, mayorEvidencia);
    }
}
