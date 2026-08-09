using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Matching;

// spec: RN-01, RN-02, RN-03, RN-05, RN-06 — cruza Orders Pending contra el OHLCV
// de manera determinista, resolviendo una unica trayectoria por invocacion.
public static class MatchingEngine
{
    public static Fill? Resolver(Order orden, Candle vela, Trayectoria trayectoria)
    {
        if (orden.Status != OrderStatus.Pending)
            return null;

        if (orden.Type == OrderType.StopLimit)
            return ResolverStopLimit(orden, vela, trayectoria);

        var precioFill = orden.Type switch
        {
            OrderType.Market => vela.Open,
            OrderType.Limit => PrecioCruceLimite(orden, vela),
            OrderType.Stop => PrecioCruceStop(orden, vela),
            _ => throw new InvalidOperationException($"Tipo de orden no soportado: {orden.Type}")
        };

        if (precioFill is null)
            return null;

        // spec: RN-02 — todo Fill satisface el 100% de la cantidad, cero Partial Fills
        OrdenTransiciones.Ejecutar(orden);
        return new Fill(orden.SecuenciaCausal, orden.Side, orden.Cantidad, precioFill.Value, CostoFriccionReal: 0m, vela.Timestamp, orden.Type);
    }

    // spec: RN-06 — Stop se dispara y, en la misma vela, si el rango restante cruza el Limit, hace Fill
    private static Fill? ResolverStopLimit(Order orden, Candle vela, Trayectoria trayectoria)
    {
        var precioStop = orden.PrecioStop!.Value;
        var disparado = CruzaInclusiveStop(orden.Side, vela.Open, precioStop) || RangoCruzaStop(orden.Side, vela, precioStop);
        if (!disparado)
            return null;

        var tipoOrdenOriginal = orden.Type;
        OrdenTransiciones.Disparar(orden, orden.PrecioLimite!.Value);

        var precioFill = PrecioCruceLimiteDesdeDisparo(orden, vela);
        if (precioFill is null)
            return null;

        OrdenTransiciones.Ejecutar(orden);
        return new Fill(orden.SecuenciaCausal, orden.Side, orden.Cantidad, precioFill.Value, CostoFriccionReal: 0m, vela.Timestamp, tipoOrdenOriginal);
    }

    // spec: RN-03, CU-09..11, EC-01 — Limit se dispara cuando el precio se mueve a favor del comprador/vendedor
    // (Buy: precio baja hasta el limite; Sell: precio sube hasta el limite).
    private static decimal? PrecioCruceLimite(Order orden, Candle vela)
    {
        var precioLimite = orden.PrecioLimite!.Value;
        if (CruzaInclusiveLimit(orden.Side, vela.Open, precioLimite))
            return vela.Open;
        if (RangoCruzaLimit(orden.Side, vela, precioLimite))
            return precioLimite;
        return null;
    }

    // spec: RN-03, CU-12 — Stop se dispara en direccion opuesta al Limit del mismo lado
    // (Buy Stop: precio sube hasta el stop; Sell Stop: precio baja hasta el stop — proteccion).
    private static decimal? PrecioCruceStop(Order orden, Candle vela)
    {
        var precioStop = orden.PrecioStop!.Value;
        if (CruzaInclusiveStop(orden.Side, vela.Open, precioStop))
            return vela.Open;
        if (RangoCruzaStop(orden.Side, vela, precioStop))
            return precioStop;
        return null;
    }

    // spec: RN-06, CU-13, CU-14 — tras dispararse el Stop, el precio sigue moviendose en la
    // direccion del disparo (Buy: subiendo; Sell: bajando). El Limit intercepta en esa misma
    // direccion (techo de precio a pagar / piso de precio a recibir), no en la direccion de un Limit aislado.
    private static decimal? PrecioCruceLimiteDesdeDisparo(Order orden, Candle vela)
    {
        var precioLimite = orden.PrecioLimite!.Value;
        if (RangoCruzaStop(orden.Side, vela, precioLimite))
            return precioLimite;
        return null;
    }

    // spec: RN-03 — evaluacion inclusiva (>=, <=). El Open es el primer precio observable.
    private static bool CruzaInclusiveLimit(Side side, decimal open, decimal precioObjetivo) =>
        side == Side.Buy ? open <= precioObjetivo : open >= precioObjetivo;

    private static bool RangoCruzaLimit(Side side, Candle vela, decimal precioObjetivo) =>
        side == Side.Buy ? vela.Low <= precioObjetivo : vela.High >= precioObjetivo;

    // spec: RN-03 — Stop dispara en direccion inversa al Limit del mismo lado
    private static bool CruzaInclusiveStop(Side side, decimal open, decimal precioObjetivo) =>
        side == Side.Buy ? open >= precioObjetivo : open <= precioObjetivo;

    private static bool RangoCruzaStop(Side side, Candle vela, decimal precioObjetivo) =>
        side == Side.Buy ? vela.High >= precioObjetivo : vela.Low <= precioObjetivo;

    // spec: RN-05 — la rama que ejecuta cancela atomicamente a sus hermanas OCO
    public static void ResolverOco(OcoGroup grupo, Candle vela, Trayectoria trayectoria)
    {
        var ramasOrdenadas = grupo.Ramas.OrderBy(r => r.SecuenciaCausal).ToList();
        foreach (var rama in ramasOrdenadas)
        {
            var fill = Resolver(rama, vela, trayectoria);
            if (fill is not null)
            {
                foreach (var hermana in ramasOrdenadas.Where(h => h != rama && h.Status == OrderStatus.Pending))
                    OrdenTransiciones.Cancelar(hermana);
                return;
            }
        }
    }
}
