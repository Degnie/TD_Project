namespace TD_Project.Domain.Portfolio;

// spec: RN-08, RN-09 — resultado de consumir lotes FIFO: que se consumio, cuanto Margin se libero
// y el RealizedPnL reconocido en esa reduccion.
public sealed record ResultadoConsumoFifo(IReadOnlyList<ConsumoLote> Consumidos, decimal MarginLiberado, decimal RealizedPnL);
