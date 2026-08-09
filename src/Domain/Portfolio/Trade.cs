namespace TD_Project.Domain.Portfolio;

// spec: glosario "Trade" — ciclo vital de exposicion desde apertura hasta cierre total
public sealed record Trade(decimal CantidadInicial, decimal PrecioApertura, decimal? PrecioCierre, decimal RealizedPnL);
