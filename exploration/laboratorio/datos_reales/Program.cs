using TD_Project.DatosReales;

// Etapa 1 (fixtures, sin red): siempre corre primero — es la puerta de entrada antes de
// cualquier llamada real a Binance.
var (total, pasaron, detalles) = FixturesValidador.EjecutarTodos();
var (totalExploracion, pasaronExploracion, detallesExploracion) = FixturesExploradorDisponibilidad.EjecutarTodos();

Console.WriteLine("=== Fase 2A — Fixtures del validador de integridad (sin red) ===");
foreach (var detalle in detalles)
    Console.WriteLine($"  {detalle}");
Console.WriteLine($"\n=== Resumen fixtures: {pasaron}/{total} OK ===");

Console.WriteLine("\n=== Caso 5C D-122 — Fixtures del explorador de disponibilidad (sin red) ===");
foreach (var detalle in detallesExploracion)
    Console.WriteLine($"  {detalle}");
Console.WriteLine($"\n=== Resumen fixtures exploracion: {pasaronExploracion}/{totalExploracion} OK ===");

if (pasaron != total || pasaronExploracion != totalExploracion)
{
    Console.WriteLine("Fixtures fallando — no se procede con ninguna operacion de red.");
    Environment.Exit(1);
}

const string symbol = "BTCUSDT";
const string interval = "1m";

// Etapa 3 (exploracion, red real, opt-in separado de DESCARGAR_BINANCE): antes de una descarga
// completa de anio, permite verificar continuidad por mes de un candidato. Nunca se combina con
// DESCARGAR_BINANCE en la misma invocacion — evita confundir exploracion con descarga real.
// spec: Caso 5C D-122 (DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md, Opcion B).
// symbolExploracion es independiente de `symbol` (que sigue fijando BTCUSDT para la descarga real,
// Etapa 4) — spec: ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md §1/§2 (D-125):
// permite explorar un instrumento candidato distinto sin tocar el flujo de descarga de BTCUSDT.
var symbolExploracion = Environment.GetEnvironmentVariable("EXPLORAR_DISPONIBILIDAD_SYMBOL") ?? symbol;
var explorarAnio = Environment.GetEnvironmentVariable("EXPLORAR_DISPONIBILIDAD_ANIO");
if (explorarAnio is not null)
{
    if (!int.TryParse(explorarAnio, out var anio))
    {
        Console.WriteLine($"\nEXPLORAR_DISPONIBILIDAD_ANIO='{explorarAnio}' no es un anio valido.");
        Environment.Exit(1);
        return;
    }

    var inicioAnio = new DateTimeOffset(anio, 1, 1, 0, 0, 0, TimeSpan.Zero);
    var finAnioExploracion = inicioAnio.AddYears(1);
    var resultadoExploracion = await ExploradorDisponibilidad.ExplorarAsync(new BinanceClient(), symbolExploracion, interval, inicioAnio, finAnioExploracion);

    Console.WriteLine($"\n=== Exploracion de disponibilidad: {symbolExploracion} {interval} {anio} ===");
    foreach (var b in resultadoExploracion.Bloques)
        Console.WriteLine($"  {b.InicioUtc:yyyy-MM} .. {b.FinUtc:yyyy-MM}: {(b.Continuo ? "OK" : $"HUECO ({b.Huecos} huecos, {b.MinutosFaltantes} min)")}");
    Console.WriteLine($"\nCandidato {symbolExploracion} {anio}: {(resultadoExploracion.TodosContinuos ? "VIABLE — continuidad OK en los 12 meses" : "DESCARTADO — al menos 1 mes con discontinuidad")}");
    return;
}

// Etapa 3b (exploracion de rango exacto, no anio calendario): D-121/D-125 exigen el rango
// 2024-01-02..2025-01-02 (el ya congelado para BTCUSDT), no el anio calendario 2024 completo.
// ExploradorDisponibilidad.ExplorarAsync ya acepta cualquier DateTimeOffset de inicio/fin — no
// requiere generalizacion, solo una invocacion con las fechas exactas en vez de 1-enero..1-enero.
var explorarRango = Environment.GetEnvironmentVariable("EXPLORAR_DISPONIBILIDAD_RANGO_2024_2025");
if (explorarRango is not null)
{
    var inicioRango = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);
    var finRango = new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero);
    var resultadoExploracionRango = await ExploradorDisponibilidad.ExplorarAsync(new BinanceClient(), symbolExploracion, interval, inicioRango, finRango);

    Console.WriteLine($"\n=== Exploracion de disponibilidad: {symbolExploracion} {interval} [{inicioRango:yyyy-MM-dd} .. {finRango:yyyy-MM-dd}) ===");
    foreach (var b in resultadoExploracionRango.Bloques)
        Console.WriteLine($"  {b.InicioUtc:yyyy-MM-dd} .. {b.FinUtc:yyyy-MM-dd}: {(b.Continuo ? "OK" : $"HUECO ({b.Huecos} huecos, {b.MinutosFaltantes} min)")}");
    Console.WriteLine($"\nCandidato {symbolExploracion} [{inicioRango:yyyy-MM-dd}..{finRango:yyyy-MM-dd}): {(resultadoExploracionRango.TodosContinuos ? "VIABLE — continuidad OK en todos los bloques" : "DESCARTADO — al menos 1 bloque con discontinuidad")}");
    return;
}

// Etapa 4 (red real, descarga completa): opt-in explicito via variable de entorno, para que
// "dotnet run" sin mas nunca dispare una descarga contra Binance por accidente.
var descargar = Environment.GetEnvironmentVariable("DESCARGAR_BINANCE");
if (descargar is not ("DIA" or "ANIO"))
{
    Console.WriteLine("\nDESCARGAR_BINANCE no establecido (valores: DIA | ANIO) — fin de la corrida.");
    return;
}

var raiz = AppContext.BaseDirectory;
var directorioDatosReales = Path.GetFullPath(Path.Combine(raiz, "..", "..", ".."));
var dirRaw = Path.Combine(directorioDatosReales, "raw");
var dirMetadata = Path.Combine(directorioDatosReales, "metadata");
Directory.CreateDirectory(dirRaw);
Directory.CreateDirectory(dirMetadata);

// symbolDescarga/rango2024_2025 son independientes de `symbol`/rango 2022-2023 por defecto —
// spec: ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md §2 (D-125). Sin la
// variable de entorno, el comportamiento es identico al ya congelado (BTCUSDT, rango 2022-2023).
var symbolDescarga = Environment.GetEnvironmentVariable("DESCARGAR_BINANCE_SYMBOL") ?? symbol;
var usarRango2024_2025 = Environment.GetEnvironmentVariable("DESCARGAR_BINANCE_RANGO_2024_2025") is not null;

// spec: DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md (D-122) — rango 2023-01-02..2024-01-02 fue
// rechazado por ValidadorIntegridadDatos (hueco real de 80 min, ver
// HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md). Candidato 2022 confirmado VIABLE por
// ExploradorDisponibilidad (12/12 meses continuos) — se descarga completo para validacion real.
// Rango 2024-01-02..2025-01-02: D-121/D-125, mismo rango ya congelado para BTCUSDT — usado aqui
// para el instrumento candidato ETHUSDT (D-125), confirmado VIABLE por exploracion (12/12 bloques).
var finUtc = usarRango2024_2025
    ? new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero)
    : new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero); // dia de prueba fijo, determinista
var inicioUtc = descargar == "DIA"
    ? finUtc.AddDays(-1)
    : usarRango2024_2025
        ? new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
        : finUtc.AddYears(-1);

// sufijo incluye el anio de fin para no colisionar con el crudo 2024-2025 ya usado para congelar
// el dataset actual (datos_reales/raw/ es solo registro de la descarga, pero se preserva por
// trazabilidad).
var sufijo = descargar == "DIA" ? "1dia_prueba" : $"1anio_{finUtc.Year}";
var rutaCsv = Path.Combine(dirRaw, $"{symbolDescarga}_{interval}_{sufijo}.csv");
var rutaMetadata = Path.Combine(dirMetadata, $"{symbolDescarga}_{interval}_{sufijo}.json");

Console.WriteLine($"\n=== Descarga Binance: {symbolDescarga} {interval} [{inicioUtc:O} .. {finUtc:O}) ===");
Console.WriteLine($"CSV crudo: {rutaCsv}");

var cliente = new BinanceClient();
var resultado = await DescargadorVelas.DescargarAsync(
    cliente, symbolDescarga, interval, inicioUtc.ToUnixTimeMilliseconds(), finUtc.ToUnixTimeMilliseconds(), rutaCsv);

var velasEsperadas = (int)((resultado.FinUtcMs - resultado.InicioUtcMs) / ValidadorIntegridadDatos.UnMinutoMs);

Console.WriteLine("\nDESCARGA COMPLETADA");
Console.WriteLine($"  Velas esperadas: {velasEsperadas}");
Console.WriteLine($"  Velas recibidas: {resultado.VelasDescargadas}");
Console.WriteLine($"  Inicio UTC: {DateTimeOffset.FromUnixTimeMilliseconds(resultado.InicioUtcMs):O}");
Console.WriteLine($"  Fin UTC: {DateTimeOffset.FromUnixTimeMilliseconds(resultado.FinUtcMs):O}");
Console.WriteLine("  Validacion: PENDIENTE");

var velas = DescargadorVelas.LeerCsv(rutaCsv);
var veredicto = ValidadorIntegridadDatos.Verificar(velas);
var duplicados = velas.Count - velas.Select(v => v.TimestampUtcMs).Distinct().Count();
var totalMinutosFaltantes = veredicto.Huecos.Sum(h => h.MinutosFaltantes);

Console.WriteLine("\n=== Reporte de integridad (checklist de escala) ===");
Console.WriteLine($"  Simbolo/Intervalo: {symbolDescarga} {interval}");
Console.WriteLine($"  Velas esperadas: {velasEsperadas}");
Console.WriteLine($"  Velas recibidas: {velas.Count}");
Console.WriteLine($"  Huecos: {veredicto.Huecos.Count} (minutos faltantes: {totalMinutosFaltantes})");
foreach (var h in veredicto.Huecos.Take(10))
    Console.WriteLine($"    - Desde={h.DesdeMs} Hasta={h.HastaMs} MinutosFaltantes={h.MinutosFaltantes}");
Console.WriteLine($"  Duplicados: {duplicados}");
Console.WriteLine($"  Orden: {(veredicto.Errores.Any(e => e.StartsWith("Orden")) ? "FALLA" : "OK")}");
Console.WriteLine($"  Errores estructurales: {veredicto.Errores.Count}");
foreach (var e in veredicto.Errores.Take(10))
    Console.WriteLine($"    - {e}");

if (!veredicto.AptoParaCongelar)
{
    Console.WriteLine("\n  Estado: NO APTO PARA CONGELAR");
    Console.WriteLine("Dataset NO apto. Queda solo en raw/, documentado, sin promover a datasets/reales/. No se intenta reparar (politica aprobada: rechazo, no relleno automatico).");
    Environment.Exit(1);
}

DescargadorVelas.EscribirMetadataValidada(rutaMetadata, symbolDescarga, interval, velas);
Console.WriteLine("\n  Estado: APTO PARA CONGELAR");
Console.WriteLine($"  SHA-256: (ver {rutaMetadata})");
Console.WriteLine($"\nMetadata (con SHA-256) escrita en: {rutaMetadata}");
Console.WriteLine($"Recordatorio: la promocion a datasets/reales/{symbolDescarga}/1m/ es un paso manual explicito (ver PLAN_FASE2A.md seccion 6), no automatico desde este programa.");
