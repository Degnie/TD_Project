using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

namespace TD_Project.Application;

// spec: RNF-08 (Fill Log), RNF-09 (estados de observabilidad), RNF-10 (integridad de falla)
public sealed record ResultadoBacktest(
    EstadoBacktest Estado,
    IReadOnlyList<Fill> Fills,
    decimal CashFinal,
    IReadOnlyList<Trade> Trades,
    IReadOnlyList<Order> OrdenesFinales,
    IReadOnlyList<EquityPoint> EquityCurve,
    IReadOnlyList<PortfolioSnapshot> PortfolioSnapshots,
    IReadOnlyList<BranchResolutionInfo> BranchResolutions);
