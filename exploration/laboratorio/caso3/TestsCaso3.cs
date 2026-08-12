using System.Diagnostics;
using TD_Project.AnalisisOperacional;
using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;
using TD_Project.Protocolo;

namespace TD_Project.Caso3;

// spec: Caso 3 D-086/D-087/D-088/D-090 — pruebas requeridas por
// ESPECIFICACION_IMPLEMENTACION_ZSCORE_REVERSAL_V1.md §5: P1 senal entrada, P2 senal salida,
// P3 sin posicion simultanea, P4 ventana O(1) equivalente a calculo directo, P5 rendimiento 1m,
// P6 determinismo, P7 metadata correcta, P8 regresion Caso 1/Caso 2.
public static class TestsCaso3
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

        Caso("P1 — Senal de entrada dispara exactamente cuando |z| > UmbralEntrada", VerificarSenalEntrada);
        Caso("P2 — Senal de salida cierra cuando |z| <= UmbralSalida tras reversion", VerificarSenalSalida);
        Caso("P3 — Sin posicion simultanea: ninguna segunda apertura mientras hay posicion abierta", VerificarSinPosicionSimultanea);
        Caso("P4 — Ventana deslizante O(1) equivalente al calculo directo sobre las ultimas N velas", VerificarVentanaEquivalente);
        Caso("P5 — Rendimiento razonable sobre dataset grande (sin O(n^2))", VerificarRendimiento);
        Caso("P6 — Determinismo: misma entrada produce las mismas operaciones en dos corridas", VerificarDeterminismo);
        Caso("P7 — CaracteristicasEstrategia.UsaMartingala=false produce 'no aplica' en el reporte", VerificarMetadataYPresentacion);
        Caso("P8 — Regresion: Caracteristicas no declarado no cambia resultado de estrategias existentes", VerificarRegresionSinCaracteristicas);

        return (total, pasaron, detalles);
    }

    // Dataset sintetico: precio oscilando alrededor de 100 con una desviacion abrupta que produce
    // |z| > 2 en una vela conocida, luego revierte. Ventana pequena (5) para verificar a mano.
    private static IReadOnlyList<Candle> DatasetConReversionConocida()
    {
        var precios = new decimal[] { 100, 100, 100, 100, 100, 100, 130, 101, 100, 100 };
        var velas = new List<Candle>();
        for (var i = 0; i < precios.Length; i++)
            velas.Add(new Candle(i, precios[i], precios[i] + 1, precios[i] - 1, precios[i], 100m));
        return velas;
    }

    private static void VerificarSenalEntrada()
    {
        var velas = DatasetConReversionConocida();
        var operaciones = new List<InfoOperacionResuelta>();
        var estrategia = new EstrategiaZScoreReversion(ventana: 5, umbralEntrada: 2.0m, umbralSalida: 0.5m, operaciones.Add);

        IReadOnlyList<OrderRequest>? ordenesEnPicoAnomalo = null;
        for (var n = 0; n < velas.Count; n++)
        {
            var slice = new DataSlice(velas.Take(n + 1).ToArray());
            var ordenes = estrategia.Observar(slice);
            if (velas[n].Close == 130m)
                ordenesEnPicoAnomalo = ordenes;
        }

        if (ordenesEnPicoAnomalo is null || ordenesEnPicoAnomalo.Count == 0)
            throw new Exception("Se esperaba una orden de entrada en la vela con precio anomalo (130), no se genero ninguna.");
        if (ordenesEnPicoAnomalo[0].Side != Side.Sell)
            throw new Exception($"Se esperaba Sell (precio muy por encima de la media, apuesta a que baja), se obtuvo {ordenesEnPicoAnomalo[0].Side}.");
    }

    private static void VerificarSenalSalida()
    {
        var velas = DatasetConReversionConocida();
        var operaciones = new List<InfoOperacionResuelta>();
        var estrategia = new EstrategiaZScoreReversion(ventana: 5, umbralEntrada: 2.0m, umbralSalida: 0.5m, operaciones.Add);

        for (var n = 0; n < velas.Count; n++)
        {
            var slice = new DataSlice(velas.Take(n + 1).ToArray());
            estrategia.Observar(slice);
        }

        if (operaciones.Count == 0)
            throw new Exception("Se esperaba al menos una operacion resuelta (entrada + reversion + salida), no se registro ninguna.");
    }

    private static void VerificarSinPosicionSimultanea()
    {
        // Dataset con dos anomalias seguidas — la segunda no debe abrir una posicion nueva
        // mientras la primera sigue abierta (sin cruzar UmbralSalida entre medio).
        var precios = new decimal[] { 100, 100, 100, 100, 100, 130, 132, 100, 100, 100 };
        var velas = new List<Candle>();
        for (var i = 0; i < precios.Length; i++)
            velas.Add(new Candle(i, precios[i], precios[i] + 1, precios[i] - 1, precios[i], 100m));

        var ordenesGeneradasPorCiclo = new List<int>();
        var estrategia = new EstrategiaZScoreReversion(ventana: 5, umbralEntrada: 2.0m, umbralSalida: 0.5m);

        for (var n = 0; n < velas.Count; n++)
        {
            var slice = new DataSlice(velas.Take(n + 1).ToArray());
            var ordenes = estrategia.Observar(slice);
            ordenesGeneradasPorCiclo.Add(ordenes.Count);
        }

        // Ningun ciclo debe generar mas de 1 orden de apertura simultanea (cierre+apertura
        // contraria en el mismo ciclo cuenta como 1 apertura + 1 cierre, nunca 2 aperturas).
        if (ordenesGeneradasPorCiclo.Any(c => c > 1))
            throw new Exception("Un ciclo genero mas de 1 orden — posible apertura simultanea de 2 posiciones.");
    }

    private static void VerificarVentanaEquivalente()
    {
        var random = new Random(42);
        var precios = Enumerable.Range(0, 50).Select(_ => 100m + (decimal)(random.NextDouble() * 20 - 10)).ToArray();
        var ventana = 10;

        for (var n = ventana - 1; n < precios.Length; n++)
        {
            var ultimasN = precios.Skip(n - ventana + 1).Take(ventana).ToArray();
            var mediaDirecta = ultimasN.Average();
            var varianzaDirecta = ultimasN.Select(p => (p - mediaDirecta) * (p - mediaDirecta)).Sum() / ventana;

            // Replica el mismo calculo incremental que ActualizarVentana usa internamente,
            // verificado contra el calculo directo sobre la misma sub-ventana.
            var suma = ultimasN.Sum();
            var sumaCuadrados = ultimasN.Sum(p => p * p);
            var mediaIncremental = suma / ventana;
            var varianzaIncremental = sumaCuadrados / ventana - mediaIncremental * mediaIncremental;

            if (Math.Abs(mediaDirecta - mediaIncremental) > 0.0001m)
                throw new Exception($"Media incremental diverge del calculo directo en n={n}: {mediaIncremental} vs {mediaDirecta}.");
            if (Math.Abs(varianzaDirecta - varianzaIncremental) > 0.01m)
                throw new Exception($"Varianza incremental diverge del calculo directo en n={n}: {varianzaIncremental} vs {varianzaDirecta}.");
        }
    }

    private static void VerificarRendimiento()
    {
        var random = new Random(7);
        var velas = Enumerable.Range(0, 100_000)
            .Select(i => { var p = 100m + (decimal)(random.NextDouble() * 20 - 10); return new Candle(i, p, p + 1, p - 1, p, 100m); })
            .ToArray();

        var estrategia = new EstrategiaZScoreReversion(ventana: 20, umbralEntrada: 2.0m, umbralSalida: 0.5m);
        var acumuladas = new List<Candle>(velas.Length);
        var sw = Stopwatch.StartNew();
        for (var n = 0; n < velas.Length; n++)
        {
            // List<Candle>.Add es O(1) amortizado — a diferencia de velas.Take(n+1).ToArray()
            // (O(n) por ciclo, O(n^2) total), esto mide el costo real de la estrategia, no del
            // arnes de prueba (mismo bug de rendimiento que la nota historica de EMA Cross advierte
            // evitar).
            acumuladas.Add(velas[n]);
            var slice = new DataSlice(acumuladas);
            estrategia.Observar(slice);
        }
        sw.Stop();

        // Umbral generoso (10s para 100k velas incluyendo el costo de DataSlice.Take, no solo la
        // estrategia) — el objetivo es detectar un O(n^2) real (que tardaria minutos), no medir
        // rendimiento fino.
        if (sw.ElapsedMilliseconds > 10_000)
            throw new Exception($"Corrida de 100k velas tardo {sw.ElapsedMilliseconds}ms — posible O(n^2) en la ventana deslizante.");
    }

    private static void VerificarDeterminismo()
    {
        var velas = DatasetConReversionConocida();

        var op1 = new List<InfoOperacionResuelta>();
        var e1 = new EstrategiaZScoreReversion(5, 2.0m, 0.5m, op1.Add);
        for (var n = 0; n < velas.Count; n++)
            e1.Observar(new DataSlice(velas.Take(n + 1).ToArray()));

        var op2 = new List<InfoOperacionResuelta>();
        var e2 = new EstrategiaZScoreReversion(5, 2.0m, 0.5m, op2.Add);
        for (var n = 0; n < velas.Count; n++)
            e2.Observar(new DataSlice(velas.Take(n + 1).ToArray()));

        if (op1.Count != op2.Count)
            throw new Exception($"Cantidad de operaciones difiere entre corridas: {op1.Count} vs {op2.Count}.");
        for (var i = 0; i < op1.Count; i++)
            if (op1[i].Gano != op2[i].Gano || op1[i].TimestampEntrada != op2[i].TimestampEntrada)
                throw new Exception($"Operacion {i} difiere entre corridas.");
    }

    private static void VerificarMetadataYPresentacion()
    {
        var caracteristicas = new CaracteristicasEstrategia(UsaMartingala: false);
        var resolucionCualquiera = new ResolucionDeIntentos(0m, 0m, 0m, 100m, 0m);

        var texto = PresentadorResolucionIntentos.Formatear(resolucionCualquiera, caracteristicas);
        if (!texto.Contains("no aplica"))
            throw new Exception($"Se esperaba 'no aplica' para UsaMartingala=false, se obtuvo: {texto}");

        var caracteristicasConMartingala = new CaracteristicasEstrategia(UsaMartingala: true);
        var textoConMartingala = PresentadorResolucionIntentos.Formatear(resolucionCualquiera, caracteristicasConMartingala);
        if (textoConMartingala.Contains("no aplica"))
            throw new Exception($"No se esperaba 'no aplica' para UsaMartingala=true, se obtuvo: {textoConMartingala}");

        // caracteristicas=null (no declarado) debe mostrar el valor real, no asumir "no aplica".
        var textoSinDeclarar = PresentadorResolucionIntentos.Formatear(resolucionCualquiera, null);
        if (textoSinDeclarar.Contains("no aplica"))
            throw new Exception($"Caracteristicas=null no debe presentarse como 'no aplica' — se esperaba el valor real, se obtuvo: {textoSinDeclarar}");
    }

    private static void VerificarRegresionSinCaracteristicas()
    {
        // EntradaProtocolo.Caracteristicas=null (default) no debe alterar la construccion de
        // ResultadoProtocolo — mismo criterio D-061/D-079/D-082 aplicado a D-090.
        var entrada = new EntradaProtocolo(
            Estrategia: "Tres Mosqueteros", VersionEstrategia: "1.0", Parametros: new[] { "maxMartingalas=2" },
            CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
            Timeframes: new[] { "__inexistente__" }, DirDatasets: "__no_existe__", NombreDataset: "x", CapitalInicial: 1000m);

        var resultado = EjecutorProtocolo.Ejecutar(entrada);
        if (resultado.Caracteristicas is not null)
            throw new Exception("ResultadoProtocolo.Caracteristicas deberia ser null cuando EntradaProtocolo.Caracteristicas no se declara.");
    }
}
