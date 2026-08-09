namespace TD_Project.Application;

// spec: RNF-08 — punto historico de observabilidad, un EquityPoint por vela procesada.
public sealed record EquityPoint(long Timestamp, decimal Cash, decimal Margin, decimal UnrealizedPnL, decimal Equity);
