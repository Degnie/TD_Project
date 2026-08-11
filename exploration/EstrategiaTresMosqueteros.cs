using TD_Project.Domain.Shared;
using TD_Project.Domain.Strategy;

namespace TD_Project.Exploration;

// Estrategia "Los Tres Mosqueteros" (fuente: usuario, patrón encontrado en internet).
// Mercado dividido en cuadrantes FIJOS de 5 velas, anclados a la posición absoluta en el
// dataset (N%5), no a un contador interno de la estrategia. Vela 3 del cuadrante (N%5==2,
// 0-indexed) es la vela analizada; si es verde se abre Buy, si es roja se abre Sell, ejecutado
// contra la Vela 4 (N+1) por el desfase RN-13. El calendario del mercado no se detiene porque
// la estrategia esté ocupada esperando el cierre de una apuesta anterior: la señal del
// cuadrante N se evalúa siempre en N%5==2, exista o no una operación en curso en ese momento.
//
// Traducción a órdenes reales (fixture de auditoría, no regla de dominio): abrir con el mismo
// lado que el color apostado (Buy si se apuesta verde, Sell si se apuesta roja) y cerrar con el
// lado OPUESTO al de apertura — nunca "Sell fijo": si la apuesta abrió con Sell, cerrar también
// con Sell agrandaría la posición corta en vez de cerrarla.
//
// Desfase N/N+1 (RN-13): Observar es invocado con dataSlice hasta N; cualquier orden devuelta
// se ejecuta contra Velas[N+1], nunca contra Velas[N]. Cada Buy/Sell de apertura (o reapertura
// por martingala) exige una llamada de "espera" antes de poder leer su resultado (fase interna
// EsperandoCierre), pero esa espera NUNCA desplaza el cálculo de la próxima señal de cuadrante,
// que sigue leyendo N%5 directamente en cada llamada.
public sealed class EstrategiaTresMosqueteros : IStrategy
{
    private readonly int _maxMartingalas;

    // Instrumentacion OPCIONAL exclusiva de analisis (no forma parte de la logica de decision):
    // invocado una sola vez por operacion completa, al resolverse (gana o agota martingalas).
    // La estrategia es la unica fuente de verdad sobre que intentos pertenecen a la misma
    // operacion logica; un observador externo (ej. contar Trades por Side consecutivo) no puede
    // distinguir con certeza una martingala de una operacion nueva que coincide en direccion.
    private readonly Action<InfoOperacionResuelta>? _onOperacionResuelta;
    private int _siguienteOperacionId = 1;
    private int _operacionIdActual;
    private long _timestampEntradaActual;

    // EsperandoCierre: se emitió Buy/Sell en el ciclo anterior (Pending), la vela actual ya es
    // el resultado de esa ejecución. EsperandoReapertura: la apuesta perdió y hay martingalas
    // restantes; la reapertura se difiere un ciclo (RN-14: no puede mezclarse Buy+Sell en la
    // misma bolsa junto al cierre).
    private enum Fase { Ninguna, EsperandoCierre, EsperandoReapertura }

    private Fase _fase = Fase.Ninguna;
    private Side? _colorApostado;
    private int _martingalasUsadas;

    public EstrategiaTresMosqueteros(int maxMartingalas, Action<InfoOperacionResuelta>? onOperacionResuelta = null)
    {
        if (maxMartingalas is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(maxMartingalas), "Casos definidos: 0, 1 o 2 martingalas.");
        _maxMartingalas = maxMartingalas;
        _onOperacionResuelta = onOperacionResuelta;
    }

    public IReadOnlyList<OrderRequest> Observar(DataSlice dataSlice)
    {
        var velaActual = dataSlice.VelaActual;

        if (_fase == Fase.EsperandoCierre)
        {
            var acerto = ColorDe(velaActual) == _colorApostado;
            var ladoCierre = Opuesto(_colorApostado!.Value);

            if (acerto || _martingalasUsadas >= _maxMartingalas)
            {
                _fase = Fase.Ninguna;
                _onOperacionResuelta?.Invoke(new InfoOperacionResuelta(_operacionIdActual, _martingalasUsadas, acerto, _timestampEntradaActual, velaActual.Timestamp));
                return new[] { new OrderRequest(ladoCierre, OrderType.Market, 1m) };
            }

            _martingalasUsadas++;
            _fase = Fase.EsperandoReapertura;
            return new[] { new OrderRequest(ladoCierre, OrderType.Market, 1m) };
        }

        if (_fase == Fase.EsperandoReapertura)
        {
            _fase = Fase.EsperandoCierre;
            return new[] { new OrderRequest(_colorApostado!.Value, OrderType.Market, 1m) };
        }

        // Sin apuesta en curso: evaluar si ESTA vela (por su posición absoluta N) es la vela 3
        // del cuadrante. No hay contador interno — el cuadrante se calcula siempre igual,
        // exista o no una apuesta que recién se resolvió en este mismo ciclo.
        if (dataSlice.N % 5 != 2)
            return Array.Empty<OrderRequest>();

        var colorReferencia = ColorDe(velaActual);
        if (colorReferencia is null)
            return Array.Empty<OrderRequest>(); // vela doji (Open == Close): sin señal

        _colorApostado = colorReferencia;
        _martingalasUsadas = 0;
        _fase = Fase.EsperandoCierre;
        _operacionIdActual = _siguienteOperacionId++;
        _timestampEntradaActual = velaActual.Timestamp;
        return new[] { new OrderRequest(colorReferencia.Value, OrderType.Market, 1m) };
    }

    private static Side? ColorDe(Candle vela) =>
        vela.Close > vela.Open ? Side.Buy
        : vela.Close < vela.Open ? Side.Sell
        : null;

    private static Side Opuesto(Side lado) => lado == Side.Buy ? Side.Sell : Side.Buy;
}

// Instrumentacion de analisis: resultado consolidado de UNA operacion logica completa (intento
// inicial + martingalas), no de cada Trade individual del motor.
// TimestampEntrada/TimestampResolucion (D-040): timestamp de la vela donde la estrategia ya tiene
// el dato en mano (DataSlice.VelaActual.Timestamp) en el ciclo de apertura y en el de resolucion
// final, respectivamente. Sin martingala ambos coinciden con la misma vela de senal; con
// martingala, TimestampResolucion queda varias velas despues de TimestampEntrada.
public sealed record InfoOperacionResuelta(int OperacionId, int MartingalasUsadas, bool Gano, long TimestampEntrada, long TimestampResolucion);
