using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Matching;

// spec: RN-11 — recorrido temporal simulado dentro de una vela (trayectorias canonicas A/B),
// usado para resolver ambiguedad cuando dos condiciones de cruce caen ambas dentro de [Low, High].
public readonly record struct RecorridoVela(decimal Open, decimal Primero, decimal Segundo, decimal Close)
{
    public static RecorridoVela Para(Candle vela, Trayectoria trayectoria) => trayectoria switch
    {
        Trayectoria.A => new(vela.Open, vela.High, vela.Low, vela.Close),
        Trayectoria.B => new(vela.Open, vela.Low, vela.High, vela.Close),
        _ => throw new ArgumentOutOfRangeException(nameof(trayectoria))
    };
}
