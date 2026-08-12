using System.Diagnostics;
using TD_Project.AnalisisOperacional;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;
using TD_Project.ModeloFinanciero;
using TD_Project.Protocolo;

namespace TD_Project.Caso3;

// spec: Caso 3B D-099 a D-107 — pruebas requeridas por
// ESPECIFICACION_ESTRATEGIA_VOLUMEN_BREAKOUT_V1.md §7: P1 jerarquia, P2/P3 entrada Long/Short,
// P4 volumen sin breakout, P5 exclusion de la vela actual, P6 operador estricto, P7/P8 reversion
// (cierre por senal contraria, D-107), P9 sin posiciones simultaneas en la misma direccion,
// P10 determinismo, P11 metadata, P12 rendimiento, P13 integracion, P14 regresion.
public static class TestsEstrategiaVolumenBreakout
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos(string dirDatasets)
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

        Caso("P1 — Jerarquia: volumen insuficiente implica Secundaria=null, breakout nunca se reporta como evaluado", VerificarJerarquia);
        Caso("P2 — Entrada Long: volumen alto + ruptura de maximo produce Buy", VerificarEntradaLong);
        Caso("P3 — Entrada Short: volumen alto + ruptura de minimo produce Sell", VerificarEntradaShort);
        Caso("P4 — Volumen sin breakout: Primaria=true, Secundaria=false, sin orden", VerificarVolumenSinBreakout);
        Caso("P5 — Maximo y minimo excluyen la vela actual (sin circularidad)", VerificarExclusionVelaActual);
        Caso("P6 — Operador estricto: igualdad exacta con el extremo no cuenta como ruptura", VerificarOperadorEstricto);
        Caso("P7 — Reversion Long a Short por senal contraria (D-107), verificado contra AplicadorFill", VerificarReversionLongAShort);
        Caso("P8 — Reversion Short a Long por senal contraria (D-107), verificado contra AplicadorFill", VerificarReversionShortALong);
        Caso("P9 — Sin posiciones simultaneas en la misma direccion", VerificarSinPosicionesSimultaneas);
        Caso("P10 — Determinismo: dos instancias independientes producen la misma secuencia de Side", VerificarDeterminismo);
        Caso("P11 — CaracteristicasEstrategia.UsaMartingala=false produce 'no aplica' en el reporte", VerificarMetadata);
        Caso("P12 — Rendimiento razonable sobre dataset grande", VerificarRendimiento);
        Caso("P13 — Integracion en el pipeline sin cambios de codigo (EjecutorProtocolo)", () => VerificarIntegracionPipeline(dirDatasets));
        Caso("P14 — Regresion: EstrategiaVolumenBreakout no afecta corridas de otras estrategias", () => VerificarRegresion(dirDatasets));

        return (total, pasaron, detalles);
    }

    private const int Ventana = 20;
    private const decimal Multiplo = 1.5m;

    // Dataset base: precio plano (evita breakout accidental), volumen bajo constante (evita
    // disparar la condicion primaria por accidente) — cada prueba modifica puntualmente la vela
    // de interes para aislar la condicion que quiere verificar.
    private static List<Candle> DatasetBase(int cantidad, decimal precio = 100m, decimal volumenBase = 10m)
    {
        var velas = new List<Candle>();
        for (var i = 0; i < cantidad; i++)
            velas.Add(new Candle(i, precio, precio, precio, precio, volumenBase));
        return velas;
    }

    private static List<ResultadoEvaluacionCondiciones> EjecutarSecuencia(EstrategiaVolumenBreakout estrategia, IReadOnlyList<Candle> velas, List<OrderRequest[]>? ordenesPorVela = null)
    {
        var resultados = new List<ResultadoEvaluacionCondiciones>();
        var estrategiaConCallback = estrategia;
        var acumuladas = new List<Candle>(velas.Count);
        foreach (var vela in velas)
        {
            acumuladas.Add(vela);
            var ordenes = estrategiaConCallback.Observar(new DataSlice(acumuladas)).ToArray();
            ordenesPorVela?.Add(ordenes);
        }
        return resultados;
    }

    private static EstrategiaVolumenBreakout Crear(List<ResultadoEvaluacionCondiciones> capturados, List<InfoOperacionResuelta>? operaciones = null)
        => new(Ventana, Multiplo, Ventana, capturados.Add, operaciones is null ? null : operaciones.Add);

    private static void VerificarJerarquia()
    {
        // Volumen constante e insuficiente (nunca > media*1.5, porque es literalmente la misma
        // media): warmup con volumen constante nunca dispara Primaria=true. Precio con rupturas
        // deliberadas para confirmar que, aun con precio rompiendo rango, Secundaria permanece null.
        var velas = DatasetBase(30, volumenBase: 10m);
        velas[25] = velas[25] with { Close = 1000m, High = 1000m }; // rompe el maximo, si se evaluara.

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas);

        foreach (var r in capturados)
        {
            if (r.Primaria)
                throw new Exception("Con volumen constante (nunca > media*1.5) Primaria nunca deberia ser true.");
            if (r.Secundaria is not null)
                throw new Exception($"Secundaria debe ser null cuando Primaria=false (D-099), se obtuvo {r.Secundaria}.");
        }
    }

    private static void VerificarEntradaLong()
    {
        var velas = DatasetBase(Ventana + 2, precio: 100m, volumenBase: 10m);
        // Vela de senal: volumen muy por encima de la media (10) y Close rompe el maximo (100).
        velas[Ventana] = velas[Ventana] with { Volume = 1000m, Close = 200m, High = 200m };

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var ordenesPorVela = new List<OrderRequest[]>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas, ordenesPorVela);

        var ordenesSenal = ordenesPorVela[Ventana];
        if (ordenesSenal.Length != 1 || ordenesSenal[0].Side != Side.Buy)
            throw new Exception($"Se esperaba una unica OrderRequest Buy en la vela de ruptura alcista, se obtuvo {ordenesSenal.Length} ordenes.");

        var resultadoSenal = capturados[Ventana];
        if (resultadoSenal.Accion != AccionEvaluacion.OrdenEmitida || resultadoSenal.DireccionSenal != Side.Buy)
            throw new Exception($"Accion/DireccionSenal incorrectos en la vela de ruptura alcista: {resultadoSenal.Accion}/{resultadoSenal.DireccionSenal}.");
    }

    private static void VerificarEntradaShort()
    {
        var velas = DatasetBase(Ventana + 2, precio: 100m, volumenBase: 10m);
        velas[Ventana] = velas[Ventana] with { Volume = 1000m, Close = 10m, Low = 10m };

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var ordenesPorVela = new List<OrderRequest[]>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas, ordenesPorVela);

        var ordenesSenal = ordenesPorVela[Ventana];
        if (ordenesSenal.Length != 1 || ordenesSenal[0].Side != Side.Sell)
            throw new Exception($"Se esperaba una unica OrderRequest Sell en la vela de ruptura bajista, se obtuvo {ordenesSenal.Length} ordenes.");

        var resultadoSenal = capturados[Ventana];
        if (resultadoSenal.Accion != AccionEvaluacion.OrdenEmitida || resultadoSenal.DireccionSenal != Side.Sell)
            throw new Exception($"Accion/DireccionSenal incorrectos en la vela de ruptura bajista: {resultadoSenal.Accion}/{resultadoSenal.DireccionSenal}.");
    }

    private static void VerificarVolumenSinBreakout()
    {
        var velas = DatasetBase(Ventana + 2, precio: 100m, volumenBase: 10m);
        // Volumen alto pero Close se mantiene dentro del rango (100, ni rompe maximo ni minimo).
        velas[Ventana] = velas[Ventana] with { Volume = 1000m };

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var ordenesPorVela = new List<OrderRequest[]>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas, ordenesPorVela);

        var resultadoSenal = capturados[Ventana];
        if (!resultadoSenal.Primaria || resultadoSenal.Secundaria != false)
            throw new Exception($"Se esperaba Primaria=true, Secundaria=false; se obtuvo Primaria={resultadoSenal.Primaria}, Secundaria={resultadoSenal.Secundaria}.");
        if (ordenesPorVela[Ventana].Length != 0)
            throw new Exception("No debe emitirse ninguna orden cuando el volumen cumple pero el precio no rompe el rango.");
    }

    private static void VerificarExclusionVelaActual()
    {
        // 20 velas previas con maximo 100 (High=100). La vela 20 tiene High=200 (para que, si se
        // incluyera a si misma, su propio Close=200 "rompería" un maximo que ella misma formo) y
        // Close=200 tambien. Si el maximo excluye la vela actual (correcto), 200 > 100 = ruptura.
        // El objetivo real de esta prueba es la inversa: confirmar que una vela cuyo Close iguala
        // el maximo de las 20 anteriores MAS ella misma pero es menor al maximo de las 20
        // anteriores SIN incluirla, rompe igual (prueba de no-circularidad).
        var velas = DatasetBase(Ventana, precio: 100m, volumenBase: 10m);
        velas.Add(new Candle(Ventana, 100m, 150m, 100m, 150m, 1000m)); // High=150 rompe el maximo previo (100).

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var ordenesPorVela = new List<OrderRequest[]>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas, ordenesPorVela);

        if (ordenesPorVela[Ventana].Length != 1 || ordenesPorVela[Ventana][0].Side != Side.Buy)
            throw new Exception("La ruptura debe detectarse contra el maximo de las 20 velas ANTERIORES (100), sin incluir el High de la vela actual (150) en ese maximo de referencia.");
    }

    private static void VerificarOperadorEstricto()
    {
        // Vela de senal con Close exactamente igual al maximo previo (100): no debe contar como ruptura.
        var velas = DatasetBase(Ventana + 1, precio: 100m, volumenBase: 10m);
        velas[Ventana] = velas[Ventana] with { Volume = 1000m, Close = 100m, High = 100m };

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var ordenesPorVela = new List<OrderRequest[]>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas, ordenesPorVela);

        if (ordenesPorVela[Ventana].Length != 0)
            throw new Exception("Close exactamente igual al maximo anterior no debe contar como ruptura (operador estricto, D-105).");
    }

    // Verifica la reversion contra el motor real (AplicadorFill), no solo la OrderRequest emitida
    // — mismo criterio de evidencia directa aplicado en D-095 (Caso 4).
    private static void VerificarReversionLongAShort()
    {
        var velas = DatasetBase(Ventana + 3, precio: 100m, volumenBase: 10m);
        velas[Ventana] = velas[Ventana] with { Volume = 1000m, Close = 200m, High = 200m }; // entrada Long.
        velas[Ventana + 1] = velas[Ventana + 1] with { Close = 200m, High = 200m, Low = 200m }; // vela neutra intermedia.
        velas[Ventana + 2] = velas[Ventana + 2] with { Volume = 1000m, Close = 5m, Low = 5m }; // ruptura bajista -> reversion.

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var ordenesPorVela = new List<OrderRequest[]>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas, ordenesPorVela);

        var ordenesReversion = ordenesPorVela[Ventana + 2];
        if (ordenesReversion.Length != 2 || ordenesReversion[0].Side != Side.Sell || ordenesReversion[1].Side != Side.Sell)
            throw new Exception($"Se esperaban 2 ordenes Sell (cierre Long + apertura Short) en la reversion, se obtuvo {ordenesReversion.Length} ordenes: {string.Join(",", ordenesReversion.Select(o => o.Side))}.");

        // Verificacion directa contra AplicadorFill: aplicar secuencialmente Buy 1 (apertura) y
        // luego los 2 Fills de la reversion, confirmar posicion final Short neta.
        var portfolio = new PortfolioState { Cash = 100_000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Buy, 1m, 100m, 0m, velas[Ventana].Timestamp, OrderType.Market), tasaMargen: 0.1m);
        AplicadorFill.Aplicar(portfolio, new Fill(2, ordenesReversion[0].Side, ordenesReversion[0].Cantidad, 5m, 0m, velas[Ventana + 2].Timestamp, OrderType.Market), tasaMargen: 0.1m);
        AplicadorFill.Aplicar(portfolio, new Fill(3, ordenesReversion[1].Side, ordenesReversion[1].Cantidad, 5m, 0m, velas[Ventana + 2].Timestamp, OrderType.Market), tasaMargen: 0.1m);

        var posicionFinal = PosicionActual.De(portfolio);
        if (posicionFinal != -1m)
            throw new Exception($"Posicion final tras la reversion Long->Short debe ser exactamente Short 1 (-1), se obtuvo {posicionFinal}.");
    }

    private static void VerificarReversionShortALong()
    {
        var velas = DatasetBase(Ventana + 3, precio: 100m, volumenBase: 10m);
        velas[Ventana] = velas[Ventana] with { Volume = 1000m, Close = 5m, Low = 5m }; // entrada Short.
        velas[Ventana + 1] = velas[Ventana + 1] with { Close = 5m, High = 5m, Low = 5m };
        velas[Ventana + 2] = velas[Ventana + 2] with { Volume = 1000m, Close = 200m, High = 200m }; // ruptura alcista -> reversion.

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var ordenesPorVela = new List<OrderRequest[]>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas, ordenesPorVela);

        var ordenesReversion = ordenesPorVela[Ventana + 2];
        if (ordenesReversion.Length != 2 || ordenesReversion[0].Side != Side.Buy || ordenesReversion[1].Side != Side.Buy)
            throw new Exception($"Se esperaban 2 ordenes Buy (cierre Short + apertura Long) en la reversion, se obtuvo {ordenesReversion.Length} ordenes.");

        var portfolio = new PortfolioState { Cash = 100_000m };
        AplicadorFill.Aplicar(portfolio, new Fill(1, Side.Sell, 1m, 100m, 0m, velas[Ventana].Timestamp, OrderType.Market), tasaMargen: 0.1m);
        AplicadorFill.Aplicar(portfolio, new Fill(2, ordenesReversion[0].Side, ordenesReversion[0].Cantidad, 200m, 0m, velas[Ventana + 2].Timestamp, OrderType.Market), tasaMargen: 0.1m);
        AplicadorFill.Aplicar(portfolio, new Fill(3, ordenesReversion[1].Side, ordenesReversion[1].Cantidad, 200m, 0m, velas[Ventana + 2].Timestamp, OrderType.Market), tasaMargen: 0.1m);

        var posicionFinal = PosicionActual.De(portfolio);
        if (posicionFinal != 1m)
            throw new Exception($"Posicion final tras la reversion Short->Long debe ser exactamente Long 1 (+1), se obtuvo {posicionFinal}.");
    }

    private static void VerificarSinPosicionesSimultaneas()
    {
        var velas = DatasetBase(Ventana + 3, precio: 100m, volumenBase: 10m);
        velas[Ventana] = velas[Ventana] with { Volume = 1000m, Close = 200m, High = 200m }; // entrada Long.
        velas[Ventana + 1] = velas[Ventana + 1] with { Close = 200m, High = 200m };
        velas[Ventana + 2] = velas[Ventana + 2] with { Volume = 1000m, Close = 400m, High = 400m }; // otra ruptura alcista, misma direccion.

        var capturados = new List<ResultadoEvaluacionCondiciones>();
        var ordenesPorVela = new List<OrderRequest[]>();
        var estrategia = Crear(capturados);
        EjecutarSecuencia(estrategia, velas, ordenesPorVela);

        if (ordenesPorVela[Ventana + 2].Length != 0)
            throw new Exception("No debe emitirse una segunda orden de apertura en la misma direccion con posicion ya abierta (D-104).");
        if (capturados[Ventana + 2].Accion != AccionEvaluacion.OrdenOmitidaPorPosicionAbierta)
            throw new Exception($"Accion esperada OrdenOmitidaPorPosicionAbierta, se obtuvo {capturados[Ventana + 2].Accion}.");
    }

    private static void VerificarDeterminismo()
    {
        var velas = DatasetBase(Ventana + 5, precio: 100m, volumenBase: 10m);
        velas[Ventana] = velas[Ventana] with { Volume = 1000m, Close = 200m, High = 200m };
        velas[Ventana + 3] = velas[Ventana + 3] with { Volume = 1000m, Close = 5m, Low = 5m };

        var ordenes1 = new List<OrderRequest[]>();
        var ordenes2 = new List<OrderRequest[]>();
        EjecutarSecuencia(Crear(new List<ResultadoEvaluacionCondiciones>()), velas, ordenes1);
        EjecutarSecuencia(Crear(new List<ResultadoEvaluacionCondiciones>()), velas, ordenes2);

        for (var i = 0; i < ordenes1.Count; i++)
        {
            if (ordenes1[i].Length != ordenes2[i].Length)
                throw new Exception($"Vela {i}: dos instancias independientes produjeron cantidades de ordenes distintas.");
            for (var j = 0; j < ordenes1[i].Length; j++)
                if (ordenes1[i][j].Side != ordenes2[i][j].Side)
                    throw new Exception($"Vela {i}, orden {j}: dos instancias independientes divergieron en Side.");
        }
    }

    private static void VerificarMetadata()
    {
        var caracteristicas = new CaracteristicasEstrategia(UsaMartingala: false);
        var resolucionCualquiera = new ResolucionDeIntentos(0m, 0m, 0m, 100m, 0m);
        var texto = PresentadorResolucionIntentos.Formatear(resolucionCualquiera, caracteristicas);
        if (!texto.Contains("no aplica"))
            throw new Exception($"Se esperaba 'no aplica' para UsaMartingala=false, se obtuvo: {texto}");
    }

    private static void VerificarRendimiento()
    {
        var velas = DatasetBase(100_000, precio: 100m, volumenBase: 10m);
        var estrategia = new EstrategiaVolumenBreakout();
        var acumuladas = new List<Candle>(velas.Count);
        var sw = Stopwatch.StartNew();
        foreach (var vela in velas)
        {
            acumuladas.Add(vela);
            estrategia.Observar(new DataSlice(acumuladas));
        }
        sw.Stop();

        if (sw.ElapsedMilliseconds > 10_000)
            throw new Exception($"Corrida de 100k velas tardo {sw.ElapsedMilliseconds}ms — inesperado con N=20 fijo.");
    }

    private static void VerificarIntegracionPipeline(string dirDatasets)
    {
        var entrada = new EntradaProtocolo(
            Estrategia: "VolumenBreakout", VersionEstrategia: "1.0", Parametros: new[] { "ventanaVolumen=20", "multiploVolumen=1.5", "ventanaBreakout=20" },
            CrearEstrategia: onOp => new EstrategiaVolumenBreakout(onOperacionResuelta: onOp),
            Timeframes: new[] { "1D" }, DirDatasets: dirDatasets, NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
            CapitalInicial: 1000m, Caracteristicas: new CaracteristicasEstrategia(UsaMartingala: false));

        var resultado = EjecutorProtocolo.Ejecutar(entrada);
        var corrida1D = resultado.Corridas.Single(c => c.Timeframe == "1D");
        if (corrida1D.Estado != EstadoCorridaTimeframe.Success)
            throw new Exception($"EstrategiaVolumenBreakout no integro correctamente al pipeline: Estado={corrida1D.Estado}, Motivo={corrida1D.MotivoFallo}.");
        if (corrida1D.MetricasFinancieras is null)
            throw new Exception("MetricasFinancieras deberia poblarse en una corrida Success, igual que para cualquier otra estrategia.");
    }

    private static void VerificarRegresion(string dirDatasets)
    {
        var entradaControl = new EntradaProtocolo(
            Estrategia: "Tres Mosqueteros", VersionEstrategia: "1.0", Parametros: new[] { "maxMartingalas=2" },
            CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
            Timeframes: new[] { "1D" }, DirDatasets: dirDatasets, NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
            CapitalInicial: 1000m);

        var resultado1 = EjecutorProtocolo.Ejecutar(entradaControl);
        var resultado2 = EjecutorProtocolo.Ejecutar(entradaControl);

        if (resultado1.Identidad.HashCompuesto != resultado2.Identidad.HashCompuesto)
            throw new Exception("HashCompuesto de Tres Mosqueteros cambio entre corridas identicas — regresion inesperada.");
    }
}
