using TD_Project.Application;

namespace TD_Project.ModeloFinanciero;

// spec: Caso 2 D-077 — unica fuente de calculo de MetricasFinancieras. Ningun generador de
// reporte recalcula estos valores por su cuenta (separacion calculo/reporte).
public static class CalculadoraMetricasFinancieras
{
    public static MetricasFinancieras Calcular(ResultadoBacktest resultado, decimal capitalInicial)
    {
        var equityFinal = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[^1].Equity : 0m;
        var pnlTotal = resultado.Trades.Sum(t => t.RealizedPnL);
        var drawdownMaximo = CalcularDrawdownMaximo(resultado.EquityCurve);
        var exposicionMaxima = resultado.PortfolioSnapshots.Count > 0
            ? resultado.PortfolioSnapshots.Max(s => s.Margin)
            : 0m;

        return new MetricasFinancieras(capitalInicial, resultado.CashFinal, equityFinal, pnlTotal, drawdownMaximo, exposicionMaxima);
    }

    // spec: D-073 — Peak(t) = max(Equity(0..t)), Drawdown(t) = (Peak(t)-Equity(t))/Peak(t),
    // DrawdownMax = max(Drawdown(t)). spec: D-078 — EquityCurve vacia -> null, nunca 0m.
    private static decimal? CalcularDrawdownMaximo(IReadOnlyList<EquityPoint> curva)
    {
        if (curva.Count == 0)
            return null;

        var pico = curva[0].Equity;
        var drawdownMaximo = 0m;

        foreach (var punto in curva)
        {
            pico = Math.Max(pico, punto.Equity);
            if (pico == 0m)
                continue; // sin capital, drawdown porcentual no definido para este punto
            var drawdown = (pico - punto.Equity) / pico;
            drawdownMaximo = Math.Max(drawdownMaximo, drawdown);
        }

        return drawdownMaximo;
    }
}
