using System.Globalization;
using System.Text.Json;

namespace TD_Project.DatosReales;

// Cliente crudo del endpoint publico GET /api/v3/klines. Responsabilidad unica: traducir la
// respuesta de Binance a VelaCruda, campo por campo, SIN corregir nada (no inserta huecos, no
// ajusta OHLC, no descarta velas "sospechosas" — esa decision es del validador, no del cliente).
// Formato de respuesta confirmado contra la documentacion oficial: 12 campos por vela,
// [0]=OpenTime(ms) [1]=Open [2]=High [3]=Low [4]=Close [5]=Volume [6..11]=no usados aqui.
public sealed class BinanceClient
{
    private const string BaseUrl = "https://api.binance.com";
    private readonly HttpClient _http;

    public BinanceClient(HttpClient? http = null) => _http = http ?? new HttpClient();

    // limit maximo documentado por Binance: 1000 velas por request.
    public async Task<IReadOnlyList<VelaCruda>> ObtenerKlinesAsync(string symbol, string interval, long startTimeMs, int limit = 1000)
    {
        var url = $"{BaseUrl}/api/v3/klines?symbol={symbol}&interval={interval}&startTime={startTimeMs}&limit={limit}";
        using var respuesta = await _http.GetAsync(url);
        respuesta.EnsureSuccessStatusCode();

        var json = await respuesta.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(json);

        var velas = new List<VelaCruda>();
        foreach (var elemento in documento.RootElement.EnumerateArray())
        {
            velas.Add(new VelaCruda(
                TimestampUtcMs: elemento[0].GetInt64(),
                Open: decimal.Parse(elemento[1].GetString()!, CultureInfo.InvariantCulture),
                High: decimal.Parse(elemento[2].GetString()!, CultureInfo.InvariantCulture),
                Low: decimal.Parse(elemento[3].GetString()!, CultureInfo.InvariantCulture),
                Close: decimal.Parse(elemento[4].GetString()!, CultureInfo.InvariantCulture),
                Volume: decimal.Parse(elemento[5].GetString()!, CultureInfo.InvariantCulture)));
        }
        return velas;
    }
}
