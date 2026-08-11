using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Tendencia sostenida que se invierte abruptamente a mitad del dataset (ej. alcista -> bajista).
// Valida que el motor no arrastra ningun sesgo direccional de la primera mitad hacia la segunda
// (cada vela se resuelve con sus propios datos, sin memoria de la tendencia anterior).
public static class GeneradorTendenciaConReversion
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioInicial, decimal pendienteAntes, decimal pendienteDespues, decimal ruido, int seed)
    {
        var random = new Random(seed);
        var resultado = new List<Candle>(velas);
        var precio = precioInicial;
        var puntoReversion = velas / 2;

        for (var i = 0; i < velas; i++)
        {
            var pendiente = i < puntoReversion ? pendienteAntes : pendienteDespues;
            var open = precio;
            var deriva = pendiente + (decimal)(random.NextDouble() - 0.5) * ruido;
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
