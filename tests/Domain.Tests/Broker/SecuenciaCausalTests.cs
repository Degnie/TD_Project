using TD_Project.Domain.Broker;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Broker;

public class SecuenciaCausalTests
{
    // spec: RN-04 — Secuencia Causal es entera, monotona creciente, unica dentro del Experiment
    [Fact]
    public void CadaOrdenRegistradaRecibeUnaSecuenciaCausalEstrictamenteCreciente()
    {
        var registrador = new RegistradorOrdenes();
        var req1 = new OrderRequest(Side.Buy, OrderType.Market, 1m);
        var req2 = new OrderRequest(Side.Sell, OrderType.Market, 1m);

        var orden1 = registrador.Registrar(req1);
        var orden2 = registrador.Registrar(req2);

        Assert.True(orden2.SecuenciaCausal > orden1.SecuenciaCausal);
    }

    // spec: EC-02 — ejecuciones simultaneas se procesan en orden ascendente estricto de Secuencia Causal
    [Fact]
    public void LasOrdenesSimultaneasSeOrdenanPorSecuenciaCausalAscendente()
    {
        var ordenA = new Order { SecuenciaCausal = 5, Side = Side.Buy, Type = OrderType.Market, Cantidad = 1m };
        var ordenB = new Order { SecuenciaCausal = 2, Side = Side.Buy, Type = OrderType.Market, Cantidad = 1m };
        var ordenC = new Order { SecuenciaCausal = 8, Side = Side.Buy, Type = OrderType.Market, Cantidad = 1m };

        var ordenadas = OrdenadorPorSecuenciaCausal.Ordenar(new[] { ordenA, ordenB, ordenC });

        Assert.Equal(new long[] { 2, 5, 8 }, ordenadas.Select(o => o.SecuenciaCausal));
    }
}
