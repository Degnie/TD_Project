using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Exploration;

// Estrategia sintetica de auditoria (Ronda 2, Tarea B): intenta forzar una divergencia
// real entre trayectorias A/B (RN-11) por el UNICO camino que BacktestRunner ejecuta
// realmente (ResolutorVela.Resolver, sin OCO — ver hallazgo: ResolverOco existe en
// Domain pero no esta cableado desde Application/BacktestRunner). No se puede usar Buy+Sell
// en la misma bolsa (RN-14 la rechazaria entera), asi que se prueban DOS variantes con un
// solo lado por ciclo, ambas dentro del rango H/L de la vela objetivo para forzar que
// RangoCruzaLimit/RangoCruzaStop decidan (no el gap de Open):
//  - Modo "DosLimits": dos Buy Limit del mismo lado a distintos precios en la misma vela.
//  - Modo "StopLimit": una Stop-Limit (CU-13) cuyo Stop y Limit estan ambos dentro del rango.
public enum ModoDivergencia { DosLimits, StopLimit }

public sealed class EstrategiaDivergenciaAB : IStrategy
{
    private readonly ModoDivergencia _modo;
    private bool _emitida;

    public EstrategiaDivergenciaAB(ModoDivergencia modo) => _modo = modo;

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        if (_emitida || dataSlice.N != 0)
            return Array.Empty<OrderRequest>();

        _emitida = true;
        // Vela N+1 (indice 1): Open=100.2 High=100.75 Low=99.57 Close=100.57
        return _modo switch
        {
            ModoDivergencia.DosLimits => new OrderRequest[]
            {
                new(Side.Buy, OrderType.Limit, 1m, PrecioLimite: 99.8m),
                new(Side.Buy, OrderType.Limit, 1m, PrecioLimite: 99.6m),
            },
            ModoDivergencia.StopLimit => new OrderRequest[]
            {
                // CU-13: Stop Buy a 100.6 (dispara si sube), Limit a 100.7 (techo a pagar).
                // Ambos dentro de [Low=99.57, High=100.75].
                new(Side.Buy, OrderType.StopLimit, 1m, PrecioLimite: 100.7m, PrecioStop: 100.6m),
            },
            _ => throw new InvalidOperationException()
        };
    }
}
