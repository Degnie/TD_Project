using TD_Project.Domain.Shared;
using TD_Project.EvaluacionMultiTf;
using TD_Project.Exploration;
using TD_Project.ModeloFinanciero;
using TD_Project.Protocolo;

// spec: Caso 2 D-081 — baseline financiero, ejecutable dedicado. Misma estrategia/dataset/
// timeframes que baseline_final/ (Caso 1), configuracion financiera explicita (D-079), sin tocar
// protocolo/Program.cs (que queda asociado exclusivamente al baseline de Caso 1).

var raiz = AppContext.BaseDirectory;
var dirLaboratorio = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", ".."));
var dirDatasets = Path.Combine(dirLaboratorio, "datasets", "reales", "BTCUSDT");
var dirModeloFinanciero = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", ".."));
var dirSalida = Path.Combine(dirModeloFinanciero, "baseline_financiero_final");

var instrumento = new Instrumento(Simbolo: "BTCUSDT", TasaMargen: 0.1m);
var costes = new ConfiguracionCostes(TasaComision: 0.001m, TasaSlippage: 0.001m);
// spec: Caso 2 D-084 — Sizing=null en el baseline congelado. GestorCapital (D-066-D-071) recalcula
// Cantidad en TODA OrderRequest, incluidas las de cierre, sin distinguir intencion (apertura vs.
// cierre) — produce residuos de lotes que crecen sin limite en corridas largas (1m, ~82k
// operaciones). Deuda tecnica registrada en DEUDA_TECNICA_CASO2_V1.md/DECISIONES_MODELO_
// ECONOMICO_V1.md D-084, resolucion fuera de Caso 2 V1. Instrumento/Costes si quedan activos.
ConfiguracionSizing? sizing = null;

var entrada = new EntradaProtocolo(
    Estrategia: "Tres Mosqueteros",
    VersionEstrategia: "1.0",
    Parametros: new[] { "maxMartingalas=2" },
    CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
    Timeframes: new[] { "1m", "1D" },
    DirDatasets: dirDatasets,
    NombreDataset: "BTCUSDT_2024-01-02_2025-01-02",
    CapitalInicial: 1000m,
    Instrumento: instrumento,
    Costes: costes,
    Sizing: sizing);

Console.WriteLine("=== Caso 2 — Baseline financiero (D-081) ===");

var corridaA = EjecutorProtocolo.Ejecutar(entrada);
var corridaB = EjecutorProtocolo.Ejecutar(entrada);

var repetible = corridaA.Identidad.HashCompuesto == corridaB.Identidad.HashCompuesto
    && corridaA.Identidad.HashConfiguracionEconomica == corridaB.Identidad.HashConfiguracionEconomica;
Console.WriteLine($"HashCompuesto (corrida A): {corridaA.Identidad.HashCompuesto}");
Console.WriteLine($"HashCompuesto (corrida B): {corridaB.Identidad.HashCompuesto}");
Console.WriteLine($"HashConfiguracionEconomica (corrida A): {corridaA.Identidad.HashConfiguracionEconomica}");
Console.WriteLine($"HashConfiguracionEconomica (corrida B): {corridaB.Identidad.HashConfiguracionEconomica}");
Console.WriteLine($"Repetibilidad: {(repetible ? "IDENTICO" : "DIVERGENTE")}");
foreach (var c in corridaA.Corridas)
    Console.WriteLine($"  {c.Timeframe}: {c.Estado}{(c.MotivoFallo is not null ? $" — {c.MotivoFallo}" : "")}");

if (!repetible)
{
    Console.WriteLine("ERROR: el baseline financiero no es reproducible — no se escriben artefactos.");
    Environment.Exit(1);
}

Directory.CreateDirectory(dirSalida);
Directory.CreateDirectory(Path.Combine(dirSalida, "ANEXOS"));

File.WriteAllText(Path.Combine(dirSalida, "REPORTE_FINANCIERO_V1.md"), ReporteFinancieroGenerador.Generar(corridaA, entrada));

foreach (var c in corridaA.Corridas.Where(c => c.Estado == EstadoCorridaTimeframe.Success && c.Anexo is not null))
    File.WriteAllText(Path.Combine(dirSalida, "ANEXOS", $"REPORTE_FINANCIERO_V1_ANEXO_{c.Timeframe}.md"), c.Anexo);

var identidadJson = $$"""
    {
      "estrategia": "{{corridaA.Identidad.Estrategia}}",
      "versionEstrategia": "{{corridaA.Identidad.VersionEstrategia}}",
      "parametros": [{{string.Join(", ", corridaA.Identidad.Parametros.Select(p => $"\"{p}\""))}}],
      "datasetSourceSha256": "{{corridaA.Identidad.DatasetSourceSha256}}",
      "clasificadorRegimenVersion": "{{corridaA.Identidad.ClasificadorRegimenVersion}}",
      "versionProtocolo": "{{corridaA.Identidad.VersionProtocolo}}",
      "hashCompuesto": "{{corridaA.Identidad.HashCompuesto}}",
      "hashConfiguracionEconomica": "{{corridaA.Identidad.HashConfiguracionEconomica}}",
      "instrumento": { "simbolo": "{{instrumento.Simbolo}}", "tasaMargen": {{instrumento.TasaMargen}} },
      "costes": { "tasaComision": {{costes.TasaComision}}, "tasaSlippage": {{costes.TasaSlippage}} },
      "sizing": {{(sizing is null ? "null" : $"{{ \"porcentajeRiesgo\": {sizing.PorcentajeRiesgo} }}")}}
    }
    """;
File.WriteAllText(Path.Combine(dirSalida, "IDENTIDAD_EXPERIMENTAL.json"), identidadJson);

Console.WriteLine($"Artefactos escritos en: {dirSalida}");
