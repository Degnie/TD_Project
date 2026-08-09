namespace TD_Project.Contracts;

public sealed record ResultDto(
    string Estado,
    ExperimentInfoDto ExperimentInfo,
    IReadOnlyList<EquityPointDto> EquityCurve,
    IReadOnlyList<TradeDto> Trades,
    IReadOnlyList<FillLogEntryDto> FillLog,
    IReadOnlyList<PortfolioSnapshotDto> PortfolioSnapshots,
    MetricsDto Metrics,
    IReadOnlyList<BranchResolutionDto> BranchResolutions);
