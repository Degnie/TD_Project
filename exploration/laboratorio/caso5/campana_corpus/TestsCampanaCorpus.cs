namespace TD_Project.Caso5;

// spec: ESPECIFICACION_IMPLEMENTACION_CAMPANA_CORPUS_CASO5C_V1.md §8 — P1/P2, verificacion
// estructural previa a la ejecucion real. Si falla, ProgramCampanaCorpus.cs no ejecuta ninguna
// corrida (ver Program.cs, Environment.Exit(1) antes del bucle). P3 (cobertura de 6 comparaciones
// persistidas) se verifica en el propio Program.cs, despues de la ejecucion real — no aqui, porque
// depende de resultados reales de disco, no de estructura de codigo.
public static class TestsCampanaCorpus
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) VerificarEstructura(int cantidadEstrategias, int cantidadTimeframes, int cantidadGestores)
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

        Caso("P1 — Matriz fija de 18 ejecuciones internas (2 estrategias x 3 timeframes x 3 gestores)", () =>
        {
            var totalEjecuciones = cantidadEstrategias * cantidadTimeframes * cantidadGestores;
            if (totalEjecuciones != 18)
                throw new Exception($"Se esperaban 18 ejecuciones internas (2x3x3), se calcularon {totalEjecuciones} ({cantidadEstrategias}x{cantidadTimeframes}x{cantidadGestores}).");
        });

        Caso("P2 — Ausencia estructural de seleccion por resultado en el codigo fuente de la campana", VerificarAusenciaDeSeleccionPorResultado);

        return (total, pasaron, detalles);
    }

    // P2: ProgramCampanaCorpus.cs usa top-level statements (sin tipo nombrado propio sobre el que
    // aplicar reflexion de superficie publica, a diferencia de ComparadorGestores/
    // PersistidorComparaciones). La verificacion estructural equivalente es textual, acotada
    // exclusivamente al CUERPO del doble bucle foreach (donde se decide que combinacion ejecutar) —
    // no al archivo completo, para no confundir logica adaptativa dentro del bucle con
    // verificaciones legitimas de cobertura que corren despues (P3, fuera del bucle).
    private static void VerificarAusenciaDeSeleccionPorResultado()
    {
        var rutaPrograma = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ProgramCampanaCorpus.cs");
        rutaPrograma = Path.GetFullPath(rutaPrograma);
        if (!File.Exists(rutaPrograma))
            throw new Exception($"No se encontro ProgramCampanaCorpus.cs en '{rutaPrograma}' para verificar.");

        var codigo = File.ReadAllText(rutaPrograma);

        const string marcaInicio = "foreach (var estrategia in estrategias)";
        const string marcaFin = "// P3";
        var inicio = codigo.IndexOf(marcaInicio, StringComparison.Ordinal);
        var fin = codigo.IndexOf(marcaFin, StringComparison.Ordinal);
        if (inicio < 0 || fin < 0 || fin <= inicio)
            throw new Exception("No se pudo delimitar el cuerpo del bucle de ejecucion (marca de inicio/fin no encontrada) para verificar P2.");

        var cuerpoBucle = codigo.Substring(inicio, fin - inicio);
        if (!cuerpoBucle.Contains("foreach (var timeframe in timeframes)"))
            throw new Exception("ProgramCampanaCorpus.cs no contiene el doble bucle foreach esperado sobre la matriz fija.");

        var patronesProhibidos = new[]
        {
            "if (resultado", "if(resultado", "if (carpeta", "if(carpeta",
            "continue;", "break;", "while (", "while(",
            ".OrderBy", ".OrderByDescending", "Recomend", "Ranking", "MejorGestor",
        };

        foreach (var patron in patronesProhibidos)
        {
            if (cuerpoBucle.Contains(patron, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"El cuerpo del bucle de ejecucion contiene '{patron}' — posible logica adaptativa o de seleccion por resultado, no permitida en una campana declarativa.");
        }
    }
}
