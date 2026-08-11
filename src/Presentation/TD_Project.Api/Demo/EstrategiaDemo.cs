using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Api.Demo;

// spec: deuda documentada en PENDIENTES.md — implementacion fija de IStrategy (Domain) usada
// por el endpoint POST /api/backtest/run mientras no exista un StrategyCatalog real. Referencia
// explicita a Domain.Strategy: TD_Project.Api implementa aqui un contrato de extension de
// usuario, no oculta la dependencia detras de Application.
// spec: RN-11 — emite en N=0 el Stop-Limit del caso canonico de divergencia (Stop=102/Limite=101,
// ver DatasetDemo), para que la demo en vivo muestre BranchResolutions con trayectorias A/B
// realmente distintas, no el caso degenerado A=B de un Market Buy sin ambiguedad posible.
internal sealed class EstrategiaDemo : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) =>
        dataSlice.N == 0
            ? new[] { new OrderRequest(Side.Buy, OrderType.StopLimit, 1m, PrecioStop: 102m, PrecioLimite: 101m) }
            : Array.Empty<OrderRequest>();
}
