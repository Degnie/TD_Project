using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: RN-18, CU-23 — evaluacion comparativa aislada y deterministica contra cada Gestor de
// Capital pre-cargado (RN-17: reutiliza GestorFixedFractional/GestorFixedRisk/GestorVolatilitySizing
// ya existentes, sin crear gestores nuevos), CR = RealizedPnL_total / (MaxDrawdown_abs + 1).
public class CapitalManagerRecommenderTests
{
    private static ConfiguracionExperimento ConfigConVelas() =>
        new(CapitalInicial: 1000m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        });

    // spec: RN-18 — el gestor que maximiza CR sin quiebra de cuenta es el Gestor Recomendado
    [Fact]
    public void ElGestorConMayorCrEsElRecomendado()
    {
        var config = ConfigConVelas();
        var gestores = new IGestorRiesgo[]
        {
            new GestorFixedFractional(0.1m),
            new GestorFixedRisk(50m)
        };

        var recomendacion = CapitalManagerRecommender.Recomendar(config, new EstrategiaMarketSiempre(), gestores);

        Assert.NotNull(recomendacion.GestorRecomendado);
        Assert.Equal(gestores.Length, recomendacion.Resultados.Count);
    }

    // spec: RN-18 — invariante de exclusion: el sistema recomienda un gestor de capital, nunca una
    // estrategia; el resultado no debe exponer ninguna nocion de "mejor estrategia"
    [Fact]
    public void CadaResultadoIdentificaSuGestorPorConfiguracionDeclarada()
    {
        var config = ConfigConVelas();
        var gestor = new GestorFixedFractional(0.1m);

        var recomendacion = CapitalManagerRecommender.Recomendar(config, new EstrategiaMarketSiempre(), new IGestorRiesgo[] { gestor });

        Assert.Equal(((IIdentidadGestorRiesgo)gestor).ObtenerIdentidadConfiguracion(), recomendacion.Resultados[0].IdentidadGestor);
    }

    // spec: RN-18 — violacion: si todos los gestores sufren liquidacion (Equity <= 0), se emite
    // una recomendacion de inadaptabilidad (GestorRecomendado = null)
    [Fact]
    public void SiTodosLosGestoresLiquidanLaCuentaNoHayGestorRecomendado()
    {
        var configCapitalMinimo = new ConfiguracionExperimento(CapitalInicial: 0.0000001m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });
        var gestores = new IGestorRiesgo[] { new GestorFixedRisk(1000000m) };

        var recomendacion = CapitalManagerRecommender.Recomendar(configCapitalMinimo, new EstrategiaMarketSiempre(), gestores);

        Assert.Null(recomendacion.GestorRecomendado);
    }

    // spec: RN-18 — cada gestor se ejecuta de forma aislada (RNF-07): el resultado de un gestor no
    // contamina el de otro, mismo config/estrategia base para los dos
    [Fact]
    public void LaEvaluacionDeUnGestorNoAfectaElResultadoDeOtro()
    {
        var config = ConfigConVelas();
        var gestorA = new GestorFixedFractional(0.1m);
        var gestorB = new GestorFixedFractional(0.1m);

        var recomendacion = CapitalManagerRecommender.Recomendar(config, new EstrategiaMarketSiempre(), new IGestorRiesgo[] { gestorA, gestorB });

        Assert.Equal(recomendacion.Resultados[0].PnLTotal, recomendacion.Resultados[1].PnLTotal);
        Assert.Equal(recomendacion.Resultados[0].MaxDrawdown, recomendacion.Resultados[1].MaxDrawdown);
    }
}
