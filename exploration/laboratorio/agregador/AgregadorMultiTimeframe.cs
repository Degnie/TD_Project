using TD_Project.DatosReales;

namespace TD_Project.Agregador;

// Transformacion determinista 1m -> timeframe superior (DISENO_FASE2B.md). Responsabilidad
// unica: agrupar velas 1m ya ordenadas/validadas (Fase 2A) en intervalos de calendario UTC y
// aplicar la regla OHLCV fija. No decide nada de trading, no descarta datos, no rellena huecos —
// si faltan minutos dentro de un intervalo, se refleja en MinutosRecibidos < MinutosEsperados,
// nunca se inventa una vela.
public static class AgregadorMultiTimeframe
{
    // 'velas' debe venir ya ordenada y sin duplicados (garantizado por ValidadorIntegridadDatos
    // en Fase 2A para el dataset base de 1 minuto).
    public static IReadOnlyList<VelaSuperior> Agregar(IReadOnlyList<VelaCruda> velas, Timeframe tf) =>
        AgregarConMinutos(velas.Select(v => (v, MinutosDeEstaVela: 1)).ToList(), tf);

    // Caso recursivo (1m->5m->1h == 1m->1h): agrega directamente sobre velas YA agregadas,
    // reutilizando el MinutosRecibidos real de cada una en vez de asumir una duracion fija —
    // asi una vela intermedia parcial en el borde del dataset no infla el conteo del nivel
    // superior (DISENO_FASE2B.md, Punto 3: la completitud se propaga, nunca se inventa).
    public static IReadOnlyList<VelaSuperior> Agregar(IReadOnlyList<VelaSuperior> velasOrigen, Timeframe tf) =>
        AgregarConMinutos(velasOrigen.Select(v => (ComoVelaCruda(v), v.MinutosRecibidos)).ToList(), tf);

    private static IReadOnlyList<VelaSuperior> AgregarConMinutos(IReadOnlyList<(VelaCruda Vela, int MinutosDeEstaVela)> velas, Timeframe tf)
    {
        var resultado = new List<VelaSuperior>();
        if (velas.Count == 0) return resultado;

        var i = 0;
        while (i < velas.Count)
        {
            var inicioIntervalo = AnclajeCalendario.InicioIntervalo(velas[i].Vela.TimestampUtcMs, tf);
            var finIntervalo = AnclajeCalendario.FinIntervaloExclusivo(inicioIntervalo, tf);

            var j = i;
            var open = velas[i].Vela.Open;
            var high = velas[i].Vela.High;
            var low = velas[i].Vela.Low;
            var close = velas[i].Vela.Close;
            var volumen = 0m;
            var minutosRecibidos = 0;

            while (j < velas.Count && velas[j].Vela.TimestampUtcMs < finIntervalo)
            {
                if (velas[j].Vela.High > high) high = velas[j].Vela.High;
                if (velas[j].Vela.Low < low) low = velas[j].Vela.Low;
                close = velas[j].Vela.Close;
                volumen += velas[j].Vela.Volume;
                minutosRecibidos += velas[j].MinutosDeEstaVela;
                j++;
            }

            var minutosEsperados = tf.DuracionMinutos();
            resultado.Add(new VelaSuperior(inicioIntervalo, finIntervalo, open, high, low, close, volumen, minutosEsperados, minutosRecibidos));

            i = j;
        }

        return resultado;
    }

    private static VelaCruda ComoVelaCruda(VelaSuperior v) =>
        new(v.InicioUtcMs, v.Open, v.High, v.Low, v.Close, v.Volume);
}
