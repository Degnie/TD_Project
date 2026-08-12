using System.Linq;
using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: Caso 5A D-110 (DECISIONES_CASO5_V1.md), ESPECIFICACION_IMPLEMENTACION_GESTORES_RIESGO_
// V1.md S4.3 — exposicion inversamente proporcional a volatilidad reciente. Ventana de Closes via
// DataSlice.VelasHastaN (unico dato historico disponible en el punto de invocacion, S1 de la
// especificacion). A diferencia de EstrategiaZScoreReversion, este gestor no tiene estado propio
// entre llamadas (GestorCapital.Ajustar es estatico) — recalcula la desviacion sobre la ventana
// de la propia DataSlice en cada invocacion: O(Ventana) por vela, no O(1). Ventana fija y pequena
// (mismo orden de magnitud que CondicionBreakout en Caso 3B), sin impacto practico observado.
public sealed class GestorVolatilitySizing : IGestorRiesgo, IIdentidadGestorRiesgo
{
    private readonly int _ventana;
    private readonly decimal _porcentajeRiesgoBase;
    private readonly decimal _desviacionReferencia;

    public GestorVolatilitySizing(int ventana, decimal porcentajeRiesgoBase, decimal desviacionReferencia)
    {
        if (ventana <= 1)
            throw new ArgumentOutOfRangeException(nameof(ventana), "Ventana debe ser mayor que 1.");
        if (desviacionReferencia <= 0m)
            throw new ArgumentOutOfRangeException(nameof(desviacionReferencia), "DesviacionReferencia debe ser positiva.");
        _ventana = ventana;
        _porcentajeRiesgoBase = porcentajeRiesgoBase;
        _desviacionReferencia = desviacionReferencia;
    }

    public decimal CalcularCantidad(PortfolioState portfolio, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen)
    {
        var velas = dataSlice.VelasHastaN;
        // Warmup: sin ventana suficiente, sin cantidad — mismo patron que EstrategiaZScoreReversion.
        if (velas.Count < _ventana)
            return 0m;

        var ventana = velas.Skip(velas.Count - _ventana).Select(v => v.Close).ToList();
        var media = ventana.Sum() / _ventana;
        var varianza = ventana.Sum(c => (c - media) * (c - media)) / _ventana;
        if (varianza < 0m)
            varianza = 0m;
        var desviacion = (decimal)Math.Sqrt((double)varianza);

        var factorVolatilidad = Math.Max(1m, desviacion / _desviacionReferencia);
        var capitalDisponible = portfolio.Cash - portfolio.Margin;
        var margenObjetivo = capitalDisponible * _porcentajeRiesgoBase;
        return margenObjetivo / (precioReferencia * tasaMargen * factorVolatilidad);
    }

    public string ObtenerIdentidadConfiguracion() =>
        $"volatility-sizing:v1:ventana={_ventana}:base={_porcentajeRiesgoBase}:desviacionReferencia={_desviacionReferencia}";
}
