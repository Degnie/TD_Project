using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Fixtures;

// Escenario de validacion de motor, NO representa un mercado. Reproduce el caso de tres
// Cross-Zero consecutivos ya confirmado en tests/Application.Tests/CicloVitalTests.cs
// (TresCrossZeroConsecutivosReportanElPrecioDeAperturaRealDeCadaCiclo) y en CHANGELOG.md (fix
// de AcumuladorTrade, Ronda 4): cada Fill de inversion cierra el ciclo viejo y abre el nuevo en
// la MISMA operacion, sin pasar por Position == 0 en ningun punto observable.
public static class FixtureCrossZero
{
    public static IReadOnlyList<Candle> Velas() => new[]
    {
        new Candle(1, 100m, 100m, 100m, 100m, 500m),
        new Candle(2, 100m, 102m, 90m, 102m, 500m),  // ejecuta Buy 10 (abre)
        new Candle(3, 105m, 105m, 105m, 105m, 500m), // ejecuta Sell 15 (cierra 10, abre Short 5)
        new Candle(4, 110m, 110m, 110m, 110m, 500m), // ejecuta Buy 8 (cierra 5, abre Long 3)
        new Candle(5, 120m, 120m, 120m, 120m, 500m), // ejecuta Sell 6 (cierra 3, abre Short 3, queda viva)
    };
}
