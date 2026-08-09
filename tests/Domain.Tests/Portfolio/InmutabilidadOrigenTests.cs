using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Portfolio;

public class InmutabilidadOrigenTests
{
    // spec: RN-07 — Position muta exclusivamente a causa de un Fill
    [Fact]
    public void LaPosicionSoloMutaAlAplicarUnFill()
    {
        var portfolio = new PortfolioState();
        var estadoInicial = PosicionActual.De(portfolio);
        var fill = new Fill(SecuenciaCausal: 1, Side: Side.Buy, Cantidad: 5m, PrecioFill: 100m, CostoFriccionReal: 0.5m, Timestamp: 2, TipoOrdenOriginal: OrderType.Market);

        AplicadorFill.Aplicar(portfolio, fill);

        var estadoFinal = PosicionActual.De(portfolio);
        Assert.NotEqual(estadoInicial, estadoFinal);
    }
}
