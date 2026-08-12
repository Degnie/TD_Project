namespace TD_Project.Domain.Shared;

// spec: Caso 2 D-057 — TasaMargen pertenece al instrumento, no al experimento ni al motor.
public sealed record Instrumento(string Simbolo, decimal TasaMargen)
{
    // spec: Caso 2 D-061 — unica fuente del valor historico (0.1m), para no dejar el default
    // duplicado en mas de un lugar mientras ConfiguracionExperimento migra de forma incremental.
    public static Instrumento Default { get; } = new("N/A", 0.1m);
}
