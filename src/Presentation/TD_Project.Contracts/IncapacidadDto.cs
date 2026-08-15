namespace TD_Project.Contracts;

// spec: RN-12, RNF-16 — evidencia de una orden que careció de capacidad de capital al originarse.
public sealed record IncapacidadDto(
    long Timestamp,
    string Side,
    decimal Cantidad,
    decimal ReservaRequerida,
    decimal CashDisponible,
    bool Bloqueada);
