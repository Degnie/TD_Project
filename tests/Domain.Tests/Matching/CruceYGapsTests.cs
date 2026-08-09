using TD_Project.Domain.Matching;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Matching;

public class CruceYGapsTests
{
    // spec: CU-08
    [Fact]
    public void SignalMarketEnNEjecutaAlOpenDeNMasUno()
    {
        var orden = new Order { SecuenciaCausal = 1, Side = Side.Buy, Type = OrderType.Market, Cantidad = 1m };
        var velaSiguiente = new Candle(Timestamp: 2, Open: 100m, High: 105m, Low: 98m, Close: 103m, Volume: 500m);

        var fill = MatchingEngine.Resolver(orden, velaSiguiente, Trayectoria.A);

        Assert.NotNull(fill);
        Assert.Equal(100m, fill!.PrecioFill);
    }

    // spec: RN-03, CU-09 — Limit Buy $100, Open $90 -> Fill $90 (gap atraviesa el precio solicitado)
    [Fact]
    public void GapDeAperturaQueAtraviesaElPrecioLimiteEjecutaAlOpen()
    {
        var orden = new Order { SecuenciaCausal = 1, Side = Side.Buy, Type = OrderType.Limit, Cantidad = 1m, PrecioLimite = 100m };
        var vela = new Candle(Timestamp: 2, Open: 90m, High: 95m, Low: 85m, Close: 92m, Volume: 500m);

        var fill = MatchingEngine.Resolver(orden, vela, Trayectoria.A);

        Assert.NotNull(fill);
        Assert.Equal(90m, fill!.PrecioFill);
    }

    // spec: RN-03, CU-10 — Limit Buy $100, Open $105, Low $95 -> Fill $100 (rango cruza, no el open)
    [Fact]
    public void CuandoElOpenNoAtraviesaPeroElRangoSiCruzaEjecutaAlPrecioSolicitado()
    {
        var orden = new Order { SecuenciaCausal = 1, Side = Side.Buy, Type = OrderType.Limit, Cantidad = 1m, PrecioLimite = 100m };
        var vela = new Candle(Timestamp: 2, Open: 105m, High: 106m, Low: 95m, Close: 101m, Volume: 500m);

        var fill = MatchingEngine.Resolver(orden, vela, Trayectoria.A);

        Assert.NotNull(fill);
        Assert.Equal(100m, fill!.PrecioFill);
    }

    // spec: CU-11
    [Fact]
    public void UnaLimitNoAlcanzadaSigueEnPending()
    {
        var orden = new Order { SecuenciaCausal = 1, Side = Side.Buy, Type = OrderType.Limit, Cantidad = 1m, PrecioLimite = 80m };
        var vela = new Candle(Timestamp: 2, Open: 100m, High: 106m, Low: 95m, Close: 101m, Volume: 500m);

        var fill = MatchingEngine.Resolver(orden, vela, Trayectoria.A);

        Assert.Null(fill);
        Assert.Equal(OrderStatus.Pending, orden.Status);
    }

    // spec: RN-03, EC-01 — igualdad exacta cuenta como cruce (evaluacion inclusiva, >=/<=)
    [Fact]
    public void UnaIgualdadExactaEntrePrecioYRangoEjecuta()
    {
        var orden = new Order { SecuenciaCausal = 1, Side = Side.Buy, Type = OrderType.Limit, Cantidad = 1m, PrecioLimite = 100m };
        var vela = new Candle(Timestamp: 2, Open: 105m, High: 106m, Low: 100.00m, Close: 101m, Volume: 500m);

        var fill = MatchingEngine.Resolver(orden, vela, Trayectoria.A);

        Assert.NotNull(fill);
        Assert.Equal(100m, fill!.PrecioFill);
    }
}
