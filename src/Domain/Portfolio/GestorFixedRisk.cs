using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: Caso 5A D-110 (DECISIONES_CASO5_V1.md) — riesgo monetario fijo por operacion, no
// proporcional a capital (a diferencia de GestorFixedFractional). MontoRiesgoFijo fijado por
// convencion declarada, nunca calibrado (D-030). Puede exceder CapitalDisponible: cubierto aguas
// abajo por ValidadorCapacidad/CalculadoraReservaPreventiva (D-059/D-060), mismo comportamiento
// que GestorFixedFractional ya tiene hoy con PorcentajeRiesgo alto.
public sealed class GestorFixedRisk : IGestorRiesgo, IIdentidadGestorRiesgo
{
    private readonly decimal _montoRiesgoFijo;

    public GestorFixedRisk(decimal montoRiesgoFijo) => _montoRiesgoFijo = montoRiesgoFijo;

    public decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen)
        => _montoRiesgoFijo / (precioReferencia * tasaMargen);

    public string ObtenerIdentidadConfiguracion() => $"fixed-risk:v1:monto={_montoRiesgoFijo}";
}
