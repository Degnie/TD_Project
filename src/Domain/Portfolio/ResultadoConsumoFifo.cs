namespace TD_Project.Domain.Portfolio;

// spec: RN-08, RN-09 — resultado de consumir lotes FIFO: que se consumio y cuanto Margin se libero
public sealed record ResultadoConsumoFifo(IReadOnlyList<ConsumoLote> Consumidos, decimal MarginLiberado);
