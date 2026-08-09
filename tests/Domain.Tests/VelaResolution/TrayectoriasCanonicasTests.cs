using TD_Project.Domain.Shared;
using TD_Project.Domain.VelaResolution;
using Xunit;

namespace TD_Project.Domain.Tests.VelaResolution;

public class TrayectoriasCanonicasTests
{
    // spec: RN-11 — se evaluan obligatoriamente ambas trayectorias, se selecciona la de menor Equity
    [Fact]
    public void SeSeleccionaLaTrayectoriaConMenorEquityFinal()
    {
        var ordenesPending = new[]
        {
            new Order { SecuenciaCausal = 1, Side = Side.Sell, Type = OrderType.Stop, Cantidad = 1m, PrecioStop = 98m }
        };
        var vela = new Candle(Timestamp: 2, Open: 100m, High: 105m, Low: 95m, Close: 101m, Volume: 500m);
        var portfolio = new TD_Project.Domain.Portfolio.PortfolioState { Cash = 1000m };

        var resultado = ResolutorVela.Resolver(ordenesPending, vela, portfolio);

        Assert.True(resultado.EquityFinal <= resultado.EquityDescartada);
    }

    // spec: RN-11 — desempate: si Equity_A == Equity_B, se selecciona A
    [Fact]
    public void EnCasoDeEmpateSeSeleccionaLaTrayectoriaA()
    {
        var ordenesPending = Array.Empty<Order>();
        var vela = new Candle(Timestamp: 2, Open: 100m, High: 100m, Low: 100m, Close: 100m, Volume: 0m);
        var portfolio = new TD_Project.Domain.Portfolio.PortfolioState { Cash = 1000m };

        var resultado = ResolutorVela.Resolver(ordenesPending, vela, portfolio);

        Assert.Equal(Trayectoria.A, resultado.TrayectoriaOficial);
    }

    // spec: CU-19 — OCO Multiple Ambiguo: motor evalua trayectorias, rama cruzada ejecuta, hermana se cancela
    [Fact]
    public void OcoAmbiguoResuelveLaRamaCruzadaYCancelaLaHermanaSegunTrayectoriaOficial()
    {
        var rama1 = new Order { SecuenciaCausal = 1, Side = Side.Sell, Type = OrderType.Limit, Cantidad = 1m, PrecioLimite = 104m };
        var rama2 = new Order { SecuenciaCausal = 2, Side = Side.Sell, Type = OrderType.Stop, Cantidad = 1m, PrecioStop = 96m };
        var grupo = new OcoGroup(new[] { rama1, rama2 });
        var vela = new Candle(Timestamp: 2, Open: 100m, High: 106m, Low: 94m, Close: 101m, Volume: 500m);
        var portfolio = new TD_Project.Domain.Portfolio.PortfolioState { Cash = 1000m };

        var resultado = ResolutorVela.ResolverOco(grupo, vela, portfolio);

        Assert.Single(resultado.Fills);
        Assert.Contains(resultado.OrdenesCanceladas, o => o.Status == OrderStatus.Cancelled);
    }
}
