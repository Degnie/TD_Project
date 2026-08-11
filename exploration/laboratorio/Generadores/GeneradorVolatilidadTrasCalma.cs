using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Primera mitad del dataset con rango minimo (calma), segunda mitad con rango amplio
// (volatilidad subita). Valida que el motor no depende de una volatilidad estable previa para
// resolver correctamente Fills en el tramo de alta volatilidad.
public static class GeneradorVolatilidadTrasCalma
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioInicial, decimal rangoCalma, decimal rangoVolatil, int seed)
    {
        var random = new Random(seed);
        var resultado = new List<Candle>(velas);
        var precio = precioInicial;
        var puntoQuiebre = velas / 2;

        for (var i = 0; i < velas; i++)
        {
            var rango = i < puntoQuiebre ? rangoCalma : rangoVolatil;
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
