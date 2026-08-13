using System.Text.Json;
using TD_Project.Agregador;
using TD_Project.DatosReales;

// Etapa 1 (fixtures, sin dataset real): siempre corre primero.
var resultados = FixturesAgregador.EjecutarTodos();

Console.WriteLine("=== Fase 2B — Fixtures del agregador multi-timeframe (sin dataset real) ===");
foreach (var r in resultados)
    Console.WriteLine($"  {(r.Paso ? "OK" : "FALLA")}: {r.Nombre} — {r.Detalle}");

var pasaronFixtures = resultados.Count(r => r.Paso);
Console.WriteLine($"\n=== Resumen fixtures: {pasaronFixtures}/{resultados.Count} OK ===");

if (pasaronFixtures != resultados.Count)
{
    Console.WriteLine("Fixtures del agregador fallando — no se procede sobre el dataset real.");
    Environment.Exit(1);
}

// spec: ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md §2 — Via B de D-121.
// rangoDataset generalizado a variable (antes hardcodeado 3 veces en este archivo) para poder
// reutilizar el mismo pipeline con un segundo periodo, sin convertir esto en un parametro de CLI
// de proposito general. Carpeta de origen ("1m" o "1m_2023") y sufijo de carpeta destino
// ("" o "_2023") derivan de la misma variable para no duplicar el punto de configuracion.
var rangoDataset = Environment.GetEnvironmentVariable("RANGO_DATASET") ?? "2024-01-02_2025-01-02";
var sufijoCarpeta = rangoDataset == "2024-01-02_2025-01-02" ? "" : "_" + rangoDataset[..4];

var raiz = AppContext.BaseDirectory;
var dirLaboratorio = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", ".."));
var dirBase1m = Path.Combine(dirLaboratorio, "datasets", "reales", "BTCUSDT", "1m" + sufijoCarpeta);
var rutaCsv1m = Path.Combine(dirBase1m, $"BTCUSDT_{rangoDataset}_1m.csv");
var rutaMetadata1m = Path.Combine(dirBase1m, "metadata.json");

if (!File.Exists(rutaCsv1m))
{
    Console.WriteLine($"\nDataset base 1m no encontrado en {rutaCsv1m} — fin de la corrida (solo fixtures).");
    return;
}

using var metadataDoc = JsonDocument.Parse(File.ReadAllText(rutaMetadata1m));
var sourceSha256 = metadataDoc.RootElement.GetProperty("sha256").GetString()!;
var nombreArchivo1m = Path.GetFileName(rutaCsv1m);

var velas1m = LectorCsv.Leer(rutaCsv1m);
Console.WriteLine($"\nDataset base cargado: {velas1m.Count} velas 1m (sha256={sourceSha256[..12]}...)");

// Etapa 2: primera ejecucion controlada, solo 5m, con verificacion manual de un tramo conocido
// (primeras 60 velas 1m -> deben coincidir con la primera vela 1h agregada directamente).
var derivado5m = AgregadorMultiTimeframe.Agregar(velas1m, Timeframe.M5);
var velasEsperadas5m = (int)Math.Ceiling(velas1m.Count / 5.0);

Console.WriteLine("\n=== Primera ejecucion controlada: 1m -> 5m ===");
Console.WriteLine($"  Velas 5m generadas: {derivado5m.Count} (esperado aprox: {velasEsperadas5m})");
Console.WriteLine($"  Parciales: {derivado5m.Count(v => v.EsParcial)} (deberian concentrarse en los bordes del rango)");
Console.WriteLine($"  Completas: {derivado5m.Count(v => !v.EsParcial)}");

VerificarTramoManual(velas1m);

var conteoOk = derivado5m.Count > 0 && Math.Abs(derivado5m.Count - velasEsperadas5m) <= 1;
if (!conteoOk)
{
    Console.WriteLine("\nFALLA: conteo de velas 5m fuera del rango esperado — no se congela, no se generaliza a los demas timeframes.");
    Environment.Exit(1);
}

var dirDerivado5m = Path.Combine(dirLaboratorio, "datasets", "reales", "BTCUSDT", "5m" + sufijoCarpeta);
Directory.CreateDirectory(dirDerivado5m);
var rutaCsv5m = Path.Combine(dirDerivado5m, $"BTCUSDT_{rangoDataset}_5m.csv");
var rutaMetadata5m = Path.Combine(dirDerivado5m, "metadata.json");

var sha5m = EscritorDerivado.EscribirCsv(rutaCsv5m, derivado5m);
EscritorDerivado.EscribirMetadata(rutaMetadata5m, nombreArchivo1m, sourceSha256, "1m", "5m", sha5m, derivado5m);

Console.WriteLine($"\n5m congelado: {rutaCsv5m}");
Console.WriteLine($"Metadata: {rutaMetadata5m}");
Console.WriteLine("\n5m validado y congelado. Generalizacion a los 12 timeframes: opt-in via GENERAR_TODOS_TIMEFRAMES=1 (no automatico).");

// Etapa 3: generalizacion, solo si 5m paso y el usuario lo pide explicitamente.
if (Environment.GetEnvironmentVariable("GENERAR_TODOS_TIMEFRAMES") != "1")
{
    Console.WriteLine("\nGENERAR_TODOS_TIMEFRAMES no establecido — fin de la corrida (solo 5m generado).");
    return;
}

Console.WriteLine("\n=== Generalizacion a los 12 timeframes ===");
foreach (var tf in Enum.GetValues<Timeframe>())
{
    var derivado = tf == Timeframe.M5 ? derivado5m : AgregadorMultiTimeframe.Agregar(velas1m, tf);
    var etiqueta = tf.Etiqueta();
    var dirDerivado = Path.Combine(dirLaboratorio, "datasets", "reales", "BTCUSDT", etiqueta + sufijoCarpeta);
    Directory.CreateDirectory(dirDerivado);
    var rutaCsv = Path.Combine(dirDerivado, $"BTCUSDT_{rangoDataset}_{etiqueta}.csv");
    var rutaMetadata = Path.Combine(dirDerivado, "metadata.json");

    var sha = EscritorDerivado.EscribirCsv(rutaCsv, derivado);
    EscritorDerivado.EscribirMetadata(rutaMetadata, nombreArchivo1m, sourceSha256, "1m", etiqueta, sha, derivado);

    Console.WriteLine($"  {etiqueta,-4}: {derivado.Count,7} velas ({derivado.Count(v => v.EsParcial)} parciales) — {rutaCsv}");
}

Console.WriteLine("\n=== Fase 2B: 12 timeframes generados y congelados ===");

// Verificacion manual explicita del tramo pedido: primeras 60 velas 1m -> comparar contra la
// primera vela 1h generada por el agregador directamente (1m -> 1h, sin pasar por 5m).
static void VerificarTramoManual(IReadOnlyList<VelaCruda> velas1m)
{
    var primeras60 = velas1m.Take(60).ToList();
    var manual = (
        Open: primeras60[0].Open,
        High: primeras60.Max(v => v.High),
        Low: primeras60.Min(v => v.Low),
        Close: primeras60[^1].Close,
        Volume: primeras60.Sum(v => v.Volume));

    var agregado1h = AgregadorMultiTimeframe.Agregar(velas1m, Timeframe.H1);
    var primeraHora = agregado1h.FirstOrDefault();

    var coincide = primeraHora is not null
        && primeraHora.Open == manual.Open && primeraHora.High == manual.High
        && primeraHora.Low == manual.Low && primeraHora.Close == manual.Close
        && primeraHora.Volume == manual.Volume;

    Console.WriteLine("\n=== Verificacion manual: primeras 60 velas 1m vs. primera vela 1h agregada ===");
    Console.WriteLine($"  Manual:   Open={manual.Open} High={manual.High} Low={manual.Low} Close={manual.Close} Volume={manual.Volume}");
    Console.WriteLine($"  Agregado: Open={primeraHora?.Open} High={primeraHora?.High} Low={primeraHora?.Low} Close={primeraHora?.Close} Volume={primeraHora?.Volume}");
    Console.WriteLine($"  Coincide: {coincide}");

    if (!coincide)
    {
        Console.WriteLine("FALLA CRITICA: el agregador no reproduce el calculo manual sobre datos reales.");
        Environment.Exit(1);
    }
}
