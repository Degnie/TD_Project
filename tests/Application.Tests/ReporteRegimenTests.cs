using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Regimen;
using TD_Project.Domain.Shared;
using Xunit;

namespace TD_Project.Application.Tests;

// spec: RN-19, CU-24 — cada Trade se asocia al regimen de mercado en que fue abierto usando
// TimestampApertura (asociacion explicita, sin reconstruccion por orden de listas ni inferencia
// sobre Fills/EquityCurve — decision explicita del cliente sobre RN-19).
public class ReporteRegimenTests
{
    private static Candle[] VelasAlcistas(int cantidad) =>
        Enumerable.Range(0, cantidad)
            .Select(i => new Candle(i, 100m + 5m * i, 100m + 5m * i, 100m + 5m * i, 100m + 5m * i, 0m))
            .ToArray();

    // spec: RN-19 — un Trade conserva su timestamp de apertura y el regimen se determina usando
    // la vela correspondiente a ese timestamp (no la ultima vela del dataset)
    [Fact]
    public void UnTradeSeAsociaAlRegimenDeSuVelaDeApertura()
    {
        var velas = VelasAlcistas(20);
        var trades = new[] { new Trade(CantidadInicial: 1m, PrecioApertura: 100m, PrecioCierre: 195m, RealizedPnL: 95m, TimestampApertura: 19, TimestampCierre: 19) };

        var reporte = ReporteRegimen.Calcular(trades, velas);

        var filaAlcista = reporte.Fases.Single(f => f.Regimen == Regimen.Alcista);
        Assert.Equal(1, filaAlcista.TotalTrades);
    }

    // spec: CU-24 — el reporte segmenta PnL/WinRate/Drawdown en las 3 fases (Alza/Baja/Horizontal)
    [Fact]
    public void ElReporteSegmentaPnLYWinRatePorFase()
    {
        var velas = VelasAlcistas(20);
        var trades = new[]
        {
            new Trade(CantidadInicial: 1m, PrecioApertura: 100m, PrecioCierre: 195m, RealizedPnL: 95m, TimestampApertura: 19, TimestampCierre: 19),
            new Trade(CantidadInicial: 1m, PrecioApertura: 100m, PrecioCierre: 90m, RealizedPnL: -10m, TimestampApertura: 19, TimestampCierre: 19)
        };

        var reporte = ReporteRegimen.Calcular(trades, velas);

        var filaAlcista = reporte.Fases.Single(f => f.Regimen == Regimen.Alcista);
        Assert.Equal(2, filaAlcista.TotalTrades);
        Assert.Equal(85m, filaAlcista.PnLTotal);
        Assert.Equal(0.5m, filaAlcista.WinRate);
    }

    // spec: RN-19 — el reporte indica explicitamente el regimen donde la estrategia obtiene mejor
    // desempeno (Regimen Optimo)
    [Fact]
    public void ElReporteIndicaElRegimenOptimo()
    {
        var velas = VelasAlcistas(20);
        var trades = new[] { new Trade(CantidadInicial: 1m, PrecioApertura: 100m, PrecioCierre: 195m, RealizedPnL: 95m, TimestampApertura: 19, TimestampCierre: 19) };

        var reporte = ReporteRegimen.Calcular(trades, velas);

        Assert.Equal(Regimen.Alcista, reporte.RegimenOptimo);
    }

    // spec: RN-19 — sin trades, el reporte no falla y no propone un regimen optimo espurio
    [Fact]
    public void SinTradesElReporteNoTieneRegimenOptimo()
    {
        var velas = VelasAlcistas(20);

        var reporte = ReporteRegimen.Calcular(Array.Empty<Trade>(), velas);

        Assert.Null(reporte.RegimenOptimo);
    }
}
