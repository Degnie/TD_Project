using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Genera un dataset de mercado lateral: precio oscila dentro de una banda fija sin tendencia
// neta, valida que el motor no "inventa" ganancia/perdida sistematica cuando el mercado no
// tiene direccion.
public static class GeneradorLateral
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioCentral, decimal amplitudBanda, int seed)
    {
        var random = new Random(seed);
        var resultado = new List<Candle>(velas);
        var precio = precioCentral;

        for (var i = 0; i < velas; i++)
        {
            var open = precio;
            // Reversion suave hacia el centro para que la banda no derive con el tiempo.
            var reversion = (precioCentral - open) * 0.1m;
            var ruido = (decimal)(random.NextDouble() - 0.5) * amplitudBanda;
            var close = open + reversion + ruido;
            var extremoAlto = Math.Max(open, close) + (decimal)random.NextDouble() * amplitudBanda * 0.3m;
            var extremoBajo = Math.Min(open, close) - (decimal)random.NextDouble() * amplitudBanda * 0.3m;

            resultado.Add(new Candle(
                Timestamp: i + 1,
                Open: Math.Round(open, 2),
                High: Math.Round(extremoAlto, 2),
                Low: Math.Round(Math.Max(0.01m, extremoBajo), 2),
                Close: Math.Round(close, 2),
                Volume: 500m));

            precio = close;
        }

        return resultado;
    }
}
