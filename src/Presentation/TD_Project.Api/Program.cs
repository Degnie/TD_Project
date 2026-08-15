using TD_Project.Api.Demo;
using TD_Project.Api.Mapping;
using TD_Project.Application;
using TD_Project.Contracts;
using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;
using TD_Project.Domain.Strategy.Dsl;
using TD_Project.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
// spec: RN-15, CU-21 — Opcion A del ADR-001: catalogo local en disco, carpeta propia bajo la
// app, independiente de cualquier otro almacenamiento (ej. caso11, ya auditado, no se toca).
builder.Services.AddSingleton<IDatasetRepository>(
    _ => new DatasetRepositoryLocal(Path.Combine(AppContext.BaseDirectory, "dataset-catalogo")));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// spec: RNF-08, RNF-13 — ejecucion sincrona sin estado entre requests; cada POST corre el
// backtest demo desde cero y devuelve el ResultDto directo, sin persistencia ni runId.
// Sin cambios respecto al endpoint ya auditado.
app.MapPost("/api/backtest/run", () =>
{
    var config = DatasetDemo.Configuracion();
    var resultado = BacktestRunner.Ejecutar(config, new EstrategiaDemo());
    return Results.Ok(ResultDtoMapper.Mapear(resultado, config));
});

// spec: RN-15, CU-21 — Dataset ingestion: subida de velas 1m, validacion, persistencia en
// catalogo local, retorna DatasetHash. Grupo separado conceptualmente de backtest/strategy DSL.
var datasets = app.MapGroup("/api/datasets");

datasets.MapPost("/", (IngestarDatasetRequestDto request, IDatasetRepository repositorio) =>
{
    var velas = request.Velas.Select(v => new Candle(v.Timestamp, v.Open, v.High, v.Low, v.Close, v.Volume)).ToList();
    try
    {
        var hash = repositorio.Guardar(request.Nombre, velas);
        return Results.Ok(new IngestarDatasetResponseDto(hash));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

datasets.MapGet("/", (IDatasetRepository repositorio) =>
    Results.Ok(repositorio.ListarCatalogo().Select(e => new CatalogoDatasetEntradaDto(e.Nombre, e.Hash)).ToList()));

// spec: RN-16, CU-22 — Strategy execution: carga una estrategia DSL JSON valida, la ejecuta
// contra un dataset ya ingerido via BacktestRunner (sin cambios), emite el ResultDto completo.
app.MapPost("/api/strategies/dsl/run", (EjecutarDslRequestDto request, IDatasetRepository repositorio) =>
{
    var velas = repositorio.Obtener(request.DatasetHash);
    if (velas is null)
        return Results.NotFound($"Dataset no encontrado: {request.DatasetHash}");

    IStrategy estrategia;
    try
    {
        estrategia = InterpreteDsl.CargarDesdeJson(request.EstrategiaDslJson);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    var config = new ConfiguracionExperimento(CapitalInicial: request.CapitalInicial, Velas: velas);
    var resultado = BacktestRunner.Ejecutar(config, estrategia);
    return Results.Ok(ResultDtoMapper.Mapear(resultado, config));
});

// spec: RN-18, CU-23 — Manager recommendation: evalua la estrategia DSL contra los Gestores de
// Capital pre-cargados (RN-17) y recomienda el de mejor CR, o inadaptabilidad si todos liquidan.
app.MapPost("/api/capital-managers/recommend", (RecomendarGestorRequestDto request, IDatasetRepository repositorio) =>
{
    var velas = repositorio.Obtener(request.DatasetHash);
    if (velas is null)
        return Results.NotFound($"Dataset no encontrado: {request.DatasetHash}");

    IStrategy estrategia;
    try
    {
        estrategia = InterpreteDsl.CargarDesdeJson(request.EstrategiaDslJson);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    var config = new ConfiguracionExperimento(CapitalInicial: request.CapitalInicial, Velas: velas);
    var gestores = new IGestorRiesgo[] { new GestorFixedFractional(0.1m), new GestorFixedRisk(50m), new GestorVolatilitySizing(20, 0.1m, 1m) };
    var recomendacion = CapitalManagerRecommender.Recomendar(config, estrategia, gestores);

    var resultados = recomendacion.Resultados
        .Select(r => new ResultadoGestorDto(r.IdentidadGestor, r.PnLTotal, r.MaxDrawdown, r.Cr, r.CuentaLiquidada))
        .ToList();
    return Results.Ok(new RecomendarGestorResponseDto(resultados, recomendacion.GestorRecomendado));
});

app.Run();

public partial class Program { }
