using System.Security.Cryptography;
using System.Text.Json;

namespace TD_Project.Agregador;

// Congelacion de un timeframe derivado (DISENO_FASE2B.md, Punto 5). Formato CSV extendido con
// columnas de completitud: un consumidor que ignore MinutosEsperados/MinutosRecibidos/EsParcial
// sigue leyendo OHLCV valido, pero quien necesite filtrar parciales (Fase 2C) tiene el dato ahi.
public static class EscritorDerivado
{
    public static string EscribirCsv(string rutaCsv, IReadOnlyList<VelaSuperior> velas)
    {
        using (var writer = new StreamWriter(rutaCsv))
        {
            writer.WriteLine("InicioUtcMs,FinUtcMsExclusivo,Open,High,Low,Close,Volume,MinutosEsperados,MinutosRecibidos,EsParcial");
            foreach (var v in velas)
                writer.WriteLine($"{v.InicioUtcMs},{v.FinUtcMsExclusivo},{v.Open},{v.High},{v.Low},{v.Close},{v.Volume},{v.MinutosEsperados},{v.MinutosRecibidos},{v.EsParcial}");
        }
        return CalcularSha256(rutaCsv);
    }

    public static void EscribirMetadata(
        string rutaMetadataJson, string sourceDataset, string sourceSha256, string sourceTimeframe,
        string targetTimeframe, string derivadoSha256, IReadOnlyList<VelaSuperior> velas)
    {
        var metadata = new
        {
            sourceDataset,
            sourceSha256,
            sourceTimeframe,
            targetTimeframe,
            aggregationVersion = "1.0",
            generatedUtc = DateTime.UtcNow.ToString("O"),
            velaCount = velas.Count,
            velasCompletas = velas.Count(v => !v.EsParcial),
            velasParciales = velas.Count(v => v.EsParcial),
            sha256 = derivadoSha256,
        };
        File.WriteAllText(rutaMetadataJson, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string CalcularSha256(string rutaCsv)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(rutaCsv);
        return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
    }
}
