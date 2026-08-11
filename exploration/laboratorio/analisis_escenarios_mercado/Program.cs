using System.Text;
using TD_Project.AnalisisEscenariosMercado;
using TD_Project.EvaluacionMultiTf;

// Fase 1.4-A, Paso 3/4: ejecuta los 3 candidatos experimentales sobre el dataset BTCUSDT ya
// congelado, en varios timeframes (consistencia multi-TF), verifica determinismo, y escribe
// RESULTADO_EVALUACION_CLASIFICADORES_REGIMEN_V1.md. No ejecuta ninguna estrategia (D-016/D-021).
// Todos los parametros usados son CONFIGURACION EXPLORATORIA (D-022), no oficial.

var raiz = AppContext.BaseDirectory;
var dirLaboratorio = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", ".."));
var dirDatasets = Path.Combine(dirLaboratorio, "datasets", "reales", "BTCUSDT");
var nombreDataset = "BTCUSDT_2024-01-02_2025-01-02";

// Timeframes usados para medir consistencia multi-TF (§3.3): mismo subconjunto ya evaluado por
// backtest en Fase 2C, para poder comparar contra infraestructura ya congelada si hiciera falta.
var timeframes = new[] { "1m", "5m", "15m", "1h", "4h", "1D" };

var filas = new List<MetricasCandidato>();
var determinismos = new List<(string Candidato, string Tf, bool Determinista)>();

foreach (var tf in timeframes)
{
    var rutaCsv = tf == "1m"
        ? Path.Combine(dirDatasets, "1m", $"{nombreDataset}_1m.csv")
        : Path.Combine(dirDatasets, tf, $"{nombreDataset}_{tf}.csv");

    if (!File.Exists(rutaCsv))
    {
        Console.WriteLine($"ADVERTENCIA: dataset {tf} no encontrado en {rutaCsv} — se omite.");
        continue;
    }

    var velas = LectorDerivado.Leer(rutaCsv);

    // Candidato C — Retorno + Volatilidad
    var ventanaC = Math.Max(5, velas.Count / 200); // ventana exploratoria: ~200 tramos por timeframe
    var clasifC = ClasificadorRetornoVolatilidadExperimental.Clasificar(velas, ventanaC);
    filas.Add(EvaluadorClasificadores.Evaluar("C — Retorno+Volatilidad", tf, clasifC, velas.Count / ventanaC));
    determinismos.Add(("C — Retorno+Volatilidad", tf, EvaluadorClasificadores.VerificarDeterminismo(
        () => ClasificadorRetornoVolatilidadExperimental.Clasificar(velas, ventanaC))));

    // Candidato A — EMA
    var clasifA = ClasificadorEmaExperimental.Clasificar(velas, ClasificadorEmaExperimental.PeriodoEmaExploratorio);
    filas.Add(EvaluadorClasificadores.Evaluar("A — EMA", tf, clasifA, velas.Count));
    determinismos.Add(("A — EMA", tf, EvaluadorClasificadores.VerificarDeterminismo(
        () => ClasificadorEmaExperimental.Clasificar(velas, ClasificadorEmaExperimental.PeriodoEmaExploratorio))));

    // Candidato B — ADX+DI
    var clasifB = ClasificadorAdxExperimental.Clasificar(velas, ClasificadorAdxExperimental.PeriodoAdxExploratorio);
    filas.Add(EvaluadorClasificadores.Evaluar("B — ADX+DI", tf, clasifB, velas.Count));
    determinismos.Add(("B — ADX+DI", tf, EvaluadorClasificadores.VerificarDeterminismo(
        () => ClasificadorAdxExperimental.Clasificar(velas, ClasificadorAdxExperimental.PeriodoAdxExploratorio))));

    Console.WriteLine($"Timeframe {tf}: {velas.Count} velas — 3 candidatos ejecutados.");
}

Console.WriteLine("\n=== Resultado (consola) ===");
Console.WriteLine($"{"Candidato",-26}{"TF",-5}{"Cobertura%",12}{"CambiosRegimen%",17}{"Ventanas",10}{"Determinista",14}");
foreach (var f in filas)
{
    var det = determinismos.First(d => d.Candidato == f.Candidato && d.Tf == f.Timeframe).Determinista;
    Console.WriteLine($"{f.Candidato,-26}{f.Timeframe,-5}{f.PctCobertura,11:F2}%{f.PctCambiosDeRegimen,16:F2}%{f.VentanasTotales,10}{(det ? "si" : "NO"),14}");
}

Console.WriteLine("\n=== Distribucion por regimen y duracion media (revision pendiente §1/§2) ===");
Console.WriteLine($"{"Candidato",-26}{"TF",-5}{"Alcista%",9}{"Bajista%",9}{"Lateral%",9}{"Ambiguo%",9}{"DurMedia",10}{"Tramos=1",9}");
foreach (var f in filas)
    Console.WriteLine($"{f.Candidato,-26}{f.Timeframe,-5}{f.PctAlcista,8:F2}%{f.PctBajista,8:F2}%{f.PctLateral,8:F2}%{f.PctAmbiguo,8:F2}%{f.DuracionMediaVentanas,10:F2}{f.TramosDeUnaSolaVentana,9}");

var todosDeterministas = determinismos.All(d => d.Determinista);
Console.WriteLine($"\nDeterminismo global: {(todosDeterministas ? "OK — los 3 candidatos son reproducibles" : "FALLA — revisar candidato no determinista")}");

// Paso 4: informe comparativo, sin declarar ganador (EVALUACION_CLASIFICADORES_REGIMEN_V1.md §5).
var sb = new StringBuilder();
sb.AppendLine("# Resultado — Evaluación de Clasificadores de Régimen Candidatos V1");
sb.AppendLine();
sb.AppendLine("Estado: **evidencia experimental — Fase 1.4-A, Paso 4**. Presenta evidencia para una");
sb.AppendLine("decisión posterior de selección; no concluye cuál candidato es \"el mejor\". Ninguna");
sb.AppendLine("estrategia fue ejecutada para producir este informe (D-016/D-021). Todos los parámetros");
sb.AppendLine("usados son **Configuración exploratoria** (D-022), no oficial — ver cada archivo de");
sb.AppendLine("clasificador para el detalle de los valores usados.");
sb.AppendLine();
sb.AppendLine($"Dataset: `{nombreDataset}` (BTC/USDT Spot, Binance, hash verificado en Fase 1.0/baseline).");
sb.AppendLine($"Timeframes evaluados: {string.Join(", ", timeframes)}.");
sb.AppendLine();
sb.AppendLine("---");
sb.AppendLine();
sb.AppendLine("## 1. Estabilidad temporal y cobertura, por candidato y timeframe");
sb.AppendLine();
sb.AppendLine("*(§3.1 — % de cambios de régimen entre ventanas consecutivas; §3.2 — % de ventanas");
sb.AppendLine("clasificadas como Alcista/Bajista/Lateral, excluyendo Ambiguo.)*");
sb.AppendLine();
sb.AppendLine("| Candidato | TF | Cobertura % | Cambios de régimen % | Ventanas | Determinista |");
sb.AppendLine("|---|---|---|---|---|---|");
foreach (var f in filas)
{
    var det = determinismos.First(d => d.Candidato == f.Candidato && d.Tf == f.Timeframe).Determinista;
    sb.AppendLine($"| {f.Candidato} | {f.Timeframe} | {f.PctCobertura:F2}% | {f.PctCambiosDeRegimen:F2}% | {f.VentanasTotales} | {(det ? "sí" : "NO")} |");
}
sb.AppendLine();
sb.AppendLine("---");
sb.AppendLine();
sb.AppendLine("## 1bis. Distribución por régimen y duración (revisión pendiente §1/§2 de auditoría)");
sb.AppendLine();
sb.AppendLine("*(% de ventanas por categoría; duración media = tramo medio, en ventanas, antes de que el");
sb.AppendLine("candidato cambie de escenario; \"Tramos=1\" = cantidad de tramos que duran exactamente 1");
sb.AppendLine("ventana, posible indicador de fragmentación/ruido si es una proporción alta del total de");
sb.AppendLine("tramos.)*");
sb.AppendLine();
sb.AppendLine("| Candidato | TF | Alcista % | Bajista % | Lateral % | Ambiguo % | Duración media (ventanas) | Tramos de 1 ventana |");
sb.AppendLine("|---|---|---|---|---|---|---|---|");
foreach (var f in filas)
    sb.AppendLine($"| {f.Candidato} | {f.Timeframe} | {f.PctAlcista:F2}% | {f.PctBajista:F2}% | {f.PctLateral:F2}% | {f.PctAmbiguo:F2}% | {f.DuracionMediaVentanas:F2} | {f.TramosDeUnaSolaVentana} |");
sb.AppendLine();
sb.AppendLine("---");
sb.AppendLine();
sb.AppendLine("## 2. Consistencia multi-timeframe (§3.3)");
sb.AppendLine();
sb.AppendLine("Por candidato, rango de cobertura y de cambios de régimen a través de los timeframes");
sb.AppendLine("evaluados — mismo formato mínimo/máximo/amplitud ya usado en Fase 1.3 (D-014), sin");
sb.AppendLine("clasificación cualitativa.");
sb.AppendLine();
sb.AppendLine("| Candidato | Cobertura mín-máx | Amplitud cobertura | Cambios régimen mín-máx | Amplitud cambios |");
sb.AppendLine("|---|---|---|---|---|");
foreach (var candidato in filas.Select(f => f.Candidato).Distinct())
{
    var filasCandidato = filas.Where(f => f.Candidato == candidato).ToList();
    var covMin = filasCandidato.Min(f => f.PctCobertura);
    var covMax = filasCandidato.Max(f => f.PctCobertura);
    var camMin = filasCandidato.Min(f => f.PctCambiosDeRegimen);
    var camMax = filasCandidato.Max(f => f.PctCambiosDeRegimen);
    sb.AppendLine($"| {candidato} | {covMin:F2}% – {covMax:F2}% | {covMax - covMin:F2}pp | {camMin:F2}% – {camMax:F2}% | {camMax - camMin:F2}pp |");
}
sb.AppendLine();
sb.AppendLine("---");
sb.AppendLine();
sb.AppendLine("## 3. Explicabilidad (§3.4 — descriptivo, no numérico)");
sb.AppendLine();
sb.AppendLine("- **A — EMA**: alta. \"El precio subió/bajó de forma sostenida según el promedio móvil\" es");
sb.AppendLine("  comprensible sin formación técnica previa.");
sb.AppendLine("- **B — ADX+DI**: media-baja. Requiere explicar qué mide un índice direccional promediado;");
sb.AppendLine("  el motivo de una clasificación no es legible directamente del precio.");
sb.AppendLine("- **C — Retorno+Volatilidad**: alta. \"El precio subió X% en esta ventana, con un rango de");
sb.AppendLine("  Y%\" es una frase directa sobre el precio observado, sin indicador intermedio.");
sb.AppendLine();
sb.AppendLine("---");
sb.AppendLine();
sb.AppendLine("## 4. Reproducibilidad (§3.5)");
sb.AppendLine();
sb.AppendLine($"Los 3 candidatos fueron ejecutados dos veces sobre la misma entrada por cada timeframe y");
sb.AppendLine($"comparados campo a campo (inicio, fin, escenario de cada ventana). Resultado:");
sb.AppendLine($"**{(todosDeterministas ? "determinismo confirmado en las 18 combinaciones (3 candidatos × 6 timeframes)" : "determinismo NO confirmado — ver detalle por candidato/timeframe arriba")}**.");
sb.AppendLine();
sb.AppendLine("---");
sb.AppendLine();
sb.AppendLine("## 5. Evidencia, no conclusión");
sb.AppendLine();
sb.AppendLine("Este documento presenta las 5 dimensiones por separado, sin puntaje único combinado");
sb.AppendLine("(EVALUACION_CLASIFICADORES_REGIMEN_V1.md §5). La selección del candidato oficial es una");
sb.AppendLine("decisión posterior, no automática ni derivada de este informe.");
sb.AppendLine();
sb.AppendLine("Observaciones objetivas para esa decisión futura (sin declarar ganador):");
sb.AppendLine("- El candidato A (EMA) no distingue \"Ambiguo\" de \"Lateral\" en esta implementación");
sb.AppendLine("  exploratoria — su cobertura reportada no es comparable 1:1 con B/C en ese sentido.");
sb.AppendLine("- El candidato B (ADX+DI) requiere una ventana de calentamiento mayor (2×periodo) antes de");
sb.AppendLine("  producir su primera clasificación, reduciendo el número de ventanas evaluables frente a");
sb.AppendLine("  A y C en el mismo dataset.");
sb.AppendLine("- El candidato C (Retorno+Volatilidad) es el único con categoría \"Ambiguo\" explícita en");
sb.AppendLine("  esta implementación exploratoria, consistente con el diseño de");
sb.AppendLine("  ESPECIFICACION_ANALISIS_ESCENARIOS_MERCADO_V1.md §5.");

var rutaInforme = Path.Combine(dirLaboratorio, "analisis_escenarios_mercado", "RESULTADO_EVALUACION_CLASIFICADORES_REGIMEN_V1.md");
File.WriteAllText(rutaInforme, sb.ToString());
Console.WriteLine($"\nInforme escrito en: {rutaInforme}");

// Paso 3-B (D-028..D-033): pruebas requeridas antes de cerrar el congelamiento de ClasificadorRegimenV1.
Console.WriteLine("\n=== Fase 1.4-B, Paso 3-B — Pruebas ClasificadorRegimenV1 ===");
var (totalV1, pasaronV1, detallesV1) = TestsClasificadorRegimenV1.EjecutarTodos(dirDatasets, nombreDataset);
foreach (var d in detallesV1)
    Console.WriteLine($"  {d}");
Console.WriteLine($"\nResultado: {pasaronV1}/{totalV1} pruebas de ClasificadorRegimenV1 pasaron.");

if (pasaronV1 != totalV1)
    Environment.Exit(1);
