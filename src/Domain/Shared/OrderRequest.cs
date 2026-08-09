namespace TD_Project.Domain.Shared;

// spec: glosario "OrderRequest", RN-12 (parametros de precio segun tipo)
public sealed record OrderRequest(
    Side Side,
    OrderType Type,
    decimal Cantidad,
    decimal? PrecioLimite = null,
    decimal? PrecioStop = null);
