using TD_Project.Domain.Matching;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Matching;

public class OcoTests
{
    // spec: RN-05, EC-02 — escalonadas $90/$80/$70, Open $60 -> cruzan todas por gap;
    // la primera en ejecutar (por Secuencia Causal) cancela a sus hermanas OCO.
    [Fact]
    public void LaRamaQueEjecutaCancelaAtomicamenteALasHermanas()
    {
        var rama1 = new Order { SecuenciaCausal = 1, Side = Side.Sell, Type = OrderType.Limit, Cantidad = 1m, PrecioLimite = 90m };
        var rama2 = new Order { SecuenciaCausal = 2, Side = Side.Sell, Type = OrderType.Limit, Cantidad = 1m, PrecioLimite = 80m };
        var rama3 = new Order { SecuenciaCausal = 3, Side = Side.Sell, Type = OrderType.Limit, Cantidad = 1m, PrecioLimite = 70m };
        var grupo = new OcoGroup(new[] { rama1, rama2, rama3 });
        var vela = new Candle(Timestamp: 2, Open: 60m, High: 65m, Low: 55m, Close: 61m, Volume: 500m);

        MatchingEngine.ResolverOco(grupo, vela, Trayectoria.A);

        Assert.Equal(OrderStatus.Executed, rama1.Status);
        Assert.Equal(OrderStatus.Cancelled, rama2.Status);
        Assert.Equal(OrderStatus.Cancelled, rama3.Status);
    }
}
