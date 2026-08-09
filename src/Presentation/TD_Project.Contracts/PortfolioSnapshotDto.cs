namespace TD_Project.Contracts;

public sealed record PortfolioSnapshotDto(long Timestamp, decimal Cash, decimal Margin, IReadOnlyList<LoteDto> LotesVivos);
