namespace TD_Project.Domain.Shared;

// spec: glosario "Fill / Execution", RNF-08 (Fill Log Minimo)
public sealed record Fill(
    long SecuenciaCausal,
    Side Side,
    decimal Cantidad,
    decimal PrecioFill,
    decimal CostoFriccionReal,
    long Timestamp,
    OrderType TipoOrdenOriginal);
