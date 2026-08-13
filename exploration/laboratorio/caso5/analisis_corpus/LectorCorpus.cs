using System.Globalization;
using System.Text.RegularExpressions;

namespace TD_Project.Caso5.AnalisisCorpus;

// spec: ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md (D-123) — una fila por gestor por
// comparacion (misma granularidad que FilaComparacionGestor, Caso 5B D-114). Metricas null si el
// gestor no tuvo Success (mismo criterio D-072/D-077 ya aplicado en ComparadorGestores). Estado
// distinto de Success NO se descarta — cuenta como evidencia incompleta, no como ausencia (D-119
// necesita distinguir "sin evidencia" de "se intento y no fue exitoso").
public sealed record FilaCorpus(
    string Estrategia,
    string Timeframe,
    string NombreDataset,
    string IdentidadGestor,
    string Estado,
    decimal? PnLTotal,
    decimal? DrawdownMaximoPct,
    decimal? ProfitFactor,
    decimal? ExposicionMaxima,
    decimal? CashFinal,
    decimal? EquityFinal,
    string CarpetaOrigen);

// spec: ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md §2 — lee exclusivamente lo ya persistido
// por Caso 5C Capa 1 (IDENTIDAD_COMPARACION.json + COMPARACION_GESTORES_V1.md). Nunca llama a
// ComparadorGestores/EjecutorProtocolo ni ejecuta ningun backtest — traduccion mecanica de disco a
// estructura en memoria, sin filtrar/reordenar/descartar por valor propio del contenido.
//
// spec: precision post-inspeccion (registrada en AUDITORIA_CORPUS_COMPARATIVO_CASO5C_V2.md, nota
// de precision) — caso5/resultados/ contiene 106 carpetas fisicas, no solo las 49 del corpus
// oficial (V1+V2+Sub-campana D): incluye reintentos/repeticiones de desarrollo. LectorCorpus.Leer
// recibe la ruta del manifiesto (caso5/MANIFIESTO_CORPUS_CASO5C_V1.json — artefacto declarado y
// versionado, fuera de resultados/ deliberadamente: gobierna la interpretacion de la evidencia, no
// es evidencia generada por una ejecucion) como parametro independiente de dirResultados. Lee
// UNICAMENTE las carpetas ahi listadas — nunca todo resultados/. El manifiesto define que
// evidencia se analiza; no selecciona resultados favorables (fue construido sin mirar ninguna
// metrica numerica, solo identidad estructural).
public static class LectorCorpus
{
    private static readonly Regex CampoGestor = new(
        @"^Gestor: (?<identidad>.+)$", RegexOptions.Compiled);
    private static readonly Regex CampoEstado = new(
        @"^\s*Estado: (?<estado>.+)$", RegexOptions.Compiled);
    private static readonly Regex CampoMetrica = new(
        @"^\s*(?<nombre>PnLTotal|DrawdownMaximoPct|ProfitFactor|ExposicionMaxima|CashFinal|EquityFinal): (?<valor>.+)$",
        RegexOptions.Compiled);

    public static (IReadOnlyList<FilaCorpus> Filas, IReadOnlyList<string> CarpetasIgnoradas) Leer(string dirResultados, string rutaManifiesto)
    {
        var filas = new List<FilaCorpus>();
        var ignoradas = new List<string>();

        if (!Directory.Exists(dirResultados))
            return (filas, ignoradas);

        if (!File.Exists(rutaManifiesto))
            throw new InvalidOperationException(
                $"No se encontro el manifiesto en '{rutaManifiesto}'. LectorCorpus requiere un manifiesto " +
                "explicito que declare que carpetas pertenecen al corpus oficial — no lee todo resultados/ " +
                "directamente (ver ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md §2, precision post-inspeccion).");

        var nombresDeclarados = LeerNombresDelManifiesto(File.ReadAllText(rutaManifiesto));

        foreach (var nombreCarpeta in nombresDeclarados)
        {
            var carpeta = Path.Combine(dirResultados, nombreCarpeta);
            var rutaJson = Path.Combine(carpeta, "IDENTIDAD_COMPARACION.json");
            var rutaMd = Path.Combine(carpeta, "COMPARACION_GESTORES_V1.md");
            if (!Directory.Exists(carpeta) || !File.Exists(rutaJson) || !File.Exists(rutaMd))
            {
                ignoradas.Add(carpeta);
                continue;
            }

            var (estrategia, timeframe, nombreDataset, identidadesEnOrden) = LeerIdentidad(File.ReadAllText(rutaJson));
            if (identidadesEnOrden is null)
            {
                ignoradas.Add(carpeta);
                continue;
            }

            var metricasPorGestor = LeerMetricas(File.ReadAllLines(rutaMd));

            foreach (var identidad in identidadesEnOrden)
            {
                var (estado, metricas) = metricasPorGestor.TryGetValue(identidad, out var valor)
                    ? valor
                    : ("Desconocido", (Metricas?)null);

                filas.Add(new FilaCorpus(
                    estrategia!, timeframe!, nombreDataset!, identidad, estado,
                    metricas?.PnLTotal, metricas?.DrawdownMaximoPct, metricas?.ProfitFactor,
                    metricas?.ExposicionMaxima, metricas?.CashFinal, metricas?.EquityFinal,
                    carpeta));
            }
        }

        return (filas, ignoradas);
    }

    // Lee unicamente el arreglo "comparaciones"[].carpeta del manifiesto — no interpreta
    // "excluidos" (esa seccion es documental, para trazabilidad humana, no un segundo filtro).
    private static List<string> LeerNombresDelManifiesto(string json)
    {
        var iComparaciones = json.IndexOf("\"comparaciones\"", StringComparison.Ordinal);
        var iExcluidos = json.IndexOf("\"excluidos\"", StringComparison.Ordinal);
        var seccion = iExcluidos > iComparaciones ? json[iComparaciones..iExcluidos] : json[iComparaciones..];

        return Regex.Matches(seccion, @"""carpeta"":\s*""(?<c>[^""]+)""")
            .Select(m => m.Groups["c"].Value)
            .ToList();
    }

    private static (string? Estrategia, string? Timeframe, string? NombreDataset, List<string>? Identidades) LeerIdentidad(string json)
    {
        var estrategia = ExtraerCampoJson(json, "estrategia");
        var timeframe = ExtraerCampoJson(json, "timeframe");
        var nombreDataset = ExtraerCampoJson(json, "nombreDataset");
        if (estrategia is null || timeframe is null || nombreDataset is null)
            return (null, null, null, null);

        var identidades = Regex.Matches(json, @"""identidad"":\s*""(?<id>[^""]+)""")
            .Select(m => m.Groups["id"].Value)
            .ToList();
        if (identidades.Count == 0)
            return (null, null, null, null);

        return (estrategia, timeframe, nombreDataset, identidades);
    }

    private static string? ExtraerCampoJson(string json, string campo)
    {
        var m = Regex.Match(json, $@"""{campo}"":\s*""(?<v>[^""]*)""");
        return m.Success ? m.Groups["v"].Value : null;
    }

    private sealed record Metricas(decimal PnLTotal, decimal? DrawdownMaximoPct, decimal? ProfitFactor, decimal ExposicionMaxima, decimal CashFinal, decimal EquityFinal);

    private static Dictionary<string, (string Estado, Metricas? Metricas)> LeerMetricas(string[] lineasMd)
    {
        var resultado = new Dictionary<string, (string, Metricas?)>();
        string? identidadActual = null;
        string? estadoActual = null;
        decimal? pnlTotal = null, drawdown = null, profitFactor = null, exposicion = null, cashFinal = null, equityFinal = null;
        var tieneMetricas = false;

        void CerrarBloque()
        {
            if (identidadActual is null || estadoActual is null) return;
            var metricas = tieneMetricas
                ? new Metricas(pnlTotal!.Value, drawdown, profitFactor, exposicion!.Value, cashFinal!.Value, equityFinal!.Value)
                : null;
            resultado[identidadActual] = (estadoActual, metricas);
        }

        foreach (var linea in lineasMd)
        {
            var mGestor = CampoGestor.Match(linea);
            if (mGestor.Success)
            {
                CerrarBloque();
                identidadActual = mGestor.Groups["identidad"].Value;
                estadoActual = null;
                pnlTotal = drawdown = profitFactor = exposicion = cashFinal = equityFinal = null;
                tieneMetricas = false;
                continue;
            }

            var mEstado = CampoEstado.Match(linea);
            if (mEstado.Success && identidadActual is not null)
            {
                estadoActual = mEstado.Groups["estado"].Value;
                continue;
            }

            var mMetrica = CampoMetrica.Match(linea);
            if (mMetrica.Success && identidadActual is not null)
            {
                tieneMetricas = true;
                var nombre = mMetrica.Groups["nombre"].Value;
                var valorTexto = mMetrica.Groups["valor"].Value;
                decimal? valor = valorTexto == "null" ? null : decimal.Parse(valorTexto, CultureInfo.InvariantCulture);

                switch (nombre)
                {
                    case "PnLTotal": pnlTotal = valor ?? 0m; break;
                    case "DrawdownMaximoPct": drawdown = valor; break;
                    case "ProfitFactor": profitFactor = valor; break;
                    case "ExposicionMaxima": exposicion = valor ?? 0m; break;
                    case "CashFinal": cashFinal = valor ?? 0m; break;
                    case "EquityFinal": equityFinal = valor ?? 0m; break;
                }
            }
        }
        CerrarBloque();

        return resultado;
    }
}
