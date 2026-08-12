using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Exploration;

// Estrategia "Reversion a la Media por Z-Score" (Caso 3A, D-087): primera de las 2 familias
// nuevas requeridas por D-086. Senal por distribucion estadistica de la serie de Close (ventana
// deslizante, z-score), tercera categoria de origen de senal en el laboratorio junto a "color de
// vela" (patron, Tres Mosqueteros/MHI) y "cruce de indicador acumulado" (tendencia, EMA Cross).
// Sin cuadrantes, sin martingala/reintentos (D-055: hallazgo documentado, no oculto, MartingalasUsadas
// siempre 0). Parametros congelados por convencion estadistica externa, no calibrados sobre el
// dataset (mismo criterio D-030): Ventana=20 (Bandas de Bollinger estandar), UmbralEntrada=2.0
// (~95% de una distribucion normal), UmbralSalida=0.5.
// ESPECIFICACION_IMPLEMENTACION_ZSCORE_REVERSAL_V1.md: sin stop por falla de reversion (misma
// aceptacion que EMA Cross para posiciones sin limite de duracion), sin posiciones simultaneas.
public sealed class EstrategiaZScoreReversion : IStrategy
{
    private readonly int _ventana;
    private readonly decimal _umbralEntrada;
    private readonly decimal _umbralSalida;

    // Misma instrumentacion opcional que las estrategias existentes (InfoOperacionResuelta, D-040).
    private readonly Action<InfoOperacionResuelta>? _onOperacionResuelta;
    private int _siguienteOperacionId = 1;
    private int _operacionIdActual;
    private long _timestampEntradaActual;

    private Side? _posicionAbierta;
    private decimal _precioEntradaActual;

    // Ventana deslizante O(1) por vela (suma y suma de cuadrados) — evita el mismo bug O(n^2) ya
    // detectado y corregido en EMA Cross (recalcular el historial completo en cada Observar sobre
    // datasets de ~500k velas en 1m).
    private readonly Queue<decimal> _ventanaClose = new();
    private decimal _sumaVentana;
    private decimal _sumaCuadradosVentana;

    public EstrategiaZScoreReversion(int ventana, decimal umbralEntrada, decimal umbralSalida, Action<InfoOperacionResuelta>? onOperacionResuelta = null)
    {
        if (ventana <= 1)
            throw new ArgumentOutOfRangeException(nameof(ventana), "Ventana debe ser mayor que 1.");
        if (umbralEntrada <= 0m)
            throw new ArgumentOutOfRangeException(nameof(umbralEntrada), "UmbralEntrada debe ser positivo.");
        if (umbralSalida < 0m || umbralSalida >= umbralEntrada)
            throw new ArgumentOutOfRangeException(nameof(umbralSalida), "UmbralSalida debe ser no negativo y menor que UmbralEntrada.");
        _ventana = ventana;
        _umbralEntrada = umbralEntrada;
        _umbralSalida = umbralSalida;
        _onOperacionResuelta = onOperacionResuelta;
    }

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        var velaActual = dataSlice.VelaActual;
        ActualizarVentana(velaActual.Close);

        // Warmup: sin senal hasta acumular Ventana velas — mismo patron que EMA Cross durante el
        // calculo de la semilla de EMA.
        if (_ventanaClose.Count < _ventana)
            return Array.Empty<OrderRequest>();

        var media = _sumaVentana / _ventana;
        var varianza = _sumaCuadradosVentana / _ventana - media * media;
        // Guarda: error de redondeo acumulado de punto flotante puede producir una varianza
        // ligeramente negativa cuando la serie es casi constante — mismo tipo de guarda que
        // D-073/CalcularDrawdownMaximo aplico a "pico == 0m".
        if (varianza < 0m)
            varianza = 0m;
        var desviacion = (decimal)Math.Sqrt((double)varianza);

        // Desviacion 0 (ventana perfectamente constante): z-score no definido, sin senal posible.
        if (desviacion == 0m)
            return Array.Empty<OrderRequest>();

        var z = (velaActual.Close - media) / desviacion;
        var ordenes = new List<OrderRequest>();

        // Cierre por reversion: |z| volvio a estar dentro de UmbralSalida.
        if (_posicionAbierta == Side.Buy && z >= -_umbralSalida)
        {
            ordenes.Add(new OrderRequest(Side.Sell, OrderType.Market, 1m));
            _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(_operacionIdActual, MartingalasUsadas: 0, Gano: velaActual.Close >= _precioEntradaActual, _timestampEntradaActual, velaActual.Timestamp));
            _posicionAbierta = null;
        }
        else if (_posicionAbierta == Side.Sell && z <= _umbralSalida)
        {
            ordenes.Add(new OrderRequest(Side.Buy, OrderType.Market, 1m));
            _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(_operacionIdActual, MartingalasUsadas: 0, Gano: velaActual.Close <= _precioEntradaActual, _timestampEntradaActual, velaActual.Timestamp));
            _posicionAbierta = null;
        }

        // Sin posiciones simultaneas: solo se evalua entrada si no hay posicion abierta.
        if (_posicionAbierta is null)
        {
            // z muy negativo: precio muy por debajo de la media -> apuesta a que sube (Buy).
            if (z <= -_umbralEntrada)
            {
                ordenes.Add(new OrderRequest(Side.Buy, OrderType.Market, 1m));
                _posicionAbierta = Side.Buy;
                _operacionIdActual = _siguienteOperacionId++;
                _timestampEntradaActual = velaActual.Timestamp;
                _precioEntradaActual = velaActual.Close;
            }
            // z muy positivo: precio muy por encima de la media -> apuesta a que baja (Sell).
            else if (z >= _umbralEntrada)
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

    private void ActualizarVentana(decimal close)
    {
        _ventanaClose.Enqueue(close);
        _sumaVentana += close;
        _sumaCuadradosVentana += close * close;

        if (_ventanaClose.Count > _ventana)
        {
            var salida = _ventanaClose.Dequeue();
            _sumaVentana -= salida;
            _sumaCuadradosVentana -= salida * salida;
        }
    }
}
