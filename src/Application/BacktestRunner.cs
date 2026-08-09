using TD_Project.Domain.Broker;
using TD_Project.Domain.Matching;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Domain.VelaResolution;

namespace TD_Project.Application;

// spec: glosario "Backtest" — orquestador temporal. Avanza el reloj, inyecta el
// DataSlice a la Strategy y delega la ejecucion.
public static class BacktestRunner
{
    public static ResultadoBacktest Ejecutar(ConfiguracionExperimento config, IStrategy strategy)
    {
        // spec: CU-02 — dataset corrupto -> DataInvalid -> 0 velas procesadas
        if (config.Velas is null)
            return Vacio(EstadoBacktest.DataInvalid);

        // spec: CU-03 — longitud <= warmup -> NotEvaluable -> 0 Fills
        if (config.Velas.Count <= config.Warmup)
            return Vacio(EstadoBacktest.NotEvaluable);

        var portfolio = new PortfolioState { Cash = config.CapitalInicial };
        var registrador = new RegistradorOrdenes();
        var ordenesPending = new List<Order>();
        var fills = new List<Fill>();
        var trades = new List<Trade>();
        var acumuladorTradeActivo = new AcumuladorTrade();
        var equityCurve = new List<EquityPoint>();
        var portfolioSnapshots = new List<PortfolioSnapshot>();
        var branchResolutions = new List<BranchResolutionInfo>();

        try
        {
            // spec: RN-13 — en N, Strategy lee DataSlice(N); en N+1, Matching cruza contra OHLCV(N+1)
            for (var n = 0; n < config.Velas.Count - 1; n++)
            {
                var dataSlice = new DataSlice(config.Velas.Take(n + 1).ToList());
                var requests = strategy.Observar(dataSlice);

                if (requests.Count > 0)
                {
                    // spec: RN-14 — la bolsa completa del ciclo N se evalua junta
                    var evaluacion = ValidadorBolsaRequests.Evaluar(requests);
                    if (evaluacion.Aprobada)
                    {
                        foreach (var request in requests)
                            ordenesPending.Add(registrador.Registrar(request));
                    }
                }

                var velaSiguiente = config.Velas[n + 1];
                var resolucion = ResolutorVela.Resolver(ordenesPending.Where(o => o.Status == OrderStatus.Pending).ToList(), velaSiguiente, portfolio);

                equityCurve.Add(new EquityPoint(velaSiguiente.Timestamp, resolucion.CashFinal, resolucion.MarginFinal, resolucion.UnrealizedPnLFinal, resolucion.EquityFinal));
                portfolioSnapshots.Add(new PortfolioSnapshot(velaSiguiente.Timestamp, resolucion.CashFinal, resolucion.MarginFinal, resolucion.LotesVivosFinal));
                branchResolutions.Add(new BranchResolutionInfo(
                    velaSiguiente.Timestamp,
                    resolucion.TrayectoriaOficial == Trayectoria.A ? TrayectoriaResolucion.A : TrayectoriaResolucion.B,
                    resolucion.EquityA,
                    resolucion.EquityB,
                    resolucion.FillsA,
                    resolucion.FillsB));

                foreach (var fill in resolucion.Fills)
                {
                    fills.Add(fill);
                    acumuladorTradeActivo.AntesDeAplicar(PosicionActual.De(portfolio), fill.PrecioFill);
                    var resultadoFill = AplicadorFill.Aplicar(portfolio, fill);
                    acumuladorTradeActivo.Registrar(resultadoFill);
                    if (resultadoFill.TradeCerrado is not null)
                        trades.Add(acumuladorTradeActivo.CerrarYExtraer(resultadoFill.TradeCerrado));
                    var ordenDelFill = ordenesPending.First(o => o.SecuenciaCausal == fill.SecuenciaCausal);
                    OrdenTransiciones.Ejecutar(ordenDelFill);
                }
            }

            // spec: CU-07 (Fin) — ordenes Pending se cancelan; posicion viva se documenta M2M (Etapa posterior)
            foreach (var orden in ordenesPending.Where(o => o.Status == OrderStatus.Pending))
                OrdenTransiciones.Cancelar(orden);

            return new ResultadoBacktest(EstadoBacktest.Success, fills, portfolio.Cash, trades, ordenesPending, equityCurve, portfolioSnapshots, branchResolutions);
        }
        catch (ArgumentOutOfRangeException)
        {
            // spec: CU-06 (Look-ahead) — lectura futura bloqueada -> Aborto (StrategyError)
            return Vacio(EstadoBacktest.StrategyError);
        }
        catch (Exception)
        {
            // spec: EC-04, RNF-10 — aborto no manejado -> InternalCrash, 0 resultados financieros
            return Vacio(EstadoBacktest.InternalCrash);
        }
    }

    private static ResultadoBacktest Vacio(EstadoBacktest estado) =>
        new(estado, Array.Empty<Fill>(), 0m, Array.Empty<Trade>(), Array.Empty<Order>(),
            Array.Empty<EquityPoint>(), Array.Empty<PortfolioSnapshot>(), Array.Empty<BranchResolutionInfo>());
}
