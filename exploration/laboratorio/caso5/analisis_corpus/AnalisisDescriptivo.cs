namespace TD_Project.Caso5.AnalisisCorpus;

// spec: ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md §3 — cobertura de la matriz declarada, no
// interpretacion de resultado. Toda fila cuenta (D-119: evidencia incompleta != ausencia).
public sealed record CoberturaAnalizada(
    int TotalFilas,
    IReadOnlyDictionary<string, int> ComparacionesPorEstrategia,
    IReadOnlyDictionary<string, int> ComparacionesPorTimeframe,
    IReadOnlyDictionary<string, int> ComparacionesPorGestor,
    IReadOnlyDictionary<string, int> ComparacionesPorDataset,
    IReadOnlyList<string> CarpetasIgnoradas);

// spec: §3 — nunca colapsada a un solo valor "representativo".
public sealed record EstadisticaDescriptiva(
    int Cantidad,
    decimal Minimo,
    decimal Maximo,
    decimal Media,
    decimal Mediana);

// spec: §3 — una sola dimension de agrupacion por instancia (evita matriz que invite a "ganador").
public sealed record DistribucionMetrica(
    string NombreMetrica,
    string AgrupadoPor,
    IReadOnlyDictionary<string, EstadisticaDescriptiva> PorGrupo);

// spec: §3 — presencia/ausencia factual de un patron por periodo, nunca veredicto de robustez.
public sealed record ComparacionPeriodos(
    string NombreMetrica,
    string Gestor,
    IReadOnlyDictionary<string, EstadisticaDescriptiva> PorDataset);

// spec: §4 — hecho observable, sin evaluacion de si es deseable.
public sealed record CasoAtipico(
    string Descripcion,
    IReadOnlyList<string> CarpetasOrigen);

public sealed record ResumenCorpus(
    CoberturaAnalizada Cobertura,
    IReadOnlyList<DistribucionMetrica> Distribuciones,
    IReadOnlyList<ComparacionPeriodos> ComparacionesTemporal,
    IReadOnlyList<CasoAtipico> CasosAtipicos,
    string Limitaciones);

// spec: §5 — salvaguardas estructurales: ningun tipo expone "el mejor", ningun metodo ordena por
// valor (orden de insercion de los datos de entrada), Limitaciones siempre presente y no vacio,
// ningun calculo combina 2+ metricas en un numero, DetectarCasosAtipicos no descarta ni prioriza.
public static class AnalisisDescriptivo
{
    public static CoberturaAnalizada CalcularCobertura(IReadOnlyList<FilaCorpus> filas, IReadOnlyList<string> carpetasIgnoradas)
    {
        return new CoberturaAnalizada(
            filas.Count,
            ContarEnOrden(filas, f => f.Estrategia),
            ContarEnOrden(filas, f => f.Timeframe),
            ContarEnOrden(filas, f => f.IdentidadGestor),
            ContarEnOrden(filas, f => f.NombreDataset),
            carpetasIgnoradas);
    }

    private static IReadOnlyDictionary<string, int> ContarEnOrden(IReadOnlyList<FilaCorpus> filas, Func<FilaCorpus, string> clave)
    {
        var resultado = new Dictionary<string, int>();
        foreach (var fila in filas)
        {
            var k = clave(fila);
            resultado[k] = resultado.TryGetValue(k, out var actual) ? actual + 1 : 1;
        }
        return resultado;
    }

    public static DistribucionMetrica CalcularDistribucion(IReadOnlyList<FilaCorpus> filas, string nombreMetrica, string agrupadoPor)
    {
        Func<FilaCorpus, string> clave = agrupadoPor switch
        {
            "Gestor" => f => f.IdentidadGestor,
            "Timeframe" => f => f.Timeframe,
            "Dataset" => f => f.NombreDataset,
            "Estrategia" => f => f.Estrategia,
            _ => throw new ArgumentException($"AgrupadoPor no reconocido: '{agrupadoPor}'. Valores validos: Gestor, Timeframe, Dataset, Estrategia.", nameof(agrupadoPor))
        };
        Func<FilaCorpus, decimal?> valor = ExtractorDeMetrica(nombreMetrica);

        var porGrupo = new Dictionary<string, EstadisticaDescriptiva>();
        var vistos = new List<string>();
        foreach (var fila in filas)
        {
            var k = clave(fila);
            if (!vistos.Contains(k)) vistos.Add(k);
        }

        foreach (var grupo in vistos)
        {
            var valores = filas.Where(f => clave(f) == grupo).Select(valor).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (valores.Count == 0) continue;
            porGrupo[grupo] = CalcularEstadistica(valores);
        }

        return new DistribucionMetrica(nombreMetrica, agrupadoPor, porGrupo);
    }

    public static ComparacionPeriodos CompararPeriodos(IReadOnlyList<FilaCorpus> filas, string nombreMetrica, string gestor)
    {
        Func<FilaCorpus, decimal?> valor = ExtractorDeMetrica(nombreMetrica);
        var filasGestor = filas.Where(f => f.IdentidadGestor == gestor).ToList();

        var datasetsEnOrden = new List<string>();
        foreach (var f in filasGestor)
            if (!datasetsEnOrden.Contains(f.NombreDataset)) datasetsEnOrden.Add(f.NombreDataset);

        var porDataset = new Dictionary<string, EstadisticaDescriptiva>();
        foreach (var dataset in datasetsEnOrden)
        {
            var valores = filasGestor.Where(f => f.NombreDataset == dataset).Select(valor).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (valores.Count == 0) continue;
            porDataset[dataset] = CalcularEstadistica(valores);
        }

        return new ComparacionPeriodos(nombreMetrica, gestor, porDataset);
    }

    // spec: §4 — reglas fijas, ya documentadas en prosa por auditorias previas (V2, diversidad
    // temporal), no umbrales nuevos calibrados sobre el corpus.
    public static IReadOnlyList<CasoAtipico> DetectarCasosAtipicos(IReadOnlyList<FilaCorpus> filas)
    {
        var casos = new List<CasoAtipico>();

        var gruposSinActividad = filas
            .Where(f => f.Estado == "Success")
            .GroupBy(f => (f.Estrategia, f.NombreDataset))
            .Where(g => g.All(f => f.PnLTotal == 0m && f.CashFinal.HasValue));
        foreach (var g in gruposSinActividad)
        {
            var lista = g.ToList();
            casos.Add(new CasoAtipico(
                $"{g.Key.Estrategia}: sin actividad (PnLTotal=0 en {lista.Count} corrida(s) Success) sobre {g.Key.NombreDataset}",
                lista.Select(f => f.CarpetaOrigen).Distinct().ToList()));
        }

        var filasDrawdownExtremo = filas.Where(f => f.DrawdownMaximoPct is { } d && d >= 0.99m);
        foreach (var f in filasDrawdownExtremo)
        {
            casos.Add(new CasoAtipico(
                $"{f.Estrategia}/{f.Timeframe}/{f.NombreDataset}: DrawdownMaximoPct={f.DrawdownMaximoPct} con gestor {f.IdentidadGestor}",
                new[] { f.CarpetaOrigen }));
        }

        return casos;
    }

    public static ResumenCorpus Resumir(IReadOnlyList<FilaCorpus> filas, IReadOnlyList<string> carpetasIgnoradas)
    {
        var cobertura = CalcularCobertura(filas, carpetasIgnoradas);
        // "periodo temporal" = dataset con al menos una fila con metrica numerica (Success). Un
        // dataset que solo aparece en filas sin metrica (ej. DatasetInexistente_ParaCorpusDeFallo,
        // evidencia parcial deliberada de la sub-campana C) no es un periodo real — es un caso de
        // fallo, ya reportado aparte en CasosAtipicos/ComparacionesPorDataset (cobertura cruda).
        var datasets = filas.Where(f => f.PnLTotal.HasValue).Select(f => f.NombreDataset).Distinct().ToList();
        var gestores = cobertura.ComparacionesPorGestor.Keys.ToList();

        var distribuciones = new List<DistribucionMetrica>();
        foreach (var metrica in new[] { "PnLTotal", "DrawdownMaximoPct", "ProfitFactor", "ExposicionMaxima", "CashFinal", "EquityFinal" })
            distribuciones.Add(CalcularDistribucion(filas, metrica, "Gestor"));

        var comparaciones = new List<ComparacionPeriodos>();
        if (datasets.Count > 1)
        {
            foreach (var gestor in gestores)
            foreach (var metrica in new[] { "DrawdownMaximoPct", "PnLTotal" })
                comparaciones.Add(CompararPeriodos(filas, metrica, gestor));
        }

        var limitaciones =
            $"Corpus descriptivo: {cobertura.TotalFilas} filas sobre {datasets.Count} periodo(s) " +
            "temporal(es), instrumento unico (BTCUSDT). Esta salida describe unicamente lo que el " +
            "corpus persistido contiene — no constituye recomendacion, ranking, ni evaluacion de " +
            "que gestor/estrategia es preferible (D-118/D-119/D-120). Ningun patron aqui descrito " +
            "se extiende a instrumentos no representados en el corpus.";

        return new ResumenCorpus(cobertura, distribuciones, comparaciones, DetectarCasosAtipicos(filas), limitaciones);
    }

    private static Func<FilaCorpus, decimal?> ExtractorDeMetrica(string nombreMetrica) => nombreMetrica switch
    {
        "PnLTotal" => f => f.PnLTotal,
        "DrawdownMaximoPct" => f => f.DrawdownMaximoPct,
        "ProfitFactor" => f => f.ProfitFactor,
        "ExposicionMaxima" => f => f.ExposicionMaxima,
        "CashFinal" => f => f.CashFinal,
        "EquityFinal" => f => f.EquityFinal,
        _ => throw new ArgumentException($"Metrica no reconocida: '{nombreMetrica}'.", nameof(nombreMetrica))
    };

    private static EstadisticaDescriptiva CalcularEstadistica(List<decimal> valores)
    {
        var ordenados = valores.OrderBy(v => v).ToList();
        var n = ordenados.Count;
        var mediana = n % 2 == 1
            ? ordenados[n / 2]
            : (ordenados[n / 2 - 1] + ordenados[n / 2]) / 2m;

        return new EstadisticaDescriptiva(n, ordenados[0], ordenados[^1], valores.Sum() / n, mediana);
    }
}
