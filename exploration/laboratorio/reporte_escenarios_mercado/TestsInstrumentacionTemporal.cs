using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Exploration;

namespace TD_Project.ReporteEscenariosMercado;

// Fase 1.5-A, Paso 1 (D-040): pruebas requeridas por la auditoria antes de cerrar la
// instrumentacion de TimestampEntrada/TimestampResolucion en InfoOperacionResuelta.
// Usa velas sinteticas deterministas (no el dataset real) porque el objetivo es aislar el
// comportamiento de la instrumentacion misma, no evaluar la estrategia.
public static class TestsInstrumentacionTemporal
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos()
    {
        var detalles = new List<string>();
        var pasaron = 0;
        var total = 0;

        void Caso(string nombre, Action verificacion)
        {
            total++;
            try
            {
                verificacion();
                pasaron++;
                detalles.Add($"[PASA] {nombre}");
            }
            catch (Exception ex)
            {
                detalles.Add($"[FALLA] {nombre}: {ex.Message}");
            }
        }

        Caso("Sin martingala — TimestampResolucion es exactamente una vela despues de TimestampEntrada (minimo posible por RN-13)",
            VerificarSinMartingala);
        Caso("Con martingala — la distancia TimestampResolucion-TimestampEntrada es mayor que el caso sin martingala",
            VerificarConMartingala);
        Caso("Determinismo — dos corridas identicas producen exactamente los mismos timestamps por operacion",
            VerificarDeterminismo);
        Caso("Compatibilidad — PerfilMultiTf.Medir sigue funcionando sin cambios sobre InfoOperacionResuelta extendido",
            VerificarCompatibilidadPerfilMultiTf);

        return (total, pasaron, detalles);
    }

    // Cuadrante de 5 velas: posicion 2 (0-indexed) es la vela de senal para Tres Mosqueteros.
    // Vela verde en la posicion de cierre (N+1) => acierta en el primer intento, sin martingala.
    private static IReadOnlyList<Candle> VelasSinMartingala()
    {
        long t = 1_000_000L;
        var velas = new List<Candle>();
        // cuadrante 0: velas 0,1 neutras (rojas), vela 2 = senal verde, vela 3 = cierre (verde, acierta), vela 4 relleno
        velas.Add(Vela(t += 60, 100m, 99m));   // 0: roja
        velas.Add(Vela(t += 60, 100m, 99m));   // 1: roja
        velas.Add(Vela(t += 60, 100m, 101m));  // 2: verde -> senal Buy
        velas.Add(Vela(t += 60, 100m, 101m));  // 3: verde -> cierre acierta (N+1 de la senal)
        velas.Add(Vela(t += 60, 100m, 99m));   // 4: relleno
        return velas;
    }

    // Igual que el caso anterior pero la vela de cierre (N+1) pierde, forzando 1 martingala;
    // la reapertura se resuelve 2 ciclos despues de la vela de senal original.
    private static IReadOnlyList<Candle> VelasConMartingala()
    {
        long t = 1_000_000L;
        var velas = new List<Candle>();
        velas.Add(Vela(t += 60, 100m, 99m));   // 0: roja
        velas.Add(Vela(t += 60, 100m, 99m));   // 1: roja
        velas.Add(Vela(t += 60, 100m, 101m));  // 2: verde -> senal Buy, TimestampEntrada = esta vela
        velas.Add(Vela(t += 60, 100m, 99m));   // 3: roja -> cierre falla, martingala 1 (reapertura diferida)
        velas.Add(Vela(t += 60, 100m, 99m));   // 4: reapertura (EsperandoReapertura -> EsperandoCierre)
        velas.Add(Vela(t += 60, 100m, 101m));  // 5: verde -> cierre de la martingala, acierta, TimestampResolucion = esta vela
        velas.Add(Vela(t += 60, 100m, 99m));   // 6: relleno
        return velas;
    }

    private static Candle Vela(long timestamp, decimal open, decimal close) =>
        new(timestamp, open, Math.Max(open, close), Math.Min(open, close), close, 1m);

    private static void VerificarSinMartingala()
    {
        var operaciones = new List<InfoOperacionResuelta>();
        var estrategia = new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: operaciones.Add);
        BacktestRunner.Ejecutar(new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: VelasSinMartingala()), estrategia);

        Assert(operaciones.Count >= 1, "Debe haberse resuelto al menos una operacion");
        var op = operaciones[0];
        Assert(op.MartingalasUsadas == 0, $"Esta operacion no debe usar martingala (uso {op.MartingalasUsadas})");
        // RN-13 (desfase N/N+1): incluso sin martingala, la senal se abre en la vela N y su cierre
        // se evalua en el Observar() de la vela N+1 — la distancia minima posible es 1 vela.
        Assert(op.TimestampResolucion - op.TimestampEntrada == 60,
            $"Sin martingala, la distancia debe ser exactamente 1 vela (60s) por el desfase RN-13 (Entrada={op.TimestampEntrada}, Resolucion={op.TimestampResolucion})");
    }

    private static void VerificarConMartingala()
    {
        var operaciones = new List<InfoOperacionResuelta>();
        var estrategia = new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: operaciones.Add);
        BacktestRunner.Ejecutar(new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: VelasConMartingala()), estrategia);

        Assert(operaciones.Count >= 1, "Debe haberse resuelto al menos una operacion");
        var op = operaciones[0];
        Assert(op.MartingalasUsadas >= 1, $"Esta operacion debe haber usado al menos 1 martingala (uso {op.MartingalasUsadas})");
        // Con martingala, la distancia debe superar el minimo de 1 vela (60s) del caso sin martingala.
        Assert(op.TimestampResolucion - op.TimestampEntrada > 60,
            $"Con martingala, la distancia debe superar el minimo de 1 vela (Entrada={op.TimestampEntrada}, Resolucion={op.TimestampResolucion})");
    }

    private static void VerificarDeterminismo()
    {
        var velas = VelasConMartingala();

        var op1 = new List<InfoOperacionResuelta>();
        BacktestRunner.Ejecutar(new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velas),
            new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: op1.Add));

        var op2 = new List<InfoOperacionResuelta>();
        BacktestRunner.Ejecutar(new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velas),
            new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: op2.Add));

        Assert(op1.Count == op2.Count, "Ambas corridas deben producir el mismo numero de operaciones");
        for (var i = 0; i < op1.Count; i++)
        {
            Assert(op1[i].TimestampEntrada == op2[i].TimestampEntrada, $"TimestampEntrada debe coincidir en la operacion {i}");
            Assert(op1[i].TimestampResolucion == op2[i].TimestampResolucion, $"TimestampResolucion debe coincidir en la operacion {i}");
        }
    }

    private static void VerificarCompatibilidadPerfilMultiTf()
    {
        var operaciones = new List<InfoOperacionResuelta>();
        var estrategia = new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: operaciones.Add);
        var velas = VelasConMartingala();
        var resultado = BacktestRunner.Ejecutar(new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velas), estrategia);

        var identidad = new EvaluacionMultiTf.IdentidadExperimento(
            Dataset: "sintetico-test", Timeframe: "1m", Estrategia: "Tres Mosqueteros",
            CapitalInicial: 1000m, AggregationVersion: "test", SourceSha256: "test", TimeframeSha256: "test",
            FechaEjecucionUtc: DateTime.UtcNow);

        var perfil = EvaluacionMultiTf.PerfilMultiTf.Medir(identidad, resultado, operaciones, velas.Count, velas.Count);

        Assert(perfil.OperacionesCompletadas == operaciones.Count,
            "PerfilMultiTf.Medir debe seguir contando operaciones igual que antes de extender InfoOperacionResuelta");
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
}
