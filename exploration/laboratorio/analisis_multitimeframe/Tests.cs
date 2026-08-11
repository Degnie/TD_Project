using TD_Project.Application;
using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisMultiTimeframe;

// Fase 1.3, Paso 3: pruebas con cifras YA publicadas en catalogo_estrategias/*.md (Fase 2C,
// determinismo confirmado en Fase 1.0). Reconstruye los 6 PerfilMultiTf por estrategia con los
// mismos numeros documentados y verifica que ComparadorMultiTimeframe agrupa/presenta sin alterar
// ningun valor — no genera dato nuevo, no toca AnalizadorOperacional ni el motor.
public static class TestsFixtures
{
    public static List<PerfilMultiTf> PerfilesTresMosqueterosPublico() => Tests.PerfilesTresMosqueteros();
}

public static class Tests
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

        Caso("Tres Mosqueteros — 1m tiene la mayor evidencia (82475 operaciones)", TresMosqueterosMayorEvidenciaEs1m);
        Caso("Tres Mosqueteros — 1D tiene menor muestra pero mejor resultado observado (separacion correcta)", TresMosqueterosSeparacionMejorResultadoVsEvidencia);
        Caso("Tres Mosqueteros — consistencia refleja rango real (86.29%-88.52%, amplitud 2.23pp)", TresMosqueterosConsistencia);
        Caso("Tres Mosqueteros — orden de filas preservado (no reordena por valor, evita ranking)", TresMosqueterosOrdenPreservado);
        Caso("MHI Mayoria — estructura completa, 6 filas con tamaño de muestra en cada una (D-010)", MhiMayoriaEstructuraCompleta);
        Caso("MHI Mayoria — comparacion consistente con winrate estable segun catalogo publicado", MhiMayoriaComparacionConsistente);

        return (total, pasaron, detalles);
    }

    // catalogo_estrategias/TRES_MOSQUETEROS.md — tabla "Metricas operativas oficiales", las 6 filas.
    internal static List<PerfilMultiTf> PerfilesTresMosqueteros() => new()
    {
        Perfil("1m", 82475, 71816, 10659, 6, 41097, 20396, 10323, 1000m, 99965.42m, "f1a9dcbe72bd..."),
        Perfil("5m", 16829, 14791, 2038, 4, 8337, 4285, 2169, 1000m, 40470.91m, "7c8dc059320f..."),
        Perfil("15m", 5605, 4914, 691, 6, 2695, 1493, 726, 1000m, 19672.58m, "26ed3d03f494..."),
        Perfil("1h", 1380, 1194, 186, 3, 632, 372, 190, 1000m, -29940.68m, "f3f120c7c672..."),
        Perfil("4h", 350, 302, 48, 2, 167, 95, 40, 1000m, -14331.63m, "2be5fba6896a..."),
        Perfil("1D", 61, 54, 7, 2, 30, 19, 5, 1000m, -5063.81m, "1356dd242e5a..."),
    };

    // catalogo_estrategias/MHI_MAYORIA.md — tabla "Metricas operativas oficiales", las 6 filas.
    private static List<PerfilMultiTf> PerfilesMhiMayoria() => new()
    {
        Perfil("1m", 79443, 69073, 10370, 5, 38680, 20294, 10099, 1000m, 22514.40m, "f1a9dcbe72bd...", estrategia: "MHI Mayoria"),
        Perfil("5m", 16795, 14721, 2074, 4, 8226, 4381, 2114, 1000m, 16981.64m, "7c8dc059320f...", estrategia: "MHI Mayoria"),
        Perfil("15m", 5551, 4829, 722, 4, 2609, 1472, 748, 1000m, -12657.32m, "26ed3d03f494...", estrategia: "MHI Mayoria"),
        Perfil("1h", 1392, 1211, 181, 4, 658, 370, 183, 1000m, -18438.48m, "f3f120c7c672...", estrategia: "MHI Mayoria"),
        Perfil("4h", 350, 308, 42, 2, 189, 73, 46, 1000m, -498.19m, "2be5fba6896a...", estrategia: "MHI Mayoria"),
        Perfil("1D", 57, 48, 9, 2, 30, 11, 7, 1000m, -8170.30m, "1356dd242e5a...", estrategia: "MHI Mayoria"),
    };

    private static PerfilMultiTf Perfil(
        string tf, int completadas, int ganadas, int perdidas, int rachaMax,
        int ganoInicial, int ganoM1, int ganoM2, decimal equityInicial, decimal equityFinal, string tfSha,
        string estrategia = "Tres Mosqueteros") => new()
    {
        Identidad = new IdentidadExperimento("BTCUSDT_2024-01-02_2025-01-02", tf, estrategia, 1000m, tf == "1m" ? "n/a (origen, no derivado)" : "1.0", "f1a9dcbe72bd...", tfSha, DateTime.UtcNow),
        EstadoMotor = EstadoBacktest.Success,
        EquityInicial = equityInicial,
        EquityFinal = equityFinal,
        OperacionesCompletadas = completadas,
        OperacionesGanadas = ganadas,
        OperacionesPerdidas = perdidas,
        OperacionAbiertaAlCierre = false,
        CapitalComprometidoAlCierre = 0m,
        RachaNegativaMaxima = rachaMax,
        Racha2 = 0,
        Racha3 = 0,
        Racha4 = 0,
        Racha5Mas = 0,
        GanoInicial = ganoInicial,
        GanoM1 = ganoM1,
        GanoM2 = ganoM2,
        PerdioAgotandoMartingalas = perdidas,
        MaxExposicion = 1m,
        ReconciliacionCoherente = true,
        ErroresReconciliacion = Array.Empty<string>(),
        VelasDisponibles = completadas * 5, // no relevante para estas pruebas, solo debe ser >= utilizadas
        VelasUtilizadas = completadas * 5,
    };

    private static void TresMosqueterosMayorEvidenciaEs1m()
    {
        var perfil = ComparadorMultiTimeframe.Comparar("Tres Mosqueteros", PerfilesTresMosqueteros());

        Assert(perfil.MayorEvidencia.Timeframe == "1m", "Mayor evidencia debe ser 1m");
        Assert(perfil.MayorEvidencia.IntentosCompletados == 82475, "1m debe reportar 82475 operaciones, igual que el catalogo");
    }

    private static void TresMosqueterosSeparacionMejorResultadoVsEvidencia()
    {
        var perfil = ComparadorMultiTimeframe.Comparar("Tres Mosqueteros", PerfilesTresMosqueteros());

        // Caso real documentado en la especificacion: 1D tiene la mayor eficiencia (88.52%) pero
        // la MENOR evidencia del conjunto (61 operaciones) — exactamente lo que las tres preguntas
        // separadas (mejor resultado / mayor evidencia) deben exponer sin fusionar.
        Assert(perfil.MejorResultadoObservado.Timeframe == "1D", "Mejor resultado observado debe ser 1D (88.52%, el mas alto)");
        AssertAproximado(perfil.MejorResultadoObservado.EficienciaOperacionalPct, 88.52m, 0.01m, "Eficiencia de 1D debe reproducir 88.52% publicado");
        Assert(perfil.MejorResultadoObservado.IntentosCompletados == 61, "1D (mejor resultado) tiene solo 61 operaciones");
        Assert(perfil.MayorEvidencia.Timeframe != perfil.MejorResultadoObservado.Timeframe, "Mayor evidencia (1m) y mejor resultado observado (1D) deben ser timeframes DISTINTOS — la separacion existe exactamente para este caso");
    }

    private static void TresMosqueterosConsistencia()
    {
        var perfil = ComparadorMultiTimeframe.Comparar("Tres Mosqueteros", PerfilesTresMosqueteros());
        var c = perfil.ConsistenciaEficienciaOperacional;

        Assert(c.TimeframeMinimo == "4h", "Minimo de eficiencia debe ser 4h (86.29%, segun catalogo)");
        Assert(c.TimeframeMaximo == "1D", "Maximo de eficiencia debe ser 1D (88.52%, segun catalogo)");
        AssertAproximado(c.AmplitudPuntosPorcentuales, 2.23m, 0.02m, "Amplitud debe ser ~2.23 puntos porcentuales, igual al calculo manual de la especificacion (seccion 4)");
    }

    private static void TresMosqueterosOrdenPreservado()
    {
        var perfil = ComparadorMultiTimeframe.Comparar("Tres Mosqueteros", PerfilesTresMosqueteros());
        var ordenEsperado = new[] { "1m", "5m", "15m", "1h", "4h", "1D" };
        var ordenObtenido = perfil.Filas.Select(f => f.Timeframe).ToArray();

        Assert(ordenObtenido.SequenceEqual(ordenEsperado), $"Las filas deben mantener el orden de entrada (1m->1D), no reordenarse por valor de metrica. Obtenido: {string.Join(",", ordenObtenido)}");
    }

    private static void MhiMayoriaEstructuraCompleta()
    {
        var perfil = ComparadorMultiTimeframe.Comparar("MHI Mayoria", PerfilesMhiMayoria());

        Assert(perfil.Estrategia == "MHI Mayoria", "Identidad de estrategia debe preservarse");
        Assert(perfil.Filas.Count == 6, "Deben existir 6 filas, una por timeframe evaluado en Fase 2C");
        foreach (var fila in perfil.Filas)
            Assert(fila.IntentosCompletados > 0, $"D-010: toda fila debe traer tamaño de muestra > 0 (timeframe {fila.Timeframe})");
    }

    private static void MhiMayoriaComparacionConsistente()
    {
        var perfil = ComparadorMultiTimeframe.Comparar("MHI Mayoria", PerfilesMhiMayoria());
        var c = perfil.ConsistenciaEficienciaOperacional;

        // Winrate publicado en catalogo: 84.21%-88.00% a traves de los 6 timeframes.
        Assert(c.ValorMinimo >= 84m && c.ValorMinimo <= 85m, $"Minimo de eficiencia esperado ~84.21% (1D), obtenido {c.ValorMinimo}");
        Assert(c.ValorMaximo >= 87m && c.ValorMaximo <= 89m, $"Maximo de eficiencia esperado ~88.00% (4h), obtenido {c.ValorMaximo}");
        Assert(c.AmplitudPuntosPorcentuales < 5m, "Amplitud debe reflejar consistencia (menor a 5pp), igual que lo ya documentado en la ficha MHI_MAYORIA.md");
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }

    private static void AssertAproximado(decimal actual, decimal esperado, decimal tolerancia, string mensaje)
    {
        if (Math.Abs(actual - esperado) > tolerancia)
            throw new Exception($"{mensaje} (esperado={esperado}, actual={actual})");
    }
}
