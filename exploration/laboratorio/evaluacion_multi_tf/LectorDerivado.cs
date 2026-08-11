using System.Text.Json;
using TD_Project.Domain.Shared;

namespace TD_Project.EvaluacionMultiTf;

public sealed record VelaDerivadaCruda(long InicioUtcMs, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume, bool EsParcial);

public sealed record MetadataDerivado(string SourceDataset, string SourceSha256, string TargetTimeframe, string AggregationVersion, string Sha256, int VelaCount);

public static class LectorDerivado
{
    // Dos formatos posibles: el dataset base 1m de Fase 2A (6 columnas, siempre completo por
    // construccion — ValidadorIntegridadDatos ya garantizo continuidad sin huecos) o un derivado
    // de Fase 2B (10 columnas, agregador/EscritorDerivado.cs, con EsParcial explicito). Se
    // detecta por el numero de columnas del encabezado, no por el nombre del timeframe.
    public static IReadOnlyList<VelaDerivadaCruda> Leer(string rutaCsv)
    {
        var lineas = File.ReadAllLines(rutaCsv);
        if (lineas.Length == 0) return Array.Empty<VelaDerivadaCruda>();

        var esFormatoBase = lineas[0].Split(',').Length == 6;
        var velas = new List<VelaDerivadaCruda>(lineas.Length - 1);
        for (var i = 1; i < lineas.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lineas[i])) continue;
            var c = lineas[i].Split(',');
            velas.Add(esFormatoBase
                ? new VelaDerivadaCruda(
                    InicioUtcMs: long.Parse(c[0]),
                    Open: decimal.Parse(c[1]),
                    High: decimal.Parse(c[2]),
                    Low: decimal.Parse(c[3]),
                    Close: decimal.Parse(c[4]),
                    Volume: decimal.Parse(c[5]),
                    EsParcial: false)
                : new VelaDerivadaCruda(
                    InicioUtcMs: long.Parse(c[0]),
                    Open: decimal.Parse(c[2]),
                    High: decimal.Parse(c[3]),
                    Low: decimal.Parse(c[4]),
                    Close: decimal.Parse(c[5]),
                    Volume: decimal.Parse(c[6]),
                    EsParcial: bool.Parse(c[9])));
        }
        return velas;
    }

    // El dataset base 1m (Fase 2A) tiene su propia forma de metadata (sin sourceDataset/
    // aggregationVersion, porque no es un derivado — es el origen). Se detecta por presencia de
    // "sourceDataset" y se completa con valores que reflejan "es el origen de si mismo".
    public static MetadataDerivado LeerMetadata(string rutaJson)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(rutaJson));
        var r = doc.RootElement;

        if (r.TryGetProperty("sourceDataset", out _))
        {
            return new MetadataDerivado(
                SourceDataset: r.GetProperty("sourceDataset").GetString()!,
                SourceSha256: r.GetProperty("sourceSha256").GetString()!,
                TargetTimeframe: r.GetProperty("targetTimeframe").GetString()!,
                AggregationVersion: r.GetProperty("aggregationVersion").GetString()!,
                Sha256: r.GetProperty("sha256").GetString()!,
                VelaCount: r.GetProperty("velaCount").GetInt32());
        }

        var sha = r.GetProperty("sha256").GetString()!;
        return new MetadataDerivado(
            SourceDataset: "(origen — dataset base de Fase 2A)",
            SourceSha256: sha,
            TargetTimeframe: r.GetProperty("intervalo").GetString()!,
            AggregationVersion: "n/a (origen, no derivado)",
            Sha256: sha,
            VelaCount: r.GetProperty("velas").GetInt32());
    }

    // Punto 1 del diseno (Fase 2C): excluir parciales ANTES de construir el experimento — el
    // motor nunca ve una vela parcial. Devuelve (velas completas convertidas a Candle, total
    // disponible, total utilizado) para que el reporte declare la completitud por corrida.
    public static (IReadOnlyList<Candle> Completas, int Disponibles, int Utilizadas) FiltrarParaBacktest(IReadOnlyList<VelaDerivadaCruda> crudas)
    {
        var completas = crudas.Where(v => !v.EsParcial)
            .Select(v => new Candle(v.InicioUtcMs, v.Open, v.High, v.Low, v.Close, v.Volume))
            .ToList();
        return (completas, crudas.Count, completas.Count);
    }
}
