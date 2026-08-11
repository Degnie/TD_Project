using TD_Project.EvaluacionMultiTf;
using TD_Project.Exploration;
using TD_Project.Protocolo;

var raiz = AppContext.BaseDirectory;
var dirLaboratorio = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", ".."));
var dirDatasets = Path.Combine(dirLaboratorio, "datasets", "reales", "BTCUSDT");
var dirResultados = Path.Combine(dirLaboratorio, "protocolo", "resultados");

var entrada = new EntradaProtocolo(
    Estrategia: "Tres Mosqueteros",
    VersionEstrategia: "1.0",
    Parametros: new[] { "maxMartingalas=2" },
    CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
    Timeframes: new[] { "1m", "1D" },
    DirDatasets: dirDatasets,
    NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
    CapitalInicial: 1000m);

Console.WriteLine("=== Fase 1.6-C — Ejecución del protocolo experimental ===");
var resultado = EjecutorProtocolo.Ejecutar(entrada);
EscribirArtefactos(dirResultados, resultado);

Console.WriteLine($"Identidad experimental: {resultado.Identidad.HashCompuesto}");
foreach (var c in resultado.Corridas)
    Console.WriteLine($"  {c.Timeframe}: {c.Estado}{(c.MotivoFallo is not null ? $" — {c.MotivoFallo}" : "")}");
Console.WriteLine();

var totalGeneral = 0;
var pasaronGeneral = 0;

void CorrerSuite(string titulo, (int Total, int Pasaron, IReadOnlyList<string> Detalles) r)
{
    Console.WriteLine($"=== {titulo} ===");
    foreach (var d in r.Detalles)
        Console.WriteLine($"  {d}");
    Console.WriteLine($"{r.Pasaron}/{r.Total} pruebas pasaron.\n");
    totalGeneral += r.Total;
    pasaronGeneral += r.Pasaron;
}

CorrerSuite("Fase 1.6-C — EjecutorProtocolo", TestsEjecutorProtocolo.EjecutarTodos(dirDatasets, dirResultados));

Console.WriteLine($"TOTAL: {pasaronGeneral}/{totalGeneral} pruebas pasaron.");
if (pasaronGeneral != totalGeneral)
    Environment.Exit(1);

static void EscribirArtefactos(string dirResultados, ResultadoProtocolo resultado)
{
    var carpeta = Path.Combine(dirResultados, $"{resultado.Estrategia.Replace(" ", "")}_{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
    Directory.CreateDirectory(carpeta);

    File.WriteAllText(Path.Combine(carpeta, "REPORTE_EXPERIMENTAL_ESTRATEGIA_V1.md"), ReporteConsolidadoGenerador.Generar(resultado));

    foreach (var c in resultado.Corridas.Where(c => c.Estado == EstadoCorridaTimeframe.Success && c.Anexo is not null))
        File.WriteAllText(Path.Combine(carpeta, $"REPORTE_EXPERIMENTAL_ESTRATEGIA_V1_ANEXO_{c.Timeframe}.md"), c.Anexo);

    var identidadJson = $$"""
        {
          "estrategia": "{{resultado.Identidad.Estrategia}}",
          "versionEstrategia": "{{resultado.Identidad.VersionEstrategia}}",
          "parametros": [{{string.Join(", ", resultado.Identidad.Parametros.Select(p => $"\"{p}\""))}}],
          "datasetSourceSha256": "{{resultado.Identidad.DatasetSourceSha256}}",
          "clasificadorRegimenVersion": "{{resultado.Identidad.ClasificadorRegimenVersion}}",
          "versionProtocolo": "{{resultado.Identidad.VersionProtocolo}}",
          "hashCompuesto": "{{resultado.Identidad.HashCompuesto}}"
        }
        """;
    File.WriteAllText(Path.Combine(carpeta, "IDENTIDAD_EXPERIMENTAL.json"), identidadJson);

    Console.WriteLine($"Artefactos escritos en: {carpeta}");
}
