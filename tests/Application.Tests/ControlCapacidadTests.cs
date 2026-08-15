using TD_Project.Application.Tests.Fakes;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: RN-12, CU-15 — activacion condicional del bloqueo de capacidad ya descrito por la regla
// ("la orden se ajusta a la capacidad maxima permitida o se rechaza atomicamente"), restringida al
// flujo de cliente final mediante ConfiguracionExperimento.BloquearPorCapacidad (caso14,
// ESPECIFICACION_IMPLEMENTACION_VALIDACION_RESULTADOS_BACKTEST_V1.md).
public class ControlCapacidadTests
{
    private static ConfiguracionExperimento ConfigConVelas(bool bloquearPorCapacidad) =>
        new(CapitalInicial: 100m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 108m, 102m, 106m, 500m)
        }, BloquearPorCapacidad: bloquearPorCapacidad);

    // spec: RN-12, CU-15 — BloquearPorCapacidad=true: la orden sin capacidad suficiente no genera Fill.
    [Fact]
    public void ConBloqueoActivoUnaOrdenSinCapacidadNoGeneraFill()
    {
        var config = ConfigConVelas(bloquearPorCapacidad: true);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCapacidadInsuficiente());

        Assert.DoesNotContain(resultado.Fills, f => f.Cantidad == 1000m);
    }

    // spec: RN-12, CU-15 — la orden bloqueada no se registra como orden ejecutada (no aparece en
    // OrdenesFinales; unicamente la orden posterior de capacidad valida sí se registra).
    [Fact]
    public void ConBloqueoActivoLaOrdenSinCapacidadNoSeRegistraComoOrdenFinal()
    {
        var config = ConfigConVelas(bloquearPorCapacidad: true);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCapacidadInsuficiente());

        Assert.DoesNotContain(resultado.OrdenesFinales, o => o.Cantidad == 1000m);
    }

    // spec: RN-12, CU-15 — la incapacidad queda registrada con Bloqueada=true cuando el bloqueo esta activo.
    [Fact]
    public void ConBloqueoActivoLaIncapacidadSeRegistraConBloqueadaVerdadero()
    {
        var config = ConfigConVelas(bloquearPorCapacidad: true);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCapacidadInsuficiente());

        Assert.Single(resultado.IncapacidadesEfectivas);
        Assert.True(resultado.IncapacidadesEfectivas[0].Bloqueada);
    }

    // spec: RN-12, CU-15, RNF-09 — un bloqueo de capacidad es una condicion del experimento, no
    // una falla del sistema: EstadoBacktest permanece Success, sin agregar un estado nuevo.
    [Fact]
    public void ConBloqueoActivoElEstadoDelBacktestSigueSiendoSuccess()
    {
        var config = ConfigConVelas(bloquearPorCapacidad: true);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCapacidadInsuficiente());

        Assert.Equal(EstadoBacktest.Success, resultado.Estado);
    }

    // spec: RN-12 — con el bloqueo desactivado, la incapacidad se registra pero no impide la
    // ejecucion de la orden (comportamiento historico D-059/D-060, sin cambios). Usa una estrategia
    // de una unica orden (EstrategiaUnaOrdenCapacidadInsuficiente): con dos ordenes, la primera
    // (sin capacidad, ejecutada igual sin bloqueo) deja Cash negativo y arrastra una segunda
    // incapacidad sobre cualquier orden posterior — aislar una unica orden es lo unico que permite
    // verificar limpiamente "una incapacidad, no bloqueante, ejecucion historica".
    [Fact]
    public void SinBloqueoLaOrdenSinCapacidadSeEjecutaIgualQueHistoricamente()
    {
        var config = ConfigConVelas(bloquearPorCapacidad: false);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaUnaOrdenCapacidadInsuficiente());

        Assert.Contains(resultado.Fills, f => f.Cantidad == 1000m);
        Assert.Single(resultado.IncapacidadesEfectivas);
        Assert.False(resultado.IncapacidadesEfectivas[0].Bloqueada);
    }

    // spec: RN-12 — verificacion explicita de regresion pedida por el auditor: si
    // BloquearPorCapacidad no se especifica, el default debe seguir siendo false, protegiendo los
    // experimentos historicos y los mas de 25 puntos de invocacion del laboratorio que no lo mencionan.
    [Fact]
    public void ElValorPorDefectoDeBloquearPorCapacidadEsFalse()
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 100m, Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m)
        });

        Assert.False(config.BloquearPorCapacidad);
    }

    // spec: RN-04 — una orden bloqueada por capacidad nunca llega a RegistradorOrdenes.Registrar,
    // por lo que no consume Secuencia Causal: la orden posterior de capacidad valida recibe la
    // primera Secuencia Causal del experimento, sin huecos artificiales.
    [Fact]
    public void UnaOrdenBloqueadaNoConsumeSecuenciaCausalYNoDejaHuecos()
    {
        var config = ConfigConVelas(bloquearPorCapacidad: true);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCapacidadInsuficiente());

        Assert.Single(resultado.OrdenesFinales);
        Assert.Equal(1m, resultado.OrdenesFinales[0].Cantidad);
        Assert.Equal(1, resultado.OrdenesFinales[0].SecuenciaCausal);
    }

    // spec: CU-15 — flujo de cliente final de punta a punta: configuracion con
    // BloquearPorCapacidad=true -> estrategia que intenta una orden sin capacidad suficiente ->
    // backtest con control activo -> resultado que un cliente veria (la orden bloqueada no aparece
    // ejecutada, la incapacidad queda registrada como Bloqueada, el experimento sigue siendo Success).
    [Fact]
    public void ElFlujoDeClienteFinalConBloqueoActivoProduceUnResultadoCoherenteParaElUsuario()
    {
        var config = ConfigConVelas(bloquearPorCapacidad: true);

        var resultado = BacktestRunner.Ejecutar(config, new EstrategiaCapacidadInsuficiente());

        Assert.Equal(EstadoBacktest.Success, resultado.Estado);
        Assert.DoesNotContain(resultado.Fills, f => f.Cantidad == 1000m);
        Assert.Contains(resultado.Fills, f => f.Cantidad == 1m);
        Assert.Single(resultado.IncapacidadesEfectivas);
        Assert.True(resultado.IncapacidadesEfectivas[0].Bloqueada);
    }
}
