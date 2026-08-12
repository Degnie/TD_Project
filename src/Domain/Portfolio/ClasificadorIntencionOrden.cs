using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: Caso 4 D-092 — clasifica la intencion de una OrderRequest ANTES del Fill, reutilizando el
// mismo criterio que AplicadorFill ya aplica DESPUES del Fill (signo de la orden vs. signo de
// PosicionActual.LotesVivos). Componente puro de consulta: no muta PortfolioState, no crea ni
// elimina ordenes (D-071). Fuente de verdad exclusiva: PortfolioState/LotesVivos — nunca nombre de
// estrategia, tipo de orden, ni metadata declarada por la estrategia.
public enum IntencionOrden
{
    Apertura,
    Aumento,
    ReduccionParcial,
    CierreTotal,
    CrossZero
}

// spec: Caso 4 D-095 — CantidadEfectiva es la cantidad que corresponde a la intencion clasificada
// tal cual (request.Cantidad para Apertura/Aumento/CrossZero, magnitud real para
// ReduccionParcial/CierreTotal — que ya coincide con request.Cantidad salvo cuando esta ultima
// excede la posicion viva). El clasificador NO decide si normaliza segun sizing este activo o no
// — eso es responsabilidad de GestorCapital (unico consumidor que conoce si sizing esta activo),
// preservando que este componente siga siendo una consulta pura sobre PortfolioState/OrderRequest,
// sin conocer configuracion de sizing (ESPECIFICACION_NORMALIZACION_CIERRES_SIZING_V1.md S2).
public readonly record struct ResultadoClasificacion(IntencionOrden Intencion, decimal CantidadEfectiva);

public static class ClasificadorIntencionOrden
{
    public static ResultadoClasificacion Clasificar(PortfolioState portfolio, OrderRequest request)
    {
        var posicionActual = PosicionActual.De(portfolio);
        var cantidadConSigno = request.Side == Side.Buy ? request.Cantidad : -request.Cantidad;
        var mismoSigno = posicionActual == 0m || Math.Sign(posicionActual) == Math.Sign(cantidadConSigno);

        if (mismoSigno)
        {
            var intencionApertura = posicionActual == 0m ? IntencionOrden.Apertura : IntencionOrden.Aumento;
            return new ResultadoClasificacion(intencionApertura, request.Cantidad);
        }

        var magnitudRequest = Math.Abs(cantidadConSigno);
        var magnitudPosicion = Math.Abs(posicionActual);

        if (magnitudRequest < magnitudPosicion)
            return new ResultadoClasificacion(IntencionOrden.ReduccionParcial, magnitudRequest);
        if (magnitudRequest == magnitudPosicion)
            return new ResultadoClasificacion(IntencionOrden.CierreTotal, magnitudPosicion);
        return new ResultadoClasificacion(IntencionOrden.CrossZero, magnitudRequest);
    }
}
