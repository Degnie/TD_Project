namespace TD_Project.Contracts;

public sealed record EquityPointDto(long Timestamp, decimal Cash, decimal Margin, decimal UnrealizedPnL, decimal Equity);
