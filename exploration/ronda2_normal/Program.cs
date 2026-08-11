using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;

var csvPath = args.Length > 0 ? args[0] : "velas_simuladas_1anio.csv";
var velas = CargarVelas(csvPath);
Console.WriteLine($"Velas cargadas: {velas.Count} desde {csvPath}");

var escenarios = new (string Nombre, Func<TD_Project.Domain.Strategy.IStrategy> Fabrica)[]
{
    ("TresMosqueteros_Caso1_SinMartingala", () => new EstrategiaTresMosqueteros(0)),
    ("TresMosqueteros_Caso2_1Martingala", () => new EstrategiaTresMosqueteros(1)),
    ("TresMosqueteros_Caso3_2Martingalas", () => new EstrategiaTresMosqueteros(2)),
    ("MhiMayoria_Caso1_SinMartingala", () => new EstrategiaMhiMayoria(0)),
    ("MhiMayoria_Caso2_1Martingala", () => new EstrategiaMhiMayoria(1)),
    ("MhiMayoria_Caso3_2Martingalas", () => new EstrategiaMhiMayoria(2)),
};

var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velas);

var soloEscenario = Environment.GetEnvironmentVariable("SOLO_ESCENARIO");
var volcarTrades = Environment.GetEnvironmentVariable("VOLCAR_TRADES") == "1";
var volcarFills = Environment.GetEnvironmentVariable("VOLCAR_FILLS") == "1";
var volcarBranches = Environment.GetEnvironmentVariable("VOLCAR_BRANCHES") == "1";

foreach (var (nombre, fabrica) in escenarios)
{
    if (soloEscenario is not null && nombre != soloEscenario)
        continue;

    var resultado = BacktestRunner.Ejecutar(config, fabrica());
    ReportarResultado(nombre, resultado);

    if (volcarTrades)
    {
        Console.WriteLine($"--- TODOS los trades de {nombre} ---");
        var idx = 0;
        foreach (var t in resultado.Trades)
            Console.WriteLine($"  [{idx++}] CantidadInicial={t.CantidadInicial} PrecioApertura={t.PrecioApertura} PrecioCierre={t.PrecioCierre} RealizedPnL={t.RealizedPnL}");
    }

    if (volcarFills)
    {
        Console.WriteLine($"--- TODOS los fills de {nombre} ---");
        foreach (var f in resultado.Fills)
            Console.WriteLine($"  Seq={f.SecuenciaCausal} Timestamp={f.Timestamp} Side={f.Side} Cantidad={f.Cantidad} PrecioFill={f.PrecioFill} CostoFriccion={f.CostoFriccionReal}");
    }

    if (volcarBranches)
    {
        Console.WriteLine($"--- BranchResolutions de {nombre} (total={resultado.BranchResolutions.Count}) ---");
        foreach (var b in resultado.BranchResolutions)
        {
            var empate = b.EquityA == b.EquityB;
            var diff = b.EquityA - b.EquityB;
            Console.WriteLine($"  Timestamp={b.Timestamp} Oficial={b.TrayectoriaOficial} EquityA={b.EquityA} EquityB={b.EquityB} Diff={diff} Empate={empate} FillsA={b.FillsA.Count} FillsB={b.FillsB.Count}");
        }
    }
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

static void ReportarResultado(string nombre, ResultadoBacktest resultado)
{
    Console.WriteLine($"\n=== {nombre} ===");
    Console.WriteLine($"Estado: {resultado.Estado}");
    Console.WriteLine($"CashFinal: {resultado.CashFinal}");
    Console.WriteLine($"Trades: {resultado.Trades.Count}");

    if (resultado.Trades.Count == 0)
        return;

    var pnlTotal = resultado.Trades.Sum(t => t.RealizedPnL);
    var ganadores = resultado.Trades.Count(t => t.RealizedPnL > 0);
    var perdedores = resultado.Trades.Count(t => t.RealizedPnL < 0);
    var neutros = resultado.Trades.Count(t => t.RealizedPnL == 0);

    Console.WriteLine($"PnL total: {pnlTotal}");
    Console.WriteLine($"Ganadores: {ganadores} · Perdedores: {perdedores} · Neutros(PnL=0): {neutros}");

    var equityFinal = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve[^1].Equity : resultado.CashFinal;
    Console.WriteLine($"EquityFinal: {equityFinal}");

    // Muestra los primeros y últimos 3 trades para inspección manual.
    foreach (var t in resultado.Trades.Take(3))
        Console.WriteLine($"  Trade: CantidadInicial={t.CantidadInicial} PrecioApertura={t.PrecioApertura} PrecioCierre={t.PrecioCierre} RealizedPnL={t.RealizedPnL}");
    if (resultado.Trades.Count > 6)
        Console.WriteLine("  ...");
    foreach (var t in resultado.Trades.TakeLast(3))
        Console.WriteLine($"  Trade: CantidadInicial={t.CantidadInicial} PrecioApertura={t.PrecioApertura} PrecioCierre={t.PrecioCierre} RealizedPnL={t.RealizedPnL}");
}
