using TD_Project.Domain.Matching;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

namespace TD_Project.Domain.VelaResolution;

// spec: RN-11 — coordina Matching Engine y Portfolio sobre ambas trayectorias candidatas (A y B),
// sin contaminacion cruzada, y selecciona la oficial por Equity minimo (desempate: A).
public static class ResolutorVela
{
    public static ResultadoResolucionVela Resolver(IReadOnlyList<Order> ordenesPending, Candle vela, PortfolioState portfolio)
    {
        var (fillsA, portfolioA) = ResolverRama(ordenesPending, vela, Trayectoria.A, portfolio);
        var (fillsB, portfolioB) = ResolverRama(ordenesPending, vela, Trayectoria.B, portfolio);

        var equityA = CalcularEquity(portfolioA);
        var equityB = CalcularEquity(portfolioB);

        // spec: RN-11 — Trayectoria_Oficial = argmin(Equity_A, Equity_B); desempate: A
        var oficialEsA = equityA <= equityB;

        return new ResultadoResolucionVela(
            TrayectoriaOficial: oficialEsA ? Trayectoria.A : Trayectoria.B,
            EquityFinal: oficialEsA ? equityA : equityB,
            EquityDescartada: oficialEsA ? equityB : equityA,
            Fills: oficialEsA ? fillsA : fillsB,
            OrdenesCanceladas: Array.Empty<Order>());
    }

    public static ResultadoResolucionVela ResolverOco(OcoGroup grupo, Candle vela, PortfolioState portfolio)
    {
        var (canceladasA, fillsA, portfolioA) = ResolverRamaOco(grupo, vela, Trayectoria.A, portfolio);
        var (canceladasB, fillsB, portfolioB) = ResolverRamaOco(grupo, vela, Trayectoria.B, portfolio);

        var equityA = CalcularEquity(portfolioA);
        var equityB = CalcularEquity(portfolioB);
        var oficialEsA = equityA <= equityB;

        return new ResultadoResolucionVela(
            TrayectoriaOficial: oficialEsA ? Trayectoria.A : Trayectoria.B,
            EquityFinal: oficialEsA ? equityA : equityB,
            EquityDescartada: oficialEsA ? equityB : equityA,
            Fills: oficialEsA ? fillsA : fillsB,
            OrdenesCanceladas: oficialEsA ? canceladasA : canceladasB);
    }

    private static (List<Fill> Fills, PortfolioState Portfolio) ResolverRama(
        IReadOnlyList<Order> ordenesPending, Candle vela, Trayectoria trayectoria, PortfolioState portfolioOriginal)
    {
        var portfolioRama = portfolioOriginal.Clonar();
        var fills = new List<Fill>();

        foreach (var ordenOriginal in ordenesPending)
        {
            var ordenClonada = ordenOriginal.Clonar();
            var fill = MatchingEngine.Resolver(ordenClonada, vela, trayectoria);
            if (fill is not null)
            {
                fills.Add(fill);
                AplicadorFill.Aplicar(portfolioRama, fill);
            }
        }

        return (fills, portfolioRama);
    }

    private static (List<Order> Canceladas, List<Fill> Fills, PortfolioState Portfolio) ResolverRamaOco(
        OcoGroup grupo, Candle vela, Trayectoria trayectoria, PortfolioState portfolioOriginal)
    {
        var portfolioRama = portfolioOriginal.Clonar();
        var ramasClonadas = grupo.Ramas.Select(r => r.Clonar()).OrderBy(r => r.SecuenciaCausal).ToList();
        var fills = new List<Fill>();
        var canceladas = new List<Order>();

        foreach (var rama in ramasClonadas)
        {
            var fill = MatchingEngine.Resolver(rama, vela, trayectoria);
            if (fill is not null)
            {
                fills.Add(fill);
                AplicadorFill.Aplicar(portfolioRama, fill);
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

    // spec: glosario "Equity" — Equity = Cash + Margin + UnrealizedPnL. Sin M2M (Unrealized) en este alcance minimo: 0.
    private static decimal CalcularEquity(PortfolioState portfolio) => portfolio.Cash + portfolio.Margin;
}
