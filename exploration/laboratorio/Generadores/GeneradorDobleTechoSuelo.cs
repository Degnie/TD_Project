using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Generadores;

// Patron de doble techo (sube, baja, sube a un maximo similar, baja definitivamente) o doble
// suelo (el espejo). Valida que el motor resuelve correctamente una secuencia de reversiones
// parciales sin acumular error entre tramos.
public static class GeneradorDobleTechoSuelo
{
    public static IReadOnlyList<Candle> Generar(int velas, decimal precioBase, decimal amplitud, bool esTecho, decimal ruido, int seed)
    {
        var random = new Random(seed);
        var resultado = new List<Candle>(velas);
        var precio = precioBase;
        var cuartoDeVelas = Math.Max(1, velas / 4);
        var signo = esTecho ? 1m : -1m;

        for (var i = 0; i < velas; i++)
        {
            // Cuatro tramos: sube, baja, sube (pico similar al primero), baja definitivo.
            var tramo = Math.Min(i / cuartoDeVelas, 3);
            var pendienteTramo = tramo switch
            {
                0 => signo * amplitud / cuartoDeVelas,
                1 => -signo * amplitud / cuartoDeVelas,
                2 => signo * amplitud / cuartoDeVelas,
                _ => -signo * amplitud / cuartoDeVelas,
            };

            var open = precio;
            var deriva = pendienteTramo + (decimal)(random.NextDouble() - 0.5) * ruido;
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
