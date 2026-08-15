using TD_Project.Contracts;
using Xunit;

namespace TD_Project.Contracts.Tests;

// spec: RNF-16 — ResultDto debe presentar las metricas acompanadas de descripciones interpretativas
// en espanol, aclarando que los resultados provienen exclusivamente de simulacion historica.
public class ExplicacionDtoTests
{
    // spec: RNF-16 — el aviso de simulacion historica es el texto acordado, literal, sin variacion
    [Fact]
    public void ElAvisoDeSimulacionHistoricaUsaElTextoAcordado()
    {
        var explicacion = new ExplicacionDto(
            Resumen: "La estrategia obtuvo un rendimiento positivo.",
            RegimenOptimoDescripcion: "Mejor desempeno en fase Alcista.",
            GestorRecomendadoDescripcion: "Gestor recomendado: Riesgo Porcentual 2%.",
            AvisoSimulacionHistorica: "Los resultados corresponden a simulacion historica y no garantizan resultados futuros.");

        Assert.Equal(
            "Los resultados corresponden a simulacion historica y no garantizan resultados futuros.",
            explicacion.AvisoSimulacionHistorica);
    }

    // spec: RNF-16 — ResultDto expone el campo Explicacion junto al resto de metricas
    [Fact]
    public void ResultDtoExponeUnaExplicacion()
    {
        var explicacion = new ExplicacionDto(
            Resumen: "resumen",
            RegimenOptimoDescripcion: "regimen",
            GestorRecomendadoDescripcion: "gestor",
            AvisoSimulacionHistorica: "aviso");
        var resultado = new ResultDto(
            Estado: "Success",
            ExperimentInfo: new ExperimentInfoDto(FechaInicioTimestamp: 1, FechaFinTimestamp: 10, TotalVelas: 10),
            EquityCurve: Array.Empty<EquityPointDto>(),
            Trades: Array.Empty<TradeDto>(),
            FillLog: Array.Empty<FillLogEntryDto>(),
            PortfolioSnapshots: Array.Empty<PortfolioSnapshotDto>(),
            Metrics: new MetricsDto(EquityFinal: 0m, PnLTotal: 0m, TotalTrades: 0),
            BranchResolutions: Array.Empty<BranchResolutionDto>(),
            Explicacion: explicacion);

        Assert.Equal(explicacion, resultado.Explicacion);
    }
}
