using TD_Project.Application;
using TD_Project.EvaluacionMultiTf;

namespace TD_Project.AnalisisOperacional;

// Fase 1.2, Paso 2/3: pruebas con resultados YA conocidos y publicados en
// catalogo_estrategias/TRES_MOSQUETEROS.md y MHI_MAYORIA.md (Fase 2C, corrida verificada,
// determinismo confirmado en Fase 1.0). No se genera dato nuevo: se reconstruye un PerfilMultiTf
// con los mismos numeros ya documentados y se compara "Motor dice: X" vs "Analizador interpreta: X",
// sin tocar BacktestRunner, IStrategy ni PerfilMultiTf.Medir().
public static class Tests
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

        Caso("Tres Mosqueteros 1m — resultado general reproduce catalogo publicado", TresMosqueteros1m);
        Caso("Tres Mosqueteros 1m — resolucion de intentos reproduce catalogo publicado", TresMosqueteros1mResolucion);
        Caso("MHI Mayoria 1D — resultado general reproduce catalogo publicado (muestra pequeña)", MhiMayoria1D);
        Caso("MHI Mayoria 4h — peores escenarios y exposicion coinciden con catalogo", MhiMayoria4hPeoresEscenarios);
        Caso("Analizador no altera EquityFinal negativo (Tres Mosqueteros 1h)", TresMosqueteros1hEquityNegativo);
        Caso("Particion exhaustiva: los 4 porcentajes de ResolucionDeIntentos suman 100%", ParticionExhaustiva);

        return (total, pasaron, detalles);
    }

    // catalogo_estrategias/TRES_MOSQUETEROS.md — tabla "Metricas operativas oficiales", fila 1m.
    private static PerfilMultiTf PerfilTresMosqueteros1m() => new()
    {
        Identidad = new IdentidadExperimento("BTCUSDT_2024-01-02_2025-01-02", "1m", "Tres Mosqueteros", 1000m, "n/a (origen, no derivado)", "f1a9dcbe72bd...", "f1a9dcbe72bd...", DateTime.UtcNow),
        EstadoMotor = EstadoBacktest.Success,
        EquityInicial = 1000m,
        EquityFinal = 99965.42m,
        OperacionesCompletadas = 82475,
        OperacionesGanadas = 71816,
        OperacionesPerdidas = 10659,
        OperacionAbiertaAlCierre = false,
        CapitalComprometidoAlCierre = 0m,
        RachaNegativaMaxima = 6,
        Racha2 = 1085,
        Racha3 = 141,
        Racha4 = 21,
        Racha5Mas = 3,
        GanoInicial = 41097,
        GanoM1 = 20396,
        GanoM2 = 10323,
        PerdioAgotandoMartingalas = 10659,
        MaxExposicion = 1m,
        ReconciliacionCoherente = true,
        ErroresReconciliacion = Array.Empty<string>(),
        VelasDisponibles = 527040,
        VelasUtilizadas = 527040,
    };

    private static void TresMosqueteros1m()
    {
        var reporte = AnalizadorOperacional.Analizar(PerfilTresMosqueteros1m());

        Assert(reporte.ResultadoGeneral.IntentosCompletados == 82475, "IntentosCompletados debe ser 82475");
        Assert(reporte.ResultadoGeneral.Victorias == 71816, "Victorias debe ser 71816");
        Assert(reporte.ResultadoGeneral.Derrotas == 10659, "Derrotas debe ser 10659");
        Assert(!reporte.ResultadoGeneral.IntentoIncompleto, "1m no tiene operacion abierta al cierre");
        // Winrate publicado en catalogo: 87.08%
        AssertAproximado(reporte.ResultadoGeneral.EficienciaOperacionalPct, 87.08m, 0.01m, "EficienciaOperacionalPct debe reproducir el 87.08% ya publicado");
    }

    private static void TresMosqueteros1mResolucion()
    {
        var reporte = AnalizadorOperacional.Analizar(PerfilTresMosqueteros1m());
        var r = reporte.ResolucionDeIntentos;

        // Valores publicados en catalogo: %RecuperacionM1=24.73%, %RecuperacionM2=12.52%
        AssertAproximado(r.RecuperacionM1Pct, 24.73m, 0.01m, "RecuperacionM1Pct debe reproducir 24.73%");
        AssertAproximado(r.RecuperacionM2Pct, 12.52m, 0.01m, "RecuperacionM2Pct debe reproducir 12.52%");
        // %Martingala publicado en catalogo: 37.2%
        AssertAproximado(r.PctResueltasPorMartingala, 37.2m, 0.05m, "PctResueltasPorMartingala debe reproducir 37.2% ya publicado (motor dice X, analizador interpreta X)");
    }

    // catalogo_estrategias/MHI_MAYORIA.md — fila 1D, la muestra mas pequeña del catalogo (57 operaciones).
    private static PerfilMultiTf PerfilMhiMayoria1D() => new()
    {
        Identidad = new IdentidadExperimento("BTCUSDT_2024-01-02_2025-01-02", "1D", "MHI Mayoria", 1000m, "1.0", "f1a9dcbe72bd...", "1356dd242e5a...", DateTime.UtcNow),
        EstadoMotor = EstadoBacktest.Success,
        EquityInicial = 1000m,
        EquityFinal = -8170.30m,
        OperacionesCompletadas = 57,
        OperacionesGanadas = 48,
        OperacionesPerdidas = 9,
        OperacionAbiertaAlCierre = false,
        CapitalComprometidoAlCierre = 0m,
        RachaNegativaMaxima = 2,
        Racha2 = 1,
        Racha3 = 0,
        Racha4 = 0,
        Racha5Mas = 0,
        GanoInicial = 30,
        GanoM1 = 11,
        GanoM2 = 7,
        PerdioAgotandoMartingalas = 9,
        MaxExposicion = 1m,
        ReconciliacionCoherente = true,
        ErroresReconciliacion = Array.Empty<string>(),
        VelasDisponibles = 366,
        VelasUtilizadas = 366,
    };

    private static void MhiMayoria1D()
    {
        var reporte = AnalizadorOperacional.Analizar(PerfilMhiMayoria1D());

        Assert(reporte.ResultadoGeneral.IntentosCompletados == 57, "IntentosCompletados debe ser 57 (muestra pequeña)");
        // Winrate publicado en catalogo: 84.21%
        AssertAproximado(reporte.ResultadoGeneral.EficienciaOperacionalPct, 84.21m, 0.01m, "EficienciaOperacionalPct debe reproducir 84.21% incluso con muestra chica");
    }

    // catalogo_estrategias/MHI_MAYORIA.md — fila 4h: AbiertaAlCierre=si, capital comprometido=9462.708.
    private static PerfilMultiTf PerfilMhiMayoria4h() => new()
    {
        Identidad = new IdentidadExperimento("BTCUSDT_2024-01-02_2025-01-02", "4h", "MHI Mayoria", 1000m, "1.0", "f1a9dcbe72bd...", "2be5fba6896a...", DateTime.UtcNow),
        EstadoMotor = EstadoBacktest.Success,
        EquityInicial = 1000m,
        EquityFinal = -498.19m,
        OperacionesCompletadas = 350,
        OperacionesGanadas = 308,
        OperacionesPerdidas = 42,
        OperacionAbiertaAlCierre = true,
        CapitalComprometidoAlCierre = 9462.708m,
        RachaNegativaMaxima = 2,
        Racha2 = 5,
        Racha3 = 0,
        Racha4 = 0,
        Racha5Mas = 0,
        GanoInicial = 189,
        GanoM1 = 73,
        GanoM2 = 46,
        PerdioAgotandoMartingalas = 42,
        MaxExposicion = 1m,
        ReconciliacionCoherente = true,
        ErroresReconciliacion = Array.Empty<string>(),
        VelasDisponibles = 2196,
        VelasUtilizadas = 2196,
    };

    private static void MhiMayoria4hPeoresEscenarios()
    {
        var reporte = AnalizadorOperacional.Analizar(PerfilMhiMayoria4h());

        Assert(reporte.ResultadoGeneral.IntentoIncompleto, "4h debe reportar operacion abierta al cierre (categoria separada, no ganada/perdida)");
        Assert(reporte.PeoresEscenarios.MayorRachaNegativa == 2, "MayorRachaNegativa debe ser 2");
        Assert(reporte.PeoresEscenarios.AlcanzoTopeMartingala, "GanoM2=46>0 implica que si alcanzo el tope de martingala configurado");
        Assert(reporte.PeoresEscenarios.MayorExposicionExperimental == 1m, "MaxExposicion debe ser 1, igual que el motor");
    }

    // catalogo_estrategias/TRES_MOSQUETEROS.md — fila 1h: EquityFinal=-29940.68 (negativo, observacion experimental, no bug).
    private static void TresMosqueteros1hEquityNegativo()
    {
        var perfil = new PerfilMultiTf
        {
            Identidad = new IdentidadExperimento("BTCUSDT_2024-01-02_2025-01-02", "1h", "Tres Mosqueteros", 1000m, "1.0", "f1a9dcbe72bd...", "f3f120c7c672...", DateTime.UtcNow),
            EstadoMotor = EstadoBacktest.Success,
            EquityInicial = 1000m,
            EquityFinal = -29940.68m,
            OperacionesCompletadas = 1380,
            OperacionesGanadas = 1194,
            OperacionesPerdidas = 186,
            OperacionAbiertaAlCierre = true,
            CapitalComprometidoAlCierre = 9480.299m,
            RachaNegativaMaxima = 3,
            Racha2 = 19,
            Racha3 = 4,
            Racha4 = 0,
            Racha5Mas = 0,
            GanoInicial = 632,
            GanoM1 = 372,
            GanoM2 = 190,
            PerdioAgotandoMartingalas = 186,
            MaxExposicion = 1m,
            ReconciliacionCoherente = true,
            ErroresReconciliacion = Array.Empty<string>(),
            VelasDisponibles = 8784,
            VelasUtilizadas = 8784,
        };

        var reporte = AnalizadorOperacional.Analizar(perfil);

        // El analizador NO reinterpreta el equity negativo — lo reporta exactamente como el motor lo produjo.
        Assert(reporte.DatosDerivadosModeloActual.EquityFinal == -29940.68m, "El analizador debe reportar EquityFinal negativo sin alterarlo");
        Assert(reporte.ReconciliacionCoherente, "Reconciliacion sigue coherente aunque el equity sea negativo (no es un bug)");
        // La eficiencia operacional (winrate) es independiente del signo del equity.
        AssertAproximado(reporte.ResultadoGeneral.EficienciaOperacionalPct, 86.52m, 0.01m, "EficienciaOperacionalPct no debe verse afectada por el equity negativo");
    }

    private static void ParticionExhaustiva()
    {
        var reporte = AnalizadorOperacional.Analizar(PerfilTresMosqueteros1m());
        var r = reporte.ResolucionDeIntentos;
        var suma = r.VictoriaInicialPct + r.RecuperacionM1Pct + r.RecuperacionM2Pct + r.PerdidaAgotandoPct;
        AssertAproximado(suma, 100m, 0.01m, "Los 4 porcentajes de ResolucionDeIntentos deben sumar 100% (particion exhaustiva, ESPECIFICACION §4.2)");
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }

    private static void AssertAproximado(decimal actual, decimal esperado, decimal tolerancia, string mensaje)
    {
        if (Math.Abs(actual - esperado) > tolerancia)
            throw new Exception($"{mensaje} (esperado={esperado}, actual={actual})");
    }
}
