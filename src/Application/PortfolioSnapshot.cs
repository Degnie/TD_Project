using TD_Project.Domain.Portfolio;

namespace TD_Project.Application;

// spec: RNF-08 — snapshot historico del Portfolio por vela procesada.
public sealed record PortfolioSnapshot(long Timestamp, decimal Cash, decimal Margin, IReadOnlyList<Lote> LotesVivos);
