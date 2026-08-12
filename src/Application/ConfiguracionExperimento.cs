using TD_Project.Domain.Shared;

namespace TD_Project.Application;

// spec: glosario "Experiment", RNF-08 (Estado Canonico Inicial)
// spec: Caso 2 D-061 — Instrumento opcional, default = Instrumento.Default (TasaMargen historica),
// para no romper consumidores existentes mientras el modelo economico migra incrementalmente.
// spec: Caso 2 D-064 — Costes pertenece a la condicion economica del experimento, no a Instrumento
// (el mismo simbolo puede evaluarse bajo distintas hipotesis de coste).
// spec: Caso 2 D-066/D-069 — Sizing opcional, null = sizing inactivo (Cantidad de la Strategy
// intacta), preserva baseline_final/ sin modificacion; activar sizing en una estrategia existente
// cambia ConfiguracionExperimento y por lo tanto el HashCompuesto (D-069, sin mecanismo adicional).
public sealed record ConfiguracionExperimento(
    decimal CapitalInicial,
    IReadOnlyList<Candle> Velas,
    int Warmup = 0,
    Instrumento? Instrumento = null,
    ConfiguracionCostes? Costes = null,
    ConfiguracionSizing? Sizing = null)
{
    public Instrumento InstrumentoEfectivo => Instrumento ?? Domain.Shared.Instrumento.Default;
    public ConfiguracionCostes CostesEfectivos => Costes ?? Domain.Shared.ConfiguracionCostes.Default;
}
