using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Api.Demo;

// spec: deuda documentada en PENDIENTES.md — implementacion fija de IStrategy (Domain) usada
// por el endpoint POST /api/backtest/run mientras no exista un StrategyCatalog real. Referencia
// explicita a Domain.Strategy: TD_Project.Api implementa aqui un contrato de extension de
// usuario, no oculta la dependencia detras de Application.
internal sealed class EstrategiaDemo : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) =>
        dataSlice.N == 0
            ? new[] { new OrderRequest(Side.Buy, OrderType.Market, 1m) }
            : Array.Empty<OrderRequest>();
}
