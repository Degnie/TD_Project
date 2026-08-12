using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Matching;

// spec: RN-01, RN-02, RN-03, RN-05, RN-06 — cruza Orders Pending contra el OHLCV
// de manera determinista, resolviendo una unica trayectoria por invocacion.
public static class MatchingEngine
{
    // spec: Caso 2 D-063/D-064/D-065 — costes son opcionales (default = coste cero, preserva
    // baseline de Caso 1). Slippage solo aplica a Market (ver CalcularCostoFriccion).
    public static Fill? Resolver(Order orden, Candle vela, Trayectoria trayectoria, ConfiguracionCostes? costes = null)
    {
        if (orden.Status != OrderStatus.Pending)
            return null;

        if (orden.Type == OrderType.StopLimit)
            return ResolverStopLimit(orden, vela, trayectoria, costes);

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
        var costoFriccion = CalcularCostoFriccion(orden, precioFill.Value, aplicaSlippage: orden.Type == OrderType.Market, costes);
        return new Fill(orden.SecuenciaCausal, orden.Side, orden.Cantidad, precioFill.Value, costoFriccion, vela.Timestamp, orden.Type);
    }

    // spec: RN-06, RN-11 — Stop se dispara y, en el recorrido temporal simulado que queda desde el
    // punto de disparo hasta el Close (segun la trayectoria A u B), si el Limit es cruzado, hace Fill.
    private static Fill? ResolverStopLimit(Order orden, Candle vela, Trayectoria trayectoria, ConfiguracionCostes? costes)
    {
        var precioStop = orden.PrecioStop!.Value;
        var recorrido = RecorridoVela.Para(vela, trayectoria);
        var tramoRestante = PuntoDeDisparo(orden.Side, recorrido, precioStop);
        if (tramoRestante is null)
            return null;

        var tipoOrdenOriginal = orden.Type;
        OrdenTransiciones.Disparar(orden, orden.PrecioLimite!.Value);

        var precioFill = PrecioCruceLimiteDesdeDisparo(orden, tramoRestante);
        if (precioFill is null)
            return null;

        OrdenTransiciones.Ejecutar(orden);
        // spec: D-063 — StopLimit no es Market: sin slippage (precio pactado = precio ejecucion).
        var costoFriccion = CalcularCostoFriccion(orden, precioFill.Value, aplicaSlippage: false, costes);
        return new Fill(orden.SecuenciaCausal, orden.Side, orden.Cantidad, precioFill.Value, costoFriccion, vela.Timestamp, tipoOrdenOriginal);
    }

    // spec: D-063 — CostoTotal = Comision + Slippage. Comision = Cantidad * PrecioFill * TasaComision
    // (todo tipo de orden). Slippage solo para Market: el motor no tiene un segundo precio de
    // referencia distinto de PrecioFill (Market ya ejecuta al Open, sin libro de ordenes) — se
    // modela como Cantidad * PrecioFill * TasaSlippage, igual patron que Comision pero con su
    // propia tasa. Limit/Stop/StopLimit ejecutan exactamente al precio pactado (RN-03), sin
    // divergencia que modelar (aplicaSlippage=false desactiva el termino).
    private static decimal CalcularCostoFriccion(Order orden, decimal precioFill, bool aplicaSlippage, ConfiguracionCostes? costes)
    {
        var config = costes ?? ConfiguracionCostes.Default;
        var comision = orden.Cantidad * precioFill * config.TasaComision;
        var slippage = aplicaSlippage ? orden.Cantidad * precioFill * config.TasaSlippage : 0m;
        return comision + slippage;
    }

    // spec: RN-11 — recorre Open->Primero->Segundo->Close buscando el primer tramo donde el Stop
    // se dispara; devuelve el tramo restante en el que el Limit todavia puede ser evaluado: el
    // camino que falta recorrer DESPUES del punto de disparo, nunca el punto de disparo repetido.
    private static decimal[]? PuntoDeDisparo(Side side, RecorridoVela recorrido, decimal precioStop)
    {
        var puntos = new[] { recorrido.Open, recorrido.Primero, recorrido.Segundo, recorrido.Close };
        if (CruzaInclusiveStop(side, puntos[0], precioStop))
            return puntos;

        for (var i = 0; i < puntos.Length - 1; i++)
        {
            var desde = puntos[i];
            var hasta = puntos[i + 1];
            if (CruzaTramoStop(side, desde, hasta, precioStop))
                return puntos[(i + 1)..];
        }
        return null;
    }

    private static bool CruzaTramoStop(Side side, decimal desde, decimal hasta, decimal precioObjetivo) =>
        side == Side.Buy ? hasta >= precioObjetivo : hasta <= precioObjetivo;

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

    // spec: RN-06, RN-11, CU-13, CU-14 — tras dispararse el Stop, el precio sigue moviendose en la
    // direccion del disparo (Buy: subiendo; Sell: bajando), pero solo dentro del tramo restante del
    // recorrido temporal simulado (no de la vela completa). El Limit intercepta en esa misma
    // direccion (techo de precio a pagar / piso de precio a recibir).
    private static decimal? PrecioCruceLimiteDesdeDisparo(Order orden, decimal[] tramoRestante)
    {
        var precioLimite = orden.PrecioLimite!.Value;
        for (var i = 0; i < tramoRestante.Length - 1; i++)
        {
            if (CruzaTramoLimit(orden.Side, tramoRestante[i], tramoRestante[i + 1], precioLimite))
                return precioLimite;
        }
        return null;
    }

    private static bool CruzaTramoLimit(Side side, decimal desde, decimal hasta, decimal precioObjetivo) =>
        side == Side.Buy
            ? desde >= precioObjetivo && hasta <= precioObjetivo
            : desde <= precioObjetivo && hasta >= precioObjetivo;

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
