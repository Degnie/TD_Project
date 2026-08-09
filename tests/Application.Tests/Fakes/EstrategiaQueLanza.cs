using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Application.Tests.Fakes;

// spec: EC-04 — aborto no manejado dentro de la Strategy
internal sealed class EstrategiaQueLanza : IStrategy
{
    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice) =>
        throw new InvalidOperationException("Fallo simulado no controlado.");
}
