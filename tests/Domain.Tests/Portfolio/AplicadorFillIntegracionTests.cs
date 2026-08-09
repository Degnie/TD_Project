using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Portfolio;

public class AplicadorFillIntegracionTests
{
    // spec: RN-09, CU-17 — Fill contrario a una posicion viva reduce mediante FIFO real,
    // no abre un lote nuevo con signo contrario. Long 2@$50 + Long 8@$60 (via dos Fills Buy),
    // Sell 4 @ $70 consume 2@$50 y 2@$60. RealizedPnL = 2*(70-50) + 2*(70-60) = 40+20 = 60.
    // MarginLiberado = 2*50*0.1 + 2*60*0.1 = 10+12 = 22.
    [Fact]
    public void UnFillDeReduccionConsumeLotesFIFOEnFlujoReal()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 2m, 50m, 0m, 1, OrderType.Market));
        AplicadorFill.Aplicar(portfolio, new Fill(2, Side.Buy, 8m, 60m, 0m, 2, OrderType.Market));

        var resultado = AplicadorFill.Aplicar(portfolio, new Fill(3, Side.Sell, 4m, 70m, 0m, 3, OrderType.Market));

        Assert.Equal(60m, resultado.RealizedPnLReconocido);
        Assert.Equal(22m, resultado.MarginLiberado);
        Assert.Null(resultado.TradeCerrado);
        Assert.Equal(6m, PosicionActual.De(portfolio));
    }

    // spec: RN-10, CU-18 — Fill que invierte la posicion cierra el Trade activo (RealizedPnL,
    // libera todo el Margin viejo) y abre uno nuevo por el excedente. Long 5@$100, Sell 8@$110:
    // cierra Long 5 (RealizedPnL = 5*(110-100) = 50, Margin liberado = 5*100*0.1 = 50),
    // abre Short 3 (Margin nuevo = 3*110*0.1 = 33).
    [Fact]
    public void UnFillCrossZeroCierraLaPosicionAnteriorYAbreUnaNueva()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 5m, 100m, 0m, 1, OrderType.Market));

        var resultado = AplicadorFill.Aplicar(portfolio, new Fill(2, Side.Sell, 8m, 110m, 0m, 2, OrderType.Market));

        Assert.Equal(50m, resultado.RealizedPnLReconocido);
        Assert.Equal(50m, resultado.MarginLiberado);
        Assert.NotNull(resultado.TradeCerrado);
        Assert.Equal(5m, resultado.TradeCerrado!.CantidadInicial);
        Assert.Equal(100m, resultado.TradeCerrado.PrecioApertura);
        Assert.Equal(110m, resultado.TradeCerrado.PrecioCierre);
        Assert.Equal(50m, resultado.TradeCerrado.RealizedPnL);
        Assert.Equal(-3m, PosicionActual.De(portfolio));
        Assert.Equal(33m, portfolio.LotesVivos.Single().Margin);
    }

    // spec: RN-07, RN-08 — apertura/aumento (mismo signo) no reduce ni genera RealizedPnL.
    [Fact]
    public void UnFillDelMismoSignoAbreUnLoteSinRealizedPnL()
    {
        var portfolio = new PortfolioState { Cash = 1000m };

        var resultado = AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 10m, 100m, 0m, 1, OrderType.Market));

        Assert.Equal(0m, resultado.RealizedPnLReconocido);
        Assert.Null(resultado.TradeCerrado);
        Assert.Equal(10m, PosicionActual.De(portfolio));
    }
}
