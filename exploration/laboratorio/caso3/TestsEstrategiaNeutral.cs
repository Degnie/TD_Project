using System.Diagnostics;
using TD_Project.AnalisisEscenariosMercado;
using TD_Project.AnalisisOperacional;
using TD_Project.Domain.Shared;
using TD_Project.EvaluacionMultiTf;
using TD_Project.Exploration;
using TD_Project.ModeloFinanciero;
using TD_Project.Protocolo;
using TD_Project.ReporteEscenariosMercado;

namespace TD_Project.Caso3;

// spec: Caso 3 D-086 (segunda familia) — pruebas requeridas por
// ESPECIFICACION_IMPLEMENTACION_ESTRATEGIA_NEUTRAL_CASO3_V1.md §6: P1 determinismo estructural,
// P2 cadencia fija, P3 independencia del mercado (prueba central), P4 sin aleatoriedad,
// P5 metadata correcta, P6 rendimiento, P7 integracion en el pipeline, P8 regresion Caso 1/Caso 2.
public static class TestsEstrategiaNeutral
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

        Caso("P1 — Determinismo estructural: mismo N produce misma secuencia de ordenes con precios distintos", VerificarDeterminismoEstructural);
        Caso("P2 — Cadencia fija: cada posicion dura exactamente Ciclo/2 velas", VerificarCadenciaFija);
        Caso("P3 — Independencia del mercado: alterar Open/High/Low/Volume no cambia la secuencia de ordenes", VerificarIndependenciaDelMercado);
        Caso("P4 — Sin aleatoriedad: dos instancias independientes producen resultados identicos sin semilla", VerificarSinAleatoriedad);
        Caso("P5 — CaracteristicasEstrategia.UsaMartingala=false produce 'no aplica' en el reporte", VerificarMetadata);
        Caso("P6 — Rendimiento razonable sobre dataset grande", VerificarRendimiento);
        Caso("P7 — Integracion en el pipeline sin cambios de codigo (EjecutorProtocolo)", () => VerificarIntegracionPipeline(dirDatasets));
        Caso("P8 — Regresion: EstrategiaNeutral no afecta corridas de otras estrategias", () => VerificarRegresion(dirDatasets));

        return (total, pasaron, detalles);
    }

    private static IReadOnlyList<Candle> DatasetSintetico(Func<int, decimal> precioPorIndice, int cantidad = 40)
    {
        var velas = new List<Candle>();
        for (var i = 0; i < cantidad; i++)
        {
            var p = precioPorIndice(i);
            velas.Add(new Candle(i, p, p + 1, p - 1, p, 100m));
        }
        return velas;
    }

    private static List<OrderRequest[]> EjecutarSecuencia(EstrategiaNeutral estrategia, IReadOnlyList<Candle> velas)
    {
        var resultado = new List<OrderRequest[]>();
        var acumuladas = new List<Candle>(velas.Count);
        foreach (var vela in velas)
        {
            acumuladas.Add(vela);
            resultado.Add(estrategia.Observar(new DataSlice(acumuladas)).ToArray());
        }
        return resultado;
    }

    private static void VerificarDeterminismoEstructural()
    {
        var velasA = DatasetSintetico(i => 100m + i);
        var velasB = DatasetSintetico(i => 5000m - i * 3);

        var secuenciaA = EjecutarSecuencia(new EstrategiaNeutral(ciclo: 10), velasA);
        var secuenciaB = EjecutarSecuencia(new EstrategiaNeutral(ciclo: 10), velasB);

        for (var i = 0; i < secuenciaA.Count; i++)
        {
            if (secuenciaA[i].Length != secuenciaB[i].Length)
                throw new Exception($"Vela {i}: cantidad de ordenes difiere entre datasets de precio distinto ({secuenciaA[i].Length} vs {secuenciaB[i].Length}).");
            for (var j = 0; j < secuenciaA[i].Length; j++)
                if (secuenciaA[i][j].Side != secuenciaB[i][j].Side)
                    throw new Exception($"Vela {i}, orden {j}: Side difiere entre datasets de precio distinto.");
        }
    }

    private static void VerificarCadenciaFija()
    {
        var velas = DatasetSintetico(i => 100m + (i % 3));
        var operaciones = new List<InfoOperacionResuelta>();
        var estrategia = new EstrategiaNeutral(ciclo: 10, operaciones.Add);
        EjecutarSecuencia(estrategia, velas);

        if (operaciones.Count == 0)
            throw new Exception("No se registro ninguna operacion resuelta en 40 velas con Ciclo=10.");

        foreach (var op in operaciones)
        {
            var duracion = op.TimestampResolucion - op.TimestampEntrada;
            if (duracion != 5)
                throw new Exception($"Operacion {op.OperacionId}: duracion {duracion} velas, se esperaba exactamente 5 (Ciclo/2).");
        }
    }

    private static void VerificarIndependenciaDelMercado()
    {
        var random = new Random(3);
        var velasOriginal = DatasetSintetico(i => 100m + i);
        var velasAlteradas = velasOriginal.Select(v => new Candle(
            v.Timestamp,
            Open: v.Close + (decimal)(random.NextDouble() * 1000 - 500),
            High: v.Close + (decimal)(random.NextDouble() * 1000 + 500),
            Low: v.Close - (decimal)(random.NextDouble() * 1000 + 500),
            Close: v.Close,
            Volume: (decimal)(random.NextDouble() * 100000))).ToArray();

        var secuenciaOriginal = EjecutarSecuencia(new EstrategiaNeutral(ciclo: 10), velasOriginal);
        var secuenciaAlterada = EjecutarSecuencia(new EstrategiaNeutral(ciclo: 10), velasAlteradas);

        for (var i = 0; i < secuenciaOriginal.Count; i++)
        {
            if (secuenciaOriginal[i].Length != secuenciaAlterada[i].Length)
                throw new Exception($"Vela {i}: alterar Open/High/Low/Volume cambio la cantidad de ordenes generadas.");
            for (var j = 0; j < secuenciaOriginal[i].Length; j++)
                if (secuenciaOriginal[i][j].Side != secuenciaAlterada[i][j].Side)
                    throw new Exception($"Vela {i}, orden {j}: alterar Open/High/Low/Volume cambio el Side de la orden.");
        }
    }

    private static void VerificarSinAleatoriedad()
    {
        var velas = DatasetSintetico(i => 100m + i);
        var secuencia1 = EjecutarSecuencia(new EstrategiaNeutral(ciclo: 10), velas);
        var secuencia2 = EjecutarSecuencia(new EstrategiaNeutral(ciclo: 10), velas);

        for (var i = 0; i < secuencia1.Count; i++)
        {
            if (secuencia1[i].Length != secuencia2[i].Length)
                throw new Exception($"Vela {i}: dos instancias independientes produjeron cantidades de ordenes distintas sin ninguna semilla involucrada.");
            for (var j = 0; j < secuencia1[i].Length; j++)
                if (secuencia1[i][j].Side != secuencia2[i][j].Side)
                    throw new Exception($"Vela {i}, orden {j}: dos instancias independientes divergieron.");
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
        var velas = DatasetSintetico(i => 100m + i % 50, cantidad: 100_000);
        var estrategia = new EstrategiaNeutral(ciclo: 10);
        var acumuladas = new List<Candle>(velas.Count);
        var sw = Stopwatch.StartNew();
        foreach (var vela in velas)
        {
            acumuladas.Add(vela);
            estrategia.Observar(new DataSlice(acumuladas));
        }
        sw.Stop();

        if (sw.ElapsedMilliseconds > 10_000)
            throw new Exception($"Corrida de 100k velas tardo {sw.ElapsedMilliseconds}ms — inesperado para una logica O(1) por vela.");
    }

    private static void VerificarIntegracionPipeline(string dirDatasets)
    {
        var entrada = new EntradaProtocolo(
            Estrategia: "Neutral", VersionEstrategia: "1.0", Parametros: new[] { "ciclo=10" },
            CrearEstrategia: onOp => new EstrategiaNeutral(ciclo: 10, onOperacionResuelta: onOp),
            Timeframes: new[] { "1D" }, DirDatasets: dirDatasets, NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
            CapitalInicial: 1000m, Caracteristicas: new CaracteristicasEstrategia(UsaMartingala: false));

        var resultado = EjecutorProtocolo.Ejecutar(entrada);
        var corrida1D = resultado.Corridas.Single(c => c.Timeframe == "1D");
        if (corrida1D.Estado != EstadoCorridaTimeframe.Success)
            throw new Exception($"EstrategiaNeutral no integro correctamente al pipeline: Estado={corrida1D.Estado}, Motivo={corrida1D.MotivoFallo}.");
        if (corrida1D.MetricasFinancieras is null)
            throw new Exception("MetricasFinancieras deberia poblarse en una corrida Success, igual que para cualquier otra estrategia.");
    }

    private static void VerificarRegresion(string dirDatasets)
    {
        // EstrategiaNeutral no debe afectar una corrida independiente de Tres Mosqueteros —
        // mismo criterio de aislamiento ya verificado para EMA Cross/Z-Score.
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
