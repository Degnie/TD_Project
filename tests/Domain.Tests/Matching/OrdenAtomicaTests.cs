using TD_Project.Domain.Matching;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Matching;

public class OrdenAtomicaTests
{
    // spec: RN-02
    [Fact]
    public void UnFillSatisfaceElCienPorCientoDeLaCantidadSolicitada()
    {
        var orden = new Order
        {
            SecuenciaCausal = 1,
            Side = Side.Buy,
            Type = OrderType.Market,
            Cantidad = 10m
        };
        var vela = new Candle(Timestamp: 1, Open: 100m, High: 105m, Low: 95m, Close: 102m, Volume: 1000m);

        var fill = MatchingEngine.Resolver(orden, vela, Trayectoria.A);

        Assert.NotNull(fill);
        Assert.Equal(orden.Cantidad, fill!.Cantidad);
    }
}
