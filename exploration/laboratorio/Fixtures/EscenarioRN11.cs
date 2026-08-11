using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Fixtures;

// Escenario de validacion de motor, NO representa un mercado. Reproduce el caso canonico de
// divergencia real entre trayectorias A/B ya confirmado en
// tests/Domain.Tests/Matching/StopLimitTests.cs (StopLimitPuedeDivergirEntreTrayectorias) y en
// CHANGELOG.md (fix de RN-11): Buy Stop-Limit 102/101 sobre vela Open=100/High=102/Low=90/
// Close=102. Trayectoria A hace Fill @101 (Stop dispara subiendo directo al High, cruza el
// Limit de camino); trayectoria B no hace Fill (el Stop dispara justo al llegar al High tras
// bajar primero, sin tramo restante hasta Close).
public static class EscenarioRN11
{
    public const decimal PrecioStop = 102m;
    public const decimal PrecioLimite = 101m;

    public static IReadOnlyList<Candle> Velas() => new[]
    {
        new Candle(1, 100m, 100m, 100m, 100m, 500m), // vela de warmup, sin senal
        new Candle(2, 100m, 102m, 90m, 102m, 500m),  // vela divergente RN-11
    };
}
