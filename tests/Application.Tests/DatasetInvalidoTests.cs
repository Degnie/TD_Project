using TD_Project.Application;
using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

public class DatasetInvalidoTests
{
    // spec: CU-02 — dataset corrupto (nulo/malformado) -> DataInvalid -> 0 velas procesadas.
    // Distinto de CU-03 (longitud <= warmup), que es un dataset valido pero insuficiente.
    [Fact]
    public void UnDatasetCorruptoProduceDataInvalidSinProcesarVelas()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: null!);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCiega());

        Assert.Equal(EstadoBacktest.DataInvalid, resultado.Estado);
        Assert.Empty(resultado.Fills);
    }
}
