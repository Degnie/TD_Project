using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy.Dsl;
using Xunit;

namespace TD_Project.Domain.Tests.Strategy.Dsl;

public class InterpreteDslTests
{
    // spec: RN-16 — "Si Close(N) > SMA(20) -> Emitir OrderRequest Market Buy" (ejemplo valido
    // textual del SPEC). Con solo 2 velas (periodo SMA=1 para poder evaluar sin warmup adicional al
    // que ya impone CU-03), Close(N) > SMA(1) = Close(N) > Close(N) es siempre falso: se usa un caso
    // trivial (periodo=1) donde SMA(1) = Close(N-1) para poder forzar la condicion con 2 velas.
    private const string JsonSmaBuy = """
    {
      "condicion": { "indicador": "SMA", "periodo": 1, "operador": ">", "campo": "Close" },
      "accion": { "side": "Buy", "type": "Market" }
    }
    """;

    // spec: RN-16 — DSL valido se interpreta como IStrategy ejecutable: Close(N) > SMA(1,N) donde
    // SMA(1,N) = Close(N-1). Con velas [100, 200], en N=1 Close=200 > SMA(1)=Close(0)=100 -> Buy.
    [Fact]
    public void UnDslValidoEmiteOrderRequestCuandoLaCondicionSeCumple()
    {
        var estrategia = InterpreteDsl.CargarDesdeJson(JsonSmaBuy);
        var velas = new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 0m),
            new Candle(2, 200m, 200m, 200m, 200m, 0m)
        };
        var dataSlice = new DataSlice(velas);

        var requests = estrategia.Observar(dataSlice);

        Assert.Single(requests);
        Assert.Equal(Side.Buy, requests[0].Side);
        Assert.Equal(OrderType.Market, requests[0].Type);
    }

    // spec: RN-16 — sin cumplirse la condicion, no se emite ningun OrderRequest
    [Fact]
    public void UnDslValidoNoEmiteOrderRequestCuandoLaCondicionNoSeCumple()
    {
        var estrategia = InterpreteDsl.CargarDesdeJson(JsonSmaBuy);
        var velas = new[]
        {
            new Candle(1, 200m, 200m, 200m, 200m, 0m),
            new Candle(2, 100m, 100m, 100m, 100m, 0m)
        };
        var dataSlice = new DataSlice(velas);

        var requests = estrategia.Observar(dataSlice);

        Assert.Empty(requests);
    }

    // spec: RN-16 — evaluacion aislada y determinista: solo accede a DataSlice(N), nunca a
    // datos futuros; un DSL invalido (rechazado por ValidadorDsl) no debe poder cargarse
    [Fact]
    public void CargarUnDslInvalidoLanzaExcepcion()
    {
        const string jsonInvalido = "{ esto no es json valido";

        Assert.Throws<InvalidOperationException>(() => InterpreteDsl.CargarDesdeJson(jsonInvalido));
    }
}
