using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Application.Tests.Fakes;

// spec: RN-12, CU-15 — N=0 emite una Market Buy sobredimensionada frente al capital disponible
// (fuerza el rechazo de ValidadorCapacidad); N=1 emite una Market Buy de capacidad valida, para
// verificar que una orden bloqueada no impide que la siguiente se registre y ejecute con normalidad.
internal sealed class EstrategiaCapacidadInsuficiente : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) => dataSlice.N switch
    {
        0 => new[] { new OrderRequest(Side.Buy, OrderType.Market, 1000m) },
        1 => new[] { new OrderRequest(Side.Buy, OrderType.Market, 1m) },
        _ => Array.Empty<OrderRequest>()
    };
}
