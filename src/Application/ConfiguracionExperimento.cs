using TD_Project.Domain.Shared;

namespace TD_Project.Application;

// spec: glosario "Experiment", RNF-08 (Estado Canonico Inicial)
public sealed record ConfiguracionExperimento(
    decimal CapitalInicial,
    IReadOnlyList<Candle> Velas,
    int Warmup = 0);
