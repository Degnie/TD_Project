using System.Reflection;
using TD_Project.Caso5.AnalisisCorpus;

namespace TD_Project.Caso5.AnalisisInterpretativo;

// spec: ESPECIFICACION_IMPLEMENTACION_ANALISIS_INTERPRETATIVO_CASO5C_V1.md §6 (D-124) — P1-P8.
// Mismo patron "Caso" ya usado en analisis_corpus/TestsAnalisisCorpus.cs.
public static class TestsAnalisisInterpretativo
{
    // spec: §4, salvaguarda 4 — ninguna plantilla de texto de DetectorRelaciones/
    // ProgramAnalisisInterpretativo puede contener lenguaje prescriptivo. Verificado por P6 sobre
    // el codigo fuente (no sobre datos del corpus).
    private static readonly string[] TerminosPrescriptivosProhibidos =
        { "debe ", "debería", "debera", "recomendado", "recomendable", "óptimo", "optimo", "mejor ", "usar ", "elegir", "preferible" };

    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodas(string dirResultadosReal, string rutaManifiestoReal)
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

        Caso("P1 — CruzarDimensiones no pierde ni inventa combinaciones", VerificarP1);
        Caso("P2 — CruzarDimensiones nunca devuelve una sola combinacion destacada", VerificarP2);
        Caso("P3 — Orden de combinaciones es orden de aparicion, no de valor", VerificarP3);
        Caso("P4 — AgruparPorPatron expone ambos lados sin solapamiento ni omision", VerificarP4);
        Caso("P5 — Ausencia estructural de ranking/seleccion en los tipos de salida (reflexion)", VerificarP5);
        Caso("P6 — Ausencia lexica de lenguaje prescriptivo en las plantillas de texto", VerificarP6);
        Caso("P7 — Trazabilidad completa a evidencia origen sobre el corpus real", () => VerificarP7(dirResultadosReal, rutaManifiestoReal));
        Caso("P8 — Ausencia estructural de llamadas a componentes de ejecucion (textual)", VerificarP8);

        return (total, pasaron, detalles);
    }

    private static List<FilaCorpus> FixtureBasico() => new()
    {
        new("EstA", "15m", "DatasetX", "gestorZ:v1", "Success", 10m, 0.9m, 1m, 1m, 100m, 100m, "c1"),
        new("EstA", "1h", "DatasetX", "gestorA:v1", "Success", 5m, 0.2m, 1m, 1m, 100m, 100m, "c2"),
        new("EstB", "15m", "DatasetX", "gestorZ:v1", "Success", 0m, 0.99m, 1m, 1m, 100m, 100m, "c3"),
    };

    private static void VerificarP1()
    {
        var filas = FixtureBasico();
        var relacion = DetectorRelaciones.CruzarDimensiones(filas, "PnLTotal", new[] { "Estrategia", "Timeframe" });
        if (relacion.Combinaciones.Count != 3)
            throw new Exception($"Se esperaban 3 combinaciones (EstA/15m, EstA/1h, EstB/15m), se obtuvieron {relacion.Combinaciones.Count}.");
        var estA15m = relacion.Combinaciones.Single(c => c.Dimensiones["Estrategia"] == "EstA" && c.Dimensiones["Timeframe"] == "15m");
        if (estA15m.Estadistica is null || estA15m.Estadistica.Cantidad != 1 || estA15m.Estadistica.Minimo != 10m)
            throw new Exception("La combinacion EstA/15m no coincide con el fixture (esperado 1 fila, PnLTotal=10).");
    }

    private static void VerificarP2()
    {
        var filas = FixtureBasico();
        var relacion = DetectorRelaciones.CruzarDimensiones(filas, "PnLTotal", new[] { "Estrategia" });
        if (relacion.Combinaciones.Count != 2)
            throw new Exception($"Se esperaban 2 combinaciones (EstA, EstB), se obtuvieron {relacion.Combinaciones.Count} — RelacionObservada.Combinaciones debe listar todas, nunca una sola destacada.");
    }

    private static void VerificarP3()
    {
        var filas = new List<FilaCorpus>
        {
            new("EstZ", "1h", "DatasetX", "g:v1", "Success", 999m, 0.1m, 1m, 1m, 1m, 1m, "c1"),
            new("EstA", "1h", "DatasetX", "g:v1", "Success", 1m, 0.1m, 1m, 1m, 1m, 1m, "c2"),
            new("EstM", "1h", "DatasetX", "g:v1", "Success", 500m, 0.1m, 1m, 1m, 1m, 1m, "c3"),
        };
        var relacion = DetectorRelaciones.CruzarDimensiones(filas, "PnLTotal", new[] { "Estrategia" });
        var ordenObtenido = relacion.Combinaciones.Select(c => c.Dimensiones["Estrategia"]).ToList();
        var ordenEsperado = new[] { "EstZ", "EstA", "EstM" };
        if (!ordenObtenido.SequenceEqual(ordenEsperado))
            throw new Exception($"El orden deberia ser el de insercion ({string.Join(",", ordenEsperado)}), fue ({string.Join(",", ordenObtenido)}).");
    }

    private static void VerificarP4()
    {
        var filas = FixtureBasico();
        bool EsDrawdownAlto(FilaCorpus f) => f.DrawdownMaximoPct is { } d && d >= 0.5m;
        var agrupacion = DetectorRelaciones.AgruparPorPatron(filas, "DrawdownAlto", EsDrawdownAlto);

        if (agrupacion.DondeAparece.Count + agrupacion.DondeNoAparece.Count != filas.Count)
            throw new Exception("DondeAparece + DondeNoAparece debe cubrir exactamente el universo de filas evaluadas, sin solapamiento ni omision.");
        if (agrupacion.DondeAparece.Count != 2)
            throw new Exception($"Se esperaban 2 filas con drawdown>=0.5 (EstA/15m=0.9, EstB/15m=0.99), se obtuvieron {agrupacion.DondeAparece.Count}.");
        if (agrupacion.DondeNoAparece.Count != 1)
            throw new Exception($"Se esperaba 1 fila sin el patron (EstA/1h=0.2), se obtuvieron {agrupacion.DondeNoAparece.Count}.");

        try
        {
            DetectorRelaciones.AgruparPorPatron(filas, "", EsDrawdownAlto);
            throw new Exception("AgruparPorPatron deberia fallar explicitamente con nombrePatron vacio.");
        }
        catch (ArgumentException) { /* esperado */ }
    }

    private static void VerificarP5()
    {
        var prohibidos = new[] { "mejor", "ganador", "ranking", "score", "recomend", "winner", "best", "leader", "top", "optim" };
        var tipos = new[]
        {
            typeof(CombinacionObservada), typeof(RelacionObservada), typeof(AgrupacionPorPatron),
            typeof(CondicionesDeAparicion), typeof(ConsistenciaEntrePeriodos),
        };

        foreach (var tipo in tipos)
        {
            foreach (var prop in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var nombreLower = prop.Name.ToLowerInvariant();
                foreach (var p in prohibidos)
                {
                    if (nombreLower.Contains(p))
                        throw new Exception($"{tipo.Name}.{prop.Name} contiene el termino prohibido '{p}'.");
                }
            }
        }

        foreach (var metodo in typeof(DetectorRelaciones).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var nombreLower = metodo.Name.ToLowerInvariant();
            foreach (var p in prohibidos)
            {
                if (nombreLower.Contains(p))
                    throw new Exception($"DetectorRelaciones.{metodo.Name} contiene el termino prohibido '{p}'.");
            }
        }
    }

    private static void VerificarP6()
    {
        var rutaCarpeta = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var archivos = new[] { "DetectorRelaciones.cs", "ProgramAnalisisInterpretativo.cs" }
            .Select(nombre => Directory.GetFiles(rutaCarpeta, nombre, SearchOption.AllDirectories).FirstOrDefault())
            .Where(p => p is not null)
            .ToList();

        if (archivos.Count != 2)
            throw new Exception($"No se pudieron localizar DetectorRelaciones.cs/ProgramAnalisisInterpretativo.cs bajo '{rutaCarpeta}' para verificar P6.");

        foreach (var archivo in archivos)
        {
            var codigoLower = File.ReadAllText(archivo!).ToLowerInvariant();
            foreach (var termino in TerminosPrescriptivosProhibidos)
            {
                if (codigoLower.Contains(termino))
                    throw new Exception($"'{Path.GetFileName(archivo)}' contiene el termino prescriptivo '{termino.Trim()}' — el codigo no debe generar lenguaje prescriptivo (D-124).");
            }
        }
    }

    private static void VerificarP7(string dirResultadosReal, string rutaManifiestoReal)
    {
        var (filas, _) = LectorCorpus.Leer(dirResultadosReal, rutaManifiestoReal);
        if (filas.Count == 0)
            throw new Exception($"No se encontraron filas en el corpus real ('{dirResultadosReal}') — no se puede verificar P7 sin evidencia persistida.");

        var relacion = DetectorRelaciones.CruzarDimensiones(filas, "DrawdownMaximoPct", new[] { "Estrategia", "Gestor" });

        var manifiestoTexto = File.ReadAllText(rutaManifiestoReal);
        foreach (var combinacion in relacion.Combinaciones)
        {
            if (combinacion.CarpetasOrigen.Count == 0)
                throw new Exception($"Combinacion [{string.Join(",", combinacion.Dimensiones.Select(kv => $"{kv.Key}={kv.Value}"))}] no tiene CarpetasOrigen — trazabilidad rota.");
            foreach (var carpeta in combinacion.CarpetasOrigen)
            {
                var nombreCarpeta = Path.GetFileName(carpeta);
                if (!Directory.Exists(carpeta))
                    throw new Exception($"CarpetaOrigen '{carpeta}' no existe fisicamente.");
                if (!manifiestoTexto.Contains($"\"{nombreCarpeta}\""))
                    throw new Exception($"CarpetaOrigen '{nombreCarpeta}' no aparece en el manifiesto — trazabilidad rota.");
            }
        }
    }

    private static void VerificarP8()
    {
        var rutaCarpeta = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var archivos = new[] { "DetectorRelaciones.cs" }
            .Select(nombre => Directory.GetFiles(rutaCarpeta, nombre, SearchOption.AllDirectories).FirstOrDefault())
            .Where(p => p is not null)
            .ToList();

        if (archivos.Count != 1)
            throw new Exception($"No se pudo localizar DetectorRelaciones.cs bajo '{rutaCarpeta}' para verificar P8.");

        var prohibidos = new[] { "ComparadorGestores.Comparar", "EjecutorProtocolo.Ejecutar", "PersistidorComparaciones", "CrearEstrategia", "IStrategy" };
        var codigo = File.ReadAllText(archivos[0]!);
        foreach (var patron in prohibidos)
        {
            if (codigo.Contains(patron, StringComparison.Ordinal))
                throw new Exception($"'DetectorRelaciones.cs' contiene una referencia prohibida a '{patron}' — esta capa no debe ejecutar backtests ni tocar Capa 1.");
        }
    }
}
