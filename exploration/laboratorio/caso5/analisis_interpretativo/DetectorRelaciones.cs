using TD_Project.Caso5.AnalisisCorpus;

namespace TD_Project.Caso5.AnalisisInterpretativo;

// spec: ESPECIFICACION_IMPLEMENTACION_ANALISIS_INTERPRETATIVO_CASO5C_V1.md §3 (D-124) — cruce de
// 2+ dimensiones SIEMPRE como el conjunto completo de combinaciones observadas, nunca una
// combinacion aislada/destacada. "Dimension" = Estrategia | Timeframe | Gestor | Dataset, mismo
// vocabulario que DistribucionMetrica.AgrupadoPor de Capa 2, ahora combinable.
public sealed record CombinacionObservada(
    IReadOnlyDictionary<string, string> Dimensiones,
    EstadisticaDescriptiva? Estadistica,
    int CantidadFilas,
    IReadOnlyList<string> CarpetasOrigen);

public sealed record RelacionObservada(
    string NombreMetrica,
    IReadOnlyList<string> DimensionesCruzadas,
    IReadOnlyList<CombinacionObservada> Combinaciones);

// spec: §3 — agrupacion de comparaciones segun presencia/ausencia de un patron YA NOMBRADO (nunca
// un patron nuevo inferido aqui). Expone ambos lados para evitar sesgo de confirmacion.
public sealed record AgrupacionPorPatron(
    string NombrePatron,
    IReadOnlyList<string> DondeAparece,
    IReadOnlyList<string> DondeNoAparece);

// spec: §3 — condiciones bajo las que aparece una metrica en un rango dado. Responde "en que
// condiciones aparece esta evidencia", no "que condicion es mejor".
public sealed record CondicionesDeAparicion(
    string NombreMetrica,
    string CondicionValor,
    IReadOnlyList<CombinacionObservada> Combinaciones);

// spec: §3 — comparacion de consistencia de un patron entre 2+ datasets. Solo presencia/ausencia
// del mismo conjunto de condiciones, nunca una palabra evaluativa (robusto/confiable/garantizado).
public sealed record ConsistenciaEntrePeriodos(
    string NombrePatron,
    IReadOnlyDictionary<string, IReadOnlyList<string>> CondicionesPorDataset);

// spec: §4/§5 — salvaguardas estructurales heredadas de Capa 2 (sin campo "mejor", sin
// ordenamiento por valor, sin puntuacion compuesta) mas 2 nuevas: prohibicion lexica de lenguaje
// prescriptivo en las plantillas de texto, y ausencia de prosa interpretativa (solo estructuras de
// datos y descripciones factuales templadas — la interpretacion es responsabilidad humana, en el
// documento de resultado, nunca en este codigo).
public static class DetectorRelaciones
{
    public static RelacionObservada CruzarDimensiones(IReadOnlyList<FilaCorpus> filas, string nombreMetrica, IReadOnlyList<string> dimensiones)
    {
        if (dimensiones.Count == 0)
            throw new ArgumentException("Se requiere al menos 1 dimension para cruzar.", nameof(dimensiones));

        Func<FilaCorpus, string> ExtractorDeDimension(string dimension) => dimension switch
        {
            "Estrategia" => f => f.Estrategia,
            "Timeframe" => f => f.Timeframe,
            "Gestor" => f => f.IdentidadGestor,
            "Dataset" => f => f.NombreDataset,
            _ => throw new ArgumentException($"Dimension no reconocida: '{dimension}'. Validas: Estrategia, Timeframe, Gestor, Dataset.", nameof(dimensiones))
        };
        var extractores = dimensiones.Select(ExtractorDeDimension).ToList();
        var valorMetrica = ExtractorDeMetrica(nombreMetrica);

        var clavesEnOrden = new List<string>();
        var porClave = new Dictionary<string, List<FilaCorpus>>();
        foreach (var fila in filas)
        {
            var valores = extractores.Select(ex => ex(fila)).ToList();
            var clave = string.Join("", valores);
            if (!porClave.ContainsKey(clave))
            {
                clavesEnOrden.Add(clave);
                porClave[clave] = new List<FilaCorpus>();
            }
            porClave[clave].Add(fila);
        }

        var combinaciones = new List<CombinacionObservada>();
        foreach (var clave in clavesEnOrden)
        {
            var filasDeCombinacion = porClave[clave];
            var primera = filasDeCombinacion[0];
            var dimensionesDict = dimensiones
                .Select((d, i) => (Nombre: d, Valor: extractores[i](primera)))
                .ToDictionary(x => x.Nombre, x => x.Valor);

            var valores = filasDeCombinacion.Select(valorMetrica).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            var estadistica = valores.Count > 0 ? CalcularEstadistica(valores) : null;

            combinaciones.Add(new CombinacionObservada(
                dimensionesDict, estadistica, filasDeCombinacion.Count,
                filasDeCombinacion.Select(f => f.CarpetaOrigen).Distinct().ToList()));
        }

        return new RelacionObservada(nombreMetrica, dimensiones, combinaciones);
    }

    public static AgrupacionPorPatron AgruparPorPatron(IReadOnlyList<FilaCorpus> filas, string nombrePatron, Func<FilaCorpus, bool> predicadoPatron)
    {
        if (string.IsNullOrWhiteSpace(nombrePatron))
            throw new ArgumentException("nombrePatron no puede ser vacio — todo patron detectado requiere una etiqueta factual explicita.", nameof(nombrePatron));

        var dondeAparece = new List<string>();
        var dondeNoAparece = new List<string>();
        foreach (var fila in filas)
        {
            var descripcion = $"{fila.Estrategia}/{fila.Timeframe}/{fila.NombreDataset}/{fila.IdentidadGestor}";
            if (predicadoPatron(fila))
                dondeAparece.Add(descripcion);
            else
                dondeNoAparece.Add(descripcion);
        }

        return new AgrupacionPorPatron(nombrePatron, dondeAparece, dondeNoAparece);
    }

    public static CondicionesDeAparicion DescribirCondicionesDeAparicion(IReadOnlyList<FilaCorpus> filas, string nombreMetrica, Func<decimal, bool> condicion, string condicionTexto)
    {
        var valorMetrica = ExtractorDeMetrica(nombreMetrica);
        var filasQueCumplen = filas.Where(f => valorMetrica(f) is { } v && condicion(v)).ToList();

        var clavesEnOrden = new List<(string Estrategia, string Timeframe, string Dataset, string Gestor)>();
        var porClave = new Dictionary<(string, string, string, string), List<FilaCorpus>>();
        foreach (var fila in filasQueCumplen)
        {
            var clave = (fila.Estrategia, fila.Timeframe, fila.NombreDataset, fila.IdentidadGestor);
            if (!porClave.ContainsKey(clave))
            {
                clavesEnOrden.Add(clave);
                porClave[clave] = new List<FilaCorpus>();
            }
            porClave[clave].Add(fila);
        }

        var combinaciones = clavesEnOrden.Select(clave =>
        {
            var filasDeCombinacion = porClave[clave];
            var valores = filasDeCombinacion.Select(valorMetrica).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            var dimensionesDict = new Dictionary<string, string>
            {
                ["Estrategia"] = clave.Estrategia, ["Timeframe"] = clave.Timeframe,
                ["Dataset"] = clave.Dataset, ["Gestor"] = clave.Gestor,
            };
            return new CombinacionObservada(
                dimensionesDict, valores.Count > 0 ? CalcularEstadistica(valores) : null,
                filasDeCombinacion.Count, filasDeCombinacion.Select(f => f.CarpetaOrigen).Distinct().ToList());
        }).ToList();

        return new CondicionesDeAparicion(nombreMetrica, condicionTexto, combinaciones);
    }

    public static ConsistenciaEntrePeriodos CompararConsistencia(IReadOnlyList<FilaCorpus> filas, string nombrePatron, Func<FilaCorpus, bool> predicadoPatron)
    {
        if (string.IsNullOrWhiteSpace(nombrePatron))
            throw new ArgumentException("nombrePatron no puede ser vacio — toda comparacion de consistencia requiere una etiqueta factual explicita.", nameof(nombrePatron));

        var datasetsEnOrden = new List<string>();
        foreach (var f in filas)
            if (!datasetsEnOrden.Contains(f.NombreDataset)) datasetsEnOrden.Add(f.NombreDataset);

        var condicionesPorDataset = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var dataset in datasetsEnOrden)
        {
            var condiciones = filas
                .Where(f => f.NombreDataset == dataset && predicadoPatron(f))
                .Select(f => $"{f.Estrategia}/{f.Timeframe}/{f.IdentidadGestor}")
                .Distinct()
                .ToList();
            condicionesPorDataset[dataset] = condiciones;
        }

        return new ConsistenciaEntrePeriodos(nombrePatron, condicionesPorDataset);
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
