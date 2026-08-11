using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: RNF-06 (determinismo), CU-05 — regresion de rendimiento descubierta al introducir datos
// reales de escala completa (Fase 2A/2B del laboratorio, 527.040 velas). BacktestRunner
// reconstruia una sublista completa por vela (O(n) por iteracion, O(n^2) total) para el
// DataSlice, y ademas filtraba "ordenes Pending" recorriendo el historial completo por vela y
// por fill — ambos invisibles con datasets sinteticos (~200 velas) pero bloqueantes a escala
// real. El Timeout hace que una regresion futura falle en segundos en vez de colgar la suite.
public class RendimientoEscalaTests
{
    // spec: CU-05 — mismo input -> mismo resultado, verificado a escala real (527.040 velas,
    // el tamano exacto del dataset BTCUSDT congelado en Fase 2A). Usa un patron abre/cierra
    // alternado (misma forma que Tres Mosqueteros/MHI): una estrategia que jamas cierra
    // posiciones (ej. EstrategiaMarketSiempre) degenera en O(n) lotes vivos simultaneos, un caso
    // que ninguna estrategia real del laboratorio produce y queda fuera de este test.
    [Fact(Timeout = 30_000)]
    public void UnDatasetDeEscalaRealCompletaEnTiempoLinealYEsDeterminista()
    {
        var velas = GenerarVelasDeterministas(527_040);
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velas);

        var r1 = BacktestRunner.Ejecutar(config, new EstrategiaAbreYCierraAlternado());
        var r2 = BacktestRunner.Ejecutar(config, new EstrategiaAbreYCierraAlternado());

        Assert.Equal(EstadoBacktest.Success, r1.Estado);
        Assert.Equal(r1.CashFinal, r2.CashFinal);
        Assert.Equal(r1.Trades.Count, r2.Trades.Count);
        Assert.Equal(r1.Fills.Count, r2.Fills.Count);
        Assert.True(r1.Trades.Count > 0, "la estrategia debe haber cerrado operaciones (no degenerar en 0 Trades)");
    }

    private static IReadOnlyList<Candle> GenerarVelasDeterministas(int cantidad)
    {
        var velas = new List<Candle>(cantidad);
        var precio = 100m;
        var rnd = new Random(42);
        for (var i = 0; i < cantidad; i++)
        {
            var close = precio + (decimal)(rnd.NextDouble() - 0.5);
            var high = Math.Max(precio, close) + 0.5m;
            var low = Math.Min(precio, close) - 0.5m;
            velas.Add(new Candle(i, precio, high, low, close, 100m));
            precio = close;
        }
        return velas;
    }
}
