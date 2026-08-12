using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Exploration;

// Estrategia "Neutral" (Caso 3A, segunda familia de D-086): control experimental determinista, no
// una estrategia candidata. Decide exclusivamente por posicion secuencial (DataSlice.N) — nunca
// lee Open/High/Low/Volume para decidir direccion ni momento de apertura/cierre (esa es la
// propiedad esencial de "sin mercado", ver ESPECIFICACION_IMPLEMENTACION_ESTRATEGIA_NEUTRAL_
// CASO3_V1.md §1). No aleatoria — 100% determinista y reproducible sin semilla, verificable a
// mano contando velas. Ciclo=10, alternancia estricta Buy->Sell->Buy..., 5 velas por posicion.
// Sin martingala/reintentos (mismo perfil que EMA Cross/Z-Score en este eje, MartingalasUsadas
// siempre 0, D-055).
public sealed class EstrategiaNeutral : IStrategy
{
    private readonly int _ciclo;

    // Misma instrumentacion opcional que las estrategias existentes (InfoOperacionResuelta, D-040).
    private readonly Action<InfoOperacionResuelta>? _onOperacionResuelta;
    private int _siguienteOperacionId = 1;
    private int _operacionIdActual;
    private long _timestampEntradaActual;

    private Side? _posicionAbierta;
    private decimal _precioEntradaActual;

    public EstrategiaNeutral(int ciclo, Action<InfoOperacionResuelta>? onOperacionResuelta = null)
    {
        if (ciclo <= 1 || ciclo % 2 != 0)
            throw new ArgumentOutOfRangeException(nameof(ciclo), "Ciclo debe ser un entero par mayor que 1 (para que Ciclo/2 sea un punto de ciclo entero).");
        _ciclo = ciclo;
        _onOperacionResuelta = onOperacionResuelta;
    }

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        var velaActual = dataSlice.VelaActual;
        var n = dataSlice.N;
        var mitad = _ciclo / 2;
        var posicionEnCiclo = n % _ciclo;

        var ordenes = new List<OrderRequest>();

        // Punto de mitad de ciclo: cerrar Buy y abrir Sell (solo si hay una posicion Buy abierta
        // que corresponde a este ciclo — evita cerrar en el primer ciclo antes de haber abierto).
        if (posicionEnCiclo == mitad && _posicionAbierta == Side.Buy)
        {
            ordenes.Add(new OrderRequest(Side.Sell, OrderType.Market, 1m));
            _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(_operacionIdActual, MartingalasUsadas: 0, Gano: velaActual.Close >= _precioEntradaActual, _timestampEntradaActual, velaActual.Timestamp));

            ordenes.Add(new OrderRequest(Side.Sell, OrderType.Market, 1m));
            _posicionAbierta = Side.Sell;
            _operacionIdActual = _siguienteOperacionId++;
            _timestampEntradaActual = velaActual.Timestamp;
            _precioEntradaActual = velaActual.Close;
            return ordenes;
        }

        // Punto de inicio de ciclo: cerrar Sell (si viene de un ciclo anterior) y abrir Buy.
        if (posicionEnCiclo == 0)
        {
            if (_posicionAbierta == Side.Sell)
            {
                ordenes.Add(new OrderRequest(Side.Buy, OrderType.Market, 1m));
                _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(_operacionIdActual, MartingalasUsadas: 0, Gano: velaActual.Close <= _precioEntradaActual, _timestampEntradaActual, velaActual.Timestamp));
            }

            ordenes.Add(new OrderRequest(Side.Buy, OrderType.Market, 1m));
            _posicionAbierta = Side.Buy;
            _operacionIdActual = _siguienteOperacionId++;
            _timestampEntradaActual = velaActual.Timestamp;
            _precioEntradaActual = velaActual.Close;
            return ordenes;
        }

        return ordenes;
    }
}
