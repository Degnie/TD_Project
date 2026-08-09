namespace TD_Project.Domain.Shared;

// spec: glosario "Candle / OHLCV"
public readonly record struct Candle(
    long Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);
