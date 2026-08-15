namespace TD_Project.Contracts;

public sealed record ResultDto(
    string Estado,
    ExperimentInfoDto ExperimentInfo,
    IReadOnlyList<EquityPointDto> EquityCurve,
    IReadOnlyList<TradeDto> Trades,
    IReadOnlyList<FillLogEntryDto> FillLog,
    IReadOnlyList<PortfolioSnapshotDto> PortfolioSnapshots,
    MetricsDto Metrics,
    IReadOnlyList<BranchResolutionDto> BranchResolutions,
    // spec: RNF-16 — opcional para no romper la construccion posicional de ResultDtoMapperTests
    // (mismo patron ya usado en ConfiguracionExperimento para campos incorporados incrementalmente)
    ExplicacionDto? Explicacion = null);
