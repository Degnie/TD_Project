using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: RN-04, EC-02 — procesamiento contable en orden estrictamente ascendente de Secuencia Causal
public static class OrdenadorPorSecuenciaCausal
{
    public static IReadOnlyList<Order> Ordenar(IEnumerable<Order> ordenes) =>
        ordenes.OrderBy(o => o.SecuenciaCausal).ToList();
}
