using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Genera un dataset de ruido puro: caminata aleatoria sin sesgo de tendencia ni de reversion.
// Sirve de control negativo — ninguna estrategia deberia mostrar una ventaja sistematica sobre
// datos sin estructura direccional (mas alla de la varianza propia de la muestra).
public static class GeneradorRuido
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioInicial, decimal volatilidad, int seed)
    {
        var random = new Random(seed);
        var resultado = new List<Candle>(velas);
        var precio = precioInicial;

        for (var i = 0; i < velas; i++)
        {
            var open = precio;
            var close = Math.Max(0.01m, open + (decimal)(random.NextDouble() - 0.5) * volatilidad);
            var extremoAlto = Math.Max(open, close) + (decimal)random.NextDouble() * volatilidad * 0.5m;
            var extremoBajo = Math.Max(0.01m, Math.Min(open, close) - (decimal)random.NextDouble() * volatilidad * 0.5m);

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
