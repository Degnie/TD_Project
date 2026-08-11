using TD_Project.ReporteEscenariosMercado;

var totalGeneral = 0;
var pasaronGeneral = 0;

void CorrerSuite(string titulo, (int Total, int Pasaron, IReadOnlyList<string> Detalles) resultado)
{
    Console.WriteLine($"=== {titulo} ===");
    foreach (var d in resultado.Detalles)
        Console.WriteLine($"  {d}");
    Console.WriteLine($"{resultado.Pasaron}/{resultado.Total} pruebas pasaron.\n");
    totalGeneral += resultado.Total;
    pasaronGeneral += resultado.Pasaron;
}

CorrerSuite("Fase 1.5-A, Paso 1 — Instrumentacion temporal (TimestampEntrada/TimestampResolucion)", TestsInstrumentacionTemporal.EjecutarTodos());
CorrerSuite("Fase 1.5-A, Paso 2 — AsignadorOperacionRegimen", TestsAsignadorOperacionRegimen.EjecutarTodos());
CorrerSuite("Fase 1.5-A, Paso 3 — MetricasPorEscenario", TestsMetricasPorEscenario.EjecutarTodos());
CorrerSuite("Fase 1.5-B — ReporteEscenariosGenerador", TestsReporteEscenariosGenerador.EjecutarTodos());

Console.WriteLine($"TOTAL: {pasaronGeneral}/{totalGeneral} pruebas pasaron.");

if (pasaronGeneral != totalGeneral)
    Environment.Exit(1);
