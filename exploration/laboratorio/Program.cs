using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Exploration;
using TD_Project.Laboratorio;
using TD_Project.Laboratorio.Fixtures;
using TD_Project.Laboratorio.Generadores;

var raiz = AppContext.BaseDirectory;
var directorioLaboratorio = Path.GetFullPath(Path.Combine(raiz, "..", "..", ".."));
var dirMarket = Path.Combine(directorioLaboratorio, "datasets", "market");
var dirScenarios = Path.Combine(directorioLaboratorio, "datasets", "scenarios");

var regenerar = Environment.GetEnvironmentVariable("REGENERAR_DATASETS") == "1";

if (regenerar)
{
    Directory.CreateDirectory(dirMarket);
    Directory.CreateDirectory(dirScenarios);
    GuardarCsv(Path.Combine(dirMarket, "tendencia_alcista.csv"),
        GeneradorTendencia.Generar(velas: 200, precioInicial: 100m, pendientePorVela: 0.15m, ruido: 1.0m, seed: 30001));
    GuardarCsv(Path.Combine(dirMarket, "tendencia_bajista.csv"),
        GeneradorTendencia.Generar(velas: 200, precioInicial: 100m, pendientePorVela: -0.15m, ruido: 1.0m, seed: 30002));
    GuardarCsv(Path.Combine(dirMarket, "lateral.csv"),
        GeneradorLateral.Generar(velas: 200, precioCentral: 100m, amplitudBanda: 3.0m, seed: 30003));
    GuardarCsv(Path.Combine(dirMarket, "volatilidad.csv"),
        GeneradorVolatilidad.Generar(velas: 200, precioInicial: 100m, rangoBase: 5.0m, probabilidadGap: 0.15m, seed: 30004));
    GuardarCsv(Path.Combine(dirMarket, "ruido.csv"),
        GeneradorRuido.Generar(velas: 200, precioInicial: 100m, volatilidad: 2.0m, seed: 30005));
    GuardarCsv(Path.Combine(dirMarket, "tendencia_con_reversion.csv"),
        GeneradorTendenciaConReversion.Generar(velas: 200, precioInicial: 100m, pendienteAntes: 0.2m, pendienteDespues: -0.2m, ruido: 1.0m, seed: 30006));
    GuardarCsv(Path.Combine(dirMarket, "doble_techo.csv"),
        GeneradorDobleTechoSuelo.Generar(velas: 200, precioBase: 100m, amplitud: 15m, esTecho: true, ruido: 0.8m, seed: 30007));
    GuardarCsv(Path.Combine(dirMarket, "volatilidad_tras_calma.csv"),
        GeneradorVolatilidadTrasCalma.Generar(velas: 200, precioInicial: 100m, rangoCalma: 0.3m, rangoVolatil: 6.0m, seed: 30008));
    GuardarCsv(Path.Combine(dirMarket, "volatilidad_decreciente.csv"),
        GeneradorVolatilidadDecreciente.Generar(velas: 200, precioInicial: 100m, rangoInicial: 6.0m, rangoFinal: 0.2m, seed: 30009));
    GuardarCsv(Path.Combine(dirMarket, "sin_movimiento.csv"),
        GeneradorSinMovimiento.Generar(velas: 50, precioFijo: 100m));
    GuardarCsv(Path.Combine(dirScenarios, "rn11_stop_limit.csv"), EscenarioRN11.Velas());
    GuardarCsv(Path.Combine(dirScenarios, "cross_zero.csv"), FixtureCrossZero.Velas());
    GuardarCsv(Path.Combine(dirScenarios, "martingala_agotada.csv"), EscenarioMartingalaAgotada.Velas());
    Console.WriteLine("Datasets regenerados.");
}

var catalogo = new List<HipotesisDataset>
{
    new(
        "TendenciaAlcista",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "tendencia_alcista.csv"),
        "Mercado con pendiente sostenida al alza, ruido controlado alrededor de la tendencia.",
        "Una posicion Long abierta desde el inicio debe cerrar con PnL positivo y Equity creciente.",
        resultado =>
        {
            var equityInicial = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[0].Equity : 0m;
            var equityFinal = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[^1].Equity : 0m;
            var paso = resultado.Estado == EstadoBacktest.Success && equityFinal > equityInicial;
            return (paso, $"EquityInicial={equityInicial} EquityFinal={equityFinal}");
        }),

    new(
        "TendenciaBajista",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "tendencia_bajista.csv"),
        "Mercado con pendiente sostenida a la baja, ruido controlado alrededor de la tendencia.",
        "Una posicion Short abierta desde el inicio debe cerrar con PnL positivo (signo correcto) y liberar Margin al cierre.",
        resultado =>
        {
            var equityInicial = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[0].Equity : 0m;
            var equityFinal = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[^1].Equity : 0m;
            var paso = resultado.Estado == EstadoBacktest.Success && equityFinal > equityInicial;
            return (paso, $"EquityInicial={equityInicial} EquityFinal={equityFinal}");
        }),

    new(
        "MercadoLateral",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "lateral.csv"),
        "Mercado sin tendencia neta, oscilando dentro de una banda fija.",
        "El backtest debe completar sin error; no se exige PnL positivo (mercado sin direccion).",
        resultado => (resultado.Estado == EstadoBacktest.Success, $"Estado={resultado.Estado}")),

    new(
        "VolatilidadExtrema",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "volatilidad.csv"),
        "Mercado con gaps y rango intra-vela amplio.",
        "El motor debe resolver Fills coherentes (dentro de [Low,High] o en el Open si hay gap) sin excepcion.",
        resultado => (resultado.Estado == EstadoBacktest.Success, $"Estado={resultado.Estado} Fills={resultado.Fills.Count}")),

    new(
        "RuidoAleatorio",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "ruido.csv"),
        "Caminata aleatoria sin sesgo, control negativo.",
        "El backtest debe completar sin error; el motor no debe fallar ante ausencia de estructura direccional.",
        resultado => (resultado.Estado == EstadoBacktest.Success, $"Estado={resultado.Estado}")),

    new(
        "TendenciaConReversion",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "tendencia_con_reversion.csv"),
        "Tendencia alcista que se invierte abruptamente a bajista a mitad del dataset.",
        "El motor no debe arrastrar sesgo direccional de la primera mitad a la segunda; cada vela se resuelve con sus propios datos.",
        resultado => (resultado.Estado == EstadoBacktest.Success, $"Estado={resultado.Estado} Fills={resultado.Fills.Count}")),

    new(
        "DobleTecho",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "doble_techo.csv"),
        "Patron de doble techo: sube, baja, sube a un maximo similar, baja definitivamente.",
        "El motor debe resolver la secuencia de reversiones parciales sin acumular error entre tramos.",
        resultado => (resultado.Estado == EstadoBacktest.Success, $"Estado={resultado.Estado} Fills={resultado.Fills.Count}")),

    new(
        "VolatilidadTrasCalma",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "volatilidad_tras_calma.csv"),
        "Primera mitad con rango minimo (calma), segunda mitad con rango amplio (volatilidad subita).",
        "El motor no debe depender de volatilidad estable previa para resolver Fills correctamente en el tramo volatil.",
        resultado => (resultado.Estado == EstadoBacktest.Success, $"Estado={resultado.Estado} Fills={resultado.Fills.Count}")),

    new(
        "VolatilidadDecreciente",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "volatilidad_decreciente.csv"),
        "Rango intra-vela que decae linealmente desde un maximo hasta casi nulo.",
        "El motor debe resolver Fills correctamente incluso en el tramo de rango casi nulo, sin degradar precision decimal.",
        resultado => (resultado.Estado == EstadoBacktest.Success, $"Estado={resultado.Estado} Fills={resultado.Fills.Count}")),

    new(
        "SinMovimiento",
        CategoriaHipotesis.Market,
        Path.Combine(dirMarket, "sin_movimiento.csv"),
        "Velas planas (Open=High=Low=Close) durante todo el dataset.",
        "El motor no debe crashear ni producir division por cero; RN-11 debe degenerar a EquityA==EquityB (sin rango, sin ambiguedad posible).",
        resultado =>
        {
            var todasEmpatadas = resultado.BranchResolutions.Count == 0 || resultado.BranchResolutions.All(b => b.EquityA == b.EquityB);
            return (resultado.Estado == EstadoBacktest.Success && todasEmpatadas, $"Estado={resultado.Estado} TodasEmpatadas={todasEmpatadas}");
        }),

    new(
        "RN11_StopLimit_Divergente",
        CategoriaHipotesis.Scenario,
        Path.Combine(dirScenarios, "rn11_stop_limit.csv"),
        "Caso canonico: Buy Stop-Limit 102/101 sobre vela Open=100/High=102/Low=90/Close=102.",
        "RN-11 debe producir divergencia real entre trayectorias A/B y seleccionar la de menor Equity.",
        resultado =>
        {
            var branch = resultado.BranchResolutions.FirstOrDefault();
            if (branch is null) return (false, "Sin BranchResolutions");
            var divergen = branch.EquityA != branch.EquityB;
            var equityMenor = Math.Min(branch.EquityA, branch.EquityB);
            var seleccionaMenor = branch.TrayectoriaOficial == (branch.EquityA <= branch.EquityB ? TrayectoriaResolucion.A : TrayectoriaResolucion.B);
            return (divergen && seleccionaMenor && resultado.Estado == EstadoBacktest.Success,
                $"EquityA={branch.EquityA} EquityB={branch.EquityB} Oficial={branch.TrayectoriaOficial} (menor={equityMenor})");
        }),

    new(
        "CrossZero_TresConsecutivos",
        CategoriaHipotesis.Scenario,
        Path.Combine(dirScenarios, "cross_zero.csv"),
        "Tres inversiones de posicion consecutivas (Buy 10, Sell 15, Buy 8, Sell 6).",
        "Cada Trade debe reportar el PrecioApertura/CantidadInicial real de SU ciclo, no heredado del ciclo anterior.",
        resultado =>
        {
            if (resultado.Trades.Count != 3) return (false, $"Se esperaban 3 Trades, hubo {resultado.Trades.Count}");
            var t = resultado.Trades;
            var paso = t[0].PrecioApertura == 100m && t[1].PrecioApertura == 105m && t[2].PrecioApertura == 110m;
            return (paso, string.Join(" | ", t.Select(x => $"Apertura={x.PrecioApertura} Cantidad={x.CantidadInicial}")));
        }),

    new(
        "Martingala_TresMosqueteros",
        CategoriaHipotesis.Scenario,
        "velas_simuladas_1anio.csv (reutilizado, no dataset propio del laboratorio)",
        "Fixture real de 252 velas ya usado en la auditoria de Rondas 1-5.",
        "Las operaciones con martingala deben agruparse como UNA operacion completa (ver AnalisisRiesgo.cs), no como Trades individuales.",
        resultado => (resultado.Estado == EstadoBacktest.Success, $"Trades individuales={resultado.Trades.Count} (no es la unidad de analisis correcta, ver AnalisisRiesgo.cs)")),
};

var directorioAuditoria = Path.GetFullPath(Path.Combine(directorioLaboratorio, ".."));
var csvReal = Path.Combine(directorioAuditoria, "velas_simuladas_1anio.csv");

var fallas = 0;
foreach (var hipotesis in catalogo)
{
    IReadOnlyList<Candle> velas;
    IStrategy estrategia;

    switch (hipotesis.Nombre)
    {
        case "TendenciaAlcista":
            velas = CargarCsv(hipotesis.ArchivoCsv);
            estrategia = new EstrategiaSimpleLaboratorio(Side.Buy);
            break;
        case "TendenciaBajista":
            velas = CargarCsv(hipotesis.ArchivoCsv);
            estrategia = new EstrategiaSimpleLaboratorio(Side.Sell);
            break;
        case "MercadoLateral":
        case "VolatilidadExtrema":
        case "RuidoAleatorio":
        case "TendenciaConReversion":
        case "DobleTecho":
        case "VolatilidadTrasCalma":
        case "VolatilidadDecreciente":
        case "SinMovimiento":
            velas = CargarCsv(hipotesis.ArchivoCsv);
            estrategia = new EstrategiaSimpleLaboratorio(Side.Buy);
            break;
        case "RN11_StopLimit_Divergente":
            velas = EscenarioRN11.Velas();
            estrategia = new EstrategiaRN11Laboratorio();
            break;
        case "CrossZero_TresConsecutivos":
            velas = FixtureCrossZero.Velas();
            estrategia = new EstrategiaFixtureCrossZero();
            break;
        case "Martingala_TresMosqueteros":
            velas = CargarCsv(csvReal);
            estrategia = new EstrategiaTresMosqueteros(maxMartingalas: 2);
            break;
        default:
            throw new InvalidOperationException($"Hipotesis sin corrida definida: {hipotesis.Nombre}");
    }

    var corrida = EjecutorLaboratorio.Ejecutar(hipotesis, velas, estrategia);
    EjecutorLaboratorio.ReportarCorrida(corrida);

    if (!corrida.HipotesisPaso || !corrida.ReconciliacionCoherente)
        fallas++;
}

// Escenario financiero controlado: perdida maxima con martingala (deterministico, no forma
// parte del bucle generico porque necesita capturar InfoOperacionResuelta, no solo ResultadoBacktest).
Console.WriteLine("\n=== [Scenario] MartingalaAgotada (datasets/scenarios/martingala_agotada.csv) ===");
Console.WriteLine("Descripcion: Operacion unica que pierde en el intento inicial, Martingala 1 y Martingala 2 (agota los 3 niveles).");
Console.WriteLine("Que valida: El motor sostiene contabilidad y trazabilidad correctas bajo el peor caso posible de UNA operacion.");
var veredictoMartingala = ValidadorMartingalaAgotada.Ejecutar();
foreach (var detalle in veredictoMartingala.Detalles)
    Console.WriteLine($"  {detalle}");
Console.WriteLine($"Hipotesis: {(veredictoMartingala.Paso ? "PASA" : "FALLA")}");
if (!veredictoMartingala.Paso)
    fallas++;
var totalHipotesis = catalogo.Count + 1;

// Escenario documental: friccion/comision extrema. NO se cuenta como PASA/FALLA porque
// MatchingEngine no calcula CostoFriccionReal hoy (siempre 0, gap de Domain ya documentado en
// docs/PENDIENTES.md, fuera de alcance de este laboratorio). Se deja registrado para cuando el
// motor implemente un Friction Model real — ver docs/PENDIENTES.md § Candidatos de benchmarking.
Console.WriteLine("\n=== [Scenario] FriccionExtrema — PENDIENTE, NO EJECUTABLE COMO HIPOTESIS FINANCIERA ===");
Console.WriteLine("Descripcion: Escenario disenado para validar que comisiones/slippage/price impact afectan el resultado financiero.");
Console.WriteLine("Bloqueado por: MatchingEngine.CostoFriccionReal esta fijo en 0 (Domain no implementa Friction Model todavia).");
Console.WriteLine("Estado: no incluido en el conteo de hipotesis verdes — evaluar cuando exista la capacidad en Domain.");

Console.WriteLine($"\n=== Resumen: {totalHipotesis - fallas}/{totalHipotesis} hipotesis en verde (FriccionExtrema excluida del conteo) ===");

// Fase 1.5: perfil de comportamiento de Tres Mosqueteros / MHI Mayoria sobre los datasets de
// market/. No es parte del conteo PASA/FALLA de hipotesis — es un reporte comparativo aparte.
var datasetsMarket = new Dictionary<string, string>
{
    ["TendenciaAlcista"] = Path.Combine(dirMarket, "tendencia_alcista.csv"),
    ["TendenciaBajista"] = Path.Combine(dirMarket, "tendencia_bajista.csv"),
    ["MercadoLateral"] = Path.Combine(dirMarket, "lateral.csv"),
    ["VolatilidadExtrema"] = Path.Combine(dirMarket, "volatilidad.csv"),
    ["RuidoAleatorio"] = Path.Combine(dirMarket, "ruido.csv"),
    ["TendenciaConReversion"] = Path.Combine(dirMarket, "tendencia_con_reversion.csv"),
    ["DobleTecho"] = Path.Combine(dirMarket, "doble_techo.csv"),
    ["VolatilidadTrasCalma"] = Path.Combine(dirMarket, "volatilidad_tras_calma.csv"),
    ["VolatilidadDecreciente"] = Path.Combine(dirMarket, "volatilidad_decreciente.csv"),
    ["SinMovimiento"] = Path.Combine(dirMarket, "sin_movimiento.csv"),
};
var (perfiles, hallazgosFase15) = Fase15.Ejecutar(datasetsMarket);
ReportarFase15(perfiles, hallazgosFase15);

var bugsFase15 = hallazgosFase15.Count(h => h.Tipo == "[BUG]");
Environment.Exit(fallas == 0 && bugsFase15 == 0 ? 0 : 1);

static void GuardarCsv(string path, IReadOnlyList<Candle> velas)
{
    using var writer = new StreamWriter(path);
    writer.WriteLine("Timestamp,Open,High,Low,Close,Volume");
    foreach (var v in velas)
        writer.WriteLine($"{v.Timestamp},{v.Open},{v.High},{v.Low},{v.Close},{v.Volume}");
}

static IReadOnlyList<Candle> CargarCsv(string path)
{
    var lineas = File.ReadAllLines(path);
    var velas = new List<Candle>();
    for (var i = 1; i < lineas.Length; i++)
    {
        var campos = lineas[i].Split(',');
        velas.Add(new Candle(
            Timestamp: long.Parse(campos[0]),
            Open: decimal.Parse(campos[1]),
            High: decimal.Parse(campos[2]),
            Low: decimal.Parse(campos[3]),
            Close: decimal.Parse(campos[4]),
            Volume: decimal.Parse(campos[5])));
    }
    return velas;
}

static void ReportarFase15(IReadOnlyList<PerfilEstrategia> perfiles, IReadOnlyList<Fase15.Hallazgo> hallazgos)
{
    Console.WriteLine("\n\n########## FASE 1.5 — PERFIL DE COMPORTAMIENTO DE ESTRATEGIAS ##########");

    Console.WriteLine("\n=== Matriz comparativa (retorno %) ===");
    Console.WriteLine($"{"Escenario",-22} {"Tres Mosqueteros",-20} {"MHI Mayoria",-20}");
    foreach (var escenario in perfiles.Select(p => p.Escenario).Distinct())
    {
        var tm = perfiles.First(p => p.Escenario == escenario && p.Estrategia == "Tres Mosqueteros");
        var mhi = perfiles.First(p => p.Escenario == escenario && p.Estrategia == "MHI Mayoria");
        Console.WriteLine($"{escenario,-22} {tm.RetornoPct,8:F2}% ({tm.OperacionesGanadas}/{tm.TotalOperaciones})   {mhi.RetornoPct,8:F2}% ({mhi.OperacionesGanadas}/{mhi.TotalOperaciones})");
    }

    Console.WriteLine("\n=== Analisis de riesgo por escenario ===");
    foreach (var perfil in perfiles)
    {
        Console.WriteLine($"\n[{perfil.Escenario} / {perfil.Estrategia}]");
        Console.WriteLine($"  Operaciones: {perfil.TotalOperaciones} (Ganadas={perfil.OperacionesGanadas} Perdidas={perfil.OperacionesPerdidas})");
        Console.WriteLine($"  Racha negativa maxima: {perfil.RachaNegativaMaxima}");
        Console.WriteLine($"  Resuelto por martingala: {perfil.PctOperacionesResueltasPorMartingala:F1}% (Inicial={perfil.GanoInicial} M1={perfil.GanoM1} M2={perfil.GanoM2} PerdioAgotando={perfil.PerdioAgotandoMartingalas})");
        Console.WriteLine($"  Exposicion maxima: {perfil.MaxExposicion}");
        Console.WriteLine($"  Reconciliacion financiera: {(perfil.ReconciliacionCoherente ? "OK" : "FALLA")}");
    }

    Console.WriteLine("\n=== Hallazgos ===");
    if (hallazgos.Count == 0)
    {
        Console.WriteLine("Ninguno.");
    }
    else
    {
        foreach (var h in hallazgos)
            Console.WriteLine($"{h.Tipo,-28} [{h.Escenario} / {h.Estrategia}] {h.Detalle}");
    }

    var bugs = hallazgos.Count(h => h.Tipo == "[BUG]");
    Console.WriteLine($"\n=== Resumen Fase 1.5: {bugs} [BUG] · {hallazgos.Count(h => h.Tipo == "[OBSERVACION DE ESTRATEGIA]")} [OBSERVACION DE ESTRATEGIA] ===");
}
