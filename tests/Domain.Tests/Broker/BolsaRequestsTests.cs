using TD_Project.Domain.Broker;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Broker;

public class BolsaRequestsTests
{
    // spec: RN-14 — bolsa evaluada junta; sin contradiccion, se procesa normalmente
    [Fact]
    public void UnaBolsaSinContradiccionSeAprueba()
    {
        var bolsa = new[]
        {
            new OrderRequest(Side.Buy, OrderType.Market, 1m),
            new OrderRequest(Side.Buy, OrderType.Market, 2m)
        };

        var resultado = ValidadorBolsaRequests.Evaluar(bolsa);

        Assert.True(resultado.Aprobada);
    }

    // spec: RN-14, CU-20 — Buy + Sell en el mismo ciclo N para un activo -> rechazo total de la bolsa
    [Fact]
    public void UnaBolsaConBuyYSellSeRechazaCompleta()
    {
        var bolsa = new[]
        {
            new OrderRequest(Side.Buy, OrderType.Market, 1m),
            new OrderRequest(Side.Sell, OrderType.Market, 1m)
        };

        var resultado = ValidadorBolsaRequests.Evaluar(bolsa);

        Assert.False(resultado.Aprobada);
        Assert.Equal(bolsa.Length, resultado.Rechazadas.Count);
    }

    // spec: RN-08, RN-14 — la direccion pertenece exclusivamente a Side; Cantidad debe ser
    // magnitud estrictamente positiva. Una Cantidad negativa duplicaria la fuente de verdad de
    // direccion (Side contra el signo de Cantidad), pudiendo contradecirse.
    [Fact]
    public void OrderRequestConCantidadNegativaEsRechazado()
    {
        var bolsa = new[] { new OrderRequest(Side.Buy, OrderType.Market, Cantidad: -10m) };

        var resultado = ValidadorBolsaRequests.Evaluar(bolsa);

        Assert.False(resultado.Aprobada);
        Assert.Equal(bolsa.Length, resultado.Rechazadas.Count);
    }

    // spec: RN-08, RN-14 — Cantidad = 0 no representa ninguna operacion real, viola la misma
    // invariante de magnitud estrictamente positiva.
    [Fact]
    public void OrderRequestConCantidadCeroEsRechazado()
    {
        var bolsa = new[] { new OrderRequest(Side.Buy, OrderType.Market, Cantidad: 0m) };

        var resultado = ValidadorBolsaRequests.Evaluar(bolsa);

        Assert.False(resultado.Aprobada);
        Assert.Equal(bolsa.Length, resultado.Rechazadas.Count);
    }
}
