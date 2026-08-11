using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Genera un dataset de mercado con tendencia sostenida (alcista o bajista), con ruido
// controlado alrededor de la pendiente para no ser una linea perfectamente recta (un mercado
// sintetico sin ruido no ejercita casos limite reales de cruce Open/High/Low/Close).
public static class GeneradorTendencia
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioInicial, decimal pendientePorVela, decimal ruido, int seed)
    {
        var random = new Random(seed);
        var resultado = new List<Candle>(velas);
        var precio = precioInicial;

        for (var i = 0; i < velas; i++)
        {
            var open = precio;
            var deriva = pendientePorVela + (decimal)(random.NextDouble() - 0.5) * ruido;
            var close = Math.Max(0.01m, open + deriva);
            var extremoAlto = Math.Max(open, close) + (decimal)random.NextDouble() * ruido;
            var extremoBajo = Math.Max(0.01m, Math.Min(open, close) - (decimal)random.NextDouble() * ruido);

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
