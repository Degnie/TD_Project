using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Strategy;

// spec: glosario "Strategy" — observa DataSlice(N) y emite OrderRequests
public interface IStrategy
{
    IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice);
}
