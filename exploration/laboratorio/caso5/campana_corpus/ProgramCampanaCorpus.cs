using TD_Project.Caso5;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Exploration;
using TD_Project.Protocolo;

// spec: PROPUESTA_CAMPANA_CORPUS_CASO5C_V1.md, ESPECIFICACION_IMPLEMENTACION_CAMPANA_CORPUS_
// CASO5C_V1.md — ejecutable de campana separado (no extiende caso5/Program.cs). Genera evidencia
// usando exclusivamente ComparadorGestores (Caso 5B) + PersistidorComparaciones (Caso 5C Capa 1),
// sin modificar ninguno de los dos. No analiza, no rankea, no selecciona.

// spec: §3 — matriz declarada por completo antes de ejecutar. No se agrega, quita, ni reordena
// ninguna combinacion en funcion de un resultado intermedio.
string[] timeframes = { "15m", "1h", "1D" };

(string Nombre, Func<Action<InfoOperacionResuelta>?, IStrategy> Crear)[] estrategias =
{
    ("Tres Mosqueteros", onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp)),
    ("Ema Cross", onOp => new EstrategiaEmaCross(periodoEmaCorta: 5, periodoEmaLarga: 20, onOperacionResuelta: onOp)),
};

IReadOnlyList<IGestorRiesgo> Gestores() => new IGestorRiesgo[]
{
    new GestorFixedFractional(0.1m),
    new GestorFixedRisk(50m),
    new GestorVolatilitySizing(20, 0.1m, 2m),
};

var raiz = AppContext.BaseDirectory;
var dirDatasets = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "..", "datasets", "reales", "BTCUSDT"));
var dirResultados = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "resultados"));
var instrumento = new Instrumento("BTCUSDT", 0.1m);
var costes = new ConfiguracionCostes(0.001m, 0.001m);

// P1/P2 — verificacion estructural previa. Si falla, la campana no ejecuta ninguna corrida real.
var (totalPrevio, pasaronPrevio, detallesPrevio) = TestsCampanaCorpus.VerificarEstructura(estrategias.Length, timeframes.Length, Gestores().Count);
Console.WriteLine("=== Verificacion estructural previa (P1/P2) ===");
foreach (var d in detallesPrevio)
    Console.WriteLine($"  {d}");
if (pasaronPrevio != totalPrevio)
{
    Console.WriteLine("Verificacion estructural fallida — campana no ejecutada.");
    Environment.Exit(1);
}
Console.WriteLine();

Console.WriteLine("=== Campana de generacion de corpus comparativo — Caso 5C ===");
var carpetasEscritas = new List<string>();

foreach (var estrategia in estrategias)
{
    foreach (var timeframe in timeframes)
    {
        var entradaBase = new EntradaProtocolo(
            Estrategia: estrategia.Nombre,
            VersionEstrategia: "1.0",
            Parametros: estrategia.Nombre == "Tres Mosqueteros" ? new[] { "maxMartingalas=2" } : new[] { "rapida=5", "lenta=20" },
            CrearEstrategia: estrategia.Crear,
            Timeframes: new[] { timeframe },
            DirDatasets: dirDatasets,
            NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
            CapitalInicial: 1000m,
            Instrumento: instrumento,
            Costes: costes);

        var resultado = ComparadorGestores.Comparar(entradaBase, Gestores());
        var carpeta = PersistidorComparaciones.Persistir(dirResultados, resultado);
        carpetasEscritas.Add(carpeta);
        Console.WriteLine($"  {estrategia.Nombre} / {timeframe} -> {carpeta}");
    }
}

Console.WriteLine();
Console.WriteLine($"Comparaciones generadas: {carpetasEscritas.Count} (esperado: {estrategias.Length * timeframes.Length}).");

// P3 — cobertura de campana: 1 carpeta persistida por comparacion (no por gestor — cada
// comparacion ya agrupa los 3 gestores en sus Filas).
if (carpetasEscritas.Count != estrategias.Length * timeframes.Length)
{
    Console.WriteLine("P3 FALLA — cantidad de comparaciones persistidas no coincide con la matriz declarada.");
    Environment.Exit(1);
}
if (carpetasEscritas.Distinct().Count() != carpetasEscritas.Count || carpetasEscritas.Any(c => !Directory.Exists(c)))
{
    Console.WriteLine("P3 FALLA — alguna carpeta de comparacion no existe o esta duplicada.");
    Environment.Exit(1);
}
Console.WriteLine("P3 — cobertura de campana verificada: cada comparacion declarada tiene su carpeta persistida.");
