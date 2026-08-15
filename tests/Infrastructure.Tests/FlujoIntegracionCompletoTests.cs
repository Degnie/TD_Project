using TD_Project.Application;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Regimen;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy.Dsl;
using TD_Project.Infrastructure;
using Xunit;

namespace TD_Project.Infrastructure.Tests;

// spec: RN-15 a RN-19, CU-21 a CU-24 — prueba de integracion del flujo completo pedido por el
// auditor: Dataset valido -> Estrategia DSL valida -> Backtest -> Comparacion de gestores ->
// Reporte por regimen. No sustituye los tests unitarios de cada pieza; verifica que las piezas
// nuevas funcionan juntas de punta a punta.
public class FlujoIntegracionCompletoTests : IDisposable
{
    private readonly string _carpetaTemporal = Path.Combine(Path.GetTempPath(), "td-project-integracion-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
            Directory.Delete(_carpetaTemporal, recursive: true);
    }

    private const string JsonSmaBuy = """
    {
      "condicion": { "indicador": "SMA", "periodo": 1, "operador": ">", "campo": "Close" },
      "accion": { "side": "Buy", "type": "Market" }
    }
    """;

    private static Candle[] VelasDeEjemplo() =>
        Enumerable.Range(0, 25)
            .Select(i => new Candle(i, 100m + 5m * i, 100m + 5m * i, 100m + 5m * i, 100m + 5m * i, 500m))
            .ToArray();

    [Fact]
    public void ElFlujoCompletoDeIngestionDslRecomendacionYRegimenFunciona()
    {
        // 1. Dataset valido -> ingestion (RN-15, CU-21)
        var repositorio = new DatasetRepositoryLocal(_carpetaTemporal);
        var velas = VelasDeEjemplo();
        var hash = repositorio.Guardar("dataset-integracion", velas);
        var velasRecuperadas = repositorio.Obtener(hash);
        Assert.NotNull(velasRecuperadas);

        // 2. Estrategia DSL valida (RN-16)
        var estrategia = InterpreteDsl.CargarDesdeJson(JsonSmaBuy);

        // 3. Backtest (RN-13, ya congelado)
        var config = new ConfiguracionExperimento(CapitalInicial: 1000m, Velas: velasRecuperadas!);
        var resultado = BacktestRunner.Ejecutar(config, estrategia);
        Assert.Equal(EstadoBacktest.Success, resultado.Estado);

        // 4. Comparacion de gestores (RN-18, CU-23) — reutiliza gestores ya existentes (RN-17)
        var gestores = new IGestorRiesgo[] { new GestorFixedFractional(0.1m), new GestorFixedRisk(50m) };
        var recomendacion = CapitalManagerRecommender.Recomendar(config, estrategia, gestores);
        Assert.Equal(gestores.Length, recomendacion.Resultados.Count);

        // 5. Reporte por regimen (RN-19, CU-24) — asociacion explicita via Trade.TimestampApertura,
        // sin reconstruccion por orden de listas
        var reporte = ReporteRegimen.Calcular(resultado.Trades, velasRecuperadas!);
        Assert.All(resultado.Trades, t => Assert.True(t.TimestampApertura >= 0));
        Assert.NotNull(reporte);
    }
}
