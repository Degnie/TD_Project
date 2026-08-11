using System.Diagnostics;
using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Exploration;

// Auditoria RONDA 2 - USO REPETIDO/SECUENCIAL (angulo: profundizar mas alla del hallazgo
// ya confirmado en Ronda 1 de "misma instancia produce resultados distintos").
//
// Cubre:
//   A. Drift de reutilizacion de instancia, para las 6 combinaciones estrategia x martingala
//      (Ronda 1 solo probo 1 combinacion). Busca si el drift es sistematico o erratico.
//   B. ConfiguracionExperimento reutilizada/con record 'with' intercalado agresivamente,
//      incluyendo Warmup distinto y slices Take/Skip del mismo arreglo subyacente de Candle,
//      para buscar fugas de estado entre corridas via el dataset compartido.
//   C. 20 corridas consecutivas alternando estrategias/martingalas con instancias frescas,
//      midiendo tiempo por corrida (deteccion de leak/degradacion) y verificando que el
//      patron de determinismo no se rompa en ninguna combinacion particular.

var csvPath = args.Length > 0 ? args[0] : Path.Combine("..", "velas_simuladas_1anio.csv");
var velasBase = CargarVelas(csvPath);
Console.WriteLine($"Velas cargadas: {velasBase.Count} desde {csvPath}");

var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velasBase);

Console.WriteLine("\n########## A: Drift de reutilizacion de instancia, las 6 combinaciones ##########");
{
    (string Nombre, Func<int, IStrategy> Fabrica)[] familias =
    {
        ("TresMosqueteros", m => new EstrategiaTresMosqueteros(m)),
        ("MhiMayoria", m => new EstrategiaMhiMayoria(m)),
    };

    foreach (var (nombreFamilia, fabrica) in familias)
    {
        foreach (var martingalas in new[] { 0, 1, 2 })
        {
            var instanciaCompartida = fabrica(martingalas);
            var r1 = BacktestRunner.Ejecutar(config, instanciaCompartida);
            var r2 = BacktestRunner.Ejecutar(config, instanciaCompartida);
            var r3 = BacktestRunner.Ejecutar(config, instanciaCompartida);

            var pnl1 = SumaPnL(r1);
            var pnl2 = SumaPnL(r2);
            var pnl3 = SumaPnL(r3);

            Console.WriteLine($"[{nombreFamilia}(maxMartingalas={martingalas})] " +
                $"Corrida1: Fills={r1.Fills.Count} Trades={r1.Trades.Count} PnL={pnl1} | " +
                $"Corrida2: Fills={r2.Fills.Count} Trades={r2.Trades.Count} PnL={pnl2} | " +
                $"Corrida3: Fills={r3.Fills.Count} Trades={r3.Trades.Count} PnL={pnl3} | " +
                $"Drift(2-1)={pnl2 - pnl1} Drift(3-2)={pnl3 - pnl2}");
        }
    }
}

Console.WriteLine("\n########## B: ConfiguracionExperimento reutilizada/mutada agresivamente ##########");
{
    // B1: misma referencia 'config' reutilizada en 4 corridas seguidas con estrategias frescas
    // distintas -- ya cubierto en Escenario4 de exploration/reuso, aqui se repite con MAS
    // variedad de estrategias (ambas familias, no solo TresMosqueteros) y se compara la
    // PRIMERA corrida contra una nueva corrida final con la MISMA config de nuevo.
    var rInicial = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(1));
    var rMhi = BacktestRunner.Ejecutar(config, new EstrategiaMhiMayoria(2));
    var rOtraFamilia = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(2));
    var rFinal = BacktestRunner.Ejecutar(config, new EstrategiaTresMosqueteros(1));
    Comparar("B1 (misma config, TresMosqueteros(1) al inicio vs al final, con Mhi y TM(2) en medio)", rInicial, rFinal);

    // B2: 'with' produce variantes con Warmup distinto y slices Take/Skip del MISMO arreglo
    // subyacente de Candle[] (velasBase). Verifica que evaluar con Warmup=50 no deje "sucio"
    // el dataset para una corrida posterior con Warmup=0 sobre el arreglo completo.
    var configWarmup50 = config with { Warmup = 50 };
    var configSkip100 = config with { Velas = velasBase.Skip(100).ToList() };
    var configTake200 = config with { Velas = velasBase.Take(200).ToList() };

    var rBase1 = BacktestRunner.Ejecutar(config, new EstrategiaMhiMayoria(1));
    var rWarmup = BacktestRunner.Ejecutar(configWarmup50, new EstrategiaMhiMayoria(1));
    var rSkip = BacktestRunner.Ejecutar(configSkip100, new EstrategiaMhiMayoria(1));
    var rTake = BacktestRunner.Ejecutar(configTake200, new EstrategiaMhiMayoria(1));
    var rBase2 = BacktestRunner.Ejecutar(config, new EstrategiaMhiMayoria(1));
    Comparar("B2 (config base MhiMayoria(1) antes vs despues de intercalar Warmup50/Skip100/Take200)", rBase1, rBase2);
    Console.WriteLine($"  [B2 detalle] Base1 Fills={rBase1.Fills.Count} | Warmup50 Fills={rWarmup.Fills.Count} | " +
        $"Skip100 Fills={rSkip.Fills.Count} | Take200 Fills={rTake.Fills.Count} | Base2 Fills={rBase2.Fills.Count}");

    // B3: verifica que el arreglo original velasBase no fue mutado tras todas las corridas
    // anteriores (comparacion elemento a elemento contra una carga fresca del CSV).
    var velasRelectura = CargarVelas(csvPath);
    var intacto = velasBase.Count == velasRelectura.Count &&
        velasBase.Zip(velasRelectura, (a, b) => a == b).All(igual => igual);
    Console.WriteLine($"  [B3] Dataset original intacto tras todas las corridas anteriores: {intacto}");
}

Console.WriteLine("\n########## C: 20 corridas consecutivas alternando estrategias/martingalas ##########");
{
    (string Nombre, Func<IStrategy> Fabrica)[] combinaciones =
    {
        ("TM_m0", () => new EstrategiaTresMosqueteros(0)),
        ("TM_m1", () => new EstrategiaTresMosqueteros(1)),
        ("TM_m2", () => new EstrategiaTresMosqueteros(2)),
        ("Mhi_m0", () => new EstrategiaMhiMayoria(0)),
        ("Mhi_m1", () => new EstrategiaMhiMayoria(1)),
        ("Mhi_m2", () => new EstrategiaMhiMayoria(2)),
    };

    var primerResultadoPorCombinacion = new Dictionary<string, ResultadoBacktest>();
    var tiempos = new List<(string Nombre, long Ms)>();
    var sw = new Stopwatch();

    for (var i = 0; i < 20; i++)
    {
        var (nombre, fabrica) = combinaciones[i % combinaciones.Length];
        var etiqueta = $"{nombre}#{i / combinaciones.Length}";

        sw.Restart();
        var resultado = BacktestRunner.Ejecutar(config, fabrica());
        sw.Stop();
        tiempos.Add((etiqueta, sw.ElapsedMilliseconds));

        Console.WriteLine($"[{i + 1:D2}] {etiqueta} Estado={resultado.Estado} Fills={resultado.Fills.Count} " +
            $"Trades={resultado.Trades.Count} PnL={SumaPnL(resultado)} Tiempo={sw.ElapsedMilliseconds}ms");

        if (primerResultadoPorCombinacion.TryGetValue(nombre, out var previo))
        {
            var pnlPrevio = SumaPnL(previo);
            var pnlActual = SumaPnL(resultado);
            var determinista = previo.Fills.Count == resultado.Fills.Count &&
                previo.Trades.Count == resultado.Trades.Count && pnlPrevio == pnlActual &&
                previo.CashFinal == resultado.CashFinal;
            if (!determinista)
                Console.WriteLine($"     *** ROMPE DETERMINISMO vs corrida previa de {nombre}: " +
                    $"Fills {previo.Fills.Count}->{resultado.Fills.Count} Trades {previo.Trades.Count}->{resultado.Trades.Count} " +
                    $"PnL {pnlPrevio}->{pnlActual} Cash {previo.CashFinal}->{resultado.CashFinal}");
        }
        else
        {
            primerResultadoPorCombinacion[nombre] = resultado;
        }
    }

    var tiempoPromedioPrimeraMitad = tiempos.Take(10).Average(t => t.Ms);
    var tiempoPromedioSegundaMitad = tiempos.Skip(10).Average(t => t.Ms);
    Console.WriteLine($"\n[C resumen] Tiempo promedio primeras 10 corridas: {tiempoPromedioPrimeraMitad:F2}ms | " +
        $"ultimas 10 corridas: {tiempoPromedioSegundaMitad:F2}ms | " +
        $"Ratio(ultimas/primeras)={(tiempoPromedioPrimeraMitad > 0 ? tiempoPromedioSegundaMitad / tiempoPromedioPrimeraMitad : 0):F2}");
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

static decimal SumaPnL(ResultadoBacktest resultado) =>
    resultado.Trades.Count > 0 ? resultado.Trades.Sum(t => t.RealizedPnL) : 0m;

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
