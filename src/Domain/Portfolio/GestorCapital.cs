using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: Caso 2 D-066/D-067/D-068/D-070/D-071 — capa externa entre Strategy y ejecucion. No conoce
// direccion/logica de la estrategia, solo transforma Cantidad de ordenes ya existentes (D-071:
// nunca crea ni elimina ordenes de la bolsa). sizing=null -> Cantidad intacta (D-061/D-069,
// preserva baseline_final/ sin modificacion).
// spec: Caso 4 D-084/D-092/D-093 — sizing solo se aplica a Apertura/Aumento (ESPECIFICACION_
// INTEGRACION_GESTOR_CAPITAL_V1.md S3); ReduccionParcial/CierreTotal conservan la Cantidad
// efectiva (magnitud real, D-095), evitando el residuo de lotes que originó D-084. La bolsa se
// clasifica secuencialmente contra una posicion PROYECTADA local (S2) — nunca PortfolioState.
// LotesVivos real, que sigue mutando exclusivamente via AplicadorFill (D-071).
// spec: Caso 4 D-095 — bajo sizing activo, una orden de cierre con Cantidad nominal fija de una
// estrategia historica casi nunca coincide con la posicion real que sizing dejo abierta. Un
// CrossZero detectado por el clasificador en ese contexto es espurio (la estrategia "quiso" cerrar
// todo, no invertir el excedente exacto que pidio) — se normaliza a CierreTotal usando la magnitud
// de la posicion proyectada, nunca la Cantidad nominal (ESPECIFICACION_NORMALIZACION_CIERRES_
// SIZING_V1.md S3). Solo aplica con sizing activo: bajo Sizing=null este metodo retorna temprano
// (linea siguiente), preservando intacto el CrossZero genuino de fixtures/baselines historicos.
// spec: Caso 5A D-108 (DECISIONES_CASO5_V1.md) — GestorCapital orquesta: invoca al IGestorRiesgo
// activo para obtener cantidadCalculada (antes, formula de Fixed Fractional inline aqui mismo,
// Caso 4 D-093/D-094 — ver GestorFixedFractional.cs para la formula migrada sin cambios), y
// conserva integra la clasificacion de intencion + normalizacion de Cross-Zero (D-092/D-095) como
// codigo unico, compartido por cualquier gestor. DataSlice se agrega a la firma (antes solo
// precioReferencia escalar) porque GestorVolatilitySizing requiere una ventana de Closes que un
// escalar no puede proveer (hallazgo S1, ESPECIFICACION_IMPLEMENTACION_GESTORES_RIESGO_V1.md) —
// mismo dato que BacktestRunner ya construye para la Strategy, sin calculo adicional.
public static class GestorCapital
{
    public static IReadOnlyList<OrderRequest> Ajustar(IReadOnlyList<OrderRequest> requests, PortfolioState portfolio, ConfiguracionSizing? sizing, DataSlice dataSlice, decimal precioReferencia, decimal tasaMargen)
    {
        if (sizing is null)
            return requests;

        var cantidadCalculada = sizing.GestorActivo.CalcularCantidad(portfolio, dataSlice, precioReferencia, tasaMargen);

        var posicionProyectada = PosicionActual.De(portfolio);
        var resultado = new List<OrderRequest>(requests.Count);

        foreach (var request in requests)
        {
            var portfolioProyectado = new PortfolioState { Cash = portfolio.Cash, Margin = portfolio.Margin };
            if (posicionProyectada != 0m)
                portfolioProyectado.LotesVivos.Add(new Lote(posicionProyectada, 0m, 0m));

            var (intencion, cantidadEfectiva) = ClasificadorIntencionOrden.Clasificar(portfolioProyectado, request);

            decimal cantidadFinal;
            if (intencion is IntencionOrden.Apertura or IntencionOrden.Aumento)
            {
                cantidadFinal = cantidadCalculada;
            }
            else if (intencion is IntencionOrden.CrossZero)
            {
                // spec: D-095 — normalizar el CrossZero espurio a CierreTotal: la magnitud de la
                // posicion proyectada, no la Cantidad nominal solicitada que lo excedia.
                cantidadFinal = Math.Abs(posicionProyectada);
            }
            else
            {
                cantidadFinal = cantidadEfectiva;
            }

            resultado.Add(request with { Cantidad = cantidadFinal });

            var cantidadConSigno = request.Side == Side.Buy ? cantidadFinal : -cantidadFinal;
            posicionProyectada += cantidadConSigno;
        }

        return resultado;
    }
}
