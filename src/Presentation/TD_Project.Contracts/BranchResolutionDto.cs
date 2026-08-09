namespace TD_Project.Contracts;

public sealed record BranchResolutionDto(
    long Timestamp,
    string TrayectoriaOficial,
    decimal EquityA,
    decimal EquityB,
    IReadOnlyList<FillLogEntryDto> FillsA,
    IReadOnlyList<FillLogEntryDto> FillsB);
