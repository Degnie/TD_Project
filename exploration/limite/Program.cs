using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;

// Harness de exploracion de configuracion al limite. No modifica src/Domain ni src/Application.
// Reutiliza las estrategias reales de exploration/ (referenciadas via <Compile Include>).

var velasCompletas = CargarVelas(Path.Combine("..", "velas_simuladas_1anio.csv"));
Console.WriteLine($"Velas base cargadas: {velasCompletas.Count}");

Console.WriteLine("\n########## BLOQUE 1: CAPITAL INICIAL MUY BAJO ##########");
foreach (var capital in new[] { 100m, 10m, 5m, 1m })
{
    var config = new ConfiguracionExperimento(CapitalInicial: capital, Velas: velasCompletas);
    var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(0));
    var fillsBuy = resultado.Fills.Count(f => f.Side == Side.Buy);
    var fillsSell = resultado.Fills.Count(f => f.Side == Side.Sell);
    Console.WriteLine($"CapitalInicial={capital,-8} Estado={resultado.Estado,-10} Fills={resultado.Fills.Count,-4} (Buy={fillsBuy},Sell={fillsSell}) Trades={resultado.Trades.Count,-4} CashFinal={resultado.CashFinal}");
    if (resultado.Fills.Count > 0)
    {
        var primerFill = resultado.Fills[0];
        Console.WriteLine($"  Primer fill: Seq={primerFill.SecuenciaCausal} Side={primerFill.Side} Cantidad={primerFill.Cantidad} PrecioFill={primerFill.PrecioFill} -> Margin implicito={primerFill.Cantidad * primerFill.PrecioFill * 0.1m}");
    }
    if (resultado.CashFinal < 0)
        Console.WriteLine($"  *** CashFinal NEGATIVO: {resultado.CashFinal} ***");
}

Console.WriteLine("\n########## BLOQUE 1b: CASH MINIMO EN TRANSITO (CapitalInicial=1) ##########");
{
    var config = new ConfiguracionExperimento(CapitalInicial: 1m, Velas: velasCompletas);
    var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(0));
    var minCash = resultado.PortfolioSnapshots.Count > 0 ? resultado.PortfolioSnapshots.Min(s => s.Cash) : (decimal?)null;
    var minEquity = resultado.EquityCurve.Count > 0 ? resultado.EquityCurve.Min(e => e.Equity) : (decimal?)null;
    Console.WriteLine($"CapitalInicial=1: Snapshots={resultado.PortfolioSnapshots.Count} MinCashEnTransito={minCash} MinEquityEnTransito={minEquity}");
    Console.WriteLine($"Primeros 5 snapshots:");
    foreach (var s in resultado.PortfolioSnapshots.Take(5))
        Console.WriteLine($"  t={s.Timestamp} Cash={s.Cash} Margin={s.Margin} LotesVivos={s.LotesVivos.Count}");
}

Console.WriteLine("\n########## BLOQUE 2: DATASET MUY CORTO (Warmup=0) ##########");
foreach (var n in new[] { 5, 10, 20 })
{
    var subVelas = velasCompletas.Take(n).ToList();
    var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: subVelas);
    var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(0));
    Console.WriteLine($"N={n,-4} Estado={resultado.Estado,-10} Fills={resultado.Fills.Count,-4} Trades={resultado.Trades.Count,-4} CashFinal={resultado.CashFinal}");
}

Console.WriteLine("\n########## BLOQUE 3: WARMUP VARIABLE ##########");
foreach (var warmup in new[] { 5, 10, 20, 252, 253 })
{
    var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velasCompletas, Warmup: warmup);
    var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(0));
    Console.WriteLine($"Warmup={warmup,-4} (Velas={velasCompletas.Count}) Estado={resultado.Estado,-10} Fills={resultado.Fills.Count,-4} Trades={resultado.Trades.Count,-4} CashFinal={resultado.CashFinal}");
}
// Caso limite exacto: Warmup == Velas.Count - 1 (deberia SI evaluar, exactamente 1 vela util)
{
    var warmup = velasCompletas.Count - 1;
    var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velasCompletas, Warmup: warmup);
    var resultado = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(0));
    Console.WriteLine($"Warmup=Count-1={warmup} Estado={resultado.Estado,-10} Fills={resultado.Fills.Count,-4} Trades={resultado.Trades.Count,-4} CashFinal={resultado.CashFinal}");
}

Console.WriteLine("\n########## BLOQUE 4: VELAS SINTETICAS (todas doji / todas mismo color) ##########");
var velasDoji = GenerarVelasDoji(30);
var velasVerdes = GenerarVelasMismoColor(30, verde: true);
foreach (var (nombre, velasSint) in new (string, IReadOnlyList<Candle>)[] { ("TodasDoji", velasDoji), ("TodasVerdes", velasVerdes) })
{
    foreach (var (estNombre, fabrica) in new (string, Func<TD_Project.Domain.Strategy.IStrategy>)[]
    {
        ("TresMosqueteros_0m", () => new EstrategiaTresMosqueteros(0)),
        ("MhiMayoria_0m", () => new EstrategiaMhiMayoria(0)),
    })
    {
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velasSint);
        var resultado = BacktestRunner.Ejecutar(config, fabrica());
        Console.WriteLine($"{nombre,-12} {estNombre,-20} Estado={resultado.Estado,-10} Fills={resultado.Fills.Count,-4} Trades={resultado.Trades.Count,-4} CashFinal={resultado.CashFinal} (CapitalInicial=1000)");
    }
}

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

// Doji puro: Open == Close (sin senal de color para ninguna de las dos estrategias).
// High/Low levemente separados para no violar High>=max(O,C), Low<=min(O,C).
static IReadOnlyList<Candle> GenerarVelasDoji(int n)
{
    var velas = new List<Candle>();
    for (var i = 0; i < n; i++)
    {
        var precio = 100m + (i % 3); // variacion minima entre bloques, pero O==C siempre
        velas.Add(new Candle(Timestamp: i + 1, Open: precio, High: precio + 0.5m, Low: precio - 0.5m, Close: precio, Volume: 100m));
    }
    return velas;
}

// Todas verdes (Close > Open) o todas rojas, mismo color repetido n veces seguidas.
static IReadOnlyList<Candle> GenerarVelasMismoColor(int n, bool verde)
{
    var velas = new List<Candle>();
    var basePrecio = 100m;
    for (var i = 0; i < n; i++)
    {
        var open = basePrecio;
        var close = verde ? open + 0.5m : open - 0.5m;
        var high = Math.Max(open, close) + 0.3m;
        var low = Math.Min(open, close) - 0.3m;
        velas.Add(new Candle(Timestamp: i + 1, Open: open, High: high, Low: low, Close: close, Volume: 100m));
        basePrecio = close; // continua desde el cierre anterior
    }
    return velas;
}
