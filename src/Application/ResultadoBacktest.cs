using TD_Project.Domain.Broker;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

namespace TD_Project.Application;

// spec: RNF-08 (Fill Log), RNF-09 (estados de observabilidad), RNF-10 (integridad de falla)
// spec: Caso 2 D-059 — Incapacidades es solo observacion, nunca vacia el resto del resultado ni
// afecta Fills/Trades/EquityCurve (el comportamiento operacional del Caso 1 no se altera).
public sealed record ResultadoBacktest(
    EstadoBacktest Estado,
    IReadOnlyList<Fill> Fills,
    decimal CashFinal,
    IReadOnlyList<Trade> Trades,
    IReadOnlyList<Order> OrdenesFinales,
    IReadOnlyList<EquityPoint> EquityCurve,
    IReadOnlyList<PortfolioSnapshot> PortfolioSnapshots,
    IReadOnlyList<BranchResolutionInfo> BranchResolutions,
    IReadOnlyList<RegistroIncapacidad>? Incapacidades = null)
{
    public IReadOnlyList<RegistroIncapacidad> IncapacidadesEfectivas => Incapacidades ?? Array.Empty<RegistroIncapacidad>();
}
