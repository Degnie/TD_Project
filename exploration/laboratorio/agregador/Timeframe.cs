namespace TD_Project.Agregador;

// Los 12 timeframes derivados de Fase 2B (DISENO_FASE2B.md). 1m es el origen (Fase 2A), no un
// derivado — no aparece aqui.
public enum Timeframe { M2, M5, M10, M15, M30, H1, H2, H4, H8, H12, D1, W1 }

public static class TimeframeExtensiones
{
    public const long UnMinutoMs = 60_000;

    // Duracion fija en minutos para timeframes de duracion constante (todos salvo 1D/1W, que
    // dependen del calendario — dias con 1440 min siempre, pero la semana ISO se resuelve aparte
    // por AnclajeCalendario porque su borde de inicio no es "cada N minutos desde epoch").
    public static int DuracionMinutos(this Timeframe tf) => tf switch
    {
        Timeframe.M2 => 2,
        Timeframe.M5 => 5,
        Timeframe.M10 => 10,
        Timeframe.M15 => 15,
        Timeframe.M30 => 30,
        Timeframe.H1 => 60,
        Timeframe.H2 => 120,
        Timeframe.H4 => 240,
        Timeframe.H8 => 480,
        Timeframe.H12 => 720,
        Timeframe.D1 => 1440,
        Timeframe.W1 => 10080,
        _ => throw new ArgumentOutOfRangeException(nameof(tf)),
    };

    public static string Etiqueta(this Timeframe tf) => tf switch
    {
        Timeframe.M2 => "2m", Timeframe.M5 => "5m", Timeframe.M10 => "10m", Timeframe.M15 => "15m",
        Timeframe.M30 => "30m", Timeframe.H1 => "1h", Timeframe.H2 => "2h", Timeframe.H4 => "4h",
        Timeframe.H8 => "8h", Timeframe.H12 => "12h", Timeframe.D1 => "1D", Timeframe.W1 => "1W",
        _ => throw new ArgumentOutOfRangeException(nameof(tf)),
    };
}
