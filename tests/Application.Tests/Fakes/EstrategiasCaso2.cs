using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Application.Tests.Fakes;

// spec: Caso 2 D-063 P3 — Limit que se dispara de inmediato al precio de la vela actual (Open),
// para verificar que el slippage (solo Market) no afecta ordenes Limit.
internal sealed class EstrategiaLimitSiempreAlPrecioDeEntrada : IStrategy
{
    // Buy Limit se ejecuta cuando Open <= PrecioLimite (RN-03) — 1000m garantiza fill inmediato
    // en N=0 contra el dataset de prueba (Open ~100-104), sin depender de rangos de vela.
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) => dataSlice.N switch
    {
        0 => new[] { new OrderRequest(Side.Buy, OrderType.Limit, 1m, PrecioLimite: 1000m) },
        _ => Array.Empty<OrderRequest>()
    };
}

// spec: Caso 2 D-065 P4 — mismo patron que EstrategiaTresCrossZeroConsecutivos (Domain.Tests) pero
// con un unico cruce: N=0 abre Long 10, N=1 vende 15 (cierra 10, cruza cero, abre Short 5).
internal sealed class EstrategiaCrossZeroControlada : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) => dataSlice.N switch
    {
        0 => new[] { new OrderRequest(Side.Buy, OrderType.Market, 10m) },
        1 => new[] { new OrderRequest(Side.Sell, OrderType.Market, 15m) },
        _ => Array.Empty<OrderRequest>()
    };
}

// spec: Caso 2 D-071 P4 — emite 2 OrderRequest Buy en el mismo ciclo (misma bolsa, RN-14: mismo
// Side no genera contradiccion) para verificar que GestorCapital calcula la misma Cantidad para
// ambas, sobre el mismo CapitalDisponible (portfolio no cambia dentro del mismo ciclo N).
internal sealed class EstrategiaOcoDosOrdenes : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) => dataSlice.N switch
    {
        0 => new[]
        {
            new OrderRequest(Side.Buy, OrderType.Limit, 1m, PrecioLimite: 1000m),
            new OrderRequest(Side.Buy, OrderType.Limit, 1m, PrecioLimite: 999m)
        },
        _ => Array.Empty<OrderRequest>()
    };
}
