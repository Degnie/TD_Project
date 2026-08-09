using TD_Project.Domain.Shared;

namespace TD_Project.Domain.Portfolio;

// spec: RN-07 — Position y Trade mutan EXCLUSIVAMENTE a causa de un Fill.
// spec: RN-08 — apertura/aumento mueve fondos de Cash hacia Margin segun la formula del lote.
public static class AplicadorFill
{
    public static void Aplicar(PortfolioState portfolio, Fill fill, decimal tasaMargen = 0.1m)
    {
        var cantidadConSigno = fill.Side == Side.Buy ? fill.Cantidad : -fill.Cantidad;
        var lote = CalculadoraLotes.AbrirLote(cantidadConSigno, fill.PrecioFill, tasaMargen);

        portfolio.LotesVivos.Add(lote);
        portfolio.Cash -= lote.Margin;
        portfolio.Margin += lote.Margin;
    }
}
