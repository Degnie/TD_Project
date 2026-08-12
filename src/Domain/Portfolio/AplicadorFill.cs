using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: RN-07 — Position y Trade mutan EXCLUSIVAMENTE a causa de un Fill.
// spec: RN-08 — decide, segun signo/magnitud del Fill contra la Position actual, si abre/aumenta
// (mismo signo), reduce via FIFO (signo contrario, |Fill| <= |Position|) o invierte via Cross-Zero
// (signo contrario, |Fill| > |Position|). Las matematicas de cada camino viven en
// CalculadoraLotes/ConsumidorFifo/ResolutorCrossZero/CalculadoraRealizedPnL; este metodo solo
// coordina y muta el PortfolioState.
public static class AplicadorFill
{
    public static ResultadoAplicacionFill Aplicar(PortfolioState portfolio, Fill fill, decimal tasaMargen = 0.1m)
    {
        var posicionActual = PosicionActual.De(portfolio);
        var cantidadConSigno = fill.Side == Side.Buy ? fill.Cantidad : -fill.Cantidad;
        var mismoSigno = posicionActual == 0m || Math.Sign(posicionActual) == Math.Sign(cantidadConSigno);

        if (mismoSigno)
        {
            var lote = CalculadoraLotes.AbrirLote(cantidadConSigno, fill.PrecioFill, tasaMargen);
            portfolio.LotesVivos.Add(lote);
            portfolio.Cash -= lote.Margin;
            portfolio.Margin += lote.Margin;
            // spec: Caso 2 D-065 — coste aplicado por tramo realmente ejecutado (apertura/aumento).
            portfolio.Cash -= fill.CostoFriccionReal;
            return new ResultadoAplicacionFill(TradeCerrado: null, RealizedPnLReconocido: 0m, MarginLiberado: 0m);
        }

        var posicionEraLarga = posicionActual > 0m;
        var magnitudFill = Math.Abs(cantidadConSigno);
        var magnitudPosicion = Math.Abs(posicionActual);

        if (magnitudFill <= magnitudPosicion)
        {
            var consumo = ConsumidorFifo.Consumir(portfolio.LotesVivos, magnitudFill, fill.PrecioFill, posicionEraLarga);

            foreach (var item in consumo.Consumidos)
            {
                // item.CantidadConsumida es siempre magnitud positiva (ver ConsumidorFifo);
                // item.Lote.Cantidad conserva el signo de la posicion (negativo si es Short).
                var magnitudLote = Math.Abs(item.Lote.Cantidad);
                if (item.CantidadConsumida == magnitudLote)
                {
                    portfolio.LotesVivos.Remove(item.Lote);
                }
                else
                {
                    var magnitudRestante = magnitudLote - item.CantidadConsumida;
                    var restante = Math.Sign(item.Lote.Cantidad) * magnitudRestante;
                    var margenRestante = item.Lote.Margin * (magnitudRestante / magnitudLote);
                    var index = portfolio.LotesVivos.IndexOf(item.Lote);
                    portfolio.LotesVivos[index] = item.Lote with { Cantidad = restante, Margin = margenRestante };
                }
            }

            portfolio.Margin -= consumo.MarginLiberado;
            portfolio.Cash += consumo.MarginLiberado + consumo.RealizedPnL;
            // spec: Caso 2 D-065 — coste aplicado por tramo realmente ejecutado (reduccion FIFO).
            portfolio.Cash -= fill.CostoFriccionReal;

            Trade? tradeCerrado = null;
            if (magnitudFill == magnitudPosicion)
            {
                var primerLote = consumo.Consumidos[0].Lote;
                tradeCerrado = new Trade(magnitudPosicion, primerLote.PrecioEntrada, fill.PrecioFill, consumo.RealizedPnL);
            }

            return new ResultadoAplicacionFill(tradeCerrado, consumo.RealizedPnL, consumo.MarginLiberado);
        }
        else
        {
            var precioEntradaPromedio = portfolio.LotesVivos.Sum(l => l.Cantidad * l.PrecioEntrada) / portfolio.LotesVivos.Sum(l => l.Cantidad);
            var posicionViejaAgregada = new Lote(magnitudPosicion, precioEntradaPromedio, portfolio.Margin);

            var resultado = ResolutorCrossZero.Resolver(posicionViejaAgregada, magnitudFill, fill.PrecioFill, tasaMargen, posicionEraLarga);

            // spec: Caso 2 D-065 — Cross-Zero cierra un tramo (posicion vieja) y abre otro (posicion
            // nueva) con un unico Fill; el coste total se prorratea entre ambos tramos segun la
            // cantidad que cada uno ejecuta, evitando aplicar un coste artificial unico al evento.
            var costoTramoCierre = fill.CostoFriccionReal * (magnitudPosicion / magnitudFill);
            var costoTramoApertura = fill.CostoFriccionReal - costoTramoCierre;

            portfolio.LotesVivos.Clear();
            portfolio.Margin -= resultado.MarginLiberadoPosicionVieja;
            portfolio.Cash += resultado.MarginLiberadoPosicionVieja + resultado.RealizedPnL;
            portfolio.Cash -= costoTramoCierre;

            var cantidadNuevaConSigno = posicionEraLarga ? -resultado.CantidadPosicionNueva : resultado.CantidadPosicionNueva;
            var loteNuevo = CalculadoraLotes.AbrirLote(resultado.CantidadPosicionNueva, fill.PrecioFill, tasaMargen) with { Cantidad = cantidadNuevaConSigno };
            portfolio.LotesVivos.Add(loteNuevo);
            portfolio.Cash -= loteNuevo.Margin;
            portfolio.Margin += loteNuevo.Margin;
            portfolio.Cash -= costoTramoApertura;

            var tradeCerrado = new Trade(magnitudPosicion, precioEntradaPromedio, fill.PrecioFill, resultado.RealizedPnL);
            return new ResultadoAplicacionFill(tradeCerrado, resultado.RealizedPnL, resultado.MarginLiberadoPosicionVieja);
        }
    }
}
