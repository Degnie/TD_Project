using TD_Project.Api.Demo;
using TD_Project.Api.Mapping;
using TD_Project.Application;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// spec: RNF-08, RNF-13 — ejecucion sincrona sin estado entre requests; cada POST corre el
// backtest demo desde cero y devuelve el ResultDto directo, sin persistencia ni runId.
app.MapPost("/api/backtest/run", () =>
{
    var config = DatasetDemo.Configuracion();
    var resultado = BacktestRunner.Ejecutar(config, new EstrategiaDemo());
    return Results.Ok(ResultDtoMapper.Mapear(resultado, config));
});

app.Run();

public partial class Program { }
