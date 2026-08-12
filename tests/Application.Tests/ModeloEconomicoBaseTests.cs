using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: Caso 2 D-057/D-058/D-059/D-060/D-061 — pruebas obligatorias de cierre de implementacion
// (ESPECIFICACION_MODELO_ECONOMICO_BASE_V1.md): P1 compatibilidad historica, P2 cambio explicito
// de margen, P3 incapacidad no bloqueante, P4 determinismo.
public class ModeloEconomicoBaseTests
{
    private static ConfiguracionExperimento ConfigConVelas(decimal capitalInicial, int warmup = 0, Instrumento? instrumento = null) =>
        new(CapitalInicial: capitalInicial, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        }, Warmup: warmup, Instrumento: instrumento);

    // P1 — compatibilidad historica: sin Instrumento explicito, el resultado economico debe ser
    // identico al que producia AplicadorFill con su default anterior (TasaMargen=0.1m).
    [Fact]
    public void SinInstrumentoExplicitoElResultadoEconomicoEsIdenticoAlHistorico()
    {
        var configSinInstrumento = ConfigConVelas(1000m);
        var configConDefaultExplicito = ConfigConVelas(1000m, instrumento: Instrumento.Default);

        var r1 = BacktestRunner.Ejecutar(configSinInstrumento, new EstrategiaMarketSiempre());
        var r2 = BacktestRunner.Ejecutar(configConDefaultExplicito, new EstrategiaMarketSiempre());

        Assert.Equal(r1.CashFinal, r2.CashFinal);
        Assert.Equal(r1.EquityCurve[^1].Margin, r2.EquityCurve[^1].Margin);
        Assert.Equal(0.1m, Instrumento.Default.TasaMargen);
    }

    // P2 — cambiar TasaMargen del instrumento debe cambiar Margin (y por lo tanto Cash/Equity).
    [Fact]
    public void CambiarTasaMargenDelInstrumentoCambiaElMargenCalculado()
    {
        var configMargenBajo = ConfigConVelas(1000m, instrumento: new Instrumento("TEST", 0.1m));
        var configMargenAlto = ConfigConVelas(1000m, instrumento: new Instrumento("TEST", 0.5m));

        var rBajo = BacktestRunner.Ejecutar(configMargenBajo, new EstrategiaMarketSiempre());
        var rAlto = BacktestRunner.Ejecutar(configMargenAlto, new EstrategiaMarketSiempre());

        Assert.NotEqual(rBajo.EquityCurve[0].Margin, rAlto.EquityCurve[0].Margin);
        Assert.NotEqual(rBajo.CashFinal, rAlto.CashFinal);
    }

    // P3 — capital insuficiente: la orden se ejecuta igual (Fill generado, Caso 1 intacto) y ademas
    // queda registrada la incapacidad — nunca se elimina ni se bloquea la operacion (D-059).
    [Fact]
    public void CapitalInsuficienteRegistraIncapacidadSinBloquearLaOperacion()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        Assert.NotEmpty(resultado.Fills);
        Assert.NotEmpty(resultado.IncapacidadesEfectivas);
        Assert.True(resultado.IncapacidadesEfectivas[0].ReservaRequerida > resultado.IncapacidadesEfectivas[0].CashDisponible);
    }

    // P4 — determinismo: misma entrada produce las mismas incapacidades y el mismo resultado.
    [Fact]
    public void MismaEntradaProduceLasMismasIncapacidadesYElMismoResultado()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });

        var r1 = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());
        var r2 = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        Assert.Equal(r1.IncapacidadesEfectivas.Count, r2.IncapacidadesEfectivas.Count);
        Assert.Equal(r1.CashFinal, r2.CashFinal);
        Assert.Equal(r1.Fills.Count, r2.Fills.Count);
    }

    // Sin capital insuficiente, Incapacidades debe quedar vacio (no aparece de la nada).
    [Fact]
    public void ConCapitalSuficienteNoHayIncapacidadesRegistradas()
    {
        var config = ConfigConVelas(1000m);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());

        Assert.Empty(resultado.IncapacidadesEfectivas);
    }

    // P3 (D-062) — consistencia interna: el ultimo EquityPoint de la curva debe coincidir con
    // Cash + Margin + UnrealizedPnL calculado a partir del estado final (CashFinal), con el mismo
    // instrumento — EquityCurve y CashFinal no pueden divergir (mismo instrumento en toda la cadena).
    [Fact]
    public void ElUltimoPuntoDeEquityCurveEsConsistenteConElEstadoFinal()
    {
        var config = ConfigConVelas(1000m, instrumento: new Instrumento("TEST", 0.3m));

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaMarketSiempre());
        var ultimoPunto = resultado.EquityCurve[^1];

        Assert.Equal(resultado.CashFinal, ultimoPunto.Cash);
        Assert.Equal(ultimoPunto.Cash + ultimoPunto.Margin + ultimoPunto.UnrealizedPnL, ultimoPunto.Equity);
    }
}
