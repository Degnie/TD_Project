using TD_Project.ModeloFinanciero;

Console.WriteLine("=== Caso 2.4 — MetricasFinancieras ===");
var (total, pasaron, detalles) = TestsMetricasFinancieras.EjecutarTodos();
foreach (var d in detalles)
    Console.WriteLine($"  {d}");
Console.WriteLine($"{pasaron}/{total} pruebas pasaron.");

if (pasaron != total)
    Environment.Exit(1);
