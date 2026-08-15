using TD_Project.Domain.Ingestion;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Domain.Tests.Ingestion;

public class ValidadorDatasetTests
{
    // spec: RN-15 — dataset ordenado, timestamps estrictamente crecientes, precios validos
    [Fact]
    public void UnDatasetOrdenadoConPreciosValidosEsValido()
    {
        var velas = new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        };

        var resultado = ValidadorDataset.Validar(velas);

        Assert.True(resultado.EsValido);
    }

    // spec: RN-15 — timestamps duplicados o desordenados -> rechazo atomico
    [Fact]
    public void TimestampsDesordenadosSonInvalidos()
    {
        var velas = new[]
        {
            new Candle(2, 100m, 105m, 95m, 102m, 500m),
            new Candle(1, 102m, 106m, 100m, 104m, 500m)
        };

        var resultado = ValidadorDataset.Validar(velas);

        Assert.False(resultado.EsValido);
    }

    // spec: RN-15 — timestamps duplicados -> rechazo atomico
    [Fact]
    public void TimestampsDuplicadosSonInvalidos()
    {
        var velas = new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(1, 102m, 106m, 100m, 104m, 500m)
        };

        var resultado = ValidadorDataset.Validar(velas);

        Assert.False(resultado.EsValido);
    }

    // spec: RN-15 — High < Low es invalido
    [Fact]
    public void HighMenorQueLowEsInvalido()
    {
        var velas = new[] { new Candle(1, 100m, 95m, 100m, 97m, 500m) };

        var resultado = ValidadorDataset.Validar(velas);

        Assert.False(resultado.EsValido);
    }

    // spec: RN-15 — precios <= 0 son invalidos
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PreciosNoPositivosSonInvalidos(decimal openInvalido)
    {
        var velas = new[] { new Candle(1, openInvalido, 105m, 95m, 102m, 500m) };

        var resultado = ValidadorDataset.Validar(velas);

        Assert.False(resultado.EsValido);
    }

    // spec: RN-15 — High/Low deben contener Open y Close (High >= Open,Close >= Low)
    [Fact]
    public void OpenOCloseFueraDelRangoHighLowEsInvalido()
    {
        var velas = new[] { new Candle(1, 110m, 105m, 95m, 102m, 500m) };

        var resultado = ValidadorDataset.Validar(velas);

        Assert.False(resultado.EsValido);
    }

    // spec: RN-15 — violacion produce evento DataInvalid y 0 velas procesadas: el resultado expone
    // el motivo para que Infrastructure no persista nada (CU-21 depende de este contrato)
    [Fact]
    public void UnDatasetInvalidoExponeUnMotivoDeRechazo()
    {
        var velas = new[] { new Candle(1, 100m, 95m, 100m, 97m, 500m) };

        var resultado = ValidadorDataset.Validar(velas);

        Assert.False(resultado.EsValido);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Motivo));
    }
}
