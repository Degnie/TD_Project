namespace TD_Project.Domain.Shared;

// spec: Caso 2 D-064 — pertenece a la condicion economica del experimento, no al Instrumento
// (el mismo simbolo puede evaluarse bajo distintas hipotesis de coste).
public sealed record ConfiguracionCostes(decimal TasaComision, decimal TasaSlippage)
{
    // spec: Caso 2 D-061/D-065 — experimento antiguo = coste cero. Fuente unica del default,
    // preserva el baseline de Caso 1 sin modificacion.
    public static ConfiguracionCostes Default { get; } = new(TasaComision: 0m, TasaSlippage: 0m);
}
