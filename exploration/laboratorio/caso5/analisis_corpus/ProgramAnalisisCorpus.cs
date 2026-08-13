using TD_Project.Caso5.AnalisisCorpus;

// spec: ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md (D-123) — punto de entrada. Ejecuta las
// pruebas P1-P9 primero (si fallan, no calcula ningun resumen real); despues lee caso5/resultados/
// (unica fuente de datos, ninguna ejecucion nueva) y presenta ResumenCorpus. No implementa
// recomendacion, ranking, ni seleccion (D-118/D-119/D-120 permanecen en estado de principio).

var raiz = AppContext.BaseDirectory;
var dirCaso5 = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", ".."));
var dirResultados = Path.Combine(dirCaso5, "resultados");
var rutaManifiesto = Path.Combine(dirCaso5, "MANIFIESTO_CORPUS_CASO5C_V1.json");

var (total, pasaron, detalles) = TestsAnalisisCorpus.EjecutarTodas(dirResultados, rutaManifiesto);
Console.WriteLine("=== Caso 5C Capa 2 — Pruebas de analisis_corpus (P1-P9) ===");
foreach (var d in detalles)
    Console.WriteLine($"  {d}");
Console.WriteLine();
Console.WriteLine($"Resumen: {pasaron}/{total} pruebas pasaron.");
if (pasaron != total)
{
    Console.WriteLine("Al menos una prueba fallo — no se calcula ningun resumen sobre el corpus real.");
    Environment.Exit(1);
}

Console.WriteLine();
Console.WriteLine("=== Resumen descriptivo del corpus real (caso5/resultados/, segun caso5/MANIFIESTO_CORPUS_CASO5C_V1.json) ===");
var (filas, ignoradas) = LectorCorpus.Leer(dirResultados, rutaManifiesto);
var resumen = AnalisisDescriptivo.Resumir(filas, ignoradas);

Console.WriteLine();
Console.WriteLine("--- Cobertura ---");
Console.WriteLine($"Total de filas (comparacion x gestor): {resumen.Cobertura.TotalFilas}");
Console.WriteLine($"Carpetas ignoradas (estructura invalida): {resumen.Cobertura.CarpetasIgnoradas.Count}");
Console.WriteLine("Por estrategia:");
foreach (var (k, v) in resumen.Cobertura.ComparacionesPorEstrategia)
    Console.WriteLine($"  {k}: {v}");
Console.WriteLine("Por timeframe:");
foreach (var (k, v) in resumen.Cobertura.ComparacionesPorTimeframe)
    Console.WriteLine($"  {k}: {v}");
Console.WriteLine("Por gestor:");
foreach (var (k, v) in resumen.Cobertura.ComparacionesPorGestor)
    Console.WriteLine($"  {k}: {v}");
Console.WriteLine("Por dataset (periodo):");
foreach (var (k, v) in resumen.Cobertura.ComparacionesPorDataset)
    Console.WriteLine($"  {k}: {v}");

Console.WriteLine();
Console.WriteLine("--- Distribuciones (agrupadas por gestor) ---");
foreach (var dist in resumen.Distribuciones)
{
    Console.WriteLine($"{dist.NombreMetrica}:");
    foreach (var (grupo, est) in dist.PorGrupo)
        Console.WriteLine($"  {grupo}: n={est.Cantidad} min={est.Minimo} max={est.Maximo} media={est.Media} mediana={est.Mediana}");
}

Console.WriteLine();
Console.WriteLine("--- Comparacion entre periodos ---");
foreach (var comp in resumen.ComparacionesTemporal)
{
    Console.WriteLine($"{comp.NombreMetrica} / {comp.Gestor}:");
    foreach (var (dataset, est) in comp.PorDataset)
        Console.WriteLine($"  {dataset}: n={est.Cantidad} min={est.Minimo} max={est.Maximo} media={est.Media} mediana={est.Mediana}");
}

Console.WriteLine();
Console.WriteLine("--- Casos atipicos ---");
foreach (var caso in resumen.CasosAtipicos)
    Console.WriteLine($"  {caso.Descripcion} ({caso.CarpetasOrigen.Count} carpeta(s))");

Console.WriteLine();
Console.WriteLine("--- Limitaciones ---");
Console.WriteLine(resumen.Limitaciones);
