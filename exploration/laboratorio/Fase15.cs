using TD_Project.Application;
using TD_Project.Domain.Shared;
using TD_Project.Exploration;

namespace TD_Project.Laboratorio;

// Fase 1.5: perfil de comportamiento de EstrategiaTresMosqueteros y EstrategiaMhiMayoria sobre
// los datasets de market/ del laboratorio. NO busca una estrategia ganadora — busca entender
// como se comporta cada una segun la forma del mercado, para saber que patrones buscar cuando
// se pase a datos reales. No toca src/, SPEC.md, ni la logica de ninguna estrategia.
public static class Fase15
{
    public sealed record Hallazgo(string Tipo, string Escenario, string Estrategia, string Detalle);

    public static (IReadOnlyList<PerfilEstrategia> Perfiles, IReadOnlyList<Hallazgo> Hallazgos) Ejecutar(
        IReadOnlyDictionary<string, string> datasetsMarket, decimal capitalInicial = 1000m)
    {
        var perfiles = new List<PerfilEstrategia>();
        var hallazgos = new List<Hallazgo>();

        foreach (var (nombreEscenario, ruta) in datasetsMarket)
        {
            var velas = CargarCsv(ruta);

            CorrerUna("Tres Mosqueteros", nombreEscenario, velas, capitalInicial,
                onOp => new TD_Project.Exploration.EstrategiaTresMosqueteros(maxMartingalas: 2, onOperacionResuelta: onOp),
                perfiles, hallazgos);

            CorrerUna("MHI Mayoria", nombreEscenario, velas, capitalInicial,
                onOp => new TD_Project.Exploration.EstrategiaMhiMayoria(maxMartingalas: 2, onOperacionResuelta: onOp),
                perfiles, hallazgos);
        }

        // Determinismo (RNF-06): repetir la corrida completa para el primer escenario y
        // confirmar que ambos motores producen exactamente el mismo resultado bit-a-bit.
        VerificarDeterminismo(datasetsMarket, capitalInicial, hallazgos);

        return (perfiles, hallazgos);
    }

    private static void CorrerUna(
        string nombreEstrategia, string nombreEscenario, IReadOnlyList<Candle> velas, decimal capitalInicial,
        Func<Action<InfoOperacionResuelta>, TD_Project.Domain.Strategy.IStrategy> crearEstrategia,
        List<PerfilEstrategia> perfiles, List<Hallazgo> hallazgos)
    {
        var operaciones = new List<InfoOperacionResuelta>();
        var estrategia = crearEstrategia(operaciones.Add);
        var config = new ConfiguracionExperimento(CapitalInicial: capitalInicial, Velas: velas);
        var resultado = BacktestRunner.Ejecutar(config, estrategia);

        var perfil = PerfilEstrategia.Medir(nombreEscenario, nombreEstrategia, resultado, operaciones);
        perfiles.Add(perfil);

        // Clasificacion de hallazgos: motor vs. estrategia (no mezclar).
        if (resultado.Estado != EstadoBacktest.Success)
        {
            hallazgos.Add(new Hallazgo("[BUG]", nombreEscenario, nombreEstrategia,
                $"El backtest no completo en Success (Estado={resultado.Estado}) — posible ruptura del motor, no una perdida de estrategia."));
        }

        if (!perfil.ReconciliacionCoherente)
        {
            hallazgos.Add(new Hallazgo("[BUG]", nombreEscenario, nombreEstrategia,
                $"Reconciliacion financiera rota: {string.Join("; ", perfil.ErroresReconciliacion)}"));
        }

        if (perfil.MaxExposicion > 1m)
        {
            hallazgos.Add(new Hallazgo("[BUG]", nombreEscenario, nombreEstrategia,
                $"Exposicion maxima observada ({perfil.MaxExposicion}) excede 1 unidad — ambas estrategias operan un unico lote por diseno."));
        }

        // Observaciones de estrategia (comportamiento esperado, no defecto del motor). El win
        // rate por operacion no es el indicador relevante aqui (la martingala hace que la
        // mayoria de las operaciones se ganen por diseno); lo que importa es el retorno neto,
        // porque las pocas perdidas (agotar M2) pesan mas que muchas ganancias chicas.
        if (perfil.TotalOperaciones > 0 && perfil.RetornoPct < 0)
        {
            hallazgos.Add(new Hallazgo("[OBSERVACION DE ESTRATEGIA]", nombreEscenario, nombreEstrategia,
                RazonPerdida(nombreEscenario, nombreEstrategia, perfil)));
        }

        if (nombreEscenario == "SinMovimiento" && perfil.TotalOperaciones == 0)
        {
            hallazgos.Add(new Hallazgo("[OBSERVACION DE ESTRATEGIA]", nombreEscenario, nombreEstrategia,
                "Cero operaciones: todas las velas son dojis (Open=Close), ninguna produce color de referencia ni mayoria valida. " +
                "La estrategia queda fuera de su dominio de aplicacion — no hay senal que evaluar, no es una perdida sino ausencia total de actividad."));
        }

        if (perfil.RachaNegativaMaxima >= 4)
        {
            hallazgos.Add(new Hallazgo("[OBSERVACION DE ESTRATEGIA]", nombreEscenario, nombreEstrategia,
                $"Racha negativa maxima de {perfil.RachaNegativaMaxima} operaciones completas consecutivas — riesgo de secuencia elevado en este escenario."));
        }
    }

    // Interpretacion cualitativa: por que el resultado fue el que fue, no solo cuanto perdio.
    // Basado en la naturaleza de la senal de cada estrategia (color de vela de referencia /
    // mayoria de 3 velas) contrastada con la forma del mercado del escenario.
    private static string RazonPerdida(string escenario, string estrategia, PerfilEstrategia perfil)
    {
        var basePct = $"Retorno={perfil.RetornoPct:F2}% ({perfil.OperacionesPerdidas}/{perfil.TotalOperaciones} operaciones perdidas).";
        return escenario switch
        {
            "TendenciaAlcista" or "TendenciaBajista" =>
                $"{basePct} La tendencia sostenida no garantiza que cada vela individual (o mayoria de 3) siga el sesgo global: " +
                "el ruido local alrededor de la pendiente sigue generando velas en contra, y una racha de esas justo antes de agotar M2 borra varias operaciones ganadoras chicas.",
            "SinMovimiento" =>
                $"{basePct} Con velas planas (Open=Close) la senal de color no tiene ventaja estadistica real: " +
                "las velas dojis producen 'sin senal', pero cualquier micro-ruido rompe el empate al azar, sin sesgo direccional que la estrategia pueda explotar.",
            "RuidoAleatorio" =>
                $"{basePct} En una caminata aleatoria sin sesgo, tanto el color de referencia (Tres Mosqueteros) como la mayoria de 3 velas (MHI) son ruido puro: " +
                "no hay estructura direccional que la senal pueda capturar, por lo que el resultado tiende a la paridad menos el costo de las rachas perdidas.",
            "MercadoLateral" =>
                $"{basePct} Dentro de una banda sin tendencia neta, el color de una vela (o la mayoria de 3) no anticipa reversion ni continuidad: " +
                "la estrategia apuesta direccion en un mercado que no la tiene.",
            "TendenciaConReversion" =>
                $"{basePct} La mitad del dataset invierte la tendencia; una estrategia que ya acumulo martingalas en la direccion vieja enfrenta el cambio de regimen sin ajuste, " +
                "arrastrando exposicion en el sentido equivocado justo en el quiebre.",
            "DobleTecho" =>
                $"{basePct} El patron de reversion parcial (sube-baja-sube-baja) genera velas de color mixto cerca de los maximos/minimos locales, " +
                "que la senal de color/mayoria interpreta como continuidad en el peor momento (el punto de giro).",
            "VolatilidadTrasCalma" =>
                $"{basePct} La calma inicial no entrena ninguna ventaja explotable (la senal no tiene memoria entre cuadrantes); al llegar la volatilidad subita, " +
                "el rango amplio golpea el stop/martingala con mas fuerza que en el tramo calmo.",
            "VolatilidadDecreciente" =>
                $"{basePct} El rango decreciente reduce progresivamente la separacion entre niveles; cerca del final casi no hay rango para generar una senal de color confiable (velas casi doji).",
            _ => basePct,
        };
    }

    private static void VerificarDeterminismo(IReadOnlyDictionary<string, string> datasetsMarket, decimal capitalInicial, List<Hallazgo> hallazgos)
    {
        var (nombre, ruta) = datasetsMarket.First();
        var velas = CargarCsv(ruta);
        var config = new ConfiguracionExperimento(CapitalInicial: capitalInicial, Velas: velas);

        var r1 = BacktestRunner.Ejecutar(config, new TD_Project.Exploration.EstrategiaTresMosqueteros(maxMartingalas: 2));
        var r2 = BacktestRunner.Ejecutar(config, new TD_Project.Exploration.EstrategiaTresMosqueteros(maxMartingalas: 2));

        var determinista = r1.Trades.Count == r2.Trades.Count
            && r1.CashFinal == r2.CashFinal
            && r1.Trades.Select(t => t.RealizedPnL).SequenceEqual(r2.Trades.Select(t => t.RealizedPnL));

        if (!determinista)
        {
            hallazgos.Add(new Hallazgo("[BUG]", nombre, "Tres Mosqueteros",
                "Dos corridas identicas (mismo dataset, misma config) produjeron resultados distintos — ruptura de RNF-06 (determinismo)."));
        }
    }

    private static IReadOnlyList<Candle> CargarCsv(string path)
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
}
