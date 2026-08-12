using TD_Project.Application;
using TD_Project.Domain.Portfolio;

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
        // spec: Caso 5A D-111 — MargenMaximoUtilizado NO es un campo nuevo: es exactamente el
        // mismo calculo que ExposicionMaxima ya tenia (Max(s => s.Margin)) — equivalencia
        // documentada en DECISIONES_CASO5_V1.md, sin duplicar fuente (D-072).
        var exposicionMaxima = resultado.PortfolioSnapshots.Count > 0
            ? resultado.PortfolioSnapshots.Max(s => s.Margin)
            : 0m;
        var profitFactor = CalcularProfitFactor(resultado.Trades);
        var capitalLibreMinimo = CalcularCapitalLibreMinimo(resultado.PortfolioSnapshots, capitalInicial);

        return new MetricasFinancieras(capitalInicial, resultado.CashFinal, equityFinal, pnlTotal, drawdownMaximo, exposicionMaxima, profitFactor, capitalLibreMinimo);
    }

    // spec: Caso 5A D-111 — ProfitFactor = suma(ganancias) / suma(|perdidas|), sobre Trades ya
    // calculados por el motor (D-072/D-077, ninguna fuente nueva). Null si no hay perdidas (evita
    // division por cero) — mismo criterio decimal? que DrawdownMaximoPct (D-078): null nunca se
    // confunde con un valor numerico (ej. 0 perdidas no es "profit factor cero").
    private static decimal? CalcularProfitFactor(IReadOnlyList<Trade> trades)
    {
        var ganancias = trades.Where(t => t.RealizedPnL > 0m).Sum(t => t.RealizedPnL);
        var perdidas = trades.Where(t => t.RealizedPnL < 0m).Sum(t => -t.RealizedPnL);
        return perdidas == 0m ? null : ganancias / perdidas;
    }

    // spec: Caso 5A D-111 — CapitalLibreMinimo = min(Cash - Margin) sobre PortfolioSnapshots, mas
    // el estado inicial (Cash=CapitalInicial, Margin=0) que ningun snapshot registra explicitamente.
    private static decimal CalcularCapitalLibreMinimo(IReadOnlyList<PortfolioSnapshot> snapshots, decimal capitalInicial)
    {
        var capitalLibreMinimo = capitalInicial;
        foreach (var snapshot in snapshots)
        {
            var capitalLibre = snapshot.Cash - snapshot.Margin;
            if (capitalLibre < capitalLibreMinimo)
                capitalLibreMinimo = capitalLibre;
        }
        return capitalLibreMinimo;
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
