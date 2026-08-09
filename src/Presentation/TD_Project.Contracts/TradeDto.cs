namespace TD_Project.Contracts;

public sealed record TradeDto(decimal CantidadInicial, decimal PrecioApertura, decimal? PrecioCierre, decimal RealizedPnL);
