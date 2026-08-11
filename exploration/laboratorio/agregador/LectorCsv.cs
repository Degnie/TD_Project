using TD_Project.DatosReales;

namespace TD_Project.Agregador;

public static class LectorCsv
{
    // Mismo formato que datos_reales/DescargadorVelas.cs: TimestampUtcMs,Open,High,Low,Close,Volume.
    public static IReadOnlyList<VelaCruda> Leer(string rutaCsv)
    {
        var lineas = File.ReadAllLines(rutaCsv);
        var velas = new List<VelaCruda>(lineas.Length - 1);
        for (var i = 1; i < lineas.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lineas[i])) continue;
            var campos = lineas[i].Split(',');
            velas.Add(new VelaCruda(
                TimestampUtcMs: long.Parse(campos[0]),
                Open: decimal.Parse(campos[1]),
                High: decimal.Parse(campos[2]),
                Low: decimal.Parse(campos[3]),
                Close: decimal.Parse(campos[4]),
                Volume: decimal.Parse(campos[5])));
        }
        return velas;
    }
}
