namespace TD_Project.Contracts;

public sealed record FillLogEntryDto(
    long SecuenciaCausal,
    string Side,
    decimal Cantidad,
    decimal PrecioFill,
    decimal CostoFriccionReal,
    long VelaTimestamp,
    string TipoOrdenOriginal);
