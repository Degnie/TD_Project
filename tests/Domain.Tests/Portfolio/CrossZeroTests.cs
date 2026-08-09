using TD_Project.Domain.Portfolio;
using Xunit;

namespace TD_Project.Domain.Tests.Portfolio;

public class CrossZeroTests
{
    // spec: RN-10, CU-18 — un Fill que invierte la posicion cierra el Trade activo
    // (liquida PnL, libera Margin viejo) y abre uno nuevo por el excedente (retiene Margin nuevo)
    [Fact]
    public void UnFillQueInvierteLaPosicionLiberaTodoElMargenViejoYRetieneElNuevo()
    {
        var posicionVieja = new Lote(Cantidad: 5m, PrecioEntrada: 100m, Margin: 50m);
        var cantidadFillInversion = 8m;
        var precioFillInversion = 110m;
        var tasaMargen = 0.1m;

        var resultado = ResolutorCrossZero.Resolver(posicionVieja, cantidadFillInversion, precioFillInversion, tasaMargen);

        Assert.Equal(50m, resultado.MarginLiberadoPosicionVieja);
        Assert.Equal(3m, resultado.CantidadPosicionNueva);
        Assert.Equal(110m * 3m * 0.1m, resultado.MarginRetenidoPosicionNueva);
    }
}
