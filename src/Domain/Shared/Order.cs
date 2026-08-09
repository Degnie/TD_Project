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

    // spec: RN-11 — cada trayectoria candidata resuelve sobre su propia copia, sin contaminar a la otra
    public Order Clonar() => new()
    {
        SecuenciaCausal = SecuenciaCausal,
        Side = Side,
        Type = Type,
        Cantidad = Cantidad,
        PrecioLimite = PrecioLimite,
        PrecioStop = PrecioStop,
        Status = Status
    };
}
