using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Portfolio;

// spec: Caso 4 D-092, ESPECIFICACION_CLASIFICADOR_INTENCION_ORDEN_V1.md P1-P8. Actualizado en
// D-095 (ESPECIFICACION_NORMALIZACION_CIERRES_SIZING_V1.md S2): Clasificar retorna
// ResultadoClasificacion (Intencion, CantidadEfectiva) en vez de solo IntencionOrden — mismo
// criterio verificado en cada caso, mas la verificacion de CantidadEfectiva donde aplica.
public class ClasificadorIntencionOrdenTests
{
    // P1 — Apertura: sin posicion previa, cualquier orden abre.
    [Fact]
    public void SinPosicionPreviaClasificaApertura()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        var request = new OrderRequest(Side.Buy, OrderType.Market, 10m);

        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        Assert.Equal(IntencionOrden.Apertura, resultado.Intencion);
        Assert.Equal(10m, resultado.CantidadEfectiva);
    }

    // P2 — Aumento: posicion viva, orden del mismo signo.
    [Fact]
    public void MismoSignoConPosicionPreviaClasificaAumento()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 10m, 100m, 0m, 1, OrderType.Market));
        var request = new OrderRequest(Side.Buy, OrderType.Market, 5m);

        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        Assert.Equal(IntencionOrden.Aumento, resultado.Intencion);
        Assert.Equal(5m, resultado.CantidadEfectiva);
    }

    // P3 — ReduccionParcial: signo contrario, magnitud menor a la posicion.
    [Fact]
    public void SignoContrarioConMagnitudMenorClasificaReduccionParcial()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 10m, 100m, 0m, 1, OrderType.Market));
        var request = new OrderRequest(Side.Sell, OrderType.Market, 4m);

        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        Assert.Equal(IntencionOrden.ReduccionParcial, resultado.Intencion);
        Assert.Equal(4m, resultado.CantidadEfectiva);
    }

    // P4 — CierreTotal: signo contrario, magnitud exactamente igual a la posicion.
    [Fact]
    public void SignoContrarioConMagnitudIgualClasificaCierreTotal()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 10m, 100m, 0m, 1, OrderType.Market));
        var request = new OrderRequest(Side.Sell, OrderType.Market, 10m);

        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        Assert.Equal(IntencionOrden.CierreTotal, resultado.Intencion);
        Assert.Equal(10m, resultado.CantidadEfectiva);
    }

    // P5 — CrossZero: signo contrario, magnitud mayor a la posicion. Clasificador NO normaliza
    // (D-095: la normalizacion es responsabilidad de GestorCapital, solo bajo sizing activo) —
    // CantidadEfectiva conserva la magnitud solicitada tal cual, igual que antes de D-095.
    [Fact]
    public void SignoContrarioConMagnitudMayorClasificaCrossZero()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 5m, 100m, 0m, 1, OrderType.Market));
        var request = new OrderRequest(Side.Sell, OrderType.Market, 8m);

        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        Assert.Equal(IntencionOrden.CrossZero, resultado.Intencion);
        Assert.Equal(8m, resultado.CantidadEfectiva);
    }

    // P3b — simetrico sobre posicion corta (mismo criterio, signo invertido).
    [Fact]
    public void SignoContrarioSobrePosicionCortaClasificaCierreTotal()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Sell, 4m, 70m, 0m, 1, OrderType.Market));
        var request = new OrderRequest(Side.Buy, OrderType.Market, 4m);

        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        Assert.Equal(IntencionOrden.CierreTotal, resultado.Intencion);
    }

    // P6 — Coincidencia con AplicadorFill: clasificar ANTES del Fill debe predecir la rama que
    // AplicadorFill.Aplicar efectivamente toma DESPUES del Fill, en los 4 escenarios ya cubiertos
    // por AplicadorFillIntegracionTests.cs (mismo dataset, sin modificar ese archivo).
    [Fact]
    public void PrediceReduccionFifoQueAplicadorFillEjecutaEnFlujoReal()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 2m, 50m, 0m, 1, OrderType.Market));
        AplicadorFill.Aplicar(portfolio, new Fill(2, Side.Buy, 8m, 60m, 0m, 2, OrderType.Market));

        var request = new OrderRequest(Side.Sell, OrderType.Market, 4m);
        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        var resultadoFill = AplicadorFill.Aplicar(portfolio, new Fill(3, Side.Sell, 4m, 70m, 0m, 3, OrderType.Market));

        Assert.Equal(IntencionOrden.ReduccionParcial, resultado.Intencion);
        Assert.Null(resultadoFill.TradeCerrado);
    }

    [Fact]
    public void PrediceCierreTotalQueAplicadorFillEjecutaSobrePosicionCorta()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Sell, 4m, 70m, 0m, 1, OrderType.Market));

        var request = new OrderRequest(Side.Buy, OrderType.Market, 4m);
        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        AplicadorFill.Aplicar(portfolio, new Fill(2, Side.Buy, 4m, 60m, 0m, 2, OrderType.Market));

        Assert.Equal(IntencionOrden.CierreTotal, resultado.Intencion);
        Assert.Equal(0m, PosicionActual.De(portfolio));
    }

    [Fact]
    public void PrediceCrossZeroQueAplicadorFillEjecutaCerrandoYAbriendo()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 5m, 100m, 0m, 1, OrderType.Market));

        var request = new OrderRequest(Side.Sell, OrderType.Market, 8m);
        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        var resultadoFill = AplicadorFill.Aplicar(portfolio, new Fill(2, Side.Sell, 8m, 110m, 0m, 2, OrderType.Market));

        Assert.Equal(IntencionOrden.CrossZero, resultado.Intencion);
        Assert.NotNull(resultadoFill.TradeCerrado);
        Assert.Equal(-3m, PosicionActual.De(portfolio));
    }

    [Fact]
    public void PrediceAperturaQueAplicadorFillEjecutaSinPosicionPrevia()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        var request = new OrderRequest(Side.Buy, OrderType.Market, 10m);

        var resultado = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        var resultadoFill = AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 10m, 100m, 0m, 1, OrderType.Market));

        Assert.Equal(IntencionOrden.Apertura, resultado.Intencion);
        Assert.Equal(0m, resultadoFill.RealizedPnLReconocido);
        Assert.Null(resultadoFill.TradeCerrado);
    }

    // P7 — Pureza: no muta PortfolioState, dos invocaciones sucesivas producen el mismo resultado.
    [Fact]
    public void ClasificarNoMutaElPortfolioYEsRepetible()
    {
        var portfolio = new PortfolioState { Cash = 1000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 10m, 100m, 0m, 1, OrderType.Market));
        var cashAntes = portfolio.Cash;
        var marginAntes = portfolio.Margin;
        var lotesAntes = portfolio.LotesVivos.Count;
        var request = new OrderRequest(Side.Sell, OrderType.Market, 4m);

        var primera = ClasificadorIntencionOrden.Clasificar(portfolio, request);
        var segunda = ClasificadorIntencionOrden.Clasificar(portfolio, request);

        Assert.Equal(primera, segunda);
        Assert.Equal(cashAntes, portfolio.Cash);
        Assert.Equal(marginAntes, portfolio.Margin);
        Assert.Equal(lotesAntes, portfolio.LotesVivos.Count);
    }
}
