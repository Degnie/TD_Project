using System.Reflection;
using TD_Project.Caso5.AnalisisCorpus;

namespace TD_Project.Caso6.Recomendador;

// spec: ESPECIFICACION_IMPLEMENTACION_RECOMENDADOR_CASO6_V1.md §6 (D-128) — P1-P9 (numeracion tal
// cual el documento: P9 antes de P6-P8 en el texto, orden logico preservado aqui). Mismo patron
// "Caso" ya usado en analisis_corpus/TestsAnalisisCorpus.cs y analisis_interpretativo/
// TestsAnalisisInterpretativo.cs.
public static class TestsRecomendador
{
    // spec: §5 — ningun termino de juicio absoluto sin calificar. Verificado por P7 sobre el codigo
    // fuente (no sobre datos del corpus). Incluye la frase compuesta prohibida explicitamente por
    // el auditor.
    private static readonly string[] TerminosProhibidos =
        { "mejor", "óptimo", "optimo", "ganador", "ideal", "configuración recomendada", "configuracion recomendada" };

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

        Caso("P1 — Candidata cruza el umbral de su grupo y se reporta con su propio gestor", VerificarP1);
        Caso("P2 — La mediana se calcula sobre el conjunto de gestores del grupo, no por gestor aislado", VerificarP2);
        Caso("P3 — Orden de candidatas es orden de aparicion, no de valor", VerificarP3);
        Caso("P4 — Datasets distintos producen medianas independientes, evaluadas solo contra sus propios gestores", VerificarP4);
        Caso("P5 — Evidencia insuficiente produce Candidatas vacio, nunca recomendacion de baja confianza", VerificarP5);
        Caso("P9 — El grupo de comparacion nunca incluye el gestor en su clave", VerificarP9);
        Caso("P6 — Ausencia estructural de ranking/seleccion en los tipos y metodos (reflexion)", VerificarP6);
        Caso("P7 — Ausencia lexica de lenguaje prescriptivo/absoluto en el codigo fuente", VerificarP7);
        Caso("P8 — Trazabilidad completa a evidencia origen sobre el corpus real", () => VerificarP8(dirResultadosReal, rutaManifiestoReal));

        return (total, pasaron, detalles);
    }

    private static void VerificarP1()
    {
        // 3 gestores, misma combinacion Estrategia+Timeframe+Dataset. PnLTotal: 10, 20, 30 ->
        // mediana=20. Perfil Crecimiento (Mayor): deben cruzar el umbral gestorB (20) y gestorC (30).
        var filas = new List<FilaCorpus>
        {
            new("EstA", "1h", "DatasetX", "gestorA:v1", "Success", 10m, 0.1m, 1m, 1m, 100m, 100m, "c1"),
            new("EstA", "1h", "DatasetX", "gestorB:v1", "Success", 20m, 0.1m, 1m, 1m, 100m, 100m, "c2"),
            new("EstA", "1h", "DatasetX", "gestorC:v1", "Success", 30m, 0.1m, 1m, 1m, 100m, 100m, "c3"),
        };
        var r = MotorRecomendacion.Recomendar(filas, "Crecimiento");
        if (r.Candidatas.Count != 2)
            throw new Exception($"Se esperaban 2 candidatas (gestorB=20, gestorC=30 >= mediana 20), se obtuvieron {r.Candidatas.Count}.");
        if (!r.Candidatas.Any(c => c.IdentidadGestor == "gestorB:v1") || !r.Candidatas.Any(c => c.IdentidadGestor == "gestorC:v1"))
            throw new Exception("Las candidatas deben reportarse con su propio gestor especifico (gestorB, gestorC).");
    }

    private static void VerificarP2()
    {
        // La mediana debe calcularse sobre el conjunto {10,20,30} como UN solo grupo — no como 3
        // medianas de 1 elemento cada una (lo que haria que cada valor fuera su propia mediana y
        // las 3 filas cruzaran el umbral).
        var filas = new List<FilaCorpus>
        {
            new("EstA", "1h", "DatasetX", "gestorA:v1", "Success", 10m, 0.1m, 1m, 1m, 100m, 100m, "c1"),
            new("EstA", "1h", "DatasetX", "gestorB:v1", "Success", 20m, 0.1m, 1m, 1m, 100m, 100m, "c2"),
            new("EstA", "1h", "DatasetX", "gestorC:v1", "Success", 30m, 0.1m, 1m, 1m, 100m, 100m, "c3"),
        };
        var r = MotorRecomendacion.Recomendar(filas, "Crecimiento");
        if (r.Candidatas.Any(c => c.IdentidadGestor == "gestorA:v1"))
            throw new Exception("gestorA (valor=10) no deberia cruzar el umbral si la mediana se calcula sobre {10,20,30}=20 — indicio de que el grupo se particiono por gestor.");
        if (r.ConfiguracionesAnalizadas != 1)
            throw new Exception($"Se esperaba 1 grupo de comparacion (Estrategia+Timeframe+Dataset unico), se obtuvieron {r.ConfiguracionesAnalizadas}.");
    }

    private static void VerificarP3()
    {
        var filas = new List<FilaCorpus>
        {
            new("EstZ", "1h", "DatasetX", "g1:v1", "Success", 999m, 0.1m, 1m, 1m, 1m, 1m, "c1"),
            new("EstZ", "1h", "DatasetX", "g2:v1", "Success", 1m, 0.1m, 1m, 1m, 1m, 1m, "c2"),
            new("EstA", "1h", "DatasetX", "g1:v1", "Success", 500m, 0.1m, 1m, 1m, 1m, 1m, "c3"),
            new("EstA", "1h", "DatasetX", "g2:v1", "Success", 1m, 0.1m, 1m, 1m, 1m, 1m, "c4"),
        };
        var r = MotorRecomendacion.Recomendar(filas, "Crecimiento");
        var ordenObtenido = r.Candidatas.Select(c => c.Estrategia).ToList();
        var ordenEsperado = new[] { "EstZ", "EstA" };
        if (!ordenObtenido.SequenceEqual(ordenEsperado))
            throw new Exception($"El orden deberia ser el de aparicion en el corpus ({string.Join(",", ordenEsperado)}), fue ({string.Join(",", ordenObtenido)}).");
    }

    private static void VerificarP4()
    {
        // 2 datasets distintos, cada uno con sus propios gestores/valores -> 2 medianas
        // independientes, cada fila evaluada solo contra la mediana de su propio dataset.
        var filas = new List<FilaCorpus>
        {
            new("EstA", "1h", "DatasetX", "g1:v1", "Success", 10m, 0.1m, 1m, 1m, 1m, 1m, "c1"),
            new("EstA", "1h", "DatasetX", "g2:v1", "Success", 100m, 0.1m, 1m, 1m, 1m, 1m, "c2"),
            new("EstA", "1h", "DatasetY", "g1:v1", "Success", 5m, 0.1m, 1m, 1m, 1m, 1m, "c3"),
            new("EstA", "1h", "DatasetY", "g2:v1", "Success", 50m, 0.1m, 1m, 1m, 1m, 1m, "c4"),
        };
        var r = MotorRecomendacion.Recomendar(filas, "Crecimiento");
        if (r.ConfiguracionesAnalizadas != 2)
            throw new Exception($"Se esperaban 2 grupos de comparacion (DatasetX, DatasetY), se obtuvieron {r.ConfiguracionesAnalizadas}.");
        if (r.Candidatas.Count != 2)
            throw new Exception($"Se esperaban 2 candidatas (g2:v1 en cada dataset, valor mas alto de su propio grupo), se obtuvieron {r.Candidatas.Count}.");
        var datasetsCandidatas = r.Candidatas.Select(c => c.NombreDataset).Distinct().ToList();
        if (!datasetsCandidatas.Contains("DatasetX") || !datasetsCandidatas.Contains("DatasetY"))
            throw new Exception("Cada dataset debe evaluarse contra su propia mediana, no una mediana global.");
    }

    private static void VerificarP5()
    {
        var filas = new List<FilaCorpus>
        {
            new("EstA", "1h", "DatasetX", "g1:v1", "Fallo", null, null, null, null, null, null, "c1"),
        };
        var r = MotorRecomendacion.Recomendar(filas, "Crecimiento");
        if (r.Candidatas.Count != 0)
            throw new Exception("Sin evidencia con Estado=Success y metrica disponible, Candidatas debe quedar vacio — nunca una recomendacion de baja confianza.");
        if (r.ConfiguracionesConEvidencia != 0)
            throw new Exception("ConfiguracionesConEvidencia debe ser 0 cuando no hay filas con evidencia real.");
    }

    private static void VerificarP6()
    {
        var prohibidos = new[] { "score", "ranking", "priorit", "winner", "best", "leader", "top" };
        var tipos = new[] { typeof(ConfiguracionCandidata), typeof(RecomendacionExperimental) };

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

        foreach (var metodo in typeof(MotorRecomendacion).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var nombreLower = metodo.Name.ToLowerInvariant();
            foreach (var p in prohibidos)
            {
                if (nombreLower.Contains(p))
                    throw new Exception($"MotorRecomendacion.{metodo.Name} contiene el termino prohibido '{p}'.");
            }
        }

        var rutaCarpeta = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var archivoMotor = Directory.GetFiles(rutaCarpeta, "MotorRecomendacion.cs", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new Exception($"No se pudo localizar MotorRecomendacion.cs bajo '{rutaCarpeta}' para verificar P6.");

        var prohibidosEjecucion = new[] { "ComparadorGestores.Comparar", "EjecutorProtocolo.Ejecutar", "PersistidorComparaciones", "CrearEstrategia", "IStrategy" };
        var codigo = File.ReadAllText(archivoMotor);
        foreach (var patron in prohibidosEjecucion)
        {
            if (codigo.Contains(patron, StringComparison.Ordinal))
                throw new Exception($"'MotorRecomendacion.cs' contiene una referencia prohibida a '{patron}' — esta capa no debe ejecutar backtests ni tocar Capa 1.");
        }
    }

    private static void VerificarP7()
    {
        var rutaCarpeta = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var archivos = new[] { "MotorRecomendacion.cs", "ProgramRecomendador.cs" }
            .Select(nombre => Directory.GetFiles(rutaCarpeta, nombre, SearchOption.AllDirectories).FirstOrDefault())
            .Where(p => p is not null)
            .ToList();

        if (archivos.Count != 2)
            throw new Exception($"No se pudieron localizar MotorRecomendacion.cs/ProgramRecomendador.cs bajo '{rutaCarpeta}' para verificar P7.");

        foreach (var archivo in archivos)
        {
            var codigoLower = File.ReadAllText(archivo!).ToLowerInvariant();
            foreach (var termino in TerminosProhibidos)
            {
                if (codigoLower.Contains(termino))
                    throw new Exception($"'{Path.GetFileName(archivo)}' contiene el termino prohibido '{termino}' — no debe generar lenguaje de seleccion (D-118).");
            }
        }
    }

    // spec: regresion directa del defecto identificado durante la especificacion — si
    // CalcularCandidatas agrupara por (Estrategia, Timeframe, Dataset, Gestor), cada grupo tendria
    // 1 sola fila -> mediana = ese unico valor -> el umbral "cruza" siempre -> las 3 filas
    // aparecerian como candidatas sin excepcion. El fixture esta disenado para que la mediana real
    // (sobre el grupo de 3) excluya exactamente 1 fila.
    private static void VerificarP9()
    {
        var filas = new List<FilaCorpus>
        {
            new("EstA", "1h", "DatasetX", "gestorA:v1", "Success", 5m, 0.1m, 1m, 1m, 1m, 1m, "c1"),
            new("EstA", "1h", "DatasetX", "gestorB:v1", "Success", 15m, 0.1m, 1m, 1m, 1m, 1m, "c2"),
            new("EstA", "1h", "DatasetX", "gestorC:v1", "Success", 25m, 0.1m, 1m, 1m, 1m, 1m, "c3"),
        };
        // Mediana real del grupo (sin gestor en la clave) = 15. Perfil Crecimiento (Mayor):
        // gestorA (5) queda EXCLUIDO; gestorB (15) y gestorC (25) quedan incluidos.
        var r = MotorRecomendacion.Recomendar(filas, "Crecimiento");
        if (r.Candidatas.Count == 3)
            throw new Exception("Las 3 filas cruzaron el umbral — indicio de que el grupo de comparacion incluyo el gestor en su clave (grupo de 1 fila, mediana=valor propio, filtro siempre verdadero). El grupo de comparacion debe ser (Estrategia, Timeframe, Dataset) sin gestor.");
        if (r.Candidatas.Count != 2)
            throw new Exception($"Se esperaban exactamente 2 candidatas (gestorB=15, gestorC=25 >= mediana real 15), se obtuvieron {r.Candidatas.Count}.");
        if (r.Candidatas.Any(c => c.IdentidadGestor == "gestorA:v1"))
            throw new Exception("gestorA:v1 (valor=5) no deberia cruzar la mediana real del grupo (15).");
    }

    private static void VerificarP8(string dirResultadosReal, string rutaManifiestoReal)
    {
        var (filas, _) = LectorCorpus.Leer(dirResultadosReal, rutaManifiestoReal);
        if (filas.Count == 0)
            throw new Exception($"No se encontraron filas en el corpus real ('{dirResultadosReal}') — no se puede verificar P8 sin evidencia persistida.");

        var r = MotorRecomendacion.Recomendar(filas, "Crecimiento");
        var manifiestoTexto = File.ReadAllText(rutaManifiestoReal);
        foreach (var c in r.Candidatas)
        {
            if (c.CarpetasOrigen.Count == 0)
                throw new Exception($"Candidata {c.Estrategia}/{c.Timeframe}/{c.NombreDataset}/{c.IdentidadGestor} no tiene CarpetasOrigen — trazabilidad rota.");
            foreach (var carpeta in c.CarpetasOrigen)
            {
                var nombreCarpeta = Path.GetFileName(carpeta);
                if (!Directory.Exists(carpeta))
                    throw new Exception($"CarpetaOrigen '{carpeta}' no existe fisicamente.");
                if (!manifiestoTexto.Contains($"\"{nombreCarpeta}\""))
                    throw new Exception($"CarpetaOrigen '{nombreCarpeta}' no aparece en el manifiesto — trazabilidad rota.");
            }
        }
    }
}
