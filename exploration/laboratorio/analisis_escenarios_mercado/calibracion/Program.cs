using System.Text;
using System.Security.Cryptography;
using TD_Project.AnalisisEscenariosMercado;
using TD_Project.EvaluacionMultiTf;

// D-032 (auditoria, 2026-08-11): ejecuta el metodo aprobado (mediana de SesgoDI en la zona
// ADX < 25) sobre el dataset congelado, en los 6 timeframes ya usados en Fase 1.4-A, y escribe
// RESULTADO_CALIBRACION_UMBRAL_SESGO_DI_V1.md. No ejecuta ninguna estrategia (D-016). No ajusta
// el resultado — el valor que produzca el metodo es el que se reporta, sin edicion posterior.

const int periodoAdx = 14; // PeriodoAdxExploratorio, PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §2.1
const decimal umbralTendencia = 25m; // UmbralAdxTendenciaExploratorio, §2.3

var raiz = AppContext.BaseDirectory;
var dirLaboratorio = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", ".."));
var dirDatasets = Path.Combine(dirLaboratorio, "datasets", "reales", "BTCUSDT");
var nombreDataset = "BTCUSDT_2024-01-02_2025-01-02";
var timeframes = new[] { "1m", "5m", "15m", "1h", "4h", "1D" };

var sb = new StringBuilder();
sb.AppendLine("# Resultado — Calibración de UmbralSesgoDI V1");
sb.AppendLine();
sb.AppendLine("Estado: **evidencia de calibración — Fase 1.4-B, Paso 3-A (ejecución de D-032)**.");
sb.AppendLine("Método ejecutado exactamente como fue aprobado en");
sb.AppendLine("`DEFINICION_VALOR_UMBRAL_SESGO_DI_V1.md §1`: mediana de `|DI+-DI-|/(DI+ + DI-)` sobre");
sb.AppendLine("la zona `ADX < 25` del dataset congelado. Ninguna estrategia participó en este cálculo");
sb.AppendLine("(D-016). El valor obtenido no fue ajustado tras calcularse.");
sb.AppendLine();
sb.AppendLine("## Identidad");
sb.AppendLine();
sb.AppendLine($"- **Dataset**: `{nombreDataset}` (BTC/USDT Spot, Binance)");
sb.AppendLine($"- **Timeframes**: {string.Join(", ", timeframes)} (mismo subconjunto ya usado en Fase 1.4-A)");
sb.AppendLine($"- **Versión del cálculo**: D-032 v1 — mediana sobre zona `ADX < {umbralTendencia}`, `PeriodoAdx = {periodoAdx}`");
sb.AppendLine();
sb.AppendLine("## Método");
sb.AppendLine();
sb.AppendLine("- **Fórmula**: `SesgoDI = |DI+ - DI-| / (DI+ + DI-)`, calculada sobre suavizado de Wilder");
sb.AppendLine("  (idéntico a `ClasificadorAdxExperimental.cs`), restringida a ventanas con `ADX < 25`.");
sb.AppendLine("- **Tratamiento de división por cero**: ventanas con `TR_suavizado = 0` (rango verdadero");
sb.AppendLine("  nulo) o `DI+ + DI- = 0` se excluyen de la serie — misma regla ya usada en");
sb.AppendLine("  `ClasificadorAdxExperimental.cs`, no se introduce ninguna excepción nueva.");
sb.AppendLine("- **Valores faltantes**: la ventana de calentamiento (`2 × PeriodoAdx` primeras velas)");
sb.AppendLine("  no produce muestra — no hay ADX válido todavía, consistente con");
sb.AppendLine("  `PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §3`.");
sb.AppendLine("- **Definición exacta de muestra**: una muestra = una ventana (vela) con `ADX` válido y");
sb.AppendLine("  `ADX < 25`; el conjunto de muestras es toda la zona \"sin tendencia\" del timeframe.");
sb.AppendLine();
sb.AppendLine("## Resultado por timeframe");
sb.AppendLine();
sb.AppendLine("| Timeframe | Muestras (ADX<25) | Mediana SesgoDI |");
sb.AppendLine("|---|---|---|");

var medianasPorTf = new List<(string Tf, decimal? Mediana, int Muestras)>();

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
    var serie = CalibradorUmbralSesgoDI.CalcularSerie(velas, periodoAdx);
    var muestrasSinTendencia = serie.Count(m => m.Adx < umbralTendencia);
    var mediana = CalibradorUmbralSesgoDI.CalcularMedianaEnZonaSinTendencia(serie, umbralTendencia);

    medianasPorTf.Add((tf, mediana, muestrasSinTendencia));
    sb.AppendLine($"| {tf} | {muestrasSinTendencia} | {(mediana.HasValue ? mediana.Value.ToString("F6") : "n/a")} |");
    Console.WriteLine($"{tf}: muestras(ADX<25)={muestrasSinTendencia} mediana={(mediana.HasValue ? mediana.Value.ToString("F6") : "n/a")}");
}

// Valor final: mediana de las medianas por timeframe, para tener UN solo UmbralSesgoDI aplicable
// a los 13 timeframes (consistente con PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §4: periodo
// uniforme entre escalas, no diferenciado por timeframe). Mismo estadistico (mediana), aplicado
// una vez mas sobre los 6 valores ya obtenidos — no se elige un timeframe "representativo" a mano.
var medianasValidas = medianasPorTf.Where(m => m.Mediana.HasValue).Select(m => m.Mediana!.Value).OrderBy(v => v).ToList();
decimal? valorFinal = null;
if (medianasValidas.Count > 0)
{
    var mitad = medianasValidas.Count / 2;
    valorFinal = medianasValidas.Count % 2 == 1
        ? medianasValidas[mitad]
        : (medianasValidas[mitad - 1] + medianasValidas[mitad]) / 2m;
}

sb.AppendLine();
sb.AppendLine("## Valor propuesto de UmbralSesgoDI");
sb.AppendLine();
sb.AppendLine("Mediana de las 6 medianas por timeframe (mismo estadístico aplicado una segunda vez para");
sb.AppendLine("obtener un único valor aplicable a todos los timeframes, sin elegir un timeframe");
sb.AppendLine("\"representativo\" a mano — consistente con la decisión de periodo uniforme entre escalas,");
sb.AppendLine("`PARAMETRIZACION_CLASIFICADOR_REGIMEN_V1.md §4`).");
sb.AppendLine();
sb.AppendLine($"**UmbralSesgoDI (propuesto) = {(valorFinal.HasValue ? valorFinal.Value.ToString("F6") : "n/a — sin muestras válidas")}**");
sb.AppendLine();
sb.AppendLine("Estado: **PROPUESTO, no oficial** — este valor es la salida directa del método aprobado");
sb.AppendLine("(D-032), no ha sido editado ni redondeado. Su congelamiento formal como parte de");
sb.AppendLine("`ClasificadorRegimenV1` requiere aprobación explícita adicional (Paso 3-B).");

Console.WriteLine($"\nUmbralSesgoDI propuesto (mediana de medianas): {(valorFinal.HasValue ? valorFinal.Value.ToString("F6") : "n/a")}");

// Validacion posterior (D-032, "punto importante antes de ejecutar"): con el valor propuesto,
// medir cuantas ventanas quedan en cada uno de los 4 estados — observacion, no criterio de ajuste.
sb.AppendLine();
sb.AppendLine("---");
sb.AppendLine();
sb.AppendLine("## Validación posterior — distribución resultante con el valor propuesto");
sb.AppendLine();
sb.AppendLine("*(Observación, no criterio de ajuste — D-032: la distribución NO se usa para modificar");
sb.AppendLine("el valor obtenido por el método. Señales de alerta según");
sb.AppendLine("`DEFINICION_VALOR_UMBRAL_SESGO_DI_V1.md §3`: % Ambiguo < 1% o > 50%, o fragmentación");
sb.AppendLine("desproporcionada de Ambiguo frente a Lateral.)*");
sb.AppendLine();

if (valorFinal.HasValue)
{
    sb.AppendLine("| Timeframe | Alcista % | Bajista % | Lateral % | Ambiguo % | Ventanas |");
    sb.AppendLine("|---|---|---|---|---|---|");

    foreach (var tf in timeframes)
    {
        var rutaCsv = tf == "1m"
            ? Path.Combine(dirDatasets, "1m", $"{nombreDataset}_1m.csv")
            : Path.Combine(dirDatasets, tf, $"{nombreDataset}_{tf}.csv");
        if (!File.Exists(rutaCsv)) continue;

        var velas = LectorDerivado.Leer(rutaCsv);
        var serie = CalibradorUmbralSesgoDI.CalcularSerie(velas, periodoAdx);

        var alcista = serie.Count(m => m.Adx >= umbralTendencia && m.DiMas > m.DiMenos);
        var bajista = serie.Count(m => m.Adx >= umbralTendencia && m.DiMenos > m.DiMas);
        var empateConTendencia = serie.Count(m => m.Adx >= umbralTendencia && m.DiMas == m.DiMenos); // Ambiguo por empate exacto (§3, tratamiento de bordes)
        var lateral = serie.Count(m => m.Adx < umbralTendencia && m.SesgoDiRelativo < valorFinal.Value);
        var ambiguo = serie.Count(m => m.Adx < umbralTendencia && m.SesgoDiRelativo >= valorFinal.Value) + empateConTendencia;
        var total = serie.Count;

        if (total == 0) continue;

        sb.AppendLine($"| {tf} | {alcista * 100m / total:F2}% | {bajista * 100m / total:F2}% | {lateral * 100m / total:F2}% | {ambiguo * 100m / total:F2}% | {total} |");
        Console.WriteLine($"{tf}: Alcista={alcista * 100m / total:F2}% Bajista={bajista * 100m / total:F2}% Lateral={lateral * 100m / total:F2}% Ambiguo={ambiguo * 100m / total:F2}%");
    }
}
else
{
    sb.AppendLine("No aplica — no se obtuvo un valor de UmbralSesgoDI (sin muestras válidas).");
}

var rutaSalida = Path.Combine(dirLaboratorio, "analisis_escenarios_mercado", "RESULTADO_CALIBRACION_UMBRAL_SESGO_DI_V1.md");
var contenido = sb.ToString();
File.WriteAllText(rutaSalida, contenido);

var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contenido)));
Console.WriteLine($"\nInforme escrito en: {rutaSalida}");
Console.WriteLine($"SHA256 del informe: {hash}");
