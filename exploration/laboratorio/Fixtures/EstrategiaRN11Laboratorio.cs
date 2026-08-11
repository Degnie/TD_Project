using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Laboratorio.Fixtures;

// Estrategia companera de EscenarioRN11: emite el Stop-Limit exacto del caso canonico de
// divergencia en N=0. No es una estrategia de trading real, es parte del escenario de
// validacion del motor.
public sealed class EstrategiaRN11Laboratorio : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) => dataSlice.N switch
    {
        0 => new[] { new OrderRequest(Side.Buy, OrderType.StopLimit, 1m, PrecioStop: EscenarioRN11.PrecioStop, PrecioLimite: EscenarioRN11.PrecioLimite) },
        _ => Array.Empty<OrderRequest>()
    };
}
