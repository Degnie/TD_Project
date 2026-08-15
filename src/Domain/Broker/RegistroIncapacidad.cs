using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Broker;

// spec: Caso 2 D-059/D-060 — evidencia de que una orden careció de capacidad de capital al
// momento de originarse. Solo observación por defecto: no bloquea ni altera la ejecución de la
// orden, salvo que el experimento active BloquearPorCapacidad (RN-12, CU-15, caso14).
public sealed record RegistroIncapacidad(
    long Timestamp,
    OrderRequest Request,
    decimal ReservaRequerida,
    decimal CashDisponible,
    bool Bloqueada = false);
