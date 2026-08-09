namespace TD_Project.Domain.Portfolio;

// spec: RN-10
public sealed record ResultadoCrossZero(decimal MarginLiberadoPosicionVieja, decimal CantidadPosicionNueva, decimal MarginRetenidoPosicionNueva, decimal RealizedPnL);
