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
    var resultadoExploracion = await ExploradorDisponibilidad.ExplorarAsync(new BinanceClient(), symbol, interval, inicioAnio, finAnioExploracion);

    Console.WriteLine($"\n=== Exploracion de disponibilidad: {symbol} {interval} {anio} ===");
    foreach (var b in resultadoExploracion.Bloques)
        Console.WriteLine($"  {b.InicioUtc:yyyy-MM} .. {b.FinUtc:yyyy-MM}: {(b.Continuo ? "OK" : $"HUECO ({b.Huecos} huecos, {b.MinutosFaltantes} min)")}");
    Console.WriteLine($"\nCandidato {anio}: {(resultadoExploracion.TodosContinuos ? "VIABLE — continuidad OK en los 12 meses" : "DESCARTADO — al menos 1 mes con discontinuidad")}");
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

// spec: DECISIONES_RANGO_ALTERNATIVO_CASO5C_V1.md (D-122) — rango 2023-01-02..2024-01-02 fue
// rechazado por ValidadorIntegridadDatos (hueco real de 80 min, ver
// HALLAZGO_RECHAZO_DATASET_2023_CASO5C_V1.md). Candidato 2022 confirmado VIABLE por
// ExploradorDisponibilidad (12/12 meses continuos) — se descarga completo para validacion real.
var finUtc = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero); // dia de prueba fijo, determinista
var inicioUtc = descargar == "DIA"
    ? finUtc.AddDays(-1)
    : finUtc.AddYears(-1);

// sufijo incluye el anio de fin para no colisionar con el crudo 2024-2025 ya usado para congelar
// el dataset actual (datos_reales/raw/ es solo registro de la descarga, pero se preserva por
// trazabilidad).
var sufijo = descargar == "DIA" ? "1dia_prueba" : $"1anio_{finUtc.Year}";
var rutaCsv = Path.Combine(dirRaw, $"{symbol}_{interval}_{sufijo}.csv");
var rutaMetadata = Path.Combine(dirMetadata, $"{symbol}_{interval}_{sufijo}.json");

Console.WriteLine($"\n=== Descarga Binance: {symbol} {interval} [{inicioUtc:O} .. {finUtc:O}) ===");
Console.WriteLine($"CSV crudo: {rutaCsv}");

var cliente = new BinanceClient();
var resultado = await DescargadorVelas.DescargarAsync(
    cliente, symbol, interval, inicioUtc.ToUnixTimeMilliseconds(), finUtc.ToUnixTimeMilliseconds(), rutaCsv);

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
Console.WriteLine($"  Simbolo/Intervalo: {symbol} {interval}");
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

DescargadorVelas.EscribirMetadataValidada(rutaMetadata, symbol, interval, velas);
Console.WriteLine("\n  Estado: APTO PARA CONGELAR");
Console.WriteLine($"  SHA-256: (ver {rutaMetadata})");
Console.WriteLine($"\nMetadata (con SHA-256) escrita en: {rutaMetadata}");
Console.WriteLine("Recordatorio: la promocion a datasets/reales/BTCUSDT/1m/ es un paso manual explicito (ver PLAN_FASE2A.md seccion 6), no automatico desde este programa.");
