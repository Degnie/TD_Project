namespace TD_Project.Domain.Portfolio;

// spec: RN-08 — RealizedPnL de una reduccion FIFO: por cada lote consumido, la diferencia entre
// el precio de Fill y el precio de entrada del lote, con signo segun la direccion de la posicion
// que se esta reduciendo (Long reduce con Sell: PnL positivo si Fill > Entrada; Short al reves).
public static class CalculadoraRealizedPnL
{
    public static decimal DeConsumoFifo(IReadOnlyList<ConsumoLote> consumidos, decimal precioFill, bool posicionEraLarga)
    {
        var signo = posicionEraLarga ? 1m : -1m;
        return consumidos.Sum(c => signo * c.CantidadConsumida * (precioFill - c.Lote.PrecioEntrada));
    }
}
