using System.Security.Cryptography;
using System.Text;
using TD_Project.Domain.Shared;

namespace TD_Project.ValidacionIntegral;

// spec: PROPUESTA_PRUEBA_INTEGRAL_V1.md §3-4 — genera datasets sinteticos deterministas (sin
// Random) y los escribe en el formato base de 6 columnas que EjecutorProtocolo/LectorDerivado ya
// leen (verificado contra exploration/laboratorio/datasets/reales/BTCUSDT/1m/*.csv), sin requerir
// ningun cambio en src/ ni en el lector existente. Cada escenario es una funcion pura de indice de
// vela — regenerable exactamente, mismo criterio de determinismo ya exigido a EstrategiaNeutral
// (D-086/D-087).
public static class GeneradorDatasetSintetico
{
    public const int VelasPorEscenario = 300;
    private const long TimestampInicioMs = 1704153600000; // 2024-01-02T00:00:00Z, mismo origen que el dataset real.
    private const long PasoMs = 86_400_000; // 1 dia (timeframe unico de esta validacion, 1D).

    // Escenario 1 — Alcista: tendencia ascendente monotona, rupturas periodicas de maximo,
    // volumen creciente. Precio base 100, incremento fijo 2 por vela.
    //
    // Nota de correccion (hallazgo aislado durante ejecucion de la validacion integral, ver
    // AUDITORIA_PRUEBA_INTEGRAL_SISTEMA_V1.md): un crecimiento LINEAL suave de volumen (ej.
    // 10 + i*0.5) nunca alcanza un multiplo fijo (1.5x, D-105) sobre su propia media movil de
    // ventana 20 — la brecha entre la vela actual y la media crece mas lento que la media misma
    // bajo una progresion lineal moderada, verificado matematicamente antes de corregir. No es un
    // defecto de EstrategiaVolumenBreakout ni de D-105 (que exige un pico real de participacion,
    // no una tendencia suave) — es un defecto de este generador de datos sinteticos. Corregido con
    // picos periodicos de volumen coordinados con las rupturas de precio (mismo indice i%10==0),
    // en vez de una progresion lineal.
    public static IReadOnlyList<Candle> Escenario1Alcista(int cantidad = VelasPorEscenario)
        => Generar(cantidad, i =>
        {
            var close = 100m + i * 2m;
            var open = close - 1.5m;
            // Ruptura periodica: cada 10 velas, High supera claramente el maximo de la ventana previa,
            // coincidiendo con un pico de volumen que si supera 1.5x la media movil de 20 velas.
            var esVelaDePico = i % 10 == 0 && i > 0;
            var high = esVelaDePico ? close + 5m : close + 0.5m;
            var low = open - 0.5m;
            var volume = esVelaDePico ? 100m : 10m; // pico puntual, no tendencia lineal.
            return (open, high, low, close, volume);
        });

    // Escenario 2 — Bajista: simetrico al 1, tendencia descendente, rupturas de minimo, volumen
    // suficiente (mismo criterio de pico puntual, no lineal, ver nota de Escenario1Alcista).
    public static IReadOnlyList<Candle> Escenario2Bajista(int cantidad = VelasPorEscenario)
        => Generar(cantidad, i =>
        {
            var close = 10_000m - i * 2m;
            var open = close + 1.5m;
            var high = open + 0.5m;
            var esVelaDePico = i % 10 == 0 && i > 0;
            var low = esVelaDePico ? close - 5m : close - 0.5m;
            var volume = esVelaDePico ? 100m : 10m;
            return (open, high, low, close, volume);
        });

    // Escenario 3 — Lateral: rango estrecho fijo +/- ruido controlado (determinista, sin Random —
    // ruido generado por una funcion periodica de i, no por muestreo aleatorio).
    public static IReadOnlyList<Candle> Escenario3Lateral(int cantidad = VelasPorEscenario)
        => Generar(cantidad, i =>
        {
            var ruido = (i % 7) * 0.3m - 0.9m; // oscila en un rango pequeno y acotado, determinista.
            var close = 500m + ruido;
            var open = close - 0.2m;
            var high = close + 1m;
            var low = close - 1m;
            var volume = 10m; // constante — sin contexto de volumen creciente.
            return (open, high, low, close, volume);
        });

    // Escenario 4 — Cambio brusco de regimen: tendencia alcista sostenida (mitad 1) seguida de
    // inversion abrupta de direccion en un punto fijo conocido (mitad 2), con salto de volatilidad
    // y de volumen en el punto de quiebre.
    public static IReadOnlyList<Candle> Escenario4CambioRegimen(int cantidad = VelasPorEscenario)
        => Generar(cantidad, i =>
        {
            var mitad = cantidad / 2;
            if (i < mitad)
            {
                var close = 1000m + i * 3m;
                return (close - 1m, close + 1m, close - 2m, close, 10m + i * 0.3m);
            }
            else
            {
                var precioEnQuiebre = 1000m + (mitad - 1) * 3m;
                var iDesdeQuiebre = i - mitad;
                var close = precioEnQuiebre - iDesdeQuiebre * 6m; // pendiente inversa mas pronunciada.
                var volatilidadExtra = iDesdeQuiebre == 0 ? 20m : 3m; // salto de volatilidad en la vela de quiebre.
                return (close + 1m, close + volatilidadExtra, close - volatilidadExtra, close, 200m + iDesdeQuiebre * 5m);
            }
        });

    // Escenario 5 — Economico extremo: reutiliza la forma del Escenario 1 (garantiza al menos una
    // orden emitida) — la condicion "extrema" no vive en el dataset, vive en CapitalInicial/
    // Instrumento/Costes de la EntradaProtocolo (ver TestsValidacionIntegral.cs), mismo criterio
    // ya verificado en TestsReporteIncapacidades.cs (Caso 4.4).
    public static IReadOnlyList<Candle> Escenario5EconomicoExtremo(int cantidad = VelasPorEscenario)
        => Escenario1Alcista(cantidad);

    private static IReadOnlyList<Candle> Generar(int cantidad, Func<int, (decimal Open, decimal High, decimal Low, decimal Close, decimal Volume)> porIndice)
    {
        var velas = new List<Candle>(cantidad);
        for (var i = 0; i < cantidad; i++)
        {
            var (open, high, low, close, volume) = porIndice(i);
            velas.Add(new Candle(TimestampInicioMs + i * PasoMs, open, high, low, close, volume));
        }
        return velas;
    }

    // Escribe el CSV (formato base de 6 columnas, sin encabezado con nombre relevante para el
    // parser — LectorDerivado.Leer descarta siempre la linea 0 y detecta el formato por conteo de
    // columnas) + metadata.json minimo (intervalo + velas, unicos campos leidos por
    // LectorDerivado.LeerMetadata para el formato base) en {dirDatasets}/{timeframe}/.
    public static string EscribirDataset(string dirDatasets, string nombreDataset, string timeframe, IReadOnlyList<Candle> velas)
    {
        var dirTimeframe = Path.Combine(dirDatasets, timeframe);
        Directory.CreateDirectory(dirTimeframe);

        var rutaCsv = Path.Combine(dirTimeframe, $"{nombreDataset}_{timeframe}.csv");
        var sb = new StringBuilder();
        sb.AppendLine("TimestampUtcMs,Open,High,Low,Close,Volume");
        foreach (var v in velas)
            sb.AppendLine($"{v.Timestamp},{v.Open.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.High.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.Low.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.Close.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        File.WriteAllText(rutaCsv, sb.ToString());

        var sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(rutaCsv))).ToLowerInvariant();
        var rutaMetadata = Path.Combine(dirTimeframe, "metadata.json");
        File.WriteAllText(rutaMetadata,
            $$"""
            {
              "intervalo": "{{timeframe}}",
              "velas": {{velas.Count}},
              "sha256": "{{sha256}}",
              "origen": "sintetico — GeneradorDatasetSintetico.cs, determinista, sin Random"
            }
            """);

        return rutaCsv;
    }
}
