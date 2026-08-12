using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;
using TD_Project.Protocolo;

namespace TD_Project.Caso5;

// spec: Caso 5C D-116/D-117 (DECISIONES_CASO5C_V1.md),
// ESPECIFICACION_IMPLEMENTACION_PERSISTENCIA_EVIDENCIA_V1.md §8 — 7 pruebas obligatorias (P1-P7).
// Mismo patron runner manual que TestsComparadorGestores.cs.
public static class TestsPersistidorComparaciones
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos(string dirDatasets, string dirResultados)
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

        Caso("P1 — Estructura de carpeta correcta (2 archivos)", () => VerificarEstructuraDeCarpeta(dirDatasets, dirResultados));
        Caso("P2 — Contenido de IDENTIDAD_COMPARACION.json coincide con el resultado en memoria", () => VerificarContenidoJson(dirDatasets, dirResultados));
        Caso("P3 — COMPARACION_GESTORES_V1.md identico al render de Caso 5B", () => VerificarContenidoMarkdown(dirDatasets, dirResultados));
        Caso("P4 — Reproducibilidad de contenido salvo timestamp", () => VerificarReproducibilidad(dirDatasets, dirResultados));
        Caso("P5 — No persiste ninguna metrica numerica en el JSON", () => VerificarSinMetricasEnJson(dirDatasets, dirResultados));
        Caso("P6 — Fallo de escritura no invalida el resultado en memoria", () => VerificarFalloDeEscrituraNoInvalida(dirDatasets));
        Caso("P7 — ComparadorGestores.cs sin cambios (firma publica)", VerificarComparadorGestoresSinCambios);

        return (total, pasaron, detalles);
    }

    private static EntradaProtocolo EntradaBase(string dirDatasets) => new(
        Estrategia: "Tres Mosqueteros", VersionEstrategia: "1.0", Parametros: new[] { "maxMartingalas=2" },
        CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
        Timeframes: new[] { "1D" }, DirDatasets: dirDatasets, NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
        CapitalInicial: 1000m, Instrumento: new Instrumento("BTCUSDT", 0.1m),
        Costes: new ConfiguracionCostes(0.001m, 0.001m));

    private static ResultadoComparativoGestores ResultadoDePrueba(string dirDatasets)
    {
        IGestorRiesgo[] gestores = { new GestorFixedFractional(0.1m), new GestorFixedRisk(50m) };
        return ComparadorGestores.Comparar(EntradaBase(dirDatasets), gestores);
    }

    // P1
    private static void VerificarEstructuraDeCarpeta(string dirDatasets, string dirResultados)
    {
        var resultado = ResultadoDePrueba(dirDatasets);
        var carpeta = PersistidorComparaciones.Persistir(dirResultados, resultado);

        if (!Directory.Exists(carpeta))
            throw new Exception($"Carpeta no creada: {carpeta}");

        var archivos = Directory.GetFiles(carpeta).Select(Path.GetFileName).OrderBy(n => n).ToArray();
        var esperados = new[] { "COMPARACION_GESTORES_V1.md", "IDENTIDAD_COMPARACION.json" };
        if (!archivos.SequenceEqual(esperados))
            throw new Exception($"Archivos esperados {string.Join(",", esperados)}, obtenidos {string.Join(",", archivos)}.");
    }

    // P2
    private static void VerificarContenidoJson(string dirDatasets, string dirResultados)
    {
        var resultado = ResultadoDePrueba(dirDatasets);
        var carpeta = PersistidorComparaciones.Persistir(dirResultados, resultado);
        var json = File.ReadAllText(Path.Combine(carpeta, "IDENTIDAD_COMPARACION.json"));

        if (!json.Contains($"\"estrategia\": \"{resultado.Estrategia}\""))
            throw new Exception("estrategia no coincide en el JSON.");
        if (!json.Contains($"\"timeframe\": \"{resultado.Timeframe}\""))
            throw new Exception("timeframe no coincide en el JSON.");
        if (!json.Contains($"\"nombreDataset\": \"{resultado.NombreDataset}\""))
            throw new Exception("nombreDataset no coincide en el JSON.");
        foreach (var fila in resultado.Filas)
        {
            if (!json.Contains($"\"identidad\": \"{fila.IdentidadGestor}\""))
                throw new Exception($"identidad '{fila.IdentidadGestor}' no encontrada en el JSON.");
            if (!json.Contains($"\"estado\": \"{fila.Estado}\""))
                throw new Exception($"estado '{fila.Estado}' no encontrado en el JSON.");
        }
    }

    // P3
    private static void VerificarContenidoMarkdown(string dirDatasets, string dirResultados)
    {
        var resultado = ResultadoDePrueba(dirDatasets);
        var carpeta = PersistidorComparaciones.Persistir(dirResultados, resultado);
        var md = File.ReadAllText(Path.Combine(carpeta, "COMPARACION_GESTORES_V1.md"));
        var esperado = RenderizadorComparacionGestores.Generar(resultado);

        if (md != esperado)
            throw new Exception("COMPARACION_GESTORES_V1.md difiere del render directo de RenderizadorComparacionGestores.Generar.");
    }

    // P4
    private static void VerificarReproducibilidad(string dirDatasets, string dirResultados)
    {
        var resultado = ResultadoDePrueba(dirDatasets);
        var carpeta1 = PersistidorComparaciones.Persistir(dirResultados, resultado);
        var carpeta2 = PersistidorComparaciones.Persistir(dirResultados, resultado);

        var json1 = File.ReadAllText(Path.Combine(carpeta1, "IDENTIDAD_COMPARACION.json"));
        var json2 = File.ReadAllText(Path.Combine(carpeta2, "IDENTIDAD_COMPARACION.json"));

        string QuitarFecha(string j) => System.Text.RegularExpressions.Regex.Replace(j, "\"fechaGeneracionUtc\": \"[^\"]*\"", "\"fechaGeneracionUtc\": \"\"");

        if (QuitarFecha(json1) != QuitarFecha(json2))
            throw new Exception("El JSON deberia ser identico entre 2 llamadas salvo fechaGeneracionUtc.");
    }

    // P5
    private static void VerificarSinMetricasEnJson(string dirDatasets, string dirResultados)
    {
        var resultado = ResultadoDePrueba(dirDatasets);
        var carpeta = PersistidorComparaciones.Persistir(dirResultados, resultado);
        var json = File.ReadAllText(Path.Combine(carpeta, "IDENTIDAD_COMPARACION.json"));

        var clavesProhibidas = new[] { "pnlTotal", "drawdownMaximoPct", "profitFactor", "exposicionMaxima", "cashFinal", "equityFinal" };
        foreach (var clave in clavesProhibidas)
        {
            if (json.Contains(clave, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"IDENTIDAD_COMPARACION.json no deberia contener la clave de metrica '{clave}'.");
        }
    }

    // P6
    private static void VerificarFalloDeEscrituraNoInvalida(string dirDatasets)
    {
        var resultado = ResultadoDePrueba(dirDatasets);

        // Ruta invalida en Windows (caracteres no permitidos) para forzar fallo de escritura.
        var dirInvalido = Path.Combine(Path.GetTempPath(), "caso5c_invalido_" + Path.GetInvalidFileNameChars()[0]);

        try
        {
            PersistidorComparaciones.Persistir(dirInvalido, resultado);
        }
        catch
        {
            // esperado — la excepcion de escritura no debe afectar el resultado ya calculado
        }

        if (resultado.Filas.Count == 0)
            throw new Exception("El resultado en memoria no deberia verse afectado por un fallo de escritura.");
        if (resultado.Filas[0].IdentidadGestor != new GestorFixedFractional(0.1m).ObtenerIdentidadConfiguracion())
            throw new Exception("El resultado en memoria cambio inesperadamente tras un fallo de escritura.");
    }

    // P7
    private static void VerificarComparadorGestoresSinCambios()
    {
        var metodoComparar = typeof(ComparadorGestores).GetMethod(nameof(ComparadorGestores.Comparar));
        if (metodoComparar is null)
            throw new Exception("ComparadorGestores.Comparar ya no existe.");
        var parametros = metodoComparar.GetParameters();
        if (parametros.Length != 2 || parametros[0].ParameterType != typeof(EntradaProtocolo) || parametros[1].ParameterType != typeof(IReadOnlyList<IGestorRiesgo>))
            throw new Exception("La firma de ComparadorGestores.Comparar cambio respecto a Caso 5B.");

        var metodoGenerar = typeof(RenderizadorComparacionGestores).GetMethod(nameof(RenderizadorComparacionGestores.Generar));
        if (metodoGenerar is null || metodoGenerar.GetParameters().Length != 1 || metodoGenerar.GetParameters()[0].ParameterType != typeof(ResultadoComparativoGestores))
            throw new Exception("La firma de RenderizadorComparacionGestores.Generar cambio respecto a Caso 5B.");
    }
}
