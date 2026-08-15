using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Regimen;
using TD_Project.Domain.Shared;

namespace TD_Project.Application;

// spec: RN-19, CU-24 — segmentacion de PnL/WinRate/Drawdown por fase de mercado. Cada Trade se
// asocia al regimen de su TimestampApertura (asociacion explicita, no reconstruida por orden de
// listas). Racha negativa/exposicion quedan fuera de este reporte (heredado de RN-19: no se
// segmenta lo que el SPEC no pide segmentar; no hay ranking comparativo mas alla del RegimenOptimo
// que el propio SPEC exige mostrar explicitamente).
public sealed record FilaFaseRegimen(Regimen Regimen, int TotalTrades, decimal PnLTotal, decimal WinRate);

public sealed record ReporteRegimenResultado(IReadOnlyList<FilaFaseRegimen> Fases, Regimen? RegimenOptimo);

public static class ReporteRegimen
{
    public static ReporteRegimenResultado Calcular(IReadOnlyList<Trade> trades, IReadOnlyList<Candle> velas)
    {
        var timestampAIndice = new Dictionary<long, int>();
        for (var i = 0; i < velas.Count; i++)
            timestampAIndice[velas[i].Timestamp] = i;

        var fases = Enum.GetValues<Regimen>()
            .Select(regimen =>
            {
                var tradesDeLaFase = trades.Where(t => ClasificarTrade(t, velas, timestampAIndice) == regimen).ToList();
                var totalTrades = tradesDeLaFase.Count;
                var pnlTotal = tradesDeLaFase.Sum(t => t.RealizedPnL);
                var winRate = totalTrades == 0 ? 0m : (decimal)tradesDeLaFase.Count(t => t.RealizedPnL > 0m) / totalTrades;
                return new FilaFaseRegimen(regimen, totalTrades, pnlTotal, winRate);
            })
            .ToList();

        var regimenOptimo = fases.Where(f => f.TotalTrades > 0).OrderByDescending(f => f.PnLTotal).FirstOrDefault()?.Regimen;

        return new ReporteRegimenResultado(fases, regimenOptimo);
    }

    private static Regimen ClasificarTrade(Trade trade, IReadOnlyList<Candle> velas, Dictionary<long, int> timestampAIndice)
    {
        if (!timestampAIndice.TryGetValue(trade.TimestampApertura, out var indice))
            return Regimen.Horizontal;

        return ClasificadorRegimen.Clasificar(velas, indice);
    }
}
