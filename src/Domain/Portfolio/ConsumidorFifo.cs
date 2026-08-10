namespace TD_Project.Domain.Portfolio;

// spec: RN-09 — reducciones parciales imputan PnL y liberacion de Margin contra los lotes mas antiguos (FIFO).
// spec: RN-08 — la reduccion libera exactamente el Margin original de los lotes consumidos (proporcional si es parcial).
public static class ConsumidorFifo
{
    public static ResultadoConsumoFifo Consumir(IReadOnlyList<Lote> lotes, decimal cantidadAReducir, decimal precioFill, bool posicionEraLarga)
    {
        var consumidos = new List<ConsumoLote>();
        var margenLiberado = 0m;
        var restante = cantidadAReducir;

        foreach (var lote in lotes)
        {
            if (restante <= 0m)
                break;

            // Lote.Cantidad conserva el signo de la posicion (negativo si es Short, RN-08); el
            // algoritmo FIFO opera sobre magnitudes — "cuanto se consumio" nunca es negativo,
            // el signo de la posicion ya lo captura por separado posicionEraLarga.
            var magnitudLote = Math.Abs(lote.Cantidad);
            var cantidadConsumida = Math.Min(magnitudLote, restante);
            var proporcion = cantidadConsumida / magnitudLote;

            consumidos.Add(new ConsumoLote(lote, cantidadConsumida));
            margenLiberado += lote.Margin * proporcion;
            restante -= cantidadConsumida;
        }

        var realizedPnL = CalculadoraRealizedPnL.DeConsumoFifo(consumidos, precioFill, posicionEraLarga);
        return new ResultadoConsumoFifo(consumidos, margenLiberado, realizedPnL);
    }
}
