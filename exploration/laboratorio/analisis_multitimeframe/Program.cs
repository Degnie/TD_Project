using TD_Project.AnalisisMultiTimeframe;

// Fase 1.3, Paso 3 (ESPECIFICACION_ANALISIS_MULTITIMEFRAME_V1.md): pruebas del comparador
// multi-timeframe contra cifras ya publicadas, mas un reporte descriptivo de ejemplo que respeta
// D-010 (tamaño de muestra obligatorio) y la separacion mejor-resultado/mayor-evidencia (§7).
var (total, pasaron, detalles) = Tests.EjecutarTodos();

Console.WriteLine("=== Fase 1.3 — Pruebas ComparadorMultiTimeframe (Paso 3) ===");
foreach (var d in detalles)
    Console.WriteLine($"  {d}");
Console.WriteLine($"\nResultado: {pasaron}/{total} pruebas pasaron.");

if (pasaron != total)
{
    Environment.Exit(1);
    return;
}

Console.WriteLine("\n=== Ejemplo de reporte descriptivo — Tres Mosqueteros (D-010: muestra obligatoria) ===");
var tm = ComparadorMultiTimeframe.Comparar("Tres Mosqueteros", TestsFixtures.PerfilesTresMosqueterosPublico());
Console.WriteLine($"{"TF",-5}{"Eficiencia",12}{"Muestra",14}{"%Marting",10}{"RachaMax",10}");
foreach (var f in tm.Filas)
    Console.WriteLine($"{f.Timeframe,-5}{f.EficienciaOperacionalPct,11:F2}%{f.IntentosCompletados,14}{f.PctResueltasPorMartingala,9:F1}%{f.MayorRachaNegativa,10}");

Console.WriteLine($"\nConsistencia (rango, sin clasificacion): {tm.ConsistenciaEficienciaOperacional.TimeframeMinimo}={tm.ConsistenciaEficienciaOperacional.ValorMinimo:F2}% .. {tm.ConsistenciaEficienciaOperacional.TimeframeMaximo}={tm.ConsistenciaEficienciaOperacional.ValorMaximo:F2}% (amplitud {tm.ConsistenciaEficienciaOperacional.AmplitudPuntosPorcentuales:F2}pp)");
Console.WriteLine($"Mejor resultado observado: {tm.MejorResultadoObservado.Timeframe} ({tm.MejorResultadoObservado.EficienciaOperacionalPct:F2}%, muestra={tm.MejorResultadoObservado.IntentosCompletados})");
Console.WriteLine($"Mayor evidencia: {tm.MayorEvidencia.Timeframe} (muestra={tm.MayorEvidencia.IntentosCompletados})");
Console.WriteLine("Nota: ambas dimensiones apuntan a timeframes DISTINTOS — separacion intencional (ESPECIFICACION §7), no se combinan en un ranking unico.");
