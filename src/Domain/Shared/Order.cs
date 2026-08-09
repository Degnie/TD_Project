namespace TD_Project.Domain.Shared;

// spec: glosario "Order", "Secuencia Causal"
public sealed class Order
{
    public required long SecuenciaCausal { get; init; }
    public required Side Side { get; init; }
    public required OrderType Type { get; set; }
    public required decimal Cantidad { get; init; }
    public decimal? PrecioLimite { get; set; }
    public decimal? PrecioStop { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}
