using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Exploration;

// Estrategia "Volumen + Breakout" (Caso 3B, D-102 a D-107): familia jerarquica de decision —
// volumen (condicion primaria, contexto de participacion) habilita la evaluacion de breakout
// (condicion secundaria, ruptura de rango). Si la primaria no se cumple, la secundaria no se
// evalua (D-099). Bidireccional (D-105 ampliada): ruptura de Maximo20Anterior -> senal Long,
// ruptura de Minimo20Anterior -> senal Short, ambos extremos excluyen la vela actual. Cierre
// exclusivamente por senal contraria (D-107) — la misma regla jerarquica evaluada en sentido
// opuesto a la posicion abierta, nunca por perdida de volumen ni por stop de precio aislado.
// Sin martingala, una posicion maxima abierta (D-104, hereda el patron de Z-Score/Neutral).
public sealed class EstrategiaVolumenBreakout : IStrategy
{
    private readonly CondicionVolumen _condicionVolumen;
    private readonly CondicionBreakout _condicionBreakout;
    private readonly Action<ResultadoEvaluacionCondiciones>? _onEvaluacion;
    private readonly Action<InfoOperacionResuelta>? _onOperacionResuelta;

    private Side? _posicionAbierta;
    private int _siguienteOperacionId = 1;
    private int _operacionIdActual;
    private long _timestampEntradaActual;
    private decimal _precioEntradaActual;

    public EstrategiaVolumenBreakout(
        int ventanaVolumen = 20, decimal multiploVolumen = 1.5m, int ventanaBreakout = 20,
        Action<ResultadoEvaluacionCondiciones>? onEvaluacion = null,
        Action<InfoOperacionResuelta>? onOperacionResuelta = null)
    {
        _condicionVolumen = new CondicionVolumen(ventanaVolumen, multiploVolumen);
        _condicionBreakout = new CondicionBreakout(ventanaBreakout);
        _onEvaluacion = onEvaluacion;
        _onOperacionResuelta = onOperacionResuelta;
    }

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        var velaActual = dataSlice.VelaActual;

        var primariaCumple = _condicionVolumen.Evaluar(velaActual.Volume);
        var (rupturaAlcista, rupturaBajista) = _condicionBreakout.Evaluar(velaActual.Close, velaActual.High, velaActual.Low);

        if (!primariaCumple)
        {
            _onEvaluacion?.Invoke(new ResultadoEvaluacionCondiciones(Primaria: false, Secundaria: null, DireccionSenal: null, AccionEvaluacion.Ninguna));
            return Array.Empty<OrderRequest>();
        }

        if (!rupturaAlcista && !rupturaBajista)
        {
            _onEvaluacion?.Invoke(new ResultadoEvaluacionCondiciones(Primaria: true, Secundaria: false, DireccionSenal: null, AccionEvaluacion.Ninguna));
            return Array.Empty<OrderRequest>();
        }

        var direccionSenal = rupturaAlcista ? Side.Buy : Side.Sell;
        return EmitirSegunSenal(direccionSenal, velaActual);
    }

    private List<OrderRequest> EmitirSegunSenal(Side direccionSenal, Candle velaActual)
    {
        var ordenes = new List<OrderRequest>();

        if (_posicionAbierta is null)
        {
            ordenes.Add(new OrderRequest(direccionSenal, OrderType.Market, 1m));
            AbrirPosicion(direccionSenal, velaActual);
            _onEvaluacion?.Invoke(new ResultadoEvaluacionCondiciones(Primaria: true, Secundaria: true, direccionSenal, AccionEvaluacion.OrdenEmitida));
            return ordenes;
        }

        if (_posicionAbierta == direccionSenal)
        {
            // Misma direccion de una posicion ya abierta: D-104, una posicion maxima, sin escalado.
            _onEvaluacion?.Invoke(new ResultadoEvaluacionCondiciones(Primaria: true, Secundaria: true, direccionSenal, AccionEvaluacion.OrdenOmitidaPorPosicionAbierta));
            return ordenes;
        }

        // Senal contraria a la posicion abierta (D-107): cierre explicito + apertura en la nueva
        // direccion, mismo patron de dos ordenes ya usado por EstrategiaNeutral en su punto de
        // reversion (Cantidad=1m fija no cruza cero por si sola con una sola orden).
        var cierre = _posicionAbierta == Side.Buy ? Side.Sell : Side.Buy;
        ordenes.Add(new OrderRequest(cierre, OrderType.Market, 1m));
        _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(
            _operacionIdActual, MartingalasUsadas: 0,
            Gano: _posicionAbierta == Side.Buy ? velaActual.Close >= _precioEntradaActual : velaActual.Close <= _precioEntradaActual,
            _timestampEntradaActual, velaActual.Timestamp));

        ordenes.Add(new OrderRequest(direccionSenal, OrderType.Market, 1m));
        AbrirPosicion(direccionSenal, velaActual);
        _onEvaluacion?.Invoke(new ResultadoEvaluacionCondiciones(Primaria: true, Secundaria: true, direccionSenal, AccionEvaluacion.OrdenEmitida));
        return ordenes;
    }

    private void AbrirPosicion(Side direccion, Candle velaActual)
    {
        _posicionAbierta = direccion;
        _operacionIdActual = _siguienteOperacionId++;
        _timestampEntradaActual = velaActual.Timestamp;
        _precioEntradaActual = velaActual.Close;
    }
}

// Condicion primaria (D-100, D-102, D-103=P3, D-105): volumen actual respecto a media movil de
// ventana fija. Estado propio (ventana deslizante O(1), mismo patron que EstrategiaZScoreReversion).
internal sealed class CondicionVolumen
{
    private readonly int _ventana;
    private readonly decimal _multiplo;
    private readonly Queue<decimal> _ventanaVolumen = new();
    private decimal _sumaVentana;

    public CondicionVolumen(int ventana, decimal multiplo)
    {
        if (ventana <= 1)
            throw new ArgumentOutOfRangeException(nameof(ventana), "Ventana debe ser mayor que 1.");
        if (multiplo <= 0m)
            throw new ArgumentOutOfRangeException(nameof(multiplo), "Multiplo debe ser positivo.");
        _ventana = ventana;
        _multiplo = multiplo;
    }

    // Actualiza la ventana con el volumen de la vela actual y evalua la condicion sobre el estado
    // ya actualizado (a diferencia de CondicionBreakout, el volumen actual SI participa en su
    // propia media — D-105 solo exige exclusion de la vela actual para el extremo de breakout).
    public bool Evaluar(decimal volumenActual)
    {
        _ventanaVolumen.Enqueue(volumenActual);
        _sumaVentana += volumenActual;
        if (_ventanaVolumen.Count > _ventana)
            _sumaVentana -= _ventanaVolumen.Dequeue();

        if (_ventanaVolumen.Count < _ventana)
            return false; // warmup: sin suficiente historia para una media representativa.

        var media = _sumaVentana / _ventana;
        return volumenActual > media * _multiplo;
    }
}

// Condicion secundaria (D-100, D-102, D-103=S2, D-105 ampliada): ruptura del maximo/minimo de una
// ventana fija que EXCLUYE la vela actual (D-105). Estado propio: dos ventanas deslizantes
// (High y Low) sobre la misma ventana temporal.
internal sealed class CondicionBreakout
{
    private readonly int _ventana;
    private readonly Queue<decimal> _ventanaMaximos = new();
    private readonly Queue<decimal> _ventanaMinimos = new();

    public CondicionBreakout(int ventana)
    {
        if (ventana <= 1)
            throw new ArgumentOutOfRangeException(nameof(ventana), "Ventana debe ser mayor que 1.");
        _ventana = ventana;
    }

    // Compara Close contra el maximo/minimo de las N velas ANTERIORES (excluyendo la vela actual,
    // D-105) y solo despues incorpora High/Low de la vela actual a las ventanas. Invertir este
    // orden reintroduciria la circularidad que D-105 descarto explicitamente.
    public (bool RupturaAlcista, bool RupturaBajista) Evaluar(decimal closeActual, decimal highActual, decimal lowActual)
    {
        var hayHistoriaSuficiente = _ventanaMaximos.Count >= _ventana;
        var rupturaAlcista = false;
        var rupturaBajista = false;

        if (hayHistoriaSuficiente)
        {
            var maximoAnterior = _ventanaMaximos.Max();
            var minimoAnterior = _ventanaMinimos.Min();
            rupturaAlcista = closeActual > maximoAnterior;
            rupturaBajista = closeActual < minimoAnterior;
        }

        _ventanaMaximos.Enqueue(highActual);
        _ventanaMinimos.Enqueue(lowActual);
        if (_ventanaMaximos.Count > _ventana)
        {
            _ventanaMaximos.Dequeue();
            _ventanaMinimos.Dequeue();
        }

        return (rupturaAlcista, rupturaBajista);
    }
}

// Observabilidad (D-101, D-104): callback opcional, mismo patron que InfoOperacionResuelta.
// DireccionSenal agregado tras D-105/D-107 (bidireccionalidad) para distinguir Long/Short.
public readonly record struct ResultadoEvaluacionCondiciones(
    bool Primaria,
    bool? Secundaria,
    Side? DireccionSenal,
    AccionEvaluacion Accion);

public enum AccionEvaluacion
{
    Ninguna,
    OrdenEmitida,
    OrdenOmitidaPorPosicionAbierta
}
