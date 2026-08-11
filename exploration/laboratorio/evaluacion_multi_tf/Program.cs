using TD_Project.Application;
using TD_Project.Domain.Strategy;
using TD_Project.EvaluacionMultiTf;
using TD_Project.Exploration;

// Fase 2C: evaluacion de comportamiento de Tres Mosqueteros / MHI Mayoria sobre datos reales
// BTCUSDT en 6 timeframes (DISENO_FASE2C.md). No busca "estrategia ganadora" — busca describir
// como cambia el comportamiento de cada estrategia segun la escala temporal. No modifica
// src/, SPEC.md, ni la logica de las estrategias.
var timeframesIniciales = new[] { "1m", "5m", "15m", "1h", "4h", "1D" };
var nombreDataset = "BTCUSDT_2024-01-02_2025-01-02";

var raiz = AppContext.BaseDirectory;
var dirLaboratorio = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", ".."));
var dirDatasets = Path.Combine(dirLaboratorio, "datasets", "reales", "BTCUSDT");

var perfiles = new List<PerfilMultiTf>();

foreach (var tf in timeframesIniciales)
{
    var rutaCsv = Path.Combine(dirDatasets, tf, $"{nombreDataset}_{tf}.csv");
    var rutaMetadata = Path.Combine(dirDatasets, tf, "metadata.json");

    if (!File.Exists(rutaCsv))
    {
        Console.WriteLine($"ADVERTENCIA: dataset {tf} no encontrado en {rutaCsv} — se omite.");
        continue;
    }

    var crudas = LectorDerivado.Leer(rutaCsv);
    var metadata = LectorDerivado.LeerMetadata(rutaMetadata);
    var (velas, disponibles, utilizadas) = LectorDerivado.FiltrarParaBacktest(crudas);

    CorrerUna("Tres Mosqueteros", tf, velas, disponibles, utilizadas, metadata,
        onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp), perfiles);

    CorrerUna("MHI Mayoria", tf, velas, disponibles, utilizadas, metadata,
        onOp => new EstrategiaMhiMayoria(maxMartingalas: 2, onOperacionResuelta: onOp), perfiles);
}

ReportarCompletitud(perfiles);
ReportarMatriz(perfiles);
ReportarRachas(perfiles);
ReportarIntegridad(perfiles);
ReportarInterpretacion(perfiles);

static void CorrerUna(
    string nombreEstrategia, string timeframe, IReadOnlyList<TD_Project.Domain.Shared.Candle> velas,
    int disponibles, int utilizadas, MetadataDerivado metadata,
    Func<Action<InfoOperacionResuelta>, IStrategy> crearEstrategia, List<PerfilMultiTf> perfiles)
{
    var operaciones = new List<InfoOperacionResuelta>();
    var estrategia = crearEstrategia(operaciones.Add);
    var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velas);
    var resultado = BacktestRunner.Ejecutar(config, estrategia);

    var identidad = new IdentidadExperimento(
        Dataset: "BTCUSDT_2024-01-02_2025-01-02",
        Timeframe: timeframe,
        Estrategia: nombreEstrategia,
        CapitalInicial: 1000m,
        AggregationVersion: metadata.AggregationVersion,
        SourceSha256: metadata.SourceSha256,
        TimeframeSha256: metadata.Sha256,
        FechaEjecucionUtc: DateTime.UtcNow);

    perfiles.Add(PerfilMultiTf.Medir(identidad, resultado, operaciones, disponibles, utilizadas));
}

static void ReportarCompletitud(List<PerfilMultiTf> perfiles)
{
    Console.WriteLine("=== Fase 2C — Completitud del dataset por timeframe (Punto 1: velas parciales excluidas del backtest) ===");
    foreach (var p in perfiles.Where(p => p.Identidad.Estrategia == "Tres Mosqueteros")) // completitud es igual para ambas estrategias en el mismo timeframe
    {
        Console.WriteLine($"  {p.Identidad.Timeframe,-4}: disponibles={p.VelasDisponibles,7} utilizadas={p.VelasUtilizadas,7} excluidas={p.VelasExcluidas,3} ({p.PctUtilizado:F2}% utilizado)");
    }
}

static void ReportarMatriz(List<PerfilMultiTf> perfiles)
{
    Console.WriteLine("\n=== Matriz comparativa (DATOS — sin interpretacion) ===");
    Console.WriteLine($"{"Estrategia",-18}{"TF",-5}{"Retorno%",10}{"OpCompl",9}{"Ganadas",9}{"RachaMax",9}{"%Marting",9}{"MaxExp",8}{"AbiertaCierre",14}{"Reconcil",10}");
    foreach (var p in perfiles)
    {
        Console.WriteLine($"{p.Identidad.Estrategia,-18}{p.Identidad.Timeframe,-5}{p.RetornoPct,9:F2}%{p.OperacionesCompletadas,9}{p.OperacionesGanadas,9}{p.RachaNegativaMaxima,9}{p.PctResueltasPorMartingala,8:F1}%{p.MaxExposicion,8}{(p.OperacionAbiertaAlCierre ? "SI" : "no"),14}{(p.ReconciliacionCoherente ? "OK" : "FALLA"),10}");
    }
}

static void ReportarRachas(List<PerfilMultiTf> perfiles)
{
    Console.WriteLine("\n=== Distribucion de rachas negativas por longitud ===");
    foreach (var p in perfiles)
    {
        Console.WriteLine($"  {p.Identidad.Estrategia,-18}{p.Identidad.Timeframe,-5}: 2={p.Racha2} 3={p.Racha3} 4={p.Racha4} 5+={p.Racha5Mas} (max={p.RachaNegativaMaxima})");
    }
}

static void ReportarIntegridad(List<PerfilMultiTf> perfiles)
{
    Console.WriteLine("\n=== Integridad del motor ===");
    var fallas = perfiles.Where(p => !p.ReconciliacionCoherente || p.EstadoMotor != EstadoBacktest.Success || p.MaxExposicion > 1m).ToList();
    if (fallas.Count == 0)
    {
        Console.WriteLine("  Sin hallazgos: reconciliacion financiera OK, Estado=Success y exposicion maxima=1 en las 12 corridas.");
    }
    else
    {
        foreach (var f in fallas)
            Console.WriteLine($"  [BUG] {f.Identidad.Estrategia}/{f.Identidad.Timeframe}: Estado={f.EstadoMotor} Reconciliacion={f.ReconciliacionCoherente} MaxExposicion={f.MaxExposicion}");
    }

    Console.WriteLine("\n=== Operaciones abiertas al cierre del dataset (categoria separada, no ganada/perdida) ===");
    foreach (var p in perfiles.Where(p => p.OperacionAbiertaAlCierre))
        Console.WriteLine($"  {p.Identidad.Estrategia}/{p.Identidad.Timeframe}: capital comprometido={p.CapitalComprometidoAlCierre}");
    if (perfiles.All(p => !p.OperacionAbiertaAlCierre))
        Console.WriteLine("  Ninguna: las 12 corridas cerraron toda posicion antes del fin del dataset.");

    Console.WriteLine("\n=== Identidad de cada experimento (trazabilidad, precision D) ===");
    foreach (var p in perfiles)
    {
        var id = p.Identidad;
        Console.WriteLine($"  Dataset={id.Dataset} TF={id.Timeframe} Estrategia={id.Estrategia} Capital={id.CapitalInicial} " +
            $"AggVersion={id.AggregationVersion} SourceSha={id.SourceSha256[..12]}... TfSha={id.TimeframeSha256[..12]}... Fecha={id.FechaEjecucionUtc:O}");
    }
}

// Interpretacion cualitativa SEPARADA de los datos (precision E): explica el comportamiento
// observado, nunca declara "mejor"/"peor" entre estrategias o timeframes (precision B).
static void ReportarInterpretacion(List<PerfilMultiTf> perfiles)
{
    Console.WriteLine("\n=== Interpretacion (capa separada de los datos — no declara ganadores) ===");
    foreach (var p in perfiles)
    {
        var id = p.Identidad;
        var texto = Interpretar(id.Estrategia, id.Timeframe, p);
        if (texto is not null)
            Console.WriteLine($"  [{id.Estrategia}/{id.Timeframe}] {texto}");
    }
}

static string? Interpretar(string estrategia, string timeframe, PerfilMultiTf p)
{
    if (p.OperacionesCompletadas == 0)
        return "Cero operaciones completadas en el rango: el cuadrante de 5 velas de este timeframe no genero ninguna senal de color/mayoria valida, o el dataset filtrado (tras excluir parciales) no alcanzo a completar un cuadrante.";

    var origenGanancia = p.PctResueltasPorMartingala >= 40m
        ? "una porcion relevante de las operaciones ganadoras dependio de recuperar via M1/M2, no de acertar en el intento inicial"
        : "la mayoria de las operaciones ganadoras se resolvieron en el intento inicial, sin depender de martingala";

    var riesgoRacha = p.RachaNegativaMaxima >= 4
        ? $", con una racha negativa maxima de {p.RachaNegativaMaxima} operaciones completas consecutivas"
        : "";

    return $"Retorno={p.RetornoPct:F2}% sobre {p.OperacionesCompletadas} operaciones completadas; {origenGanancia}{riesgoRacha}.";
}
