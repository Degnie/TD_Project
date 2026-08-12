using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: Caso 2 D-066/D-067/D-068/D-070/D-071 — capa externa entre Strategy y ejecucion. No conoce
// direccion/logica de la estrategia, solo transforma Cantidad de ordenes ya existentes (D-071:
// nunca crea ni elimina ordenes de la bolsa). sizing=null -> Cantidad intacta (D-061/D-069,
// preserva baseline_final/ sin modificacion).
public static class GestorCapital
{
    public static IReadOnlyList<OrderRequest> Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing? sizing)
    {
        if (sizing is null)
            return requests;

        // spec: D-067 — CapitalDisponible = Cash - Margin (no Equity: PortfolioState no expone
        // UnrealizedPnL en este punto del ciclo, ver DECISIONES_MODELO_ECONOMICO_V1.md D-067).
        var capitalDisponible = portfolio.Cash - portfolio.Margin;
        var cantidadCalculada = capitalDisponible * sizing.PorcentajeRiesgo;

        return requests.Select(r => r with { Cantidad = cantidadCalculada }).ToList();
    }
}
