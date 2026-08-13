using TD_Project.Caso5.AnalisisCorpus;
using TD_Project.Caso5.AnalisisInterpretativo;

// spec: ESPECIFICACION_IMPLEMENTACION_ANALISIS_INTERPRETATIVO_CASO5C_V1.md (D-124) — punto de
// entrada. Ejecuta las pruebas P1-P8 primero (si fallan, no calcula ningun analisis real); despues
// lee caso5/resultados/ via el mismo MANIFIESTO_CORPUS_CASO5C_V1.json de Capa 2 (ninguna ejecucion
// nueva, ninguna segunda ruta de carga de evidencia) y presenta relaciones/agrupaciones/
// condiciones/consistencia usando SOLO los patrones ya nombrados en auditorias previas
// (DrawdownMaximoPct>=99%, SinActividad). No implementa recomendacion, ranking, ni seleccion
// (D-118/D-119/D-120 permanecen en estado de principio).

var raiz = AppContext.BaseDirectory;
var dirCaso5 = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", ".."));
var dirResultados = Path.Combine(dirCaso5, "resultados");
var rutaManifiesto = Path.Combine(dirCaso5, "MANIFIESTO_CORPUS_CASO5C_V1.json");

var (total, pasaron, detalles) = TestsAnalisisInterpretativo.EjecutarTodas(dirResultados, rutaManifiesto);
Console.WriteLine("=== Caso 5C — Pruebas de analisis_interpretativo (P1-P8) ===");
foreach (var d in detalles)
    Console.WriteLine($"  {d}");
Console.WriteLine();
Console.WriteLine($"Resumen: {pasaron}/{total} pruebas pasaron.");
if (pasaron != total)
{
    Console.WriteLine("Al menos una prueba fallo — no se calcula ningun analisis sobre el corpus real.");
    Environment.Exit(1);
}

Console.WriteLine();
Console.WriteLine("=== Analisis interpretativo limitado del corpus real (caso5/resultados/, segun caso5/MANIFIESTO_CORPUS_CASO5C_V1.json) ===");
var (filas, _) = LectorCorpus.Leer(dirResultados, rutaManifiesto);

// Patrones ya nombrados por auditorias previas — no se definen umbrales nuevos aqui (D-124, D-030).
bool EsDrawdownExtremo(FilaCorpus f) => f.DrawdownMaximoPct is { } d && d >= 0.99m;
bool EsSinActividad(FilaCorpus f) => f.Estado == "Success" && f.PnLTotal == 0m && f.CashFinal.HasValue;

Console.WriteLine();
Console.WriteLine("--- Relacion observada: DrawdownMaximoPct por Estrategia x Gestor ---");
var relacion = DetectorRelaciones.CruzarDimensiones(filas, "DrawdownMaximoPct", new[] { "Estrategia", "Gestor" });
foreach (var c in relacion.Combinaciones)
{
    var dims = string.Join(", ", c.Dimensiones.Select(kv => $"{kv.Key}={kv.Value}"));
    var est = c.Estadistica is { } e ? $"n={e.Cantidad} min={e.Minimo} max={e.Maximo} media={e.Media} mediana={e.Mediana}" : "(sin metrica)";
    Console.WriteLine($"  [{dims}] {est}");
}

Console.WriteLine();
Console.WriteLine("--- Agrupacion por patron: DrawdownMaximoPct>=99% ---");
var agrupacionDrawdown = DetectorRelaciones.AgruparPorPatron(filas, "DrawdownMaximoPct>=99%", EsDrawdownExtremo);
Console.WriteLine($"  Donde aparece ({agrupacionDrawdown.DondeAparece.Count}):");
foreach (var d in agrupacionDrawdown.DondeAparece) Console.WriteLine($"    {d}");
Console.WriteLine($"  Donde NO aparece ({agrupacionDrawdown.DondeNoAparece.Count}): (omitido por volumen — ver conteo)");

Console.WriteLine();
Console.WriteLine("--- Agrupacion por patron: SinActividad ---");
var agrupacionSinActividad = DetectorRelaciones.AgruparPorPatron(filas, "SinActividad", EsSinActividad);
Console.WriteLine($"  Donde aparece ({agrupacionSinActividad.DondeAparece.Count}):");
foreach (var d in agrupacionSinActividad.DondeAparece.Distinct()) Console.WriteLine($"    {d}");
Console.WriteLine($"  Donde NO aparece ({agrupacionSinActividad.DondeNoAparece.Count}): (omitido por volumen — ver conteo)");

Console.WriteLine();
Console.WriteLine("--- Condiciones de aparicion: DrawdownMaximoPct >= 0.99 ---");
var condiciones = DetectorRelaciones.DescribirCondicionesDeAparicion(filas, "DrawdownMaximoPct", v => v >= 0.99m, "DrawdownMaximoPct >= 0.99");
foreach (var c in condiciones.Combinaciones)
{
    var dims = string.Join(", ", c.Dimensiones.Select(kv => $"{kv.Key}={kv.Value}"));
    Console.WriteLine($"  [{dims}] filas={c.CantidadFilas}");
}

Console.WriteLine();
Console.WriteLine("--- Consistencia entre periodos: DrawdownMaximoPct>=99% ---");
var consistencia = DetectorRelaciones.CompararConsistencia(filas, "DrawdownMaximoPct>=99%", EsDrawdownExtremo);
foreach (var (dataset, condicionesDataset) in consistencia.CondicionesPorDataset)
{
    Console.WriteLine($"  {dataset} ({condicionesDataset.Count}):");
    foreach (var c in condicionesDataset) Console.WriteLine($"    {c}");
}

// Instrumento = prefijo del nombre de dataset antes del rango de fechas, tomado solo de filas con
// metrica real (mismo filtro de AnalisisDescriptivo.Resumir para "periodo temporal" — excluye
// DatasetInexistente_ParaCorpusDeFallo, evidencia deliberada de fallo sin PnLTotal, que de otro
// modo se contaria como un instrumento inexistente). Mismo criterio ya aplicado en
// analisis_corpus/AnalisisDescriptivo.cs tras D-126 — antes hardcodeado "BTCUSDT", quedo
// desactualizado al incorporar ETHUSDT.
var instrumentosInterpretativo = filas.Where(f => f.PnLTotal.HasValue).Select(f => f.NombreDataset.Split('_')[0]).Distinct().OrderBy(s => s).ToList();
var textoInstrumentosInterpretativo = instrumentosInterpretativo.Count == 1
    ? $"instrumento unico ({instrumentosInterpretativo[0]})"
    : $"{instrumentosInterpretativo.Count} instrumentos ({string.Join(", ", instrumentosInterpretativo)})";

Console.WriteLine();
Console.WriteLine("--- Limitaciones ---");
Console.WriteLine(
    $"Analisis interpretativo limitado sobre {filas.Count} filas, {textoInstrumentosInterpretativo}. " +
    "Esta salida describe relaciones/agrupaciones/condiciones observadas en el corpus persistido — " +
    "no constituye recomendacion, ranking, seleccion, ni regla operativa (D-118/D-119/D-120). " +
    "Los patrones usados (DrawdownMaximoPct>=99%, SinActividad) son los ya documentados en " +
    "auditorias previas, no umbrales calibrados en esta ejecucion (D-030). Ningun patron aqui " +
    "descrito se extiende a instrumentos no representados en el corpus. Observacion historica, " +
    "no proyeccion de comportamiento futuro.");
