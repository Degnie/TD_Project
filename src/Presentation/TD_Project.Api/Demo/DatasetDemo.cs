using TD_Project.Application;
using TD_Project.Domain.Shared;

namespace TD_Project.Api.Demo;

// spec: deuda documentada en PENDIENTES.md — configuracion fija de demostracion para el
// endpoint POST /api/backtest/run. No representa ingestion real de datasets (hueco abierto
// desde el inicio del proyecto); vive exclusivamente en Presentation.Api.Demo, nunca en
// Domain/Application/Infrastructure.
// spec: RN-11 — vela en N=1 (Open=100/High=102/Low=90/Close=102) es el caso canonico de
// divergencia real entre trayectorias, ya usado en StopLimitTests.StopLimitPuedeDivergirEntreTrayectorias:
// Trayectoria A hace Fill @101 (Stop dispara subiendo directo al High, cruza el Limit de
// camino), Trayectoria B no hace Fill (el Stop dispara justo al llegar al High tras bajar
// primero, sin tramo restante hasta Close). Vela en N=2 permite que, si hay Fill, la posicion
// se cierre y se vea un Trade completo (Ciclo 4B: la demo debe mostrar la garantia RN-11, no
// solo un Market Buy sin divergencia posible).
public static class DatasetDemo
{
    public static ConfiguracionExperimento Configuracion() => new(
        CapitalInicial: 1000m,
        Velas: new[]
        {
            new Candle(1, 100m, 100m, 100m, 100m, 500m),
            new Candle(2, 100m, 102m, 90m, 102m, 500m),
            new Candle(3, 105m, 108m, 103m, 106m, 500m)
        });
}
