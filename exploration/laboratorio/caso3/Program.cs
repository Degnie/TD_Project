using TD_Project.Caso3;

var raiz = AppContext.BaseDirectory;
var dirDatasets = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "datasets", "reales", "BTCUSDT"));

var totalGeneral = 0;
var pasaronGeneral = 0;

void CorrerSuite(string titulo, (int Total, int Pasaron, IReadOnlyList<string> Detalles) r)
{
    Console.WriteLine($"=== {titulo} ===");
    foreach (var d in r.Detalles)
        Console.WriteLine($"  {d}");
    Console.WriteLine($"{r.Pasaron}/{r.Total} pruebas pasaron.\n");
    totalGeneral += r.Total;
    pasaronGeneral += r.Pasaron;
}

CorrerSuite("Caso 3A — EstrategiaZScoreReversion", TestsCaso3.EjecutarTodos());
CorrerSuite("Caso 3A — EstrategiaNeutral", TestsEstrategiaNeutral.EjecutarTodos(dirDatasets));

Console.WriteLine($"TOTAL: {pasaronGeneral}/{totalGeneral} pruebas pasaron.");
if (pasaronGeneral != totalGeneral)
    Environment.Exit(1);
