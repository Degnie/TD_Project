using TD_Project.Application;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

namespace TD_Project.ModeloFinanciero;

// spec: Caso 2 D-072/D-073/D-075/D-077/D-078 — pruebas requeridas por
// ESPECIFICACION_METRICAS_FINANCIERAS_IMPLEMENTACION_V1.md §6: P1 valores basicos, P2 drawdown
// correcto, P3 pico cero, P4 determinismo, P5 fuente unica, P6 regresion Caso 1, P7 integridad.
public static class TestsMetricasFinancieras
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodos()
    {
        var detalles = new List<string>();
        var pasaron = 0;
        var total = 0;

        void Caso(string nombre, Action verificacion)
        {
            total++;
            try
            {
                verificacion();
                pasaron++;
                detalles.Add($"[PASA] {nombre}");
            }
            catch (Exception ex)
            {
                detalles.Add($"[FALLA] {nombre}: {ex.Message}");
            }
        }

        Caso("P1 — Capital inicial coincide exactamente con el declarado en la entrada", VerificarCapitalInicial);
        Caso("P2 — Drawdown correcto sobre una curva sintetica con pico y caida conocidos", VerificarDrawdownCorrecto);
        Caso("P3 — Pico en cero no produce excepcion ni NaN, se omite ese punto del calculo", VerificarPicoCero);
        Caso("P4 — Determinismo: misma entrada produce las mismas MetricasFinancieras", VerificarDeterminismo);
        Caso("P5 — Cash/Equity/PnL provienen exclusivamente de ResultadoBacktest, sin recalculo paralelo", VerificarFuenteUnica);
        Caso("P6 — EquityCurve vacia produce DrawdownMaximoPct=null, nunca 0m (D-078)", VerificarDrawdownNuloConCurvaVacia);
        Caso("P7 — Exposicion maxima coincide con Max(PortfolioSnapshots.Margin) calculado independientemente", VerificarExposicionMaxima);

        return (total, pasaron, detalles);
    }

    private static ResultadoBacktest ResultadoConCurva(IReadOnlyList<EquityPoint> curva, IReadOnlyList<PortfolioSnapshot>? snapshots = null) =>
        new(EstadoBacktest.Success, Array.Empty<Fill>(), curva.Count > 0 ? curva[^1].Cash : 0m,
            Array.Empty<Trade>(), Array.Empty<Order>(), curva,
            snapshots ?? Array.Empty<PortfolioSnapshot>(), Array.Empty<BranchResolutionInfo>());

    private static void VerificarCapitalInicial()
    {
        var resultado = ResultadoConCurva(new[] { new EquityPoint(1, 1000m, 0m, 0m, 1000m) });
        var metricas = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 1000m);

        Assert(metricas.CapitalInicial == 1000m, $"CapitalInicial esperado 1000, obtuvo {metricas.CapitalInicial}");
    }

    private static void VerificarDrawdownCorrecto()
    {
        // Pico=1000 en t=1, cae a 800 en t=2 (drawdown=20%), sube a 900 en t=3 (recupera parcial).
        var curva = new[]
        {
            new EquityPoint(1, 1000m, 0m, 0m, 1000m),
            new EquityPoint(2, 800m, 0m, 0m, 800m),
            new EquityPoint(3, 900m, 0m, 0m, 900m)
        };
        var resultado = ResultadoConCurva(curva);
        var metricas = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 1000m);

        Assert(metricas.DrawdownMaximoPct == 0.2m, $"DrawdownMaximoPct esperado 0.2, obtuvo {metricas.DrawdownMaximoPct}");
    }

    private static void VerificarPicoCero()
    {
        var curva = new[]
        {
            new EquityPoint(1, 0m, 0m, 0m, 0m),
            new EquityPoint(2, 100m, 0m, 0m, 100m)
        };
        var resultado = ResultadoConCurva(curva);

        var metricas = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 0m);

        Assert(metricas.DrawdownMaximoPct is not null, "No debe lanzar excepcion ni producir null cuando hay curva no vacia");
        Assert(metricas.DrawdownMaximoPct == 0m, $"Sin caida real desde el pico, drawdown esperado 0, obtuvo {metricas.DrawdownMaximoPct}");
    }

    private static void VerificarDeterminismo()
    {
        var curva = new[] { new EquityPoint(1, 950m, 10m, 5m, 965m) };
        var resultado = ResultadoConCurva(curva);

        var m1 = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 1000m);
        var m2 = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 1000m);

        Assert(m1 == m2, "Misma entrada debe producir MetricasFinancieras identicas (record, igualdad estructural)");
    }

    private static void VerificarFuenteUnica()
    {
        var trades = new[] { new Trade(1m, 100m, 110m, RealizedPnL: 10m), new Trade(2m, 100m, 90m, RealizedPnL: -20m) };
        var curva = new[] { new EquityPoint(1, 990m, 0m, 0m, 990m) };
        var resultado = new ResultadoBacktest(EstadoBacktest.Success, Array.Empty<Fill>(), CashFinal: 990m,
            trades, Array.Empty<Order>(), curva, Array.Empty<PortfolioSnapshot>(), Array.Empty<BranchResolutionInfo>());

        var metricas = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 1000m);

        Assert(metricas.CashFinal == resultado.CashFinal, "CashFinal debe ser identico al de ResultadoBacktest, sin recalculo");
        Assert(metricas.EquityFinal == curva[^1].Equity, "EquityFinal debe ser identico al ultimo punto de EquityCurve");
        Assert(metricas.PnLTotal == -10m, $"PnLTotal debe ser la suma exacta de RealizedPnL de Trades (10-20=-10), obtuvo {metricas.PnLTotal}");
    }

    private static void VerificarDrawdownNuloConCurvaVacia()
    {
        var resultado = ResultadoConCurva(Array.Empty<EquityPoint>());
        var metricas = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 1000m);

        Assert(metricas.DrawdownMaximoPct is null, $"EquityCurve vacia debe producir DrawdownMaximoPct=null, obtuvo {metricas.DrawdownMaximoPct}");
    }

    private static void VerificarExposicionMaxima()
    {
        var snapshots = new[]
        {
            new PortfolioSnapshot(1, 900m, 50m, Array.Empty<Lote>()),
            new PortfolioSnapshot(2, 850m, 120m, Array.Empty<Lote>()),
            new PortfolioSnapshot(3, 880m, 90m, Array.Empty<Lote>())
        };
        var resultado = ResultadoConCurva(new[] { new EquityPoint(1, 880m, 90m, 0m, 970m) }, snapshots);

        var metricas = CalculadoraMetricasFinancieras.Calcular(resultado, capitalInicial: 1000m);
        var esperado = snapshots.Max(s => s.Margin);

        Assert(metricas.ExposicionMaxima == esperado, $"ExposicionMaxima esperada {esperado}, obtuvo {metricas.ExposicionMaxima}");
    }

    private static void Assert(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
}
