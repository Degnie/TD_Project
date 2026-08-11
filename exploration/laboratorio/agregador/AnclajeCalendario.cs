namespace TD_Project.Agregador;

// Calendario UTC estricto (DISENO_FASE2B.md, Punto 1): los bordes de cada vela superior se
// anclan al tiempo real de mercado (medianoche UTC, lunes ISO), NUNCA al primer timestamp del
// dataset. Dos datasets con distinto punto de inicio deben producir velas superiores en los
// MISMOS bordes de calendario donde se solapen — condicion necesaria para comparar contra
// cualquier fuente externa futura.
public static class AnclajeCalendario
{
    // Timestamp UTC (ms) de inicio del intervalo de este timeframe que contiene 'timestampMs'.
    public static long InicioIntervalo(long timestampMs, Timeframe tf)
    {
        var instante = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);

        if (tf == Timeframe.W1)
        {
            var diaUtc = instante.UtcDateTime.Date;
            // DayOfWeek: Sunday=0..Saturday=6. ISO: semana empieza en lunes.
            var diasDesdeLunes = ((int)diaUtc.DayOfWeek + 6) % 7;
            var lunes = diaUtc.AddDays(-diasDesdeLunes);
            return new DateTimeOffset(lunes, TimeSpan.Zero).ToUnixTimeMilliseconds();
        }

        if (tf == Timeframe.D1)
        {
            var diaUtc = instante.UtcDateTime.Date;
            return new DateTimeOffset(diaUtc, TimeSpan.Zero).ToUnixTimeMilliseconds();
        }

        // Timeframes intradia: multiplos exactos de la duracion, anclados a medianoche UTC del
        // dia calendario del timestamp (ej. 4h: 00:00, 04:00, 08:00... no relativo al dataset).
        var duracionMs = tf.DuracionMinutos() * TimeframeExtensiones.UnMinutoMs;
        var inicioDiaMs = new DateTimeOffset(instante.UtcDateTime.Date, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var offsetDentroDelDia = timestampMs - inicioDiaMs;
        var bloque = offsetDentroDelDia / duracionMs;
        return inicioDiaMs + bloque * duracionMs;
    }

    public static long FinIntervaloExclusivo(long inicioIntervaloMs, Timeframe tf)
    {
        var inicio = DateTimeOffset.FromUnixTimeMilliseconds(inicioIntervaloMs).UtcDateTime;

        if (tf == Timeframe.W1)
            return new DateTimeOffset(inicio.AddDays(7), TimeSpan.Zero).ToUnixTimeMilliseconds();

        if (tf == Timeframe.D1)
            return new DateTimeOffset(inicio.AddDays(1), TimeSpan.Zero).ToUnixTimeMilliseconds();

        return inicioIntervaloMs + tf.DuracionMinutos() * TimeframeExtensiones.UnMinutoMs;
    }
}
