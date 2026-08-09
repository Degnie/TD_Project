using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Precision;

public class RedondeoDecimalTests
{
    // spec: RNF-05 — redondeo Half-to-Even a 2 decimales, exclusivo al final
    [Theory]
    [InlineData(1.005, 1.00)]   // par mas cercano hacia abajo
    [InlineData(1.015, 1.02)]   // par mas cercano hacia arriba
    [InlineData(2.675, 2.68)]
    public void ElRedondeoFinalUsaHalfToEven(decimal valorInterno, decimal esperado)
    {
        var redondeado = RedondeoReporte.ADosDecimales(valorInterno);

        Assert.Equal(esperado, redondeado);
    }

    // spec: RNF-05 — Equity_rep = Cash_rep + Margin_rep + UnrealizedPnL_rep (suma de componentes ya redondeados)
    [Fact]
    public void ElEquityReportadoEsLaSumaEstrictaDeSusComponentesYaRedondeados()
    {
        var cashInterno = 100.005m;
        var marginInterno = 50.005m;
        var unrealizedInterno = 0.005m;

        var equityReportado = RedondeoReporte.EquityReportado(cashInterno, marginInterno, unrealizedInterno);

        var cashRep = RedondeoReporte.ADosDecimales(cashInterno);
        var marginRep = RedondeoReporte.ADosDecimales(marginInterno);
        var unrealizedRep = RedondeoReporte.ADosDecimales(unrealizedInterno);
        Assert.Equal(cashRep + marginRep + unrealizedRep, equityReportado);
    }
}
