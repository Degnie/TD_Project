using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: Caso 5A D-108/D-110 (DECISIONES_CASO5_V1.md) — migracion literal de la formula que antes
// vivia inline en GestorCapital.Ajustar (Caso 4 D-093/D-094): CantidadActivo = MargenObjetivo /
// (PrecioReferencia * TasaMargen), MargenObjetivo = CapitalDisponible * PorcentajeRiesgo. Sin
// cambio de formula — gestor de referencia/control obligatorio de toda comparacion (D-110).
public sealed class GestorFixedFractional : IGestorRiesgo, IIdentidadGestorRiesgo
{
    private readonly decimal _porcentajeRiesgo;

    public GestorFixedFractional(decimal porcentajeRiesgo) => _porcentajeRiesgo = porcentajeRiesgo;

    public decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen)
    {
        var capitalDisponible = portfolio.Cash - portfolio.Margin;
        var margenObjetivo = capitalDisponible * _porcentajeRiesgo;
        return margenObjetivo / (precioReferencia * tasaMargen);
    }

    public string ObtenerIdentidadConfiguracion() => $"fixed-fractional:v1:riesgo={_porcentajeRiesgo}";
}
