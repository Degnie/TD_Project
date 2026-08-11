using System.Security.Cryptography;
using System.Text.Json;

namespace TD_Project.DatosReales;

// Orquesta la paginacion contra BinanceClient y escribe el CSV crudo. Responsabilidad unica:
// obtener y persistir lo que Binance entrega, sin corregirlo (PLAN_FASE2A.md seccion "no
// aceptar automaticamente" + condicion del usuario: "el descargador no debe corregir datos").
// La decision de si el dataset es apto para datasets/reales/ es exclusiva de
// ValidadorIntegridadDatos, ejecutado aparte, despues de que termine la descarga.
public static class DescargadorVelas
{
    private const int LimitePorRequest = 1000;
    private static readonly TimeSpan PausaEntreRequests = TimeSpan.FromMilliseconds(150);

    public sealed record ResultadoDescarga(int VelasDescargadas, long InicioUtcMs, long FinUtcMs, string RutaCsv);

    // Reanudable: si rutaCsv ya existe con contenido, retoma desde el ultimo timestamp escrito +
    // 1 minuto en vez de volver a descargar todo (relevante a ~526 requests para un anio completo).
    public static async Task<ResultadoDescarga> DescargarAsync(
        BinanceClient cliente, string symbol, string interval, long inicioUtcMs, long finUtcMs, string rutaCsv)
    {
        var cursor = inicioUtcMs;
        var totalEscritas = 0;

        var yaExiste = File.Exists(rutaCsv);
        if (yaExiste)
        {
            var ultimoTimestamp = LeerUltimoTimestamp(rutaCsv);
            if (ultimoTimestamp is not null)
            {
                cursor = ultimoTimestamp.Value + ValidadorIntegridadDatos.UnMinutoMs;
                totalEscritas = ContarLineasDatos(rutaCsv);
            }
        }

        using var writer = new StreamWriter(rutaCsv, append: yaExiste);
        if (!yaExiste)
            await writer.WriteLineAsync("TimestampUtcMs,Open,High,Low,Close,Volume");

        while (cursor < finUtcMs)
        {
            var lote = await cliente.ObtenerKlinesAsync(symbol, interval, cursor, LimitePorRequest);
            if (lote.Count == 0)
                break; // Binance no tiene mas datos en el rango — no se inventa nada, se corta aqui.

            foreach (var vela in lote)
            {
                if (vela.TimestampUtcMs >= finUtcMs)
                    break;
                await writer.WriteLineAsync($"{vela.TimestampUtcMs},{vela.Open},{vela.High},{vela.Low},{vela.Close},{vela.Volume}");
                totalEscritas++;
            }

            cursor = lote[^1].TimestampUtcMs + ValidadorIntegridadDatos.UnMinutoMs;
            await writer.FlushAsync(); // progreso persistido antes del siguiente request: reanudable ante corte.
            await Task.Delay(PausaEntreRequests);
        }

        return new ResultadoDescarga(totalEscritas, inicioUtcMs, finUtcMs, rutaCsv);
    }

    public static IReadOnlyList<VelaCruda> LeerCsv(string rutaCsv)
    {
        var lineas = File.ReadAllLines(rutaCsv);
        var velas = new List<VelaCruda>();
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

    // Metadata solo se genera para un dataset que YA paso ValidadorIntegridadDatos — no antes.
    public static void EscribirMetadataValidada(
        string rutaMetadataJson, string instrumento, string intervalo, IReadOnlyList<VelaCruda> velas)
    {
        var sha256 = CalcularSha256(velas);
        var metadata = new
        {
            instrumento,
            mercado = "Spot",
            fuente = "Binance",
            intervalo,
            inicioUtcMs = velas[0].TimestampUtcMs,
            finUtcMs = velas[^1].TimestampUtcMs,
            descargaUtc = DateTime.UtcNow.ToString("O"),
            velas = velas.Count,
            valido = true,
            sha256,
        };
        File.WriteAllText(rutaMetadataJson, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string CalcularSha256(IReadOnlyList<VelaCruda> velas)
    {
        using var sha = SHA256.Create();
        using var buffer = new MemoryStream();
        using (var writer = new StreamWriter(buffer, leaveOpen: true))
        {
            writer.WriteLine("TimestampUtcMs,Open,High,Low,Close,Volume");
            foreach (var v in velas)
                writer.WriteLine($"{v.TimestampUtcMs},{v.Open},{v.High},{v.Low},{v.Close},{v.Volume}");
        }
        buffer.Position = 0;
        return Convert.ToHexString(sha.ComputeHash(buffer)).ToLowerInvariant();
    }

    private static long? LeerUltimoTimestamp(string rutaCsv)
    {
        var lineas = File.ReadAllLines(rutaCsv);
        for (var i = lineas.Length - 1; i >= 1; i--)
        {
            if (string.IsNullOrWhiteSpace(lineas[i])) continue;
            return long.Parse(lineas[i].Split(',')[0]);
        }
        return null;
    }

    private static int ContarLineasDatos(string rutaCsv) =>
        File.ReadAllLines(rutaCsv).Count(l => !string.IsNullOrWhiteSpace(l)) - 1;
}
