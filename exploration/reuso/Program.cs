using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Exploration;

// Auditoria de USO REPETIDO/SECUENCIAL sobre BacktestRunner.Ejecutar.
// No es parte del producto (ver exploration/README.md). Cubre 4 escenarios:
//   1. Reutilizacion de la MISMA instancia de estrategia en dos llamadas.
//   2. Instancias FRESCAS repetidas -> determinismo bit-a-bit (RNF-06).
//   3. Ejecuciones intercaladas A/B/A -> aislamiento (RNF-07).
//   4. Mismo dataset, distinto CapitalInicial, en secuencia -> sin contaminacion.

var csvPath = args.Length > 0 ? args[0] : Path.Combine("..", "velas_simuladas_1anio.csv");
var velas = CargarVelas(csvPath);
Console.WriteLine($"Velas cargadas: {velas.Count} desde {csvPath}");

var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velas);

Console.WriteLine("\n########## ESCENARIO 1: MISMA instancia de estrategia, dos corridas ##########");
{
    var estrategiaCompartida = new EstrategiaTresMosqueteros(1);
    var r1 = BacktestRunner.Ejecutar(config, estrategiaCompartida);
    Reportar("Corrida1_InstanciaCompartida", r1);
    var r2 = BacktestRunner.Ejecutar(config, estrategiaCompartida);
    Reportar("Corrida2_InstanciaCompartida", r2);
    Comparar("Escenario1 (misma instancia)", r1, r2);
}

Console.WriteLine("\n########## ESCENARIO 2: Instancias FRESCAS repetidas (RNF-06) ##########");
{
    var resultados = new List<ResultadoBacktest>();
    for (var i = 0; i < 5; i++)
    {
        var r = BacktestRunner.Ejecutar(config, new EstrategiaMhiMayoria(2));
        Reportar($"Corrida{i + 1}_InstanciaFresca", r);
        resultados.Add(r);
    }
    for (var i = 1; i < resultados.Count; i++)
        Comparar($"Escenario2 (corrida1 vs corrida{i + 1})", resultados[0], resultados[i]);
}

Console.WriteLine("\n########## ESCENARIO 3: Ejecuciones intercaladas A/B/A (RNF-07) ##########");
{
    var a1 = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(1));
    Reportar("A1_TresMosqueteros", a1);
    var b1 = BacktestRunner.Ejecutar(config, new EstrategiaMhiMayoria(1));
    Reportar("B1_MhiMayoria", b1);
    var a2 = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(1));
    Reportar("A2_TresMosqueteros_InstanciaFrescaDeNuevo", a2);
    Comparar("Escenario3 (A1 vs A2, con B1 corrido en medio, instancias frescas)", a1, a2);
}

Console.WriteLine("\n########## ESCENARIO 4: Mismo dataset, distinto CapitalInicial, en secuencia ##########");
{
    var config500 = config with { CapitalInicial = 500m };
    var config1000 = config with { CapitalInicial = 1000m };
    var config2000 = config with { CapitalInicial = 2000m };

    var r500 = BacktestRunner.Ejecutar(config500, new EstrategiaTresMosqueteros(1));
    Reportar("Capital500", r500);
    var r1000 = BacktestRunner.Ejecutar(config1000, new EstrategiaTresMosqueteros(1));
    Reportar("Capital1000", r1000);
    var r2000 = BacktestRunner.Ejecutar(config2000, new EstrategiaTresMosqueteros(1));
    Reportar("Capital2000", r2000);
    // Re-corre Capital1000 para confirmar que intercalar 500 y 2000 en medio no lo contamino.
    var r1000_repetido = BacktestRunner.Ejecutar(config1000, new EstrategiaTresMosqueteros(1));
    Reportar("Capital1000_Repetido", r1000_repetido);
    Comparar("Escenario4 (Capital1000 vs Capital1000_Repetido, con 500/2000 en medio)", r1000, r1000_repetido);
}

static IReadOnlyList<Candle> CargarVelas(string path)
{
    var lineas = File.ReadAllLines(path);
    var velas = new List<Candle>();
    for (var i = 1; i < lineas.Length; i++) // salta encabezado
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

static void Reportar(string nombre, ResultadoBacktest resultado)
{
    var pnlTotal = resultado.Trades.Count > 0 ? resultado.Trades.Sum(t => t.RealizedPnL) : 0m;
    var equityFinal = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[^1].Equity : resultado.CashFinal;
    Console.WriteLine($"[{nombre}] Estado={resultado.Estado} Fills={resultado.Fills.Count} Trades={resultado.Trades.Count} " +
                       $"PnLTotal={pnlTotal} CashFinal={resultado.CashFinal} EquityFinal={equityFinal}");
}

static void Comparar(string etiqueta, ResultadoBacktest r1, ResultadoBacktest r2)
{
    var mismosFills = r1.Fills.Count == r2.Fills.Count &&
        r1.Fills.Zip(r2.Fills, (a, b) =>
            a.SecuenciaCausal == b.SecuenciaCausal && a.Timestamp == b.Timestamp && a.Side == b.Side &&
            a.Cantidad == b.Cantidad && a.PrecioFill == b.PrecioFill && a.CostoFriccionReal == b.CostoFriccionReal)
        .All(igual => igual);

    var mismosTrades = r1.Trades.Count == r2.Trades.Count &&
        r1.Trades.Zip(r2.Trades, (a, b) =>
            a.CantidadInicial == b.CantidadInicial && a.PrecioApertura == b.PrecioApertura &&
            a.PrecioCierre == b.PrecioCierre && a.RealizedPnL == b.RealizedPnL)
        .All(igual => igual);

    var mismoCash = r1.CashFinal == r2.CashFinal;
    var idénticos = mismosFills && mismosTrades && mismoCash && r1.Estado == r2.Estado;

    Console.WriteLine($"[COMPARAR] {etiqueta}: {(idénticos ? "IDENTICOS" : "*** DIFERENTES ***")} " +
                       $"(Fills iguales={mismosFills} [{r1.Fills.Count} vs {r2.Fills.Count}], " +
                       $"Trades iguales={mismosTrades} [{r1.Trades.Count} vs {r2.Trades.Count}], " +
                       $"CashFinal iguales={mismoCash} [{r1.CashFinal} vs {r2.CashFinal}])");
}
