using TD_Project.Domain.Matching;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Matching;

public class StopLimitTests
{
    // spec: CU-13 — Stop-Limit Buy $100/$102. Vela 95/105/95 (Trayectoria A: O->H->L->C).
    // Stop $100 se dispara subiendo hacia el High, Limit $102 intercepta bajando -> Fill $102.
    [Fact]
    public void StopSeDisparaYLimitInterceptaEnLaMismaVela()
    {
        var orden = new Order
        {
            SecuenciaCausal = 1,
            Side = Side.Buy,
            Type = OrderType.StopLimit,
            Cantidad = 1m,
            PrecioStop = 100m,
            PrecioLimite = 102m
        };
        var vela = new Candle(Timestamp: 2, Open: 95m, High: 105m, Low: 95m, Close: 98m, Volume: 500m);

        var fill = MatchingEngine.Resolver(orden, vela, Trayectoria.A);

        Assert.NotNull(fill);
        Assert.Equal(102m, fill!.PrecioFill);
    }

    // spec: CU-14 — Stop-Limit gatillado sin alcanzar el Limit -> muta a Pending Limit
    [Fact]
    public void StopDisparadoSinAlcanzarElLimitMutaAPendingLimit()
    {
        var orden = new Order
        {
            SecuenciaCausal = 1,
            Side = Side.Buy,
            Type = OrderType.StopLimit,
            Cantidad = 1m,
            PrecioStop = 100m,
            PrecioLimite = 110m
        };
        var vela = new Candle(Timestamp: 2, Open: 95m, High: 105m, Low: 95m, Close: 101m, Volume: 500m);

        var fill = MatchingEngine.Resolver(orden, vela, Trayectoria.A);

        Assert.Null(fill);
        Assert.Equal(OrderStatus.Pending, orden.Status);
        Assert.Equal(OrderType.Limit, orden.Type);
    }
}
