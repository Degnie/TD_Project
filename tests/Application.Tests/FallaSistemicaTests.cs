using TD_Project.Application;
using TD_Project.Application.Tests.Fakes;
using Xunit;

namespace TD_Project.Application.Tests;

public class FallaSistemicaTests
{
    // spec: EC-04, RNF-09, RNF-10 — el contrato observable es InternalCrash + 0 resultados
    // financieros emitidos. El mecanismo que provoca el fallo (aqui, una excepcion no
    // controlada en Strategy) no es parte del requisito verificado, solo un vehiculo de prueba.
    [Fact]
    public void UnAbortoNoManejadoProduceInternalCrashSinResultadosFinancieros()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: new[]
        {
            new Domain.Shared.Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Domain.Shared.Candle(2, 102m, 106m, 100m, 104m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaQueLanza());

        Assert.Equal(EstadoBacktest.InternalCrash, resultado.Estado);
        Assert.Empty(resultado.Fills);
        Assert.Empty(resultado.Trades);
    }
}
