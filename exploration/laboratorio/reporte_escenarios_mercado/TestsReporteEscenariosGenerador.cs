using TD_Project.AnalisisEscenariosMercado;
using TD_Project.EvaluacionMultiTf;

namespace TD_Project.ReporteEscenariosMercado;

// Fase 1.5-B: pruebas requeridas antes de cerrar ReporteEscenariosGenerador. Verifica estructura y
// contenido de texto, no vuelve a calcular metricas (MetricasPorEscenario ya probado en Paso 3).
public static class TestsReporteEscenariosGenerador
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos()
    {
        var detalles = new List<string>();
        var pasaron = 0;
        var total = 0;

        void Caso(string nombre, Action verificacion)
        {
            total++;
            try
            {
                verificacion();
                pasaron++;
                detalles.Add($"[PASA] {nombre}");
            }
            catch (Exception ex)
            {
                detalles.Add($"[FALLA] {nombre}: {ex.Message}");
            }
        }

        Caso("Estructura — el reporte contiene los 4 bloques en el orden definido por V2 §3",
            VerificarEstructuraDeBloques);
        Caso("Ambiguo vs Sin régimen — ambas etiquetas aparecen diferenciadas cuando ambas categorías tienen operaciones",
            VerificarDiferenciacionAmbiguoSinRegimen);
        Caso("Nota D-037 — el texto obligatorio de correlación != causalidad siempre aparece, sin importar los datos",
            VerificarNotaD037SiempreAparece);
        Caso("Sin conclusión comparativa (D-047) — el texto no contiene frases de juicio/ranking/causalidad prohibidas",
            VerificarSinFrasesProhibidas);
        Caso("Integridad visible — el total de cada vista coincide con el resumen general mostrado en el bloque 1",
            VerificarIntegridadVisible);

        return (total, pasaron, detalles);
    }

    private static IdentidadExperimento IdentidadDePrueba() => new(
        Dataset: "sintetico-test", Timeframe: "1m", Estrategia: "Tres Mosqueteros",
        CapitalInicial: 1000m, AggregationVersion: "test", SourceSha256: "test", TimeframeSha256: "test",
        FechaEjecucionUtc: DateTime.UtcNow);

    private static OperacionConRegimen Op(int id, bool gano, int martingalas, Escenario? entrada, Escenario? resolucion) =>
        new(id, gano, martingalas, TimestampEntrada: id * 10, TimestampResolucion: id * 10 + 1, entrada, resolucion);

    private static void VerificarEstructuraDeBloques()
    {
        var operaciones = new[] { Op(1, true, 0, Escenario.Alcista, Escenario.Alcista) };
        var metricas = MetricasPorEscenario.Calcular(operaciones);
        var texto = ReporteEscenariosGenerador.Generar(IdentidadDePrueba(), operaciones.Length, metricas);

        Assert(texto.Contains("1. Resumen general"), "Debe contener el bloque 1");
        Assert(texto.Contains("2. Vista por régimen de entrada"), "Debe contener el bloque 2");
        Assert(texto.Contains("3. Vista por régimen de resolución"), "Debe contener el bloque 3");
        Assert(texto.Contains("4. Nota metodológica obligatoria"), "Debe contener el bloque 4");

        var pos1 = texto.IndexOf("1. Resumen general", StringComparison.Ordinal);
        var pos2 = texto.IndexOf("2. Vista por régimen de entrada", StringComparison.Ordinal);
        var pos3 = texto.IndexOf("3. Vista por régimen de resolución", StringComparison.Ordinal);
        var pos4 = texto.IndexOf("4. Nota metodológica obligatoria", StringComparison.Ordinal);
        Assert(pos1 < pos2 && pos2 < pos3 && pos3 < pos4, "Los bloques deben aparecer en el orden 1-2-3-4");
    }

    private static void VerificarDiferenciacionAmbiguoSinRegimen()
    {
        var operaciones = new[]
        {
            Op(1, true, 0, Escenario.Ambiguo, Escenario.Alcista),
            Op(2, false, 0, null, Escenario.Alcista),
        };
        var metricas = MetricasPorEscenario.Calcular(operaciones);
        var texto = ReporteEscenariosGenerador.Generar(IdentidadDePrueba(), operaciones.Length, metricas);

        Assert(texto.Contains("Ambiguo"), "Debe mostrar la etiqueta Ambiguo");
        Assert(texto.Contains("Sin régimen"), "Debe mostrar la etiqueta Sin régimen");
        Assert(!texto.Contains("Ambiguo/Sin régimen") && !texto.Contains("AmbiguoSin régimen"),
            "Ambiguo y Sin régimen no deben aparecer combinados en una sola etiqueta");
    }

    private static void VerificarNotaD037SiempreAparece()
    {
        // Caso extremo: cero operaciones — la nota debe aparecer igual, no es condicional a los datos.
        var metricas = MetricasPorEscenario.Calcular(Array.Empty<OperacionConRegimen>());
        var texto = ReporteEscenariosGenerador.Generar(IdentidadDePrueba(), 0, metricas);

        Assert(texto.Contains("No demuestra que el régimen"), "La nota D-037 debe aparecer literalmente, incluso sin operaciones");
        Assert(texto.Contains("no están distribuidos de forma experimentalmente controlada"), "La nota D-037 debe estar completa");
    }

    private static void VerificarSinFrasesProhibidas()
    {
        var operaciones = new[]
        {
            Op(1, true, 0, Escenario.Alcista, Escenario.Alcista),
            Op(2, false, 2, Escenario.Lateral, Escenario.Bajista),
            Op(3, true, 1, Escenario.Ambiguo, null),
        };
        var metricas = MetricasPorEscenario.Calcular(operaciones);
        var texto = ReporteEscenariosGenerador.Generar(IdentidadDePrueba(), operaciones.Length, metricas).ToLowerInvariant();

        var frasesProhibidas = new[] { "mejor escenario", "peor escenario", "funciona mejor", "funciona peor", "conviene usar", "recomendado", "recomendacion", "recomendación" };
        foreach (var frase in frasesProhibidas)
            Assert(!texto.Contains(frase), $"El reporte no debe contener la frase prohibida '{frase}' (D-047/D-014/D-009)");
    }

    private static void VerificarIntegridadVisible()
    {
        var operaciones = new[]
        {
            Op(1, true, 0, Escenario.Alcista, Escenario.Alcista),
            Op(2, false, 2, Escenario.Bajista, Escenario.Lateral),
            Op(3, true, 1, null, Escenario.Ambiguo),
            Op(4, false, 0, Escenario.Lateral, null),
        };
        var metricas = MetricasPorEscenario.Calcular(operaciones);

        Assert(metricas.PorRegimenEntrada.TotalOperaciones == operaciones.Length, "Precondicion: vista entrada debe sumar el total (ya probado en Paso 3)");
        Assert(metricas.PorRegimenResolucion.TotalOperaciones == operaciones.Length, "Precondicion: vista resolucion debe sumar el total (ya probado en Paso 3)");

        var texto = ReporteEscenariosGenerador.Generar(IdentidadDePrueba(), operaciones.Length, metricas);

        Assert(texto.Contains($"Operaciones completadas de la corrida: {operaciones.Length}"), "El resumen general debe mostrar el total real");
        Assert(texto.Contains($"Total: {operaciones.Length} operaciones"), "Cada vista debe mostrar su propio total, igual al resumen general");
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
}
