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
    // spec: RNF-16 — opcional para no romper la construccion con argumentos nombrados de
    // ExplicacionDtoTests (mismo patron ya usado en ConfiguracionExperimento para campos
    // incorporados incrementalmente)
    ExplicacionDto? Explicacion = null,
    // spec: RN-12, RNF-16 — lista vacia si no hubo ninguna incapacidad, nunca null. El mapper la
    // popula siempre; el default vacio solo cubre construcciones que no la mencionan.
    IReadOnlyList<IncapacidadDto>? Incapacidades = null,
    // spec: RNF-16 — siempre calculable a partir de la ultima PortfolioSnapshot/EquityPoint. El
    // mapper la popula siempre; el default cero solo cubre construcciones que no la mencionan.
    ExposicionFinalDto? Exposicion = null,
    // spec: RN-19, CU-24 — null cuando el analisis de regimen no se ejecuto (todo endpoint salvo
    // /api/strategies/dsl/run); a diferencia de Incapacidades/Exposicion, no hay una "tabla vacia"
    // razonable que forzar cuando el calculo ni siquiera corrio.
    ReporteRegimenDto? ReporteRegimen = null)
{
    public IReadOnlyList<IncapacidadDto> Incapacidades { get; init; } = Incapacidades ?? Array.Empty<IncapacidadDto>();
    public ExposicionFinalDto Exposicion { get; init; } = Exposicion ?? new ExposicionFinalDto(0m, 0m, 0m, 0m, 0m);
}
