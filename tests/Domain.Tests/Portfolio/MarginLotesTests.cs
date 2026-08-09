using TD_Project.Domain.Portfolio;
using Xunit;

namespace TD_Project.Domain.Tests.Portfolio;

public class MarginLotesTests
{
    // spec: RN-08 — Margin por lote = Q_k * PrecioFill_k * TasaMargen, fijo hasta que se consume
    [Fact]
    public void ElMarginDeUnLoteSeCalculaEnElInstanteDeSuFillYQuedaFijo()
    {
        var lote = CalculadoraLotes.AbrirLote(cantidad: 10m, precioFill: 50m, tasaMargen: 0.1m);

        Assert.Equal(50m, lote.Margin);
    }

    // spec: RN-09, CU-17 — Long 2@$50 + Long 8@$60. Sell 4 -> consume Lote(2x$50) y Lote(2x$60), FIFO
    [Fact]
    public void UnaReduccionParcialConsumeLosLotesMasAntiguosPrimero()
    {
        var lotes = new List<Lote>
        {
            new(Cantidad: 2m, PrecioEntrada: 50m, Margin: 10m),
            new(Cantidad: 8m, PrecioEntrada: 60m, Margin: 48m)
        };

        var consumo = ConsumidorFifo.Consumir(lotes, cantidadAReducir: 4m);

        Assert.Equal(2m, consumo.Consumidos[0].CantidadConsumida);
        Assert.Equal(50m, consumo.Consumidos[0].Lote.PrecioEntrada);
        Assert.Equal(2m, consumo.Consumidos[1].CantidadConsumida);
        Assert.Equal(60m, consumo.Consumidos[1].Lote.PrecioEntrada);
    }

    // spec: RN-08 — cierre/reduccion libera exactamente el Margin original de los lotes consumidos
    [Fact]
    public void LaReduccionLiberaExactamenteElMarginOriginalDeLosLotesConsumidos()
    {
        var lotes = new List<Lote> { new(Cantidad: 2m, PrecioEntrada: 50m, Margin: 10m) };

        var consumo = ConsumidorFifo.Consumir(lotes, cantidadAReducir: 2m);

        Assert.Equal(10m, consumo.MarginLiberado);
    }
}
