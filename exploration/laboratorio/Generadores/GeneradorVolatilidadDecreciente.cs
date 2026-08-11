using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Rango intra-vela que decae linealmente desde un maximo hasta un minimo a lo largo del
// dataset (mercado que se "apaga"). Valida que el motor resuelve correctamente Fills tanto en
// el extremo de alto rango como en el extremo de rango casi nulo, sin degradar precision
// decimal en los tramos finales.
public static class GeneradorVolatilidadDecreciente
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioInicial, decimal rangoInicial, decimal rangoFinal, int seed)
    {
        var random = new Random(seed);
        var resultado = new List<Candle>(velas);
        var precio = precioInicial;

        for (var i = 0; i < velas; i++)
        {
            var progreso = velas <= 1 ? 0m : (decimal)i / (velas - 1);
            var rango = rangoInicial + (rangoFinal - rangoInicial) * progreso;

            var open = precio;
            var close = Math.Max(0.01m, open + (decimal)(random.NextDouble() - 0.5) * rango * 2m);
            var extremoAlto = Math.Max(open, close) + (decimal)random.NextDouble() * rango;
            var extremoBajo = Math.Max(0.01m, Math.Min(open, close) - (decimal)random.NextDouble() * rango);

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
