using TD_Project.Domain.Matching;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

namespace TD_Project.Domain.VelaResolution;

// spec: RN-11 — coordina Matching Engine y Portfolio sobre ambas trayectorias candidatas (A y B),
// sin contaminacion cruzada, y selecciona la oficial por Equity minimo (desempate: A).
public static class ResolutorVela
{
    // spec: Caso 2 D-062 — tasaMargen se propaga desde el Instrumento del experimento; el default
    // preserva el comportamiento historico para call sites que no lo especifican (D-061).
    // spec: Caso 2 D-063/D-064/D-065 — costes se propagan desde la configuracion economica del
    // experimento (no del Instrumento); default null = coste cero (preserva baseline Caso 1).
    public static ResultadoResolucionVela Resolver(IReadOnlyList<Order> ordenesPending, Candle vela, PortfolioState portfolio, decimal tasaMargen = 0.1m, ConfiguracionCostes? costes = null)
    {
        var (fillsA, portfolioA) = ResolverRama(ordenesPending, vela, Trayectoria.A, portfolio, tasaMargen, costes);
        var (fillsB, portfolioB) = ResolverRama(ordenesPending, vela, Trayectoria.B, portfolio, tasaMargen, costes);

        var equityA = CalcularEquity(portfolioA, vela);
        var equityB = CalcularEquity(portfolioB, vela);

        // spec: RN-11 — Trayectoria_Oficial = argmin(Equity_A, Equity_B); desempate: A
        var oficialEsA = equityA <= equityB;
        var portfolioOficial = oficialEsA ? portfolioA : portfolioB;

        return new ResultadoResolucionVela(
            TrayectoriaOficial: oficialEsA ? Trayectoria.A : Trayectoria.B,
            EquityFinal: oficialEsA ? equityA : equityB,
            EquityDescartada: oficialEsA ? equityB : equityA,
            Fills: oficialEsA ? fillsA : fillsB,
            OrdenesCanceladas: Array.Empty<Order>(),
            FillsA: fillsA,
            FillsB: fillsB,
            EquityA: equityA,
            EquityB: equityB,
            CashFinal: portfolioOficial.Cash,
            MarginFinal: portfolioOficial.Margin,
            UnrealizedPnLFinal: CalcularUnrealizedPnL(portfolioOficial, vela.Close),
            LotesVivosFinal: portfolioOficial.LotesVivos.ToList());
    }

    public static ResultadoResolucionVela ResolverOco(OcoGroup grupo, Candle vela, PortfolioState portfolio, decimal tasaMargen = 0.1m, ConfiguracionCostes? costes = null)
    {
        var (canceladasA, fillsA, portfolioA) = ResolverRamaOco(grupo, vela, Trayectoria.A, portfolio, tasaMargen, costes);
        var (canceladasB, fillsB, portfolioB) = ResolverRamaOco(grupo, vela, Trayectoria.B, portfolio, tasaMargen, costes);

        var equityA = CalcularEquity(portfolioA, vela);
        var equityB = CalcularEquity(portfolioB, vela);
        var oficialEsA = equityA <= equityB;
        var portfolioOficial = oficialEsA ? portfolioA : portfolioB;

        return new ResultadoResolucionVela(
            TrayectoriaOficial: oficialEsA ? Trayectoria.A : Trayectoria.B,
            EquityFinal: oficialEsA ? equityA : equityB,
            EquityDescartada: oficialEsA ? equityB : equityA,
            Fills: oficialEsA ? fillsA : fillsB,
            OrdenesCanceladas: oficialEsA ? canceladasA : canceladasB,
            FillsA: fillsA,
            FillsB: fillsB,
            EquityA: equityA,
            EquityB: equityB,
            CashFinal: portfolioOficial.Cash,
            MarginFinal: portfolioOficial.Margin,
            UnrealizedPnLFinal: CalcularUnrealizedPnL(portfolioOficial, vela.Close),
            LotesVivosFinal: portfolioOficial.LotesVivos.ToList());
    }

    private static (List<Fill> Fills, PortfolioState Portfolio) ResolverRama(
        IReadOnlyList<Order> ordenesPending, Candle vela, Trayectoria trayectoria, PortfolioState portfolioOriginal, decimal tasaMargen, ConfiguracionCostes? costes)
    {
        var portfolioRama = portfolioOriginal.Clonar();
        var fills = new List<Fill>();

        foreach (var ordenOriginal in ordenesPending)
        {
            var ordenClonada = ordenOriginal.Clonar();
            var fill = MatchingEngine.Resolver(ordenClonada, vela, trayectoria, costes);
            if (fill is not null)
            {
                fills.Add(fill);
                AplicadorFill.Aplicar(portfolioRama, fill, tasaMargen);
            }
        }

        return (fills, portfolioRama);
    }

    private static (List<Order> Canceladas, List<Fill> Fills, PortfolioState Portfolio) ResolverRamaOco(
        OcoGroup grupo, Candle vela, Trayectoria trayectoria, PortfolioState portfolioOriginal, decimal tasaMargen, ConfiguracionCostes? costes)
    {
        var portfolioRama = portfolioOriginal.Clonar();
        var ramasClonadas = grupo.Ramas.Select(r => r.Clonar()).OrderBy(r => r.SecuenciaCausal).ToList();
        var fills = new List<Fill>();
        var canceladas = new List<Order>();

        foreach (var rama in ramasClonadas)
        {
            var fill = MatchingEngine.Resolver(rama, vela, trayectoria, costes);
            if (fill is not null)
            {
                fills.Add(fill);
                AplicadorFill.Aplicar(portfolioRama, fill, tasaMargen);
                foreach (var hermana in ramasClonadas.Where(h => h != rama && h.Status == OrderStatus.Pending))
                {
                    OrdenTransiciones.Cancelar(hermana);
                    canceladas.Add(hermana);
                }
                break;
            }
        }

        return (canceladas, fills, portfolioRama);
    }

    // spec: RN-08, glosario "Equity", "Unrealized PnL" — Equity = Cash + Margin + UnrealizedPnL.
    // UnrealizedPnL es la valoracion M2M de la posicion viva al ultimo Close conocido.
    private static decimal CalcularEquity(PortfolioState portfolio, Candle vela) =>
        portfolio.Cash + portfolio.Margin + CalcularUnrealizedPnL(portfolio, vela.Close);

    // spec: RN-08 — UnrealizedPnL = Sum(Cantidad_lote * (Close_actual - PrecioEntrada_lote))
    private static decimal CalcularUnrealizedPnL(PortfolioState portfolio, decimal closeActual) =>
        portfolio.LotesVivos.Sum(lote => lote.Cantidad * (closeActual - lote.PrecioEntrada));
}
