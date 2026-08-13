using TD_Project.Caso5.AnalisisCorpus;
using TD_Project.Caso6.Recomendador;

// spec: ESPECIFICACION_IMPLEMENTACION_RECOMENDADOR_CASO6_V1.md (D-128) — punto de entrada. Ejecuta
// P1-P9 primero (si fallan, no calcula ninguna recomendacion real); despues lee caso5/resultados/
// via el mismo MANIFIESTO_CORPUS_CASO5C_V1.json ya usado por Capa 2/interpretativo (ninguna
// ejecucion nueva, ninguna segunda ruta de carga de evidencia) y produce RecomendacionExperimental
// para los perfiles Crecimiento y Preservacion sobre el corpus real. No implementa seleccion ni
// ejecucion automatica (D-118/D-119/D-120).

var raiz = AppContext.BaseDirectory;
var dirRecomendador = Path.GetFullPath(Path.Combine(raiz, "..", "..", ".."));
var dirCaso5 = Path.GetFullPath(Path.Combine(dirRecomendador, "..", "..", "caso5"));
var dirResultados = Path.Combine(dirCaso5, "resultados");
var rutaManifiesto = Path.Combine(dirCaso5, "MANIFIESTO_CORPUS_CASO5C_V1.json");

var (total, pasaron, detalles) = TestsRecomendador.EjecutarTodas(dirResultados, rutaManifiesto);
Console.WriteLine("=== Caso 6 — Pruebas de recomendador (P1-P9) ===");
foreach (var d in detalles)
    Console.WriteLine($"  {d}");
Console.WriteLine();
Console.WriteLine($"Resumen: {pasaron}/{total} pruebas pasaron.");
if (pasaron != total)
{
    Console.WriteLine("Al menos una prueba fallo — no se calcula ninguna recomendacion sobre el corpus real.");
    Environment.Exit(1);
}

Console.WriteLine();
Console.WriteLine("=== Recomendador experimental sobre el corpus real (caso5/resultados/, segun caso5/MANIFIESTO_CORPUS_CASO5C_V1.json) ===");
var (filas, _) = LectorCorpus.Leer(dirResultados, rutaManifiesto);

void ImprimirRecomendacion(RecomendacionExperimental r)
{
    Console.WriteLine();
    Console.WriteLine($"--- Perfil: {r.Perfil} ---");
    Console.WriteLine($"CriterioUsado: {r.CriterioUsado}");
    Console.WriteLine($"ConfiguracionesAnalizadas (grupos Estrategia+Timeframe+Dataset): {r.ConfiguracionesAnalizadas}");
    Console.WriteLine($"ConfiguracionesConEvidencia: {r.ConfiguracionesConEvidencia}");
    Console.WriteLine($"Candidatas ({r.Candidatas.Count}), en orden de aparicion en el corpus:");
    foreach (var c in r.Candidatas)
        Console.WriteLine($"  {c.Estrategia}/{c.Timeframe}/{c.NombreDataset} gestor={c.IdentidadGestor} valor={c.ValorMetrica}");
    Console.WriteLine($"Limitaciones: {r.Limitaciones}");
}

ImprimirRecomendacion(MotorRecomendacion.Recomendar(filas, "Crecimiento"));
ImprimirRecomendacion(MotorRecomendacion.Recomendar(filas, "Preservacion"));
