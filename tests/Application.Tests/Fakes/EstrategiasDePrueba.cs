using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Application.Tests.Fakes;

// spec: CU-04 — estrategia que nunca emite OrderRequests
internal sealed class EstrategiaCiega : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) => Array.Empty<OrderRequest>();
}

// spec: CU-06 — estrategia que intenta leer mas alla de N (viola el bloqueo de DataSlice)
internal sealed class EstrategiaConLookAhead : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        _ = dataSlice.VelasHastaN[dataSlice.N + 1];
        return Array.Empty<OrderRequest>();
    }
}

// spec: CU-01, RN-13 — emite un Market Buy en cada ciclo N
internal sealed class EstrategiaMarketSiempre : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) =>
        new[] { new OrderRequest(Side.Buy, OrderType.Market, 1m) };
}

// spec: CU-07 — deja una orden Limit sin ejecutar para verificar cancelacion al finalizar
internal sealed class EstrategiaConOrdenPendingAlFinal : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) =>
        new[] { new OrderRequest(Side.Buy, OrderType.Limit, 1m, PrecioLimite: 1m) };
}

// spec: RN-09, glosario "Trade" — abre Long 10 en N=0, reduce 4 en N=1, reduce 6 en N=2 (cierra a cero)
internal sealed class EstrategiaAperturaYDosReduccionesParciales : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) => dataSlice.N switch
    {
        0 => new[] { new OrderRequest(Side.Buy, OrderType.Market, 10m) },
        1 => new[] { new OrderRequest(Side.Sell, OrderType.Market, 4m) },
        2 => new[] { new OrderRequest(Side.Sell, OrderType.Market, 6m) },
        _ => Array.Empty<OrderRequest>()
    };
}
