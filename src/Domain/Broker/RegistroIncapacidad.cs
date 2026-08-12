using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: Caso 2 D-059/D-060 — evidencia de que una orden careció de capacidad de capital al
// momento de originarse. Solo observación: no bloquea ni altera la ejecución de la orden.
public sealed record RegistroIncapacidad(
    long Timestamp,
    OrderRequest Request,
    decimal ReservaRequerida,
    decimal CashDisponible);
