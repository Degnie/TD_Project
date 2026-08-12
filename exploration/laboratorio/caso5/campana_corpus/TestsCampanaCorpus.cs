namespace TD_Project.Caso5;

// spec: ESPECIFICACION_IMPLEMENTACION_CAMPANA_CORPUS_CASO5C_V1.md §8,
// ESPECIFICACION_IMPLEMENTACION_EXPANSION_CORPUS_CASO5C_V2.md §4 — P1/P2/P3, verificacion
// estructural previa a la ejecucion real. Si falla, ProgramCampanaCorpus.cs no ejecuta ninguna
// corrida (ver Program.cs, Environment.Exit(1) antes de los bloques). P4/P5 (cobertura de
// expansion, evidencia parcial) se verifican en el propio Program.cs, despues de la ejecucion
// real — no aqui, porque dependen de resultados reales de disco/memoria, no de estructura de
// codigo.
public static class TestsCampanaCorpus
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) VerificarEstructura(
        int cantidadEstrategiasV1, int cantidadEstrategiasNuevas, int cantidadTimeframes, int cantidadGestores)
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

        Caso("P1 — Matriz de sub-campana A: 4 estrategias x 3 timeframes x 3 gestores = 36 ejecuciones internas", () =>
        {
            var total = cantidadEstrategiasNuevas * cantidadTimeframes * cantidadGestores;
            if (total != 36)
                throw new Exception($"Se esperaban 36 ejecuciones internas (4x3x3), se calcularon {total} ({cantidadEstrategiasNuevas}x{cantidadTimeframes}x{cantidadGestores}).");
        });

        Caso("P2 — Matriz de sub-campana B (repeticion V1): 2 estrategias x 3 timeframes x 3 gestores = 18 ejecuciones internas", () =>
        {
            var total = cantidadEstrategiasV1 * cantidadTimeframes * cantidadGestores;
            if (total != 18)
                throw new Exception($"Se esperaban 18 ejecuciones internas (2x3x3), se calcularon {total} ({cantidadEstrategiasV1}x{cantidadTimeframes}x{cantidadGestores}).");
        });

        Caso("P3 — Ausencia estructural de seleccion por resultado en el codigo fuente de la campana", VerificarAusenciaDeSeleccionPorResultado);

        return (total, pasaron, detalles);
    }

    // P3: ProgramCampanaCorpus.cs usa top-level statements (sin tipo nombrado propio sobre el que
    // aplicar reflexion de superficie publica). La verificacion estructural equivalente es
    // textual, acotada al cuerpo de la funcion local EjecutarMatriz (unico lugar donde se decide
    // que combinacion ejecutar en las sub-campanas V1/A/B) y al bloque de la sub-campana C — no al
    // archivo completo, para no confundir logica adaptativa con verificaciones legitimas de
    // cobertura (P4/P5) que corren despues.
    private static void VerificarAusenciaDeSeleccionPorResultado()
    {
        var rutaPrograma = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ProgramCampanaCorpus.cs");
        rutaPrograma = Path.GetFullPath(rutaPrograma);
        if (!File.Exists(rutaPrograma))
            throw new Exception($"No se encontro ProgramCampanaCorpus.cs en '{rutaPrograma}' para verificar.");

        var codigo = File.ReadAllText(rutaPrograma);

        const string marcaInicio = "List<string> EjecutarMatriz(";
        const string marcaFin = "// P4";
        var inicio = codigo.IndexOf(marcaInicio, StringComparison.Ordinal);
        var fin = codigo.IndexOf(marcaFin, StringComparison.Ordinal);
        if (inicio < 0 || fin < 0 || fin <= inicio)
            throw new Exception("No se pudo delimitar el cuerpo de ejecucion (EjecutarMatriz + sub-campana C) para verificar P3.");

        var cuerpoEjecucion = codigo.Substring(inicio, fin - inicio);
        if (!cuerpoEjecucion.Contains("foreach (var estrategia in estrategias)") || !cuerpoEjecucion.Contains("foreach (var timeframe in timeframes)"))
            throw new Exception("ProgramCampanaCorpus.cs no contiene el doble bucle foreach esperado dentro de EjecutarMatriz.");

        var patronesProhibidos = new[]
        {
            "if (resultado", "if(resultado", "if (carpeta", "if(carpeta",
            "continue;", "break;", "while (", "while(",
            ".OrderBy", ".OrderByDescending", "Recomend", "Ranking", "MejorGestor",
        };

        foreach (var patron in patronesProhibidos)
        {
            if (cuerpoEjecucion.Contains(patron, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"El cuerpo de ejecucion contiene '{patron}' — posible logica adaptativa o de seleccion por resultado, no permitida en una campana declarativa.");
        }
    }
}
