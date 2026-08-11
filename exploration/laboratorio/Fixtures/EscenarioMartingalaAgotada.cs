using TD_Project.Domain.Shared;

namespace TD_Project.Laboratorio.Fixtures;

// Escenario de estres financiero controlado y DETERMINISTA (no ruido aleatorio): una unica
// operacion que pierde en el intento inicial, en Martingala 1 y en Martingala 2 (agota los 3
// niveles), forzando el peor caso posible de exposicion por operacion con EstrategiaTresMosqueteros
// (maxMartingalas=2). No mide si la estrategia "acierta" — mide si el motor sostiene
// contabilidad y trazabilidad correctas bajo el peor caso de una unica operacion.
//
// Timing verificado contra EstrategiaTresMosqueteros.cs (N%5==2 = vela 3 = senal; N/N+1 por
// cada apertura/cierre/reapertura, RN-13):
//   N=2 (vela 3, VERDE) -> senal Buy, ejecuta N=3 (vela 4)
//   N=3 (vela 4, ROJA)  -> pierde intento inicial, cierra Sell (ejecuta N=4/vela5), M1 pendiente
//   N=4 (vela 5)        -> reabre Buy (ejecuta N=5/vela6)
//   N=5 (vela 6, ROJA)  -> pierde M1, cierra Sell (ejecuta N=6/vela7), M2 pendiente
//   N=6 (vela 7)        -> reabre Buy (ejecuta N=7/vela8)
//   N=7 (vela 8, ROJA)  -> pierde M2 (martingalasUsadas=2=max) -> operacion perdida definitiva
public static class EscenarioMartingalaAgotada
{
    public static IReadOnlyList<Candle> Velas() => new[]
    {
        new Candle(1, 100m, 100m, 100m, 100m, 500m),
        new Candle(2, 100m, 100m, 100m, 100m, 500m),
        new Candle(3, 100m, 102m, 99m, 101m, 500m),  // vela 3: VERDE (Close>Open) -> senal Buy
        new Candle(4, 101m, 101m, 98m, 99m, 500m),   // vela 4: ROJA -> pierde intento inicial
        new Candle(5, 99m, 99m, 99m, 99m, 500m),     // vela 5: neutra, solo ejecuta la reapertura
        new Candle(6, 99m, 100m, 96m, 97m, 500m),    // vela 6: ROJA -> pierde Martingala 1
        new Candle(7, 97m, 97m, 97m, 97m, 500m),     // vela 7: neutra, solo ejecuta la reapertura
        new Candle(8, 97m, 98m, 93m, 94m, 500m),     // vela 8: ROJA -> pierde Martingala 2 (agotada)
        new Candle(9, 94m, 94m, 94m, 94m, 500m),     // vela de cierre final (M2 se cierra aqui)
    };
}
