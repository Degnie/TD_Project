using TD_Project.Application;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Infrastructure;
using Xunit;

namespace TD_Project.Infrastructure.Tests;

public class SerializacionSimetricaTests
{
    // spec: RNF-13 — Deserializar(Serializar(Result)) == Result
    [Fact]
    public void DeserializarSerializarProduceUnResultadoEquivalente()
    {
        var resultado = new ResultadoBacktest(
            Estado: EstadoBacktest.Success,
            Fills: new[] { new Fill(SecuenciaCausal: 1, Side: Side.Buy, Cantidad: 1m, PrecioFill: 100m, CostoFriccionReal: 0.1m, Timestamp: 2, TipoOrdenOriginal: OrderType.Market) },
            CashFinal: 900m,
            Trades: Array.Empty<Trade>(),
            OrdenesFinales: Array.Empty<Order>(),
            EquityCurve: Array.Empty<EquityPoint>(),
            PortfolioSnapshots: Array.Empty<PortfolioSnapshot>(),
            BranchResolutions: Array.Empty<BranchResolutionInfo>());

        var serializado = SerializadorResultado.Serializar(resultado);
        var deserializado = SerializadorResultado.Deserializar(serializado);

        Assert.Equal(resultado.Estado, deserializado.Estado);
        Assert.Equal(resultado.Fills, deserializado.Fills);
        Assert.Equal(resultado.CashFinal, deserializado.CashFinal);
        Assert.Equal(resultado.Trades, deserializado.Trades);
        Assert.Equal(resultado.OrdenesFinales.Count, deserializado.OrdenesFinales.Count);
    }
}
