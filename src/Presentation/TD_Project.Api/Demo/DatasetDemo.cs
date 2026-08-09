using TD_Project.Application;
using TD_Project.Domain.Shared;

namespace TD_Project.Api.Demo;

// spec: deuda documentada en PENDIENTES.md — configuracion fija de demostracion para el
// endpoint POST /api/backtest/run. No representa ingestion real de datasets (hueco abierto
// desde el inicio del proyecto); vive exclusivamente en Presentation.Api.Demo, nunca en
// Domain/Application/Infrastructure.
public static class DatasetDemo
{
    public static ConfiguracionExperimento Configuracion() => new(
        CapitalInicial: 1000m,
        Velas: new[]
        {
            new Candle(1, 100m, 105m, 95m, 102m, 500m),
            new Candle(2, 102m, 106m, 100m, 104m, 500m),
            new Candle(3, 104m, 112m, 102m, 110m, 500m),
            new Candle(4, 110m, 114m, 108m, 112m, 500m)
        });
}
