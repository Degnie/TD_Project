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
}
