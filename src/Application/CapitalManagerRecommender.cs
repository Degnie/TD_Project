using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Strategy;

namespace TD_Project.Application;

// spec: RN-18, CU-23 — evaluacion comparativa aislada y deterministica del backtest completo
// contra cada Gestor de Capital pre-cargado (RN-17). CR = RealizedPnL_total / (MaxDrawdown_abs + 1).
// El gestor que maximiza CR sin liquidar la cuenta es el recomendado; si todos liquidan
// (Equity <= 0 en algun punto), se emite inadaptabilidad (GestorRecomendado = null).
public static class CapitalManagerRecommender
{
    public static RecomendacionGestorResultado Recomendar(
        ConfiguracionExperimento config, IStrategy strategy, IReadOnlyList<IGestorRiesgo> gestores)
    {
        var resultados = gestores.Select(gestor => EvaluarGestor(config, strategy, gestor)).ToList();

        var candidatos = resultados.Where(r => !r.CuentaLiquidada).ToList();
        var recomendado = candidatos.Count == 0
            ? null
            : candidatos.OrderByDescending(r => r.Cr).ThenBy(r => r.MaxDrawdown).First().IdentidadGestor;

        return new RecomendacionGestorResultado(resultados, recomendado);
    }

    private static ResultadoGestorEvaluado EvaluarGestor(ConfiguracionExperimento config, IStrategy strategy, IGestorRiesgo gestor)
    {
        // spec: RNF-07 — cada gestor se evalua de forma aislada, config propia con Sizing distinto,
        // ninguna corrida comparte estado con otra.
        var configDelGestor = config with { Sizing = new Domain.Shared.ConfiguracionSizing(gestor) };
        var resultado = BacktestRunner.Ejecutar(configDelGestor, strategy);

        var pnlTotal = resultado.Trades.Sum(t => t.RealizedPnL);
        var maxDrawdown = CalcularMaxDrawdown(resultado.EquityCurve);
        var cuentaLiquidada = resultado.EquityCurve.Any(p => p.Equity <= 0m);
        var cr = pnlTotal / (maxDrawdown + 1m);

        var identidad = gestor is IIdentidadGestorRiesgo identidadGestor
            ? identidadGestor.ObtenerIdentidadConfiguracion()
            : gestor.GetType().Name;

        return new ResultadoGestorEvaluado(identidad, pnlTotal, maxDrawdown, cr, cuentaLiquidada);
    }

    private static decimal CalcularMaxDrawdown(IReadOnlyList<EquityPoint> equityCurve)
    {
        if (equityCurve.Count == 0)
            return 0m;

        var pico = equityCurve[0].Equity;
        var maxDrawdown = 0m;
        foreach (var punto in equityCurve)
        {
            if (punto.Equity > pico)
                pico = punto.Equity;
            var drawdown = pico - punto.Equity;
            if (drawdown > maxDrawdown)
                maxDrawdown = drawdown;
        }
        return maxDrawdown;
    }
}
