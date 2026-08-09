using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Infrastructure.Tests;

// spec: RNF-08 — vehiculo de prueba, emite un Market Buy en cada ciclo N
internal sealed class EstrategiaMarketSiempreParaFillLog : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) =>
        new[] { new OrderRequest(Side.Buy, OrderType.Market, 1m) };
}
