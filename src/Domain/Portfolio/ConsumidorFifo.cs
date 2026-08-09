namespace TD_Project.Domain.Portfolio;

// spec: RN-09 — reducciones parciales imputan PnL y liberacion de Margin contra los lotes mas antiguos (FIFO).
// spec: RN-08 — la reduccion libera exactamente el Margin original de los lotes consumidos (proporcional si es parcial).
public static class ConsumidorFifo
{
    public static ResultadoConsumoFifo Consumir(IReadOnlyList<Lote> lotes, decimal cantidadAReducir)
    {
        var consumidos = new List<ConsumoLote>();
        var margenLiberado = 0m;
        var restante = cantidadAReducir;

        foreach (var lote in lotes)
        {
            if (restante <= 0m)
                break;

            var cantidadConsumida = Math.Min(lote.Cantidad, restante);
            var proporcion = cantidadConsumida / lote.Cantidad;

            consumidos.Add(new ConsumoLote(lote, cantidadConsumida));
            margenLiberado += lote.Margin * proporcion;
            restante -= cantidadConsumida;
        }

        return new ResultadoConsumoFifo(consumidos, margenLiberado);
    }
}
