using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;
using TD_Project.Protocolo;

namespace TD_Project.Caso5;

// spec: Caso 5B D-112/D-113/D-114/D-115 (DECISIONES_CASO5B_V1.md),
// ESPECIFICACION_IMPLEMENTACION_COMPARADOR_GESTORES_V1.md §4 — 8 pruebas obligatorias (P1-P8).
// Mismo patron runner manual que TestsGestoresRiesgo.cs.
public static class TestsComparadorGestores
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos(string dirDatasets)
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

        Caso("P1 — Validacion de entrada: gestores vacio/null, Sizing previo, multiples Timeframes fallan explicitamente", VerificarValidacionDeEntrada);
        Caso("P2 — Mismos parametros salvo gestor: coincide con EjecutorProtocolo.Ejecutar directo", () => VerificarMismosParametrosSalvoGestor(dirDatasets));
        Caso("P3 — Identidad experimental: HashCompuesto igual, HashConfiguracionEconomica distinto por gestor", () => VerificarIdentidadExperimental(dirDatasets));
        Caso("P4 — Orden de entrada preservado en 3 permutaciones", () => VerificarOrdenPreservado(dirDatasets));
        Caso("P5 — Ausencia estructural de ranking en los tipos de resultado", VerificarAusenciaDeRanking);
        Caso("P6 — Ausencia de metodo de recomendacion en ComparadorGestores", VerificarAusenciaDeRecomendacion);
        Caso("P7 — Corrida individual fallida no invalida la comparacion completa", () => VerificarCorridaFallidaNoInvalida(dirDatasets));
        Caso("P8 — Extraccion de identidad falla explicitamente sin IIdentidadGestorRiesgo", () => VerificarFalloExplicitoSinIdentidad(dirDatasets));

        return (total, pasaron, detalles);
    }

    private static EntradaProtocolo EntradaBase(string dirDatasets, string nombreDataset = "BTCUSDT_2024-01-02_2025-01-02") => new(
        Estrategia: "Tres Mosqueteros", VersionEstrategia: "1.0", Parametros: new[] { "maxMartingalas=2" },
        CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
        Timeframes: new[] { "1D" }, DirDatasets: dirDatasets, NombreDataset: nombreDataset,
        CapitalInicial: 1000m, Instrumento: new Instrumento("BTCUSDT", 0.1m),
        Costes: new ConfiguracionCostes(0.001m, 0.001m));

    // P1
    private static void VerificarValidacionDeEntrada()
    {
        var entradaConSizing = EntradaBase("x") with { Sizing = new ConfiguracionSizing(new GestorFixedFractional(0.1m)) };
        AssertLanza(() => ComparadorGestores.Comparar(entradaConSizing, new IGestorRiesgo[] { new GestorFixedFractional(0.1m) }),
            "entradaBase.Sizing != null deberia fallar.");

        var entradaMultiTimeframe = EntradaBase("x") with { Timeframes = new[] { "1D", "1m" } };
        AssertLanza(() => ComparadorGestores.Comparar(entradaMultiTimeframe, new IGestorRiesgo[] { new GestorFixedFractional(0.1m) }),
            "entradaBase.Timeframes.Count != 1 deberia fallar.");

        AssertLanza(() => ComparadorGestores.Comparar(EntradaBase("x"), Array.Empty<IGestorRiesgo>()),
            "gestores vacio deberia fallar.");

        AssertLanza(() => ComparadorGestores.Comparar(EntradaBase("x"), new IGestorRiesgo?[] { null }!),
            "gestores con elemento null deberia fallar.");
    }

    private static void AssertLanza(Action accion, string mensajeSiNoLanza)
    {
        try
        {
            accion();
        }
        catch
        {
            return;
        }
        throw new Exception(mensajeSiNoLanza);
    }

    // P2
    private static void VerificarMismosParametrosSalvoGestor(string dirDatasets)
    {
        var gestor = new GestorFixedFractional(0.1m);
        var entradaBase = EntradaBase(dirDatasets);

        var comparacion = ComparadorGestores.Comparar(entradaBase, new IGestorRiesgo[] { gestor });

        var entradaDirecta = entradaBase with { Sizing = new ConfiguracionSizing(gestor) };
        var resultadoDirecto = EjecutorProtocolo.Ejecutar(entradaDirecta);
        var corridaDirecta = resultadoDirecto.Corridas.Single(c => c.Timeframe == "1D");

        var fila = comparacion.Filas[0];
        if (fila.Metricas?.CashFinal != corridaDirecta.MetricasFinancieras?.CashFinal)
            throw new Exception($"CashFinal via comparador ({fila.Metricas?.CashFinal}) difiere de EjecutorProtocolo directo ({corridaDirecta.MetricasFinancieras?.CashFinal}).");
        if (fila.Estado != corridaDirecta.Estado)
            throw new Exception($"Estado via comparador ({fila.Estado}) difiere de EjecutorProtocolo directo ({corridaDirecta.Estado}).");
    }

    // P3
    private static void VerificarIdentidadExperimental(string dirDatasets)
    {
        var entradaBase = EntradaBase(dirDatasets);
        IGestorRiesgo[] gestores = { new GestorFixedFractional(0.1m), new GestorFixedRisk(50m) };

        var resultado = ComparadorGestores.Comparar(entradaBase, gestores);

        var entradaGestorA = entradaBase with { Sizing = new ConfiguracionSizing(gestores[0]) };
        var entradaGestorB = entradaBase with { Sizing = new ConfiguracionSizing(gestores[1]) };
        var resultadoA = EjecutorProtocolo.Ejecutar(entradaGestorA);
        var resultadoB = EjecutorProtocolo.Ejecutar(entradaGestorB);

        if (resultadoA.Identidad.HashCompuesto != resultadoB.Identidad.HashCompuesto)
            throw new Exception("HashCompuesto deberia ser identico entre gestores (mismo dataset/estrategia/parametros).");
        if (resultadoA.Identidad.HashConfiguracionEconomica == resultadoB.Identidad.HashConfiguracionEconomica)
            throw new Exception("HashConfiguracionEconomica deberia diferir entre gestores con identidad distinta.");
        if (resultado.Filas[0].IdentidadGestor == resultado.Filas[1].IdentidadGestor)
            throw new Exception("Las identidades de gestor en las filas deberian ser distintas.");
    }

    // P4
    private static void VerificarOrdenPreservado(string dirDatasets)
    {
        var entradaBase = EntradaBase(dirDatasets);
        var a = new GestorFixedFractional(0.1m);
        var b = new GestorFixedRisk(50m);
        var c = new GestorVolatilitySizing(5, 0.1m, 1m);

        var permutaciones = new[]
        {
            new IGestorRiesgo[] { a, b, c },
            new IGestorRiesgo[] { c, a, b },
            new IGestorRiesgo[] { b, c, a },
        };

        foreach (var permutacion in permutaciones)
        {
            var resultado = ComparadorGestores.Comparar(entradaBase, permutacion);
            for (var i = 0; i < permutacion.Length; i++)
            {
                var identidadEsperada = ((IIdentidadGestorRiesgo)permutacion[i]).ObtenerIdentidadConfiguracion();
                if (resultado.Filas[i].IdentidadGestor != identidadEsperada)
                    throw new Exception($"Orden no preservado: posicion {i} esperaba '{identidadEsperada}', obtuvo '{resultado.Filas[i].IdentidadGestor}'.");
            }
        }
    }

    // P5
    private static void VerificarAusenciaDeRanking()
    {
        var camposProhibidos = new[] { "Ranking", "Posicion", "Puntuacion", "Score", "EsMejor", "Recomendado" };

        foreach (var tipo in new[] { typeof(FilaComparacionGestor), typeof(ResultadoComparativoGestores) })
        {
            foreach (var propiedad in tipo.GetProperties())
            {
                if (camposProhibidos.Any(p => propiedad.Name.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    throw new Exception($"{tipo.Name}.{propiedad.Name} sugiere ranking/recomendacion — no deberia existir en Caso 5B.");
            }
        }
    }

    // P6
    private static void VerificarAusenciaDeRecomendacion()
    {
        var metodos = typeof(ComparadorGestores).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.DeclaringType == typeof(ComparadorGestores))
            .ToList();

        if (metodos.Count != 1 || metodos[0].Name != nameof(ComparadorGestores.Comparar))
            throw new Exception($"ComparadorGestores deberia exponer unicamente 'Comparar' — encontrados: {string.Join(",", metodos.Select(m => m.Name))}.");
    }

    // P7
    private static void VerificarCorridaFallidaNoInvalida(string dirDatasets)
    {
        var entradaBase = EntradaBase(dirDatasets, nombreDataset: "DatasetInexistente_ParaForzarFallo");
        IGestorRiesgo[] gestores = { new GestorFixedFractional(0.1m), new GestorFixedRisk(50m) };

        var resultado = ComparadorGestores.Comparar(entradaBase, gestores);

        if (resultado.Filas.Count != 2)
            throw new Exception($"Se esperaban 2 filas, se obtuvieron {resultado.Filas.Count}.");
        if (resultado.Filas.Any(f => f.Estado == TD_Project.Protocolo.EstadoCorridaTimeframe.Success))
            throw new Exception("Con un dataset inexistente, ninguna fila deberia ser Success.");
        if (resultado.Filas.Any(f => f.Metricas is not null))
            throw new Exception("Con corridas no exitosas, Metricas deberia ser null en todas las filas.");
    }

    // P8
    private static void VerificarFalloExplicitoSinIdentidad(string dirDatasets)
    {
        var entradaBase = EntradaBase(dirDatasets);
        AssertLanza(() => ComparadorGestores.Comparar(entradaBase, new IGestorRiesgo[] { new GestorSinIdentidadDePrueba() }),
            "Un gestor sin IIdentidadGestorRiesgo deberia hacer fallar Comparar explicitamente.");
    }

    private sealed class GestorSinIdentidadDePrueba : IGestorRiesgo
    {
        public decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen) => 1m;
    }
}
