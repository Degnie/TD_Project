using TD_Project.Caso4;

var raiz = AppContext.BaseDirectory;
var dirDatasets = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "datasets", "reales", "BTCUSDT"));

Console.WriteLine("=== Caso 4.4 — Reporte de Incapacidades ===");
var (total, pasaron, detalles) = TestsReporteIncapacidades.EjecutarTodos(dirDatasets);
foreach (var d in detalles)
    Console.WriteLine($"  {d}");
Console.WriteLine($"{pasaron}/{total} pruebas pasaron.");

if (pasaron != total)
    Environment.Exit(1);
