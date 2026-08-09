using CsCheck;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Determinismo;

public class DeterminismoPropertyTests
{
    private static Gen<Candle> GenCandle =>
        Gen.Select(
            Gen.Int[1, 1_000_000],
            Gen.Decimal[1m, 1000m],
            (ts, open) => new Candle(ts, open, open + 5m, open - 5m, open + 1m, 100m));

    // spec: RNF-06, CU-05, EC-03 — mismo input produce bit a bit igual output; InputHashA == InputHashB => ResultHashA == ResultHashB.
    // Una desincronizacion aqui (EC-03) es una violacion critica del dominio (RN-04/RN-11).
    [Fact]
    public void MismoInputProduceSiempreElMismoResultHash()
    {
        GenCandle.Sample(vela =>
        {
            var resultadoA = MotorDeterminista.Ejecutar(vela);
            var resultadoB = MotorDeterminista.Ejecutar(vela);

            Assert.Equal(resultadoA, resultadoB);
        });
    }
}
