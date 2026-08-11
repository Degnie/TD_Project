using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Genera un dataset de volatilidad extrema con gaps: el Open de una vela puede diferir
// bruscamente del Close de la anterior (simula apertura tras noticia/gap real), y el rango
// intra-vela (High-Low) es amplio respecto al cuerpo Open-Close.
public static class GeneradorVolatilidad
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioInicial, decimal rangoBase, decimal probabilidadGap, int seed)
    {
        var random = new Random(seed);
        var resultado = new List<Candle>(velas);
        var precio = precioInicial;

        for (var i = 0; i < velas; i++)
        {
            var hayGap = random.NextDouble() < (double)probabilidadGap;
            var open = hayGap
                ? precio + (decimal)(random.NextDouble() - 0.5) * rangoBase * 2m
                : precio;
            open = Math.Max(0.01m, open);

            var movimiento = (decimal)(random.NextDouble() - 0.5) * rangoBase * 2m;
            var close = Math.Max(0.01m, open + movimiento);
            var extremoAlto = Math.Max(open, close) + (decimal)random.NextDouble() * rangoBase;
            var extremoBajo = Math.Max(0.01m, Math.Min(open, close) - (decimal)random.NextDouble() * rangoBase);

            resultado.Add(new Candle(
                Timestamp: i + 1,
                Open: Math.Round(open, 2),
                High: Math.Round(extremoAlto, 2),
                Low: Math.Round(extremoBajo, 2),
                Close: Math.Round(close, 2),
                Volume: 500m));

            precio = close;
        }

        return resultado;
    }
}
