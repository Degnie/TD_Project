using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;
using Xunit;

namespace TD_Project.Exploration.Tests;

public class TestsAlineacionCuadrante
{
    // Vela verde: Open < Close. Vela roja: Open > Close. Rango amplio para que Market siempre
    // haga fill exacto al Open de la vela siguiente (RN-13), sin ambigüedad de trayectoria.
    private static Candle Verde(long ts) => new(ts, 100m, 110m, 90m, 105m, 500m);
    private static Candle Roja(long ts) => new(ts, 100m, 110m, 90m, 95m, 500m);

    // Test 1 — estrategia detenida por operación: la señal del cuadrante 1 (velas 5-9, señal en
    // N=7) debe evaluarse en el momento correcto (N%5==2 relativo al cuadrante, es decir N=7)
    // incluso si el cuadrante 0 generó una apuesta con martingalas que consumió varios ciclos.
    // Con contador interno (bug ya corregido) el cuadrante 1 se habria desalineado.
    [Fact]
    public void TresMosqueterosMantieneAlineacionDeCuadranteAunConMartingalasEnCurso()
    {
        // Cuadrante 0 (N=0..4): vela 3 (N=2) verde -> señal Buy, pierde 2 martingalas (ambas
        // ejecutan contra vela roja) -> se resuelve en N=4.
        // Cuadrante 1 (N=5..9): vela 3 (N=7) debe seguir siendo la señal, sin desfase.
        var velas = new[]
        {
            Verde(0), Verde(1), Verde(2) /* señal cuadrante 0 */, Roja(3) /* pierde */, Roja(4) /* pierde, tope */,
            Verde(5), Verde(6), Roja(7) /* señal cuadrante 1 (roja) */, Verde(8) /* entrada, pierde */, Roja(9),
        };
        var config = new ConfiguracionExperimento(CapitalInicial: 10000m, Velas: velas);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(maxMartingalas: 2));

        Assert.Equal(EstadoBacktest.Success, resultado.Estado);
        // La señal del cuadrante 0 (N=2, verde) debe abrir Buy ejecutando en N=3 (timestamp 3).
        var primeraApertura = resultado.Fills.First(f => f.Timestamp == 3);
        Assert.Equal(Side.Buy, primeraApertura.Side);
        // La señal del cuadrante 1 (N=7, roja) debe abrir Sell ejecutando en N=8 (timestamp 8),
        // NO desplazada por los ciclos extra que consumieron las martingalas del cuadrante 0.
        var segundaApertura = resultado.Fills.First(f => f.Timestamp == 8 && f.Side == Side.Sell);
        Assert.Equal(Side.Sell, segundaApertura.Side);
    }

    // Test 2 — MHI no genera señales intermedias: con 15 velas (3 cuadrantes completos) debe
    // haber como máximo 3 aperturas (una por cuadrante), nunca una por cada vela disponible
    // como ocurría con la ventana deslizante (bug ya corregido).
    [Fact]
    public void MhiMayoriaGeneraComoMaximoUnaAperturaPorCuadrante()
    {
        var velas = new List<Candle>();
        for (long ts = 0; ts < 15; ts++)
            velas.Add(ts % 2 == 0 ? Verde(ts) : Roja(ts));
        var config = new ConfiguracionExperimento(CapitalInicial: 10000m, Velas: velas);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMhiMayoria(maxMartingalas: 0));

        Assert.Equal(EstadoBacktest.Success, resultado.Estado);
        // Con maxMartingalas=0, cada apuesta resuelta en un solo Fill de apertura + un solo Fill
        // de cierre. 3 cuadrantes completos (N=4,9,14 son las señales) -> a lo sumo 3 aperturas.
        var timestampsDeSenal = new[] { 5m, 10m, 15m }; // apertura ejecuta en N+1 = 5, 10, 15
        var aperturasEnTimestampsEsperados = resultado.Fills.Count(f => timestampsDeSenal.Contains((decimal)f.Timestamp));
        Assert.True(aperturasEnTimestampsEsperados <= 3,
            $"Se esperaban a lo sumo 3 aperturas (una por cuadrante), se encontraron señales en timestamps: {string.Join(",", resultado.Fills.Select(f => f.Timestamp))}");
        Assert.True(resultado.Fills.Count <= 6, // 3 aperturas + 3 cierres, sin martingala
            $"Total de Fills excede lo esperado para 3 cuadrantes sin martingala: {resultado.Fills.Count}");
    }

    // Test 3 — ejecución N/N+1: la señal calculada al observar la vela 3 (Tres Mosqueteros) o
    // la vela 5 (MHI) debe ejecutar contra la vela SIGUIENTE, nunca contra la misma vela donde
    // se calculó la señal (RN-13, no look-ahead).
    [Fact]
    public void TresMosqueterosEjecutaLaEntradaEnLaVelaSiguienteALaSenal()
    {
        var velas = new[] { Verde(0), Verde(1), Verde(2) /* señal en N=2 */, Roja(3) /* entrada N=3 */, Roja(4) };
        var config = new ConfiguracionExperimento(CapitalInicial: 10000m, Velas: velas);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(maxMartingalas: 0));

        Assert.Equal(EstadoBacktest.Success, resultado.Estado);
        Assert.DoesNotContain(resultado.Fills, f => f.Timestamp == 2); // nunca ejecuta en la propia vela de señal
        Assert.Contains(resultado.Fills, f => f.Timestamp == 3 && f.Side == Side.Buy);
    }

    [Fact]
    public void MhiMayoriaEjecutaLaEntradaEnLaVelaSiguienteAlCierreDelCuadrante()
    {
        var velas = new[]
        {
            Verde(0), Verde(1), Verde(2) /* v3 */, Verde(3) /* v4 */, Verde(4) /* v5, cierra cuadrante, señal */,
            Roja(5) /* entrada */,
        };
        var config = new ConfiguracionExperimento(CapitalInicial: 10000m, Velas: velas);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMhiMayoria(maxMartingalas: 0));

        Assert.Equal(EstadoBacktest.Success, resultado.Estado);
        Assert.DoesNotContain(resultado.Fills, f => f.Timestamp == 4); // nunca ejecuta en la vela que cierra el cuadrante
        Assert.Contains(resultado.Fills, f => f.Timestamp == 5 && f.Side == Side.Buy); // mayoria verde -> Buy
    }
}
