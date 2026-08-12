using TD_Project.Caso5;

var raiz = AppContext.BaseDirectory;
var dirDatasets = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "datasets", "reales", "BTCUSDT"));

Console.WriteLine("=== Caso 5A — Gestores de Riesgo Intercambiables ===");
var (total, pasaron, detalles) = TestsGestoresRiesgo.EjecutarTodos(dirDatasets);
foreach (var d in detalles)
    Console.WriteLine($"  {d}");
Console.WriteLine($"{pasaron}/{total} pruebas pasaron.");

if (pasaron != total)
    Environment.Exit(1);
