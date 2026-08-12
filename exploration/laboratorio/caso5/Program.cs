using TD_Project.Caso5;

var raiz = AppContext.BaseDirectory;
var dirDatasets = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "datasets", "reales", "BTCUSDT"));

Console.WriteLine("=== Caso 5A — Gestores de Riesgo Intercambiables ===");
var (totalA, pasaronA, detallesA) = TestsGestoresRiesgo.EjecutarTodos(dirDatasets);
foreach (var d in detallesA)
    Console.WriteLine($"  {d}");
Console.WriteLine($"{pasaronA}/{totalA} pruebas pasaron.");

Console.WriteLine();
Console.WriteLine("=== Caso 5B — Comparador de Gestores de Riesgo ===");
var (totalB, pasaronB, detallesB) = TestsComparadorGestores.EjecutarTodos(dirDatasets);
foreach (var d in detallesB)
    Console.WriteLine($"  {d}");
Console.WriteLine($"{pasaronB}/{totalB} pruebas pasaron.");

if (pasaronA != totalA || pasaronB != totalB)
    Environment.Exit(1);
