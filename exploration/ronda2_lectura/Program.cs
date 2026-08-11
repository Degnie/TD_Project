using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;

var csvPath = "../velas_simuladas_1anio.csv";
var velas = CargarVelas(csvPath);
Console.WriteLine($"Velas cargadas: {velas.Count}");

var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velas);

var modoDivergencia = args.Length > 0 ? args[0] : null;
if (modoDivergencia is not null)
{
    var modo = modoDivergencia == "stoplimit" ? ModoDivergencia.StopLimit : ModoDivergencia.DosLimits;
    var resDiv = BacktestRunner.Ejecutar(config, new EstrategiaDivergenciaAB(modo));
    Console.WriteLine($"\n=== Divergencia A/B (modo={modo}) ===");
    Console.WriteLine($"Estado: {resDiv.Estado}  Fills: {resDiv.Fills.Count}  Trades: {resDiv.Trades.Count}");
    foreach (var f in resDiv.Fills)
        Console.WriteLine($"  FILL Seq={f.SecuenciaCausal} Timestamp={f.Timestamp} Side={f.Side} Precio={f.PrecioFill} Tipo={f.TipoOrdenOriginal}");

    Console.WriteLine("--- BranchResolutions (solo primeras 3 velas + cualquier no-empate) ---");
    var i = 0;
    foreach (var b in resDiv.BranchResolutions)
    {
        var empate = b.EquityA == b.EquityB;
        if (i < 3 || !empate)
            Console.WriteLine($"  Timestamp={b.Timestamp} Oficial={b.TrayectoriaOficial} EquityA={b.EquityA} EquityB={b.EquityB} Empate={empate} FillsA={b.FillsA.Count} FillsB={b.FillsB.Count}");
        i++;
    }
    var noEmpates = resDiv.BranchResolutions.Count(b => b.EquityA != b.EquityB);
    Console.WriteLine($"Total velas: {resDiv.BranchResolutions.Count}  No-empates: {noEmpates}");
    return;
}

var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(2));

Console.WriteLine($"Estado: {resultado.Estado}  Trades: {resultado.Trades.Count}  Fills: {resultado.Fills.Count}");

Console.WriteLine("\n--- FILLS (orden por SecuenciaCausal) ---");
foreach (var f in resultado.Fills)
    Console.WriteLine($"  Seq={f.SecuenciaCausal} Timestamp={f.Timestamp} Side={f.Side} Cantidad={f.Cantidad} PrecioFill={f.PrecioFill} CostoFriccion={f.CostoFriccionReal} Tipo={f.TipoOrdenOriginal}");

Console.WriteLine("\n--- TRADES ---");
var idx = 0;
foreach (var t in resultado.Trades)
    Console.WriteLine($"  [{idx++}] CantidadInicial={t.CantidadInicial} PrecioApertura={t.PrecioApertura} PrecioCierre={t.PrecioCierre} RealizedPnL={t.RealizedPnL}");

static IReadOnlyList<Candle> CargarVelas(string path)
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
