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

    // spec: RN-08 — Equity = Cash + Margin + UnrealizedPnL. La posicion viva al cierre de la
    // vela se valora M2M contra el ultimo Close conocido (glosario "Unrealized PnL", "Equity").
    // Entrada $100, Cantidad 10, Close $110 -> UnrealizedPnL = 10 * (110 - 100) = 100.
    // Con Cash inicial 1000 y tasaMargen por defecto 0.1: Margin = 10*100*0.1 = 100, Cash = 900.
    // Equity esperado = 900 + 100 + 100 = 1100. Con la formula anterior (Cash + Margin) habria
    // dado 1000, sin el componente UnrealizedPnL.
    [Fact]
    public void ElEquityIncluyeLaValoracionM2MDeLaPosicionVivaAlCierre()
    {
        var ordenesPending = new[]
        {
            new Order { SecuenciaCausal = 1, Side = Side.Buy, Type = OrderType.Market, Cantidad = 10m }
        };
        var vela = new Candle(Timestamp: 2, Open: 100m, High: 112m, Low: 98m, Close: 110m, Volume: 500m);
        var portfolio = new TD_Project.Domain.Portfolio.PortfolioState { Cash = 1000m };

        var resultado = ResolutorVela.Resolver(ordenesPending, vela, portfolio);

        Assert.Equal(1100m, resultado.EquityFinal);
    }

    // spec: RN-11 — el resultado conserva evidencia de AMBAS trayectorias evaluadas (Fills,
    // Equity), no solo la seleccionada. Sell Stop @98 en vela Open 100/High 105/Low 95/Close 101:
    // rama A (O-H-L-C) y rama B (O-L-H-C) ambas disparan el Stop (Low 95 <= 98), mismo resultado
    // en este caso, pero ambas listas deben estar presentes independientemente de la seleccion.
    [Fact]
    public void ElResultadoConservaFillsYEquityDeAmbasTrayectorias()
    {
        var ordenesPending = new[]
        {
            new Order { SecuenciaCausal = 1, Side = Side.Sell, Type = OrderType.Stop, Cantidad = 1m, PrecioStop = 98m }
        };
        var vela = new Candle(Timestamp: 2, Open: 100m, High: 105m, Low: 95m, Close: 101m, Volume: 500m);
        var portfolio = new TD_Project.Domain.Portfolio.PortfolioState { Cash = 1000m };

        var resultado = ResolutorVela.Resolver(ordenesPending, vela, portfolio);

        Assert.Single(resultado.FillsA);
        Assert.Single(resultado.FillsB);
        Assert.Equal(resultado.EquityA <= resultado.EquityB ? resultado.EquityA : resultado.EquityB, resultado.EquityFinal);
        Assert.Equal(Trayectoria.A, resultado.TrayectoriaOficial);
    }

    // spec: RN-08, RN-11 — el desglose Cash/Margin/UnrealizedPnL/LotesVivos expuesto corresponde
    // a la rama OFICIAL unicamente (no a una mezcla ni a la rama descartada).
    [Fact]
    public void ElDesgloseDeEquityCorrespondeALaRamaOficial()
    {
        var ordenesPending = new[]
        {
            new Order { SecuenciaCausal = 1, Side = Side.Buy, Type = OrderType.Market, Cantidad = 10m }
        };
        var vela = new Candle(Timestamp: 2, Open: 100m, High: 112m, Low: 98m, Close: 110m, Volume: 500m);
        var portfolio = new TD_Project.Domain.Portfolio.PortfolioState { Cash = 1000m };

        var resultado = ResolutorVela.Resolver(ordenesPending, vela, portfolio);

        Assert.Equal(900m, resultado.CashFinal);
        Assert.Equal(100m, resultado.MarginFinal);
        Assert.Equal(100m, resultado.UnrealizedPnLFinal);
        Assert.Equal(900m + 100m + 100m, resultado.EquityFinal);
        Assert.Single(resultado.LotesVivosFinal);
    }
}
