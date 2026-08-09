namespace TD_Project.Domain.Shared;

// spec: RNF-06 — igual input produce bit a bit igual output. Funcion pura sobre la vela.
public static class MotorDeterminista
{
    public static string Ejecutar(Candle vela) =>
        $"{vela.Timestamp}|{vela.Open}|{vela.High}|{vela.Low}|{vela.Close}|{vela.Volume}";
}
