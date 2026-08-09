namespace TD_Project.Domain.Portfolio;

// spec: RN-09 — porcion de un Lote consumida en una reduccion FIFO
public sealed record ConsumoLote(Lote Lote, decimal CantidadConsumida);
