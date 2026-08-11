using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Laboratorio.Fixtures;

// Estrategia companera de FixtureCrossZero: emite exactamente la secuencia de OrderRequests
// que reproduce el caso de tres Cross-Zero consecutivos. No es una estrategia de trading real,
// es parte del escenario de validacion del motor.
public sealed class EstrategiaFixtureCrossZero : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) => dataSlice.N switch
    {
        0 => new[] { new OrderRequest(Side.Buy, OrderType.Market, 10m) },
        1 => new[] { new OrderRequest(Side.Sell, OrderType.Market, 15m) },
        2 => new[] { new OrderRequest(Side.Buy, OrderType.Market, 8m) },
        3 => new[] { new OrderRequest(Side.Sell, OrderType.Market, 6m) },
        _ => Array.Empty<OrderRequest>()
    };
}
