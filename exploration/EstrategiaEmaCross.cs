using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Exploration;

// Estrategia "EMA Cross" (Fase 1.6-D, D-054): estrategia de validacion del pipeline, deliberadamente
// distinta de Tres Mosqueteros/MHI Mayoria (ESPECIFICACION_EMA_CROSS_V1.md). Sin cuadrantes N%5,
// sin martingala/reintentos, senal por cruce de EMA corta/larga (tendencia) en vez de color de vela
// puntual (patron). Cierra por cruce contrario (Opcion aprobada por auditoria §2.3) — sin limite de
// velas, a diferencia de las estrategias existentes.
public sealed class EstrategiaEmaCross : IStrategy
{
    private readonly int _periodoCorta;
    private readonly int _periodoLarga;

    // Misma instrumentacion opcional que las estrategias existentes (InfoOperacionResuelta,
    // D-040) — MartingalasUsadas siempre 0 (sin reintentos, D-055: hallazgo documentado, no oculto).
    private readonly Action<InfoOperacionResuelta>? _onOperacionResuelta;
    private int _siguienteOperacionId = 1;
    private int _operacionIdActual;
    private long _timestampEntradaActual;

    private Side? _posicionAbierta;
    private decimal _precioEntradaActual;

    // Estado incremental de EMA (una pasada, O(1) por vela) — evita recalcular el historial
    // completo en cada Observar (ver nota en CalcularEma sobre el costo O(n^2) de la version
    // anterior, corregido tras detectar tiempos de ejecucion excesivos sobre 1m/~500k velas).
    private int _velasVistas;
    private decimal _sumaSemillaCorta;
    private decimal _sumaSemillaLarga;
    private decimal? _emaCortaActual;
    private decimal? _emaLargaActual;
    private decimal? _emaCortaPrevia;
    private decimal? _emaLargaPrevia;

    public EstrategiaEmaCross(int periodoEmaCorta, int periodoEmaLarga, Action<InfoOperacionResuelta>? onOperacionResuelta = null)
    {
        if (periodoEmaCorta <= 0 || periodoEmaLarga <= 0)
            throw new ArgumentOutOfRangeException(nameof(periodoEmaCorta), "Los periodos de EMA deben ser positivos.");
        if (periodoEmaLarga <= periodoEmaCorta)
            throw new ArgumentOutOfRangeException(nameof(periodoEmaLarga), "PeriodoEmaLarga debe ser mayor que PeriodoEmaCorta.");
        _periodoCorta = periodoEmaCorta;
        _periodoLarga = periodoEmaLarga;
        _onOperacionResuelta = onOperacionResuelta;
    }

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        var velaActual = dataSlice.VelaActual;

        // BacktestRunner invoca Observar exactamente una vez por n, en orden creciente (verificado
        // contra BacktestRunner.cs: "for (var n = 0; n < config.Velas.Count - 1; n++)") — la
        // actualizacion incremental de EMA depende de esta garantia. Guarda defensiva: si algun
        // dia el motor invocara Observar mas de una vez para el mismo n (o fuera de orden), esta
        // condicion lo detecta en vez de corromper silenciosamente el estado de la EMA.
        if (dataSlice.N != _velasVistas)
            throw new InvalidOperationException(
                $"EstrategiaEmaCross requiere que Observar se invoque exactamente una vez por vela, en orden creciente de N. Esperado N={_velasVistas}, recibido N={dataSlice.N}.");
        _velasVistas++;

        ActualizarEma(velaActual.Close);

        // ConfiguracionExperimento.Warmup solo es guarda de tamano minimo de dataset en
        // BacktestRunner (CU-03) — no salta velas del loop. Esta condicion es la via real por la
        // que la estrategia ignora los primeros ciclos, hasta tener ambas EMA calculadas (§5 de
        // la especificacion: se propuso Warmup=PeriodoEmaLarga como intencion, esta guarda la cumple).
        if (_emaCortaActual is null || _emaLargaActual is null || _emaCortaPrevia is null || _emaLargaPrevia is null)
            return Array.Empty<OrderRequest>();

        var emaCortaActual = _emaCortaActual.Value;
        var emaLargaActual = _emaLargaActual.Value;
        var emaCortaPrevia = _emaCortaPrevia.Value;
        var emaLargaPrevia = _emaLargaPrevia.Value;

        var cruceHaciaArriba = emaCortaPrevia <= emaLargaPrevia && emaCortaActual > emaLargaActual;
        var cruceHaciaAbajo = emaCortaPrevia >= emaLargaPrevia && emaCortaActual < emaLargaActual;

        var ordenes = new List<OrderRequest>();

        // §2.4: cierre y apertura contraria pueden coexistir en la misma bolsa (RN-14), igual que
        // el cierre+reapertura por martingala de las estrategias existentes.
        if (_posicionAbierta == Side.Buy && cruceHaciaAbajo)
        {
            ordenes.Add(new OrderRequest(Side.Sell, OrderType.Market, 1m));
            _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(_operacionIdActual, MartingalasUsadas: 0, Gano: velaActual.Close >= _precioEntradaActual, _timestampEntradaActual, velaActual.Timestamp));
            _posicionAbierta = null;
        }
        else if (_posicionAbierta == Side.Sell && cruceHaciaArriba)
        {
            ordenes.Add(new OrderRequest(Side.Buy, OrderType.Market, 1m));
            _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(_operacionIdActual, MartingalasUsadas: 0, Gano: velaActual.Close <= _precioEntradaActual, _timestampEntradaActual, velaActual.Timestamp));
            _posicionAbierta = null;
        }

        if (_posicionAbierta is null)
        {
            if (cruceHaciaArriba)
            {
                ordenes.Add(new OrderRequest(Side.Buy, OrderType.Market, 1m));
                _posicionAbierta = Side.Buy;
                _operacionIdActual = _siguienteOperacionId++;
                _timestampEntradaActual = velaActual.Timestamp;
                _precioEntradaActual = velaActual.Close;
            }
            else if (cruceHaciaAbajo)
            {
                ordenes.Add(new OrderRequest(Side.Sell, OrderType.Market, 1m));
                _posicionAbierta = Side.Sell;
                _operacionIdActual = _siguienteOperacionId++;
                _timestampEntradaActual = velaActual.Timestamp;
                _precioEntradaActual = velaActual.Close;
            }
        }

        return ordenes;
    }

    // Ganancia definida por direccion de la posicion: Buy gana si el precio de cierre en la vela
    // de resolucion es >= precio de entrada, Sell gana si es <=. Compara contra el precio de
    // ENTRADA real (guardado al abrir), no contra el color de la vela de resolucion — una
    // posicion EMA Cross puede durar muchas velas (§2.3), a diferencia de Tres Mosqueteros/MHI
    // Mayoria donde cada ciclo es una sola vela y "color de vela" y "direccion de precio" coinciden.
    // Sin PnL monetario (Caso 2, fuera de alcance) — solo direccion favorable/desfavorable.

    // EMA[i] = Close[i]*k + EMA[i-1]*(1-k), k = 2/(periodo+1). Semilla: promedio simple de las
    // primeras `periodo` velas (ESPECIFICACION_EMA_CROSS_V1.md §2.1). Actualizacion incremental
    // (O(1) por vela, O(n) total) — equivalente matematicamente al recalculo completo desde cero
    // sobre el mismo historial, pero factible en tiempo sobre datasets de cientos de miles de velas.
    private void ActualizarEma(decimal close)
    {
        _emaCortaPrevia = _emaCortaActual;
        _emaLargaPrevia = _emaLargaActual;

        var indiceVelaActual = _velasVistas - 1; // 0-indexed, ya incrementado en Observar

        if (indiceVelaActual < _periodoCorta)
            _sumaSemillaCorta += close;
        if (indiceVelaActual == _periodoCorta - 1)
            _emaCortaActual = _sumaSemillaCorta / _periodoCorta;
        else if (indiceVelaActual >= _periodoCorta)
            _emaCortaActual = close * KDe(_periodoCorta) + _emaCortaActual!.Value * (1 - KDe(_periodoCorta));

        if (indiceVelaActual < _periodoLarga)
            _sumaSemillaLarga += close;
        if (indiceVelaActual == _periodoLarga - 1)
            _emaLargaActual = _sumaSemillaLarga / _periodoLarga;
        else if (indiceVelaActual >= _periodoLarga)
            _emaLargaActual = close * KDe(_periodoLarga) + _emaLargaActual!.Value * (1 - KDe(_periodoLarga));
    }

    private static decimal KDe(int periodo) => 2m / (periodo + 1);
}

// Ficha reutiliza InfoOperacionResuelta (definido en EstrategiaTresMosqueteros.cs) — mismo tipo,
// mismo namespace TD_Project.Exploration, sin duplicar el record.
