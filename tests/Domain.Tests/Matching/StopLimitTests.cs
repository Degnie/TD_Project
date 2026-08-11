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

    // spec: RN-11 — el recorrido temporal simulado dentro de la vela debe poder cambiar si el
    // Stop-Limit hace Fill. Vela Open=100/High=102/Low=90/Close=102, Buy Stop=102/Limite=101.
    // Trayectoria A (O->H->L->C: 100->102->90->102): el Stop se dispara subiendo hacia el High
    // y, en ese mismo tramo ascendente, el precio ya cruzo el Limite=101 de camino -> Fill @101.
    // Trayectoria B (O->L->H->C: 100->90->102->102): el Stop recien se dispara al llegar a
    // High=102, sin tramo restante hasta Close=102 donde el Limite=101 sea alcanzable -> sin Fill.
    [Fact]
    public void StopLimitPuedeDivergirEntreTrayectorias()
    {
        var ordenA = new Order
        {
            SecuenciaCausal = 1,
            Side = Side.Buy,
            Type = OrderType.StopLimit,
            Cantidad = 1m,
            PrecioStop = 102m,
            PrecioLimite = 101m
        };
        var ordenB = new Order
        {
            SecuenciaCausal = 1,
            Side = Side.Buy,
            Type = OrderType.StopLimit,
            Cantidad = 1m,
            PrecioStop = 102m,
            PrecioLimite = 101m
        };
        var vela = new Candle(Timestamp: 2, Open: 100m, High: 102m, Low: 90m, Close: 102m, Volume: 500m);

        var fillA = MatchingEngine.Resolver(ordenA, vela, Trayectoria.A);
        var fillB = MatchingEngine.Resolver(ordenB, vela, Trayectoria.B);

        Assert.NotNull(fillA);
        Assert.Equal(101m, fillA!.PrecioFill);
        Assert.Null(fillB);
    }
}
