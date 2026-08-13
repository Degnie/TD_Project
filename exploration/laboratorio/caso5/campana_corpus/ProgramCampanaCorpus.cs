using TD_Project.Caso5;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Exploration;
using TD_Project.Protocolo;

// spec: PROPUESTA_CAMPANA_CORPUS_CASO5C_V1.md, ESPECIFICACION_IMPLEMENTACION_CAMPANA_CORPUS_
// CASO5C_V1.md, PROPUESTA_EXPANSION_CORPUS_CASO5C_V2.md, ESPECIFICACION_IMPLEMENTACION_EXPANSION_
// CORPUS_CASO5C_V2.md — ejecutable de campana separado (no extiende caso5/Program.cs). Genera
// evidencia usando exclusivamente ComparadorGestores (Caso 5B) + PersistidorComparaciones (Caso 5C
// Capa 1), sin modificar ninguno de los dos. No analiza, no rankea, no selecciona.

// spec: V1 §3 — matriz declarada por completo antes de ejecutar. No se agrega, quita, ni reordena
// ninguna combinacion en funcion de un resultado intermedio.
string[] timeframes = { "15m", "1h", "1D" };

(string Nombre, string[] Parametros, Func<Action<InfoOperacionResuelta>?, IStrategy> Crear)[] estrategiasV1 =
{
    ("Tres Mosqueteros", new[] { "maxMartingalas=2" },
        onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp)),
    ("Ema Cross", new[] { "rapida=5", "lenta=20" },
        onOp => new EstrategiaEmaCross(periodoEmaCorta: 5, periodoEmaLarga: 20, onOperacionResuelta: onOp)),
};

// spec: V2 §2, sub-campana A — 4 estrategias no cubiertas en V1, parametros ya congelados en
// caso3/TestsCaso3.cs, caso3/TestsEstrategiaNeutral.cs, caso3/TestsEstrategiaVolumenBreakout.cs,
// Fase15.cs — ninguno calibrado aqui (D-030).
(string Nombre, string[] Parametros, Func<Action<InfoOperacionResuelta>?, IStrategy> Crear)[] estrategiasNuevas =
{
    ("ZScore Reversion", new[] { "ventana=5", "umbralEntrada=2.0", "umbralSalida=0.5" },
        onOp => new EstrategiaZScoreReversion(ventana: 5, umbralEntrada: 2.0m, umbralSalida: 0.5m, onOperacionResuelta: onOp)),
    ("Neutral", new[] { "ciclo=10" },
        onOp => new EstrategiaNeutral(ciclo: 10, onOperacionResuelta: onOp)),
    ("Volumen Breakout", Array.Empty<string>(),
        onOp => new EstrategiaVolumenBreakout(onOperacionResuelta: onOp)),
    ("Mhi Mayoria", new[] { "maxMartingalas=2" },
        onOp => new EstrategiaMhiMayoria(maxMartingalas: 2, onOperacionResuelta: onOp)),
};

IReadOnlyList<IGestorRiesgo> Gestores() => new IGestorRiesgo[]
{
    new GestorFixedFractional(0.1m),
    new GestorFixedRisk(50m),
    new GestorVolatilitySizing(20, 0.1m, 2m),
};

var raiz = AppContext.BaseDirectory;
var dirDatasets = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "..", "datasets", "reales", "BTCUSDT"));
var dirResultados = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "resultados"));
var instrumento = new Instrumento("BTCUSDT", 0.1m);
var costes = new ConfiguracionCostes(0.001m, 0.001m);

// spec: DECISIONES_DIVERSIDAD_EVIDENCIA_CASO5C_V1.md (D-121, Via B), ESPECIFICACION_
// IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md §5 — Sub-campana D. EjecutorProtocolo (Caso 1,
// congelado) usa el mismo token de timeframe para la subcarpeta y el sufijo del archivo; las
// carpetas congeladas del dataset 2022 llevan sufijo "_2022" en el nombre de carpeta pero no en el
// archivo, asi que se usa una vista de compatibilidad sin sufijo (copia verificada por SHA-256
// contra el artefacto congelado, no un segundo dataset) en datasets/reales/BTCUSDT_2022/{tf}/.
var dirDatasets2022 = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "..", "datasets", "reales", "BTCUSDT_2022"));
var nombreDataset2022 = "BTCUSDT_2022-01-01_2023-01-01";

// P1/P2/P3 — verificacion estructural previa. Si falla, la campana no ejecuta ninguna corrida real.
var (totalPrevio, pasaronPrevio, detallesPrevio) = TestsCampanaCorpus.VerificarEstructura(
    estrategiasV1.Length, estrategiasNuevas.Length, timeframes.Length, Gestores().Count);
Console.WriteLine("=== Verificacion estructural previa (P1/P2/P3) ===");
foreach (var d in detallesPrevio)
    Console.WriteLine($"  {d}");
if (pasaronPrevio != totalPrevio)
{
    Console.WriteLine("Verificacion estructural fallida — campana no ejecutada.");
    Environment.Exit(1);
}
Console.WriteLine();

List<string> EjecutarMatriz(
    (string Nombre, string[] Parametros, Func<Action<InfoOperacionResuelta>?, IStrategy> Crear)[] estrategias,
    string dirDatasetsUsado, string nombreDatasetUsado)
{
    var carpetas = new List<string>();
    foreach (var estrategia in estrategias)
    {
        foreach (var timeframe in timeframes)
        {
            var entradaBase = new EntradaProtocolo(
                Estrategia: estrategia.Nombre,
                VersionEstrategia: "1.0",
                Parametros: estrategia.Parametros,
                CrearEstrategia: estrategia.Crear,
                Timeframes: new[] { timeframe },
                DirDatasets: dirDatasetsUsado,
                NombreDataset: nombreDatasetUsado,
                CapitalInicial: 1000m,
                Instrumento: instrumento,
                Costes: costes);

            var resultado = ComparadorGestores.Comparar(entradaBase, Gestores());
            var carpeta = PersistidorComparaciones.Persistir(dirResultados, resultado);
            carpetas.Add(carpeta);
            Console.WriteLine($"  {estrategia.Nombre} / {timeframe} -> {carpeta}");
        }
    }
    return carpetas;
}

const string nombreDataset2024 = "BTCUSDT_2024-01-02_2025-01-02";

Console.WriteLine("=== Sub-campana V1 — matriz original (2 estrategias x 3 timeframes) ===");
var carpetasV1 = EjecutarMatriz(estrategiasV1, dirDatasets, nombreDataset2024);

Console.WriteLine();
Console.WriteLine("=== Sub-campana A — cobertura de estrategias (4 estrategias x 3 timeframes) ===");
var carpetasA = EjecutarMatriz(estrategiasNuevas, dirDatasets, nombreDataset2024);

Console.WriteLine();
Console.WriteLine("=== Sub-campana B — repeticion exacta de la matriz V1 ===");
var carpetasB = EjecutarMatriz(estrategiasV1, dirDatasets, nombreDataset2024);

Console.WriteLine();
Console.WriteLine("=== Sub-campana C — evidencia parcial (dataset inexistente, produce estado no-Success) ===");
var entradaFallo = new EntradaProtocolo(
    Estrategia: "Tres Mosqueteros", VersionEstrategia: "1.0", Parametros: new[] { "maxMartingalas=2" },
    CrearEstrategia: onOp => new EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
    Timeframes: new[] { "1D" }, DirDatasets: dirDatasets,
    NombreDataset: "DatasetInexistente_ParaCorpusDeFallo",
    CapitalInicial: 1000m, Instrumento: instrumento, Costes: costes);
var resultadoFallo = ComparadorGestores.Comparar(entradaFallo, Gestores());
var carpetaFallo = PersistidorComparaciones.Persistir(dirResultados, resultadoFallo);
Console.WriteLine($"  Tres Mosqueteros / 1D (dataset inexistente) -> {carpetaFallo}");

Console.WriteLine();
var totalNuevas = carpetasA.Count + carpetasB.Count + 1;
Console.WriteLine($"Comparaciones V2 generadas: {totalNuevas} (esperado: {estrategiasNuevas.Length * timeframes.Length + estrategiasV1.Length * timeframes.Length + 1}).");
Console.WriteLine($"Comparaciones totales de esta ejecucion (incluye repeticion de V1): {carpetasV1.Count + totalNuevas}.");

// P4 — cobertura de expansion: 12 (A) + 6 (B) + 1 (C) = 19 carpetas nuevas, todas existentes, sin duplicados.
var carpetasNuevas = carpetasA.Concat(carpetasB).Append(carpetaFallo).ToList();
var esperadoNuevas = estrategiasNuevas.Length * timeframes.Length + estrategiasV1.Length * timeframes.Length + 1;
if (carpetasNuevas.Count != esperadoNuevas)
{
    Console.WriteLine("P4 FALLA — cantidad de comparaciones V2 persistidas no coincide con la matriz declarada.");
    Environment.Exit(1);
}
if (carpetasNuevas.Distinct().Count() != carpetasNuevas.Count || carpetasNuevas.Any(c => !Directory.Exists(c)))
{
    Console.WriteLine("P4 FALLA — alguna carpeta de comparacion V2 no existe o esta duplicada.");
    Environment.Exit(1);
}
Console.WriteLine("P4 — cobertura de expansion V2 verificada: cada comparacion declarada tiene su carpeta persistida.");

// P5 — la sub-campana C debe reflejar un estado distinto de Success en las 3 filas (dataset
// inexistente produce Incomplete, no Failed — EjecutorProtocolo.cs:22 distingue ambos; lo que
// importa para "evidencia parcial" es que no sea Success), con Metricas null en las 3 — confirma
// que la evidencia parcial quedo representada, no un Success accidental.
if (resultadoFallo.Filas.Count != 3
    || resultadoFallo.Filas.Any(f => f.Estado == TD_Project.Protocolo.EstadoCorridaTimeframe.Success)
    || resultadoFallo.Filas.Any(f => f.Metricas is not null))
{
    Console.WriteLine("P5 FALLA — la comparacion de la sub-campana C no quedo en un estado no-Success/Metricas=null en las 3 filas.");
    Environment.Exit(1);
}
Console.WriteLine($"P5 — evidencia parcial verificada: sub-campana C persistio Estado={resultadoFallo.Filas[0].Estado} en las 3 filas, sin metricas.");

// spec: ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_TEMPORAL_CASO5C_V1.md §5 — Sub-campana D.
// Autorizacion explicita: BTCUSDT 2024-2025 vs BTCUSDT 2022-2023, misma matriz completa (6
// estrategias x 3 timeframes x 3 gestores = 18 comparaciones). No analiza, no recomienda, no
// descarta, no rankea — genera y persiste evidencia via ComparadorGestores/PersistidorComparaciones
// sin modificar ninguno de los dos (mismo principio que V1/V2).
Console.WriteLine();
Console.WriteLine("=== Sub-campana D — diversidad temporal, dataset 2022-2023 (6 estrategias x 3 timeframes) ===");
var estrategiasTodas = estrategiasV1.Concat(estrategiasNuevas).ToArray();
var carpetasD = EjecutarMatriz(estrategiasTodas, dirDatasets2022, nombreDataset2022);

// P6 — verificacion de identidad experimental y reproducibilidad (requisito explicito del usuario:
// "si verificar identidad experimental; si confirmar reproducibilidad"). Ejecuta EjecutorProtocolo
// directamente (fuera de ComparadorGestores, sin modificarlo) para la primera combinacion de la
// matriz D, una vez por dataset, y compara IdentidadExperimentoCompleta: el dataset debe ser el
// unico eje que cambia el HashCompuesto (mismo criterio D-113 aplicado a nivel de campana), y dos
// ejecuciones identicas sobre el mismo dataset deben producir el mismo HashCompuesto (reproducibilidad).
var entrada2024 = new EntradaProtocolo(
    Estrategia: estrategiasV1[0].Nombre, VersionEstrategia: "1.0", Parametros: estrategiasV1[0].Parametros,
    CrearEstrategia: estrategiasV1[0].Crear, Timeframes: new[] { timeframes[0] },
    DirDatasets: dirDatasets, NombreDataset: nombreDataset2024,
    CapitalInicial: 1000m, Instrumento: instrumento, Costes: costes);
var entrada2022 = entrada2024 with { DirDatasets = dirDatasets2022, NombreDataset = nombreDataset2022 };

var resProtocolo2024 = TD_Project.Protocolo.EjecutorProtocolo.Ejecutar(entrada2024);
var resProtocolo2022 = TD_Project.Protocolo.EjecutorProtocolo.Ejecutar(entrada2022);
var resProtocolo2022Repetido = TD_Project.Protocolo.EjecutorProtocolo.Ejecutar(entrada2022);

if (resProtocolo2024.Identidad.HashCompuesto == resProtocolo2022.Identidad.HashCompuesto)
{
    Console.WriteLine("P6 FALLA — HashCompuesto identico entre datasets distintos (2024-2025 vs 2022-2023); el dataset deberia ser un eje que cambia el hash.");
    Environment.Exit(1);
}
if (resProtocolo2024.Identidad.HashConfiguracionEconomica != resProtocolo2022.Identidad.HashConfiguracionEconomica)
{
    Console.WriteLine("P6 FALLA — HashConfiguracionEconomica distinto entre datasets; la configuracion economica no deberia depender del periodo temporal.");
    Environment.Exit(1);
}
if (resProtocolo2022.Identidad.HashCompuesto != resProtocolo2022Repetido.Identidad.HashCompuesto)
{
    Console.WriteLine("P6 FALLA — dos ejecuciones identicas sobre el dataset 2022-2023 no produjeron el mismo HashCompuesto (reproducibilidad rota).");
    Environment.Exit(1);
}
Console.WriteLine("P6 — identidad experimental verificada: HashCompuesto distingue el dataset, HashConfiguracionEconomica no depende del periodo, y el dataset 2022-2023 es reproducible entre corridas.");

if (carpetasD.Count != estrategiasTodas.Length * timeframes.Length || carpetasD.Any(c => !Directory.Exists(c)) || carpetasD.Distinct().Count() != carpetasD.Count)
{
    Console.WriteLine("P7 FALLA — Sub-campana D no persistio la cantidad esperada de comparaciones (18) o alguna carpeta no existe/esta duplicada.");
    Environment.Exit(1);
}
Console.WriteLine($"P7 — Sub-campana D verificada: {carpetasD.Count} comparaciones persistidas (esperado: {estrategiasTodas.Length * timeframes.Length}).");

// spec: ESPECIFICACION_IMPLEMENTACION_DIVERSIDAD_INSTRUMENTO_CASO5C_V2.md §5 (D-125) — Sub-campana
// E. Autorizacion explicita: BTCUSDT vs ETHUSDT, mismo rango 2024-01-02_2025-01-02 (dimension
// aislada: instrumento, no tiempo), misma matriz completa (6 estrategias x 3 timeframes x 3
// gestores = 18 comparaciones). Ningun parametro economico, gestor, ni estrategia cambia — mismo
// principio que Sub-campana D.
Console.WriteLine();
Console.WriteLine("=== Sub-campana E — diversidad de instrumento, dataset ETHUSDT (6 estrategias x 3 timeframes) ===");
var dirDatasetsEth = Path.GetFullPath(Path.Combine(raiz, "..", "..", "..", "..", "..", "datasets", "reales", "ETHUSDT"));
var nombreDatasetEth = "ETHUSDT_2024-01-02_2025-01-02";
var carpetasE = EjecutarMatriz(estrategiasTodas, dirDatasetsEth, nombreDatasetEth);

// P8 — identidad experimental y reproducibilidad para el nuevo instrumento (mismo criterio que P6
// para Sub-campana D): el dataset (ahora tambien el instrumento) debe ser el eje que cambia el
// HashCompuesto; HashConfiguracionEconomica no debe depender del instrumento (parametros
// economicos identicos, D-030/D-125); dos ejecuciones sobre el mismo dataset ETHUSDT deben producir
// el mismo HashCompuesto.
var entradaEth = entrada2024 with { DirDatasets = dirDatasetsEth, NombreDataset = nombreDatasetEth };
var resProtocoloEth = TD_Project.Protocolo.EjecutorProtocolo.Ejecutar(entradaEth);
var resProtocoloEthRepetido = TD_Project.Protocolo.EjecutorProtocolo.Ejecutar(entradaEth);

if (resProtocolo2024.Identidad.HashCompuesto == resProtocoloEth.Identidad.HashCompuesto)
{
    Console.WriteLine("P8 FALLA — HashCompuesto identico entre instrumentos distintos (BTCUSDT vs ETHUSDT); el dataset deberia ser un eje que cambia el hash.");
    Environment.Exit(1);
}
if (resProtocolo2024.Identidad.HashConfiguracionEconomica != resProtocoloEth.Identidad.HashConfiguracionEconomica)
{
    Console.WriteLine("P8 FALLA — HashConfiguracionEconomica distinto entre instrumentos; la configuracion economica no deberia depender del instrumento (D-030/D-125).");
    Environment.Exit(1);
}
if (resProtocoloEth.Identidad.HashCompuesto != resProtocoloEthRepetido.Identidad.HashCompuesto)
{
    Console.WriteLine("P8 FALLA — dos ejecuciones identicas sobre el dataset ETHUSDT no produjeron el mismo HashCompuesto (reproducibilidad rota).");
    Environment.Exit(1);
}
Console.WriteLine("P8 — identidad experimental verificada: HashCompuesto distingue el instrumento, HashConfiguracionEconomica no depende del instrumento, y el dataset ETHUSDT es reproducible entre corridas.");

if (carpetasE.Count != estrategiasTodas.Length * timeframes.Length || carpetasE.Any(c => !Directory.Exists(c)) || carpetasE.Distinct().Count() != carpetasE.Count)
{
    Console.WriteLine("P9 FALLA — Sub-campana E no persistio la cantidad esperada de comparaciones (18) o alguna carpeta no existe/esta duplicada.");
    Environment.Exit(1);
}
Console.WriteLine($"P9 — Sub-campana E verificada: {carpetasE.Count} comparaciones persistidas (esperado: {estrategiasTodas.Length * timeframes.Length}).");
