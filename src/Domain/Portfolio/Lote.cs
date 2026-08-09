namespace TD_Project.Domain.Portfolio;

// spec: glosario "Lote" — entrada individual a la posicion, consumida FIFO
public sealed record Lote(decimal Cantidad, decimal PrecioEntrada, decimal Margin);
