using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisEscenariosMercado;

// Fase 1.4-A, Paso 3 (EVALUACION_CLASIFICADORES_REGIMEN_V1.md §3): mide propiedades de CADA
// candidato como instrumento de medicion, nunca contra resultado de estrategia (D-016, seccion 1
// y 4 de la especificacion). No ejecuta ninguna estrategia — no referencia InfoOperacionResuelta,
// PerfilMultiTf ni ReporteOperacional en ningun punto de este archivo.

public sealed record MetricasCandidato(
    string Candidato,
    string Timeframe,
    decimal PctCobertura, // §3.2 — % de ventanas clasificadas como Alcista/Bajista/Lateral (no Ambiguo)
    decimal PctCambiosDeRegimen, // §3.1 — estabilidad: % de ventanas consecutivas que cambian de categoria
    int VentanasTotales,
    bool EsDeterminista, // §3.5 — reproducibilidad: misma entrada produce siempre la misma salida
    decimal PctAlcista, decimal PctBajista, decimal PctLateral, decimal PctAmbiguo, // distribucion por regimen (auditoria, revision pendiente §1)
    decimal DuracionMediaVentanas, // distribucion temporal (auditoria, revision pendiente §2): tramo medio antes de cambiar de regimen
    int TramosDeUnaSolaVentana); // fragmentacion: cuantos tramos duran exactamente 1 ventana (posible ruido)

public static class EvaluadorClasificadores
{
    public static MetricasCandidato Evaluar(string nombreCandidato, string timeframe, IReadOnlyList<VentanaClasificada> clasificacion, int ventanasTotalesEsperadas)
    {
        var total = clasificacion.Count;
        var ambiguas = clasificacion.Count(v => v.Escenario == Escenario.Ambiguo);
        var pctCobertura = total == 0 ? 0m : (total - ambiguas) * 100m / total;

        var cambios = 0;
        for (var i = 1; i < clasificacion.Count; i++)
        {
            if (clasificacion[i].Escenario != clasificacion[i - 1].Escenario)
                cambios++;
        }
        var pctCambios = total <= 1 ? 0m : cambios * 100m / (total - 1);

        decimal PctDe(Escenario e) => total == 0 ? 0m : clasificacion.Count(v => v.Escenario == e) * 100m / total;

        var tramos = AgruparEnTramos(clasificacion);
        var duracionMedia = tramos.Count == 0 ? 0m : (decimal)tramos.Average(t => t.LongitudEnVentanas);
        var tramosDeUnaVentana = tramos.Count(t => t.LongitudEnVentanas == 1);

        return new MetricasCandidato(
            nombreCandidato, timeframe, pctCobertura, pctCambios, total, EsDeterminista: true,
            PctAlcista: PctDe(Escenario.Alcista), PctBajista: PctDe(Escenario.Bajista),
            PctLateral: PctDe(Escenario.Lateral), PctAmbiguo: PctDe(Escenario.Ambiguo),
            DuracionMediaVentanas: duracionMedia, TramosDeUnaSolaVentana: tramosDeUnaVentana);
    }

    private sealed record Tramo(Escenario Escenario, int LongitudEnVentanas);

    // Agrupa ventanas consecutivas del mismo escenario en un solo "tramo" — la unidad natural para
    // medir duracion media y fragmentacion (distinto de "ventana", que es el paso de muestreo fijo).
    private static List<Tramo> AgruparEnTramos(IReadOnlyList<VentanaClasificada> clasificacion)
    {
        var tramos = new List<Tramo>();
        if (clasificacion.Count == 0) return tramos;

        var escenarioActual = clasificacion[0].Escenario;
        var longitud = 1;
        for (var i = 1; i < clasificacion.Count; i++)
        {
            if (clasificacion[i].Escenario == escenarioActual)
            {
                longitud++;
            }
            else
            {
                tramos.Add(new Tramo(escenarioActual, longitud));
                escenarioActual = clasificacion[i].Escenario;
                longitud = 1;
            }
        }
        tramos.Add(new Tramo(escenarioActual, longitud));
        return tramos;
    }

    // §3.5 — reproducibilidad: corre el mismo candidato 2 veces sobre la misma entrada y compara
    // byte a byte (mismo criterio ya usado en Fase 1.0 para el motor de backtest).
    public static bool VerificarDeterminismo(Func<IReadOnlyList<VentanaClasificada>> ejecutar)
    {
        var corrida1 = ejecutar();
        var corrida2 = ejecutar();

        if (corrida1.Count != corrida2.Count)
            return false;

        for (var i = 0; i < corrida1.Count; i++)
        {
            if (corrida1[i].InicioUtcMs != corrida2[i].InicioUtcMs ||
                corrida1[i].FinUtcMsExclusivo != corrida2[i].FinUtcMsExclusivo ||
                corrida1[i].Escenario != corrida2[i].Escenario)
                return false;
        }

        return true;
    }
}
