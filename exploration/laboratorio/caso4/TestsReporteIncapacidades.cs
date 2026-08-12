using TD_Project.Domain.Broker;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;
using TD_Project.ModeloFinanciero;
using TD_Project.Protocolo;

namespace TD_Project.Caso4;

// spec: Caso 4 D-096/D-097/D-098, ESPECIFICACION_REPORTE_INCAPACIDADES_CASO4_4_V1.md §5 — P1 sin
// incapacidades, P2 con incapacidades, P3 determinismo, P4 regresion. Vive en caso4/ (D-098),
// modulo satelite propio, sin tocar ModeloFinanciero.csproj/TestsMetricasFinancieras.cs/Program.cs
// de Caso 2 (congelados).
public static class TestsReporteIncapacidades
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

        Caso("P1 — Corrida sin incapacidades: Incapacidades vacio, reporte muestra 'Ninguna restriccion'", () => VerificarSinIncapacidades(dirDatasets));
        Caso("P2 — Corrida con incapacidades: dato llega desde ResultadoBacktest hasta el reporte", () => VerificarConIncapacidades(dirDatasets));
        Caso("P3 — Determinismo: misma entrada produce el mismo conteo de incapacidades", () => VerificarDeterminismo(dirDatasets));
        Caso("P4 — Regresion: MetricasFinancieras y demas secciones del reporte sin cambio de comportamiento", () => VerificarRegresionReporte(dirDatasets));

        return (total, pasaron, detalles);
    }

    private static EntradaProtocolo EntradaConCapital(string dirDatasets, decimal capitalInicial) => new(
        Estrategia: "Tres Mosqueteros",
        VersionEstrategia: "1.0",
        Parametros: new[] { "maxMartingalas=2" },
        CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
        Timeframes: new[] { "1D" },
        DirDatasets: dirDatasets,
        NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
        CapitalInicial: capitalInicial,
        Instrumento: new Instrumento("BTCUSDT", 0.1m),
        Costes: new ConfiguracionCostes(0.001m, 0.001m));

    // P1: capital abundante -> ninguna incapacidad esperada.
    private static void VerificarSinIncapacidades(string dirDatasets)
    {
        var entrada = EntradaConCapital(dirDatasets, capitalInicial: 1_000_000_000m);
        var resultado = EjecutorProtocolo.Ejecutar(entrada);
        var corrida = resultado.Corridas.Single(c => c.Timeframe == "1D");

        Assert(corrida.Estado == EstadoCorridaTimeframe.Success, $"Corrida debe ser Success, obtuvo {corrida.Estado}");
        Assert(corrida.Incapacidades is null || corrida.Incapacidades.Count == 0, "No debe haber incapacidades con capital abundante");

        var reporte = ReporteFinancieroGenerador.Generar(resultado, entrada);
        Assert(reporte.Contains("Ninguna restricción de capacidad observada"), "El reporte debe mostrar el texto neutral de ausencia de incapacidades");
    }

    // P2: capital minimo -> incapacidades garantizadas (mismo patron que ModeloEconomicoBaseTests P3).
    private static void VerificarConIncapacidades(string dirDatasets)
    {
        var entrada = EntradaConCapital(dirDatasets, capitalInicial: 1m);
        var resultado = EjecutorProtocolo.Ejecutar(entrada);
        var corrida = resultado.Corridas.Single(c => c.Timeframe == "1D");

        Assert(corrida.Estado == EstadoCorridaTimeframe.Success, $"Corrida debe ser Success, obtuvo {corrida.Estado}");
        Assert(corrida.Incapacidades is not null && corrida.Incapacidades.Count > 0, "Debe haber al menos una incapacidad con capital minimo");

        var reporte = ReporteFinancieroGenerador.Generar(resultado, entrada);
        Assert(reporte.Contains($"Total de incapacidades: {corrida.Incapacidades!.Count}"), "El reporte debe mostrar el total exacto de incapacidades");
        Assert(reporte.Contains("Por lado: Buy="), "El reporte debe mostrar el resumen agrupado por lado");
        // D-097: el texto niega explícitamente "es inválido"/"falló"/"debe descartarse" — el
        // AppendLine parte la frase en varias líneas físicas, se busca por fragmento continuo.
        Assert(reporte.Contains("no indica"), "El reporte debe declarar explícitamente que la incapacidad no implica falla (D-097)");
        Assert(reporte.Contains("no bloquea ni modifica"), "El reporte debe declarar explícitamente que el motor no altera la ejecución (D-059/D-060)");
    }

    private static void VerificarDeterminismo(string dirDatasets)
    {
        var entrada = EntradaConCapital(dirDatasets, capitalInicial: 1m);
        var r1 = EjecutorProtocolo.Ejecutar(entrada);
        var r2 = EjecutorProtocolo.Ejecutar(entrada);

        var c1 = r1.Corridas.Single(c => c.Timeframe == "1D");
        var c2 = r2.Corridas.Single(c => c.Timeframe == "1D");
        Assert((c1.Incapacidades?.Count ?? 0) == (c2.Incapacidades?.Count ?? 0),
            "El conteo de incapacidades debe ser identico entre dos ejecuciones con la misma entrada");
    }

    private static void VerificarRegresionReporte(string dirDatasets)
    {
        var entrada = EntradaConCapital(dirDatasets, capitalInicial: 1000m);
        var resultado = EjecutorProtocolo.Ejecutar(entrada);
        var corrida = resultado.Corridas.Single(c => c.Timeframe == "1D");

        Assert(corrida.MetricasFinancieras is not null, "MetricasFinancieras debe seguir poblandose sin cambio (D-072/D-077)");
        var reporte = ReporteFinancieroGenerador.Generar(resultado, entrada);
        Assert(reporte.Contains("1. Identificación"), "La seccion 1 debe seguir presente sin renumerarse");
        Assert(reporte.Contains("3. Métricas financieras"), "La seccion 3 debe seguir presente sin renumerarse");
        Assert(reporte.Contains("4. Restricciones de capacidad observadas"), "La nueva seccion 4 debe estar presente");
        Assert(reporte.Contains("5. Límites"), "La seccion de limites debe renumerarse a 5");
        Assert(reporte.Contains("6. Advertencia"), "La seccion de advertencia debe renumerarse a 6");
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
}
