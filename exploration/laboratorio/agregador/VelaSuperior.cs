namespace TD_Project.Agregador;

// Vela agregada + su propia metadata de completitud (DISENO_FASE2B.md, Punto 3). No se separa
// "vela" de "info de completitud" en dos estructuras porque cada vela superior necesita cargar
// su propio veredicto — dos velas del mismo dataset pueden tener distinta completitud (solo los
// extremos del rango).
public sealed record VelaSuperior(
    long InicioUtcMs,
    long FinUtcMsExclusivo,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume,
    int MinutosEsperados,
    int MinutosRecibidos)
{
    public bool EsParcial => MinutosRecibidos < MinutosEsperados;
}
