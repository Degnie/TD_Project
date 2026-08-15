using TD_Project.Domain.Strategy.Dsl;
using Xunit;

namespace TD_Project.Domain.Tests.Strategy.Dsl;

public class ValidadorDslTests
{
    private const string JsonValido = """
    {
      "condicion": { "indicador": "SMA", "periodo": 20, "operador": ">", "campo": "Close" },
      "accion": { "side": "Buy", "type": "Market" }
    }
    """;

    // spec: RN-16 — JSON DSL con regla "Si Close(N) > SMA(20) -> Emitir OrderRequest Market Buy"
    // (ejemplo valido citado textualmente en el SPEC)
    [Fact]
    public void UnDslConCondicionYAccionValidasEsValido()
    {
        var resultado = ValidadorDsl.Validar(JsonValido);

        Assert.True(resultado.EsValido);
    }

    // spec: RN-16 — el DSL prohibe explicitamente referencias look-ahead (N+k)
    [Fact]
    public void UnaReferenciaLookAheadEsInvalida()
    {
        const string jsonLookAhead = """
        {
          "condicion": { "indicador": "Close", "offset": 1, "operador": ">", "campo": "Close" },
          "accion": { "side": "Buy", "type": "Market" }
        }
        """;

        var resultado = ValidadorDsl.Validar(jsonLookAhead);

        Assert.False(resultado.EsValido);
    }

    // spec: RN-16 — JSON malformado o fuera del esquema DSL es invalido (ConfigInvalid)
    [Fact]
    public void UnJsonMalformadoEsInvalido()
    {
        const string jsonMalformado = "{ esto no es json valido";

        var resultado = ValidadorDsl.Validar(jsonMalformado);

        Assert.False(resultado.EsValido);
    }

    // spec: RN-16 — el DSL no admite comandos de ejecucion de codigo externo
    [Fact]
    public void UnComandoDeEjecucionExternaEsInvalido()
    {
        const string jsonConComando = """
        {
          "condicion": { "comando": "System.exec", "argumento": "rm -rf" },
          "accion": { "side": "Buy", "type": "Market" }
        }
        """;

        var resultado = ValidadorDsl.Validar(jsonConComando);

        Assert.False(resultado.EsValido);
    }
}
