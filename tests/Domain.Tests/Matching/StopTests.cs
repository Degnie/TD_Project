using TD_Project.Domain.Matching;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Matching;

public class StopTests
{
    // spec: CU-12 — Stop Sell $100, Open $90 -> Fill $90 (peor precio, gap atraviesa)
    [Fact]
    public void UnGapQueAtraviesaElPrecioStopEjecutaAlOpen()
    {
        var orden = new Order { SecuenciaCausal = 1, Side = Side.Sell, Type = OrderType.Stop, Cantidad = 1m, PrecioStop = 100m };
        var vela = new Candle(Timestamp: 2, Open: 90m, High: 95m, Low: 85m, Close: 92m, Volume: 500m);

        var fill = MatchingEngine.Resolver(orden, vela, Trayectoria.A);

        Assert.NotNull(fill);
        Assert.Equal(90m, fill!.PrecioFill);
    }
}
