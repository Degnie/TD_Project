using TD_Project.ValidacionIntegral;

var raiz = AppContext.BaseDirectory;
var dirBase = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "datasets_generados"));

Console.WriteLine("=== Prueba Integral del Sistema — Validacion de Madurez (Caso 1 a Caso 4) ===");
Console.WriteLine($"Directorio de datasets sinteticos: {dirBase}\n");

var (hallazgos, hayContradicciones) = TestsValidacionIntegral.EjecutarTodos(dirBase);

string? seccionActual = null;
foreach (var h in hallazgos)
{
    if (h.Seccion != seccionActual)
    {
        seccionActual = h.Seccion;
        Console.WriteLine($"\n--- {seccionActual} ---");
    }
    var marca = h.EsContradiccion ? "[CONTRADICCION]" : "[OK]";
    Console.WriteLine($"  {marca} {h.Descripcion}");
}

Console.WriteLine($"\n=== TOTAL: {hallazgos.Count} hallazgos registrados. Contradicciones: {hallazgos.Count(x => x.EsContradiccion)} ===");

if (hayContradicciones)
{
    Console.WriteLine("Se detectaron contradicciones — NO corregir automaticamente. Documentar en AUDITORIA_PRUEBA_INTEGRAL_SISTEMA_V1.md y esperar decision del auditor.");
    Environment.Exit(1);
}
